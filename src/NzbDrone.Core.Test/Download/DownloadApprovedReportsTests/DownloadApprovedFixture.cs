using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Clients;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Download.DownloadApprovedReportsTests
{
    [TestFixture]
    public class DownloadApprovedFixture : CoreTest<ProcessDownloadDecisions>
    {
        [SetUp]
        public void SetUp()
        {
            Mocker.GetMock<IPrioritizeDownloadDecision>()
                .Setup(v => v.PrioritizeDecisions(It.IsAny<List<DownloadDecision>>()))
                .Returns<List<DownloadDecision>>(v => v);
        }

        private Album GetAlbum(int id)
        {
            return Builder<Album>.CreateNew()
                            .With(e => e.Id = id)
                            .Build();
        }

        private RemoteAlbum GetRemoteAlbum(List<Album> albums, QualityModel quality, string downloadProtocol = "UsenetDownloadProtocol")
        {
            var remoteAlbum = new RemoteAlbum();
            remoteAlbum.ParsedAlbumInfo = new ParsedAlbumInfo();
            remoteAlbum.ParsedAlbumInfo.Quality = quality;

            remoteAlbum.Albums = new List<Album>();
            remoteAlbum.Albums.AddRange(albums);

            remoteAlbum.Release = new ReleaseInfo();
            remoteAlbum.Release.DownloadProtocol = downloadProtocol;
            remoteAlbum.Release.PublishDate = DateTime.UtcNow;

            remoteAlbum.Artist = Builder<Artist>.CreateNew()
                .With(e => e.QualityProfile = new QualityProfile { Items = Qualities.QualityFixture.GetDefaultQualities() })
                .Build();

            return remoteAlbum;
        }

        [Test]
        public async Task should_download_report_if_album_was_not_already_downloaded()
        {
            var albums = new List<Album> { GetAlbum(1) };
            var remoteAlbum = GetRemoteAlbum(albums, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteAlbum>()), Times.Once());
        }

        [Test]
        public async Task should_only_download_album_once()
        {
            var albums = new List<Album> { GetAlbum(1) };
            var remoteAlbum = GetRemoteAlbum(albums, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum));
            decisions.Add(new DownloadDecision(remoteAlbum));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteAlbum>()), Times.Once());
        }

        [Test]
        public  async Task should_not_download_if_any_album_was_already_downloaded()
        {
            var remoteAlbum1 = GetRemoteAlbum(
                                                    new List<Album> { GetAlbum(1) },
                                                    new QualityModel(Quality.MP3_192));

            var remoteAlbum2 = GetRemoteAlbum(
                                                    new List<Album> { GetAlbum(1), GetAlbum(2) },
                                                    new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteAlbum>()), Times.Once());
        }

        [Test]
        public async Task should_return_downloaded_reports()
        {
            var albums = new List<Album> { GetAlbum(1) };
            var remoteAlbum = GetRemoteAlbum(albums, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().HaveCount(1);
        }

        [Test]
        public async Task should_return_all_downloaded_reports()
        {
            var remoteAlbum1 = GetRemoteAlbum(
                                                    new List<Album> { GetAlbum(1) },
                                                    new QualityModel(Quality.MP3_192));

            var remoteAlbum2 = GetRemoteAlbum(
                                                    new List<Album> { GetAlbum(2) },
                                                    new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().HaveCount(2);
        }

        [Test]
        public async Task should_only_return_downloaded_reports()
        {
            var remoteAlbum1 = GetRemoteAlbum(
                                                    new List<Album> { GetAlbum(1) },
                                                    new QualityModel(Quality.MP3_192));

            var remoteAlbum2 = GetRemoteAlbum(
                                                    new List<Album> { GetAlbum(2) },
                                                    new QualityModel(Quality.MP3_192));

            var remoteAlbum3 = GetRemoteAlbum(
                                                    new List<Album> { GetAlbum(2) },
                                                    new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));
            decisions.Add(new DownloadDecision(remoteAlbum3));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().HaveCount(2);
        }

        [Test]
        public async Task should_not_add_to_downloaded_list_when_download_fails()
        {
            var albums = new List<Album> { GetAlbum(1) };
            var remoteAlbum = GetRemoteAlbum(albums, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum));

            Mocker.GetMock<IDownloadService>().Setup(s => s.DownloadReport(It.IsAny<RemoteAlbum>())).Throws(new Exception());

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().BeEmpty();

            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_return_an_empty_list_when_none_are_appproved()
        {
            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(new RemoteAlbum(), new Rejection("Failure!")));
            decisions.Add(new DownloadDecision(new RemoteAlbum(), new Rejection("Failure!")));

            Subject.GetQualifiedReports(decisions).Should().BeEmpty();
        }

        [Test]
        public async Task should_not_grab_if_pending()
        {
            var albums = new List<Album> { GetAlbum(1) };
            var remoteAlbum = GetRemoteAlbum(albums, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum, new Rejection("Failure!", RejectionType.Temporary)));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteAlbum>()), Times.Never());
        }

        [Test]
        public async Task should_not_add_to_pending_if_album_was_grabbed()
        {
            var albums = new List<Album> { GetAlbum(1) };
            var remoteAlbum = GetRemoteAlbum(albums, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum));
            decisions.Add(new DownloadDecision(remoteAlbum, new Rejection("Failure!", RejectionType.Temporary)));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IPendingReleaseService>().Verify(v => v.AddMany(It.IsAny<List<Tuple<DownloadDecision, PendingReleaseReason>>>()), Times.Never());
        }

        [Test]
        public async Task should_add_to_pending_even_if_already_added_to_pending()
        {
            var albums = new List<Album> { GetAlbum(1) };
            var remoteAlbum = GetRemoteAlbum(albums, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum, new Rejection("Failure!", RejectionType.Temporary)));
            decisions.Add(new DownloadDecision(remoteAlbum, new Rejection("Failure!", RejectionType.Temporary)));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IPendingReleaseService>().Verify(v => v.AddMany(It.IsAny<List<Tuple<DownloadDecision, PendingReleaseReason>>>()), Times.Once());
        }

        [Test]
        public async Task should_add_to_failed_if_already_failed_for_that_protocol()
        {
            var albums = new List<Album> { GetAlbum(1) };
            var remoteAlbum = GetRemoteAlbum(albums, new QualityModel(Quality.MP3_320));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum));
            decisions.Add(new DownloadDecision(remoteAlbum));

            Mocker.GetMock<IDownloadService>().Setup(s => s.DownloadReport(It.IsAny<RemoteAlbum>()))
                  .Throws(new DownloadClientUnavailableException("Download client failed"));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteAlbum>()), Times.Once());
        }

        [Test]
        public async Task should_not_add_to_failed_if_failed_for_a_different_protocol()
        {
            var albums = new List<Album> { GetAlbum(1) };
            var remoteAlbum = GetRemoteAlbum(albums, new QualityModel(Quality.MP3_320), nameof(UsenetDownloadProtocol));
            var remoteAlbum2 = GetRemoteAlbum(albums, new QualityModel(Quality.MP3_320), nameof(TorrentDownloadProtocol));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            Mocker.GetMock<IDownloadService>().Setup(s => s.DownloadReport(It.Is<RemoteAlbum>(r => r.Release.DownloadProtocol == nameof(UsenetDownloadProtocol))))
                  .Throws(new DownloadClientUnavailableException("Download client failed"));

            await Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.Is<RemoteAlbum>(r => r.Release.DownloadProtocol == nameof(UsenetDownloadProtocol))), Times.Once());
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.Is<RemoteAlbum>(r => r.Release.DownloadProtocol == nameof(TorrentDownloadProtocol))), Times.Once());
        }

        [Test]
        public async Task should_add_to_rejected_if_release_unavailable_on_indexer()
        {
            var albums = new List<Album> { GetAlbum(1) };
            var remoteAlbum = GetRemoteAlbum(albums, new QualityModel(Quality.MP3_320));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum));

            Mocker.GetMock<IDownloadService>()
                  .Setup(s => s.DownloadReport(It.IsAny<RemoteAlbum>()))
                  .Throws(new ReleaseUnavailableException(remoteAlbum.Release, "That 404 Error is not just a Quirk"));

            var result = await Subject.ProcessDecisions(decisions);

            result.Grabbed.Should().BeEmpty();
            result.Rejected.Should().NotBeEmpty();

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}

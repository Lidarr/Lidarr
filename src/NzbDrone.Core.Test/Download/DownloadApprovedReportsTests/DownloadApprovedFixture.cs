﻿using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;
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

        private Episode GetEpisode(int id)
        {
            return Builder<Episode>.CreateNew()
                            .With(e => e.Id = id)
                            .With(e => e.EpisodeNumber = id)
                            .Build();
        }

        private RemoteEpisode GetRemoteEpisode(List<Episode> episodes, QualityModel quality)
        {
            var remoteEpisode = new RemoteEpisode();
            remoteEpisode.ParsedEpisodeInfo = new ParsedEpisodeInfo();
            remoteEpisode.ParsedEpisodeInfo.Quality = quality;

            remoteEpisode.Episodes = new List<Episode>();
            remoteEpisode.Episodes.AddRange(episodes);

            remoteEpisode.Release = new ReleaseInfo();
            remoteEpisode.Release.PublishDate = DateTime.UtcNow;

            remoteEpisode.Series = Builder<Series>.CreateNew()
                .With(e => e.Profile = new Profile { Items = Qualities.QualityFixture.GetDefaultQualities() })
                .Build();

            return remoteEpisode;
        }

        [Test]
        public void should_download_report_if_epsiode_was_not_already_downloaded()
        {
            var episodes = new List<Episode> { GetEpisode(1) };
            var remoteEpisode = GetRemoteEpisode(episodes, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode));

            Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteEpisode>()), Times.Once());
        }

        [Test]
        public void should_only_download_episode_once()
        {
            var episodes = new List<Episode> { GetEpisode(1) };
            var remoteEpisode = GetRemoteEpisode(episodes, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode));
            decisions.Add(new DownloadDecision(remoteEpisode));

            Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteEpisode>()), Times.Once());
        }

        [Test]
        public void should_not_download_if_any_episode_was_already_downloaded()
        {
            var remoteEpisode1 = GetRemoteEpisode(
                                                    new List<Episode> { GetEpisode(1) },
                                                    new QualityModel(Quality.MP3_192)
                                                 );

            var remoteEpisode2 = GetRemoteEpisode(
                                                    new List<Episode> { GetEpisode(1), GetEpisode(2) },
                                                    new QualityModel(Quality.MP3_192)
                                                 );

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteEpisode>()), Times.Once());
        }

        [Test]
        public void should_return_downloaded_reports()
        {
            var episodes = new List<Episode> { GetEpisode(1) };
            var remoteEpisode = GetRemoteEpisode(episodes, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode));

            Subject.ProcessDecisions(decisions).Grabbed.Should().HaveCount(1);
        }

        [Test]
        public void should_return_all_downloaded_reports()
        {
            var remoteEpisode1 = GetRemoteEpisode(
                                                    new List<Episode> { GetEpisode(1) },
                                                    new QualityModel(Quality.MP3_192)
                                                 );

            var remoteEpisode2 = GetRemoteEpisode(
                                                    new List<Episode> { GetEpisode(2) },
                                                    new QualityModel(Quality.MP3_192)
                                                 );

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));

            Subject.ProcessDecisions(decisions).Grabbed.Should().HaveCount(2);
        }

        [Test]
        public void should_only_return_downloaded_reports()
        {
            var remoteEpisode1 = GetRemoteEpisode(
                                                    new List<Episode> { GetEpisode(1) },
                                                    new QualityModel(Quality.MP3_192)
                                                 );

            var remoteEpisode2 = GetRemoteEpisode(
                                                    new List<Episode> { GetEpisode(2) },
                                                    new QualityModel(Quality.MP3_192)
                                                 );

            var remoteEpisode3 = GetRemoteEpisode(
                                                    new List<Episode> { GetEpisode(2) },
                                                    new QualityModel(Quality.MP3_192)
                                                 );

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode1));
            decisions.Add(new DownloadDecision(remoteEpisode2));
            decisions.Add(new DownloadDecision(remoteEpisode3));

            Subject.ProcessDecisions(decisions).Grabbed.Should().HaveCount(2);
        }

        [Test]
        public void should_not_add_to_downloaded_list_when_download_fails()
        {
            var episodes = new List<Episode> { GetEpisode(1) };
            var remoteEpisode = GetRemoteEpisode(episodes, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode));

            Mocker.GetMock<IDownloadService>().Setup(s => s.DownloadReport(It.IsAny<RemoteEpisode>())).Throws(new Exception());
            Subject.ProcessDecisions(decisions).Grabbed.Should().BeEmpty();
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_return_an_empty_list_when_none_are_appproved()
        {
            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(null, new Rejection("Failure!")));
            decisions.Add(new DownloadDecision(null, new Rejection("Failure!")));

            Subject.GetQualifiedReports(decisions).Should().BeEmpty();
        }

        [Test]
        public void should_not_grab_if_pending()
        {
            var episodes = new List<Episode> { GetEpisode(1) };
            var remoteEpisode = GetRemoteEpisode(episodes, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode, new Rejection("Failure!", RejectionType.Temporary)));
            decisions.Add(new DownloadDecision(remoteEpisode));

            Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IDownloadService>().Verify(v => v.DownloadReport(It.IsAny<RemoteEpisode>()), Times.Never());
        }

        [Test]
        public void should_not_add_to_pending_if_episode_was_grabbed()
        {
            var episodes = new List<Episode> { GetEpisode(1) };
            var remoteEpisode = GetRemoteEpisode(episodes, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode));
            decisions.Add(new DownloadDecision(remoteEpisode, new Rejection("Failure!", RejectionType.Temporary)));

            Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IPendingReleaseService>().Verify(v => v.Add(It.IsAny<DownloadDecision>()), Times.Never());
        }

        [Test]
        public void should_add_to_pending_even_if_already_added_to_pending()
        {
            var episodes = new List<Episode> { GetEpisode(1) };
            var remoteEpisode = GetRemoteEpisode(episodes, new QualityModel(Quality.MP3_192));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteEpisode, new Rejection("Failure!", RejectionType.Temporary)));
            decisions.Add(new DownloadDecision(remoteEpisode, new Rejection("Failure!", RejectionType.Temporary)));

            Subject.ProcessDecisions(decisions);
            Mocker.GetMock<IPendingReleaseService>().Verify(v => v.Add(It.IsAny<DownloadDecision>()), Times.Exactly(2));
        }
    }
}

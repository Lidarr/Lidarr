using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class PrioritizeDownloadDecisionFixture : CoreTest<DownloadDecisionPriorizationService>
    {
        [SetUp]
        public void Setup()
        {
            GivenPreferredDownloadProtocol(nameof(UsenetDownloadProtocol));
        }

        private Album GivenAlbum(int id)
        {
            return Builder<Album>.CreateNew()
                            .With(e => e.Id = id)
                            .Build();
        }

        private RemoteAlbum GivenRemoteAlbum(List<Album> albums, QualityModel quality, int age = 0, long size = 0, string downloadProtocol = "UsenetDownloadProtocol", int indexerPriority = 25)
        {
            var remoteAlbum = new RemoteAlbum();
            remoteAlbum.ParsedAlbumInfo = new ParsedAlbumInfo();
            remoteAlbum.ParsedAlbumInfo.Quality = quality;

            remoteAlbum.Albums = new List<Album>();
            remoteAlbum.Albums.AddRange(albums);

            remoteAlbum.Release = new ReleaseInfo();
            remoteAlbum.Release.PublishDate = DateTime.Now.AddDays(-age);
            remoteAlbum.Release.Size = size;
            remoteAlbum.Release.DownloadProtocol = downloadProtocol;
            remoteAlbum.Release.IndexerPriority = indexerPriority;

            remoteAlbum.Artist = Builder<Artist>.CreateNew()
                                                .With(e => e.QualityProfile = new QualityProfile
                                                {
                                                    Items = Qualities.QualityFixture.GetDefaultQualities()
                                                }).Build();

            remoteAlbum.DownloadAllowed = true;

            return remoteAlbum;
        }

        private void GivenPreferredDownloadProtocol(string downloadProtocol)
        {
            var profile = new DelayProfile();
            profile.Items = profile.Items.OrderByDescending(x => x.Protocol == downloadProtocol).ToList();

            Mocker.GetMock<IDelayProfileService>()
                  .Setup(s => s.BestForTags(It.IsAny<HashSet<int>>()))
                  .Returns(profile);
        }

        [Test]
        public void should_put_reals_before_non_reals()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256, new Revision(version: 1, real: 0)));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256, new Revision(version: 1, real: 1)));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Quality.Revision.Real.Should().Be(1);
        }

        [Test]
        public void should_put_propers_before_non_propers()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256, new Revision(version: 1)));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256, new Revision(version: 2)));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_put_higher_quality_before_lower()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_192));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Quality.Quality.Should().Be(Quality.MP3_256);
        }

        [Test]
        public void should_order_by_age_then_largest_rounded_to_200mb()
        {
            var remoteAlbumSd = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_192), size: 100.Megabytes(), age: 1);
            var remoteAlbumHdSmallOld = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256), size: 1200.Megabytes(), age: 1000);
            var remoteAlbumSmallYoung = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256), size: 1250.Megabytes(), age: 10);
            var remoteAlbumHdLargeYoung = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256), size: 3000.Megabytes(), age: 1);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbumSd));
            decisions.Add(new DownloadDecision(remoteAlbumHdSmallOld));
            decisions.Add(new DownloadDecision(remoteAlbumSmallYoung));
            decisions.Add(new DownloadDecision(remoteAlbumHdLargeYoung));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.Should().Be(remoteAlbumHdLargeYoung);
        }

        [Test]
        public void should_order_by_youngest()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256), age: 10);
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256), age: 5);

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.Should().Be(remoteAlbum2);
        }

        [Test]
        public void should_not_throw_if_no_albums_are_found()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256), size: 500.Megabytes());
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256), size: 500.Megabytes());

            remoteAlbum1.Albums = new List<Album>();

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            Subject.PrioritizeDecisions(decisions);
        }

        [Test]
        public void should_put_usenet_above_torrent_when_usenet_is_preferred()
        {
            GivenPreferredDownloadProtocol(nameof(UsenetDownloadProtocol));

            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256), downloadProtocol: nameof(TorrentDownloadProtocol));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256), downloadProtocol: nameof(UsenetDownloadProtocol));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.Release.DownloadProtocol.Should().Be(nameof(UsenetDownloadProtocol));
        }

        [Test]
        public void should_put_torrent_above_usenet_when_torrent_is_preferred()
        {
            GivenPreferredDownloadProtocol(nameof(TorrentDownloadProtocol));

            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256), downloadProtocol: nameof(TorrentDownloadProtocol));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256), downloadProtocol: nameof(UsenetDownloadProtocol));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.Release.DownloadProtocol.Should().Be(nameof(TorrentDownloadProtocol));
        }

        [Test]
        public void should_prefer_discography_pack_above_single_album()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1), GivenAlbum(2) }, new QualityModel(Quality.FLAC));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC));

            remoteAlbum1.ParsedAlbumInfo.Discography = true;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Discography.Should().BeTrue();
        }

        [Test]
        public void should_prefer_quality_over_discography_pack()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1), GivenAlbum(2) }, new QualityModel(Quality.MP3_320));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC));

            remoteAlbum1.ParsedAlbumInfo.Discography = true;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Discography.Should().BeFalse();
        }

        [Test]
        public void should_prefer_single_album_over_multi_album()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1), GivenAlbum(2) }, new QualityModel(Quality.MP3_256));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.Albums.Count.Should().Be(remoteAlbum2.Albums.Count);
        }

        [Test]
        public void should_prefer_releases_with_more_seeders()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = nameof(TorrentDownloadProtocol);
            torrentInfo1.Seeders = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 100;

            remoteAlbum1.Release = torrentInfo1;
            remoteAlbum2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteAlbum.Release).Seeders.Should().Be(torrentInfo2.Seeders);
        }

        [Test]
        public void should_prefer_releases_with_more_peers_given_equal_number_of_seeds()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = nameof(TorrentDownloadProtocol);
            torrentInfo1.Seeders = 10;
            torrentInfo1.Peers = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Peers = 100;

            remoteAlbum1.Release = torrentInfo1;
            remoteAlbum2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteAlbum.Release).Peers.Should().Be(torrentInfo2.Peers);
        }

        [Test]
        public void should_prefer_releases_with_more_peers_no_seeds()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.Size = 0;
            torrentInfo1.DownloadProtocol = nameof(TorrentDownloadProtocol);
            torrentInfo1.Seeders = 0;
            torrentInfo1.Peers = 10;

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 0;
            torrentInfo2.Peers = 100;

            remoteAlbum1.Release = torrentInfo1;
            remoteAlbum2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteAlbum.Release).Peers.Should().Be(torrentInfo2.Peers);
        }

        [Test]
        public void should_prefer_first_release_if_peers_and_size_are_too_similar()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.DownloadProtocol = nameof(TorrentDownloadProtocol);
            torrentInfo1.Seeders = 1000;
            torrentInfo1.Peers = 10;
            torrentInfo1.Size = 200.Megabytes();

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 1100;
            torrentInfo2.Peers = 10;
            torrentInfo1.Size = 250.Megabytes();

            remoteAlbum1.Release = torrentInfo1;
            remoteAlbum2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteAlbum.Release).Should().Be(torrentInfo1);
        }

        [Test]
        public void should_prefer_first_release_if_age_and_size_are_too_similar()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));

            remoteAlbum1.Release.PublishDate = DateTime.UtcNow.AddDays(-100);
            remoteAlbum1.Release.Size = 200.Megabytes();

            remoteAlbum2.Release.PublishDate = DateTime.UtcNow.AddDays(-150);
            remoteAlbum2.Release.Size = 250.Megabytes();

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.Release.Should().Be(remoteAlbum1.Release);
        }

        [Test]
        public void should_prefer_quality_over_the_number_of_peers()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_320));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_192));

            var torrentInfo1 = new TorrentInfo();
            torrentInfo1.PublishDate = DateTime.Now;
            torrentInfo1.DownloadProtocol = nameof(TorrentDownloadProtocol);
            torrentInfo1.Seeders = 100;
            torrentInfo1.Peers = 10;
            torrentInfo1.Size = 200.Megabytes();

            var torrentInfo2 = torrentInfo1.JsonClone();
            torrentInfo2.Seeders = 1100;
            torrentInfo2.Peers = 10;
            torrentInfo1.Size = 250.Megabytes();

            remoteAlbum1.Release = torrentInfo1;
            remoteAlbum2.Release = torrentInfo2;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            ((TorrentInfo)qualifiedReports.First().RemoteAlbum.Release).Should().Be(torrentInfo1);
        }

        [Test]
        public void should_put_higher_quality_before_lower_always()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_256));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_320));

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Quality.Quality.Should().Be(Quality.MP3_320);
        }

        [Test]
        public void should_prefer_higher_score_over_lower_score()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC));

            remoteAlbum1.PreferredWordScore = 10;
            remoteAlbum2.PreferredWordScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.PreferredWordScore.Should().Be(10);
        }

        [Test]
        public void should_prefer_proper_over_score_when_download_propers_is_prefer_and_upgrade()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.PreferAndUpgrade);

            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(1)));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(2)));

            remoteAlbum1.PreferredWordScore = 10;
            remoteAlbum2.PreferredWordScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_prefer_proper_over_score_when_download_propers_is_do_not_upgrade()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotUpgrade);

            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(1)));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(2)));

            remoteAlbum1.PreferredWordScore = 10;
            remoteAlbum2.PreferredWordScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Quality.Revision.Version.Should().Be(2);
        }

        [Test]
        public void should_prefer_score_over_proper_when_download_propers_is_do_not_prefer()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(1)));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(2)));

            remoteAlbum1.PreferredWordScore = 10;
            remoteAlbum2.PreferredWordScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Quality.Quality.Should().Be(Quality.FLAC);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Quality.Revision.Version.Should().Be(1);
            qualifiedReports.First().RemoteAlbum.PreferredWordScore.Should().Be(10);
        }

        [Test]
        public void should_prefer_score_over_real_when_download_propers_is_do_not_prefer()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.DownloadPropersAndRepacks)
                  .Returns(ProperDownloadTypes.DoNotPrefer);

            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(1, 0)));
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(1, 1)));

            remoteAlbum1.PreferredWordScore = 10;
            remoteAlbum2.PreferredWordScore = 0;

            var decisions = new List<DownloadDecision>();
            decisions.Add(new DownloadDecision(remoteAlbum1));
            decisions.Add(new DownloadDecision(remoteAlbum2));

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Quality.Quality.Should().Be(Quality.FLAC);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Quality.Revision.Version.Should().Be(1);
            qualifiedReports.First().RemoteAlbum.ParsedAlbumInfo.Quality.Revision.Real.Should().Be(0);
            qualifiedReports.First().RemoteAlbum.PreferredWordScore.Should().Be(10);
        }

        [Test]
        public void sort_download_decisions_based_on_indexer_priority()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(1)), indexerPriority: 25);
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(1)), indexerPriority: 50);
            var remoteAlbum3 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(1)), indexerPriority: 1);

            var decisions = new List<DownloadDecision>();
            decisions.AddRange(new[] { new DownloadDecision(remoteAlbum1), new DownloadDecision(remoteAlbum2), new DownloadDecision(remoteAlbum3) });

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.Should().Be(remoteAlbum3);
            qualifiedReports.Skip(1).First().RemoteAlbum.Should().Be(remoteAlbum1);
            qualifiedReports.Last().RemoteAlbum.Should().Be(remoteAlbum2);
        }

        [Test]
        public void ensure_download_decisions_indexer_priority_is_not_perfered_over_quality()
        {
            var remoteAlbum1 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_320, new Revision(1)), indexerPriority: 25);
            var remoteAlbum2 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(1)), indexerPriority: 50);
            var remoteAlbum3 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.MP3_128, new Revision(1)), indexerPriority: 1);
            var remoteAlbum4 = GivenRemoteAlbum(new List<Album> { GivenAlbum(1) }, new QualityModel(Quality.FLAC, new Revision(1)), indexerPriority: 25);

            var decisions = new List<DownloadDecision>();
            decisions.AddRange(new[] { new DownloadDecision(remoteAlbum1), new DownloadDecision(remoteAlbum2), new DownloadDecision(remoteAlbum3), new DownloadDecision(remoteAlbum4) });

            var qualifiedReports = Subject.PrioritizeDecisions(decisions);
            qualifiedReports.First().RemoteAlbum.Should().Be(remoteAlbum4);
            qualifiedReports.Skip(1).First().RemoteAlbum.Should().Be(remoteAlbum2);
            qualifiedReports.Skip(2).First().RemoteAlbum.Should().Be(remoteAlbum1);
            qualifiedReports.Last().RemoteAlbum.Should().Be(remoteAlbum3);
        }
    }
}

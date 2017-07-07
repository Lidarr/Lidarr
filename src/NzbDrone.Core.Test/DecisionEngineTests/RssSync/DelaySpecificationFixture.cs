﻿using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Marr.Data;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.Download.Pending;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]
    public class DelaySpecificationFixture : CoreTest<DelaySpecification>
    {
        private Profile _profile;
        private DelayProfile _delayProfile;
        private RemoteEpisode _remoteEpisode;

        [SetUp]
        public void Setup()
        {
            _profile = Builder<Profile>.CreateNew()
                                       .Build();

            _delayProfile = Builder<DelayProfile>.CreateNew()
                                                 .With(d => d.PreferredProtocol = DownloadProtocol.Usenet)
                                                 .Build();

            var series = Builder<Series>.CreateNew()
                                        .With(s => s.Profile = _profile)
                                        .Build();

            _remoteEpisode = Builder<RemoteEpisode>.CreateNew()
                                                   .With(r => r.Series = series)
                                                   .Build();

            _profile.Items = new List<ProfileQualityItem>();
            _profile.Items.Add(new ProfileQualityItem { Allowed = true, Quality = Quality.MP3_256 });
            _profile.Items.Add(new ProfileQualityItem { Allowed = true, Quality = Quality.MP3_320 });
            _profile.Items.Add(new ProfileQualityItem { Allowed = true, Quality = Quality.MP3_320 });

            _profile.Cutoff = Quality.MP3_320;

            _remoteEpisode.ParsedEpisodeInfo = new ParsedEpisodeInfo();
            _remoteEpisode.Release = new ReleaseInfo();
            _remoteEpisode.Release.DownloadProtocol = DownloadProtocol.Usenet;

            _remoteEpisode.Episodes = Builder<Episode>.CreateListOfSize(1).Build().ToList();
            _remoteEpisode.Episodes.First().EpisodeFileId = 0;

            Mocker.GetMock<IDelayProfileService>()
                  .Setup(s => s.BestForTags(It.IsAny<HashSet<int>>()))
                  .Returns(_delayProfile);

            Mocker.GetMock<IPendingReleaseService>()
                  .Setup(s => s.GetPendingRemoteEpisodes(It.IsAny<int>()))
                  .Returns(new List<RemoteEpisode>());
        }

        private void GivenExistingFile(QualityModel quality)
        {
            _remoteEpisode.Episodes.First().EpisodeFileId = 1;

            _remoteEpisode.Episodes.First().EpisodeFile = new LazyLoaded<EpisodeFile>(new EpisodeFile
                                                                                 {
                                                                                     Quality = quality
                                                                                 });
        }

        private void GivenUpgradeForExistingFile()
        {
            Mocker.GetMock<IQualityUpgradableSpecification>()
                  .Setup(s => s.IsUpgradable(It.IsAny<Profile>(), It.IsAny<QualityModel>(), It.IsAny<QualityModel>()))
                  .Returns(true);
        }

        [Test]
        public void should_be_true_when_user_invoked_search()
        {
            Subject.IsSatisfiedBy(new RemoteEpisode(), new SingleEpisodeSearchCriteria { UserInvokedSearch = true }).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_system_invoked_search_and_release_is_younger_than_delay()
        {
            _remoteEpisode.ParsedEpisodeInfo.Quality = new QualityModel(Quality.MP3_192);
            _remoteEpisode.Release.PublishDate = DateTime.UtcNow;

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteEpisode, new SingleEpisodeSearchCriteria()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_when_profile_does_not_have_a_delay()
        {
            _delayProfile.UsenetDelay = 0;

            Subject.IsSatisfiedBy(_remoteEpisode, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_true_when_quality_is_last_allowed_in_profile()
        {
            _remoteEpisode.ParsedEpisodeInfo.Quality = new QualityModel(Quality.MP3_320);

            Subject.IsSatisfiedBy(_remoteEpisode, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_true_when_release_is_older_than_delay()
        {
            _remoteEpisode.ParsedEpisodeInfo.Quality = new QualityModel(Quality.MP3_256);
            _remoteEpisode.Release.PublishDate = DateTime.UtcNow.AddHours(-10);

            _delayProfile.UsenetDelay = 60;

            Subject.IsSatisfiedBy(_remoteEpisode, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_release_is_younger_than_delay()
        {
            _remoteEpisode.ParsedEpisodeInfo.Quality = new QualityModel(Quality.MP3_192);
            _remoteEpisode.Release.PublishDate = DateTime.UtcNow;

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteEpisode, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_when_release_is_a_proper_for_existing_episode()
        {
            _remoteEpisode.ParsedEpisodeInfo.Quality = new QualityModel(Quality.MP3_256, new Revision(version: 2));
            _remoteEpisode.Release.PublishDate = DateTime.UtcNow;

            GivenExistingFile(new QualityModel(Quality.MP3_256));
            GivenUpgradeForExistingFile();

            Mocker.GetMock<IQualityUpgradableSpecification>()
                  .Setup(s => s.IsRevisionUpgrade(It.IsAny<QualityModel>(), It.IsAny<QualityModel>()))
                  .Returns(true);

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteEpisode, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_true_when_release_is_a_real_for_existing_episode()
        {
            _remoteEpisode.ParsedEpisodeInfo.Quality = new QualityModel(Quality.MP3_256, new Revision(real: 1));
            _remoteEpisode.Release.PublishDate = DateTime.UtcNow;

            GivenExistingFile(new QualityModel(Quality.MP3_256));
            GivenUpgradeForExistingFile();

            Mocker.GetMock<IQualityUpgradableSpecification>()
                  .Setup(s => s.IsRevisionUpgrade(It.IsAny<QualityModel>(), It.IsAny<QualityModel>()))
                  .Returns(true);

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteEpisode, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_when_release_is_proper_for_existing_episode_of_different_quality()
        {
            _remoteEpisode.ParsedEpisodeInfo.Quality = new QualityModel(Quality.MP3_256, new Revision(version: 2));
            _remoteEpisode.Release.PublishDate = DateTime.UtcNow;

            GivenExistingFile(new QualityModel(Quality.MP3_192));

            _delayProfile.UsenetDelay = 720;

            Subject.IsSatisfiedBy(_remoteEpisode, null).Accepted.Should().BeFalse();
        }
    }
}

using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using Moq;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine.Specifications.RssSync;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Music;
using NzbDrone.Core.DecisionEngine;

using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests.RssSync
{
    [TestFixture]

    public class ProperSpecificationFixture : CoreTest<ProperSpecification>
    {
        private RemoteAlbum _parseResultMulti;
        private RemoteAlbum _parseResultSingle;
        private TrackFile _firstFile;
        private TrackFile _secondFile;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<QualityUpgradableSpecification>();

            _firstFile = new TrackFile { Quality = new QualityModel(Quality.FLAC, new Revision(version: 1)), DateAdded = DateTime.Now };
            _secondFile = new TrackFile { Quality = new QualityModel(Quality.FLAC, new Revision(version: 1)), DateAdded = DateTime.Now };

            var singleEpisodeList = new List<Album> { new Album {}, new Album {} };
            var doubleEpisodeList = new List<Album> { new Album {}, new Album {}, new Album {} };


            var fakeArtist = Builder<Artist>.CreateNew()
                         .With(c => c.Profile = new Profile { Cutoff = Quality.FLAC })
                         .Build();

            Mocker.GetMock<IMediaFileService>()
                .Setup(c => c.GetFilesByAlbum(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(new List<TrackFile> { _firstFile, _secondFile });

            _parseResultMulti = new RemoteAlbum
            {
                Artist = fakeArtist,
                ParsedAlbumInfo = new ParsedAlbumInfo { Quality = new QualityModel(Quality.MP3_256, new Revision(version: 2)) },
                Albums = doubleEpisodeList
            };

            _parseResultSingle = new RemoteAlbum
            {
                Artist = fakeArtist,
                ParsedAlbumInfo = new ParsedAlbumInfo { Quality = new QualityModel(Quality.MP3_256, new Revision(version: 2)) },
                Albums = singleEpisodeList
            };
        }

        private void WithFirstFileUpgradable()
        {
            _firstFile.Quality = new QualityModel(Quality.MP3_192);
        }

        private void GivenAutoDownloadPropers()
        {
            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.AutoDownloadPropers)
                  .Returns(true);
        }

        [Test]
        public void should_return_false_when_trackFile_was_added_more_than_7_days_ago()
        {
            _firstFile.Quality.Quality = Quality.MP3_256;

            _firstFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_first_trackFile_was_added_more_than_7_days_ago()
        {
            _firstFile.Quality.Quality = Quality.MP3_256;
            _secondFile.Quality.Quality = Quality.MP3_256;

            _firstFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_false_when_second_trackFile_was_added_more_than_7_days_ago()
        {
            _firstFile.Quality.Quality = Quality.MP3_256;
            _secondFile.Quality.Quality = Quality.MP3_256;

            _secondFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_trackFile_was_added_more_than_7_days_ago_but_proper_is_for_better_quality()
        {
            WithFirstFileUpgradable();

            _firstFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_when_trackFile_was_added_more_than_7_days_ago_but_is_for_search()
        {
            WithFirstFileUpgradable();

            _firstFile.DateAdded = DateTime.Today.AddDays(-30);
            Subject.IsSatisfiedBy(_parseResultSingle, new AlbumSearchCriteria()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_false_when_proper_but_auto_download_propers_is_false()
        {
            _firstFile.Quality.Quality = Quality.MP3_256;

            _firstFile.DateAdded = DateTime.Today;
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_when_trackFile_was_added_today()
        {
            GivenAutoDownloadPropers();

            _firstFile.Quality.Quality = Quality.MP3_256;

            _firstFile.DateAdded = DateTime.Today;
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }
    }
}

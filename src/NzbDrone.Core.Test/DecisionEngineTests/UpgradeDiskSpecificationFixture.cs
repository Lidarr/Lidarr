using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Music;
using NzbDrone.Core.DecisionEngine;

using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]

    public class UpgradeDiskSpecificationFixture : CoreTest<UpgradeDiskSpecification>
    {
        private UpgradeDiskSpecification _upgradeDisk;

        private RemoteAlbum _parseResultMulti;
        private RemoteAlbum _parseResultSingle;
        private TrackFile _firstFile;
        private TrackFile _secondFile;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<QualityUpgradableSpecification>();
            _upgradeDisk = Mocker.Resolve<UpgradeDiskSpecification>();

            _firstFile = new TrackFile { Quality = new QualityModel(Quality.MP3_512, new Revision(version: 2)), DateAdded = DateTime.Now };
            _secondFile = new TrackFile { Quality = new QualityModel(Quality.MP3_512, new Revision(version: 2)), DateAdded = DateTime.Now };

            //var singleEpisodeList = new List<Album> { new Album { EpisodeFile = _firstFile, EpisodeFileId = 1 }, new Album { EpisodeFile = null } };
            //var doubleEpisodeList = new List<Album> { new Album { EpisodeFile = _firstFile, EpisodeFileId = 1 }, new Album { EpisodeFile = _secondFile, EpisodeFileId = 1 }, new Episode { EpisodeFile = null } };

            var singleEpisodeList = new List<Album> { new Album { }, new Album {} };
            var doubleEpisodeList = new List<Album> { new Album {}, new Album {}, new Album {} };


            var fakeSeries = Builder<Artist>.CreateNew()
                         .With(c => c.Profile = new Profile { Cutoff = Quality.MP3_512, Items = Qualities.QualityFixture.GetDefaultQualities() })
                         .Build();

            _parseResultMulti = new RemoteAlbum
            {
                Artist = fakeSeries,
                ParsedAlbumInfo = new ParsedAlbumInfo { Quality = new QualityModel(Quality.MP3_192, new Revision(version: 2)) },
                Albums = doubleEpisodeList
            };

            _parseResultSingle = new RemoteAlbum
            {
                Artist = fakeSeries,
                ParsedAlbumInfo = new ParsedAlbumInfo { Quality = new QualityModel(Quality.MP3_192, new Revision(version: 2)) },
                Albums = singleEpisodeList
            };
        }

        private void WithFirstFileUpgradable()
        {
            _firstFile.Quality = new QualityModel(Quality.MP3_192);
        }

        private void WithSecondFileUpgradable()
        {
            _secondFile.Quality = new QualityModel(Quality.MP3_192);
        }

        [Test]
        public void should_return_true_if_album_has_no_existing_file()
        {
            //TODO Add for Albums
            //_parseResultSingle.Albums.ForEach(c => c.EpisodeFileId = 0);
            _upgradeDisk.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_single_album_doesnt_exist_on_disk()
        {
            _parseResultSingle.Albums = new List<Album>();

            _upgradeDisk.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_upgradable_if_only_album_is_upgradable()
        {
            WithFirstFileUpgradable();
            _upgradeDisk.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_upgradable_if_both_albums_are_upgradable()
        {
            WithFirstFileUpgradable();
            WithSecondFileUpgradable();
            _upgradeDisk.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_not_upgradable_if_both_albums_are_not_upgradable()
        {
            _upgradeDisk.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_not_upgradable_if_only_first_albums_is_upgradable()
        {
            WithFirstFileUpgradable();
            _upgradeDisk.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_not_upgradable_if_only_second_albums_is_upgradable()
        {
            WithSecondFileUpgradable();
            _upgradeDisk.IsSatisfiedBy(_parseResultMulti, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_not_be_upgradable_if_qualities_are_the_same()
        {
            _firstFile.Quality = new QualityModel(Quality.MP3_512);
            _parseResultSingle.ParsedAlbumInfo.Quality = new QualityModel(Quality.MP3_512);
            _upgradeDisk.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }
    }
}
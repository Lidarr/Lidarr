using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.EpisodeImport;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.EpisodeImport
{
    [TestFixture]
    public class GetSceneNameFixture : CoreTest
    {
        private LocalTrack _localEpisode;
        private string _seasonName = "artist.title-album.title.FLAC-ingot";
        private string _episodeName = "artist.title-album.title.FLAC-ingot";

        [SetUp]
        public void Setup()
        {
            var series = Builder<Artist>.CreateNew()
                                        .With(e => e.QualityProfile = new QualityProfile { Items = Qualities.QualityFixture.GetDefaultQualities() })
                                        .With(s => s.Path = @"C:\Test\Music\Artist Title".AsOsAgnostic())
                                        .Build();

            var episode = Builder<Track>.CreateNew()
                                          .Build();

            _localEpisode = new LocalTrack
            {
                Artist = series,
                Tracks = new List<Track> { episode },
                Path = Path.Combine(series.Path, "01 Some Body Loves.mkv"),
                Quality = new QualityModel(Quality.FLAC),
                ReleaseGroup = "DRONE"
            };
        }

        [Test]
        public void should_use_download_client_item_title_as_scene_name()
        {
            _localEpisode.DownloadClientAlbumInfo = new ParsedAlbumInfo
            {
                ReleaseTitle = _episodeName
            };

            SceneNameCalculator.GetSceneName(_localEpisode).Should()
                               .Be(_episodeName);
        }

        [Test]
        public void should_not_use_download_client_item_title_as_scene_name_if_full_season()
        {
            _localEpisode.DownloadClientAlbumInfo = new ParsedAlbumInfo
            {
                ReleaseTitle = _seasonName,
                Discography = true
            };

            _localEpisode.Path = Path.Combine(@"C:\Test\Unsorted TV", _seasonName, _episodeName)
                                     .AsOsAgnostic();

            SceneNameCalculator.GetSceneName(_localEpisode).Should()
                               .BeNull();
        }

        [Test]
        public void should_not_use_file_name_as_scenename_if_it_doesnt_look_like_scenename()
        {
            _localEpisode.Path = Path.Combine(@"C:\Test\Unsorted TV", _episodeName, "aaaaa.mkv")
                                     .AsOsAgnostic();

            SceneNameCalculator.GetSceneName(_localEpisode).Should()
                               .BeNull();
        }

        [Test]
        public void should_not_use_folder_name_as_scenename_if_it_doesnt_look_like_scenename()
        {
            _localEpisode.Path = Path.Combine(@"C:\Test\Unsorted TV", _episodeName, "aaaaa.mkv")
                                     .AsOsAgnostic();

            _localEpisode.FolderAlbumInfo = new ParsedAlbumInfo
            {
                ReleaseTitle = "aaaaa"
            };

            SceneNameCalculator.GetSceneName(_localEpisode).Should()
                               .BeNull();
        }

        [Test]
        public void should_not_use_folder_name_as_scenename_if_it_is_for_a_full_season()
        {
            _localEpisode.Path = Path.Combine(@"C:\Test\Unsorted TV", _episodeName, "aaaaa.mkv")
                                     .AsOsAgnostic();

            _localEpisode.FolderAlbumInfo = new ParsedAlbumInfo
            {
                ReleaseTitle = _seasonName,
                Discography = true
            };

            SceneNameCalculator.GetSceneName(_localEpisode).Should()
                               .BeNull();
        }

        [Test]
        public void should_not_use_folder_name_as_scenename_if_there_are_other_video_files()
        {
            _localEpisode.Path = Path.Combine(@"C:\Test\Unsorted TV", _episodeName, "aaaaa.mkv")
                                     .AsOsAgnostic();

            _localEpisode.FolderAlbumInfo = new ParsedAlbumInfo
            {
                ReleaseTitle = _seasonName,
                Discography = false
            };

            SceneNameCalculator.GetSceneName(_localEpisode).Should()
                               .BeNull();
        }

        [TestCase(".flac")]
        [TestCase(".par2")]
        [TestCase(".nzb")]
        public void should_remove_extension_from_nzb_title_for_scene_name(string extension)
        {
            _localEpisode.DownloadClientAlbumInfo = new ParsedAlbumInfo
            {
                ReleaseTitle = _episodeName + extension
            };

            SceneNameCalculator.GetSceneName(_localEpisode).Should()
                               .Be(_episodeName);
        }
    }
}

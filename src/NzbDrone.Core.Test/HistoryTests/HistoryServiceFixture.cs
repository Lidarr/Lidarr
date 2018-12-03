using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.History;
using NzbDrone.Core.Qualities;
using System.Collections.Generic;
using NzbDrone.Core.Test.Qualities;
using NzbDrone.Core.Download;
using NzbDrone.Core.Music;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Profiles.Languages;

namespace NzbDrone.Core.Test.HistoryTests
{
    public class HistoryServiceFixture : CoreTest<HistoryService>
    {
        private Profile _profile;
        private Profile _profileCustom;
        private LanguageProfile _languageProfile;

        [SetUp]
        public void Setup()
        {
            _profile = new Profile
            {
                Cutoff = Quality.MP3_320.Id,
                Items = QualityFixture.GetDefaultQualities(),
            };

            _profileCustom = new Profile

            {
                Cutoff = Quality.MP3_320.Id,
                Items = QualityFixture.GetDefaultQualities(Quality.MP3_256),

            };


            _languageProfile = new LanguageProfile

            {
                Cutoff = Language.Spanish,
                Languages = Languages.LanguageFixture.GetDefaultLanguages()
            };


        }

        [Test]
        public void should_use_file_name_for_source_title_if_scene_name_is_null()
        {
            var artist = Builder<Artist>.CreateNew().Build();
            var tracks = Builder<Track>.CreateListOfSize(1).Build().ToList();
            var trackFile = Builder<TrackFile>.CreateNew()
                .With(f => f.SceneName = null)
                .With(f => f.Artist = artist)
                .With(f => f.Album = new Album())
                .Build();

            var localTrack = new LocalTrack
            {
                Artist = artist,
                Tracks = tracks,
                Path = @"C:\Test\Unsorted\Artist.01.Hymn.mp3"
            };

            var downloadClientItem = new DownloadClientItem
                                     {
                                        DownloadClient = "sab",
                                        DownloadId = "abcd"
                                     };
            
            Subject.Handle(new TrackImportedEvent(localTrack, trackFile, new List<TrackFile>(), true, downloadClientItem));

            Mocker.GetMock<IHistoryRepository>()
                .Verify(v => v.Insert(It.Is<History.History>(h => h.SourceTitle == Path.GetFileNameWithoutExtension(localTrack.Path))));
        }
    }
}

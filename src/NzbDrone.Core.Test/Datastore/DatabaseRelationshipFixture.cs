using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class DatabaseRelationshipFixture : DbTest
    {
        [Test]
        public void one_to_one()
        {
            var trackFile = Builder<TrackFile>.CreateNew()
                .With(c => c.Id = 0)
                .With(c => c.Quality = new QualityModel())
                .With(c => c.Language = Language.English)
                .BuildNew();

            Db.Insert(trackFile);

            var track = Builder<Track>.CreateNew()
                .With(c => c.Id = 0)
                .With(c => c.TrackFileId = trackFile.Id)
                .BuildNew();

            Db.Insert(track);

            var loadedTrackFile = Db.Single<Track>().TrackFile.Value;

            loadedTrackFile.Should().NotBeNull();
            loadedTrackFile.ShouldBeEquivalentTo(trackFile,
                options => options
                    .IncludingAllRuntimeProperties()
                    .Excluding(c => c.DateAdded)
                    .Excluding(c => c.Path)
                    .Excluding(c => c.Artist)
                    .Excluding(c => c.Tracks)
                    .Excluding(c => c.Album)
                    .Excluding(c => c.ArtistId));
        }

        [Test]
        public void one_to_one_should_not_query_db_if_foreign_key_is_zero()
        {
            var track = Builder<Track>.CreateNew()
                .With(c => c.TrackFileId = 0)
                .BuildNew();

            Db.Insert(track);

            Db.Single<Track>().TrackFile.Value.Should().BeNull();
        }


        [Test]
        public void embedded_document_as_json()
        {
            var quality = new QualityModel { Quality = Quality.MP3_320, Revision = new Revision(version: 2 )};

            var history = Builder<History.History>.CreateNew()
                            .With(c => c.Id = 0)
                            .With(c => c.Quality = quality)
                            .Build();

            Db.Insert(history);

            var loadedQuality = Db.Single<History.History>().Quality;
            loadedQuality.Should().Be(quality);
        }

        [Test]
        public void embedded_list_of_document_with_json()
        {
            var history = Builder<History.History>.CreateListOfSize(2)
                            .All().With(c => c.Id = 0)
                            .Build().ToList();

            history[0].Quality = new QualityModel(Quality.MP3_320, new Revision(version: 2));
            history[1].Quality = new QualityModel(Quality.MP3_256, new Revision(version: 2));


            Db.InsertMany(history);

            var returnedHistory = Db.All<History.History>();

            returnedHistory[0].Quality.Quality.Should().Be(Quality.MP3_320);
        }
    }
}

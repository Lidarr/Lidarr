using System;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using FluentAssertions.Equivalency;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.History;
using NzbDrone.Core.Music;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class DatabaseRelationshipFixture : DbTest
    {
        [SetUp]
        public void Setup()
        {
            AssertionOptions.AssertEquivalencyUsing(options =>
            {
                options.Using<DateTime>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation.ToUniversalTime())).WhenTypeIs<DateTime>();
                options.Using<DateTime?>(ctx => ctx.Subject.Should().BeCloseTo(ctx.Expectation.Value.ToUniversalTime())).WhenTypeIs<DateTime?>();
                return options;
            });
        }

        [Test]
        public void one_to_one()
        {
            var album = Builder<Album>.CreateNew()
                .With(c => c.Id = 0)
                .With(x => x.ReleaseDate = DateTime.UtcNow)
                .With(x => x.LastInfoSync = DateTime.UtcNow)
                .With(x => x.Added = DateTime.UtcNow)
                .BuildNew();
            Db.Insert(album);

            var albumRelease = Builder<AlbumRelease>.CreateNew()
                .With(c => c.Id = 0)
                .With(c => c.AlbumId = album.Id)
                .BuildNew();
            Db.Insert(albumRelease);

            var loadedAlbum = Db.Single<AlbumRelease>().Album.Value;

            loadedAlbum.Should().NotBeNull();
            loadedAlbum.Should().BeEquivalentTo(album, AlbumComparerOptions);
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
            var quality = new QualityModel { Quality = Quality.MP3_320, Revision = new Revision(version: 2) };

            var history = Builder<EntityHistory>.CreateNew()
                            .With(c => c.Id = 0)
                            .With(c => c.Quality = quality)
                            .Build();

            Db.Insert(history);

            var loadedQuality = Db.Single<EntityHistory>().Quality;
            loadedQuality.Should().Be(quality);
        }

        [Test]
        public void embedded_list_of_document_with_json()
        {
            var history = Builder<EntityHistory>.CreateListOfSize(2)
                            .All().With(c => c.Id = 0)
                            .Build().ToList();

            history[0].Quality = new QualityModel(Quality.MP3_320, new Revision(version: 2));
            history[1].Quality = new QualityModel(Quality.MP3_256, new Revision(version: 2));

            Db.InsertMany(history);

            var returnedHistory = Db.All<EntityHistory>();

            returnedHistory[0].Quality.Quality.Should().Be(Quality.MP3_320);
        }

        private EquivalencyAssertionOptions<Album> AlbumComparerOptions(EquivalencyAssertionOptions<Album> opts) => opts.ComparingByMembers<Album>()
                .Excluding(ctx => ctx.SelectedMemberInfo.MemberType.IsGenericType && ctx.SelectedMemberInfo.MemberType.GetGenericTypeDefinition() == typeof(LazyLoaded<>))
                .Excluding(x => x.ArtistId);
    }
}

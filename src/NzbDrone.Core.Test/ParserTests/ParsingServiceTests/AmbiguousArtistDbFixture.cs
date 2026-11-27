using System;
using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests.ParsingServiceTests
{
    [TestFixture]
    public class AmbiguousArtistDbFixture : DbTest<ParsingService, Artist>
    {
        private ArtistRepository _artistRepo;
        private ArtistMetadataRepository _artistMetadataRepo;
        private AlbumRepository _albumRepo;
        private int _albumCount;

        [SetUp]
        public void Setup()
        {
            _artistRepo = Mocker.Resolve<ArtistRepository>();
            _artistMetadataRepo = Mocker.Resolve<ArtistMetadataRepository>();
            _albumRepo = Mocker.Resolve<AlbumRepository>();

            Mocker.SetConstant<IArtistRepository>(_artistRepo);
            Mocker.SetConstant<IAlbumRepository>(_albumRepo);
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());
            Mocker.SetConstant<IAlbumYearMatcher>(Mocker.Resolve<AlbumYearMatcher>());
            Mocker.SetConstant<IArtistService>(Mocker.Resolve<ArtistService>());
            Mocker.SetConstant<IAlbumService>(Mocker.Resolve<AlbumService>());
            _albumCount = 0;
        }

        private Artist AddArtist(string name, string foreignId)
        {
            var metadata = Builder<ArtistMetadata>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Name = name)
                .With(a => a.ForeignArtistId = foreignId)
                .With(a => a.Aliases = new List<string>())
                .BuildNew();

            var artist = Builder<Artist>.CreateNew()
                .With(a => a.Id = 0)
                .With(a => a.Metadata = metadata)
                .With(a => a.CleanName = Parser.Parser.CleanArtistName(name))
                .With(a => a.ForeignArtistId = foreignId)
                .BuildNew();

            _artistMetadataRepo.Insert(metadata);
            artist.ArtistMetadataId = metadata.Id;
            _artistRepo.Insert(artist);
            return artist;
        }

        private void AddAlbum(Artist artist, string title, int? year)
        {
            _albumCount++;
            _albumRepo.Insert(new Album
            {
                Title = title,
                CleanTitle = Parser.Parser.CleanArtistName(title),
                ForeignAlbumId = "album-" + _albumCount,
                ArtistMetadataId = artist.ArtistMetadataId,
                AlbumType = "Album",
                ReleaseDate = year.HasValue ? new DateTime(year.Value, 1, 1) : null
            });
        }

        [Test]
        public void should_not_pick_other_artist_when_first_artist_has_album_twice()
        {
            // Both artists have the album; one of them twice
            var a = AddArtist("Some Artist", "a-1");
            var b = AddArtist("Some-Artist", "b-1");
            AddAlbum(a, "Greatest Hits", 2005);
            AddAlbum(a, "Greatest Hits", 2015);
            AddAlbum(b, "Greatest Hits", 2005);

            Action act = () => Subject.GetArtist("Some Artist - Greatest Hits (2005) [FLAC]");

            act.Should().Throw<MultipleArtistsFoundException>();
        }

        [Test]
        public void map_should_not_pick_other_artist_when_first_artist_has_album_twice()
        {
            var a = AddArtist("Some Artist", "a-1");
            var b = AddArtist("Some-Artist", "b-1");
            AddAlbum(a, "Weezer", 1994);
            AddAlbum(a, "Weezer", 2001);
            AddAlbum(b, "Weezer", 2001);

            Action act = () => Subject.Map(Parser.Parser.ParseAlbumTitle("Some Artist - Weezer [FLAC]"));

            act.Should().Throw<MultipleArtistsFoundException>();
        }

        [Test]
        public void single_artist_exact_match_unchanged()
        {
            var a = AddArtist("Some Artist", "a-1");
            AddArtist("Other Band", "b-1");

            Subject.GetArtist("Some Artist - Anything (2005) [FLAC]").Id.Should().Be(a.Id);
        }

        [Test]
        public void no_year_picks_only_artist_with_album()
        {
            var a = AddArtist("Some Artist", "a-1");
            var b = AddArtist("Some-Artist", "b-1");
            AddAlbum(a, "Album One", 2005);
            AddAlbum(b, "Album Two", 2005);

            Subject.GetArtist("Some Artist - Album One [FLAC]").Id.Should().Be(a.Id);
        }

        [Test]
        public void year_within_tolerance_on_both_throws()
        {
            var a = AddArtist("Some Artist", "a-1");
            var b = AddArtist("Some-Artist", "b-1");
            AddAlbum(a, "Album One", 2005);
            AddAlbum(b, "Album One", 2010);

            Action act = () => Subject.GetArtist("Some Artist - Album One (2005) [FLAC]");

            act.Should().Throw<MultipleArtistsFoundException>();
        }

        [Test]
        public void undated_album_on_other_artist_throws()
        {
            var a = AddArtist("Some Artist", "a-1");
            var b = AddArtist("Some-Artist", "b-1");
            AddAlbum(a, "Album One", 2005);
            AddAlbum(b, "Album One", null);

            Action act = () => Subject.GetArtist("Some Artist - Album One (2005) [FLAC]");

            act.Should().Throw<MultipleArtistsFoundException>();
        }

        [Test]
        public void case_only_artist_difference_disambiguated_by_year()
        {
            var a = AddArtist("ABBA", "a-1");
            var b = AddArtist("Abba", "b-1");
            AddAlbum(a, "Gold", 1992);
            AddAlbum(b, "Gold", 2020);

            Subject.GetArtist("ABBA - Gold (1992) [FLAC]").Id.Should().Be(a.Id);
        }
    }
}

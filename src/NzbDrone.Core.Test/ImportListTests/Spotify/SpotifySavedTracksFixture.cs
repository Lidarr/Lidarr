using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.ImportLists.Spotify;
using NzbDrone.Core.Test.Framework;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;

namespace NzbDrone.Core.Test.ImportListTests
{
    [TestFixture]
    public class SpotifySavedTracksFixture : CoreTest<SpotifySavedTracks>
    {
        private static SavedTrack GivenSavedTrack(string artistName, string albumName, string albumId = null)
        {
            return new SavedTrack
            {
                AddedAt = DateTime.UtcNow,
                Track = new FullTrack
                {
                    Album = new SimpleAlbum
                    {
                        Id = albumId,
                        Name = albumName,
                        Artists = new List<SimpleArtist>
                        {
                            new SimpleArtist
                            {
                                Name = artistName
                            }
                        }
                    }
                }
            };
        }

        private void GivenSavedTracks(Paging<SavedTrack> savedTracks)
        {
            Mocker.GetMock<ISpotifyProxy>()
                .Setup(x => x.GetSavedTracks(It.IsAny<SpotifySavedTracks>(),
                                             It.IsAny<SpotifyWebAPI>()))
                .Returns(savedTracks);
        }

        [Test]
        public void should_not_throw_if_saved_tracks_is_null()
        {
            GivenSavedTracks(null);

            var result = Subject.Fetch(null);

            result.Should().BeEmpty();
        }

        [Test]
        public void should_not_throw_if_saved_track_items_is_null()
        {
            GivenSavedTracks(new Paging<SavedTrack> { Items = null });

            var result = Subject.Fetch(null);

            result.Should().BeEmpty();
        }

        [Test]
        public void should_not_throw_if_saved_track_is_null()
        {
            GivenSavedTracks(new Paging<SavedTrack>
            {
                Items = new List<SavedTrack> { null }
            });

            var result = Subject.Fetch(null);

            result.Should().BeEmpty();
        }

        [Test]
        public void should_not_throw_if_saved_track_track_is_null()
        {
            GivenSavedTracks(new Paging<SavedTrack>
            {
                Items = new List<SavedTrack> { new SavedTrack { Track = null } }
            });

            var result = Subject.Fetch(null);

            result.Should().BeEmpty();
        }

        [TestCase("Artist", "Album")]
        public void should_parse_saved_track(string artistName, string albumName)
        {
            GivenSavedTracks(new Paging<SavedTrack>
            {
                Items = new List<SavedTrack> { GivenSavedTrack(artistName, albumName) }
            });

            var result = Subject.Fetch(null);

            result.Should().HaveCount(1);
            result[0].Artist.Should().Be(artistName);
            result[0].Album.Should().Be(albumName);
        }

        [Test]
        public void should_only_return_each_album_once()
        {
            GivenSavedTracks(new Paging<SavedTrack>
            {
                Items = new List<SavedTrack>
                {
                    GivenSavedTrack("Artist", "Album", "albumid"),
                    GivenSavedTrack("Artist", "Album", "albumid"),
                    GivenSavedTrack("Artist", "Other Album", "otheralbumid")
                }
            });

            var result = Subject.Fetch(null);

            result.Should().HaveCount(2);
        }

        [Test]
        public void should_keep_albums_without_an_id()
        {
            GivenSavedTracks(new Paging<SavedTrack>
            {
                Items = new List<SavedTrack>
                {
                    GivenSavedTrack("Artist", "Album"),
                    GivenSavedTrack("Other Artist", "Other Album")
                }
            });

            var result = Subject.Fetch(null);

            result.Should().HaveCount(2);
        }

        [Test]
        public void should_not_throw_if_get_next_page_returns_null()
        {
            GivenSavedTracks(new Paging<SavedTrack>
            {
                Items = new List<SavedTrack> { GivenSavedTrack("Artist", "Album") },
                Next = "DummyToMakeHasNextTrue"
            });

            Mocker.GetMock<ISpotifyProxy>()
                .Setup(x => x.GetNextPage(It.IsAny<SpotifySavedTracks>(),
                                          It.IsAny<SpotifyWebAPI>(),
                                          It.IsAny<Paging<SavedTrack>>()))
                .Returns(default(Paging<SavedTrack>));

            var result = Subject.Fetch(null);

            result.Should().HaveCount(1);

            Mocker.GetMock<ISpotifyProxy>()
                .Verify(x => x.GetNextPage(It.IsAny<SpotifySavedTracks>(),
                                           It.IsAny<SpotifyWebAPI>(),
                                           It.IsAny<Paging<SavedTrack>>()),
                        Times.Once());
        }

        [TestCase(null, "Album")]
        [TestCase("Artist", null)]
        [TestCase(null, null)]
        public void should_skip_bad_artist_or_album_names(string artistName, string albumName)
        {
            GivenSavedTracks(new Paging<SavedTrack>
            {
                Items = new List<SavedTrack> { GivenSavedTrack(artistName, albumName) }
            });

            var result = Subject.Fetch(null);

            result.Should().BeEmpty();
        }
    }
}

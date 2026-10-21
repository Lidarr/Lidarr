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
        [Test]
        public void should_not_throw_if_saved_tracks_is_null()
        {
            var paging = default(Paging<SavedTrack>);

            Mocker.GetMock<ISpotifyProxy>().
                Setup(x => x.GetSavedTracks(It.IsAny<SpotifySavedTracks>(),
                                                It.IsAny<SpotifyWebAPI>()))
                .Returns(paging);

            var result = Subject.Fetch(null);

            result.Should().BeEmpty();
        }

        [Test]
        public void should_not_throw_if_saved_track_items_is_null()
        {
            var savedTracks = new Paging<SavedTrack>
            {
                Items = null
            };

            Mocker.GetMock<ISpotifyProxy>().
                Setup(x => x.GetSavedTracks(It.IsAny<SpotifySavedTracks>(),
                                                It.IsAny<SpotifyWebAPI>()))
                .Returns(savedTracks);

            var result = Subject.Fetch(null);

            result.Should().BeEmpty();
        }

        [Test]
        public void should_not_throw_if_saved_track_is_null()
        {
            var savedTracks = new Paging<SavedTrack>
            {
                Items = new List<SavedTrack>
                {
                    null
                }
            };

            Mocker.GetMock<ISpotifyProxy>().
                Setup(x => x.GetSavedTracks(It.IsAny<SpotifySavedTracks>(),
                                                It.IsAny<SpotifyWebAPI>()))
                .Returns(savedTracks);

            var result = Subject.Fetch(null);

            result.Should().BeEmpty();
        }

        [Test]
        public void should_not_throw_if_saved_track_track_is_null()
        {
            var savedTracks = new Paging<SavedTrack>
            {
                Items = new List<SavedTrack>
                {
                    new SavedTrack
                    {
                        Track = null
                    }
                }
            };

            Mocker.GetMock<ISpotifyProxy>().
                Setup(x => x.GetSavedTracks(It.IsAny<SpotifySavedTracks>(),
                                                It.IsAny<SpotifyWebAPI>()))
                .Returns(savedTracks);

            var result = Subject.Fetch(null);

            result.Should().BeEmpty();
        }

        [TestCase("Artist", "Album")]
        public void should_parse_saved_track(string artistName, string albumName)
        {
            var savedTracks = new Paging<SavedTrack>
            {
                Items = new List<SavedTrack>
                {
                    new SavedTrack
                    {
                        AddedAt = System.DateTime.Now,
                        Track = new FullTrack
                        {
                            Album = new SimpleAlbum
                            {
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
                    }
                }
            };

            Mocker.GetMock<ISpotifyProxy>().
                Setup(x => x.GetSavedTracks(It.IsAny<SpotifySavedTracks>(),
                                                It.IsAny<SpotifyWebAPI>()))
                .Returns(savedTracks);

            var result = Subject.Fetch(null);

            result.Should().HaveCount(1);
        }

        [Test]
        public void should_not_throw_if_get_next_page_returns_null()
        {
            var savedTracks = new Paging<SavedTrack>
            {
                Items = new List<SavedTrack>
                {
                    new SavedTrack
                    {
                        AddedAt = System.DateTime.Now,
                        Track = new FullTrack
                        {
                            Album = new SimpleAlbum
                            {
                                Name = "Album",
                                Artists = new List<SimpleArtist>
                                {
                                    new SimpleArtist
                                    {
                                        Name = "Artist"
                                    }
                                }
                            }
                        }
                    }
                },
                Next = "DummyToMakeHasNextTrue"
            };

            Mocker.GetMock<ISpotifyProxy>().
                Setup(x => x.GetSavedTracks(It.IsAny<SpotifySavedTracks>(),
                                                It.IsAny<SpotifyWebAPI>()))
                .Returns(savedTracks);

            Mocker.GetMock<ISpotifyProxy>()
                .Setup(x => x.GetNextPage(It.IsAny<SpotifyFollowedArtists>(),
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
            var savedTracks = new Paging<SavedTrack>
            {
                Items = new List<SavedTrack>
                {
                    new SavedTrack
                    {
                        AddedAt = System.DateTime.Now,
                        Track = new FullTrack
                        {
                            Album = new SimpleAlbum
                            {
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
                    }
                }
            };

            Mocker.GetMock<ISpotifyProxy>().
                Setup(x => x.GetSavedTracks(It.IsAny<SpotifySavedTracks>(),
                                                It.IsAny<SpotifyWebAPI>()))
                .Returns(savedTracks);

            var result = Subject.Fetch(null);

            result.Should().BeEmpty();
        }
    }
}

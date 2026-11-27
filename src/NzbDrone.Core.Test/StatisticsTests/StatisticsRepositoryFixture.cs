using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.ArtistStats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Statistics;
using NzbDrone.Core.Tags;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.StatisticsTests
{
    [TestFixture]
    public class StatisticsRepositoryFixture : DbTest<StatisticsRepository, Artist>
    {
        private int _nextId = 1;
        private DateTime _released;
        private DateTime _unreleased;

        [SetUp]
        public void Setup()
        {
            _released = DateTime.UtcNow.AddDays(-5);
            _unreleased = DateTime.UtcNow.AddDays(5);
        }

        private Artist GivenArtist(bool monitored = true,
                                   ArtistStatusType status = ArtistStatusType.Continuing,
                                   int qualityProfileId = 1,
                                   int metadataProfileId = 1,
                                   string path = null,
                                   HashSet<int> tags = null,
                                   string name = null)
        {
            var id = _nextId++;

            var metadata = Db.Insert(Builder<ArtistMetadata>.CreateNew()
                                                            .With(m => m.ForeignArtistId = $"artist-{id}")
                                                            .With(m => m.Name = name ?? $"Artist {id}")
                                                            .With(m => m.Status = status)
                                                            .BuildNew());

            return Db.Insert(Builder<Artist>.CreateNew()
                                            .With(a => a.ArtistMetadataId = metadata.Id)
                                            .With(a => a.CleanName = $"artist{id}")
                                            .With(a => a.Monitored = monitored)
                                            .With(a => a.QualityProfileId = qualityProfileId)
                                            .With(a => a.MetadataProfileId = metadataProfileId)
                                            .With(a => a.Path = (path ?? $@"C:\music\Artist {id}").AsOsAgnostic())
                                            .With(a => a.Tags = tags ?? new HashSet<int>())
                                            .BuildNew());
        }

        private Album GivenAlbum(Artist artist, DateTime? releaseDate, bool monitored = true, string albumType = "Album", List<SecondaryAlbumType> secondaryTypes = null, List<string> genres = null)
        {
            var id = _nextId++;

            return Db.Insert(Builder<Album>.CreateNew()
                                           .With(a => a.ArtistMetadataId = artist.ArtistMetadataId)
                                           .With(a => a.ForeignAlbumId = $"album-{id}")
                                           .With(a => a.CleanTitle = $"album{id}")
                                           .With(a => a.ReleaseDate = releaseDate)
                                           .With(a => a.Monitored = monitored)
                                           .With(a => a.AlbumType = albumType)
                                           .With(a => a.SecondaryTypes = secondaryTypes ?? new List<SecondaryAlbumType>())
                                           .With(a => a.Genres = genres ?? new List<string>())
                                           .BuildNew());
        }

        private QualityProfile GivenQualityProfile(string name)
        {
            return Db.Insert(new QualityProfile { Name = name, Items = new List<QualityProfileQualityItem>(), FormatItems = new List<ProfileFormatItem>() });
        }

        private MetadataProfile GivenMetadataProfile(string name)
        {
            return Db.Insert(new MetadataProfile
            {
                Name = name,
                PrimaryAlbumTypes = new List<ProfilePrimaryAlbumTypeItem>(),
                SecondaryAlbumTypes = new List<ProfileSecondaryAlbumTypeItem>(),
                ReleaseStatuses = new List<ProfileReleaseStatusItem>()
            });
        }

        private AlbumRelease GivenRelease(Album album, bool monitored = true, params string[] mediaFormats)
        {
            return Db.Insert(Builder<AlbumRelease>.CreateNew()
                                                  .With(r => r.AlbumId = album.Id)
                                                  .With(r => r.ForeignReleaseId = $"release-{_nextId++}")
                                                  .With(r => r.Monitored = monitored)
                                                  .With(r => r.Media = mediaFormats.Select((f, i) => new Medium { Number = i + 1, Format = f }).ToList())
                                                  .BuildNew());
        }

        private void GivenTrack(AlbumRelease release, Artist artist, int trackFileId = 0)
        {
            var id = _nextId++;

            Db.Insert(Builder<Track>.CreateNew()
                                    .With(t => t.AlbumReleaseId = release.Id)
                                    .With(t => t.ArtistMetadataId = artist.ArtistMetadataId)
                                    .With(t => t.ForeignTrackId = $"track-{id}")
                                    .With(t => t.ForeignRecordingId = $"recording-{id}")
                                    .With(t => t.TrackFileId = trackFileId)
                                    .BuildNew());
        }

        private TrackFile GivenTrackFile(int albumId, long size = 100, Quality quality = null)
        {
            return Db.Insert(Builder<TrackFile>.CreateNew()
                                               .With(f => f.AlbumId = albumId)
                                               .With(f => f.Path = $@"C:\music\Track {_nextId++}.flac".AsOsAgnostic())
                                               .With(f => f.Size = size)
                                               .With(f => f.Quality = new QualityModel(quality ?? Quality.FLAC))
                                               .BuildNew());
        }

        // An album whose monitored release has trackCount tracks, the first fileCount of them with a 100 byte file each
        private Album GivenAlbumWithTracks(Artist artist, DateTime? releaseDate, int trackCount, int fileCount = 0, bool monitored = true, List<string> genres = null)
        {
            var album = GivenAlbum(artist, releaseDate, monitored, genres: genres);
            var release = GivenRelease(album);

            for (var i = 0; i < trackCount; i++)
            {
                GivenTrack(release, artist, i < fileCount ? GivenTrackFile(album.Id).Id : 0);
            }

            return album;
        }

        private Tag GivenTag(string label)
        {
            return Db.Insert(new Tag { Label = label });
        }

        [Test]
        public void should_return_zeros_when_library_is_empty()
        {
            var stats = Subject.GetLibraryStatistics();

            stats.ArtistCount.Should().Be(0);
            stats.MonitoredArtistCount.Should().Be(0);
            stats.CompletedArtistCount.Should().Be(0);
            stats.ContinuingArtistCount.Should().Be(0);
            stats.EndedArtistCount.Should().Be(0);
            stats.DeletedArtistCount.Should().Be(0);
            stats.AlbumCount.Should().Be(0);
            stats.MonitoredAlbumCount.Should().Be(0);
            stats.CompletedAlbumCount.Should().Be(0);
            stats.MissingAlbumCount.Should().Be(0);
            stats.TotalTrackCount.Should().Be(0);
            stats.MonitoredTrackCount.Should().Be(0);
            stats.DownloadedTrackCount.Should().Be(0);
            stats.MissingTrackCount.Should().Be(0);
            stats.UnreleasedTrackCount.Should().Be(0);
            stats.TrackFileCount.Should().Be(0);
            stats.SizeOnDisk.Should().Be(0);
        }

        [Test]
        public void should_count_artists_by_monitored_and_status()
        {
            GivenArtist(monitored: true, status: ArtistStatusType.Continuing);
            GivenArtist(monitored: true, status: ArtistStatusType.Continuing);
            GivenArtist(monitored: false, status: ArtistStatusType.Ended);
            GivenArtist(monitored: true, status: ArtistStatusType.Deleted);

            var stats = Subject.GetLibraryStatistics();

            stats.ArtistCount.Should().Be(4);
            stats.MonitoredArtistCount.Should().Be(3);
            stats.ContinuingArtistCount.Should().Be(2);
            stats.EndedArtistCount.Should().Be(1);
            stats.DeletedArtistCount.Should().Be(1);
        }

        [Test]
        public void should_count_completed_artists()
        {
            var complete = GivenArtist();
            GivenAlbumWithTracks(complete, _released, trackCount: 2, fileCount: 2);
            GivenAlbumWithTracks(complete, _unreleased, trackCount: 1);

            var incomplete = GivenArtist();
            GivenAlbumWithTracks(incomplete, _released, trackCount: 2, fileCount: 1);

            GivenArtist();

            var stats = Subject.GetLibraryStatistics();

            stats.ArtistCount.Should().Be(3);
            stats.CompletedArtistCount.Should().Be(1);
        }

        [Test]
        public void should_count_albums_and_completed_albums()
        {
            var artist = GivenArtist();

            GivenAlbumWithTracks(artist, _released, trackCount: 2, fileCount: 2);
            GivenAlbumWithTracks(artist, _released, trackCount: 2, fileCount: 1);
            GivenAlbumWithTracks(artist, _released, trackCount: 1, monitored: false);
            GivenAlbum(artist, _released);

            var stats = Subject.GetLibraryStatistics();

            stats.AlbumCount.Should().Be(4);
            stats.MonitoredAlbumCount.Should().Be(3);
            stats.CompletedAlbumCount.Should().Be(1);
            stats.MissingAlbumCount.Should().Be(1);
        }

        [Test]
        public void should_count_missing_albums_the_way_wanted_missing_does()
        {
            var artist = GivenArtist();
            var unmonitoredArtist = GivenArtist(monitored: false);

            GivenAlbumWithTracks(artist, _released, trackCount: 3, fileCount: 2);
            GivenAlbumWithTracks(artist, _released, trackCount: 2);
            GivenAlbumWithTracks(artist, _released, trackCount: 2, monitored: false);
            GivenAlbumWithTracks(artist, _unreleased, trackCount: 2);
            GivenAlbumWithTracks(artist, null, trackCount: 2);
            GivenAlbumWithTracks(unmonitoredArtist, _released, trackCount: 2);

            var stats = Subject.GetLibraryStatistics();

            stats.MissingAlbumCount.Should().Be(2);
            stats.MissingTrackCount.Should().Be(3);
        }

        [Test]
        public void should_agree_with_wanted_missing_and_artist_statistics()
        {
            var artist = GivenArtist();
            var unmonitoredArtist = GivenArtist(monitored: false);

            GivenAlbumWithTracks(artist, _released, trackCount: 2, fileCount: 2);
            GivenAlbumWithTracks(artist, _released, trackCount: 3, fileCount: 1);
            GivenAlbumWithTracks(artist, _released, trackCount: 2, monitored: false);
            GivenAlbumWithTracks(artist, _unreleased, trackCount: 2, fileCount: 1);
            GivenAlbumWithTracks(artist, null, trackCount: 1);
            GivenAlbumWithTracks(unmonitoredArtist, _released, trackCount: 2);
            GivenAlbumWithTracks(unmonitoredArtist, _released, trackCount: 1, fileCount: 1);

            var wanted = new PagingSpec<Album> { Page = 1, PageSize = 100 };
            wanted.FilterExpressions.Add(a => a.Monitored == true && a.Artist.Value.Monitored == true);
            var albumStatistics = Mocker.Resolve<ArtistStatisticsRepository>().ArtistStatistics();

            var stats = Subject.GetLibraryStatistics();

            stats.MissingAlbumCount.Should().Be(1);
            stats.MissingAlbumCount.Should().Be(Mocker.Resolve<AlbumRepository>().AlbumsWithoutFiles(wanted).TotalRecords);
            stats.CompletedAlbumCount.Should().Be(3);
            stats.CompletedAlbumCount.Should().Be(albumStatistics.Count(a => a.TrackCount > 0 && a.TrackCount == a.TrackFileCount));
        }

        [Test]
        public void should_count_tracks()
        {
            var artist = GivenArtist();

            GivenAlbumWithTracks(artist, _released, trackCount: 3, fileCount: 1);
            GivenAlbumWithTracks(artist, _released, trackCount: 1, monitored: false);
            GivenAlbumWithTracks(artist, _unreleased, trackCount: 1);
            GivenAlbumWithTracks(artist, null, trackCount: 1);

            var stats = Subject.GetLibraryStatistics();

            stats.TotalTrackCount.Should().Be(6);
            stats.MonitoredTrackCount.Should().Be(5);
            stats.DownloadedTrackCount.Should().Be(1);
            stats.MissingTrackCount.Should().Be(2);
            stats.UnreleasedTrackCount.Should().Be(2);
        }

        [Test]
        public void should_only_count_tracks_on_the_monitored_release()
        {
            var artist = GivenArtist();
            var album = GivenAlbum(artist, _released);
            var monitoredRelease = GivenRelease(album);
            var otherRelease = GivenRelease(album, monitored: false);

            GivenTrack(monitoredRelease, artist);
            GivenTrack(otherRelease, artist);
            GivenTrack(otherRelease, artist);

            var stats = Subject.GetLibraryStatistics();

            stats.TotalTrackCount.Should().Be(1);
            stats.MissingTrackCount.Should().Be(1);
        }

        [Test]
        public void should_not_count_unreleased_track_with_file_as_unreleased()
        {
            GivenAlbumWithTracks(GivenArtist(), _unreleased, trackCount: 1, fileCount: 1);

            var stats = Subject.GetLibraryStatistics();

            stats.DownloadedTrackCount.Should().Be(1);
            stats.UnreleasedTrackCount.Should().Be(0);
        }

        [Test]
        public void should_count_track_whose_file_row_is_gone_as_missing()
        {
            var artist = GivenArtist();
            var release = GivenRelease(GivenAlbum(artist, _released));

            GivenTrack(release, artist, trackFileId: 999);

            var stats = Subject.GetLibraryStatistics();

            stats.DownloadedTrackCount.Should().Be(0);
            stats.MissingTrackCount.Should().Be(1);
            stats.MissingAlbumCount.Should().Be(1);
        }

        [Test]
        public void should_count_album_dated_year_one_as_released()
        {
            GivenAlbumWithTracks(GivenArtist(), new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc), trackCount: 1);

            var stats = Subject.GetLibraryStatistics();

            stats.MissingTrackCount.Should().Be(1);
            stats.UnreleasedTrackCount.Should().Be(0);
        }

        [Test]
        public void should_sum_track_file_count_and_size_on_disk()
        {
            var album = GivenAlbum(GivenArtist(), _released);

            GivenTrackFile(album.Id, 100);
            GivenTrackFile(album.Id, 200);

            var stats = Subject.GetLibraryStatistics();

            stats.TrackFileCount.Should().Be(2);
            stats.SizeOnDisk.Should().Be(300);
        }

        [Test]
        public void should_count_a_file_shared_by_two_tracks_once()
        {
            var artist = GivenArtist();
            var album = GivenAlbum(artist, _released);
            var release = GivenRelease(album);
            var file = GivenTrackFile(album.Id, 500);

            GivenTrack(release, artist, file.Id);
            GivenTrack(release, artist, file.Id);

            var stats = Subject.GetLibraryStatistics();

            stats.DownloadedTrackCount.Should().Be(2);
            stats.TrackFileCount.Should().Be(1);
            stats.SizeOnDisk.Should().Be(500);
        }

        [Test]
        public void should_not_count_unmapped_track_files()
        {
            GivenAlbum(GivenArtist(), _released);
            GivenTrackFile(0, 700);

            var stats = Subject.GetLibraryStatistics();

            stats.TrackFileCount.Should().Be(0);
            stats.SizeOnDisk.Should().Be(0);
        }

        [Test]
        public void should_filter_artist_counts_by_root_folder()
        {
            GivenArtist(path: @"C:\music\Artist 1".AsOsAgnostic());
            GivenArtist(path: @"C:\music2\Artist 2".AsOsAgnostic());
            GivenArtist(path: @"C:\films\Artist 3".AsOsAgnostic());

            Subject.GetLibraryStatistics().ArtistCount.Should().Be(3);
            Subject.GetLibraryStatistics(new StatisticsFilter { RootFolderPaths = new List<string> { @"C:\music".AsOsAgnostic() } }).ArtistCount.Should().Be(1);
            Subject.GetLibraryStatistics(new StatisticsFilter { RootFolderPaths = new List<string> { @"C:\music\".AsOsAgnostic() } }).ArtistCount.Should().Be(1);
            Subject.GetLibraryStatistics(new StatisticsFilter { RootFolderPaths = new List<string> { @"C:\audiobooks".AsOsAgnostic() } }).ArtistCount.Should().Be(0);
        }

        [Test]
        public void should_filter_album_track_and_file_counts_by_root_folder()
        {
            var artist1 = GivenArtist(path: @"C:\music\Artist 1".AsOsAgnostic());
            var artist2 = GivenArtist(path: @"C:\music2\Artist 2".AsOsAgnostic());

            GivenAlbumWithTracks(artist1, _released, trackCount: 2, fileCount: 1);
            GivenAlbumWithTracks(artist2, _released, trackCount: 3, fileCount: 3);

            var stats = Subject.GetLibraryStatistics(new StatisticsFilter { RootFolderPaths = new List<string> { @"C:\music".AsOsAgnostic() } });

            stats.ArtistCount.Should().Be(1);
            stats.CompletedArtistCount.Should().Be(0);
            stats.AlbumCount.Should().Be(1);
            stats.CompletedAlbumCount.Should().Be(0);
            stats.MissingAlbumCount.Should().Be(1);
            stats.TotalTrackCount.Should().Be(2);
            stats.DownloadedTrackCount.Should().Be(1);
            stats.MissingTrackCount.Should().Be(1);
            stats.TrackFileCount.Should().Be(1);
            stats.SizeOnDisk.Should().Be(100);
        }

        [Test]
        public void should_filter_by_root_folder_with_characters_outside_the_bmp()
        {
            GivenArtist(path: @"C:\music\🎵 Mix\Artist 1".AsOsAgnostic());
            GivenArtist(path: @"C:\music\Artist 2".AsOsAgnostic());

            Subject.GetLibraryStatistics(new StatisticsFilter { RootFolderPaths = new List<string> { @"C:\music\🎵 Mix".AsOsAgnostic() } }).ArtistCount.Should().Be(1);
            Subject.GetLibraryStatistics(new StatisticsFilter { RootFolderPaths = new List<string> { @"C:\music\🎵 Mix".AsOsAgnostic() }, RootFolderPathsNot = true }).ArtistCount.Should().Be(1);
        }

        [Test]
        public void should_skip_blank_root_folder_paths()
        {
            GivenArtist(path: @"C:\music\Artist 1".AsOsAgnostic());
            GivenArtist(path: @"C:\music2\Artist 2".AsOsAgnostic());

            Subject.GetLibraryStatistics(new StatisticsFilter { RootFolderPaths = new List<string> { null, "", " " } }).ArtistCount.Should().Be(2);
            Subject.GetLibraryStatistics(new StatisticsFilter { RootFolderPaths = new List<string> { null, @"C:\music".AsOsAgnostic() } }).ArtistCount.Should().Be(1);
        }

        [Test]
        public void should_filter_by_monitored()
        {
            var monitored = GivenArtist(monitored: true);
            var unmonitored = GivenArtist(monitored: false);

            GivenAlbumWithTracks(monitored, _released, trackCount: 1, fileCount: 1);
            GivenAlbumWithTracks(unmonitored, _released, trackCount: 2, fileCount: 2);

            var monitoredStats = Subject.GetLibraryStatistics(new StatisticsFilter { Monitored = true });
            var unmonitoredStats = Subject.GetLibraryStatistics(new StatisticsFilter { Monitored = false });

            monitoredStats.ArtistCount.Should().Be(1);
            monitoredStats.TrackFileCount.Should().Be(1);
            unmonitoredStats.ArtistCount.Should().Be(1);
            unmonitoredStats.TrackFileCount.Should().Be(2);
        }

        [Test]
        public void should_filter_by_quality_profile()
        {
            var artist = GivenArtist(qualityProfileId: 1);
            GivenArtist(qualityProfileId: 2);
            GivenArtist(qualityProfileId: 3);

            GivenAlbumWithTracks(artist, _released, trackCount: 1, fileCount: 1);

            var stats = Subject.GetLibraryStatistics(new StatisticsFilter { QualityProfileIds = new List<int> { 1 } });

            stats.ArtistCount.Should().Be(1);
            stats.TrackFileCount.Should().Be(1);
            Subject.GetLibraryStatistics(new StatisticsFilter { QualityProfileIds = new List<int> { 2, 3 } }).ArtistCount.Should().Be(2);
            Subject.GetLibraryStatistics(new StatisticsFilter { QualityProfileIds = new List<int> { 1 }, QualityProfileIdsNot = true }).ArtistCount.Should().Be(2);
        }

        [Test]
        public void should_filter_by_metadata_profile()
        {
            var artist = GivenArtist(metadataProfileId: 1);
            GivenArtist(metadataProfileId: 2);
            GivenArtist(metadataProfileId: 3);

            GivenAlbumWithTracks(artist, _released, trackCount: 1, fileCount: 1);

            var stats = Subject.GetLibraryStatistics(new StatisticsFilter { MetadataProfileIds = new List<int> { 1 } });

            stats.ArtistCount.Should().Be(1);
            stats.TrackFileCount.Should().Be(1);
            Subject.GetLibraryStatistics(new StatisticsFilter { MetadataProfileIds = new List<int> { 2, 3 } }).ArtistCount.Should().Be(2);
            Subject.GetLibraryStatistics(new StatisticsFilter { MetadataProfileIds = new List<int> { 1 }, MetadataProfileIdsNot = true }).ArtistCount.Should().Be(2);
        }

        [Test]
        public void should_filter_by_tag()
        {
            var tag = GivenTag("lossless");
            var otherTag = GivenTag("kids");

            var tagged = GivenArtist(tags: new HashSet<int> { tag.Id });
            var otherTagged = GivenArtist(tags: new HashSet<int> { otherTag.Id });
            GivenArtist();

            GivenAlbumWithTracks(tagged, _released, trackCount: 1, fileCount: 1);
            GivenAlbumWithTracks(otherTagged, _released, trackCount: 2, fileCount: 2);

            var stats = Subject.GetLibraryStatistics(new StatisticsFilter { TagIds = new List<int> { tag.Id } });

            stats.ArtistCount.Should().Be(1);
            stats.TrackFileCount.Should().Be(1);
            stats.SizeOnDisk.Should().Be(100);
        }

        [Test]
        public void should_filter_by_multiple_root_folders()
        {
            GivenArtist(path: @"C:\music\Artist 1".AsOsAgnostic());
            GivenArtist(path: @"C:\films\Artist 2".AsOsAgnostic());
            GivenArtist(path: @"C:\music2\Artist 3".AsOsAgnostic());

            var stats = Subject.GetLibraryStatistics(new StatisticsFilter
            {
                RootFolderPaths = new List<string> { @"C:\music".AsOsAgnostic(), @"C:\films".AsOsAgnostic() }
            });

            stats.ArtistCount.Should().Be(2);
        }

        [Test]
        public void should_filter_by_multiple_tags()
        {
            var tag = GivenTag("lossless");
            var otherTag = GivenTag("kids");

            GivenArtist(tags: new HashSet<int> { tag.Id });
            GivenArtist(tags: new HashSet<int> { otherTag.Id });
            GivenArtist();

            Subject.GetLibraryStatistics(new StatisticsFilter { TagIds = new List<int> { tag.Id, otherTag.Id } }).ArtistCount.Should().Be(2);
        }

        [Test]
        public void should_exclude_root_folders()
        {
            GivenArtist(path: @"C:\music\Artist 1".AsOsAgnostic());
            GivenArtist(path: @"C:\music2\Artist 2".AsOsAgnostic());
            GivenArtist(path: @"C:\films\Artist 3".AsOsAgnostic());

            var stats = Subject.GetLibraryStatistics(new StatisticsFilter
            {
                RootFolderPaths = new List<string> { @"C:\music".AsOsAgnostic() },
                RootFolderPathsNot = true
            });

            stats.ArtistCount.Should().Be(2);
        }

        [Test]
        public void should_exclude_tags()
        {
            var tag = GivenTag("lossless");
            var otherTag = GivenTag("kids");

            GivenArtist(tags: new HashSet<int> { tag.Id });
            GivenArtist(tags: new HashSet<int> { tag.Id, otherTag.Id });
            GivenArtist(tags: new HashSet<int> { otherTag.Id });
            GivenArtist();
            GivenArtist();

            Subject.GetLibraryStatistics(new StatisticsFilter { TagIds = new List<int> { tag.Id }, TagIdsNot = true }).ArtistCount.Should().Be(3);
        }

        [Test]
        public void should_combine_filters()
        {
            var tag = GivenTag("lossless");

            var taggedMonitored = GivenArtist(monitored: true, tags: new HashSet<int> { tag.Id });
            var taggedUnmonitored = GivenArtist(monitored: false, tags: new HashSet<int> { tag.Id });
            GivenArtist(monitored: true);

            GivenAlbumWithTracks(taggedMonitored, _released, trackCount: 1, fileCount: 1);
            GivenAlbumWithTracks(taggedUnmonitored, _released, trackCount: 2, fileCount: 2);

            var stats = Subject.GetLibraryStatistics(new StatisticsFilter { TagIds = new List<int> { tag.Id }, Monitored = true });

            stats.ArtistCount.Should().Be(1);
            stats.TrackFileCount.Should().Be(1);
            stats.SizeOnDisk.Should().Be(100);
        }

        [Test]
        public void should_return_empty_album_and_quality_breakdowns_when_library_is_empty()
        {
            var stats = Subject.GetLibraryStatistics();

            stats.QualityStatistics.Should().BeEmpty();
            stats.AlbumTypeStatistics.Should().BeEmpty();
            stats.SecondaryTypeStatistics.Should().BeEmpty();
            stats.DecadeStatistics.Should().BeEmpty();
            stats.GenreStatistics.Should().BeEmpty();
            stats.LargestArtistStatistics.Should().BeEmpty();
            stats.MediaFormatStatistics.Should().BeEmpty();
        }

        [Test]
        public void should_count_artists_and_files_per_quality_profile_in_name_order_ignoring_case()
        {
            var unused = GivenQualityProfile("Unused");
            var lossless = GivenQualityProfile("lossless");
            var artist = GivenArtist(qualityProfileId: lossless.Id);
            GivenArtist(qualityProfileId: lossless.Id);

            GivenTrackFile(GivenAlbum(artist, _released).Id, size: 100);
            GivenTrackFile(GivenAlbum(artist, _released).Id, size: 200);

            var stats = Subject.GetLibraryStatistics().QualityProfileStatistics;

            stats.Select(p => p.Name).Should().ContainInOrder("lossless", "Unused");
            stats.Single(p => p.QualityProfileId == lossless.Id).Should().BeEquivalentTo(new { Name = "lossless", ArtistCount = 2, TrackFileCount = 2, SizeOnDisk = 300L });
            stats.Single(p => p.QualityProfileId == unused.Id).Should().BeEquivalentTo(new { Name = "Unused", ArtistCount = 0, TrackFileCount = 0, SizeOnDisk = 0L });
        }

        [Test]
        public void should_count_artists_per_metadata_profile()
        {
            var standard = GivenMetadataProfile("Standard");
            var minimal = GivenMetadataProfile("Minimal");
            GivenArtist(metadataProfileId: standard.Id);
            GivenArtist(metadataProfileId: standard.Id);
            GivenArtist(metadataProfileId: minimal.Id);

            var stats = Subject.GetLibraryStatistics().MetadataProfileStatistics;

            stats.Single(p => p.MetadataProfileId == standard.Id).Should().BeEquivalentTo(new { Name = "Standard", ArtistCount = 2 });
            stats.Single(p => p.MetadataProfileId == minimal.Id).Should().BeEquivalentTo(new { Name = "Minimal", ArtistCount = 1 });
        }

        [Test]
        public void should_count_artists_and_files_per_tag()
        {
            var rock = GivenTag("rock");
            var jazz = GivenTag("jazz");
            var both = GivenArtist(tags: new HashSet<int> { rock.Id, jazz.Id });
            GivenArtist(tags: new HashSet<int> { rock.Id });
            var untagged = GivenArtist();
            GivenTrackFile(GivenAlbum(both, _released).Id, size: 300);
            GivenTrackFile(GivenAlbum(untagged, _released).Id, size: 50);

            using (var conn = Db.OpenConnection())
            {
                conn.Execute(@"UPDATE ""Artists"" SET ""Tags"" = NULL WHERE ""Id"" = @id", new { id = untagged.Id });
            }

            var stats = Subject.GetLibraryStatistics().TagStatistics;

            stats.Select(t => t.Label).Should().Equal("jazz", "rock");
            stats.Single(t => t.TagId == rock.Id).Should().BeEquivalentTo(new { ArtistCount = 2, TrackFileCount = 1, SizeOnDisk = 300L });
            stats.Single(t => t.TagId == jazz.Id).Should().BeEquivalentTo(new { ArtistCount = 1, TrackFileCount = 1, SizeOnDisk = 300L });
        }

        [Test]
        public void should_scope_profile_and_tag_counts_to_the_filter_and_keep_empty_ones()
        {
            var kept = GivenQualityProfile("Kept");
            var emptied = GivenQualityProfile("Emptied");
            var keptMetadata = GivenMetadataProfile("Kept");
            var emptiedMetadata = GivenMetadataProfile("Emptied");
            var keptTag = GivenTag("kept");
            var emptiedTag = GivenTag("emptied");

            var inside = GivenArtist(qualityProfileId: kept.Id, metadataProfileId: keptMetadata.Id, path: @"C:\music\Inside".AsOsAgnostic(), tags: new HashSet<int> { keptTag.Id });
            var outside = GivenArtist(qualityProfileId: emptied.Id, metadataProfileId: emptiedMetadata.Id, path: @"C:\other\Outside".AsOsAgnostic(), tags: new HashSet<int> { emptiedTag.Id });
            GivenTrackFile(GivenAlbum(inside, _released).Id);
            GivenTrackFile(GivenAlbum(outside, _released).Id);

            var stats = Subject.GetLibraryStatistics(new StatisticsFilter { RootFolderPaths = new List<string> { @"C:\music".AsOsAgnostic() } });

            stats.QualityProfileStatistics.Single(p => p.QualityProfileId == kept.Id).Should().BeEquivalentTo(new { ArtistCount = 1, TrackFileCount = 1, SizeOnDisk = 100L });
            stats.QualityProfileStatistics.Single(p => p.QualityProfileId == emptied.Id).Should().BeEquivalentTo(new { ArtistCount = 0, TrackFileCount = 0, SizeOnDisk = 0L });
            stats.MetadataProfileStatistics.Single(p => p.MetadataProfileId == keptMetadata.Id).ArtistCount.Should().Be(1);
            stats.MetadataProfileStatistics.Single(p => p.MetadataProfileId == emptiedMetadata.Id).ArtistCount.Should().Be(0);
            stats.TagStatistics.Single(t => t.TagId == keptTag.Id).Should().BeEquivalentTo(new { ArtistCount = 1, TrackFileCount = 1, SizeOnDisk = 100L });
            stats.TagStatistics.Single(t => t.TagId == emptiedTag.Id).Should().BeEquivalentTo(new { ArtistCount = 0, TrackFileCount = 0, SizeOnDisk = 0L });
        }

        [Test]
        public void should_count_files_per_quality_in_weight_order_without_unmapped_files()
        {
            var album = GivenAlbum(GivenArtist(), _released);
            GivenTrackFile(album.Id, size: 100, quality: Quality.FLAC);
            GivenTrackFile(album.Id, size: 200, quality: Quality.FLAC);
            GivenTrackFile(album.Id, size: 50, quality: Quality.MP3_320);
            GivenTrackFile(album.Id, size: 25, quality: Quality.MP3_160);
            GivenTrackFile(0, size: 999, quality: Quality.MP3_320);

            var expected = new[]
            {
                new { Quality = Quality.MP3_160, TrackFileCount = 1, SizeOnDisk = 25L },
                new { Quality = Quality.MP3_320, TrackFileCount = 1, SizeOnDisk = 50L },
                new { Quality = Quality.FLAC, TrackFileCount = 2, SizeOnDisk = 300L }
            };

            Subject.GetLibraryStatistics().QualityStatistics.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [Test]
        public void should_count_files_whose_quality_lidarr_cannot_read_as_unknown()
        {
            var album = GivenAlbum(GivenArtist(), _released);
            var unknownId = GivenTrackFile(album.Id, size: 100);
            var noQuality = GivenTrackFile(album.Id, size: 25);
            GivenTrackFile(album.Id, size: 50, quality: Quality.Unknown);

            using (var conn = Db.OpenConnection())
            {
                conn.Execute(@"UPDATE ""TrackFiles"" SET ""Quality"" = '{""quality"": 999, ""revision"": {""version"": 1, ""real"": 0, ""isRepack"": false}}' WHERE ""Id"" = @id", new { id = unknownId.Id });
                conn.Execute(@"UPDATE ""TrackFiles"" SET ""Quality"" = '{}' WHERE ""Id"" = @id", new { id = noQuality.Id });
            }

            Subject.GetLibraryStatistics().QualityStatistics.Should().BeEquivalentTo(new[]
            {
                new { Quality = Quality.Unknown, TrackFileCount = 3, SizeOnDisk = 175L }
            });
        }

        [Test]
        public void should_count_albums_per_album_type()
        {
            var artist = GivenArtist();
            GivenAlbum(artist, _released, albumType: "EP");
            GivenAlbum(artist, _released, albumType: "Single");
            GivenAlbum(artist, _released, albumType: "Album");
            GivenAlbum(artist, _released, albumType: "Single");

            var expected = new[]
            {
                new { AlbumType = "Single", AlbumCount = 2 },
                new { AlbumType = "Album", AlbumCount = 1 },
                new { AlbumType = "EP", AlbumCount = 1 }
            };

            Subject.GetLibraryStatistics().AlbumTypeStatistics.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [Test]
        public void should_count_albums_once_per_secondary_type_with_none_as_studio()
        {
            var artist = GivenArtist();
            GivenAlbum(artist, _released, secondaryTypes: new List<SecondaryAlbumType> { SecondaryAlbumType.Compilation, SecondaryAlbumType.Live });
            GivenAlbum(artist, _released, secondaryTypes: new List<SecondaryAlbumType> { SecondaryAlbumType.Live, SecondaryAlbumType.Live });
            GivenAlbum(artist, _released);

            var expected = new[]
            {
                new { SecondaryType = "Live", AlbumCount = 2 },
                new { SecondaryType = "Compilation", AlbumCount = 1 },
                new { SecondaryType = "Studio", AlbumCount = 1 }
            };

            Subject.GetLibraryStatistics().SecondaryTypeStatistics.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [Test]
        public void should_count_albums_files_and_size_per_decade_with_undated_last()
        {
            var artist = GivenArtist();
            GivenAlbumWithTracks(artist, new DateTime(1995, 6, 1, 0, 0, 0, DateTimeKind.Utc), trackCount: 2, fileCount: 2);
            GivenAlbum(artist, new DateTime(1999, 12, 31, 0, 0, 0, DateTimeKind.Utc));
            GivenAlbumWithTracks(artist, new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc), trackCount: 1, fileCount: 1);
            GivenAlbum(artist, null);
            GivenAlbum(artist, new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            var expected = new[]
            {
                new { Decade = (int?)1990, AlbumCount = 2, TrackFileCount = 2, SizeOnDisk = 200L },
                new { Decade = (int?)2000, AlbumCount = 1, TrackFileCount = 1, SizeOnDisk = 100L },
                new { Decade = (int?)null, AlbumCount = 2, TrackFileCount = 0, SizeOnDisk = 0L }
            };

            Subject.GetLibraryStatistics().DecadeStatistics.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [Test]
        public void should_count_albums_once_per_genre_with_files_and_size_most_albums_first_without_genreless_albums()
        {
            var artist = GivenArtist();
            GivenAlbumWithTracks(artist, _released, trackCount: 2, fileCount: 2, genres: new List<string> { "Rock", "Pop", "Rock" });
            GivenAlbumWithTracks(artist, _released, trackCount: 1, fileCount: 1, genres: new List<string> { "Rock" });
            GivenAlbumWithTracks(GivenArtist(), _released, trackCount: 1, fileCount: 1, genres: new List<string> { "jazz" });
            var nullGenres = GivenAlbumWithTracks(artist, _released, trackCount: 1, fileCount: 1);
            GivenAlbumWithTracks(artist, _released, trackCount: 1, fileCount: 1);

            using (var conn = Db.OpenConnection())
            {
                conn.Execute(@"UPDATE ""Albums"" SET ""Genres"" = NULL WHERE ""Id"" = @id", new { id = nullGenres.Id });
            }

            var expected = new[]
            {
                new { Genre = "Rock", AlbumCount = 2, TrackFileCount = 3, SizeOnDisk = 300L },
                new { Genre = "jazz", AlbumCount = 1, TrackFileCount = 1, SizeOnDisk = 100L },
                new { Genre = "Pop", AlbumCount = 1, TrackFileCount = 2, SizeOnDisk = 200L }
            };

            Subject.GetLibraryStatistics().GenreStatistics.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [Test]
        public void should_list_the_ten_largest_artists_with_ties_in_name_order()
        {
            for (var i = 1; i <= 10; i++)
            {
                GivenTrackFile(GivenAlbum(GivenArtist(name: $"Size {i:D2}"), _released).Id, size: i * 100);
            }

            var beta = GivenArtist(name: "Beta");
            GivenTrackFile(GivenAlbum(beta, _released).Id, size: 1000);
            GivenTrackFile(GivenAlbum(beta, _released).Id, size: 500);
            GivenTrackFile(GivenAlbum(GivenArtist(name: "alpha"), _released).Id, size: 1500);

            var stats = Subject.GetLibraryStatistics().LargestArtistStatistics;

            stats.Select(a => a.ArtistName).Should().Equal("alpha", "Beta", "Size 10", "Size 09", "Size 08", "Size 07", "Size 06", "Size 05", "Size 04", "Size 03");
            stats[1].Should().BeEquivalentTo(new { ArtistId = beta.Id, TrackFileCount = 2, SizeOnDisk = 1500L });
        }

        [Test]
        public void should_leave_artists_without_track_files_out_of_the_largest_artists()
        {
            var withFile = GivenArtist();
            GivenTrackFile(GivenAlbum(withFile, _released).Id);
            GivenAlbum(GivenArtist(), _released);
            GivenArtist();

            Subject.GetLibraryStatistics().LargestArtistStatistics.Select(a => a.ArtistId).Should().Equal(withFile.Id);
        }

        [Test]
        public void should_count_albums_once_per_format_of_their_monitored_release_with_no_format_as_null()
        {
            var artist = GivenArtist();
            var twoDiscs = GivenAlbum(artist, _released);
            GivenRelease(twoDiscs, true, "CD", "CD");
            GivenRelease(GivenAlbum(artist, _released), true, "CD", "DVD-Video");
            GivenRelease(GivenAlbum(artist, _released), true, "CD");
            GivenRelease(GivenAlbum(artist, _released), true, "Digital Media");
            GivenRelease(GivenAlbum(artist, _released), true, null, " ", "Unknown");
            var unmonitoredVinyl = GivenAlbum(artist, _released);
            GivenRelease(unmonitoredVinyl, false, "Vinyl");
            GivenRelease(unmonitoredVinyl, true);
            GivenAlbum(artist, _released);

            var expected = new[]
            {
                new { MediaFormat = "CD", AlbumCount = 3 },
                new { MediaFormat = (string)null, AlbumCount = 3 },
                new { MediaFormat = "Digital Media", AlbumCount = 1 },
                new { MediaFormat = "DVD-Video", AlbumCount = 1 }
            };

            Subject.GetLibraryStatistics().MediaFormatStatistics.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        [Test]
        public void should_count_a_release_with_no_media_column_as_no_format()
        {
            var album = GivenAlbum(GivenArtist(), _released);
            Db.Insert(Builder<AlbumRelease>.CreateNew()
                                           .With(r => r.AlbumId = album.Id)
                                           .With(r => r.ForeignReleaseId = "release-null-media")
                                           .With(r => r.Monitored = true)
                                           .With(r => r.Media = null)
                                           .BuildNew());

            Subject.GetLibraryStatistics().MediaFormatStatistics.Should().BeEquivalentTo(new[] { new { MediaFormat = (string)null, AlbumCount = 1 } });
        }

        [Test]
        public void should_scope_quality_and_album_breakdowns_to_the_filter()
        {
            var tag = GivenTag("keep");
            var kept = GivenArtist(tags: new HashSet<int> { tag.Id });
            var other = GivenArtist();
            var keptAlbum = GivenAlbum(kept, new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc), albumType: "EP", secondaryTypes: new List<SecondaryAlbumType> { SecondaryAlbumType.Live }, genres: new List<string> { "Synthpop" });
            var otherAlbum = GivenAlbum(other, new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc), albumType: "Single", genres: new List<string> { "Trap" });
            GivenRelease(keptAlbum, true, "Vinyl");
            GivenRelease(otherAlbum, true, "CD");
            GivenTrackFile(keptAlbum.Id, quality: Quality.FLAC);
            GivenTrackFile(otherAlbum.Id, quality: Quality.MP3_320);

            var stats = Subject.GetLibraryStatistics(new StatisticsFilter { TagIds = new List<int> { tag.Id } });

            stats.QualityStatistics.Select(q => q.Quality).Should().Equal(Quality.FLAC);
            stats.AlbumTypeStatistics.Select(a => a.AlbumType).Should().Equal("EP");
            stats.SecondaryTypeStatistics.Select(s => s.SecondaryType).Should().Equal("Live");
            stats.DecadeStatistics.Select(d => d.Decade).Should().Equal(1980);
            stats.GenreStatistics.Select(g => g.Genre).Should().Equal("Synthpop");
            stats.LargestArtistStatistics.Select(a => a.ArtistId).Should().Equal(kept.Id);
            stats.MediaFormatStatistics.Select(m => m.MediaFormat).Should().Equal("Vinyl");
        }
    }
}

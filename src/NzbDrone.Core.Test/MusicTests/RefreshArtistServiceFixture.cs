using System.Collections.Generic;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.AutoTagging;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.History;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Music;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Music.Events;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class RefreshArtistServiceFixture : CoreTest<RefreshArtistService>
    {
        private Artist _artist;
        private Album _album1;
        private Album _album2;
        private List<Album> _albums;
        private List<Album> _remoteAlbums;

        [SetUp]
        public void Setup()
        {
            _album1 = Builder<Album>.CreateNew()
                .With(s => s.ForeignAlbumId = "1")
                .Build();

            _album2 = Builder<Album>.CreateNew()
                .With(s => s.ForeignAlbumId = "2")
                .Build();

            _albums = new List<Album> { _album1, _album2 };

            _remoteAlbums = _albums.JsonClone();
            _remoteAlbums.ForEach(x => x.Id = 0);

            var metadata = Builder<ArtistMetadata>.CreateNew()
                .With(m => m.Status = ArtistStatusType.Continuing)
                .Build();

            _artist = Builder<Artist>.CreateNew()
                .With(a => a.Metadata = metadata)
                .Build();

            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                  .Setup(s => s.GetArtists(new List<int> { _artist.Id }))
                  .Returns(new List<Artist> { _artist });

            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                  .Setup(s => s.GetArtist(It.IsAny<int>()))
                  .Returns(_artist);

            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                  .Setup(s => s.FindById(It.IsAny<string>()))
                  .Returns(default(Artist));

            Mocker.GetMock<IAlbumService>(MockBehavior.Strict)
                .Setup(s => s.InsertMany(It.IsAny<List<Album>>()));

            Mocker.GetMock<IProvideArtistInfo>()
                  .Setup(s => s.GetArtistInfo(It.IsAny<string>(), It.IsAny<int>()))
                  .Callback(() => { throw new ArtistNotFoundException(_artist.ForeignArtistId); });

            Mocker.GetMock<IMediaFileService>()
                .Setup(x => x.GetFilesByArtist(It.IsAny<int>()))
                .Returns(new List<TrackFile>());

            Mocker.GetMock<IHistoryService>()
                .Setup(x => x.GetByArtist(It.IsAny<int>(), It.IsAny<EntityHistoryEventType?>()))
                .Returns(new List<EntityHistory>());

            Mocker.GetMock<IImportListExclusionService>()
                .Setup(x => x.FindByForeignId(It.IsAny<List<string>>()))
                .Returns(new List<ImportListExclusion>());

            Mocker.GetMock<IRootFolderService>()
                .Setup(x => x.All())
                .Returns(new List<RootFolder>());

            Mocker.GetMock<IMonitorNewAlbumService>()
                .Setup(x => x.ShouldMonitorNewAlbum(It.IsAny<Album>(), It.IsAny<List<Album>>(), It.IsAny<NewItemMonitorTypes>()))
                .Returns(true);

            Mocker.GetMock<IAutoTaggingService>()
                .Setup(s => s.GetTagChanges(_artist))
                .Returns(new AutoTaggingChanges());
        }

        private void GivenNewArtistInfo(Artist artist)
        {
            Mocker.GetMock<IProvideArtistInfo>()
                  .Setup(s => s.GetArtistInfo(_artist.ForeignArtistId, _artist.MetadataProfileId))
                  .Returns(artist);
        }

        private void GivenArtistFiles()
        {
            Mocker.GetMock<IMediaFileService>()
                  .Setup(x => x.GetFilesByArtist(It.IsAny<int>()))
                  .Returns(Builder<TrackFile>.CreateListOfSize(1).BuildList());
        }

        private void GivenAlbumsForRefresh(List<Album> albums)
        {
            Mocker.GetMock<IAlbumService>(MockBehavior.Strict)
                .Setup(s => s.GetAlbumsForRefresh(It.IsAny<int>(), It.IsAny<List<string>>()))
                .Returns(albums);
        }

        private void AllowArtistUpdate()
        {
            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                .Setup(x => x.UpdateArtist(It.IsAny<Artist>(), true))
                .Returns((Artist a, bool publishEvent) => a);

            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                .Setup(x => x.UpdateArtist(It.IsAny<Artist>(), false))
                .Returns((Artist a, bool publishEvent) => a);
        }

        [Test]
        public void should_not_publish_artist_updated_event_if_metadata_not_updated()
        {
            var newArtistInfo = _artist.JsonClone();
            newArtistInfo.Metadata = _artist.Metadata.Value.JsonClone();
            newArtistInfo.Albums = _remoteAlbums;

            GivenNewArtistInfo(newArtistInfo);
            GivenAlbumsForRefresh(_albums);
            AllowArtistUpdate();

            Subject.Execute(new RefreshArtistCommand(new List<int> { _artist.Id }));

            VerifyEventNotPublished<ArtistUpdatedEvent>();
            VerifyEventPublished<ArtistRefreshCompleteEvent>();
        }

        [Test]
        public void should_publish_artist_updated_event_if_metadata_updated()
        {
            var newArtistInfo = _artist.JsonClone();
            newArtistInfo.Metadata = _artist.Metadata.Value.JsonClone();
            newArtistInfo.Metadata.Value.Images = new List<MediaCover.MediaCover>
            {
                new MediaCover.MediaCover(MediaCover.MediaCoverTypes.Logo, "dummy")
            };
            newArtistInfo.Albums = _remoteAlbums;

            GivenNewArtistInfo(newArtistInfo);
            GivenAlbumsForRefresh(new List<Album>());
            AllowArtistUpdate();

            Subject.Execute(new RefreshArtistCommand(new List<int> { _artist.Id }));

            VerifyEventPublished<ArtistUpdatedEvent>();
            VerifyEventPublished<ArtistRefreshCompleteEvent>();
        }

        [Test]
        public void should_call_new_album_monitor_service_when_adding_album()
        {
            var newAlbum = Builder<Album>.CreateNew()
                .With(x => x.Id = 0)
                .With(x => x.ForeignAlbumId = "3")
                .Build();
            _remoteAlbums.Add(newAlbum);

            var newAuthorInfo = _artist.JsonClone();
            newAuthorInfo.Metadata = _artist.Metadata.Value.JsonClone();
            newAuthorInfo.Albums = _remoteAlbums;

            GivenNewArtistInfo(newAuthorInfo);
            GivenAlbumsForRefresh(_albums);
            AllowArtistUpdate();

            Subject.Execute(new RefreshArtistCommand(new List<int> { _artist.Id }));

            Mocker.GetMock<IMonitorNewAlbumService>()
                .Verify(x => x.ShouldMonitorNewAlbum(newAlbum, _albums, _artist.MonitorNewItems), Times.Once());
        }

        [Test]
        public void should_log_error_and_delete_if_musicbrainz_id_not_found_and_author_has_no_files()
        {
            GivenAlbumsForRefresh(new List<Album>());
            AllowArtistUpdate();

            Mocker.GetMock<IArtistService>()
                .Setup(x => x.DeleteArtist(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()));

            Subject.Execute(new RefreshArtistCommand(new List<int> { _artist.Id }));

            Mocker.GetMock<IArtistService>()
                .Verify(v => v.UpdateArtist(It.Is<Artist>(s => s.Metadata.Value.Status == ArtistStatusType.Deleted), true), Times.Once());

            Mocker.GetMock<IArtistService>()
                .Verify(v => v.DeleteArtist(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_log_error_but_not_delete_if_musicbrainz_id_not_found_and_artist_has_files()
        {
            GivenArtistFiles();
            GivenAlbumsForRefresh(new List<Album>());
            AllowArtistUpdate();

            Subject.Execute(new RefreshArtistCommand(new List<int> { _artist.Id }));

            Mocker.GetMock<IArtistService>()
                .Verify(v => v.UpdateArtist(It.Is<Artist>(s => s.Metadata.Value.Status == ArtistStatusType.Deleted), true), Times.Once());

            Mocker.GetMock<IArtistService>()
                .Verify(v => v.DeleteArtist(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never());

            ExceptionVerification.ExpectedErrors(2);
        }

        [Test]
        public void should_log_error_if_musicbrainz_id_not_found()
        {
            GivenAlbumsForRefresh(new List<Album>());
            AllowArtistUpdate();

            Mocker.GetMock<IArtistService>()
                .Setup(x => x.DeleteArtist(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()));

            Subject.Execute(new RefreshArtistCommand(new List<int> { _artist.Id }));

            Mocker.GetMock<IArtistService>()
                .Verify(v => v.UpdateArtist(It.Is<Artist>(s => s.Metadata.Value.Status == ArtistStatusType.Deleted), It.IsAny<bool>()), Times.Once());

            ExceptionVerification.ExpectedErrors(1);
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_not_remark_as_deleted_if_musicbrainz_id_not_found()
        {
            _artist.Metadata.Value.Status = ArtistStatusType.Deleted;
            GivenAlbumsForRefresh(new List<Album>());

            Mocker.GetMock<IArtistService>()
                .Setup(x => x.DeleteArtist(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()));

            Subject.Execute(new RefreshArtistCommand(new List<int> { _artist.Id }));

            Mocker.GetMock<IArtistService>()
                .Verify(v => v.UpdateArtist(It.IsAny<Artist>(), It.IsAny<bool>()), Times.Never());

            ExceptionVerification.ExpectedErrors(1);
            ExceptionVerification.ExpectedWarns(1);
        }

        [Test]
        public void should_update_if_musicbrainz_id_changed_and_no_clash()
        {
            var newArtistInfo = _artist.JsonClone();
            newArtistInfo.Metadata = _artist.Metadata.Value.JsonClone();
            newArtistInfo.Albums = _remoteAlbums;
            newArtistInfo.ForeignArtistId = _artist.ForeignArtistId + 1;
            newArtistInfo.Metadata.Value.Id = 100;

            GivenNewArtistInfo(newArtistInfo);

            var seq = new MockSequence();

            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                .Setup(x => x.FindById(newArtistInfo.ForeignArtistId))
                .Returns(default(Artist));

            // Make sure that the artist is updated before we refresh the albums
            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateArtist(It.IsAny<Artist>(), It.IsAny<bool>()))
                .Returns((Artist a, bool updated) => a);

            Mocker.GetMock<IAlbumService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.GetAlbumsForRefresh(It.IsAny<int>(), It.IsAny<List<string>>()))
                .Returns(new List<Album>());

            // Update called twice for a move/merge
            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateArtist(It.IsAny<Artist>(), It.IsAny<bool>()))
                .Returns((Artist a, bool updated) => a);

            Subject.Execute(new RefreshArtistCommand(new List<int> { _artist.Id }));

            Mocker.GetMock<IArtistService>()
                .Verify(v => v.UpdateArtist(It.Is<Artist>(s => s.ArtistMetadataId == 100 && s.ForeignArtistId == newArtistInfo.ForeignArtistId), It.IsAny<bool>()),
                        Times.Exactly(2));
        }

        [Test]
        public void should_merge_if_musicbrainz_id_changed_and_new_id_already_exists()
        {
            var existing = _artist;

            var clash = _artist.JsonClone();
            clash.Id = 100;
            clash.Metadata = existing.Metadata.Value.JsonClone();
            clash.Metadata.Value.Id = 101;
            clash.Metadata.Value.ForeignArtistId = clash.Metadata.Value.ForeignArtistId + 1;

            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                .Setup(x => x.FindById(clash.Metadata.Value.ForeignArtistId))
                .Returns(clash);

            var newArtistInfo = clash.JsonClone();
            newArtistInfo.Metadata = clash.Metadata.Value.JsonClone();
            newArtistInfo.Albums = _remoteAlbums;

            GivenNewArtistInfo(newArtistInfo);

            var seq = new MockSequence();

            // Make sure that the artist is updated before we refresh the albums
            Mocker.GetMock<IAlbumService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.GetAlbumsByArtist(existing.Id))
                .Returns(_albums);

            Mocker.GetMock<IAlbumService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateMany(It.IsAny<List<Album>>()));

            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.DeleteArtist(existing.Id, It.IsAny<bool>(), false));

            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateArtist(It.Is<Artist>(a => a.Id == clash.Id), It.IsAny<bool>()))
                .Returns((Artist a, bool updated) => a);

            Mocker.GetMock<IAlbumService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.GetAlbumsForRefresh(clash.ArtistMetadataId, It.IsAny<List<string>>()))
                .Returns(_albums);

            // Update called twice for a move/merge
            Mocker.GetMock<IArtistService>(MockBehavior.Strict)
                .InSequence(seq)
                .Setup(x => x.UpdateArtist(It.IsAny<Artist>(), It.IsAny<bool>()))
                .Returns((Artist a, bool updated) => a);

            Subject.Execute(new RefreshArtistCommand(new List<int> { _artist.Id }));

            // the retained artist gets updated
            Mocker.GetMock<IArtistService>()
                .Verify(v => v.UpdateArtist(It.Is<Artist>(s => s.Id == clash.Id), It.IsAny<bool>()), Times.Exactly(2));

            // the old one gets removed
            Mocker.GetMock<IArtistService>()
                .Verify(v => v.DeleteArtist(existing.Id, false, false));

            Mocker.GetMock<IAlbumService>()
                .Verify(v => v.UpdateMany(It.Is<List<Album>>(x => x.Count == _albums.Count)));

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}

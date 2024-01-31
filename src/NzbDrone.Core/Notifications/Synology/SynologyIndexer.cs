using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Notifications.Synology
{
    public class SynologyIndexer : NotificationBase<SynologyIndexerSettings>
    {
        private readonly ISynologyIndexerProxy _indexerProxy;

        public SynologyIndexer(ISynologyIndexerProxy indexerProxy)
        {
            _indexerProxy = indexerProxy;
        }

        public override string Link => "https://www.synology.com";
        public override string Name => "Synology Indexer";

        public override void OnReleaseImport(AlbumDownloadMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                foreach (var oldFile in message.OldFiles)
                {
                    var fullPath = oldFile.Path;

                    _indexerProxy.DeleteFile(fullPath);
                }

                foreach (var newFile in message.TrackFiles)
                {
                    var fullPath = newFile.Path;

                    _indexerProxy.AddFile(fullPath);
                }
            }
        }

        public override void OnRename(Artist artist, List<RenamedTrackFile> renamedFiles)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.UpdateFolder(artist.Path);
            }
        }

        public override void OnTrackRetag(TrackRetagMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.UpdateFolder(message.Artist.Path);
            }
        }

        public override void OnArtistAdd(ArtistAddMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.UpdateFolder(message.Artist.Path);
            }
        }

        public override void OnArtistDelete(ArtistDeleteMessage deleteMessage)
        {
            if (deleteMessage.DeletedFiles)
            {
                if (Settings.UpdateLibrary)
                {
                    _indexerProxy.DeleteFolder(deleteMessage.Artist.Path);
                }
            }
        }

        public override void OnAlbumDelete(AlbumDeleteMessage deleteMessage)
        {
            if (deleteMessage.DeletedFiles)
            {
                if (Settings.UpdateLibrary)
                {
                    _indexerProxy.DeleteFolder(deleteMessage.Album.Artist.Value.Path);
                }
            }
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(TestConnection());

            return new ValidationResult(failures);
        }

        protected virtual ValidationFailure TestConnection()
        {
            if (!OsInfo.IsLinux)
            {
                return new ValidationFailure(null, "Must be a Synology");
            }

            if (!_indexerProxy.Test())
            {
                return new ValidationFailure(null, "Not a Synology or synoindex not available");
            }

            return null;
        }
    }
}

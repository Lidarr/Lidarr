using System.IO;
using Equ;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.MediaCover
{
    public enum MediaCoverTypes
    {
        Unknown = 0,
        Poster = 1,
        Banner = 2,
        Fanart = 3,
        Screenshot = 4,
        Headshot = 5,
        Cover = 6,
        Disc = 7,
        Logo = 8,
        Clearlogo = 9
    }

    public enum MediaCoverEntity
    {
        Artist = 0,
        Album = 1
    }

    public class MediaCover : MemberwiseEquatable<MediaCover>, IEmbeddedDocument
    {
        private string _remoteUrl;

        public MediaCoverTypes CoverType { get; set; }
        public string Url { get; set; }
        public string RemoteUrl
        {
            get => _remoteUrl;
            set
            {
                _remoteUrl = value;

                if (Extension.IsNullOrWhiteSpace())
                {
                    Extension = Path.GetExtension(value);
                }
            }
        }

        public string Extension { get; private set; }

        public MediaCover()
        {
        }

        public MediaCover(MediaCoverTypes coverType, string remoteUrl)
        {
            CoverType = coverType;
            RemoteUrl = remoteUrl;
        }
    }
}

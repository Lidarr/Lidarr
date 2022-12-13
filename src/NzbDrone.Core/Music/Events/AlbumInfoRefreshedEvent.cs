using System.Collections.Generic;
using System.Collections.ObjectModel;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Music.Events
{
    public class AlbumInfoRefreshedEvent : IEvent
    {
        public Artist Artist { get; set; }
        public ReadOnlyCollection<Album> Added { get; private set; }
        public ReadOnlyCollection<Album> Updated { get; private set; }
        public ReadOnlyCollection<Album> Removed { get; private set; }

        public AlbumInfoRefreshedEvent(Artist artist, IList<Album> added, IList<Album> updated, IList<Album> removed)
        {
            Artist = artist;
            Added = new ReadOnlyCollection<Album>(added);
            Removed = new ReadOnlyCollection<Album>(removed);
        }
    }
}

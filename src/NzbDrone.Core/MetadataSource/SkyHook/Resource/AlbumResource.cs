using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.MetadataSource.SkyHook.Resource
{
    public class AlbumResource
    {
        public AlbumResource()
        {
            Media = new List<MediumResource>();
            Releases = new List<ReleaseResource>();
        }

        public List<ArtistResource> Artists { get; set; } // Will always be length of 1 unless a compilation
        public string Url { get; set; } // Link to the endpoint api to give full info for this object
        public string Id { get; set; } // This is a unique Album ID. Needed for all future API calls
        public DateTime ReleaseDate { get; set; }
        public List<ImageResource> Images { get; set; }
        public string Title { get; set; }
        public string Disambiguation { get; set; }
        public string Overview { get; set; }
        public List<string> Genres { get; set; }
        public List<string> Labels { get; set; }
        public string Type { get; set; }
        public List<string> SecondaryTypes { get; set; }
        public List<MediumResource> Media { get; set; }
        public List<TrackResource> Tracks { get; set; }
        public List<ReleaseResource> Releases { get; set; }
        public RatingResource Rating { get; set; }
        public string SelectedRelease { get; set; }
        public AlbumArtistResource Artist { get; set; }
    }

    
}

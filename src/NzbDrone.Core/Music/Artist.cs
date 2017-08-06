﻿using Marr.Data;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Profiles;
using NzbDrone.Core.Music;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.Music
{
    public class Artist : ModelBase
    {
        public Artist()
        {
            Images = new List<MediaCover.MediaCover>();
            Genres = new List<string>();
            Members = new List<Member>();
            Albums = new List<Album>();
            Tags = new HashSet<int>();

        }

        public string ForeignArtistId { get; set; }
        public string MBId { get; set; }
        public int TADBId { get; set; }
        public int DiscogsId { get; set; }
        public string AMId { get; set; }
        public string Name { get; set; }
        public string NameSlug { get; set; }
        public string CleanName { get; set; }
        public string SortName { get; set; }
        public string Overview { get; set; }
        public bool Monitored { get; set; }
        public bool AlbumFolder { get; set; }
        public DateTime? LastInfoSync { get; set; }
        public DateTime? LastDiskSync { get; set; }
        public int Status { get; set; } // TODO: Figure out what this is, do we need it? 
        public string Path { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public List<string> Genres { get; set; }
        public string RootFolderPath { get; set; }
        public DateTime Added { get; set; }
        public LazyLoaded<Profile> Profile { get; set; }
        public int ProfileId { get; set; }
        public List<Album> Albums { get; set; }
        public HashSet<int> Tags { get; set; }
        public AddArtistOptions AddOptions { get; set; }
        public Ratings Ratings { get; set; }
        public List<Member> Members { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}][{1}]", ForeignArtistId, Name.NullSafe());
        }

        public void ApplyChanges(Artist otherArtist)
        {

            ForeignArtistId = otherArtist.ForeignArtistId;
           
            Path = otherArtist.Path;

            Profile = otherArtist.Profile;

            Albums = otherArtist.Albums;

            ProfileId = otherArtist.ProfileId;
            Tags = otherArtist.Tags;
            AddOptions = otherArtist.AddOptions;
            RootFolderPath = otherArtist.RootFolderPath;
            Monitored = otherArtist.Monitored;
            AlbumFolder = otherArtist.AlbumFolder;

        }
    }
}

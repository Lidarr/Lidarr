namespace NzbDrone.Core.Download.Clients.Deemix
{
    public class DeemixTags
    {
        public bool Title { get; set; }
        public bool Artist { get; set; }
        public bool Album { get; set; }
        public bool Cover { get; set; }
        public bool TrackNumber { get; set; }
        public bool TrackTotal { get; set; }
        public bool DiscNumber { get; set; }
        public bool DiscTotal { get; set; }
        public bool AlbumArtist { get; set; }
        public bool Genre { get; set; }
        public bool Year { get; set; }
        public bool Date { get; set; }
        public bool Explicit { get; set; }
        public bool Isrc { get; set; }
        public bool Length { get; set; }
        public bool Barcode { get; set; }
        public bool Bpm { get; set; }
        public bool ReplayGain { get; set; }
        public bool Label { get; set; }
        public bool Lyrics { get; set; }
        public bool Copyright { get; set; }
        public bool Composer { get; set; }
        public bool InvolvedPeople { get; set; }
        public bool SavePlaylistAsCompilation { get; set; }
        public bool UseNullSeparator { get; set; }
        public bool SaveID3v1 { get; set; }
        public string MultiArtistSeparator { get; set; }
        public bool SingleAlbumArtist { get; set; }
    }

    public class DeemixConfig
    {
        public string DownloadLocation { get; set; }
        public string TracknameTemplate { get; set; }
        public string AlbumTracknameTemplate { get; set; }
        public string PlaylistTracknameTemplate { get; set; }
        public bool CreatePlaylistFolder { get; set; }
        public string PlaylistNameTemplate { get; set; }
        public bool CreateArtistFolder { get; set; }
        public string ArtistNameTemplate { get; set; }
        public bool CreateAlbumFolder { get; set; }
        public string AlbumNameTemplate { get; set; }
        public bool CreateCDFolder { get; set; }
        public bool CreateStructurePlaylist { get; set; }
        public bool CreateSingleFolder { get; set; }
        public bool PadTracks { get; set; }
        public string PaddingSize { get; set; }
        public string IllegalCharacterReplacer { get; set; }
        public int QueueConcurrency { get; set; }
        public string MaxBitrate { get; set; }
        public bool FallbackBitrate { get; set; }
        public bool FallbackSearch { get; set; }
        public bool LogErrors { get; set; }
        public bool LogSearched { get; set; }
        public bool SaveDownloadQueue { get; set; }
        public string OverwriteFile { get; set; }
        public bool CreateM3U8File { get; set; }
        public string PlaylistFilenameTemplate { get; set; }
        public bool SyncedLyrics { get; set; }
        public int EmbeddedArtworkSize { get; set; }
        public bool EmbeddedArtworkPNG { get; set; }
        public int LocalArtworkSize { get; set; }
        public string LocalArtworkFormat { get; set; }
        public bool SaveArtwork { get; set; }
        public string CoverImageTemplate { get; set; }
        public bool SaveArtworkArtist { get; set; }
        public string ArtistImageTemplate { get; set; }
        public int JpegImageQuality { get; set; }
        public string DateFormat { get; set; }
        public bool AlbumVariousArtists { get; set; }
        public bool RemoveAlbumVersion { get; set; }
        public bool RemoveDuplicateArtists { get; set; }
        public string FeaturedToTitle { get; set; }
        public string TitleCasing { get; set; }
        public string ArtistCasing { get; set; }
        public string ExecuteCommand { get; set; }
        public DeemixTags Tags { get; set; }
    }
}

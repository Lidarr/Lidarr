namespace NzbDrone.Core.MediaFiles
{
    public class RenamedTrackFile
    {
        public TrackFile TrackFile { get; set; }
        public string PreviousPath { get; set; }
    }
}

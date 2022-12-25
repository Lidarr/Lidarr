using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Notifications
{
    public class ArtistDeleteMessage
    {
        public string Message { get; set; }
        public Artist Artist { get; set; }
        public bool DeletedFiles { get; set; }
        public string DeletedFilesMessage { get; set; }

        public override string ToString()
        {
            return Message;
        }

        public ArtistDeleteMessage(Artist artist, bool deleteFiles)
        {
            Artist = artist;
            DeletedFiles = deleteFiles;
            DeletedFilesMessage = DeletedFiles ?
                "Artist removed and all files were deleted" :
                "Artist removed, files were not deleted";
            Message = artist.Metadata.Value.Name + " - " + DeletedFilesMessage;
        }
    }
}

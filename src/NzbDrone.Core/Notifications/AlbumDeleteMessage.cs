using NzbDrone.Core.Music;

namespace NzbDrone.Core.Notifications
{
    public class AlbumDeleteMessage
    {
        public string Message { get; set; }
        public Album Album { get; set; }
        public bool DeletedFiles { get; set; }
        public string DeletedFilesMessage { get; set; }

        public override string ToString()
        {
            return Message;
        }

        public AlbumDeleteMessage(Album album, bool deleteFiles)
        {
            Album = album;
            DeletedFiles = deleteFiles;
            DeletedFilesMessage = DeletedFiles ?
                "Album removed and all files were deleted" :
                "Album removed, files were not deleted";
            Message = album.Title + " - " + DeletedFilesMessage;
        }
    }
}

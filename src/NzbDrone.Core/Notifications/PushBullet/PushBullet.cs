using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Notifications.PushBullet
{
    public class PushBullet : NotificationBase<PushBulletSettings>
    {
        private readonly IPushBulletProxy _proxy;

        public PushBullet(IPushBulletProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "Pushbullet";
        public override string Link => "https://www.pushbullet.com/";


        public override void OnGrab(GrabMessage grabMessage)
        {
            _proxy.SendNotification(ALBUM_GRABBED_TITLE_BRANDED, grabMessage.Message, Settings);
        }

        public override void OnDownload(TrackDownloadMessage message)
        {
            _proxy.SendNotification(TRACK_DOWNLOADED_TITLE_BRANDED, message.Message, Settings);
        }

        public override void OnAlbumDownload(AlbumDownloadMessage message)
        {
            _proxy.SendNotification(ALBUM_DOWNLOADED_TITLE_BRANDED, message.Message, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_proxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}

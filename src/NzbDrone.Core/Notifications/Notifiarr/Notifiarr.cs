using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.HealthCheck;

namespace NzbDrone.Core.Notifications.Notifiarr;

public class Notifiarr : NotificationBase<NotifiarrSettings>
{
    private readonly INotifiarrProxy _proxy;

    public Notifiarr(INotifiarrProxy proxy)
    {
        _proxy = proxy;
    }

    public override string Link => "https://notifiarr.com";
    public override string Name => "Notifiarr";

    public override void OnGrab(GrabMessage message)
    {
        var artist = message.Artist;
        var remoteAlbum = message.Album;
        var releaseGroup = remoteAlbum.ParsedAlbumInfo.ReleaseGroup;
        var variables = new StringDictionary();

        variables.Add("Lidarr_EventType", "Grab");
        variables.Add("Lidarr_Artist_Id", artist.Id.ToString());
        variables.Add("Lidarr_Artist_Name", artist.Metadata.Value.Name);
        variables.Add("Lidarr_Artist_MBId", artist.Metadata.Value.ForeignArtistId);
        variables.Add("Lidarr_Artist_Type", artist.Metadata.Value.Type);
        variables.Add("Lidarr_Release_AlbumCount", remoteAlbum.Albums.Count.ToString());
        variables.Add("Lidarr_Release_AlbumReleaseDates", string.Join(",", remoteAlbum.Albums.Select(e => e.ReleaseDate)));
        variables.Add("Lidarr_Release_AlbumTitles", string.Join("|", remoteAlbum.Albums.Select(e => e.Title)));
        variables.Add("Lidarr_Release_AlbumMBIds", string.Join("|", remoteAlbum.Albums.Select(e => e.ForeignAlbumId)));
        variables.Add("Lidarr_Release_Title", remoteAlbum.Release.Title);
        variables.Add("Lidarr_Release_Indexer", remoteAlbum.Release.Indexer ?? string.Empty);
        variables.Add("Lidarr_Release_Size", remoteAlbum.Release.Size.ToString());
        variables.Add("Lidarr_Release_Quality", remoteAlbum.ParsedAlbumInfo.Quality.Quality.Name);
        variables.Add("Lidarr_Release_QualityVersion", remoteAlbum.ParsedAlbumInfo.Quality.Revision.Version.ToString());
        variables.Add("Lidarr_Release_ReleaseGroup", releaseGroup ?? string.Empty);
        variables.Add("Lidarr_Download_Client", message.DownloadClient ?? string.Empty);
        variables.Add("Lidarr_Download_Id", message.DownloadId ?? string.Empty);

        _proxy.SendNotification(variables, Settings);
    }

    public override void OnReleaseImport(AlbumDownloadMessage message)
    {
        var artist = message.Artist;
        var album = message.Album;
        var release = message.Release;
        var variables = new StringDictionary();

        variables.Add("Lidarr_EventType", "Download");
        variables.Add("Lidarr_Artist_Id", artist.Id.ToString());
        variables.Add("Lidarr_Artist_Name", artist.Metadata.Value.Name);
        variables.Add("Lidarr_Artist_Path", artist.Path);
        variables.Add("Lidarr_Artist_MBId", artist.Metadata.Value.ForeignArtistId);
        variables.Add("Lidarr_Artist_Type", artist.Metadata.Value.Type);
        variables.Add("Lidarr_Album_Id", album.Id.ToString());
        variables.Add("Lidarr_Album_Title", album.Title);
        variables.Add("Lidarr_Album_MBId", album.ForeignAlbumId);
        variables.Add("Lidarr_AlbumRelease_MBId", release.ForeignReleaseId);
        variables.Add("Lidarr_Album_ReleaseDate", album.ReleaseDate.ToString());
        variables.Add("Lidarr_Download_Client", message.DownloadClient ?? string.Empty);
        variables.Add("Lidarr_Download_Id", message.DownloadId ?? string.Empty);

        if (message.TrackFiles.Any())
        {
            variables.Add("Lidarr_AddedTrackPaths", string.Join("|", message.TrackFiles.Select(e => e.Path)));
        }

        if (message.OldFiles.Any())
        {
            variables.Add("Lidarr_DeletedPaths", string.Join("|", message.OldFiles.Select(e => e.Path)));
        }

        _proxy.SendNotification(variables, Settings);
    }

    public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
    {
        var variables = new StringDictionary();

        variables.Add("Lidarr_EventType", "HealthIssue");
        variables.Add("Lidarr_Health_Issue_Level", Enum.GetName(typeof(HealthCheckResult), healthCheck.Type));
        variables.Add("Lidarr_Health_Issue_Message", healthCheck.Message);
        variables.Add("Lidarr_Health_Issue_Type", healthCheck.Source.Name);
        variables.Add("Lidarr_Health_Issue_Wiki", healthCheck.WikiUrl.ToString() ?? string.Empty);

        _proxy.SendNotification(variables, Settings);
    }

    public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
    {
        var variables = new StringDictionary();

        variables.Add("Lidarr_EventType", "ApplicationUpdate");
        variables.Add("Lidarr_Update_Message", updateMessage.Message);
        variables.Add("Lidarr_Update_NewVersion", updateMessage.NewVersion.ToString());
        variables.Add("Lidarr_Update_PreviousVersion", updateMessage.PreviousVersion.ToString());

        _proxy.SendNotification(variables, Settings);
    }

    public override ValidationResult Test()
    {
        var failures = new List<ValidationFailure>();

        failures.AddIfNotNull(_proxy.Test(Settings));

        return new ValidationResult(failures);
    }
}

using System.Collections.Generic;
using System.Data;
using FluentMigrator;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(052)]
    public class download_history : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("DownloadHistory")
                  .WithColumn("EventType").AsInt32().NotNullable()
                  .WithColumn("ArtistId").AsInt32().NotNullable()
                  .WithColumn("DownloadId").AsString().NotNullable()
                  .WithColumn("SourceTitle").AsString().NotNullable()
                  .WithColumn("Date").AsDateTime().NotNullable()
                  .WithColumn("Protocol").AsString().Nullable()
                  .WithColumn("IndexerId").AsInt32().Nullable()
                  .WithColumn("DownloadClientId").AsInt32().Nullable()
                  .WithColumn("Release").AsString().Nullable()
                  .WithColumn("Data").AsString().Nullable();

            Create.Index().OnTable("DownloadHistory").OnColumn("EventType");
            Create.Index().OnTable("DownloadHistory").OnColumn("ArtistId");
            Create.Index().OnTable("DownloadHistory").OnColumn("DownloadId");

            IfDatabase("sqlite").Execute.WithConnection(InitialImportedDownloadHistory);
        }

        private static readonly Dictionary<int, int> EventTypeMap = new Dictionary<int, int>()
        {
            // EntityHistoryType.Grabbed -> DownloadHistoryType.Grabbed
            { 1, 1 },

            // EntityHistoryType.DownloadFolderImported -> DownloadHistoryType.DownloadImported
            { 8, 2 },

            // EntityHistoryType.DownloadFailed -> DownloadHistoryType.DownloadFailed
            { 4, 3 },

            // EntityHistoryType.DownloadIgnored -> DownloadHistoryType.DownloadIgnored
            { 10, 4 },

            // EntityHistoryType.DownloadImportIncomplete -> DownloadHistoryType.DownloadImportIncomplete
            { 7, 6 }
        };

        private void InitialImportedDownloadHistory(IDbConnection conn, IDbTransaction tran)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT ArtistId, DownloadId, EventType, SourceTitle, Date, Data FROM History WHERE DownloadId IS NOT NULL AND EventType IN (1, 8, 4, 10, 7) GROUP BY EventType, DownloadId";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var artistId = reader.GetInt32(0);
                        var downloadId = reader.GetString(1);
                        var eventType = reader.GetInt32(2);
                        var sourceTitle = reader.GetString(3);
                        var date = reader.GetDateTime(4);
                        var rawData = reader.GetString(5);
                        var data = Json.Deserialize<Dictionary<string, string>>(rawData);

                        var downloadHistoryEventType = EventTypeMap[eventType];

                        string protocol = null;
                        if (data.ContainsKey("protocol"))
                        {
                            if (int.TryParse(data["protocol"], out var protocolNum))
                            {
                                if (protocolNum == 1)
                                {
                                    protocol = nameof(UsenetDownloadProtocol);
                                }
                                else if (protocolNum == 2)
                                {
                                    protocol = nameof(TorrentDownloadProtocol);
                                }
                            }
                            else
                            {
                                protocol = data["protocol"];
                            }
                        }

                        var downloadHistoryData = new Dictionary<string, string>();

                        if (data.ContainsKey("indexer"))
                        {
                            downloadHistoryData.Add("indexer", data["indexer"]);
                        }

                        if (data.ContainsKey("downloadClient"))
                        {
                            downloadHistoryData.Add("downloadClient", data["downloadClient"]);
                        }

                        if (data.ContainsKey("statusMessages"))
                        {
                            downloadHistoryData.Add("statusMessages", data["statusMessages"]);
                        }

                        using (var updateCmd = conn.CreateCommand())
                        {
                            updateCmd.Transaction = tran;
                            updateCmd.CommandText = @"INSERT INTO DownloadHistory (EventType, ArtistId, DownloadId, SourceTitle, Date, Protocol, Data) VALUES (?, ?, ?, ?, ?, ?, ?)";
                            updateCmd.AddParameter(downloadHistoryEventType);
                            updateCmd.AddParameter(artistId);
                            updateCmd.AddParameter(downloadId);
                            updateCmd.AddParameter(sourceTitle);
                            updateCmd.AddParameter(date);
                            updateCmd.AddParameter(protocol);
                            updateCmd.AddParameter(downloadHistoryData.ToJson());

                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}

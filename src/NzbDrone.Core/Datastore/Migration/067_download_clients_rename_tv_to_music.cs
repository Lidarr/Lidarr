using System.Collections.Generic;
using System.Data;
using Dapper;
using FluentMigrator;
using Newtonsoft.Json.Linq;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(067)]
    public class download_clients_rename_tv_to_music : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(MigrateSettingsTvToMusic);
        }

        private void MigrateSettingsTvToMusic(IDbConnection conn, IDbTransaction tran)
        {
            var updatedClients = new List<object>();

            using (var selectCommand = conn.CreateCommand())
            {
                selectCommand.Transaction = tran;
                selectCommand.CommandText = "SELECT \"Id\", \"Settings\" FROM \"DownloadClients\"";

                using var reader = selectCommand.ExecuteReader();

                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var settings = reader.GetString(1);

                    if (!string.IsNullOrWhiteSpace(settings))
                    {
                        var jsonObject = Json.Deserialize<JObject>(settings);

                        if (jsonObject.ContainsKey("recentTvPriority"))
                        {
                            jsonObject.Add("recentMusicPriority", jsonObject.Value<int>("recentTvPriority"));
                            jsonObject.Remove("recentTvPriority");
                        }

                        if (jsonObject.ContainsKey("olderTvPriority"))
                        {
                            jsonObject.Add("olderMusicPriority", jsonObject.Value<int>("olderTvPriority"));
                            jsonObject.Remove("olderTvPriority");
                        }

                        if (jsonObject.ContainsKey("tvDirectory"))
                        {
                            jsonObject.Add("musicDirectory", jsonObject.Value<string>("tvDirectory"));
                            jsonObject.Remove("tvDirectory");
                        }

                        settings = jsonObject.ToJson();
                    }

                    updatedClients.Add(new
                    {
                        Id = id,
                        Settings = settings
                    });
                }
            }

            var updateClientsSql = "UPDATE \"DownloadClients\" SET \"Settings\" = @Settings WHERE \"Id\" = @Id";
            conn.Execute(updateClientsSql, updatedClients, transaction: tran);
        }
    }
}

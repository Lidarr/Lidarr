using System.Data;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(082)]
    public class add_tagging_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Create.TableForModel("TaggingProfiles")
                  .WithColumn("Name").AsString().NotNullable()
                  .WithColumn("Tags").AsString().NotNullable()
                  .WithColumn("Order").AsInt32().NotNullable()
                  .WithColumn("WriteAudioTags").AsInt32().NotNullable()
                  .WithColumn("ScrubAudioTags").AsBoolean().NotNullable()
                  .WithColumn("EmbedCoverArt").AsBoolean().NotNullable()
                  .WithColumn("DownloadClientIds").AsString().NotNullable()
                  .WithColumn("SkipHardlinkedFiles").AsBoolean().NotNullable();

            Execute.WithConnection(SeedDefaultProfile);
        }

        private void SeedDefaultProfile(IDbConnection conn, IDbTransaction tran)
        {
            // Values mirror WriteAudioTagsType and the ConfigService defaults at the time of this migration
            var writeAudioTags = 0;
            var scrubAudioTags = false;
            var embedCoverArt = true;

            using (var writeAudioTagsCmd = conn.CreateCommand(tran, "SELECT \"Value\" FROM \"Config\" WHERE \"Key\" = 'writeaudiotags'"))
            {
                switch ((writeAudioTagsCmd.ExecuteScalar() as string)?.ToLower())
                {
                    case "newfiles":
                        writeAudioTags = 1;
                        break;
                    case "allfiles":
                        writeAudioTags = 2;
                        break;
                    case "sync":
                        writeAudioTags = 3;
                        break;
                }
            }

            using (var scrubAudioTagsCmd = conn.CreateCommand(tran, "SELECT \"Value\" FROM \"Config\" WHERE \"Key\" = 'scrubaudiotags'"))
            {
                if ((scrubAudioTagsCmd.ExecuteScalar() as string)?.ToLower() == "true")
                {
                    scrubAudioTags = true;
                }
            }

            using (var embedCoverArtCmd = conn.CreateCommand(tran, "SELECT \"Value\" FROM \"Config\" WHERE \"Key\" = 'embedcoverart'"))
            {
                if ((embedCoverArtCmd.ExecuteScalar() as string)?.ToLower() == "false")
                {
                    embedCoverArt = false;
                }
            }

            string commandText;

            if (conn.GetType().FullName == "Npgsql.NpgsqlConnection")
            {
                commandText = "INSERT INTO \"TaggingProfiles\" (\"Name\", \"Tags\", \"Order\", \"WriteAudioTags\", \"ScrubAudioTags\", \"EmbedCoverArt\", \"DownloadClientIds\", \"SkipHardlinkedFiles\") VALUES ('Default', '[]', 2147483647, $1, $2, $3, '[]', $4)";
            }
            else
            {
                commandText = "INSERT INTO \"TaggingProfiles\" (\"Name\", \"Tags\", \"Order\", \"WriteAudioTags\", \"ScrubAudioTags\", \"EmbedCoverArt\", \"DownloadClientIds\", \"SkipHardlinkedFiles\") VALUES ('Default', '[]', 2147483647, ?, ?, ?, '[]', ?)";
            }

            using (var insertCmd = conn.CreateCommand(tran, commandText))
            {
                insertCmd.AddParameter(writeAudioTags);
                insertCmd.AddParameter(scrubAudioTags);
                insertCmd.AddParameter(embedCoverArt);
                insertCmd.AddParameter(false);
                insertCmd.ExecuteNonQuery();
            }

            using (var removeConfigCmd = conn.CreateCommand(tran, "DELETE FROM \"Config\" WHERE \"Key\" IN ('writeaudiotags', 'scrubaudiotags', 'embedcoverart')"))
            {
                removeConfigCmd.ExecuteNonQuery();
            }
        }
    }
}

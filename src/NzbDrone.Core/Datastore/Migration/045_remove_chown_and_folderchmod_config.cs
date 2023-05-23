using System;
using System.Data;
using FluentMigrator;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(045)]
    public class remove_chown_and_folderchmod_config : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            IfDatabase("sqlite").Execute.Sql("DELETE FROM config WHERE Key IN ('folderchmod', 'chownuser')");
            IfDatabase("sqlite").Execute.WithConnection(ConvertFileChmodToFolderChmod);
        }

        private void ConvertFileChmodToFolderChmod(IDbConnection conn, IDbTransaction tran)
        {
            using (var getFileChmodCmd = conn.CreateCommand())
            {
                getFileChmodCmd.Transaction = tran;
                getFileChmodCmd.CommandText = @"SELECT Value FROM Config WHERE Key = 'filechmod'";

                var fileChmod = getFileChmodCmd.ExecuteScalar() as string;
                if (fileChmod != null)
                {
                    if (fileChmod.IsNotNullOrWhiteSpace())
                    {
                        // Convert without using mono libraries. We take the 'r' bits and shifting them to the 'x' position, preserving everything else.
                        var fileChmodNum = Convert.ToInt32(fileChmod, 8);
                        var folderChmodNum = fileChmodNum | ((fileChmodNum & 0x124) >> 2);
                        var folderChmod = Convert.ToString(folderChmodNum, 8).PadLeft(3, '0');

                        using (var insertCmd = conn.CreateCommand())
                        {
                            insertCmd.Transaction = tran;
                            insertCmd.CommandText = "INSERT INTO Config (Key, Value) VALUES ('chmodfolder', ?)";
                            insertCmd.AddParameter(folderChmod);

                            insertCmd.ExecuteNonQuery();
                        }
                    }

                    using (var deleteCmd = conn.CreateCommand())
                    {
                        deleteCmd.Transaction = tran;
                        deleteCmd.CommandText = "DELETE FROM Config WHERE Key = 'filechmod'";

                        deleteCmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}

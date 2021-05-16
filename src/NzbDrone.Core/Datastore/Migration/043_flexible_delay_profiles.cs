using System.Collections.Generic;
using System.Data;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Indexers;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(043)]
    public class flexible_delay_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("DelayProfiles").AddColumn("Name").AsString().Nullable();
            Alter.Table("DelayProfiles").AddColumn("Items").AsString().WithDefaultValue("[]");

            Execute.WithConnection(Migrate);

            Delete.Column("EnableUsenet").FromTable("DelayProfiles");
            Delete.Column("EnableTorrent").FromTable("DelayProfiles");
            Delete.Column("PreferredProtocol").FromTable("DelayProfiles");
            Delete.Column("UsenetDelay").FromTable("DelayProfiles");
            Delete.Column("TorrentDelay").FromTable("DelayProfiles");
        }

        private void Migrate(IDbConnection conn, IDbTransaction tran)
        {
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<DelayProfileProtocolItem42>>());

            var rows = conn.Query<DelayProfile41>("SELECT * from DelayProfiles", transaction: tran);
            var newRows = new List<DelayProfile42>();

            foreach (var row in rows)
            {
                var usenet = new DelayProfileProtocolItem42
                {
                    Name = "Usenet",
                    Protocol = nameof(UsenetDownloadProtocol),
                    Allowed = row.EnableUsenet,
                    Delay = row.UsenetDelay
                };

                var torrent = new DelayProfileProtocolItem42
                {
                    Name = "Torrent",
                    Protocol = nameof(TorrentDownloadProtocol),
                    Allowed = row.EnableTorrent,
                    Delay = row.TorrentDelay
                };

                List<DelayProfileProtocolItem42> items;

                if (row.PreferredProtocol == 2)
                {
                    items = new List<DelayProfileProtocolItem42>
                    {
                        torrent,
                        usenet
                    };
                }
                else
                {
                    items = new List<DelayProfileProtocolItem42>
                    {
                        usenet,
                        torrent
                    };
                }

                newRows.Add(new DelayProfile42
                {
                    Id = row.Id,
                    Name = row.Id == 1 ? "Default" : $"Delay Profile {row.Id}",
                    Items = items
                });
            }

            var sql = $"UPDATE DelayProfiles SET Name = @Name, Items = @Items WHERE Id = @Id";
            conn.Execute(sql, newRows, transaction: tran);
        }

        private class DelayProfile41 : ModelBase
        {
            public bool EnableUsenet { get; set; }
            public bool EnableTorrent { get; set; }
            public int PreferredProtocol { get; set; }
            public int UsenetDelay { get; set; }
            public int TorrentDelay { get; set; }
            public int Order { get; set; }
            public HashSet<int> Tags { get; set; }
        }

        private class DelayProfile42 : ModelBase
        {
            public string Name { get; set; }
            public List<DelayProfileProtocolItem42> Items { get; set; }
            public int Order { get; set; }
            public HashSet<int> Tags { get; set; }
        }

        private class DelayProfileProtocolItem42 : IEmbeddedDocument
        {
            public string Name { get; set; }
            public string Protocol { get; set; }
            public bool Allowed { get; set; }
            public int Delay { get; set; }
        }
    }
}

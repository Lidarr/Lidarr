using System.Collections.Generic;
using System.Data;
using System.Linq;
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

            Execute.WithConnection(MigrateDelayProfiles);

            Delete.Column("EnableUsenet").FromTable("DelayProfiles");
            Delete.Column("EnableTorrent").FromTable("DelayProfiles");
            Delete.Column("PreferredProtocol").FromTable("DelayProfiles");
            Delete.Column("UsenetDelay").FromTable("DelayProfiles");
            Delete.Column("TorrentDelay").FromTable("DelayProfiles");

            // this migration was merged after 52, so it may be that DownloadHistory was created with Protocol as an int
            // and not a string.
            if (Schema.Table("DownloadHistory").Exists())
            {
                Alter.Table("DownloadHistory").AddColumn("ProtocolString").AsString().Nullable();
                Execute.WithConnection(MigrateDownloadHistory);
                Delete.Column("Protocol").FromTable("DownloadHistory");
                Rename.Column("ProtocolString").OnTable("DownloadHistory").To("Protocol");
            }
        }

        private void MigrateDownloadHistory(IDbConnection conn, IDbTransaction tran)
        {
            var rows = conn.Query<DownloadHistory52>("SELECT \"Id\", \"Protocol\" from \"DownloadHistory\"", transaction: tran);

            var newRows = rows.Select(row => new DownloadHistory43
            {
                Id = row.Id,
                ProtocolString = row.Protocol == 1 ? nameof(UsenetDownloadProtocol) : nameof(TorrentDownloadProtocol)
            }).ToList();

            var sql = $"UPDATE \"DownloadHistory\" SET \"ProtocolString\" = @ProtocolString WHERE \"Id\" = @Id";
            conn.Execute(sql, newRows, transaction: tran);
        }

        private class DownloadHistory52 : ModelBase
        {
            public int Protocol { get; set; }
        }

        public class DownloadHistory43 : ModelBase
        {
            public string ProtocolString { get; set; }
        }

        private void MigrateDelayProfiles(IDbConnection conn, IDbTransaction tran)
        {
            SqlMapper.AddTypeHandler(new EmbeddedDocumentConverter<List<DelayProfileProtocolItem42>>());

            var rows = conn.Query<DelayProfile41>("SELECT * from \"DelayProfiles\"", transaction: tran);
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

            var sql = $"UPDATE \"DelayProfiles\" SET \"Name\" = @Name, \"Items\" = @Items WHERE \"Id\" = @Id";
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

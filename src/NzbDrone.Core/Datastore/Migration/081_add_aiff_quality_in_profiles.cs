using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using FluentMigrator;
using Newtonsoft.Json;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(081)]
    public class add_aiff_quality_in_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(ConvertProfile);
        }

        private void ConvertProfile(IDbConnection conn, IDbTransaction tran)
        {
            var updater = new QualityProfileUpdater81(conn, tran);

            updater.SplitQualityAppend(13, 38);  // AIFF (38) after WAV (13)

            updater.Commit();
        }
    }

    public class QualityProfile81
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Cutoff { get; set; }
        public List<QualityProfileItem81> Items { get; set; }
    }

    public class QualityProfileItem81
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Quality { get; set; }
        public bool Allowed { get; set; }
        public List<QualityProfileItem81> Items { get; set; }
    }

    public class QualityProfileUpdater81
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;

        private List<QualityProfile81> _profiles;
        private HashSet<QualityProfile81> _changedProfiles = new ();

        public QualityProfileUpdater81(IDbConnection conn, IDbTransaction tran)
        {
            _connection = conn;
            _transaction = tran;

            _profiles = GetProfiles();
        }

        public void Commit()
        {
            var profilesToUpdate = _changedProfiles.Select(p => new
            {
                p.Id,
                p.Name,
                p.Cutoff,
                Items = p.Items.ToJson()
            });

            var updateSql = "UPDATE \"QualityProfiles\" SET \"Name\" = @Name, \"Cutoff\" = @Cutoff, \"Items\" = @Items WHERE \"Id\" = @Id";
            _connection.Execute(updateSql, profilesToUpdate, transaction: _transaction);

            _changedProfiles.Clear();
        }

        public void SplitQualityAppend(int find, int quality)
        {
            foreach (var profile in _profiles)
            {
                if (profile.Items.Any(v => v.Quality == quality) ||
                    profile.Items.Any(v => v.Items != null && v.Items.Any(b => b.Quality == quality)))
                {
                    continue;
                }

                foreach (var item in profile.Items.Where(x => x.Items != null))
                {
                    var findIndex = item.Items.FindIndex(v => v.Quality == find);

                    if (findIndex == -1)
                    {
                        continue;
                    }

                    item.Items.Insert(findIndex + 1, new QualityProfileItem81
                    {
                        Quality = quality,
                        Allowed = true
                    });
                }

                if (!profile.Items.Any(v => v.Items != null && v.Items.Any(b => b.Quality == quality)))
                {
                    profile.Items.Add(new QualityProfileItem81
                    {
                        Quality = quality,
                        Allowed = false
                    });
                }

                _changedProfiles.Add(profile);
            }
        }

        private List<QualityProfile81> GetProfiles()
        {
            var profiles = new List<QualityProfile81>();

            using (var getProfilesCmd = _connection.CreateCommand())
            {
                getProfilesCmd.Transaction = _transaction;
                getProfilesCmd.CommandText = "SELECT \"Id\", \"Name\", \"Cutoff\", \"Items\" FROM \"QualityProfiles\"";

                using (var profileReader = getProfilesCmd.ExecuteReader())
                {
                    while (profileReader.Read())
                    {
                        profiles.Add(new QualityProfile81
                        {
                            Id = profileReader.GetInt32(0),
                            Name = profileReader.GetString(1),
                            Cutoff = profileReader.GetInt32(2),
                            Items = Json.Deserialize<List<QualityProfileItem81>>(profileReader.GetString(3))
                        });
                    }
                }
            }

            return profiles;
        }
    }
}

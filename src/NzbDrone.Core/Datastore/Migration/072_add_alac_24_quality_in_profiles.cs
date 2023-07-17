using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentMigrator;
using Newtonsoft.Json;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(072)]
    public class add_alac_24_quality_in_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(ConvertProfile);
        }

        private void ConvertProfile(IDbConnection conn, IDbTransaction tran)
        {
            var updater = new QualityProfileUpdater72(conn, tran);

            updater.SplitQualityAppend(21, 37);  // ALAC_24 after FLAC 24bit

            updater.Commit();
        }
    }

    public class QualityProfile72
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Cutoff { get; set; }
        public List<QualityProfileItem72> Items { get; set; }
    }

    public class QualityProfileItem72
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Quality { get; set; }
        public bool Allowed { get; set; }
        public List<QualityProfileItem72> Items { get; set; }
    }

    public class QualityProfileUpdater72
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;

        private List<QualityProfile72> _profiles;
        private HashSet<QualityProfile72> _changedProfiles = new ();

        public QualityProfileUpdater72(IDbConnection conn, IDbTransaction tran)
        {
            _connection = conn;
            _transaction = tran;

            _profiles = GetProfiles();
        }

        public void Commit()
        {
            foreach (var profile in _changedProfiles)
            {
                using (var updateProfileCmd = _connection.CreateCommand())
                {
                    updateProfileCmd.Transaction = _transaction;
                    updateProfileCmd.CommandText = "UPDATE \"QualityProfiles\" SET \"Name\" = ?, \"Cutoff\" = ?, \"Items\" = ? WHERE \"Id\" = ?";
                    updateProfileCmd.AddParameter(profile.Name);
                    updateProfileCmd.AddParameter(profile.Cutoff);
                    updateProfileCmd.AddParameter(profile.Items.ToJson());
                    updateProfileCmd.AddParameter(profile.Id);

                    updateProfileCmd.ExecuteNonQuery();
                }
            }

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

                    item.Items.Insert(findIndex + 1, new QualityProfileItem72
                    {
                        Quality = quality,
                        Allowed = true
                    });
                }

                if (!profile.Items.Any(v => v.Items != null && v.Items.Any(b => b.Quality == quality)))
                {
                    profile.Items.Add(new QualityProfileItem72
                    {
                        Quality = quality,
                        Allowed = false
                    });
                }

                _changedProfiles.Add(profile);
            }
        }

        private List<QualityProfile72> GetProfiles()
        {
            var profiles = new List<QualityProfile72>();

            using (var getProfilesCmd = _connection.CreateCommand())
            {
                getProfilesCmd.Transaction = _transaction;
                getProfilesCmd.CommandText = "SELECT \"Id\", \"Name\", \"Cutoff\", \"Items\" FROM \"QualityProfiles\"";

                using (var profileReader = getProfilesCmd.ExecuteReader())
                {
                    while (profileReader.Read())
                    {
                        profiles.Add(new QualityProfile72
                        {
                            Id = profileReader.GetInt32(0),
                            Name = profileReader.GetString(1),
                            Cutoff = profileReader.GetInt32(2),
                            Items = Json.Deserialize<List<QualityProfileItem72>>(profileReader.GetString(3))
                        });
                    }
                }
            }

            return profiles;
        }
    }
}

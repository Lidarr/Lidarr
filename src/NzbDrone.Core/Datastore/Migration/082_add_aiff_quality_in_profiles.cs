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
    [Migration(082)]
    public class add_aiff_quality_in_profiles : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(ConvertProfile);
        }

        private void ConvertProfile(IDbConnection conn, IDbTransaction tran)
        {
            var updater = new QualityProfileUpdater82(conn, tran);

            updater.SplitQualityPrepend(13, 38, 37);  // AIFF before WAV, or after ALAC 24bit if a profile has no WAV

            updater.Commit();
        }
    }

    public class QualityProfile82
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Cutoff { get; set; }
        public List<QualityProfileItem82> Items { get; set; }
    }

    public class QualityProfileItem82
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Quality { get; set; }
        public List<QualityProfileItem82> Items { get; set; }
        public bool Allowed { get; set; }
    }

    public class QualityProfileUpdater82
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;

        private List<QualityProfile82> _profiles;
        private HashSet<QualityProfile82> _changedProfiles = new();

        public QualityProfileUpdater82(IDbConnection conn, IDbTransaction tran)
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

        public void SplitQualityPrepend(int find, int quality, int fallback)
        {
            foreach (var profile in _profiles)
            {
                if (profile.Items.Any(v => v.Quality == quality) ||
                    profile.Items.Any(v => v.Items != null && v.Items.Any(b => b.Quality == quality)))
                {
                    continue;
                }

                // Insert directly below the quality we're looking for, wherever it is (top level or inside a group),
                // and copy its allowed state.
                var container = profile.Items;
                var findIndex = container.FindIndex(v => v.Quality == find);

                if (findIndex == -1)
                {
                    var group = profile.Items.FirstOrDefault(v => v.Items != null && v.Items.Any(b => b.Quality == find));

                    if (group != null)
                    {
                        container = group.Items;
                        findIndex = container.FindIndex(v => v.Quality == find);
                    }
                }

                if (findIndex > -1)
                {
                    var findItem = container[findIndex];

                    container.Insert(findIndex, new QualityProfileItem82
                    {
                        Quality = quality,
                        Allowed = findItem.Allowed,
                        Items = new List<QualityProfileItem82>()
                    });
                }
                else
                {
                    // The quality we're looking for is missing, so add it as not allowed after the fallback quality
                    // (or the group containing it), or at the end if that is missing too.
                    var fallbackIndex = profile.Items.FindIndex(v => v.Quality == fallback || (v.Items != null && v.Items.Any(b => b.Quality == fallback)));

                    profile.Items.Insert(fallbackIndex == -1 ? profile.Items.Count : fallbackIndex + 1, new QualityProfileItem82
                    {
                        Quality = quality,
                        Allowed = false,
                        Items = new List<QualityProfileItem82>()
                    });
                }

                _changedProfiles.Add(profile);
            }
        }

        private List<QualityProfile82> GetProfiles()
        {
            var profiles = new List<QualityProfile82>();

            using (var getProfilesCmd = _connection.CreateCommand())
            {
                getProfilesCmd.Transaction = _transaction;
                getProfilesCmd.CommandText = "SELECT \"Id\", \"Name\", \"Cutoff\", \"Items\" FROM \"QualityProfiles\"";

                using (var profileReader = getProfilesCmd.ExecuteReader())
                {
                    while (profileReader.Read())
                    {
                        profiles.Add(new QualityProfile82
                        {
                            Id = profileReader.GetInt32(0),
                            Name = profileReader.GetString(1),
                            Cutoff = profileReader.GetInt32(2),
                            Items = Json.Deserialize<List<QualityProfileItem82>>(profileReader.GetString(3))
                        });
                    }
                }
            }

            return profiles;
        }
    }
}

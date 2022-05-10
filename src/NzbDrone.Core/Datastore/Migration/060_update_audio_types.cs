using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using FluentMigrator;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(060)]
    public class update_audio_types : NzbDroneMigrationBase
    {
        private readonly JsonSerializerOptions _serializerSettings;

        public update_audio_types()
        {
            _serializerSettings = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNameCaseInsensitive = true,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        protected override void MainDbUpgrade()
        {
            Execute.WithConnection(GetEntries);
        }

        private void GetEntries(IDbConnection conn, IDbTransaction tran)
        {
            var profiles = conn.Query<MetadataProfile059>($"SELECT \"Id\", \"SecondaryAlbumTypes\" FROM \"MetadataProfiles\"");

            var corrected = new List<MetadataProfile059>();

            foreach (var profile in profiles)
            {
                var oldTypes = JsonSerializer.Deserialize<List<SecondaryAlbumType059>>(profile.SecondaryAlbumTypes, _serializerSettings);

                oldTypes.Add(new SecondaryAlbumType059
                {
                    SecondaryAlbumType = 11,
                    Allowed = false
                });

                corrected.Add(new MetadataProfile059
                {
                    Id = profile.Id,
                    SecondaryAlbumTypes = JsonSerializer.Serialize(oldTypes, _serializerSettings)
                });
            }

            var updateSql = $"UPDATE \"MetadataProfiles\" SET \"SecondaryAlbumTypes\" = @SecondaryAlbumTypes WHERE \"Id\" = @Id";
            conn.Execute(updateSql, corrected, transaction: tran);
        }

        private class MetadataProfile059
        {
            public int Id { get; set; }
            public string SecondaryAlbumTypes { get; set; }
        }

        private class SecondaryAlbumType059
        {
            public int SecondaryAlbumType { get; set; }
            public bool Allowed { get; set; }
        }
    }
}

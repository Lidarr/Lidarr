using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Dapper;
using FluentMigrator;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(063)]
    public class add_custom_formats : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Alter.Table("DelayProfiles").AddColumn("BypassIfHighestQuality").AsBoolean().WithDefaultValue(false);

            // Set to true for existing Delay Profiles to keep behavior the same.
            Update.Table("DelayProfiles").Set(new { BypassIfHighestQuality = true }).AllRows();

            Alter.Table("TrackFiles").AddColumn("OriginalFilePath").AsString().Nullable();

            Execute.WithConnection(ChangeRequiredIgnoredTypes);

            // Add Custom Format Columns
            Create.TableForModel("CustomFormats")
                .WithColumn("Name").AsString().Unique()
                .WithColumn("Specifications").AsString().WithDefaultValue("[]")
                .WithColumn("IncludeCustomFormatWhenRenaming").AsBoolean().WithDefaultValue(false);

            // Add Custom Format Columns to Quality Profiles
            Alter.Table("QualityProfiles").AddColumn("FormatItems").AsString().WithDefaultValue("[]");
            Alter.Table("QualityProfiles").AddColumn("MinFormatScore").AsInt32().WithDefaultValue(0);
            Alter.Table("QualityProfiles").AddColumn("CutoffFormatScore").AsInt32().WithDefaultValue(0);

            // Migrate Preferred Words to Custom Formats
            Execute.WithConnection(MigratePreferredTerms);
            Execute.WithConnection(MigrateNamingConfigs);

            // Remove Preferred Word Columns from ReleaseProfiles
            Delete.Column("Preferred").FromTable("ReleaseProfiles");
            Delete.Column("IncludePreferredWhenRenaming").FromTable("ReleaseProfiles");

            // Remove Profiles that will no longer validate
            Execute.Sql("DELETE FROM \"ReleaseProfiles\" WHERE \"Required\" = '[]' AND \"Ignored\" = '[]'");

            Alter.Table("DelayProfiles").AddColumn("BypassIfAboveCustomFormatScore").AsBoolean().WithDefaultValue(false);
            Alter.Table("DelayProfiles").AddColumn("MinimumCustomFormatScore").AsInt32().Nullable();
        }

        private void ChangeRequiredIgnoredTypes(IDbConnection conn, IDbTransaction tran)
        {
            var updatedProfiles = new List<object>();

            using (var getEmailCmd = conn.CreateCommand())
            {
                getEmailCmd.Transaction = tran;
                getEmailCmd.CommandText = "SELECT \"Id\", \"Required\", \"Ignored\" FROM \"ReleaseProfiles\"";

                using (var reader = getEmailCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var id = reader.GetInt32(0);
                        var requiredObj = reader.GetValue(1);
                        var ignoredObj = reader.GetValue(2);

                        var required = requiredObj == DBNull.Value
                            ? Enumerable.Empty<string>()
                            : requiredObj.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        var ignored = ignoredObj == DBNull.Value
                            ? Enumerable.Empty<string>()
                            : ignoredObj.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                        updatedProfiles.Add(new
                        {
                            Id = id,
                            Required = required.ToJson(),
                            Ignored = ignored.ToJson()
                        });
                    }
                }
            }

            var updateProfileSql = "UPDATE \"ReleaseProfiles\" SET \"Required\" = @Required, \"Ignored\" = @Ignored WHERE \"Id\" = @Id";
            conn.Execute(updateProfileSql, updatedProfiles, transaction: tran);
        }

        private void MigratePreferredTerms(IDbConnection conn, IDbTransaction tran)
        {
            var updatedCollections = new List<CustomFormat057>();

            // Pull list of quality Profiles
            var qualityProfiles = new List<QualityProfile057>();
            using (var getProfiles = conn.CreateCommand())
            {
                getProfiles.Transaction = tran;
                getProfiles.CommandText = @"SELECT ""Id"" FROM ""QualityProfiles""";

                using (var definitionsReader = getProfiles.ExecuteReader())
                {
                    while (definitionsReader.Read())
                    {
                        var id = definitionsReader.GetInt32(0);
                        qualityProfiles.Add(new QualityProfile057
                        {
                            Id = id,
                        });
                    }
                }
            }

            // Generate List of Custom Formats from Preferred Words
            using (var cmd = conn.CreateCommand())
            {
                cmd.Transaction = tran;
                cmd.CommandText = "SELECT \"Preferred\", \"IncludePreferredWhenRenaming\", \"Enabled\", \"Id\" FROM \"ReleaseProfiles\" WHERE \"Preferred\" IS NOT NULL";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var preferred = reader.GetString(0);
                        var includeName = reader.GetBoolean(1);
                        var enabled = reader.GetBoolean(2);
                        var releaseProfileId = reader.GetInt32(3);

                        string name = null;

                        if (name.IsNullOrWhiteSpace())
                        {
                            name = $"Unnamed_{releaseProfileId}";
                        }
                        else
                        {
                            name = $"{name}_{releaseProfileId}";
                        }

                        var data = STJson.Deserialize<List<PreferredWord056>>(preferred);

                        var specs = new List<CustomFormatSpec057>();

                        var nameIdentifier = 0;

                        foreach (var term in data)
                        {
                            var regexTerm = term.Key
                                .TrimStart('/')
                                .TrimEnd('/')
                                .TrimEnd("/i");

                            // Validate Regex before creating a CF
                            try
                            {
                                Regex.Match("", regexTerm);
                            }
                            catch (ArgumentException)
                            {
                                continue;
                            }

                            updatedCollections.Add(new CustomFormat057
                            {
                                Name = data.Count > 1 ? $"{name}_{nameIdentifier++}" : name,
                                PreferredName = name,
                                IncludeCustomFormatWhenRenaming = includeName,
                                Score = term.Value,
                                Enabled = enabled,
                                Specifications = new List<CustomFormatSpec057>
                                {
                                    new CustomFormatSpec057
                                    {
                                        Type = "ReleaseTitleSpecification",
                                        Body = new CustomFormatReleaseTitleSpec057
                                        {
                                            Order = 1,
                                            ImplementationName = "Release Title",
                                            Name = regexTerm,
                                            Value = regexTerm
                                        }
                                    }
                                }.ToJson()
                            });
                        }
                    }
                }
            }

            // Insert Custom Formats
            var updateSql = "INSERT INTO \"CustomFormats\" (\"Name\", \"IncludeCustomFormatWhenRenaming\", \"Specifications\") VALUES (@Name, @IncludeCustomFormatWhenRenaming, @Specifications)";
            conn.Execute(updateSql, updatedCollections, transaction: tran);

            // Pull List of Custom Formats with new Ids
            var formats = new List<CustomFormat057>();
            using (var getProfiles = conn.CreateCommand())
            {
                getProfiles.Transaction = tran;
                getProfiles.CommandText = @"SELECT ""Id"", ""Name"" FROM ""CustomFormats""";

                using (var definitionsReader = getProfiles.ExecuteReader())
                {
                    while (definitionsReader.Read())
                    {
                        var id = definitionsReader.GetInt32(0);
                        var name = definitionsReader.GetString(1);
                        formats.Add(new CustomFormat057
                        {
                            Id = id,
                            Name = name
                        });
                    }
                }
            }

            // Update each profile with original scores
            foreach (var profile in qualityProfiles)
            {
                profile.FormatItems = formats.Select(x => new { Format = x.Id, Score = updatedCollections.First(f => f.Name == x.Name).Enabled ? updatedCollections.First(f => f.Name == x.Name).Score : 0 }).ToJson();
            }

            // Push profile updates to DB
            var updateProfilesSql = "UPDATE \"QualityProfiles\" SET \"FormatItems\" = @FormatItems WHERE \"Id\" = @Id";
            conn.Execute(updateProfilesSql, qualityProfiles, transaction: tran);
        }

        private void MigrateNamingConfigs(IDbConnection conn, IDbTransaction tran)
        {
            var updatedNamingConfigs = new List<object>();

            using (var namingConfigCmd = conn.CreateCommand())
            {
                namingConfigCmd.Transaction = tran;
                namingConfigCmd.CommandText = @"SELECT * FROM ""NamingConfig"" LIMIT 1";
                using (var namingConfigReader = namingConfigCmd.ExecuteReader())
                {
                    var standardTrackFormatIndex = namingConfigReader.GetOrdinal("StandardTrackFormat");
                    var multiDiscTrackFormatIndex = namingConfigReader.GetOrdinal("MultiDiscTrackFormat");

                    while (namingConfigReader.Read())
                    {
                        var standardTrackFormat = NameReplace(namingConfigReader.GetString(standardTrackFormatIndex));
                        var multiDiscTrackFormat = NameReplace(namingConfigReader.GetString(multiDiscTrackFormatIndex));

                        updatedNamingConfigs.Add(new
                        {
                            StandardTrackFormat = standardTrackFormat,
                            MultiDiscTrackFormat = multiDiscTrackFormat
                        });
                    }
                }
            }

            var updateProfileSql = "UPDATE \"NamingConfig\" SET \"StandardTrackFormat\" = @StandardTrackFormat, \"MultiDiscTrackFormat\" = @MultiDiscTrackFormat";
            conn.Execute(updateProfileSql, updatedNamingConfigs, transaction: tran);
        }

        private string NameReplace(string oldTokenString)
        {
            var newTokenString = oldTokenString.Replace("Preferred Words", "Custom Formats")
                                               .Replace("Preferred.Words", "Custom.Formats")
                                               .Replace("Preferred-Words", "Custom-Formats")
                                               .Replace("Preferred_Words", "Custom_Formats");

            return newTokenString;
        }

        private class PreferredWord056
        {
            public string Key { get; set; }
            public int Value { get; set; }
        }

        private class QualityProfile057
        {
            public int Id { get; set; }
            public string FormatItems { get; set; }
        }

        private class CustomFormat057
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string PreferredName { get; set; }
            public bool IncludeCustomFormatWhenRenaming { get; set; }
            public string Specifications { get; set; }
            public int Score { get; set; }
            public bool Enabled { get; set; }
        }

        private class CustomFormatSpec057
        {
            public string Type { get; set; }
            public CustomFormatReleaseTitleSpec057 Body { get; set; }
        }

        private class CustomFormatReleaseTitleSpec057
        {
            public int Order { get; set; }
            public string ImplementationName { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public bool Required { get; set; }
            public bool Negate { get; set; }
        }
    }
}

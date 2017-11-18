using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentMigrator;
using Newtonsoft.Json;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(4)]
    public class add_various_qualites_in_profile : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Execute.Sql("UPDATE QualityDefinitions SET Title = 'MP3-160' WHERE Quality = 5"); // Change MP3-512 to MP3-160
            Execute.WithConnection(ConvertProfile);
        }

        private void ConvertProfile(IDbConnection conn, IDbTransaction tran)
        {
            var updater = new ProfileUpdater3(conn, tran);

            updater.AddQuality(13);
            updater.MoveQuality(5, 0);
            updater.CreateNewGroup(0, 1000, "Trash Quality Lossy", new[] { 24, 25, 26, 27, 28, 29, 30, 31, 32 });
            updater.CreateGroupAt(5, 1001, "Poor Quality Lossy", new[] { 5, 19, 22, 23, 33 }); // Group Vorbis-Q5 with MP3-160
            updater.CreateGroupAt(1, 1002, "Low Quality Lossy", new[] { 1, 9, 18, 20, 34 }); // Group Vorbis-Q6, AAC 192, WMA with MP3-190
            updater.CreateGroupAt(3, 1003, "Mid Quality Lossy", new[] { 3, 8, 16, 17, 10 }); // Group Mp3-VBR-V2, Vorbis-Q7, Q8, AAC-256 with MP3-256
            updater.CreateGroupAt(4, 1004, "High Quality Lossy", new[] { 2, 4, 11, 12, 14, 15 }); // Group MP3-VBR-V0, AAC-VBR, Vorbis-Q10, Q9, AAC-320 with MP3-320
            updater.CreateGroupAt(6, 1005, "Lossless", new[] { 6, 7, 21 }); // Group ALAC with FLAC
            

            updater.Commit();
        }
    }

    public class Profile4
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Cutoff { get; set; }
        public List<ProfileItem4> Items { get; set; }
    }

    public class ProfileItem4
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Id { get; set; }

        public string Name { get; set; }
        public int? Quality { get; set; }
        public List<ProfileItem4> Items { get; set; }
        public bool Allowed { get; set; }

        public ProfileItem4()
        {
            Items = new List<ProfileItem4>();
        }
    }

    public class ProfileUpdater3
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;

        private List<Profile4> _profiles;
        private HashSet<Profile4> _changedProfiles = new HashSet<Profile4>();

        public ProfileUpdater3(IDbConnection conn, IDbTransaction tran)
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
                    updateProfileCmd.CommandText =
                        "UPDATE Profiles SET Name = ?, Cutoff = ?, Items = ? WHERE Id = ?";
                    updateProfileCmd.AddParameter(profile.Name);
                    updateProfileCmd.AddParameter(profile.Cutoff);
                    updateProfileCmd.AddParameter(profile.Items.ToJson());
                    updateProfileCmd.AddParameter(profile.Id);

                    updateProfileCmd.ExecuteNonQuery();
                }
            }

            _changedProfiles.Clear();
        }

        public void AddQuality(int quality)
        {
            foreach (var profile in _profiles)
            {
                profile.Items.Add(new ProfileItem4
                {
                    Quality = quality,
                    Allowed = false
                });
            }
            
        }

        public void CreateGroupAt(int find, int groupId, string name, int[] qualities)
        {
            foreach (var profile in _profiles)
            {
                var findIndex = profile.Items.FindIndex(v => v.Quality == find);

                if (findIndex > -1)
                {
                    var findQuality = profile.Items[findIndex];

                    profile.Items.Insert(findIndex, new ProfileItem4
                    {
                        Id = groupId,
                        Name = name,
                        Quality = null,
                        Items = qualities.Select(q => new ProfileItem4
                        {
                            Quality = q,
                            Allowed = findQuality.Allowed
                        }).ToList(),
                        Allowed = findQuality.Allowed
                    });
                }
                else
                {
                    // If the ID isn't found for some reason (mangled migration 71?)

                    profile.Items.Add(new ProfileItem4
                    {
                        Id = groupId,
                        Name = name,
                        Quality = null,
                        Items = qualities.Select(q => new ProfileItem4
                        {
                            Quality = q,
                            Allowed = false
                        }).ToList(),
                        Allowed = false
                    });
                }

                foreach (var quality in qualities)
                {
                    var index = profile.Items.FindIndex(v => v.Quality == quality);

                    if (index > -1)
                    {
                        profile.Items.RemoveAt(index);
                    }

                    if (profile.Cutoff == quality)
                    {
                        profile.Cutoff = groupId;
                    }
                }

                _changedProfiles.Add(profile);
            }
        }

        public void CreateNewGroup(int createafter, int groupId, string name, int[] qualities)
        {
            foreach (var profile in _profiles)
            {
                var findIndex = profile.Items.FindIndex(v => v.Quality == createafter) + 1;
                var allowed = profile.Name == "Any" ? true : false;

                if (findIndex > -1)
                {

                    profile.Items.Insert(findIndex, new ProfileItem4
                    {
                        Id = groupId,
                        Name = name,
                        Quality = null,
                        Items = qualities.Select(q => new ProfileItem4
                        {
                            Quality = q,
                            Allowed = false
                        }).ToList(),
                        Allowed = false
                    });
                }
                else
                {

                    profile.Items.Add(new ProfileItem4
                    {
                        Id = groupId,
                        Name = name,
                        Quality = null,
                        Items = qualities.Select(q => new ProfileItem4
                        {
                            Quality = q,
                            Allowed = false
                        }).ToList(),
                        Allowed = false
                    });
                }
            }
        }

        public void MoveQuality(int quality, int moveafter)
        {
            foreach (var profile in _profiles)
            {
                var findIndex = profile.Items.FindIndex(v => v.Quality == quality);

                if (findIndex > -1)
                {
                    var allowed = profile.Items[findIndex].Allowed;
                    profile.Items.RemoveAt(findIndex);
                    var findMoveIndex = profile.Items.FindIndex(v => v.Quality == moveafter) + 1;
                    profile.Items.Insert(findMoveIndex, new ProfileItem4
                    {
                        Quality = quality,
                        Allowed = allowed
                    });
                }

                
            }
        }

        private List<Profile4> GetProfiles()
        {
            var profiles = new List<Profile4>();

            using (var getProfilesCmd = _connection.CreateCommand())
            {
                getProfilesCmd.Transaction = _transaction;
                getProfilesCmd.CommandText = @"SELECT Id, Name, Cutoff, Items FROM Profiles";

                using (var profileReader = getProfilesCmd.ExecuteReader())
                {
                    while (profileReader.Read())
                    {
                        profiles.Add(new Profile4
                        {
                            Id = profileReader.GetInt32(0),
                            Name = profileReader.GetString(1),
                            Cutoff = profileReader.GetInt32(2),
                            Items = Json.Deserialize<List<ProfileItem4>>(profileReader.GetString(3))
                        });
                    }
                }
            }

            return profiles;
        }
    }
}

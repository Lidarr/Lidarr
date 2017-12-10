using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentMigrator;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Migration
{
    [Migration(7)]
    public class change_album_path_to_relative : NzbDroneMigrationBase
    {
        protected override void MainDbUpgrade()
        {
            Rename.Column("Path").OnTable("Albums").To("RelativePath");
            Execute.WithConnection(ConvertAlbums);
        }

        private void ConvertAlbums(IDbConnection conn, IDbTransaction tran)
        {
            var updater = new AlbumUpdater6(conn, tran);
            updater.Commit();
        }

    }

    public class Album6
    {
        public int Id { get; set; }
        public int ArtistId { get; set; }
        public string Path { get; set; }
    }

    public class Artist6
    {
        public int Id { get; set; }
        public string Path { get; set; }
    }

    public class AlbumUpdater6
    {
        private readonly IDbConnection _connection;
        private readonly IDbTransaction _transaction;

        private List<Album6> _albums;
        private List<Artist6> _artists;

        public AlbumUpdater6(IDbConnection conn, IDbTransaction tran)
        {
            _connection = conn;
            _transaction = tran;

            _albums = GetAlbums();
            _artists = GetArtists();
        }

        public void Commit()
        {
            foreach (var album in _albums)
            {
                var artist = _artists.SingleOrDefault(s => s.Id == album.ArtistId);

                if (artist == null) continue;

                using (var updateProfileCmd = _connection.CreateCommand())
                {
                    updateProfileCmd.Transaction = _transaction;
                    updateProfileCmd.CommandText =
                        "UPDATE Albums SET RelativePath = ? WHERE Id = ?";
                    updateProfileCmd.AddParameter(artist.Path.GetRelativePath(album.Path));
                    updateProfileCmd.AddParameter(album.Id);

                    updateProfileCmd.ExecuteNonQuery();
                }
            }
        }

        private List<Album6> GetAlbums()
        {
            var albums = new List<Album6>();

            using (var getProfilesCmd = _connection.CreateCommand())
            {
                getProfilesCmd.Transaction = _transaction;
                getProfilesCmd.CommandText = @"SELECT Id, ArtistId, RelativePath FROM Albums";

                using (var albumReader = getProfilesCmd.ExecuteReader())
                {
                    while (albumReader.Read())
                    {
                        albums.Add(new Album6
                        {
                            Id = albumReader.GetInt32(0),
                            ArtistId = albumReader.GetInt32(1),
                            Path = albumReader.GetString(2)
                        });
                    }
                }
            }

            return albums;
        }

        private List<Artist6> GetArtists()
        {
            var artists = new List<Artist6>();

            using (var getProfilesCmd = _connection.CreateCommand())
            {
                getProfilesCmd.Transaction = _transaction;
                getProfilesCmd.CommandText = @"SELECT Id, Path FROM Artists";

                using (var artistReader = getProfilesCmd.ExecuteReader())
                {
                    while (artistReader.Read())
                    {
                        artists.Add(new Artist6
                        {
                            Id = artistReader.GetInt32(0),
                            Path = artistReader.GetString(1)
                        });
                    }
                }
            }

            return artists;
        }
    }
}

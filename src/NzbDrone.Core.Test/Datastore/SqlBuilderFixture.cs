using System;
using Dapper;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Music;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class SqlBuilderFixture : CoreTest
    {
        [OneTimeSetUp]
        public void MapTables()
        {
            Mocker.Resolve<DbFactory>();
        }

        [TestCase(DatabaseType.SQLite)]
        [TestCase(DatabaseType.PostgreSQL)]
        public void where_exists_should_not_reuse_parameter_names(DatabaseType databaseType)
        {
            var releaseDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var template = new SqlBuilder(databaseType)
                .Join<Album, Artist>((l, r) => l.ArtistMetadataId == r.ArtistMetadataId)
                .Where<Album>(a => a.ReleaseDate <= releaseDate)
                .WhereExists<AlbumRelease>(s => s
                    .Join<AlbumRelease, Track>((r, t) => r.Id == t.AlbumReleaseId)
                    .Where<AlbumRelease, Album>((r, a) => r.AlbumId == a.Id)
                    .Where<AlbumRelease>(r => r.Monitored == true)
                    .Where<Track>(t => t.TrackFileId == 0))
                .Where<Artist>(a => a.QualityProfileId == 5)
                .AddTemplate("SELECT \"Albums\".* FROM \"Albums\" /**join**/ /**where**/");

            template.RawSql.Should().Contain("EXISTS (SELECT 1 FROM \"AlbumReleases\"");
            template.RawSql.Should().Contain("(\"AlbumReleases\".\"AlbumId\" = \"Albums\".\"Id\")");

            var parameters = (DynamicParameters)template.Parameters;

            parameters.ParameterNames.Should().HaveCount(4).And.OnlyHaveUniqueItems();
            parameters.Get<DateTime>("Clause2_P1").Should().Be(releaseDate);
            parameters.Get<bool>("Clause5_P1").Should().BeTrue();
            parameters.Get<int>("Clause6_P1").Should().Be(0);
            parameters.Get<int>("Clause8_P1").Should().Be(5);
        }
    }
}

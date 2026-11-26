using System.IO;
using FizzWare.NBuilder;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Test.Common.Datastore;

namespace NzbDrone.Core.Test
{
    [SetUpFixture]
    public class RemoveCachedDatabase
    {
        [OneTimeSetUp]
        [OneTimeTearDown]
        public void ClearCachedDatabase()
        {
            BuilderSetup.DisablePropertyNamingFor<Artist, string>(a => a.PreferredName);

            var mainCache = SqliteDatabase.GetCachedDb(MigrationType.Main);
            if (File.Exists(mainCache))
            {
                File.Delete(mainCache);
            }

            var logCache = SqliteDatabase.GetCachedDb(MigrationType.Log);
            if (File.Exists(logCache))
            {
                File.Delete(logCache);
            }
        }
    }
}

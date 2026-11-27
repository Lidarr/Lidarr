using System;
using System.Collections.Generic;
using System.Data.SQLite;
using Dapper;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Housekeeping;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.Housekeeping
{
    [TestFixture]
    public class HousekeepingServiceFixture : CoreTest<HousekeepingService>
    {
        [SetUp]
        public void Setup()
        {
            InitLogging();

            Mocker.SetConstant<IEnumerable<IHousekeepingTask>>(
                Array.Empty<IHousekeepingTask>());

            Mocker.GetMock<IMainDatabase>()
                  .SetupGet(x => x.DatabaseType)
                  .Returns(DatabaseType.SQLite);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void should_vacuum_based_on_free_pages(bool hasFreePages)
        {
            using var db = new SQLiteConnection("Data Source=:memory:;");
            db.Open();

            db.Execute("PRAGMA auto_vacuum = NONE;");
            db.Execute("CREATE TABLE Test (Data BLOB);");
            db.Execute("INSERT INTO Test VALUES (zeroblob(65536));");

            if (hasFreePages)
            {
                db.Execute("DELETE FROM Test;");
            }

            Mocker.GetMock<IMainDatabase>()
                  .Setup(x => x.OpenConnection())
                  .Returns(db);

            Subject.Execute(new HousekeepingCommand());

            Mocker.GetMock<IMainDatabase>()
                  .Verify(x => x.Vacuum(),
                      hasFreePages ? Times.Once() : Times.Never());
        }

        [Test]
        public void should_vacuum_postgresql_without_checking_free_pages()
        {
            Mocker.GetMock<IMainDatabase>()
                  .SetupGet(x => x.DatabaseType)
                  .Returns(DatabaseType.PostgreSQL);

            Subject.Execute(new HousekeepingCommand());

            Mocker.GetMock<IMainDatabase>()
                  .Verify(x => x.Vacuum(), Times.Once());

            Mocker.GetMock<IMainDatabase>()
                  .Verify(x => x.OpenConnection(), Times.Never());
        }

        [Test]
        public void should_skip_vacuum_when_evaluation_fails()
        {
            Mocker.GetMock<IMainDatabase>()
                  .Setup(x => x.OpenConnection())
                  .Throws(new InvalidOperationException());

            Subject.Execute(new HousekeepingCommand());

            Mocker.GetMock<IMainDatabase>()
                .Verify(x => x.OpenConnection(), Times.Once());

            Mocker.GetMock<IMainDatabase>()
                  .Verify(x => x.Vacuum(), Times.Never());

            ExceptionVerification.ExpectedWarns(1);
        }
    }
}

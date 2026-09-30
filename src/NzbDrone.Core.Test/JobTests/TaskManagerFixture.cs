using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Backup;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Jobs;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.JobTests
{
    [TestFixture]
    public class TaskManagerFixture : DbTest<TaskManager, ScheduledTask>
    {
        [SetUp]
        public void SetUp()
        {
            Mocker.SetConstant<ICacheManager>(Mocker.Resolve<CacheManager>());
            Mocker.SetConstant<IScheduledTaskRepository>(Mocker.Resolve<ScheduledTaskRepository>());
        }

        [Test]
        public void should_persist_custom_interval_and_reset_to_default()
        {
            Subject.Handle(new ApplicationStartedEvent());

            var backupTask = Subject.GetAll().Single(task => task.TypeName == typeof(BackupCommand).FullName);

            Subject.SetInterval(backupTask.Id, 120);

            var storedTask = Db.All<ScheduledTask>().Single(task => task.Id == backupTask.Id);
            storedTask.Interval.Should().Be(120);
            storedTask.IsUserConfigured.Should().BeTrue();

            Subject.ResetInterval(backupTask.Id);

            storedTask = Db.All<ScheduledTask>().Single(task => task.Id == backupTask.Id);
            storedTask.Interval.Should().Be(7 * 24 * 60);
            storedTask.IsUserConfigured.Should().BeFalse();
        }

        [Test]
        public void should_preserve_custom_intervals_and_refresh_default_intervals_on_startup()
        {
            Storage.Insert(new ScheduledTask
            {
                TypeName = typeof(BackupCommand).FullName,
                Interval = 120,
                IsUserConfigured = true
            });
            Storage.Insert(new ScheduledTask
            {
                TypeName = typeof(RssSyncCommand).FullName,
                Interval = 60,
                IsUserConfigured = false
            });

            Subject.Handle(new ApplicationStartedEvent());

            var tasks = Db.All<ScheduledTask>().ToList();
            tasks.Single(task => task.TypeName == typeof(BackupCommand).FullName).Interval.Should().Be(120);
            tasks.Single(task => task.TypeName == typeof(RssSyncCommand).FullName).Interval.Should().Be(15);
        }

        [Test]
        public void should_update_rss_sync_configuration_when_interval_changes_or_resets()
        {
            Subject.Handle(new ApplicationStartedEvent());

            var rssTask = Subject.GetAll().Single(task => task.TypeName == typeof(RssSyncCommand).FullName);

            Subject.SetInterval(rssTask.Id, 30);

            Mocker.GetMock<IConfigService>().VerifySet(config => config.RssSyncInterval = 30);

            Subject.ResetInterval(rssTask.Id);

            Mocker.GetMock<IConfigService>().VerifySet(config => config.RssSyncInterval = 15);
        }
    }
}

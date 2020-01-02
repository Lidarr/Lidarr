using System;
using System.Data;
using FluentAssertions;
using Marr.Data.Converters;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Music.Commands;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Converters
{
    [TestFixture]
    public class CommandConverterFixture : CoreTest<CommandConverter>
    {
        [Test]
        public void should_return_json_string_when_saving_boolean_to_db()
        {
            var command = new RefreshArtistCommand();

            Subject.ToDB(command).Should().BeOfType<string>();
        }

        [Test]
        public void should_return_null_for_null_value_when_saving_to_db()
        {
            Subject.ToDB(null).Should().Be(null);
        }

        [Test]
        public void should_return_db_null_for_db_null_value_when_saving_to_db()
        {
            Subject.ToDB(DBNull.Value).Should().Be(DBNull.Value);
        }

        [Test]
        public void should_return_command_when_getting_json_from_db()
        {
            var dataRecordMock = new Mock<IDataRecord>();
            dataRecordMock.Setup(s => s.GetOrdinal("Name")).Returns(0);
            dataRecordMock.Setup(s => s.GetString(0)).Returns("RefreshArtist");

            var context = new ConverterContext
            {
                DataRecord = dataRecordMock.Object,
                DbValue = new RefreshArtistCommand().ToJson()
            };

            Subject.FromDB(context).Should().BeOfType<RefreshArtistCommand>();
        }

        [Test]
        public void should_return_null_for_null_value_when_getting_from_db()
        {
            var context = new ConverterContext
            {
                DbValue = DBNull.Value
            };

            Subject.FromDB(context).Should().Be(null);
        }
    }
}

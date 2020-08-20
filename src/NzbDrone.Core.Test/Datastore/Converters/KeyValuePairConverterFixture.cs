using System.Collections.Generic;
using System.Data.SQLite;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Converters
{
    [TestFixture]
    public class KeyValuePairConverterFixture : CoreTest<EmbeddedDocumentConverter<List<KeyValuePair<string, int>>>>
    {
        private SQLiteParameter _param;

        [SetUp]
        public void Setup()
        {
            _param = new SQLiteParameter();
        }

        [Test]
        public void should_serialize_in_camel_case()
        {
            var items = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("word", 1)
            };

            Subject.SetValue(_param, items);

            var result = (string)_param.Value;
            result.Should().Be(@"[
  {
    ""key"": ""word"",
    ""value"": 1
  }
]");
        }

        [TestCase(@"[{""key"": ""deluxe"", ""value"": 10 }]")]
        [TestCase(@"[{""Key"": ""deluxe"", ""Value"": 10 }]")]
        public void should_deserialize_case_insensitive(string input)
        {
            Subject.Parse(input).Should().BeEquivalentTo(new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>("deluxe", 10) });
        }
    }
}

using System;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Plugins;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.PluginTests
{
    [TestFixture]
    public class PluginVersionFixture : CoreTest
    {
        [TestCase("2.1.1-master+a8ab7d71074a55e5651a4880a7c569252e5ad433")]
        [TestCase("2.1.1-main+a8ab7d7")]
        [TestCase("v2.1.1-Master")]
        [TestCase("2.1.1")]
        public void should_ignore_default_tree_suffix(string versionString)
        {
            var version = PluginVersion.Parse(versionString);

            version.BaseVersion.Should().Be(new Version(2, 1, 1));
            version.HasSuffix.Should().BeFalse();
        }

        [Test]
        public void should_keep_non_default_tree_suffix()
        {
            var version = PluginVersion.Parse("2.1.1-develop+a8ab7d7");

            version.Suffix.Should().Be("develop");
        }

        [Test]
        public void installed_default_tree_build_should_not_be_older_than_same_remote_release()
        {
            var installed = PluginVersion.Parse("2.1.1-master+a8ab7d7");
            var remote = new PluginVersion(new Version(2, 1, 1));

            (remote > installed).Should().BeFalse();
            installed.Should().Be(remote);
        }

        [Test]
        public void should_still_treat_prerelease_as_older_than_release()
        {
            var prerelease = PluginVersion.Parse("2.1.1-beta");
            var release = PluginVersion.Parse("2.1.1");

            (release > prerelease).Should().BeTrue();
        }
    }
}

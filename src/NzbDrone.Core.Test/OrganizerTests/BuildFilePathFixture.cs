using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.OrganizerTests;

[TestFixture]

public class BuildFilePathFixture : CoreTest<FileNameBuilder>
{
    private NamingConfig _namingConfig;

    [SetUp]
    public void Setup()
    {
        _namingConfig = NamingConfig.Default;

        Mocker.GetMock<INamingConfigService>()
              .Setup(c => c.GetConfig()).Returns(_namingConfig);
    }

    [Test]
    public void should_clean_artist_folder_when_it_contains_illegal_characters_in_album_or_artist_title()
    {
        var filename = @"02 - Track Title";
        var expectedPath = @"C:\Test\Fake- The Artist\02 - Track Title.flac";

        var fakeArtist = Builder<Artist>.CreateNew()
                                        .With(s => s.Name = "Fake: The Artist")
                                        .With(s => s.Path = @"C:\Test\Fake- The Artist".AsOsAgnostic())
                                        .Build();

        Subject.BuildTrackFilePath(fakeArtist, filename, ".flac").Should().Be(expectedPath.AsOsAgnostic());
    }
}

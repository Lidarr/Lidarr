using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Music;
using NzbDrone.Core.Notifications.Xbmc;
using NzbDrone.Core.Notifications.Xbmc.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.NotificationTests.Xbmc
{
    [TestFixture]
    public class UpdateFixture : CoreTest<XbmcService>
    {
        private const string MB_ID = "9f4e41c3-2648-428e-b8c7-dc10465b49ac";
        private XbmcSettings _settings;
        private List<KodiArtist> _xbmcArtist;
        private List<KodiSource> _xbmcSources;

        [SetUp]
        public void Setup()
        {
            _settings = Builder<XbmcSettings>.CreateNew()
                                             .Build();

            _xbmcArtist = Builder<KodiArtist>.CreateListOfSize(3)
                .TheFirst(1)
                .With(s => s.MusicbrainzArtistId = new List<string> { MB_ID.ToString() })
                .With(s => s.SourceId = new List<int> { 1 })
                .TheNext(2)
                .With(s => s.MusicbrainzArtistId = new List<string>())
                .Build()
                .ToList();

            _xbmcSources = Builder<KodiSource>.CreateListOfSize(1)
                 .All()
                 .With(s => s.SourceId = _xbmcArtist.First().SourceId.First())
                 .Build()
                 .ToList();

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Setup(s => s.GetArtist(_settings))
                  .Returns(_xbmcArtist);

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Setup(s => s.GetSources(_settings))
                  .Returns(_xbmcSources);

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Setup(s => s.GetActivePlayers(_settings))
                  .Returns(new List<ActivePlayer>());
        }

        [Test]
        public void should_update_using_artist_path()
        {
            var artist = Builder<Music.Artist>.CreateNew()
                                        .With(s => s.ForeignArtistId = MB_ID)
                                        .Build();

            Subject.Update(_settings, artist);

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Verify(v => v.UpdateLibrary(_settings, It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_update_all_paths_when_artist_path_not_found()
        {
            var fakeArtist = Builder<Artist>.CreateNew()
                                            .With(s => s.ForeignArtistId = "9f4e41c3-2648-428e-b8c7-dc10465b49ad")
                                            .With(s => s.Name = "Not Shawn Desman")
                                            .Build();

            Subject.Update(_settings, fakeArtist);

            Mocker.GetMock<IXbmcJsonApiProxy>()
                  .Verify(v => v.UpdateLibrary(_settings, null), Times.Once());
        }
    }
}

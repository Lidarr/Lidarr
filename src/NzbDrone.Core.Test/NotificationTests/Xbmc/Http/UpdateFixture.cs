using FizzWare.NBuilder;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Notifications.Xbmc;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.Test.NotificationTests.Xbmc.Http
{
    [TestFixture]
    public class UpdateFixture : CoreTest<HttpApiProvider>
    {
        private XbmcSettings _settings;
        private string _artistQueryUrl = "http://localhost:8080/xbmcCmds/xbmcHttp?command=QueryMusicDatabase(select path.strPath from path, artist, artistlinkpath where artist.c12 = 123d45d-d154f5d-1f5d1-5df18d5 and artistlinkpath.idArtist = artist.idArtist and artistlinkpath.idPath = path.idPath)";
        private Artist _fakeArtist;

        [SetUp]
        public void Setup()
        {
            _settings = new XbmcSettings
            {
                Host = "localhost",
                Port = 8080,
                Username = "xbmc",
                Password = "xbmc",
                AlwaysUpdate = false,
                CleanLibrary = false,
                UpdateLibrary = true
            };

            _fakeArtist = Builder<Artist>.CreateNew()
                                         .With(s => s.ForeignArtistId = "123d45d-d154f5d-1f5d1-5df18d5")
                                         .With(s => s.Name = "30 Rock")
                                         .Build();
        }

        private void WithSeriesPath()
        {
            Mocker.GetMock<IHttpProvider>()
                  .Setup(s => s.DownloadString(_artistQueryUrl, _settings.Username, _settings.Password))
                  .Returns("<xml><record><field>smb://xbmc:xbmc@HOMESERVER/Music/30 Rock/</field></record></xml>");
        }

        private void WithoutSeriesPath()
        {
            Mocker.GetMock<IHttpProvider>()
                  .Setup(s => s.DownloadString(_artistQueryUrl, _settings.Username, _settings.Password))
                  .Returns("<xml></xml>");
        }

        [Test]
        public void should_update_using_artist_path()
        {
            WithSeriesPath();
            const string url = "http://localhost:8080/xbmcCmds/xbmcHttp?command=ExecBuiltIn(UpdateLibrary(music,smb://xbmc:xbmc@HOMESERVER/Music/30 Rock/))";

            Mocker.GetMock<IHttpProvider>().Setup(s => s.DownloadString(url, _settings.Username, _settings.Password));

            Subject.Update(_settings, _fakeArtist);
            Mocker.VerifyAllMocks();
        }

        [Test]
        public void should_update_all_paths_when_artist_path_not_found()
        {
            WithoutSeriesPath();
            const string url = "http://localhost:8080/xbmcCmds/xbmcHttp?command=ExecBuiltIn(UpdateLibrary(music))";

            Mocker.GetMock<IHttpProvider>().Setup(s => s.DownloadString(url, _settings.Username, _settings.Password));

            Subject.Update(_settings, _fakeArtist);
            Mocker.VerifyAllMocks();
        }
    }
}

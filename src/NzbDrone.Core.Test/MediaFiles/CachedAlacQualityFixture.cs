using System;
using System.Collections.Generic;
using System.IO;
using Moq;
using NLog;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using ImportUpgrade = NzbDrone.Core.MediaFiles.TrackImport.Specifications.UpgradeSpecification;

namespace NzbDrone.Core.Test.MediaFiles
{
    [TestFixture]
    public class CachedAlacQualityFixture
    {
        private Mock<IMediaFileRepository> _repository;
        private TrackFileQualityService _verifier;
        private Logger _logger;
        private string _fixtures;

        [SetUp]
        public void Setup()
        {
            _logger = LogManager.GetLogger("ALAC cache tests");
            _repository = new Mock<IMediaFileRepository>();
            _verifier = new TrackFileQualityService(_repository.Object, _logger);
            _fixtures = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Media");
        }

        private TrackFile Cached(string fixture = "alac-24-96000.m4a") => new()
        {
            Id = 1,
            Path = Path.Combine(_fixtures, fixture),
            Quality = new QualityModel(Quality.ALAC, new Revision(2, 1)),
            MediaInfo = new MediaInfoModel { AudioFormat = "MPEG-4 Audio (alac)", AudioBits = 0 }
        };

        private QualityProfile Profile() => new()
        {
            UpgradeAllowed = true,
            Cutoff = Quality.ALAC_24.Id,
            Items = new() { new() { Quality = Quality.FLAC, Allowed = true }, new() { Quality = Quality.ALAC, Allowed = true }, new() { Quality = Quality.FLAC_24, Allowed = true }, new() { Quality = Quality.ALAC_24, Allowed = true } }
        };

        private IConfigService Config()
        {
            var mock = new Mock<IConfigService>();
            mock.SetupGet(x => x.DownloadPropersAndRepacks).Returns(ProperDownloadTypes.PreferAndUpgrade);
            return mock.Object;
        }

        [Test]
        public void ReclassifiesAndPersistsAnOldAlacEntryOnlyOnce()
        {
            var file = Cached();
            var revision = file.Quality.Revision;
            _verifier.EnsureCurrent(file);
            _verifier.EnsureCurrent(file);
            Assert.That(file.Quality.Quality, Is.EqualTo(Quality.ALAC_24));
            Assert.That(file.Quality.Revision, Is.SameAs(revision));
            Assert.That(file.MediaInfo.AudioBits, Is.EqualTo(24));
            Assert.That(file.MediaInfo.AudioSampleRate, Is.EqualTo(96000));
            Assert.That(file.MediaInfo.AudioBitrate, Is.GreaterThan(0));
            _repository.Verify(x => x.Update(file), Times.Once);
        }

        [Test]
        public void Keeps16BitAlacInItsCorrectTier()
        {
            var file = Cached("alac-16-44100.m4a");
            _verifier.EnsureCurrent(file);
            Assert.That(file.Quality.Quality, Is.EqualTo(Quality.ALAC));
            Assert.That(file.MediaInfo.AudioBits, Is.EqualTo(16));
            _repository.Verify(x => x.Update(file), Times.Once);
        }

        [Test]
        public void LeavesOtherFormatsAndAlreadyVerifiedAlacAlone()
        {
            _verifier.EnsureCurrent(new TrackFile { Quality = new QualityModel(Quality.FLAC), Path = "must-not-read" });
            _verifier.EnsureCurrent(new TrackFile { Quality = new QualityModel(Quality.ALAC_24), MediaInfo = new MediaInfoModel { AudioBits = 24 }, Path = "must-not-read" });
            _verifier.EnsureCurrent(null);
            _repository.Verify(x => x.Update(It.IsAny<TrackFile>()), Times.Never);
        }

        [Test]
        public void DoesNotAcceptUnverifiableCachedMetadata()
        {
            var file = Cached("does-not-exist.m4a");
            Assert.Catch(() => _verifier.EnsureCurrent(file));
            Assert.That(file.Quality.Quality, Is.EqualTo(Quality.ALAC));
            _repository.Verify(x => x.Update(It.IsAny<TrackFile>()), Times.Never);
        }

        [Test]
        public void RejectsAReadableFileWhoseLosslessBitDepthCannotBeVerified()
        {
            // A filename and old cache entry are not evidence of lossless quality.
            // This valid AAC fixture is readable but has no lossless bit depth.
            var file = Cached("aac-16.m4a");
            var oldInfo = file.MediaInfo;
            Assert.Throws<InvalidOperationException>(() => _verifier.EnsureCurrent(file));
            Assert.That(file.Quality.Quality, Is.EqualTo(Quality.ALAC));
            Assert.That(file.MediaInfo, Is.SameAs(oldInfo));
            _repository.Verify(x => x.Update(It.IsAny<TrackFile>()), Times.Never);
        }

        [Test]
        public void RejectsAFileWithoutAnAudioCodec()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");

            // A small PNG is valid media but supplies no audio codec at all.
            File.WriteAllBytes(path, Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Wl6G9sAAAAASUVORK5CYII="));
            try
            {
                var file = Cached(path);
                Assert.Throws<InvalidOperationException>(() => _verifier.EnsureCurrent(file));
                _repository.Verify(x => x.Update(It.IsAny<TrackFile>()), Times.Never);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void RejectsCorruptAlacEvenWhenItsDecoderConfigurationLooksValid()
        {
            var bytes = File.ReadAllBytes(Path.Combine(_fixtures, "alac-24-96000.m4a"));
            var header = System.Text.Encoding.ASCII.GetString(bytes).IndexOf("mdhd", StringComparison.Ordinal);
            Assert.That(header, Is.GreaterThan(0));
            bytes[header + 4] = 2;

            // Unsupported media-header version, valid ALAC cookie.
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".m4a");
            File.WriteAllBytes(path, bytes);
            try
            {
                var file = Cached(path);
                Assert.Throws<InvalidOperationException>(() => _verifier.EnsureCurrent(file));
                Assert.That(file.MediaInfo.AudioBits, Is.EqualTo(0));
                _repository.Verify(x => x.Update(It.IsAny<TrackFile>()), Times.Never);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Test]
        public void VerifiesAnUnsavedRecordWithoutWritingToTheRepository()
        {
            var file = Cached();
            file.Id = 0;
            file.MediaInfo = null;
            _verifier.EnsureCurrent(file);
            Assert.That(file.Quality.Quality, Is.EqualTo(Quality.ALAC_24));
            Assert.That(file.MediaInfo.AudioBits, Is.EqualTo(24));
            _repository.Verify(x => x.Update(It.IsAny<TrackFile>()), Times.Never);
        }

        [Test]
        public void SkipsEntriesThatHaveNoQualityClassification()
        {
            _verifier.EnsureCurrent(new TrackFile());
            _verifier.EnsureCurrent(new TrackFile { Quality = new QualityModel { Quality = null } });
            _repository.Verify(x => x.Update(It.IsAny<TrackFile>()), Times.Never);
        }

        [Test]
        public void VerifiesMissingDepthEvenWhenTheCachedTierAlreadySays24Bit()
        {
            var file = Cached("alac-16-44100.m4a");
            file.Quality = new QualityModel(Quality.ALAC_24);
            _verifier.EnsureCurrent(file);
            Assert.That(file.Quality.Quality, Is.EqualTo(Quality.ALAC));
            Assert.That(file.MediaInfo.AudioBits, Is.EqualTo(16));
            _repository.Verify(x => x.Update(file), Times.Once);
        }

        [TestCase(6)]
        [TestCase(7)]
        [TestCase(21)]
        public void RejectsImportDowngradesEvenWhenExistingAlacWasCachedAsRegular(int incomingId)
        {
            var file = Cached();
            var artist = new Artist { QualityProfile = Profile() };
            var local = new LocalTrack { Artist = artist, Quality = new QualityModel((Quality)incomingId), Path = "candidate", Tracks = new() { new Track { TrackFileId = 1, TrackFile = file } } };
            var rule = new ImportUpgrade(Config(), null, _verifier, _logger);
            Assert.That(rule.IsSatisfiedBy(local, null).Accepted, Is.False);
            Assert.That(file.Quality.Quality, Is.EqualTo(Quality.ALAC_24));
        }

        [TestCase(6)]
        [TestCase(7)]
        [TestCase(21)]
        public void RejectsLowerQualityWhenChangingAlbumEdition(int incomingId)
        {
            // Edition changes use a separate album-level comparison. It must verify
            // the same stale ALAC cache before choosing the minimum existing quality.
            var file = Cached();
            var album = new Album { Id = 1, Artist = new Artist { QualityProfile = Profile() } };
            var current = new AlbumRelease { Id = 1, Monitored = true, Album = album, Tracks = new List<Track> { new Track { TrackFileId = 1, TrackFile = file } } };
            var incoming = new AlbumRelease { Id = 2, Album = album };
            album.AlbumReleases = new List<AlbumRelease> { current, incoming };
            var local = new LocalAlbumRelease { AlbumRelease = incoming, LocalTracks = new List<LocalTrack> { new LocalTrack { Quality = new QualityModel((Quality)incomingId) } } };
            var rule = new NzbDrone.Core.MediaFiles.TrackImport.Specifications.AlbumUpgradeSpecification(_logger, _verifier);
            Assert.That(rule.IsSatisfiedBy(local, null).Accepted, Is.False);
            Assert.That(file.Quality.Quality, Is.EqualTo(Quality.ALAC_24));
        }

        [TestCase(6)]
        [TestCase(7)]
        [TestCase(21)]
        public void RejectsDownloadDowngradesWithTheOldCachedClassification(int incomingId)
        {
            var file = Cached();
            var media = new Mock<IMediaFileService>();
            media.Setup(x => x.GetFilesByAlbum(1)).Returns(new List<TrackFile> { file });
            var tracks = new Mock<ITrackService>();
            tracks.Setup(x => x.TracksWithoutFiles(1)).Returns(new List<Track>());
            var formats = new Mock<ICustomFormatCalculationService>();
            formats.Setup(x => x.ParseCustomFormat(It.IsAny<TrackFile>())).Returns(new List<CustomFormat>());
            var rule = new UpgradeDiskSpecification(new UpgradableSpecification(Config(), _logger), media.Object, _verifier, tracks.Object, new CacheManager(), formats.Object, _logger);
            var album = new RemoteAlbum { Artist = new Artist { QualityProfile = Profile() }, Albums = new() { new Album { Id = 1 } }, ParsedAlbumInfo = new ParsedAlbumInfo { Quality = new QualityModel((Quality)incomingId) } };
            Assert.That(rule.IsSatisfiedBy(album, null).Accepted, Is.False);
            Assert.That(file.Quality.Quality, Is.EqualTo(Quality.ALAC_24));
        }
    }
}

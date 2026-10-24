using System;
using System.Linq;
using NLog;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using TagLib;

namespace NzbDrone.Core.MediaFiles
{
    public interface ITrackFileQualityService
    {
        void EnsureCurrent(TrackFile trackFile);
    }

    // Older TagLib builds cached ALAC with no bit depth. Verify those entries on
    // demand before an upgrade decision; no library-wide rescan is necessary.
    public class TrackFileQualityService : ITrackFileQualityService
    {
        private readonly IMediaFileRepository _repository;
        private readonly Logger _logger;

        public TrackFileQualityService(IMediaFileRepository repository, Logger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public void EnsureCurrent(TrackFile trackFile)
        {
            if (trackFile?.Quality?.Quality == null ||
                (trackFile.Quality.Quality != Quality.ALAC && trackFile.Quality.Quality != Quality.ALAC_24) ||
                (trackFile.MediaInfo?.AudioBits ?? 0) != 0)
            {
                return;
            }

            using (var file = TagLib.File.Create(trackFile.Path))
            {
                var audio = file.Properties.Codecs.OfType<IAudioCodec>().FirstOrDefault();
                if (audio == null || file.PossiblyCorrupt || file.Properties.BitsPerSample == 0)
                {
                    // Do not accept a possible downgrade when the old file's
                    // lossless quality could not be established reliably.
                    throw new InvalidOperationException($"Unable to verify cached ALAC quality: {trackFile.Path}");
                }

                var quality = QualityParser.ParseQuality(trackFile.Path, audio.Description, audio.AudioBitrate, file.Properties.BitsPerSample);

                // Correct the codec tier without discarding release/proper
                // revisions that Lidarr already recorded for this same file.
                quality.Revision = trackFile.Quality.Revision;
                var info = new MediaInfoModel
                {
                    AudioFormat = audio.Description,
                    AudioBitrate = audio.AudioBitrate,
                    AudioChannels = audio.AudioChannels,
                    AudioBits = file.Properties.BitsPerSample,
                    AudioSampleRate = audio.AudioSampleRate
                };

                var oldQuality = trackFile.Quality;
                trackFile.Quality = quality;
                trackFile.MediaInfo = info;

                // Persist only an existing database entry. This changes the
                // cached classification, never the audio file or its tags.
                if (trackFile.Id > 0)
                {
                    _repository.Update(trackFile);
                }

                _logger.Debug("Verified cached ALAC quality for {0}: {1} -> {2}", trackFile.Path, oldQuality, trackFile.Quality);
            }
        }
    }
}

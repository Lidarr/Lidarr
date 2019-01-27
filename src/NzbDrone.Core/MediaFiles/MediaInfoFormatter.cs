using NLog;
using NLog.Fluent;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public static class MediaInfoFormatter
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(MediaInfoFormatter));

        public static string FormatAudioBitrate(MediaInfoModel mediaInfo)
        {
            return mediaInfo.AudioBitrate + " kbps";
        }

        public static string FormatAudioBitsPerSample(MediaInfoModel mediaInfo)
        {
            return mediaInfo.AudioBits + "bit";
        }

        public static string FormatAudioSampleRate(MediaInfoModel mediaInfo)
        {
            return $"{mediaInfo.AudioSampleRate / 1000:0.#}kHz";
        }

        public static decimal FormatAudioChannels(MediaInfoModel mediaInfo)
        {
            return mediaInfo.AudioChannels;
        }

        public static string FormatAudioCodec(MediaInfoModel mediaInfo)
        {
            var codec = QualityParser.ParseCodec(mediaInfo.AudioFormat, null);

            if (codec == Codec.AAC || codec == Codec.AACVBR)
            {
                return "AAC";
            }

            if (codec == Codec.ALAC)
            {
                return "ALAC";
            }

            if (codec == Codec.APE)
            {
                return "APE";
            }

            if (codec == Codec.FLAC)
            {
                return "FLAC";
            }

            if (codec == Codec.MP3CBR || codec == Codec.MP3VBR)
            {
                return "MP3";
            }
            
            if (codec == Codec.OGG)
            {
                return "OGG";
            }
            
            if (codec == Codec.WAV)
            {
                return "PCM";
            }

            if (codec == Codec.WAVPACK)
            {
                return "WavPack";
            }

            if (codec == Codec.WMA)
            {
                return "WMA";
            }

            Logger.Debug()
                  .Message("Unknown audio format: '{0}'.", string.Join(", ", mediaInfo.AudioFormat))
                  .WriteSentryWarn("UnknownAudioFormat", mediaInfo.AudioFormat)
                  .Write();

            return "Unknown";
        }
    }
}

using System;
using System.Globalization;
using System.Linq;
using NLog;
using NLog.Fluent;
using NzbDrone.Common.Extensions;
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
            int audioBitrate = mediaInfo.SchemaRevision == -1 ? mediaInfo.AudioBitrate : (mediaInfo.AudioBitrate / 1000);

            return audioBitrate + " kbps";
        }

        public static decimal FormatAudioChannels(MediaInfoModel mediaInfo)
        {
            return mediaInfo.AudioChannels;
        }

        public static string FormatAudioCodec(MediaInfoModel mediaInfo)
        {
            if (mediaInfo.SchemaRevision == -1)
            {
                return FormatAudioCodecTaglib(mediaInfo);
            }

            return FormatAudioCodecLegacy(mediaInfo);
        }

        public static string FormatAudioCodecLegacy(MediaInfoModel mediaInfo)
        {
            var audioFormat = mediaInfo.AudioFormat;

            if (audioFormat.IsNullOrWhiteSpace())
            {
                return audioFormat;
            }

            if (audioFormat.EqualsIgnoreCase("AC-3"))
            {
                return "AC3";
            }

            if (audioFormat.EqualsIgnoreCase("ALAC"))
            {
                return "ALAC";
            }

            if (audioFormat.EqualsIgnoreCase("E-AC-3"))
            {
                return "EAC3";
            }

            if (audioFormat.EqualsIgnoreCase("AAC"))
            {
                return "AAC";
            }

            if (audioFormat.EqualsIgnoreCase("MPEG Audio"))
            {
                return "MP3";
            }

            if (audioFormat.EqualsIgnoreCase("MLP"))
            {
                return "MLP";
            }

            if (audioFormat.EqualsIgnoreCase("DTS"))
            {
                return "DTS";
            }

            if (audioFormat.EqualsIgnoreCase("TrueHD"))
            {
                return "TrueHD";
            }

            if (audioFormat.EqualsIgnoreCase("FLAC"))
            {
                return "FLAC";
            }

            if (audioFormat.EqualsIgnoreCase("Vorbis"))
            {
                return "Vorbis";
            }

            if (audioFormat.EqualsIgnoreCase("Opus"))
            {
                return "Opus";
            }

            return audioFormat;
        }

        public static string FormatAudioCodecTaglib(MediaInfoModel mediaInfo)
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

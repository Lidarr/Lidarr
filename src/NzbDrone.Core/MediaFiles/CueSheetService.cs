using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Diacritics.Extensions;
using NLog;
using NzbDrone.Core.MediaFiles.TrackImport;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using UtfUnknown;

namespace NzbDrone.Core.MediaFiles
{
    public class CueSheetInfo
    {
        public List<IFileInfo> MusicFiles { get; set; } = new List<IFileInfo>();
        public IdentificationOverrides IdOverrides { get; set; }
        public CueSheet CueSheet { get; set; }
        public bool IsForMediaFile(string path) => CueSheet != null && CueSheet.Files.Count > 0 && CueSheet.Files.Any(x => Path.GetFileName(path) == x.Name);
        public CueSheet.FileEntry TryToGetFileEntryForMediaFile(string path)
        {
            if (CueSheet != null && CueSheet.Files.Count > 0)
            {
                return CueSheet.Files.Find(x => Path.GetFileName(path) == x.Name);
            }

            return null;
        }
    }

    public interface ICueSheetService
    {
        List<ImportDecision<LocalTrack>> GetImportDecisions(ref List<IFileInfo> mediaFileList, IdentificationOverrides idOverrides, ImportDecisionMakerInfo itemInfo, ImportDecisionMakerConfig config);
    }

    public class CueSheetService : ICueSheetService
    {
        private readonly IParsingService _parsingService;
        private readonly IMakeImportDecision _importDecisionMaker;
        private readonly Logger _logger;

        private static string _FileKey = "FILE";
        private static string _TrackKey = "TRACK";
        private static string _IndexKey = "INDEX";
        private static string _GenreKey = "REM GENRE";
        private static string _DateKey = "REM DATE";
        private static string _DiscIdKey = "REM DISCID";
        private static string _PerformerKey = "PERFORMER";
        private static string _TitleKey = "TITLE";

        public CueSheetService(IParsingService parsingService,
                               IMakeImportDecision importDecisionMaker,
                               Logger logger)
        {
            _parsingService = parsingService;
            _importDecisionMaker = importDecisionMaker;
            _logger = logger;
        }

        private class PunctuationReplacer
        {
            private readonly Dictionary<char, char>  _replacements = new Dictionary<char, char>
            {
                { '‘', '\'' }, { '’', '\'' }, // Single quotes
                { '“', '"' }, { '”', '"' }, // Double quotes
                { '‹', '<' }, { '›', '>' }, // Angle quotes
                { '«', '<' }, { '»', '>' }, // Guillemets
                { '–', '-' }, { '—', '-' }, // Dashes
                { '…', '.' }, // Ellipsis
                { '¡', '!' }, { '¿', '?' }, // Inverted punctuation (Spanish)
            };

            public string ReplacePunctuation(string input)
            {
                var output = new StringBuilder(input.Length);

                foreach (var c in input)
                {
                    if (_replacements.TryGetValue(c, out var replacement))
                    {
                        output.Append(replacement);
                    }
                    else
                    {
                        output.Append(c);
                    }
                }

                return output.ToString();
            }
        }

        public List<ImportDecision<LocalTrack>> GetImportDecisions(ref List<IFileInfo> mediaFileList, IdentificationOverrides idOverrides, ImportDecisionMakerInfo itemInfo, ImportDecisionMakerConfig config)
        {
            var decisions = new List<ImportDecision<LocalTrack>>();
            var cueFiles = mediaFileList.Where(x => x.Extension.Equals(".cue")).ToList();
            if (cueFiles.Count == 0)
            {
                return decisions;
            }

            mediaFileList.RemoveAll(l => cueFiles.Contains(l));
            var cueSheetInfos = new List<CueSheetInfo>();
            foreach (var cueFile in cueFiles)
            {
                var cueSheetInfo = GetCueSheetInfo(cueFile, mediaFileList);
                if (idOverrides != null)
                {
                    cueSheetInfo.IdOverrides = idOverrides;
                }

                var addedCueSheetInfo = cueSheetInfos.Find(existingCueSheetInfo => existingCueSheetInfo.CueSheet.DiscID == cueSheetInfo.CueSheet.DiscID);
                if (addedCueSheetInfo == null)
                {
                    cueSheetInfos.Add(cueSheetInfo);
                }

                // If there are multiple cue sheet files for the same disc, then we try to keep the last one or the one with the exact same name as the media file, if there's any
                else if (cueSheetInfo.CueSheet.IsSingleFileRelease && addedCueSheetInfo.CueSheet.Files.Count > 0)
                {
                    var mediaFileName = Path.GetFileName(addedCueSheetInfo.CueSheet.Files[0].Name);
                    var cueSheetFileName = Path.GetFileName(cueFile.Name);

                    if (mediaFileName != cueSheetFileName)
                    {
                        cueSheetInfos.Remove(addedCueSheetInfo);
                        cueSheetInfos.Add(cueSheetInfo);
                    }
                }
            }

            var cueSheetInfosGroupedByDiscId = cueSheetInfos.GroupBy(x => x.CueSheet.DiscID).ToList();
            foreach (var cueSheetInfoGroup in cueSheetInfosGroupedByDiscId)
            {
                var audioFilesForCues = new List<IFileInfo>();
                foreach (var cueSheetInfo in cueSheetInfoGroup)
                {
                    audioFilesForCues.AddRange(cueSheetInfo.MusicFiles);
                }

                var itemInfoWithCueSheetInfos = itemInfo;
                itemInfoWithCueSheetInfos.CueSheetInfos = cueSheetInfoGroup.ToList();
                decisions.AddRange(_importDecisionMaker.GetImportDecisions(audioFilesForCues, cueSheetInfoGroup.First().IdOverrides, itemInfoWithCueSheetInfos, config));

                foreach (var cueSheetInfo in cueSheetInfos)
                {
                    if (cueSheetInfo.CueSheet != null)
                    {
                        decisions.ForEach(item =>
                        {
                            if (cueSheetInfo.IsForMediaFile(item.Item.Path))
                            {
                                item.Item.CueSheetPath = cueSheetInfo.CueSheet.Path;
                            }
                        });
                    }

                    mediaFileList.RemoveAll(x => cueSheetInfo.MusicFiles.Contains(x));
                }
            }

            decisions.ForEach(decision =>
            {
                if (!decision.Item.IsSingleFileRelease)
                {
                    return;
                }

                var cueSheetFindResult = cueSheetInfos.Find(x => x.IsForMediaFile(decision.Item.Path));
                var cueSheet = cueSheetFindResult?.CueSheet;
                if (cueSheet == null)
                {
                    return;
                }

                if (cueSheet.Files.Count == 0)
                {
                    return;
                }

                var tracksFromCueSheet = cueSheet.Files.SelectMany(x => x.Tracks).ToList();
                if (tracksFromCueSheet.Count == 0)
                {
                    return;
                }

                if (decision.Item.Release == null)
                {
                    return;
                }

                var tracksFromRelease = decision.Item.Release.Tracks.Value;
                if (tracksFromRelease.Count == 0)
                {
                    return;
                }

                var replacer = new PunctuationReplacer();
                var i = 0;
                while (i < tracksFromRelease.Count)
                {
                    var trackFromRelease = tracksFromRelease[i];
                    var trackFromReleaseTitle = NormalizeTitle(replacer, trackFromRelease.Title);

                    var j = 0;
                    var anyMatch = false;
                    while (j < tracksFromCueSheet.Count)
                    {
                        var trackFromCueSheet = tracksFromCueSheet[j];
                        var trackFromCueSheetTitle = NormalizeTitle(replacer, trackFromCueSheet.Title);
                        anyMatch = string.Equals(trackFromReleaseTitle, trackFromCueSheetTitle, StringComparison.InvariantCultureIgnoreCase);

                        if (anyMatch)
                        {
                            decision.Item.Tracks.Add(trackFromRelease);
                            tracksFromRelease.RemoveAt(i);
                            tracksFromCueSheet.RemoveAt(j);

                            break;
                        }
                        else
                        {
                            j++;
                        }
                    }

                    if (!anyMatch)
                    {
                        i++;
                    }
                }
            });

            return decisions;
        }

        private static string NormalizeTitle(PunctuationReplacer replacer, string title)
        {
            title.Normalize(NormalizationForm.FormKD);
            title = title.RemoveDiacritics();
            title = replacer.ReplacePunctuation(title);
            return title;
        }

        private CueSheet LoadCueSheet(IFileInfo fileInfo)
        {
            using (var fs = fileInfo.OpenRead())
            {
                var bytes = new byte[fileInfo.Length];
                var result = CharsetDetector.DetectFromFile(fileInfo.FullName); // or pass FileInfo
                var encoding = result.Detected.Encoding;
                _logger.Debug("Detected encoding {0} for {1}", encoding.WebName, fileInfo.FullName);

                string content;
                while (fs.Read(bytes, 0, bytes.Length) > 0)
                {
                    content = encoding.GetString(bytes);
                    var lines = content.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                    var cueSheet = ParseLines(lines);

                    // Single-file cue means it's an unsplit image, which should be specially treated in the pipeline
                    cueSheet.IsSingleFileRelease = cueSheet.Files.Count == 1;
                    cueSheet.Path = fileInfo.FullName;

                    return cueSheet;
                }
            }

            return new CueSheet();
        }

        private string ExtractValue(string line, string keyword)
        {
            var pattern = keyword + @"\s+(?:(?:\""(.*?)\"")|(.+))";
            var match = Regex.Match(line, pattern);

            if (match.Success)
            {
                var value = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                return value;
            }

            return "";
        }

        private List<string> ExtractPerformers(string line)
        {
            var delimiters = new char[] { ',', ';' };
            var performers = ExtractValue(line, _PerformerKey);
            return performers.Split(delimiters, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
        }

        private bool GetNewLine(ref int index, ref string newLine, string[] lines)
        {
            if (index < lines.Length)
            {
                newLine = lines[index];
                index++;
                return true;
            }

            return false;
        }

        private CueSheet ParseLines(string[] lines)
        {
            var cueSheet = new CueSheet();

            var i = 0;
            string line = null;

            while (GetNewLine(ref i, ref line, lines))
            {
                if (line.StartsWith(_FileKey))
                {
                    line = line.Trim();
                    line = line.Substring(_FileKey.Length).Trim();
                    var filename = line.Split('"')[1];
                    var fileDetails = new CueSheet.FileEntry { Name = filename };

                    if (!GetNewLine(ref i, ref line, lines))
                    {
                        break;
                    }

                    while (line.StartsWith("  "))
                    {
                        line = line.Trim();
                        if (line.StartsWith(_TrackKey))
                        {
                            line = line.Substring(_TrackKey.Length).Trim();
                        }

                        var trackDetails = new CueSheet.TrackEntry();
                        var trackInfo = line.Split(' ');
                        if (trackInfo.Length > 0)
                        {
                            if (int.TryParse(trackInfo[0], out var number))
                            {
                                trackDetails.Number = number;
                            }
                        }

                        if (!GetNewLine(ref i, ref line, lines))
                        {
                            break;
                        }

                        while (line.StartsWith("    "))
                        {
                            line = line.Trim();
                            if (line.StartsWith(_IndexKey))
                            {
                                line = line.Substring(_IndexKey.Length).Trim();
                                var parts = line.Split(' ');
                                if (parts.Length > 1)
                                {
                                    if (int.TryParse(parts[0], out var key))
                                    {
                                        var value = parts[1].Trim('"');
                                        trackDetails.Indices.Add(new CueSheet.IndexEntry { Key = key, Time = value });
                                    }
                                }
                            }
                            else if (line.StartsWith(_TitleKey))
                            {
                                trackDetails.Title = ExtractValue(line, _TitleKey);
                            }
                            else if (line.StartsWith(_PerformerKey))
                            {
                                trackDetails.Performers = ExtractPerformers(line);
                            }

                            if (!GetNewLine(ref i, ref line, lines))
                            {
                                break;
                            }
                        }

                        fileDetails.Tracks.Add(trackDetails);
                    }

                    cueSheet.Files.Add(fileDetails);
                }
                else if (line.StartsWith(_GenreKey))
                {
                    cueSheet.Genre = ExtractValue(line, _GenreKey);
                }
                else if (line.StartsWith(_DateKey))
                {
                    cueSheet.Date = ExtractValue(line, _DateKey);
                }
                else if (line.StartsWith(_DiscIdKey))
                {
                    cueSheet.DiscID = ExtractValue(line, _DiscIdKey);
                }
                else if (line.StartsWith(_PerformerKey))
                {
                    cueSheet.Performers = ExtractPerformers(line);
                }
                else if (line.StartsWith(_TitleKey))
                {
                    cueSheet.Title = ExtractValue(line, _TitleKey);
                }
            }

            return cueSheet;
        }

        private Artist GetArtist(List<string> performers)
        {
            if (performers.Count == 1)
            {
                return _parsingService.GetArtist(performers[0]);
            }
            else if (performers.Count > 1)
            {
                return _parsingService.GetArtist("various artists");
            }

            return null;
        }

        private CueSheetInfo GetCueSheetInfo(IFileInfo cueFile, List<IFileInfo> musicFiles)
        {
            var cueSheetInfo = new CueSheetInfo();
            var cueSheet = LoadCueSheet(cueFile);
            if (cueSheet == null)
            {
                return cueSheetInfo;
            }

            cueSheetInfo.CueSheet = cueSheet;
            cueSheetInfo.MusicFiles = musicFiles.Where(musicFile => cueSheet.Files.Any(musicFileFromCue => musicFileFromCue.Name == musicFile.Name)).ToList();

            cueSheetInfo.IdOverrides = new IdentificationOverrides();

            var artistFromCue = GetArtist(cueSheet.Performers);

            if (artistFromCue == null && cueSheet.Files.Count > 0)
            {
                foreach (var fileEntry in cueSheet.Files)
                {
                    foreach (var track in fileEntry.Tracks)
                    {
                        artistFromCue = GetArtist(track.Performers);
                        if (artistFromCue != null)
                        {
                            break;
                        }
                    }
                }
            }

            // The cue sheet file is too incomplete in this case
            if (artistFromCue == null)
            {
                return cueSheetInfo;
            }

            cueSheetInfo.IdOverrides.Artist = artistFromCue;

            var parsedAlbumInfo = new ParsedAlbumInfo
            {
                AlbumTitle = cueSheet.Title,
                ArtistName = artistFromCue.Name,
                ReleaseDate = cueSheet.Date,
            };

            var albumsFromCue = _parsingService.GetAlbums(parsedAlbumInfo, artistFromCue);
            if (albumsFromCue != null && albumsFromCue.Count > 0)
            {
                cueSheetInfo.IdOverrides.Album = albumsFromCue[0];
            }

            return cueSheetInfo;
        }
    }
}

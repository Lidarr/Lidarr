using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Diacritical;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Disk;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Organizer
{
    public interface IBuildFileNames
    {
        string BuildTrackFileName(List<Track> tracks, Artist artist, Album album, TrackFile trackFile, string extension = "", NamingConfig namingConfig = null, List<CustomFormat> customFormats = null);
        string BuildTrackFilePath(List<Track> tracks, Artist artist, Album album, TrackFile trackFile, string extension, NamingConfig namingConfig = null, List<CustomFormat> customFormats = null);
        BasicNamingConfig GetBasicNamingConfig(NamingConfig nameSpec);
        string GetArtistFolder(Artist artist, NamingConfig namingConfig = null);
    }

    public class FileNameBuilder : IBuildFileNames
    {
        private readonly INamingConfigService _namingConfigService;
        private readonly IQualityDefinitionService _qualityDefinitionService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly ICached<TrackFormat[]> _trackFormatCache;
        private readonly ICached<AbsoluteTrackFormat[]> _absoluteTrackFormatCache;
        private readonly Logger _logger;

        private static readonly Regex TitleRegex = new Regex(@"(?<escaped>\{\{|\}\})|\{(?<prefix>[- ._\[(]*)(?<token>(?:[a-z0-9]+)(?:(?<separator>[- ._]+)(?:[a-z0-9]+))?)(?::(?<customFormat>[a-z0-9+-]+(?<!-)))?(?<suffix>[- ._)\]]*)\}",
                                                             RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static readonly Regex TrackRegex = new Regex(@"(?<track>\{track(?:\:0+)?})",
                                                               RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex MediumRegex = new Regex(@"(?<medium>\{medium(?:\:0+)?})",
                                                               RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex SeasonEpisodePatternRegex = new Regex(@"(?<separator>(?<=})[- ._]+?)?(?<seasonEpisode>s?{season(?:\:0+)?}(?<episodeSeparator>[- ._]?[ex])(?<episode>{episode(?:\:0+)?}))(?<separator>[- ._]+?(?={))?",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex ReleaseDateRegex = new Regex(@"\{Release(\s|\W|_)Year\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex ArtistNameRegex = new Regex(@"(?<token>\{(?:Artist)(?<separator>[- ._])(Clean)?Name(The)?(?::(?<customFormat>[0-9-]+))?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex AlbumTitleRegex = new Regex(@"(?<token>\{(?:Album)(?<separator>[- ._])(Clean)?Title(The)?(?::(?<customFormat>[0-9-]+))?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex TrackTitleRegex = new Regex(@"(?<token>\{(?:Track)(?<separator>[- ._])(Clean)?Title(?::(?<customFormat>[0-9-]+))?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex FileNameCleanupRegex = new Regex(@"([- ._])(\1)+", RegexOptions.Compiled);
        private static readonly Regex TrimSeparatorsRegex = new Regex(@"[- ._]+$", RegexOptions.Compiled);

        private static readonly Regex ScenifyRemoveChars = new Regex(@"(?<=\s)(,|<|>|\/|\\|;|:|'|""|\||`|~|!|\?|@|$|%|^|\*|-|_|=){1}(?=\s)|('|:|\?|,)(?=(?:(?:s|m)\s)|\s|$)|(\(|\)|\[|\]|\{|\})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ScenifyReplaceChars = new Regex(@"[\/]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // TODO: Support Written numbers (One, Two, etc) and Roman Numerals (I, II, III etc)
        private static readonly Regex MultiPartCleanupRegex = new Regex(@"(?:\(\d+\)|(Part|Pt\.?)\s?\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly char[] TrackTitleTrimCharacters = new[] { ' ', '.', '?' };

        private static readonly Regex TitlePrefixRegex = new Regex(@"^(The|An|A) (.*?)((?: *\([^)]+\))*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ReservedDeviceNamesRegex = new Regex(@"^(?:aux|com[1-9]|con|lpt[1-9]|nul|prn)\.", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public FileNameBuilder(INamingConfigService namingConfigService,
                               IQualityDefinitionService qualityDefinitionService,
                               ICacheManager cacheManager,
                               ICustomFormatCalculationService formatCalculator,
                               Logger logger)
        {
            _namingConfigService = namingConfigService;
            _qualityDefinitionService = qualityDefinitionService;
            _formatCalculator = formatCalculator;
            _trackFormatCache = cacheManager.GetCache<TrackFormat[]>(GetType(), "trackFormat");
            _absoluteTrackFormatCache = cacheManager.GetCache<AbsoluteTrackFormat[]>(GetType(), "absoluteTrackFormat");
            _logger = logger;
        }

        private string BuildTrackFileName(List<Track> tracks, Artist artist, Album album, TrackFile trackFile, string extension, int maxPath, NamingConfig namingConfig = null, List<CustomFormat> customFormats = null)
        {
            if (namingConfig == null)
            {
                namingConfig = _namingConfigService.GetConfig();
            }

            if (!namingConfig.RenameTracks)
            {
                return GetOriginalTitle(trackFile) + extension;
            }

            if (namingConfig.StandardTrackFormat.IsNullOrWhiteSpace() || namingConfig.MultiDiscTrackFormat.IsNullOrWhiteSpace())
            {
                throw new NamingFormatException("Standard and Multi track formats cannot be empty");
            }

            var pattern = namingConfig.StandardTrackFormat;

            if (tracks.First().AlbumRelease.Value.Media.Count > 1)
            {
                pattern = namingConfig.MultiDiscTrackFormat;
            }

            tracks = tracks.OrderBy(e => e.AlbumReleaseId).ThenBy(e => e.TrackNumber).ToList();

            var splitPatterns = pattern.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var components = new List<string>();

            for (var i = 0; i < splitPatterns.Length; i++)
            {
                var splitPattern = splitPatterns[i];
                var tokenHandlers = new Dictionary<string, Func<TokenMatch, string>>(FileNameBuilderTokenEqualityComparer.Instance);
                splitPattern = FormatTrackNumberTokens(splitPattern, "", tracks);
                splitPattern = FormatMediumNumberTokens(splitPattern, "", tracks);

                AddArtistTokens(tokenHandlers, artist);
                AddAlbumTokens(tokenHandlers, album);
                AddMediumTokens(tokenHandlers, tracks.First().AlbumRelease.Value.Media.SingleOrDefault(m => m.Number == tracks.First().MediumNumber));
                AddTrackTokens(tokenHandlers, tracks, artist);
                AddTrackTitlePlaceholderTokens(tokenHandlers);
                AddTrackFileTokens(tokenHandlers, trackFile);
                AddQualityTokens(tokenHandlers, artist, trackFile);
                AddMediaInfoTokens(tokenHandlers, trackFile);
                AddCustomFormats(tokenHandlers, artist, trackFile, customFormats);

                var component = ReplaceTokens(splitPattern, tokenHandlers, namingConfig, true).Trim();
                var maxPathSegmentLength = Math.Min(LongPathSupport.MaxFileNameLength, maxPath);
                if (i == splitPatterns.Length - 1)
                {
                    maxPathSegmentLength -= extension.GetByteCount();
                }

                var maxTrackTitleLength = maxPathSegmentLength - GetLengthWithoutTrackTitle(component, namingConfig);

                AddTrackTitleTokens(tokenHandlers, tracks, maxTrackTitleLength);
                component = ReplaceTokens(component, tokenHandlers, namingConfig).Trim();

                component = FileNameCleanupRegex.Replace(component, match => match.Captures[0].Value[0].ToString());
                component = TrimSeparatorsRegex.Replace(component, string.Empty);
                component = component.Replace("{ellipsis}", "...");
                component = ReplaceReservedDeviceNames(component);

                if (component.IsNotNullOrWhiteSpace())
                {
                    components.Add(component);
                }
            }

            return string.Join(Path.DirectorySeparatorChar.ToString(), components) + extension;
        }

        public string BuildTrackFileName(List<Track> tracks, Artist artist, Album album, TrackFile trackFile, string extension = "", NamingConfig namingConfig = null, List<CustomFormat> customFormats = null)
        {
            return BuildTrackFileName(tracks, artist, album, trackFile, extension, LongPathSupport.MaxFilePathLength, namingConfig, customFormats);
        }

        public string BuildTrackFilePath(List<Track> tracks, Artist artist, Album album, TrackFile trackFile, string extension, NamingConfig namingConfig = null, List<CustomFormat> customFormats = null)
        {
            Ensure.That(extension, () => extension).IsNotNullOrWhiteSpace();

            var artistPath = artist.Path;
            var remainingPathLength = LongPathSupport.MaxFilePathLength - artistPath.GetByteCount() - 1;
            var fileName = BuildTrackFileName(tracks, artist, album, trackFile, extension, remainingPathLength, namingConfig, customFormats);

            return Path.Combine(artistPath, fileName);
        }

        public BasicNamingConfig GetBasicNamingConfig(NamingConfig nameSpec)
        {
            var trackFormat = GetTrackFormat(nameSpec.StandardTrackFormat).LastOrDefault();

            if (trackFormat == null)
            {
                return new BasicNamingConfig();
            }

            var basicNamingConfig = new BasicNamingConfig
            {
                Separator = trackFormat.Separator
            };

            var titleTokens = TitleRegex.Matches(nameSpec.StandardTrackFormat);

            foreach (Match match in titleTokens)
            {
                var separator = match.Groups["separator"].Value;
                var token = match.Groups["token"].Value;

                if (!separator.Equals(" "))
                {
                    basicNamingConfig.ReplaceSpaces = true;
                }

                if (token.StartsWith("{Artist", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeArtistName = true;
                }

                if (token.StartsWith("{Album", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeAlbumTitle = true;
                }

                if (token.StartsWith("{Quality", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeQuality = true;
                }
            }

            return basicNamingConfig;
        }

        public string GetArtistFolder(Artist artist, NamingConfig namingConfig = null)
        {
            if (namingConfig == null)
            {
                namingConfig = _namingConfigService.GetConfig();
            }

            var pattern = namingConfig.ArtistFolderFormat;
            var tokenHandlers = new Dictionary<string, Func<TokenMatch, string>>(FileNameBuilderTokenEqualityComparer.Instance);

            AddArtistTokens(tokenHandlers, artist);

            var splitPatterns = pattern.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var components = new List<string>();

            foreach (var s in splitPatterns)
            {
                var splitPattern = s;

                var component = ReplaceTokens(splitPattern, tokenHandlers, namingConfig);
                component = CleanFolderName(component);
                component = ReplaceReservedDeviceNames(component);
                component = component.Replace("{ellipsis}", "...");

                if (component.IsNotNullOrWhiteSpace())
                {
                    components.Add(component);
                }
            }

            return Path.Combine(components.ToArray());
        }

        public static string CleanTitle(string title)
        {
            title = title.Replace("&", "and");
            title = ScenifyReplaceChars.Replace(title, " ");
            title = ScenifyRemoveChars.Replace(title, string.Empty);

            return title;
        }

        public static string TitleThe(string title)
        {
            return TitlePrefixRegex.Replace(title, "$2, $1$3");
        }

        public static string CleanTitleThe(string title)
        {
            if (TitlePrefixRegex.IsMatch(title))
            {
                var splitResult = TitlePrefixRegex.Split(title);
                return $"{CleanTitle(splitResult[2]).Trim()}, {splitResult[1]}{CleanTitle(splitResult[3])}";
            }

            return CleanTitle(title);
        }

        public static string TitleFirstCharacter(string title)
        {
            if (char.IsLetterOrDigit(title[0]))
            {
                return title.Substring(0, 1).ToUpper().RemoveDiacritics()[0].ToString();
            }

            // Try the second character if the first was non alphanumeric
            if (char.IsLetterOrDigit(title[1]))
            {
                return title.Substring(1, 1).ToUpper().RemoveDiacritics()[0].ToString();
            }

            // Default to "_" if no alphanumeric character can be found in the first 2 positions
            return "_";
        }

        public static string CleanFileName(string name)
        {
            return CleanFileName(name, NamingConfig.Default);
        }

        public static string CleanFolderName(string name)
        {
            name = FileNameCleanupRegex.Replace(name, match => match.Captures[0].Value[0].ToString());

            return name.Trim(' ', '.');
        }

        private void AddArtistTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Artist artist)
        {
            tokenHandlers["{Artist Name}"] = m => Truncate(artist.Name, m.CustomFormat);
            tokenHandlers["{Artist CleanName}"] = m => Truncate(CleanTitle(artist.Name), m.CustomFormat);
            tokenHandlers["{Artist NameThe}"] = m => Truncate(TitleThe(artist.Name), m.CustomFormat);
            tokenHandlers["{Artist CleanNameThe}"] = m => Truncate(CleanTitleThe(artist.Name), m.CustomFormat);
            tokenHandlers["{Artist Genre}"] = m => artist.Metadata.Value.Genres?.FirstOrDefault() ?? string.Empty;
            tokenHandlers["{Artist NameFirstCharacter}"] = m => TitleFirstCharacter(TitleThe(artist.Name));
            tokenHandlers["{Artist MbId}"] = m => artist.ForeignArtistId ?? string.Empty;

            if (artist.Metadata.Value.Disambiguation != null)
            {
                tokenHandlers["{Artist Disambiguation}"] = m => artist.Metadata.Value.Disambiguation;
            }
        }

        private void AddAlbumTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Album album)
        {
            tokenHandlers["{Album Title}"] = m => Truncate(album.Title, m.CustomFormat);
            tokenHandlers["{Album CleanTitle}"] = m => Truncate(CleanTitle(album.Title), m.CustomFormat);
            tokenHandlers["{Album TitleThe}"] = m => Truncate(TitleThe(album.Title), m.CustomFormat);
            tokenHandlers["{Album CleanTitleThe}"] = m => Truncate(CleanTitleThe(album.Title), m.CustomFormat);
            tokenHandlers["{Album Type}"] = m => album.AlbumType;
            tokenHandlers["{Album Genre}"] = m => album.Genres.FirstOrDefault() ?? string.Empty;
            tokenHandlers["{Album MbId}"] = m => album.ForeignAlbumId ?? string.Empty;

            if (album.Disambiguation != null)
            {
                tokenHandlers["{Album Disambiguation}"] = m => album.Disambiguation;
            }

            tokenHandlers["{Release Year}"] = album.ReleaseDate.HasValue
                ? m => album.ReleaseDate.Value.Year.ToString()
                : m => "Unknown";
        }

        private void AddMediumTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Medium medium)
        {
            tokenHandlers["{Medium Name}"] = m => medium.Name;
            tokenHandlers["{Medium Format}"] = m => medium.Format;
        }

        private void AddTrackTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, List<Track> tracks, Artist artist)
        {
            // Use the track's ArtistMetadata by default, as it will handle the "Various Artists" case
            // (where the album artist is "Various Artists" but each track has its own artist). Fall back
            // to the album artist if we don't have any track ArtistMetadata for whatever reason.
            var firstArtist = tracks.Select(t => t.ArtistMetadata?.Value).FirstOrDefault() ?? artist.Metadata;
            if (firstArtist != null)
            {
                tokenHandlers["{Track ArtistName}"] = m => Truncate(firstArtist.Name, m.CustomFormat);
                tokenHandlers["{Track ArtistCleanName}"] = m => Truncate(CleanTitle(firstArtist.Name), m.CustomFormat);
                tokenHandlers["{Track ArtistNameThe}"] = m => Truncate(TitleThe(firstArtist.Name), m.CustomFormat);
                tokenHandlers["{Track ArtistCleanNameThe}"] = m => Truncate(CleanTitleThe(firstArtist.Name), m.CustomFormat);
                tokenHandlers["{Track ArtistMbId}"] = m => firstArtist.ForeignArtistId ?? string.Empty;
            }
        }

        private void AddTrackTitlePlaceholderTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers)
        {
            tokenHandlers["{Track Title}"] = m => null;
            tokenHandlers["{Track CleanTitle}"] = m => null;
        }

        private void AddTrackTitleTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, List<Track> tracks, int maxLength)
        {
            tokenHandlers["{Track Title}"] = m => GetTrackTitle(GetTrackTitles(tracks), "+", maxLength, m.CustomFormat);
            tokenHandlers["{Track CleanTitle}"] = m => GetTrackTitle(GetTrackTitles(tracks).Select(CleanTitle).ToList(), "and", maxLength, m.CustomFormat);
        }

        private void AddTrackFileTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, TrackFile trackFile)
        {
            tokenHandlers["{Original Title}"] = m => GetOriginalTitle(trackFile);
            tokenHandlers["{Original Filename}"] = m => GetOriginalFileName(trackFile);
            tokenHandlers["{Release Group}"] = m => trackFile.ReleaseGroup.IsNullOrWhiteSpace() ? m.DefaultValue("Lidarr") : Truncate(trackFile.ReleaseGroup, m.CustomFormat);
        }

        private void AddQualityTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Artist artist, TrackFile trackFile)
        {
            var qualityTitle = _qualityDefinitionService.Get(trackFile.Quality.Quality).Title;
            var qualityProper = GetQualityProper(trackFile.Quality);

            // var qualityReal = GetQualityReal(artist, trackFile.Quality);
            tokenHandlers["{Quality Full}"] = m => string.Format("{0}", qualityTitle);
            tokenHandlers["{Quality Title}"] = m => qualityTitle;
            tokenHandlers["{Quality Proper}"] = m => qualityProper;

            // tokenHandlers["{Quality Real}"] = m => qualityReal;
        }

        private void AddMediaInfoTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, TrackFile trackFile)
        {
            if (trackFile.MediaInfo == null)
            {
                _logger.Trace("Media info is unavailable for {0}", trackFile);

                return;
            }

            var audioCodec = MediaInfoFormatter.FormatAudioCodec(trackFile.MediaInfo);
            var audioChannels = MediaInfoFormatter.FormatAudioChannels(trackFile.MediaInfo);
            var audioChannelsFormatted = audioChannels > 0 ?
                                audioChannels.ToString("F1", CultureInfo.InvariantCulture) :
                                string.Empty;

            tokenHandlers["{MediaInfo AudioCodec}"] = m => audioCodec;
            tokenHandlers["{MediaInfo AudioChannels}"] = m => audioChannelsFormatted;
            tokenHandlers["{MediaInfo AudioBitRate}"] = m => MediaInfoFormatter.FormatAudioBitrate(trackFile.MediaInfo);
            tokenHandlers["{MediaInfo AudioBitsPerSample}"] = m => MediaInfoFormatter.FormatAudioBitsPerSample(trackFile.MediaInfo);
            tokenHandlers["{MediaInfo AudioSampleRate}"] = m => MediaInfoFormatter.FormatAudioSampleRate(trackFile.MediaInfo);
        }

        private void AddCustomFormats(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Artist series, TrackFile episodeFile, List<CustomFormat> customFormats = null)
        {
            if (customFormats == null)
            {
                episodeFile.Artist = series;
                customFormats = _formatCalculator.ParseCustomFormat(episodeFile, series);
            }

            tokenHandlers["{Custom Formats}"] = m => string.Join(" ", customFormats.Where(x => x.IncludeCustomFormatWhenRenaming));
        }

        private string ReplaceTokens(string pattern, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig, bool escape = false)
        {
            return TitleRegex.Replace(pattern, match => ReplaceToken(match, tokenHandlers, namingConfig, escape));
        }

        private string ReplaceToken(Match match, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig, bool escape)
        {
            if (match.Groups["escaped"].Success)
            {
                if (escape)
                {
                    return match.Value;
                }
                else if (match.Value == "{{")
                {
                    return "{";
                }
                else if (match.Value == "}}")
                {
                    return "}";
                }
            }

            var tokenMatch = new TokenMatch
            {
                RegexMatch = match,
                Prefix = match.Groups["prefix"].Value,
                Separator = match.Groups["separator"].Value,
                Suffix = match.Groups["suffix"].Value,
                Token = match.Groups["token"].Value,
                CustomFormat = match.Groups["customFormat"].Value
            };

            if (tokenMatch.CustomFormat.IsNullOrWhiteSpace())
            {
                tokenMatch.CustomFormat = null;
            }

            var tokenHandler = tokenHandlers.GetValueOrDefault(tokenMatch.Token, m => string.Empty);

            var replacementText = tokenHandler(tokenMatch);

            if (replacementText == null)
            {
                // Preserve original token if handler returned null
                return match.Value;
            }

            replacementText = replacementText.Trim();

            if (tokenMatch.Token.All(t => !char.IsLetter(t) || char.IsLower(t)))
            {
                replacementText = replacementText.ToLower();
            }
            else if (tokenMatch.Token.All(t => !char.IsLetter(t) || char.IsUpper(t)))
            {
                replacementText = replacementText.ToUpper();
            }

            if (!tokenMatch.Separator.IsNullOrWhiteSpace())
            {
                replacementText = replacementText.Replace(" ", tokenMatch.Separator);
            }

            replacementText = CleanFileName(replacementText, namingConfig);

            if (!replacementText.IsNullOrWhiteSpace())
            {
                replacementText = tokenMatch.Prefix + replacementText + tokenMatch.Suffix;
            }

            if (escape)
            {
                replacementText = replacementText.Replace("{", "{{").Replace("}", "}}");
            }

            return replacementText;
        }

        private string FormatTrackNumberTokens(string basePattern, string formatPattern, List<Track> tracks)
        {
            var pattern = string.Empty;

            for (var i = 0; i < tracks.Count; i++)
            {
                var patternToReplace = i == 0 ? basePattern : formatPattern;

                pattern += TrackRegex.Replace(patternToReplace, match => ReplaceNumberToken(match.Groups["track"].Value, tracks[i].AbsoluteTrackNumber));
            }

            return pattern;
        }

        private string FormatMediumNumberTokens(string basePattern, string formatPattern, List<Track> tracks)
        {
            var pattern = string.Empty;

            for (var i = 0; i < tracks.Count; i++)
            {
                var patternToReplace = i == 0 ? basePattern : formatPattern;

                pattern += MediumRegex.Replace(patternToReplace, match => ReplaceNumberToken(match.Groups["medium"].Value, tracks[i].MediumNumber));
            }

            return pattern;
        }

        private string ReplaceNumberToken(string token, int value)
        {
            var split = token.Trim('{', '}').Split(':');
            if (split.Length == 1)
            {
                return value.ToString("0");
            }

            return value.ToString(split[1]);
        }

        private TrackFormat[] GetTrackFormat(string pattern)
        {
            return _trackFormatCache.Get(pattern, () => SeasonEpisodePatternRegex.Matches(pattern).OfType<Match>()
                .Select(match => new TrackFormat
                {
                    TrackSeparator = match.Groups["episodeSeparator"].Value,
                    Separator = match.Groups["separator"].Value,
                    TrackPattern = match.Groups["episode"].Value,
                }).ToArray());
        }

        private List<string> GetTrackTitles(List<Track> tracks)
        {
            if (tracks.Count == 1)
            {
                return new List<string>
                {
                    tracks.First().Title.TrimEnd(TrackTitleTrimCharacters)
                };
            }

            var titles = tracks.Select(c => c.Title.TrimEnd(TrackTitleTrimCharacters))
                                 .Select(CleanupTrackTitle)
                                 .Distinct()
                                 .ToList();

            if (titles.All(t => t.IsNullOrWhiteSpace()))
            {
                titles = tracks.Select(c => c.Title.TrimEnd(TrackTitleTrimCharacters))
                                 .Distinct()
                                 .ToList();
            }

            return titles;
        }

        private string GetTrackTitle(List<string> titles, string separator, int maxLength, string formatter)
        {
            var maxFormatterLength = GetMaxLengthFromFormatter(formatter);

            if (maxFormatterLength > 0)
            {
                maxLength = Math.Min(maxLength, maxFormatterLength);
            }

            separator = $" {separator.Trim()} ";

            var joined = string.Join(separator, titles);

            if (joined.GetByteCount() <= maxLength)
            {
                return joined;
            }

            var firstTitle = titles.First();
            var firstTitleLength = firstTitle.GetByteCount();

            if (titles.Count >= 2)
            {
                var lastTitle = titles.Last();
                var lastTitleLength = lastTitle.GetByteCount();
                if (firstTitleLength + lastTitleLength + 3 <= maxLength)
                {
                    return $"{firstTitle.TrimEnd(' ', '.')}{{ellipsis}}{lastTitle}";
                }
            }

            if (titles.Count > 1 && firstTitleLength + 3 <= maxLength)
            {
                return $"{firstTitle.TrimEnd(' ', '.')}{{ellipsis}}";
            }

            if (titles.Count == 1 && firstTitleLength <= maxLength)
            {
                return firstTitle;
            }

            return $"{firstTitle.Truncate(maxLength - 3).TrimEnd(' ', '.')}{{ellipsis}}";
        }

        private string CleanupTrackTitle(string title)
        {
            // this will remove (1),(2) from the end of multi part episodes.
            return MultiPartCleanupRegex.Replace(title, string.Empty).Trim();
        }

        private string GetQualityProper(QualityModel quality)
        {
            if (quality.Revision.Version > 1)
            {
                if (quality.Revision.IsRepack)
                {
                    return "Repack";
                }

                return "Proper";
            }

            return string.Empty;
        }

        // private string GetQualityReal(Series series, QualityModel quality)
        // {
        //    if (quality.Revision.Real > 0)
        //    {
        //        return "REAL";
        //    }

        // return string.Empty;
        // }
        private string GetOriginalTitle(TrackFile trackFile)
        {
            if (trackFile.SceneName.IsNullOrWhiteSpace())
            {
                return GetOriginalFileName(trackFile);
            }

            return trackFile.SceneName;
        }

        private string GetOriginalFileName(TrackFile trackFile)
        {
            return Path.GetFileNameWithoutExtension(trackFile.Path);
        }

        private int GetLengthWithoutTrackTitle(string pattern, NamingConfig namingConfig)
        {
            var tokenHandlers = new Dictionary<string, Func<TokenMatch, string>>(FileNameBuilderTokenEqualityComparer.Instance);
            tokenHandlers["{Track Title}"] = m => string.Empty;
            tokenHandlers["{Track CleanTitle}"] = m => string.Empty;
            tokenHandlers["{ellipsis}"] = m => "...";

            var result = ReplaceTokens(pattern, tokenHandlers, namingConfig);

            return result.GetByteCount();
        }

        private string ReplaceReservedDeviceNames(string input)
        {
            // Replace reserved windows device names with an alternative
            return ReservedDeviceNamesRegex.Replace(input, match => match.Value.Replace(".", "_"));
        }

        private static string CleanFileName(string name, NamingConfig namingConfig)
        {
            var result = name;
            string[] badCharacters = { "\\", "/", "<", ">", "?", "*", "|", "\"" };
            string[] goodCharacters = { "+", "+", "", "", "!", "-", "", "" };

            if (namingConfig.ReplaceIllegalCharacters)
            {
                // Smart replaces a colon followed by a space with space dash space for a better appearance
                if (namingConfig.ColonReplacementFormat == ColonReplacementFormat.Smart)
                {
                    result = result.Replace(": ", " - ");
                    result = result.Replace(":", "-");
                }
                else
                {
                    var replacement = string.Empty;

                    switch (namingConfig.ColonReplacementFormat)
                    {
                        case ColonReplacementFormat.Dash:
                            replacement = "-";
                            break;
                        case ColonReplacementFormat.SpaceDash:
                            replacement = " -";
                            break;
                        case ColonReplacementFormat.SpaceDashSpace:
                            replacement = " - ";
                            break;
                    }

                    result = result.Replace(":", replacement);
                }
            }
            else
            {
                result = result.Replace(":", string.Empty);
            }

            for (var i = 0; i < badCharacters.Length; i++)
            {
                result = result.Replace(badCharacters[i], namingConfig.ReplaceIllegalCharacters ? goodCharacters[i] : string.Empty);
            }

            return result.TrimStart(' ', '.').TrimEnd(' ');
        }

        private string Truncate(string input, string formatter)
        {
            if (input.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var maxLength = GetMaxLengthFromFormatter(formatter);

            if (maxLength == 0 || input.Length <= Math.Abs(maxLength))
            {
                return input;
            }

            if (maxLength < 0)
            {
                return $"{{ellipsis}}{input.Reverse().Truncate(Math.Abs(maxLength) - 3).TrimEnd(' ', '.').Reverse()}";
            }

            return $"{input.Truncate(maxLength - 3).TrimEnd(' ', '.')}{{ellipsis}}";
        }

        private int GetMaxLengthFromFormatter(string formatter)
        {
            int.TryParse(formatter, out var maxCustomLength);

            return maxCustomLength;
        }
    }

    internal sealed class TokenMatch
    {
        public Match RegexMatch { get; set; }
        public string Prefix { get; set; }
        public string Separator { get; set; }
        public string Suffix { get; set; }
        public string Token { get; set; }
        public string CustomFormat { get; set; }

        public string DefaultValue(string defaultValue)
        {
            if (string.IsNullOrEmpty(Prefix) && string.IsNullOrEmpty(Suffix))
            {
                return defaultValue;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public enum MultiEpisodeStyle
    {
        Extend = 0,
        Duplicate = 1,
        Repeat = 2,
        Scene = 3,
        Range = 4,
        PrefixedRange = 5
    }

    public enum ColonReplacementFormat
    {
        Delete = 0,
        Dash = 1,
        SpaceDash = 2,
        SpaceDashSpace = 3,
        Smart = 4
    }
}

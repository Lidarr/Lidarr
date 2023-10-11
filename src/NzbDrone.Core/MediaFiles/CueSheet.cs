using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.MediaFiles
{
    public class CueSheet : ModelBase
    {
        public CueSheet(IFileInfo fileInfo)
        {
            Path = fileInfo.FullName;

            using (var fs = fileInfo.OpenRead())
            {
                var bytes = new byte[fileInfo.Length];
                var encoding = new UTF8Encoding(true);
                string content;
                while (fs.Read(bytes, 0, bytes.Length) > 0)
                {
                    content = encoding.GetString(bytes);
                    var lines = content.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                    ParseCueSheet(lines);

                    // Single-file cue means it's an unsplit image, which should be specially treated in the pipeline
                    IsSingleFileRelease = Files.Count == 1;
                }
            }
        }

        public class IndexEntry
        {
            public int Key { get; set; }
            public string Time { get; set; }
        }

        public class TrackEntry
        {
            public int Number { get; set; }
            public string Title { get; set; }
            public string Performer { get; set; }
            public List<IndexEntry> Indices { get; set; } = new List<IndexEntry>();
        }

        public class FileEntry
        {
            public string Name { get; set; }
            public IndexEntry Index { get; set; }
            public List<TrackEntry> Tracks { get; set; } = new List<TrackEntry>();
        }

        public string Path { get; set; }
        public bool IsSingleFileRelease { get; set; }
        public List<FileEntry> Files { get; set; } = new List<FileEntry>();
        public string Genre { get; set; }
        public string Date { get; set; }
        public string DiscID { get; set; }
        public string Title { get; set; }
        public string Performer { get; set; }
        private static string _FileKey = "FILE";
        private static string _TrackKey = "TRACK";
        private static string _IndexKey = "INDEX";
        private static string _GenreKey = "REM GENRE";
        private static string _DateKey = "REM DATE";
        private static string _DiscIdKey = "REM DISCID";
        private static string _PerformerKey = "PERFORMER";
        private static string _TitleKey = "TITLE";

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

        private void ParseCueSheet(string[] lines)
        {
            var i = 0;
            try
            {
                while (true)
                {
                    var line = lines[i];
                    if (line.StartsWith(_FileKey))
                    {
                        line = line.Trim();
                        line = line.Substring(_FileKey.Length).Trim();
                        var filename = line.Split('"')[1];
                        var fileDetails = new FileEntry { Name = filename };

                        i++;
                        line = lines[i];
                        while (line.StartsWith("  "))
                        {
                            line = line.Trim();
                            if (line.StartsWith(_TrackKey))
                            {
                                line = line.Substring(_TrackKey.Length).Trim();
                            }

                            var trackDetails = new TrackEntry();
                            var trackInfo = line.Split(' ');
                            if (trackInfo.Length > 0)
                            {
                                if (int.TryParse(trackInfo[0], out var number))
                                {
                                    trackDetails.Number = number;
                                }
                            }

                            i++;
                            line = lines[i];
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
                                            trackDetails.Indices.Add(new IndexEntry { Key = key, Time = value });
                                        }
                                    }

                                    i++;
                                    line = lines[i];
                                }
                                else if (line.StartsWith(_TitleKey))
                                {
                                    trackDetails.Title = ExtractValue(line, _TitleKey);
                                    i++;
                                    line = lines[i];
                                }
                                else if (line.StartsWith(_PerformerKey))
                                {
                                    trackDetails.Performer = ExtractValue(line, _PerformerKey);
                                    i++;
                                    line = lines[i];
                                }
                                else
                                {
                                    i++;
                                    line = lines[i];
                                }
                            }

                            fileDetails.Tracks.Add(trackDetails);
                        }

                        Files.Add(fileDetails);
                    }
                    else if (line.StartsWith(_GenreKey))
                    {
                        Genre = ExtractValue(line, _GenreKey);
                        i++;
                    }
                    else if (line.StartsWith(_DateKey))
                    {
                        Date = ExtractValue(line, _DateKey);
                        i++;
                    }
                    else if (line.StartsWith(_DiscIdKey))
                    {
                        DiscID = ExtractValue(line, _DiscIdKey);
                        i++;
                    }
                    else if (line.StartsWith(_PerformerKey))
                    {
                        Performer = ExtractValue(line, _PerformerKey);
                        i++;
                    }
                    else if (line.StartsWith(_TitleKey))
                    {
                        Title = ExtractValue(line, _TitleKey);
                        i++;
                    }
                    else
                    {
                        i++;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
            }
        }
    }
}

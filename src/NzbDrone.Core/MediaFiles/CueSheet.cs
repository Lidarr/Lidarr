using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
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

                    // Single-file cue means it's an unsplit image
                    FileNames = ReadField(lines, "FILE");
                    IsSingleFileRelease = FileNames.Count == 1;

                    var performers = ReadField(lines, "PERFORMER");
                    if (performers.Count > 0)
                    {
                        Performer = performers[0];
                    }

                    var titles = ReadField(lines, "TITLE");
                    if (titles.Count > 0)
                    {
                        Title = titles[0];
                    }

                    var dates = ReadField(lines, "REM DATE");
                    if (dates.Count > 0)
                    {
                        Date = dates[0];
                    }
                }
            }
        }

        public string Path { get; set; }
        public bool IsSingleFileRelease { get; set; }
        public List<string> FileNames { get; set; }
        public string Title { get; set; }
        public string Performer { get; set; }
        public string Date { get; set; }

        private static List<string> ReadField(string[] lines, string fieldName)
        {
            var inQuotePattern = "\"(.*?)\"";
            var flatPattern = fieldName + " (.+)";

            var results = new List<string>();
            var candidates = lines.Where(l => l.StartsWith(fieldName)).ToList();
            foreach (var candidate in candidates)
            {
                var matches = Regex.Matches(candidate, inQuotePattern).ToList();
                if (matches.Count == 0)
                {
                    matches = Regex.Matches(candidate, flatPattern).ToList();
                }

                if (matches.Count == 0)
                {
                    continue;
                }

                var groups = matches[0].Groups;
                if (groups.Count > 0)
                {
                    var result = groups[1].Value;
                    results.Add(result);
                }
            }

            return results;
        }
    }
}

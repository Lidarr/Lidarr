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
                    var fileNames = ReadFieldFromCuesheet(lines, "FILE");
                    IsSingleFileRelease = fileNames.Count == 1;
                    FileName = fileNames[0];

                    var performers = ReadFieldFromCuesheet(lines, "PERFORMER");
                    if (performers.Count > 0)
                    {
                        Performer = performers[0];
                    }

                    var titles = ReadFieldFromCuesheet(lines, "TITLE");
                    if (titles.Count > 0)
                    {
                        Title = titles[0];
                    }

                    Date = ReadOptionalFieldFromCuesheet(lines, "REM DATE");
                }
            }
        }

        public bool IsSingleFileRelease { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public string Performer { get; set; }
        public string Date { get; set; }

        private static List<string> ReadFieldFromCuesheet(string[] lines, string fieldName)
        {
            var results = new List<string>();
            var candidates = lines.Where(l => l.StartsWith(fieldName)).ToList();
            foreach (var candidate in candidates)
            {
                var matches = Regex.Matches(candidate, "\"(.*?)\"");
                var result = matches.ToList()[0].Groups[1].Value;
                results.Add(result);
            }

            return results;
        }

        private static string ReadOptionalFieldFromCuesheet(string[] lines, string fieldName)
        {
            var results = lines.Where(l => l.StartsWith(fieldName));
            if (results.Any())
            {
                var matches = Regex.Matches(results.ToList()[0], fieldName + " (.+)");
                var result = matches.ToList()[0].Groups[1].Value;
                return result;
            }

            return "";
        }
    }
}

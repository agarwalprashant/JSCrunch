using System;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using SourceMapDotNet;
using SourceMapDotNet.Model;

namespace JSCrunch
{
    public class SourceMapMapper
    {
        public static SourceLocation[] SourceLinesFromStackTrace(string stackTrace)
        {
            var content = HttpUtility.UrlDecode(stackTrace);

            var pattern = " (evaluating ";
            var pos = content.IndexOf(pattern, StringComparison.Ordinal);
            if (pos > 0)
            {
                content = content.Substring(pos).Trim();
            }

            var lines = content
                .Split(new[] { "&#10;&#9;&#9;", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseLine)
                .ToArray();

            var groupedByFile = lines
                .GroupBy(l => l.Position.File, l => l)
                .ToList();

            foreach (var file in groupedByFile)
            {
                var pathToSourceMap = file.Key + ".map";

                pathToSourceMap = new Uri(pathToSourceMap).LocalPath;

                if (File.Exists(pathToSourceMap))
                {
                    Map(file.ToArray(), pathToSourceMap);
                }
            }

            return lines;
        }

        private static SourceLocation ParseLine(string line)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("at "))
            {
                SourceLocation retval;

                var parts = line.Split(new[] { " (" }, StringSplitOptions.None);
                if (parts.Length == 1)
                {
                    var where = parts[0].Trim().Replace("at ", "");
                    retval = new SourceLocation { Where = where };
                }
                else
                {
                    var what = parts[0].Trim().Replace("at ", "");
                    var where = parts[1].Trim().Replace(")", "");
                    retval = new SourceLocation { What = what, Where = where };
                }

                retval.Position = PositionFrom(retval.Where);

                return retval;
            }
            else
            {
                var parts = line.Split(new[] { " in " }, StringSplitOptions.None);

                var what = parts[0].Trim();

                what = what.Replace("(evaluating &apos;", "").Replace("&apos;)", "").Trim();

                var where = parts[1].Trim().Replace("(line ", ":").Replace(")", ":0");

                return new SourceLocation
                {
                    What = what,
                    Where = where,
                    Position = PositionFrom(where)
                };
            }
        }

        private static SourceReference PositionFrom(string location)
        {
            var columnPosition = location.LastIndexOf(":", StringComparison.Ordinal);

            var linePosition = location.LastIndexOf(":", columnPosition - 1, StringComparison.Ordinal);

            var line = int.Parse(location.Substring(linePosition + 1, columnPosition - linePosition - 1));

            return new SourceReference { LineNumber = line, File = location.Substring(0, linePosition).Trim() };
        }

        public static object Map(SourceLocation[] locations, string pathToSourceMap)
        {
            var file = JsonConvert.DeserializeObject<SourceMapFile>(File.ReadAllText(pathToSourceMap));

            var consumer = new SourceMapConsumer(file);

            foreach (var location in locations)
            {
                var mappedPositions = consumer.OriginalPositionsFor(location.Position.LineNumber);
                if (mappedPositions.Length >= 1)
                {
                    location.Position = mappedPositions.First();
                }
            }

            return null;
        }
    }
}
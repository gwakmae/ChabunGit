using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChabunGit.Services
{
    public static class AiResponseCleaner
    {
        public static string CleanTitle(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "chore: update project";
            raw = Regex.Replace(raw, @"```[\w]*", "");
            raw = Regex.Replace(raw, @"</?title>|</?body>", "", RegexOptions.IgnoreCase);
            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().Trim('`', '"', '*', '-', ' '))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            var typed = lines.FirstOrDefault(l =>
                Regex.IsMatch(l, @"^(feat|fix|refactor|docs|style|test|chore|perf|ci|build|init)\s*(\(.+\))?\s*:", RegexOptions.IgnoreCase));

            string title = typed ?? lines.FirstOrDefault() ?? "chore: update project";
            title = Regex.Replace(title, @"^(here\s+is\s+the\s+(commit\s+)?title\s*[:：]?\s*)", "", RegexOptions.IgnoreCase).Trim();
            if (title.Length > 50) title = title[..50];
            return title.TrimEnd('.', ' ', '\t');
        }

        public static string CleanBody(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            raw = Regex.Replace(raw, @"```[\w]*", "");
            raw = Regex.Replace(raw, @"</?title>|</?body>", "", RegexOptions.IgnoreCase);
            raw = Regex.Replace(raw, @"^#+\s+.*$", "", RegexOptions.Multiline);
            return raw.Trim('`', '"', '\r', '\n', ' ');
        }

        public static string CleanSingleLine(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            raw = Regex.Replace(raw, @"```[\w]*", "");
            raw = Regex.Replace(raw, @"</?title>|</?body>", "", RegexOptions.IgnoreCase);
            var firstLine = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().Trim('`', '"', '*', '-', '•', ' '))
                .FirstOrDefault(l => l.Length > 3);

            if (string.IsNullOrWhiteSpace(firstLine)) return string.Empty;
            firstLine = Regex.Replace(firstLine, @"^(here\s+is\s+.*?[:：]\s*|summary\s*[:：]\s*|change\s*[:：]\s*)", "", RegexOptions.IgnoreCase).Trim();
            if (firstLine.Length > 120) firstLine = firstLine[..120];
            return firstLine;
        }
    }
}
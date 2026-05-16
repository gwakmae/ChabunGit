using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChabunGit.Services
{
    public static class AiResponseCleaner
    {
        /// <summary>
        /// AI가 생성한 커밋 제목을 정제합니다.
        /// </summary>
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

        /// <summary>
        /// AI가 생성한 커밋 본문을 정제합니다.
        /// </summary>
        public static string CleanBody(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            raw = Regex.Replace(raw, @"```[\w]*", "");
            raw = Regex.Replace(raw, @"</?title>|</?body>", "", RegexOptions.IgnoreCase);
            raw = Regex.Replace(raw, @"^#+\s+.*$", "", RegexOptions.Multiline);

            return raw.Trim('`', '"', '\r', '\n', ' ');
        }

        /// <summary>
        /// 파일별 diff 요약(Map 단계)에서 반환된 단일 라인 응답을 정제합니다.
        /// </summary>
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

        // ▼▼▼ [추가] .gitignore 전용 정제 메서드 ▼▼▼
        /// <summary>
        /// AI가 생성한 .gitignore 내용에서 코드블럭, 불필요한 서두/결말을 제거합니다.
        /// </summary>
        public static string CleanGitignore(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            // 마크다운 코드블럭 제거
            raw = Regex.Replace(raw, @"```[\w]*\n?", "", RegexOptions.Multiline);
            raw = Regex.Replace(raw, @"```", "");

            // AI가 붙이는 불필요한 설명문 제거 (영문/한문 공통)
            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                           .Select(l => l.Trim())
                           .Where(l => !Regex.IsMatch(l, @"^(here is|sure|below is|let me know|hope this helps|generated|이것은|아래는|도움이|요청하신|생성된)", RegexOptions.IgnoreCase))
                           .ToList();

            return string.Join("\n", lines).Trim();
        }
        // ▲▲▲ [추가] 여기까지 ▲▲▲
    }
}
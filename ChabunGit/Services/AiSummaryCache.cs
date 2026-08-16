// File: ChabunGit/Services/AiSummaryCache.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    /// <summary>
    /// 파일별 AI 분석 결과(diff 요약)를 디스크에 캐싱합니다.
    /// diff 내용의 해시가 동일하면 GPU 호출 없이 캐시된 결과를 재사용합니다.
    /// 캐시 파일은 .git 폴더 안에 저장되어 저장소별로 독립 관리됩니다.
    /// </summary>
    public class AiSummaryCache
    {
        private readonly string _cacheFilePath;
        private Dictionary<string, CacheEntry> _entries = new();

        // 캐시 항목이 너무 오래되면 모델이 바뀌었을 수 있으므로 유효기간을 둡니다.
        private static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);

        // 캐시 파일이 무한정 커지는 것을 방지합니다.
        private const int MaxEntries = 500;

        private class CacheEntry
        {
            public string Summary { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        public AiSummaryCache(string repoPath)
        {
            _cacheFilePath = Path.Combine(repoPath, ".git", "chabungit_ai_cache.json");
        }

        public static string ComputeKey(string filePath, string diff)
        {
            // 파일 경로 + diff 내용을 함께 해시하여
            // 다른 파일이 우연히 같은 diff를 가져도 충돌하지 않게 합니다.
            string input = filePath + "\n" + diff;
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash);
        }

        public async Task LoadAsync()
        {
            _entries = new Dictionary<string, CacheEntry>();
            if (!File.Exists(_cacheFilePath)) return;

            try
            {
                string json = await File.ReadAllTextAsync(_cacheFilePath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, CacheEntry>>(json);
                if (loaded == null) return;

                var cutoff = DateTime.UtcNow - MaxAge;
                foreach (var (key, entry) in loaded)
                {
                    // 유효기간이 지난 항목은 로드 시점에 걸러냅니다.
                    if (entry.CreatedAt >= cutoff)
                        _entries[key] = entry;
                }
            }
            catch (Exception)
            {
                // 캐시 파일이 손상되었어도 앱이 멈추면 안 되므로 조용히 새로 시작
                _entries = new Dictionary<string, CacheEntry>();
            }
        }

        public bool TryGet(string key, out string summary)
        {
            summary = string.Empty;
            if (_entries.TryGetValue(key, out var entry))
            {
                summary = entry.Summary;
                return true;
            }
            return false;
        }

        public void Set(string key, string summary)
        {
            if (string.IsNullOrWhiteSpace(summary)) return;
            _entries[key] = new CacheEntry { Summary = summary, CreatedAt = DateTime.UtcNow };
        }

        public async Task SaveAsync()
        {
            try
            {
                // 항목 수 초과 시 가장 오래된 것부터 제거
                if (_entries.Count > MaxEntries)
                {
                    var trimmed = new Dictionary<string, CacheEntry>();
                    int skip = _entries.Count - MaxEntries;
                    int index = 0;

                    // 오래된 순으로 정렬해서 오래된 것 skip개를 버립니다.
                    var sorted = new List<KeyValuePair<string, CacheEntry>>(_entries);
                    sorted.Sort((a, b) => a.Value.CreatedAt.CompareTo(b.Value.CreatedAt));

                    foreach (var kv in sorted)
                    {
                        if (index++ < skip) continue;
                        trimmed[kv.Key] = kv.Value;
                    }
                    _entries = trimmed;
                }

                var options = new JsonSerializerOptions { WriteIndented = false };
                string json = JsonSerializer.Serialize(_entries, options);
                await File.WriteAllTextAsync(_cacheFilePath, json);
            }
            catch (Exception)
            {
                // 캐시 저장 실패는 치명적이지 않으므로 무시
            }
        }
    }
}

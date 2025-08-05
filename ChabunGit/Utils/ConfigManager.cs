// 파일 경로: ChabunGit/Utils/ConfigManager.cs (수정된 코드)

using ChabunGit.Models;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChabunGit.Utils
{
    /// <summary>
    /// 애플리케이션 설정(settings.json)을 관리하는 유틸리티 클래스입니다.
    /// </summary>
    public class ConfigManager
    {
        private readonly string _configFolderPath;
        private readonly string _configFilePath;

        public ConfigManager()
        {
            _configFolderPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "ChabunGit");
            _configFilePath = Path.Combine(_configFolderPath, "settings.json");
        }

        /// <summary>
        /// 현재 설정 객체를 settings.json 파일로 비동기 저장합니다.
        /// </summary>
        /// <param name="settings">저장할 AppSettings 객체</param>
        public async Task SaveConfigAsync(AppSettings settings)
        {
            Directory.CreateDirectory(_configFolderPath);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(settings, options);

            await File.WriteAllTextAsync(_configFilePath, jsonString);
        }

        /// <summary>
        /// settings.json 파일에서 설정을 비동기 로드합니다.
        /// </summary>
        /// <returns>로드된 AppSettings 객체. 파일이 없으면 기본 객체를 반환합니다.</returns>
        public async Task<AppSettings> LoadConfigAsync()
        {
            if (!File.Exists(_configFilePath))
            {
                return new AppSettings();
            }

            try
            {
                string jsonString = await File.ReadAllTextAsync(_configFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(jsonString);
                return settings ?? new AppSettings();
            }
            catch (JsonException)
            {
                return new AppSettings();
            }
        }
    }
}
// 파일 경로: ChabunGit/Utils/ConfigManager.cs

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
            // 설정 파일을 사용자별 AppData 폴더에 저장하여 다른 사용자에게 영향을 주지 않도록 합니다.
            _configFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChabunGit");
            _configFilePath = Path.Combine(_configFolderPath, "settings.json");
        }

        /// <summary>
        *   현재 설정 객체를 settings.json 파일로 비동기 저장합니다.
        /// </summary>
        /// <param name="settings">저장할 AppSettings 객체</param>
        public async Task SaveConfigAsync(AppSettings settings)
        {
            // 설정 폴더가 없으면 생성합니다.
            Directory.CreateDirectory(_configFolderPath);

            var options = new JsonSerializerOptions { WriteIndented = true }; // JSON을 예쁘게 포맷팅하여 저장
            string jsonString = JsonSerializer.Serialize(settings, options);

            await File.WriteAllTextAsync(_configFilePath, jsonString);
        }

        /// <summary>
        *   settings.json 파일에서 설정을 비동기 로드합니다.
        /// </summary>
        /// <returns>로드된 AppSettings 객체. 파일이 없으면 기본 객체를 반환합니다.</returns>
        public async Task<AppSettings> LoadConfigAsync()
        {
            if (!File.Exists(_configFilePath))
            {
                // 설정 파일이 없으면(첫 실행 등), 기본값으로 새 객체를 반환합니다.
                return new AppSettings();
            }

            try
            {
                string jsonString = await File.ReadAllTextAsync(_configFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(jsonString);
                return settings ?? new AppSettings(); // 파일 내용이 비어있을 경우를 대비
            }
            catch (JsonException)
            {
                // JSON 파일이 손상되었을 경우, 기본 설정으로 복구합니다.
                return new AppSettings();
            }
        }
    }
}
// File: ChabunGit/Services/ConfigManager.cs
using ChabunGit.Models;
using ChabunGit.Services.Abstractions;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChabunGit.Services
{
    public class ConfigManager : IConfigManager
    {
        private readonly string _configFolderPath;
        private readonly string _configFilePath;

        public ConfigManager()
        {
            _configFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChabunGit");
            _configFilePath = Path.Combine(_configFolderPath, "settings.json");
        }

        public async Task SaveConfigAsync(AppSettings settings)
        {
            Directory.CreateDirectory(_configFolderPath);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(settings, options);
            await File.WriteAllTextAsync(_configFilePath, jsonString);
        }

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
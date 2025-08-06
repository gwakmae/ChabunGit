// File: ChabunGit/Services/Abstractions/IConfigManager.cs
using ChabunGit.Models;
using System.Threading.Tasks;

namespace ChabunGit.Services.Abstractions
{
    public interface IConfigManager
    {
        Task<AppSettings> LoadConfigAsync();
        Task SaveConfigAsync(AppSettings settings);
    }
}
// File: ChabunGit/Models/CommitInfo.cs
namespace ChabunGit.Models
{
    public class CommitInfo
    {
        public string Hash { get; set; } = string.Empty;
        public string ShortHash { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string DisplayMember => $"{Date} - {Message} ({ShortHash})";
    }
}
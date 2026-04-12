// File: ChabunGit/ViewModels/FolderSelectorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace ChabunGit.ViewModels
{
    // 트리 항목 하나를 나타내는 클래스
    public partial class FileTreeItem : ObservableObject
    {
        [ObservableProperty]
        private bool _isChecked;

        [ObservableProperty]
        private bool _isExpanded;

        public string Name { get; }
        public string FullPath { get; }
        public string RelativePath { get; }
        public bool IsDirectory { get; }
        public string SizeText { get; }
        public string Icon => IsDirectory ? "📁" : "📄";

        // 용량 큰 파일/폴더 강조 표시
        public bool IsLarge { get; }
        public string DisplayText => IsLarge
            ? $"{Icon} {Name}  ⚠️ {SizeText}"
            : $"{Icon} {Name}  {SizeText}";

        public ObservableCollection<FileTreeItem> Children { get; } = new();

        // 부모 참조 (체크박스 연동용)
        private readonly FileTreeItem? _parent;

        public FileTreeItem(string fullPath, string relativePath, bool isDirectory,
                            long sizeBytes, FileTreeItem? parent = null)
        {
            FullPath = fullPath;
            RelativePath = relativePath;
            Name = Path.GetFileName(fullPath);
            IsDirectory = isDirectory;
            _parent = parent;

            // 100MB 이상이면 대용량으로 표시
            IsLarge = sizeBytes >= 100 * 1024 * 1024;
            SizeText = FormatSize(sizeBytes);
        }

        partial void OnIsCheckedChanged(bool value)
        {
            // 폴더 체크 시 모든 자식도 동일하게 체크/해제
            foreach (var child in Children)
                child.IsChecked = value;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "";
            if (bytes >= 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
            if (bytes >= 1024 * 1024)
                return $"{bytes / (1024.0 * 1024):F1} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }
    }


    public partial class FolderSelectorViewModel : ViewModelBase
    {
        public ObservableCollection<FileTreeItem> TreeItems { get; } = new();

        [ObservableProperty]
        private string _title = "제외할 폴더/파일 선택";

        [ObservableProperty]
        private string _description =
            "AI .gitignore 생성 전에 반드시 제외할 폴더나 파일을 선택하세요.\n" +
            "⚠️ 표시는 100MB 이상의 대용량 파일/폴더입니다.";

        // 확인 버튼 클릭 시 선택된 경로 목록
        public ObservableCollection<string> SelectedPaths { get; } = new();

        public event Action? RequestClose;

        private readonly string _repoPath;


        public FolderSelectorViewModel(string repoPath)
        {
            _repoPath = repoPath;
            LoadTree(repoPath);
        }

        private void LoadTree(string repoPath)
        {
            TreeItems.Clear();

            try
            {
                var gitDir = Path.Combine(repoPath, ".git");
                var ignoreNames = new[]
                    { ".git", "node_modules", "__pycache__", ".vs" };

                // 루트의 직접 자식만 1~2단계 로드 (너무 깊으면 느림)
                LoadChildren(repoPath, null, TreeItems, ignoreNames, depth: 0, maxDepth: 3);

                // 대용량 항목 자동 펼치기
                ExpandLargeItems(TreeItems);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] FolderSelectorViewModel.LoadTree: {ex.Message}");
            }
        }

        private void LoadChildren(
            string dirPath,
            FileTreeItem? parent,
            ObservableCollection<FileTreeItem> collection,
            string[] ignoreNames,
            int depth,
            int maxDepth)
        {
            if (depth > maxDepth) return;

            try
            {
                // 폴더 먼저
                foreach (var dir in Directory.GetDirectories(dirPath)
                             .OrderBy(d => Path.GetFileName(d)))
                {
                    string name = Path.GetFileName(dir);
                    if (ignoreNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                        continue;

                    string relativePath = Path.GetRelativePath(_repoPath, dir);
                    long dirSize = GetDirectorySize(dir);

                    var item = new FileTreeItem(dir, relativePath, true, dirSize, parent);
                    collection.Add(item);

                    // 재귀적으로 자식 로드
                    LoadChildren(dir, item, item.Children, ignoreNames, depth + 1, maxDepth);
                }

                // 파일
                foreach (var file in Directory.GetFiles(dirPath)
                             .OrderBy(f => Path.GetFileName(f)))
                {
                    string relativePath = Path.GetRelativePath(_repoPath, file);
                    long fileSize = new FileInfo(file).Length;

                    var item = new FileTreeItem(file, relativePath, false, fileSize, parent);
                    collection.Add(item);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // 권한 없는 폴더는 조용히 건너뜀
            }
        }

        // 폴더 크기 계산 (재귀)
        private static long GetDirectorySize(string path)
        {
            try
            {
                return Directory.GetFiles(path, "*", SearchOption.AllDirectories)
                    .Sum(f =>
                    {
                        try { return new FileInfo(f).Length; }
                        catch { return 0L; }
                    });
            }
            catch { return 0L; }
        }

        // 대용량 항목 자동 펼치기 + 체크
        private static void ExpandLargeItems(ObservableCollection<FileTreeItem> items)
        {
            foreach (var item in items)
            {
                if (item.IsLarge)
                {
                    item.IsExpanded = true;
                    // 자동 체크는 하지 않음 — 사용자가 직접 선택
                }
                if (item.Children.Any())
                    ExpandLargeItems(item.Children);
            }
        }

        // 전체 선택 / 해제
        [RelayCommand]
        private void SelectAll()
        {
            foreach (var item in TreeItems)
                item.IsChecked = true;
        }

        [RelayCommand]
        private void DeselectAll()
        {
            foreach (var item in TreeItems)
                item.IsChecked = false;
        }

        // 대용량만 자동 선택
        [RelayCommand]
        private void SelectLargeOnly()
        {
            DeselectAll();
            SelectLargeRecursive(TreeItems);
        }

        private static void SelectLargeRecursive(ObservableCollection<FileTreeItem> items)
        {
            foreach (var item in items)
            {
                if (item.IsLarge)
                    item.IsChecked = true;
                else if (item.Children.Any())
                    SelectLargeRecursive(item.Children);
            }
        }

        // 확인 버튼
        [RelayCommand]
        private void Confirm()
        {
            SelectedPaths.Clear();
            CollectCheckedPaths(TreeItems, SelectedPaths);
            RequestClose?.Invoke();
        }

        // 건너뜀 버튼 (선택 없이 그냥 AI 생성)
        [RelayCommand]
        private void Skip()
        {
            SelectedPaths.Clear();
            RequestClose?.Invoke();
        }

        private static void CollectCheckedPaths(
            ObservableCollection<FileTreeItem> items,
            ObservableCollection<string> result)
        {
            foreach (var item in items)
            {
                if (item.IsChecked)
                {
                    // 체크된 항목은 상대 경로를 결과에 추가
                    // 폴더면 뒤에 / 붙임 (gitignore 규칙)
                    result.Add(item.IsDirectory
                        ? item.RelativePath.Replace("\\", "/") + "/"
                        : item.RelativePath.Replace("\\", "/"));
                }
                else if (item.Children.Any())
                {
                    // 미체크 폴더의 자식 중 체크된 것이 있을 수 있음
                    CollectCheckedPaths(item.Children, result);
                }
            }
        }
    }
}

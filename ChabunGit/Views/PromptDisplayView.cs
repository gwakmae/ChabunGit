// File: ChabunGit/Views/PromptDisplayView.xaml.cs
using ChabunGit.ViewModels;
using System.Windows;

namespace ChabunGit.Views
{
    public partial class PromptDisplayView : Window
    {
        public PromptDisplayView()
        {
            InitializeComponent();

            // ▼▼▼ [추가] DataContext가 설정된 후 RequestClose 이벤트 구독 ▼▼▼
            DataContextChanged += (s, e) =>
            {
                if (e.NewValue is PromptDisplayViewModel vm)
                {
                    vm.RequestClose += () => this.DialogResult = true;
                }
            };
            // ▲▲▲ [추가] 여기까지 ▲▲▲
        }
    }
}

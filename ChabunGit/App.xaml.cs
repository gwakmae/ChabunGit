// File: ChabunGit/App.xaml.cs
using ChabunGit.Core;
using ChabunGit.Services;
using ChabunGit.Services.Abstractions;
using ChabunGit.ViewModels;
using ChabunGit.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Windows;

namespace ChabunGit
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Core
            services.AddSingleton<GitCommandExecutor>();

            // Services
            services.AddSingleton<IConfigManager, ConfigManager>();
            services.AddSingleton<IGitService, GitService>();
            services.AddSingleton<IDialogService, WpfDialogService>();
            services.AddSingleton<IPromptService, PromptService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<GitignoreEditViewModel>();
            services.AddTransient<PromptDisplayViewModel>();
            services.AddTransient<CommitDetailViewModel>(); // 추가된 라인

            // Views
            services.AddSingleton(s => new MainWindow(s.GetRequiredService<MainViewModel>()));
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();

            if (mainWindow.DataContext is MainViewModel mainViewModel)
            {
                await mainViewModel.InitializeAsync();
            }

            mainWindow.Show();
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }
            base.OnExit(e);
        }
    }
}

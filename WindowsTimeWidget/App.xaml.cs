using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using System.Windows;
using WindowsTimeWidget.Abstractions;
using WindowsTimeWidget.Services;
using WindowsTimeWidget.Services.BackgroundServices;
using WindowsTimeWidget.ViewModels;
using WindowsTimeWidget.Views.Windows;

namespace WindowsTimeWidget
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        public static IServiceProvider Services => ((App)Current)._host!.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var builder = Host.CreateApplicationBuilder(e.Args);

            builder.Configuration
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            // --- Core services ---
            builder.Services.AddSingleton<ISettingsService, SettingsService>();

            builder.Services.AddHttpClient<ITimeService, TimeService>(client =>
            {
                client.BaseAddress = new Uri("https://timeapi.io/");
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DateTimeWidget/1.0");
            })
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt))))
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

            builder.Services.AddTimeSyncBackgroundService(builder.Configuration);

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            builder.Services.AddSingleton<MainWindow>();
            builder.Services.AddTransient<SettingsWindow>();

            _host = builder.Build();
            await _host.StartAsync();

            var main = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = main;
            main.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (_host is not null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
            }
            base.OnExit(e);
        }
    }

}

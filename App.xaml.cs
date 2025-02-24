using System;
using System.Net.Http;
using System.Windows;
using GayorFinance.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http; // Add this using directive

namespace GayorFinance
{
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider; // Field to hold the service provider

        public App()
        {
            var services = new ServiceCollection(); // Create a new service collection
            ConfigureServices(services); // Configure services
            _serviceProvider = services.BuildServiceProvider(); // Build the service provider
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Add an HttpClient for the ApiClient with the necessary configurations
            services.AddHttpClient<ApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://lkcqjx2v-5221.euw.devtunnels.ms"); // Replace with your API base URL
            });
            // Add MainWindow as a singleton service
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>(); // Get the MainWindow instance from the service provider
            mainWindow.Show(); // Show the main window
        }
    }
}

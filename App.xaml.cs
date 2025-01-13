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
        private IServiceProvider _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            //  Added this line to create the http client, with the necessary configurations
            services.AddHttpClient<ApiClient>(client =>
            {
                client.BaseAddress = new Uri("https://lkcqjx2v-5221.euw.devtunnels.ms"); // Replace with your API base URL
            });
            //  This line is still required to make the DI work correctly
            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
    }
}

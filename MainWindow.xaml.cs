using System;
using System.Windows;
using System.Windows.Navigation;
using GayorFinance.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Threading;
using model;

namespace GayorFinance
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;

        public MainWindow() //Add default constructor
        {
            InitializeComponent();
            this.Visibility = Visibility.Hidden;

        }

        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            Dispatcher.BeginInvoke((Action)(() => {
                NavigateToWelcomePage();
            }), DispatcherPriority.ContextIdle);

        }
        private void NavigateToWelcomePage()
        {
            var welcomePage = new WelcomePage();
            MainFrame.NavigationService.Navigate(welcomePage);
        }
        public void NavigateToSignIn()
        {
            var signInPage = new SignIn(_serviceProvider);
            signInPage.OnSignInSuccess += NavigateToLandingPage;
            MainFrame.NavigationService.Navigate(signInPage);
        }
        public void NavigateToSignUp()
        {
            var signUpPage = new SignUpPage();
            signUpPage.OnSignUpSuccess += NavigateToSignIn;
            MainFrame.NavigationService.Navigate(signUpPage);
        }
        public void NavigateToStockPage(string symbol)
        {
            var apiClient = _serviceProvider.GetRequiredService<ApiClient>();

            var stockPage = new StockPage(symbol, apiClient);
            MainFrame.NavigationService.Navigate(stockPage);
        }
        private void NavigateToLandingPage()
        {
            // Resolve the ApiClient instance from DI
            var apiClient = _serviceProvider.GetRequiredService<ApiClient>();
            // Create a new instance of LandingPage by injecting the ApiClient instance to its constructor
            MainFrame.NavigationService.Navigate(new LandingPage(apiClient));
        }
        public void NavigateToUserPortfoliosPage()
        {
            var apiClient = _serviceProvider.GetRequiredService<ApiClient>();
            MainFrame.NavigationService.Navigate(new UserPortfolios(apiClient));
        }

        public void NavigateTPortfolioPage(Portfolio selectedPortfolio)
        {
            var apiClient = _serviceProvider.GetRequiredService<ApiClient>();
            MainFrame.NavigationService.Navigate(new PortfolioPage(apiClient, selectedPortfolio));
        }
    }
}
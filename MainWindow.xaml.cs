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

        // Default constructor
        public MainWindow()
        {
            InitializeComponent();
            this.Visibility = Visibility.Hidden;
        }

        // Constructor with dependency injection
        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

        // Override method called when content is rendered
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            Dispatcher.BeginInvoke((Action)(() =>
            {
                NavigateToWelcomePage();
            }), DispatcherPriority.ContextIdle);
        }

        // Navigate to the Welcome Page
        private void NavigateToWelcomePage()
        {
            var welcomePage = new WelcomePage();
            MainFrame.NavigationService.Navigate(welcomePage);
        }

        // Navigate to the Sign In Page
        public void NavigateToSignIn()
        {
            var signInPage = new SignIn(_serviceProvider);
            signInPage.OnSignInSuccess += NavigateToLandingPage;
            MainFrame.NavigationService.Navigate(signInPage);
        }

        // Navigate to the Sign Up Page
        public void NavigateToSignUp()
        {
            var signUpPage = new SignUpPage();
            signUpPage.OnSignUpSuccess += NavigateToSignIn;
            MainFrame.NavigationService.Navigate(signUpPage);
        }

        // Navigate to the Stock Page with a given symbol
        public void NavigateToStockPage(string symbol)
        {
            var apiClient = _serviceProvider.GetRequiredService<ApiClient>();
            var stockPage = new StockPage(symbol, apiClient);
            MainFrame.NavigationService.Navigate(stockPage);
        }

        // Navigate to the Landing Page
        public void NavigateToLandingPage()
        {
            var apiClient = _serviceProvider.GetRequiredService<ApiClient>();
            MainFrame.NavigationService.Navigate(new LandingPage(apiClient));
        }

        // Navigate to the User Portfolios Page
        public void NavigateToUserPortfoliosPage()
        {
            var apiClient = _serviceProvider.GetRequiredService<ApiClient>();
            MainFrame.NavigationService.Navigate(new UserPortfolios(apiClient));
        }

        // Navigate to a specific Portfolio Page
        public void NavigateTPortfolioPage(Portfolio selectedPortfolio)
        {
            var apiClient = _serviceProvider.GetRequiredService<ApiClient>();
            MainFrame.NavigationService.Navigate(new PortfolioPage(apiClient, selectedPortfolio));
        }

        // Navigate to the Settings Page
        public void NavigateToSettingsPage()
        {
            MainFrame.NavigationService.Navigate(new Settings());
        }
    }
}
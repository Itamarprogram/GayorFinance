using GayorFinance.Services;
using model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MyServices;

namespace GayorFinance
{
    public partial class UserPortfolios : Page
    {
        // Current user of the application
        public User currentUser;
        // API client for making HTTP requests
        private readonly ApiClient _apiClient;
        // Total value of all portfolios
        private double TotalAllPortfoliosValue { get; set; }
        // Total percentage change of all portfolios
        private double TotalAllPortfoliosChangePercent { get; set; }

        // Constructor to initialize the page with the API client
        public UserPortfolios(ApiClient apiClient)
        {
            InitializeComponent();
            currentUser = UserSession.Instance.CurrentUser;
            _apiClient = apiClient;

            // Load portfolios when the page is initialized
            LoadPortfolios();
        }

        // Method to load portfolios for the current user
        private async void LoadPortfolios()
        {
            try
            {
                ApiService apiService = new ApiService();
                List<Portfolio> portfolios = await FindAllPortfoliosByUserId(currentUser.Id);
                List<PortfolioDisplay> displayPortfolios = new List<PortfolioDisplay>();

                decimal totalInitialValue = 0;
                TotalAllPortfoliosValue = 0;
                TotalAllPortfoliosChangePercent = 0;

                // Iterate through each portfolio and calculate values
                foreach (var portfolio in portfolios)
                {
                    List<PortfolioStocks> stocks = await apiService.GetPortfoliosStocks();
                    stocks = stocks.Where(s => s.PortfolioId == portfolio.Id).ToList();

                    decimal initialValue = stocks.Sum(s => s.Quantity * s.PurchasePrice);
                    decimal TotalCurrentValue = portfolio.TotalValue;
                    decimal TotalPercentChange = TotalCurrentValue > 0 ? ((TotalCurrentValue - initialValue) / initialValue) : 0;

                    var portfolioDisplay = PortfolioDisplay.FromPortfolio(portfolio, (double)TotalCurrentValue, (double)TotalPercentChange, (double)initialValue);
                    displayPortfolios.Add(portfolioDisplay);

                    totalInitialValue += initialValue;
                    TotalAllPortfoliosValue += portfolio.TotalValue;
                }

                decimal totalPercentChange = totalInitialValue > 0
                    ? (decimal)(TotalAllPortfoliosValue - (double)totalInitialValue) / totalInitialValue
                    : 0;

                // Bind values to UI
                DisplayPortfolios.ItemsSource = displayPortfolios;
                TotalAllPortfoliosValueTxt.Text = $"{TotalAllPortfoliosValue:C}";
                TotalAllPortfoliosValueTxt.Foreground = (decimal)TotalAllPortfoliosValue >= totalInitialValue
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Red;

                TotalAllPortfoliosChangePercentTxt.Text = $"{totalPercentChange:P2}";
                TotalAllPortfoliosChangePercentTxt.Foreground = totalPercentChange >= 0
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Red;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading portfolios: " + ex.Message);
            }
        }

        // Method to find all portfolios by user ID
        private async Task<List<Portfolio>> FindAllPortfoliosByUserId(int userId)
        {
            try
            {
                ApiService apiService = new ApiService();
                List<Portfolio> allPortfolios = await apiService.GetPortfolios();
                return allPortfolios.Where(p => p.UserId == userId).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching portfolios: " + ex.Message);
                return new List<Portfolio>();
            }
        }

        // Event handler for deleting a portfolio
        private async void DeletePortfolio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PortfolioDisplay portfolioDisplay)
            {
                try
                {
                    ApiService apiService = new ApiService();
                    int isDeleted = await apiService.DeletePortfolio(portfolioDisplay.OriginalPortoflio.Id);

                    if (isDeleted == 1)
                    {
                        MessageBox.Show("Portfolio deleted successfully.");
                        LoadPortfolios();  // Reload the portfolios after deletion
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete portfolio.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error deleting portfolio: " + ex.Message);
                }
            }
        }

        // Event handler for adding a new portfolio
        private async void AddPortfolio(object sender, RoutedEventArgs e)
        {
            try
            {
                Portfolio portfolio = new Portfolio
                {
                    UserId = currentUser.Id,
                    PortfolioName = PortfolioNameTextBox.Text,
                    DateCreated = DateTime.Today,
                    TotalValue = 0,
                    Description = PortfolioDescriptionTextBox.Text
                };

                ApiService apiService = new ApiService();
                int result = await apiService.InsertPortfolio(portfolio);
                if (result == 1)
                {
                    MessageBox.Show("Portfolio has been successfully added!");
                    LoadPortfolios();
                }
                else
                {
                    MessageBox.Show("Portfolio has not been added!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding portfolio: " + ex.Message);
            }
        }

        // Event handler to close the add portfolio dialog
        private void CloseAddPortfolioDialog(object sender, RoutedEventArgs e)
        {
            AddPortfolioDialogHost.IsOpen = false;
        }

        // Event handler to show the add portfolio dialog
        private void ShowAddPortfolioDialog(object sender, RoutedEventArgs e)
        {
            AddPortfolioDialogHost.IsOpen = true;
        }

        // Event handler to access a specific portfolio
        private void AccessPortfolio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PortfolioDisplay portfolioDisplay)
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.NavigateTPortfolioPage(portfolioDisplay.OriginalPortoflio);
            }
        }
    }
}

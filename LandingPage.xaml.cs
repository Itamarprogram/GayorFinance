using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GayorFinance.Services;
using model;
using MyServices;

namespace GayorFinance
{
    public partial class LandingPage : Page
    {
        public readonly ApiClient _apiClient;
        private List<StockQuote> _allStocks = new List<StockQuote>();
        public User currentUser;
        private double TotalAllPortfoliosValue { get; set; }
        private double TotalAllPortfoliosChangePercent { get; set; }

        List<String> Chosen_stocks = new List<string> { "AAPL", "GOOGL", "AMZN", "MSFT", "TSLA", "NVDA", "META", "CRWD", "NET", "PANW", "AVGO", "JPM", "WMT", "ORCL", "DIS", "MS", "V", "NFLX" };

        public LandingPage(ApiClient apiClient)
        {
            InitializeComponent();
            InitializeEventHandlers();
            currentUser = UserSession.Instance.CurrentUser;
            _apiClient = apiClient;
            DataContext = this;
            LoadPortfolios();
            LoadMarketOverview();
        }

        private void InitializeEventHandlers()
        {
            MarketOverviewList.PreviewMouseLeftButtonUp += MarketOverviewList_PreviewMouseLeftButtonUp;
            SearchTextBox.KeyDown += SearchTextBox_KeyDown;
        }

        private async void LoadMarketOverview()
        {
            try
            {
                var stockQuotes = new ObservableCollection<StockQuote>();

                // Create tasks for all API calls
                var tasks = Chosen_stocks.Select(symbol => _apiClient.GetStockQuote(symbol));

                // Wait for all tasks to complete
                var results = await Task.WhenAll(tasks);

                // Add results to the collection
                foreach (var stock in results.Where(s => s != null))
                {
                    stockQuotes.Add(stock);
                }

                MarketOverviewList.ItemsSource = stockQuotes;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading market overview: " + ex.Message);
            }
        }

        private async void LoadPortfolios()
        {
            try
            {
                ApiService apiService = new ApiService();
                List<Portfolio> portfolios = await FindAllPortfoliosByUserId(currentUser.Id);
                List<PortfolioDisplay> displayPortfolios = new List<PortfolioDisplay>();

                decimal totalInitialValue = 0;
                decimal totalCurrentValue = 0;

                foreach (var portfolio in portfolios)
                {
                    List<PortfolioStocks> stocks = await apiService.GetPortfoliosStocks();
                    stocks = stocks.Where(s => s.PortfolioId == portfolio.Id).ToList();

                    decimal initialValue = stocks.Sum(s => s.Quantity * s.PurchasePrice);
                    decimal currentValue = portfolio.TotalValue;

                    // Calculate percentage change correctly
                    decimal percentChange = initialValue != 0
                        ? ((currentValue - initialValue) / initialValue) * 100
                        : 0;

                    var portfolioDisplay = PortfolioDisplay.FromPortfolio(
                        portfolio,
                        (double)currentValue,
                        (double)percentChange,
                        (double)initialValue
                    );

                    displayPortfolios.Add(portfolioDisplay);
                    totalInitialValue += initialValue;
                    totalCurrentValue += currentValue;
                }

                // Calculate total percentage change
                decimal totalPercentChange = totalInitialValue != 0
                    ? ((totalCurrentValue - totalInitialValue) / totalInitialValue) * 100
                    : 0;

                DisplayPortfolios.ItemsSource = displayPortfolios;
                TotalAllPortfoliosValueTxt.Text = $"Total Value: {totalCurrentValue:C}";
                TotalAllPortfoliosChangePercentTxt.Text = $"Total Change: {totalPercentChange:+0.00;-0.00}%";

                // Set colors based on performance
                TotalAllPortfoliosValueTxt.Foreground = totalCurrentValue >= totalInitialValue ?
                    new SolidColorBrush(Colors.ForestGreen) : new SolidColorBrush(Colors.Crimson);
                TotalAllPortfoliosChangePercentTxt.Foreground = totalPercentChange >= 0 ?
                    new SolidColorBrush(Colors.ForestGreen) : new SolidColorBrush(Colors.Crimson);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading portfolios: " + ex.Message);
            }
        }

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

        private void MarketOverviewList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var element = e.OriginalSource as FrameworkElement;
            while (element != null)
            {
                if (element.DataContext is StockQuote selectedStock)
                {
                    var mainWindow = (MainWindow)Application.Current.MainWindow;
                    mainWindow.NavigateToStockPage(selectedStock.Symbol);
                    break;
                }
                element = VisualTreeHelper.GetParent(element) as FrameworkElement;
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(sender, new RoutedEventArgs());
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text?.Trim();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.NavigateToStockPage(searchText.ToUpper());
            }
        }

        private async void DeletePortfolio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PortfolioDisplay portfolioDisplay)
            {
                var result = MessageBox.Show("Are you sure you want to delete this portfolio?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        ApiService apiService = new ApiService();
                        int isDeleted = await apiService.DeletePortfolio(portfolioDisplay.OriginalPortoflio.Id);

                        if (isDeleted == 1)
                        {
                            MessageBox.Show("Portfolio deleted successfully.");
                            LoadPortfolios();
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
        }

        private async void AddPortfolio(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PortfolioNameTextBox.Text))
            {
                MessageBox.Show("Portfolio name is required.");
                return;
            }

            try
            {
                Portfolio portfolio = new Portfolio
                {
                    UserId = currentUser.Id,
                    PortfolioName = PortfolioNameTextBox.Text.Trim(),
                    DateCreated = DateTime.Today,
                    TotalValue = 0,
                    Description = PortfolioDescriptionTextBox.Text?.Trim() ?? string.Empty
                };

                ApiService apiService = new ApiService();
                int result = await apiService.InsertPortfolio(portfolio);
                if (result == 1)
                {
                    MessageBox.Show("Portfolio has been successfully added!");
                    AddPortfolioDialogHost.IsOpen = false;
                    PortfolioNameTextBox.Text = string.Empty;
                    PortfolioDescriptionTextBox.Text = string.Empty;
                    LoadPortfolios();
                }
                else
                {
                    MessageBox.Show("Failed to add portfolio.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding portfolio: " + ex.Message);
            }
        }

        private void CloseAddPortfolioDialog(object sender, RoutedEventArgs e)
        {
            AddPortfolioDialogHost.IsOpen = false;
            PortfolioNameTextBox.Text = string.Empty;
            PortfolioDescriptionTextBox.Text = string.Empty;
        }

        private void ShowAddPortfolioDialog(object sender, RoutedEventArgs e)
        {
            AddPortfolioDialogHost.IsOpen = true;
        }

        private void AccessPortfolio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PortfolioDisplay portfolioDisplay)
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.NavigateTPortfolioPage(portfolioDisplay.OriginalPortoflio);
            }
        }

        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToSettingsPage();
        }

        private void MarketNewsClick(object sender, RoutedEventArgs e)
        {
            // Implement market news navigation
            MessageBox.Show("Market News feature coming soon!");
        }
    }
}
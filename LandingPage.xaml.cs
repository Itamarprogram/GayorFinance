using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
            currentUser = UserSession.Instance.CurrentUser;

            _apiClient = apiClient;
            DataContext = this;
            LoadPortfolios();
            LoadMarketOverview();
            //LoadAllStocks();
        }

        private async void LoadMarketOverview()
        {
            try
            {
                ObservableCollection<StockQuote> stockQuotes = new ObservableCollection<StockQuote>();

                // Get stock data for each chosen stock
                foreach (var symbol in Chosen_stocks)
                {
                    var stock = await _apiClient.GetStockQuote(symbol); // Await the task to get the StockQuote
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
                TotalAllPortfoliosValue = 0;
                TotalAllPortfoliosChangePercent = 0;

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
                TotalAllPortfoliosValueTxt.Text = $"Total Value: {TotalAllPortfoliosValue:C}";
                TotalAllPortfoliosValueTxt.Foreground = (decimal)TotalAllPortfoliosValue >= totalInitialValue
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Red;

                TotalAllPortfoliosChangePercentTxt.Text = $"Total Percentage Change: {totalPercentChange:P2}";
                TotalAllPortfoliosChangePercentTxt.Foreground = totalPercentChange >= 0
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Red;
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

        private void CloseAddPortfolioDialog(object sender, RoutedEventArgs e)
        {
            AddPortfolioDialogHost.IsOpen = false;
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
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = SearchTextBox.Text;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                SearchTextBox.Text = "";
            }
            else
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.NavigateToStockPage(searchText);
            }
        }


        private void PortfolioClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToUserPortfoliosPage();
        }

        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToSettingsPage();
        }

        private void MarketNewsClick(object sender, RoutedEventArgs e)
        {

        }

        private void MarketOverviewList_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is StockQuote selectedStock)
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.NavigateToStockPage(selectedStock.Symbol);
            }
        }
    }
}
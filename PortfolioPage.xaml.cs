using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GayorFinance.Services;
using LiveCharts;
using model;
using MyServices;
using ViewModel;

    namespace GayorFinance
    {
        public partial class PortfolioPage : Page
        {
            private readonly ApiClient _apiClient;
            private Portfolio _selectedPortfolio;
            private HistoricalDataResponse quote;
            private StockQuote latestQuote;
            public double TotalCurrentValue { get; private set; }
            public double TotalPercentChange { get; private set; }

            public ChartValues<double> PortfolioValues { get; set; } = new ChartValues<double>();
            public List<string> Dates { get; set; } = new List<string>();
            public Func<double, string> ValueFormatter { get; set; } = value => $"${value:F2}";
            public PortfolioPage(ApiClient apiClient, Portfolio selectedPortfolio)
            {
                InitializeComponent();
                _apiClient = apiClient;
                _selectedPortfolio = selectedPortfolio;
                DataContext = this;
                LoadStocks();
            }

            private async void LoadStocks()
            {

                try
                {
                    await LoadPortfolioHistory();

                    ApiService apiService = new ApiService();
                    List<PortfolioStocks> stocks = await FindAllPortfolioStocksByPortfolioId(_selectedPortfolio.Id);
                    List<PortfolioStockDisplay> displayStocks = new List<PortfolioStockDisplay>();
                    double totalInitialValue = 0;
                    TotalCurrentValue = 0;

                    foreach (var stock in stocks)
                    {
                        var latestQuote = await _apiClient.GetStockQuote(stock.TickerSymbol);
                        if (latestQuote != null)
                        {
                            var displayStock = PortfolioStockDisplay.FromPortfolioStock(stock, (decimal)latestQuote.Price);
                            displayStock.TotalValue = displayStock.Quantity * displayStock.CurrentPrice;
                            displayStocks.Add(displayStock);
                            totalInitialValue += displayStock.Quantity * (double)stock.PurchasePrice;
                            TotalCurrentValue += displayStock.Quantity * (double)displayStock.CurrentPrice;
                        }
                    }
                    _selectedPortfolio.TotalValue = (int)TotalCurrentValue;

                    await apiService.UpdatePortfolio(_selectedPortfolio);
                
                    if (totalInitialValue > 0)
                        TotalPercentChange = ((TotalCurrentValue - totalInitialValue) / totalInitialValue) * 100;
                    else
                    {
                        TotalPercentChange = 0;
                    }

                    if (TotalPercentChange > 0)
                    {
                        TotalValueText.Foreground = new SolidColorBrush(Colors.Green);
                        TotalPercentChangeText.Foreground = new SolidColorBrush(Colors.Green);
                    }
                    else
                    {
                        TotalValueText.Foreground = new SolidColorBrush(Colors.Red);
                        TotalPercentChangeText.Foreground = new SolidColorBrush(Colors.Red);
                    }


                    PortfolioStockListBinding.ItemsSource = displayStocks;
                    TotalValueText.Text = $"Total Current Value: ${TotalCurrentValue:F2}";
                    TotalPercentChangeText.Text = $"Change: {TotalPercentChange:F2}%";

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading stocks: " + ex.Message);
                }

            }

        private async Task<DateTime> GetEarliestPurchaseDate()
        {
            List<PortfolioStocks> stocks = await FindAllPortfolioStocksByPortfolioId(_selectedPortfolio.Id);
            return stocks.Min(s => s.PurchaseDate);
        }

        private async Task LoadPortfolioHistory()
        {
            try
            {
                DateTime startDate = await GetEarliestPurchaseDate();
                DateTime today = DateTime.Today;
                PortfolioValues.Clear();
                Dates.Clear();
                List<HistoricalDataResponse> historicalDataResponses = new List<HistoricalDataResponse>();

                // Get all stocks once
                List<PortfolioStocks> allStocks = await FindAllPortfolioStocksByPortfolioId(_selectedPortfolio.Id);

                // Debug the stocks we're working with
                foreach (var stock in allStocks)
                {
                    // Fetch historical data only for the dates after the stock was purchased
                    var historicalData = await _apiClient.GetStockQuoteHistoricalDataFromADate(stock.TickerSymbol, stock.PurchaseDate.ToString("yyyy-MM-dd"), today.ToString("yyyy-MM-dd"));
                    historicalDataResponses.Add(historicalData);
                    Debug.WriteLine($"Stock: {stock.TickerSymbol}, Quantity: {stock.Quantity}, Purchase Date: {stock.PurchaseDate}");
                }

                for (DateTime date = startDate; date <= today; date = date.AddDays(1))
                {
                    double dailyTotal = 0;

                    foreach (var stock in allStocks)
                    {
                        // Exclude stocks purchased after the current date
                        if (stock.PurchaseDate <= date)
                        {
                            // Find the corresponding historical data for the current stock and date
                            var historicalData = historicalDataResponses
                                .FirstOrDefault(h => h.Symbol == stock.TickerSymbol)
                                ?.Historical
                                .FirstOrDefault(h => h.Date == date.ToString("yyyy-MM-dd"));

                            if (historicalData != null)
                            {
                                double stockPrice = historicalData.Close;
                                double stockValue = stock.Quantity * stockPrice;
                                dailyTotal += stockValue;
                            }
                        }
                    }

                    if (dailyTotal > 0)
                    {
                        Debug.WriteLine($"Adding value for {date:yyyy-MM-dd}: ${dailyTotal}");
                        PortfolioValues.Add(dailyTotal);
                        Dates.Add(date.ToString("yyyy-MM-dd"));
                    }
                }

                // Update chart
                portfolioChart.Series[0].Values = PortfolioValues;
                portfolioChart.AxisX[0].Labels = Dates;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                MessageBox.Show("Error loading portfolio history: " + ex.Message);
            }
        }


        // Add this helper method to handle market holidays and weekends
        private bool IsMarketHoliday(DateTime date)
        {
            // Check if it's a weekend
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                return true;

            // Add major US market holidays here
            var holidays = new List<(int month, int day)>
            {
                (1, 1),   // New Year's Day
                (7, 4),   // Independence Day
                (12, 25), // Christmas
                // Add more holidays as needed
            };

            return holidays.Any(h => h.month == date.Month && h.day == date.Day);
        }


        private async Task<List<PortfolioStocks>> FindAllPortfolioStocksByPortfolioId(int portfolioId)
            {
                try
                {
                    ApiService apiService = new ApiService();
                    List<PortfolioStocks> allStocks = await apiService.GetPortfoliosStocks();
                    return allStocks.Where(p => p.PortfolioId == portfolioId).ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error fetching stocks: " + ex.Message);
                    return new List<PortfolioStocks>();
                }
            }

            private void OpenAddInvestmentDialog(object sender, RoutedEventArgs e)
            {
                AddInvestmentDialogHost.IsOpen = true;
            }

            private void CloseAddInvestmentDialog(object sender, RoutedEventArgs e)
            {
                AddInvestmentDialogHost.IsOpen = false;
            }

            private async void AddInvestment(object sender, RoutedEventArgs e)
            {
                try
                {

                    PortfolioStocks portfolioStock = new PortfolioStocks
                    {
                        PortfolioId = _selectedPortfolio.Id,
                        TickerSymbol = StockSymbolTextBox.Text,
                        Quantity = Convert.ToInt32(QuantityTextBox.Text),
                        PurchasePrice = Convert.ToDecimal(PriceTextBox.Text),
                        TotalValue = Convert.ToInt32(QuantityTextBox.Text) * Convert.ToDecimal(PriceTextBox.Text),
                        PurchaseDate = InvestmentDatePicker.SelectedDate ?? DateTime.Today
                    };

                    ApiService apiService = new ApiService();
                    int result = await apiService.InsertPortfolioStocks(portfolioStock);
                    if (result == 1)
                    {
                        MessageBox.Show("Investment successfully added!");
                        LoadStocks();
                    }
                    else
                    {
                        MessageBox.Show("Failed to add investment.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error adding investment: " + ex.Message);
                }
            }

            private async void InvestmentDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
            {
                if (InvestmentDatePicker.SelectedDate != null)
                {
                    await GetStockData();
                    if (quote?.Historical != null && quote.Historical.Count > 0)
                    {
                        HistoricalQuote latestData = quote.Historical[0];
                        PriceTextBox.Text = latestData.Close.ToString();
                    }
                }
            }

            private async Task GetStockData()
            {
                if (!string.IsNullOrWhiteSpace(StockSymbolTextBox.Text) && InvestmentDatePicker.SelectedDate.HasValue)
                {
                    string date = InvestmentDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd");
                    quote = await _apiClient.GetStockQuoteHistoricalDataFromADate(StockSymbolTextBox.Text, date, date);
                }
            }



            private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            {
                if (sender is Border border && border.DataContext is PortfolioStockDisplay stock)
                {
                    string clickedSymbol = stock.TickerSymbol;
                    // Navigate to the detail page with the clicked symbol
                    var mainWindow = (MainWindow)Application.Current.MainWindow;
                    mainWindow.NavigateToStockPage(clickedSymbol);
                }
            }


            private async void SellStock(object sender, RoutedEventArgs e)
            {

                if (sender is Button button && button.DataContext is PortfolioStockDisplay stock)
                {
                    try
                    {
                        ApiService apiService = new ApiService();
                        int isDeleted = await apiService.DeletePortfolioStocks(stock.Id);
                        if (isDeleted == 1)
                        {
                            MessageBox.Show("Stock deleted successfully.");
                            LoadStocks();
                        }
                        else
                        {
                            MessageBox.Show("Failed to delete stock.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error deleting stock: " + ex.Message);
                    }
                }
            }
        }
    }




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
        public int count = 0;
        public decimal TotalInvestmentPreview = 0;
        public double TotalCurrentValue { get; private set; }
        public double TotalPercentChange { get; private set; }

        public ChartValues<double> PortfolioValues { get; set; } = new ChartValues<double>();
        public List<string> Dates { get; set; } = new List<string>();
        public Func<double, string> ValueFormatter { get; set; } = value => $"${value:F2}";

        // Constructor to initialize the PortfolioPage with ApiClient and selected Portfolio
        public PortfolioPage(ApiClient apiClient, Portfolio selectedPortfolio)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _selectedPortfolio = selectedPortfolio;
            DataContext = this;

            // Initialize chart data
            PortfolioValues = new ChartValues<double>();
            Dates = new List<string>();

            // Load data asynchronously when the page is loaded
            Loaded += async (s, e) => await LoadStocks();
        }

        // Method to load stocks asynchronously
        private async Task LoadStocks()
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

                // Update UI elements based on the total percent change
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

        // Method to get the earliest purchase date of stocks in the portfolio
        private async Task<DateTime> GetEarliestPurchaseDate()
        {
            List<PortfolioStocks> stocks = await FindAllPortfolioStocksByPortfolioId(_selectedPortfolio.Id);
            return stocks.Min(s => s.PurchaseDate);
        }

        // Method to load portfolio history asynchronously
        private async Task LoadPortfolioHistory()
        {
            try
            {
                // Clear existing data
                PortfolioValues.Clear();
                Dates.Clear();

                DateTime startDate = await GetEarliestPurchaseDate();
                DateTime today = DateTime.Today;

                // Get all stocks for this portfolio
                List<PortfolioStocks> allStocks = await FindAllPortfolioStocksByPortfolioId(_selectedPortfolio.Id);

                // Pre-fetch all historical data at once
                var stockHistoricalData = new Dictionary<string, List<HistoricalQuote>>();

                foreach (var stock in allStocks)
                {
                    // Ensure we get data from purchase date to today for each stock
                    var historicalResponse = await _apiClient.GetStockQuoteHistoricalDataFromADate(
                        stock.TickerSymbol,
                        stock.PurchaseDate.ToString("yyyy-MM-dd"),
                        today.ToString("yyyy-MM-dd")
                    );

                    if (historicalResponse?.Historical != null)
                    {
                        stockHistoricalData[stock.TickerSymbol] = historicalResponse.Historical
                            .OrderBy(h => DateTime.Parse(h.Date))
                            .ToList();
                    }
                }

                // Process day by day
                double previousValue = 0;
                for (DateTime date = startDate; date <= today; date = date.AddDays(1))
                {
                    double dailyTotal = 0;

                    // Skip weekends and holidays but maintain the previous value
                    if (IsMarketHoliday(date))
                    {
                        PortfolioValues.Add(previousValue);
                        Dates.Add(date.ToString("yyyy-MM-dd"));
                        continue;
                    }

                    foreach (var stock in allStocks)
                    {
                        // Only include stocks that were purchased on or before this date
                        if (stock.PurchaseDate <= date && stockHistoricalData.ContainsKey(stock.TickerSymbol))
                        {
                            var stockQuotes = stockHistoricalData[stock.TickerSymbol];
                            var relevantQuote = stockQuotes
                                .Where(q => DateTime.Parse(q.Date) <= date)
                                .OrderByDescending(q => DateTime.Parse(q.Date))
                                .FirstOrDefault();

                            if (relevantQuote != null)
                            {
                                dailyTotal += stock.Quantity * relevantQuote.Close;
                            }
                            else
                            {
                                // If no quote is available, use purchase price
                                dailyTotal += stock.Quantity * (double)stock.PurchasePrice;
                            }
                        }
                    }

                    PortfolioValues.Add(dailyTotal);
                    Dates.Add(date.ToString("yyyy-MM-dd"));
                    previousValue = dailyTotal;
                }

                // Update the chart
                await Dispatcher.InvokeAsync(() =>
                {
                    portfolioChart.Series[0].Values = new ChartValues<double>(PortfolioValues);
                    portfolioChart.AxisX[0].Labels = new List<string>(Dates);
                    portfolioChart.Update(true);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading portfolio history: " + ex.Message);
            }
        }

        // Helper method to handle market holidays and weekends
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

        // Method to find all portfolio stocks by portfolio ID
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

        // Method to open the add investment dialog
        private void OpenAddInvestmentDialog(object sender, RoutedEventArgs e)
        {
            AddInvestmentDialogHost.IsOpen = true;
        }

        // Method to close the add investment dialog
        private void CloseAddInvestmentDialog(object sender, RoutedEventArgs e)
        {
            AddInvestmentDialogHost.IsOpen = false;
        }

        // Method to add an investment
        private async void AddInvestment(object sender, RoutedEventArgs e)
        {
            try
            {
                AddInvestmentDialogHost.IsOpen = false;  // Close dialog first

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
                    // Clear the chart data first
                    PortfolioValues.Clear();
                    Dates.Clear();

                    // Force UI update
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadStocks(); // This will call LoadPortfolioHistory

                        // Force chart refresh
                        portfolioChart.Update(true);
                        RefreshChart(); // Add this line
                    });

                    MessageBox.Show("Investment successfully added!");
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

        // Method to handle date selection change in the investment date picker
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
                TotalInvestmentPreview = (long)(Convert.ToInt32(QuantityTextBox.Text) * Convert.ToDecimal(PriceTextBox.Text));
            }
            else
            {
                // Handle the case where no date is selected
            }
        }

        // Method to get stock data based on the selected date and stock symbol
        private async Task GetStockData()
        {
            if (!string.IsNullOrWhiteSpace(StockSymbolTextBox.Text) && InvestmentDatePicker.SelectedDate.HasValue)
            {
                string date = InvestmentDatePicker.SelectedDate.Value.ToString("yyyy-MM-dd");
                quote = await _apiClient.GetStockQuoteHistoricalDataFromADate(StockSymbolTextBox.Text, date, date);
            }
        }

        // Method to handle mouse left button up event on a border
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

        // Method to sell a stock
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
                        RefreshChart(); // Add this line
                        await LoadPortfolioHistory(); // <-- ADD THIS LINE
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

        // Method to refresh the chart
        private void RefreshChart()
        {
            // Force chart to clear and redraw
            portfolioChart.Series[0].Values = null;
            portfolioChart.Series[0].Values = new ChartValues<double>(PortfolioValues);
            portfolioChart.AxisX[0].Labels = null;
            portfolioChart.AxisX[0].Labels = Dates;
        }

        // Event handler for when the portfolio chart is loaded
        private void portfolioChart_Loaded(object sender, RoutedEventArgs e)
        {
            // Add any initialization code for the chart here
        }

        // Method to decrement the quantity in the quantity text box
        private void DecrementQuantity(object sender, RoutedEventArgs e)
        {
            string quantity = QuantityTextBox.Text;

            if (Convert.ToInt32(quantity) > 0)
            {
                count--;
                QuantityTextBox.Text = (Convert.ToInt32(quantity) - 1).ToString();
            }
            else
            {
                count = 0;
            }
        }

        // Method to validate decimal input in a text box
        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Add validation logic for decimal input here
        }

        // Method to increment the quantity in the quantity text box
        private void IncrementQuantity(object sender, RoutedEventArgs e)
        {
            string quantity = QuantityTextBox.Text;
            if (quantity == "" || Convert.ToInt32(quantity) == 0)
            {
                count = 1;
                QuantityTextBox.Text = (Convert.ToInt32(quantity) + 1).ToString();
            }
            else if (quantity == "0")
            {
                QuantityTextBox.Text = "1";
            }
            else
            {
                count++;
                QuantityTextBox.Text = (Convert.ToInt32(quantity) + 1).ToString();
            }
        }

        // Method to validate number input in a text box
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Add validation logic for number input here
        }

        // Method to refresh the input fields
        private void Refersh(object sender, RoutedEventArgs e)
        {
            InvestmentDatePicker.SelectedDate = null;
            StockSymbolTextBox.Text = "";
            QuantityTextBox.Text = "";
            PriceTextBox.Text = "";
            TotalInvestmentPreview = 0;
            quote = null;
            latestQuote = null;
        }

        // Method to navigate back to the landing page
        private void NavigateBack_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToLandingPage();
        }

        private void ViewDetails(object sender, RoutedEventArgs e)
        {

        }
    }
    }




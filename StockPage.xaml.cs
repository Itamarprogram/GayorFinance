using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using model;
using GayorFinance.Services;


namespace GayorFinance
{
    public partial class StockPage : Page
    {
        private readonly ApiClient _apiClient;
        private string _symbol;
        private List<HistoricalQuote> _cachedHistoricalData; // Cache for daily data (5Y, 1Y, YTD)
        private List<HistoricalIntraDayQuote> _cachedFiveDayData;  // Cache for 5D hourly data
        private List<HistoricalIntraDayQuote> _cachedOneDayData; // Cache for 1D intraday data
        private StockQuote _currentStockQuote;

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }
        public LiveCharts.Wpf.Separator Separator { get; set; }

        public StockPage(string symbol, ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            _symbol = symbol;

            InitializeChartProperties();
            DataContext = this;

            LoadStockDataAsync(symbol, "1Y");
        }

        private void InitializeChartProperties()
        {
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Stock Price",
                    Values = new ChartValues<double>(),
                    PointGeometry = null,
                    StrokeThickness = 2,
                    LineSmoothness = 0,
                    Fill = Brushes.Transparent,
                    DataLabels = false
                }
            };

            YFormatter = value => $"${value:F2}";
            Separator = new LiveCharts.Wpf.Separator { Step = 1, IsEnabled = true };
        }


        private async Task LoadStockDataAsync(string symbol, string timeframe)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible; // Show loading overlay

                var quote = await _apiClient.GetStockQuote(symbol);
                if (quote == null)
                    throw new Exception("Unable to fetch stock quote data.");
                _currentStockQuote = quote;
                UpdateStockInfo(quote);

                switch (timeframe)
                {
                    case "5Y":
                    case "1Y":
                    case "YTD":
                        if (_cachedHistoricalData == null)
                        {
                            var historicalData = await _apiClient.GetStockQuoteHistoricalData(symbol);
                            if (historicalData == null || !historicalData.Historical.Any())
                                throw new Exception("No historical data available.");

                            _cachedHistoricalData = historicalData.Historical.OrderBy(h => DateTime.Parse(h.Date)).ToList();
                        }

                        var filteredHistoricalData = FilterHistoricalData(_cachedHistoricalData, timeframe);
                        UpdateChart(filteredHistoricalData, timeframe);
                        DisplayPeriodChange(filteredHistoricalData, timeframe);

                        break;
                    case "5D":

                        if (_cachedFiveDayData == null)
                        {
                            // Fetch and cache 5-day hourly data (intraday)
                            var fiveDaysData = await _apiClient.GetStockQouteFiveDaysData(symbol);
                            if (fiveDaysData == null)
                            {
                                ClearChart();
                                ChangeFromStart.Text = "No data available";
                                ChangeFromStart.Foreground = Brushes.Black;
                                return;
                            }

                            _cachedFiveDayData = fiveDaysData; // Changed this line
                        }
                        else
                        {
                            // Refresh 5-day hourly data if cache exists
                            var fiveDaysData = await _apiClient.GetStockQouteFiveDaysData(symbol);
                            if (fiveDaysData != null)
                            {
                                _cachedFiveDayData = fiveDaysData; // Changed this line
                            }
                            if (_cachedFiveDayData == null || !_cachedFiveDayData.Any())
                            {
                                ClearChart();
                                ChangeFromStart.Text = "No data available";
                                ChangeFromStart.Foreground = Brushes.Black;
                                return;
                            }
                        }
                        // Ensure that _cachedFiveDayData is not null
                        if (_cachedFiveDayData.Any())
                        {
                            var orderedData = _cachedFiveDayData.OrderBy(quote => quote.Date).ToList();
                            UpdateIntraDayChart(orderedData, timeframe);
                            DisplayPeriodChange(orderedData.Select(quote => new HistoricalQuote
                            {
                                Date = quote.Date.ToString("yyyy-MM-dd"),
                                Close = quote.Close,
                            }).ToList(), timeframe);
                        }
                        else
                        {
                            ClearChart();
                            ChangeFromStart.Text = "No data available";
                            ChangeFromStart.Foreground = Brushes.Black;
                        }

                        break;
                    case "1D":  

                        if (_cachedOneDayData == null)
                        {
                            // Fetch and cache 1-day intraday data
                            var oneDayData = await _apiClient.GetStockQouteTodayData(symbol);
                            if (oneDayData == null)
                            {
                                ClearChart();
                                ChangeFromStart.Text = "No data available";
                                ChangeFromStart.Foreground = Brushes.Black;
                                return;
                            }
                            _cachedOneDayData = oneDayData; // Changed this line
                        }
                        else
                        {
                            // Refresh 1 day intraday data if cache exists
                            var oneDayData = await _apiClient.GetStockQouteTodayData(symbol);
                            if (oneDayData != null)
                            {
                                _cachedOneDayData = oneDayData; // Changed this line
                            }
                            if (_cachedOneDayData == null || !_cachedOneDayData.Any())
                            {
                                ClearChart();
                                ChangeFromStart.Text = "No data available";
                                ChangeFromStart.Foreground = Brushes.Black;
                                return;
                            }
                        }


                        // Ensure that _cachedOneDayData is not null
                        if (_cachedOneDayData.Any())
                        {
                            var orderedData = _cachedOneDayData.OrderBy(quote => quote.Date).ToList();
                            UpdateIntraDayChart(orderedData, timeframe);
                            DisplayPeriodChange(orderedData.Select(quote => new HistoricalQuote
                            {
                                Date = quote.Date.ToString("yyyy-MM-dd"),
                                Close = quote.Close,
                            }).ToList(), timeframe);


                        }
                        else
                        {
                            ClearChart();
                            ChangeFromStart.Text = "No data available";
                            ChangeFromStart.Foreground = Brushes.Black;
                        }


                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data for symbol {_symbol}: {ex.Message}");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
        private void UpdateStockInfo(StockQuote quote)
        {
            Symbol.Text = quote.Symbol;
            CompanyName.Text = quote.Name;
            Price.Text = $"${quote.Price:F2}";
            ChangePrecentage.Text = $"{quote.ChangesPercentage / 100:+0.00%;-0.00%;0.00%}";
            Change.Text = $"{quote.Change:+0.00;-0.00;0.00}";
            MarketCap.Text = $"{quote.MarketCap:C0}";
            Volume.Text = $"{quote.Volume:N0}";
            DayRange.Text = $"{quote.DayLow:F2} - {quote.DayHigh:F2}";
            FiftyTwoWeekRange.Text = $"{quote.YearLow:F2} - {quote.YearHigh:F2}";
            PERatio.Text = $"{quote.Pe:F2}";
            EarningsAnnouncement.Text = $"{DateTime.Parse(quote.EarningsAnnouncement):MMMM dd, yyyy}";
            ChangePrecentage.Foreground = quote.ChangesPercentage >= 0 ? Brushes.Green : Brushes.Red;
            Change.Foreground = quote.Change >= 0 ? Brushes.Green : Brushes.Red;
            if (_currentStockQuote != null)
            {
                ChangePrecentage.Text = $"{_currentStockQuote.ChangesPercentage / 100:+0.00%;-0.00%;0.00%}";
                Change.Text = $"{_currentStockQuote.Change:+0.00;-0.00;0.00}";
                ChangePrecentage.Foreground = _currentStockQuote.ChangesPercentage >= 0 ? Brushes.Green : Brushes.Red;
                Change.Foreground = _currentStockQuote.Change >= 0 ? Brushes.Green : Brushes.Red;
            }
        }

        private void UpdateChart(List<HistoricalQuote> historicalQuotes, string timeframe)
        {
            if (historicalQuotes == null || historicalQuotes.Count == 0)
            {
                // Handle the case where there's no data, this will prevent the zero division error
                SeriesCollection[0].Values = new ChartValues<double>();
                Labels = new string[0]; // Clear labels
                DataContext = null;
                DataContext = this;
                return;
            }
            var values = new ChartValues<double>();
            var labels = new List<string>();

            int labelStep = DetermineLabelStep(historicalQuotes.Count, timeframe);

            for (int i = 0; i < historicalQuotes.Count; i++)
            {
                var quote = historicalQuotes[i];
                values.Add(quote.Close);

                // X-axis labels
                if (i % labelStep == 0)
                {
                    labels.Add(DateTime.Parse(quote.Date).ToString("MMM yyyy"));
                }
                else
                {
                    labels.Add(""); // Empty spacing for labels
                }
            }

            SeriesCollection[0].Values = values;
            Labels = labels.ToArray();


            // Tooltips display additional information
            SeriesCollection[0].LabelPoint = chartPoint =>
            {
                int index = (int)chartPoint.X;
                var dataPoint = historicalQuotes[index];

                // Calculate percentage change
                double changesPercentage = index > 0 && historicalQuotes[index - 1].Close != 0
                    ? (dataPoint.Close - historicalQuotes[index - 1].Close) / historicalQuotes[index - 1].Close * 100
                    : 0;

                return $"Date: {dataPoint.Date}\nPrice: {dataPoint.Close:F2}ִ$\nChange: {(index > 0 ? dataPoint.Close - historicalQuotes[index - 1].Close : 0):F2}\nChange: {(index > 0 && historicalQuotes[index - 1].Close != 0 ? changesPercentage : 0):F2}%";
            };

            // Refresh bindings
            DataContext = null;
            DataContext = this;
        }


        private void UpdateIntraDayChart(List<HistoricalIntraDayQuote> intraDayQuotes, string timeframe)
        {

            var values = new ChartValues<double>();
            var labels = new List<string>();
            int labelStep = DetermineLabelStep(intraDayQuotes.Count, timeframe);

            if (intraDayQuotes == null || intraDayQuotes.Count == 0)
            {
                // Handle the case where there's no data, this will prevent the zero division error
                SeriesCollection[0].Values = new ChartValues<double>();
                Labels = new string[0]; // Clear labels
                DataContext = null;
                DataContext = this;
                return;
            }

            for (int i = 0; i < intraDayQuotes.Count; i++)
            {
                var quote = intraDayQuotes[i];
                values.Add(quote.Close);

                if (i % labelStep == 0)
                {
                    labels.Add(quote.Date.ToString(timeframe == "1D" ? "HH:mm" : "ddd HH:mm"));
                }
                else
                {
                    labels.Add(""); // Empty spacing for labels
                }

            }

            SeriesCollection[0].Values = values;
            Labels = labels.ToArray();

            // Tooltips display additional information
            SeriesCollection[0].LabelPoint = chartPoint =>
            {
                int index = (int)chartPoint.X;
                var dataPoint = intraDayQuotes[index];

                // Calculate percentage change
                double changesPercentage = index > 0 && intraDayQuotes[index - 1].Close != 0
                 ? (dataPoint.Close - intraDayQuotes[index - 1].Close) / intraDayQuotes[index - 1].Close * 100
                 : 0;


                return $"Date: {dataPoint.Date:g}\nPrice: {dataPoint.Close:F2}ִ$\nChange: {(index > 0 ? dataPoint.Close - intraDayQuotes[index - 1].Close : 0):F2}\nChange: {(index > 0 && intraDayQuotes[index - 1].Close != 0 ? changesPercentage : 0):F2}%";
            };


            // Refresh bindings
            DataContext = null;
            DataContext = this;
        }


        private int DetermineLabelStep(int dataCount, string timeframe)
        {
            return timeframe switch
            {
                "5D" => Math.Max(1, dataCount / 5), // Every hour
                "1D" => Math.Max(1, dataCount / 4), // every 15 min, previously 8
                "1Y" => Math.Max(1, dataCount / 12), // Approx. every month
                "YTD" => Math.Max(1, dataCount / 12),// Approx. every month
                "5Y" => Math.Max(1, dataCount / 20), // Approx. every 3 months
                _ => 1
            };
        }
        private List<HistoricalQuote> FilterHistoricalData(List<HistoricalQuote> historicalData, string timeframe)
        {
            if (historicalData == null || !historicalData.Any())
            {
                return new List<HistoricalQuote>(); // Return empty if no data
            }
            DateTime today = DateTime.Today;

            // Downsample data for large timeframes
            return timeframe switch
            {
                "1Y" => historicalData.Where(h => DateTime.Parse(h.Date) >= today.AddYears(-1)).ToList(),
                "YTD" => historicalData.Where(h => DateTime.Parse(h.Date) >= new DateTime(today.Year, 1, 1)).ToList(),
                "5Y" => DownsampleData(historicalData, 60), // Downsample for performance
                _ => historicalData
            };
        }

        private List<HistoricalQuote> DownsampleData(List<HistoricalQuote> data, int targetPoints)
        {
            if (data.Count <= targetPoints) return data;

            var step = data.Count / targetPoints;
            var downsampled = new List<HistoricalQuote>();

            for (int i = 0; i < data.Count; i += step)
            {
                downsampled.Add(data[i]);
            }

            return downsampled;
        }


        private void DisplayPeriodChange(List<HistoricalQuote> historicalQuotes, string timeframe)
        {
            if (historicalQuotes == null || historicalQuotes.Count < 2)
            {
                ChangeFromStart.Text = "No data available";
                ChangeFromStart.Foreground = Brushes.Black;
                return;
            }


            var start = historicalQuotes.First();
            var end = historicalQuotes.Last();
            double priceChange = end.Close - start.Close;
            double percentageChange = (priceChange / start.Close) * 100;

            ChangeFromStart.Text = $"Change: {priceChange:+0.00;-0.00;0.00} ({(start.Close != 0 ? percentageChange / 100 : 0):+0.00%;-0.00%;0.00%})";
            ChangeFromStart.Foreground = priceChange >= 0 ? Brushes.Green : Brushes.Red;

        }

        private async void TimeframeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string timeframe = button.Content.ToString();
                await LoadStockDataAsync(_symbol, timeframe);
            }
        }
        private void ClearChart()
        {
            SeriesCollection[0].Values.Clear();
            Labels = new string[0];
            DataContext = null;
            DataContext = this;

        }

        private void NavigateBack_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToLandingPage();
        }
    }
}
using System.Windows;
using System.Windows.Controls;
using GayorFinance.Controls;
using GayorFinance.Services;
using model;
using MyServices;   


namespace GayorFinance
{
    public partial class PortfolioPage : Page
    {
        private readonly ApiClient _apiClient;
        private HistoricalDataResponse quote; // Assuming StockQuote is the type of the returned data


        public PortfolioPage(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;

            DataContext = new Portfolio();
        }

        private void OpenAddInvestmentDialog(object sender, RoutedEventArgs e)
        {
            AddInvestmentDialogHost.IsOpen = true;
        }

        private void CloseAddInvestmentDialog(object sender, RoutedEventArgs e)
        {
            AddInvestmentDialogHost.IsOpen = false;
        }

        private void AddInvestment(object sender, RoutedEventArgs e)
        {
            Transcations transaction = new Transcations();
            
        }

        private async void InvestmentDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            await GetStockData();
            List<HistoricalQuote> historical = quote.Historical;
            HistoricalQuote historicalData = null;
            foreach (var item in historical)
            {
                historicalData = item;
                // Do something with the historical data
            }
            double price = historicalData.Close;
            PriceTextBox.Text = price.ToString();
        }

        private async Task GetStockData()
        {
            DateTime datetime = (DateTime)InvestmentDatePicker.SelectedDate;
            string date = datetime.ToString("yyyy-MM-dd");
            quote = await _apiClient.GetStockQuoteHistoricalDataFromADate(StockSymbolTextBox.Text, date);

        }
    }
}

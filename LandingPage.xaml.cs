using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GayorFinance.Controls;
using GayorFinance.Services;
using model;

namespace GayorFinance
{
    public partial class LandingPage : Page
    {
        public readonly ApiClient _apiClient;
        private List<StockQuote> _allStocks = new List<StockQuote>();
        public LandingPage(ApiClient apiClient)
        {
            InitializeComponent();
            _apiClient = apiClient;
            //LoadAllStocks();
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
                mainWindow.NavigateToStockPage(searchText, _apiClient);
            }
        }

        private void PortfolioClick(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToPortfolioPage();
        }
    }
}
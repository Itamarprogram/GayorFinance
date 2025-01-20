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
        public User currentUser;
        private readonly ApiClient _apiClient;

        public UserPortfolios(ApiClient apiClient)
        {
            InitializeComponent();
            currentUser = UserSession.Instance.CurrentUser;
            _apiClient = apiClient;
            LoadPortfolios();
        }

        private async void LoadPortfolios()
        {
            try
            {
                ApiService apiService = new ApiService();
                List<Portfolio> portfolios = await FindAllPortfoliosByUserId(currentUser.Id);
                PortfolioList.ItemsSource = portfolios;
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
            if (sender is Button button && button.DataContext is Portfolio portfolio)
            {
                try
                {
                    ApiService apiService = new ApiService();
                    int isDeleted = await apiService.DeletePortfolio(portfolio.Id);
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
            if (sender is Button button && button.DataContext is Portfolio selectedPortfolio)
            {
                var mainWindow = (MainWindow)Application.Current.MainWindow;
                mainWindow.NavigateTPortfolioPage(selectedPortfolio);
            }
        }

    }
}

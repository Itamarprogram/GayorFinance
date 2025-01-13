using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using GayorFinance.Services;
using model;
using Microsoft.Extensions.DependencyInjection;
using MyServices;
using System.Threading.Channels;


namespace GayorFinance
{
    public partial class SignIn : Page
    {
        private readonly IServiceProvider _serviceProvider;
        public Action OnSignInSuccess { get; set; }

        public SignIn(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
        }

        private void GoToSignUpButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToSignUp();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;
            ApiService apiService = new ApiService();
            UserList users = await apiService.GetUsers();
            User? foundUser = users.Find(users => users.Email == email && users.Password == password);
            if (foundUser != null)
            {
                MessageBox.Show("Successfully signed in!");
                OnSignInSuccess?.Invoke();
            }
            else
            {
                MessageBox.Show("User has not been found!");
            }
        }   
    }
}

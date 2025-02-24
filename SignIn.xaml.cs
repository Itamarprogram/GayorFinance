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
        private readonly IServiceProvider _serviceProvider; // Dependency injection for service provider
        public Action OnSignInSuccess { get; set; } // Action to be invoked on successful sign-in

        public SignIn(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider; // Initialize service provider
        }

        // Event handler for navigating to the Sign-Up page
        private void GoToSignUpButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToSignUp(); // Navigate to Sign-Up page
        }

        // Event handler for the Login button click
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text; // Get email from the text box
            string password = PasswordBox.Password; // Get password from the password box
            ApiService apiService = new ApiService(); // Create a new instance of ApiService
            UserList users = await apiService.GetUsers(); // Fetch the list of users from the API
            User? foundUser = users.Find(users => users.Email == email && users.Password == password); // Find the user with matching email and password
            if (foundUser != null)
            {
                MessageBox.Show("Successfully signed in!"); // Show success message
                UserSession.Instance.SetUser(foundUser); // Save the user globally
                OnSignInSuccess?.Invoke(); // Invoke the success action
            }
            else
            {
                MessageBox.Show("User has not been found!"); // Show error message
            }
        }

        // Event handler for toggling password visibility
        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            // Implementation for toggling password visibility
        }

        // Event handler for forgot password click
        private void ForgotPassword_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Implementation for forgot password functionality
        }
    }
}

using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace GayorFinance
{
    public partial class WelcomePage : Page
    {
        public WelcomePage()
        {
            InitializeComponent(); // Initialize the components of the page
        }

        // Event handler for the Sign In button click event
        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow; // Get the main window instance
            mainWindow.NavigateToSignIn(); // Navigate to the Sign In page
        }

        // Event handler for the Sign Up button click event
        private void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow; // Get the main window instance
            mainWindow.NavigateToSignUp(); // Navigate to the Sign Up page
        }
    }
}

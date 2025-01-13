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
            InitializeComponent();
        }

        private void SignInButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToSignIn();
        }

        private void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToSignUp();
        }
    }
}

using model;
using MyServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GayorFinance
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        // Current user object
        public User currentUser;
        // Observable collection of countries for data binding
        public ObservableCollection<Countries> CountryList { get; set; }
        // List of country objects
        public List<Countries> CountryObjectList { get; set; }

        // Constructor
        public Settings()
        {
            InitializeComponent();
            // Get the current user from the session
            currentUser = UserSession.Instance.CurrentUser;
            // Set the data context for data binding
            DataContext = this;
            // Initialize the country list
            CountryList = new ObservableCollection<Countries>();
            // Load countries asynchronously
            LoadCountriesAsync();
        }

        // Load user details into the UI
        private void LoadUser()
        {
            // Display user details
            NameDisplay.Text = $"{currentUser.FirstName} {currentUser.LastName}";
            UserEmailDisplay.Text = $"{currentUser.Email}";
            FirstNameTextBox.Text = currentUser.FirstName;
            LastNameTextBox.Text = currentUser.LastName;
            EmailTextBox.Text = currentUser.Email;
            DateOfBirthPicker.SelectedDate = currentUser.DateOfBirth;

            // Set the selected country in the combo box
            CountryComboBox.SelectedItem = CountryList.FirstOrDefault(c => c.CountryName == currentUser.Country.CountryName);
        }

        // Load countries asynchronously and then load user details
        private async void LoadCountriesAsync()
        {
            await GetAllCountries();
            LoadUser();
        }

        // Get all countries from the API
        private async Task GetAllCountries()
        {
            try
            {
                ApiService apiService = new ApiService();
                // Fetch countries from the API
                CountryObjectList = await apiService.GetCountries();
                // Clear the existing country list
                CountryList.Clear();
                // Add fetched countries to the observable collection
                foreach (var country in CountryObjectList)
                {
                    CountryList.Add(country);
                }
            }
            catch (Exception ex)
            {
                // Show error message if fetching countries fails
                MessageBox.Show("Error loading countries: " + ex.Message);
            }
        }

        // Save updated user settings
        private async Task SaveSettings(User UpdatedUser)
        {
            ApiService apiService = new ApiService();
            // Update user via the API
            int isUpdated = await apiService.UpdateUser(UpdatedUser);
            if (isUpdated == 1)
            {
                MessageBox.Show("User updated successfully!");
            }
            else
            {
                MessageBox.Show("User update failed!");
            }
        }

        // Event handler for save settings button click
        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            string NewPassword = "";
            try
            {
                // Check if all fields are filled
                if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                    !DateOfBirthPicker.SelectedDate.HasValue ||
                    CountryComboBox.SelectedItem as Countries == null)
                {
                    MessageBox.Show("Please fill in all fields.");
                    return;
                }

                // Validate current password
                if (CheckForPasswordValid(CurrentPasswordBox.Password))
                {
                    // Check if new passwords match
                    if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                    {
                        MessageBox.Show("Passwords do not match!");
                        return;
                    }
                    NewPassword = NewPasswordBox.Password;
                }
                else
                {
                    MessageBox.Show("Password is incorrect!");
                }

                SignUpPage signUpPage = new SignUpPage();

                // Validate email format
                if (!signUpPage.IsValidEmail(EmailTextBox.Text))
                {
                    MessageBox.Show("Please enter a valid email address.");
                    return;
                }

                // Validate new password format
                if (!signUpPage.IsValidPassword(NewPassword))
                {
                    MessageBox.Show("Password must be at least 8 characters long, include at least one uppercase letter and one special character.");
                    return;
                }

                // Create updated user object
                User UpdatedUser = new User
                {
                    Id = currentUser.Id,
                    FirstName = FirstNameTextBox.Text,
                    LastName = LastNameTextBox.Text,
                    Email = EmailTextBox.Text,
                    Password = NewPassword,
                    DateOfBirth = DateOfBirthPicker.SelectedDate.Value,
                    Country = (Countries)CountryComboBox.SelectedItem
                };

                // Save updated user settings
                await SaveSettings(UpdatedUser);
                // Update user session
                UserSession.Instance.SetUser(UpdatedUser);
            }
            catch (Exception ex)
            {
                // Show error message if updating user fails
                MessageBox.Show("Error updating user: " + ex.Message);
            }
        }

        // Validate current password
        private bool CheckForPasswordValid(String CurrentPasswordBox)
        {
            if (CurrentPasswordBox != currentUser.Password)
            {
                return false;
            }
            return true;
        }

        // Event handler for logout button click
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            // Clear user session
            UserSession.Instance.SetUser(null);
            // Navigate to sign-in page
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToSignIn();
        }

        // Event handler for delete account button click
        private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            ApiService apiService = new ApiService();
            // Delete user via the API
            await apiService.DeleteUser(currentUser.Id);
            // Clear user session
            UserSession.Instance.SetUser(null);
            // Navigate to sign-in page
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToSignIn();
        }

        // Event handler for navigate back button click
        private void NavigateBack_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to landing page
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToLandingPage();
        }
    }
}

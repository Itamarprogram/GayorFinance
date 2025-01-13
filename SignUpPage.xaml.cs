using model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MyServices;
using System.Windows.Navigation;
using System.Threading.Channels;
using System.Text.RegularExpressions;

namespace GayorFinance
{
    public partial class SignUpPage : Page
    {
        public ObservableCollection<Countries> CountryList { get; set; }
        public List<Countries> CountryObjectList { get; set; }
        public Action OnSignUpSuccess { get; set; }


        public SignUpPage()
        {
            InitializeComponent();
            DataContext = this;
            CountryList = new ObservableCollection<Countries>();
            LoadCountriesAsync();
        }

        private async void LoadCountriesAsync()
        {
            await GetAllCountries();
        }

        private async Task GetAllCountries()
        {
            try
            {
                ApiService apiService = new ApiService();
                CountryObjectList = await apiService.GetCountries();
                CountryList.Clear();
                foreach (var country in CountryObjectList)
                {
                    // Adding the entire country object to preserve all properties
                    CountryList.Add(country);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading countries: " + ex.Message);
            }
        }

        private async Task InsertSignUpUser(User user)
        {
            try
            {
                ApiService apiService = new ApiService();
                int result = await apiService.InsertUser(user);
                if (result == 1)
                {
                    MessageBox.Show("User has been successfully signed up!");
                }
                else
                {
                    MessageBox.Show("User has not been signed up!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inserting user: " + ex.Message);
            }
        }

        private async void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if all fields are filled
                if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PasswordTextBox.Password) ||
                    !DateOfBirthPicker.SelectedDate.HasValue ||
                    CountryComboBox.SelectedItem as Countries == null)
                {
                    MessageBox.Show("Please fill in all fields.");
                    return;
                }
                if(await CheckForEmailDuplicate())
                {
                    MessageBox.Show("Email already exists!");
                    return;
                }
                // Validate email format
                if (!IsValidEmail(EmailTextBox.Text))
                {
                    MessageBox.Show("Please enter a valid email address.");
                    return;
                }

                if (!IsValidPassword(PasswordTextBox.Password))
                {
                    MessageBox.Show("Password must be at least 8 characters long, include at least one uppercase letter and one special character.");
                    return;
                }

                Countries selectedCountry = CountryComboBox.SelectedItem as Countries;

                // All validations passed, create the user object
                User user = new User()
                {
                    FirstName = FirstNameTextBox.Text,
                    LastName = LastNameTextBox.Text,
                    Email = EmailTextBox.Text,
                    Password = PasswordTextBox.Password,
                    DateOfBirth = DateOfBirthPicker.SelectedDate.Value, // Assign the value of DateOfBirth
                    Country = selectedCountry // Assign the selected Country object to the user
                };

                await InsertSignUpUser(user);
                OnSignUpSuccess?.Invoke();

            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        // Helper method to validate email
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Regular expression for email validation
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, emailPattern);
        }
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Regex to validate the password
            string passwordPattern = @"^(?=.*[A-Z])(?=.*[!@#$%^&*(),.?""{}|<>]).{8,}$";
            return Regex.IsMatch(password, passwordPattern);
        }

        private async Task<bool> CheckForEmailDuplicate()
        {
            try
            {
                ApiService apiService = new ApiService();
                UserList users = await apiService.GetUsers();
                User? foundUser = users.Find(users => users.Email == EmailTextBox.Text);
                if (foundUser != null)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error checking for email duplicate: " + ex.Message);
            }

            return false;
        }


        private void GoToSignInButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToSignIn();
        }


    }
}
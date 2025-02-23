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
        public User currentUser;
        public ObservableCollection<Countries> CountryList { get; set; }
        public List<Countries> CountryObjectList { get; set; }

        public Settings()
        {
            InitializeComponent();
            currentUser = UserSession.Instance.CurrentUser;
            DataContext = this;
            CountryList = new ObservableCollection<Countries>();
            LoadCountriesAsync();


        }

        private void LoadUser()
        {
            NameDisplay.Text = $"{currentUser.FirstName} {currentUser.LastName}";
            UserEmailDisplay.Text = $"{currentUser.Email}";
            FirstNameTextBox.Text = currentUser.FirstName;
            LastNameTextBox.Text = currentUser.LastName;
            EmailTextBox.Text = currentUser.Email;
            DateOfBirthPicker.SelectedDate = currentUser.DateOfBirth;

            CountryComboBox.SelectedItem = CountryList.FirstOrDefault(c => c.CountryName == currentUser.Country.CountryName);


        }

        private async void LoadCountriesAsync()
        {
            await GetAllCountries();

            LoadUser();
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
        private async Task SaveSettings(User UpdatedUser)
        {
            ApiService apiService = new ApiService();
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

        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            string NewPassword = "";
            try
            {                // Check if all fields are filled
                if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                    !DateOfBirthPicker.SelectedDate.HasValue ||
                    CountryComboBox.SelectedItem as Countries == null)
                {
                    MessageBox.Show("Please fill in all fields.");
                    return;
                }

                if (CheckForPasswordValid(CurrentPasswordBox.Password))
                {
                    if(NewPasswordBox.Password != ConfirmPasswordBox.Password)
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
                //if (await signUpPage.CheckForEmailDuplicate(EmailTextBox.Text))
                //{
                //    MessageBox.Show("Email already exists!");
                //    return;
                //}

                if (!signUpPage.IsValidEmail(EmailTextBox.Text))
                {
                    MessageBox.Show("Please enter a valid email address.");
                    return;
                }

                if (!signUpPage.IsValidPassword(NewPassword))
                {
                    MessageBox.Show("Password must be at least 8 characters long, include at least one uppercase letter and one special character.");
                    return;
                }
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
                await SaveSettings(UpdatedUser);
                UserSession.Instance.SetUser(UpdatedUser);


            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating user: " + ex.Message);
            }

        }

        private bool CheckForPasswordValid(String CurrentPasswordBox)
        {
            if(CurrentPasswordBox != currentUser.Password)
            {
                return false;
            }
            return true;
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            UserSession.Instance.SetUser(null);
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToSignIn();

        }

        private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            ApiService apiService = new ApiService();
            await apiService.DeleteUser(currentUser.Id);
            UserSession.Instance.SetUser(null);
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToSignIn();
        }
        private void NavigateBack_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.NavigateToLandingPage();
        }
    }
}

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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace GayorFinance
{
    public partial class SignUpPage : Page
    {
        public ObservableCollection<Countries> CountryList { get; set; }
        public List<Countries> CountryObjectList { get; set; }
        public Action OnSignUpSuccess { get; set; }


        // Add new properties for enhanced features
        private int _currentStep = 1;
        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                _currentStep = value;
                OnPropertyChanged();
                UpdateStepVisibility();
                UpdateNavigationButtons();
            }
        }

        private int _passwordStrength;
        public int PasswordStrength
        {
            get => _passwordStrength;
            set
            {
                _passwordStrength = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PasswordStrengthColor));
            }
        }

        public Brush PasswordStrengthColor
        {
            get
            {
                return PasswordStrength switch
                {
                    <= 25 => new SolidColorBrush(Colors.Red),
                    <= 50 => new SolidColorBrush(Colors.Orange),
                    <= 75 => new SolidColorBrush(Colors.Yellow),
                    _ => new SolidColorBrush(Colors.Green)
                };
            }
        }

        private bool _isPasswordVisible;
        public bool IsPasswordVisible
        {
            get => _isPasswordVisible;
            set
            {
                _isPasswordVisible = value;
                OnPropertyChanged();
                UpdatePasswordVisibility();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SignUpPage()
        {
            InitializeComponent();
            DataContext = this;
            CountryList = new ObservableCollection<Countries>();
            LoadCountriesAsync();
            PasswordTextBox.PasswordChanged += (s, e) => UpdatePasswordStrength();
            VisiblePasswordBox.TextChanged += (s, e) => UpdatePasswordStrength();
        }

        private void UpdatePasswordVisibility()
        {
            if (IsPasswordVisible)
            {
                VisiblePasswordBox.Text = PasswordTextBox.Password;
                VisiblePasswordBox.Visibility = Visibility.Visible;
                PasswordTextBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                PasswordTextBox.Password = VisiblePasswordBox.Text;
                VisiblePasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
            }
        }

        private void UpdatePasswordStrength()
        {
            string password = IsPasswordVisible ? VisiblePasswordBox.Text : PasswordTextBox.Password;

            int strength = 0;
            if (password.Length >= 8) strength += 25;
            if (Regex.IsMatch(password, "[A-Z]")) strength += 25;
            if (Regex.IsMatch(password, "[a-z]")) strength += 25;
            if (Regex.IsMatch(password, "[0-9!@#$%^&*(),.?\"{}|<>]")) strength += 25;

            PasswordStrength = strength;
        }



        private void UpdateStepVisibility()
        {
            Step1Panel.Visibility = CurrentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Panel.Visibility = CurrentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Panel.Visibility = CurrentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
            Step4Panel.Visibility = CurrentStep == 4 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateNavigationButtons()
        {
            NextButton.Content = CurrentStep == 4 ? "Sign Up" : "Next";
        }

        private bool ValidateCurrentStep()
        {
            switch (CurrentStep)
            {
                case 1:
                    return !string.IsNullOrWhiteSpace(FirstNameTextBox.Text) &&
                           !string.IsNullOrWhiteSpace(LastNameTextBox.Text);
                case 2:
                    return !string.IsNullOrWhiteSpace(EmailTextBox.Text) &&
                           IsValidEmail(EmailTextBox.Text);
                case 3:
                    return PasswordStrength >= 75;
                case 4:
                    return DateOfBirthPicker.SelectedDate.HasValue &&
                           CountryComboBox.SelectedItem != null;
                default:
                    return false;
            }
        }

        private void NextStep_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateCurrentStep())
            {
                if (CurrentStep < 4)
                {
                    CurrentStep++;
                }
                else
                {
                    SignUpButton_Click(sender, e);
                }
            }
            else
            {
                MessageBox.Show("Please complete all required fields correctly before proceeding.");
            }
        }

        private void PreviousStep_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentStep > 1)
            {
                CurrentStep--;
            }
        }

        private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        private async void ShowSuccessAnimation()
        {
            SuccessIcon.Visibility = Visibility.Visible;
            var scaleAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new ElasticEase { EasingMode = EasingMode.EaseOut }
            };
            SuccessIcon.RenderTransform = new ScaleTransform();
            SuccessIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            SuccessIcon.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);

            await Task.Delay(2000);
            OnSignUpSuccess?.Invoke();
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
                if(await CheckForEmailDuplicate(EmailTextBox.Text))
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
        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Regular expression for email validation
            string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, emailPattern);
        }
        public bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // Regex to validate the password
            string passwordPattern = @"^(?=.*[A-Z])(?=.*[!@#$%^&*(),.?""{}|<>]).{8,}$";
            return Regex.IsMatch(password, passwordPattern);
        }

        public async Task<bool> CheckForEmailDuplicate(String EmailTextBox)
        {
            try
            {
                ApiService apiService = new ApiService();
                UserList users = await apiService.GetUsers();
                User? foundUser = users.Find(users => users.Email == EmailTextBox);
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
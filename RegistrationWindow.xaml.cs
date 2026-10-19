using System.Windows;

namespace SmartLib
{
    public partial class RegistrationWindow : Window
    {
        public RegistrationWindow()
        {
            InitializeComponent();
        }

        private void ValidateForm()
        {
            bool isNameValid = !string.IsNullOrWhiteSpace(NameInput.Text);
            NameErrorText.Text = "Name is required";
            NameErrorText.Visibility = isNameValid ? Visibility.Collapsed : Visibility.Visible;

            bool isEmailValid = EmailInput.Text.Contains('@') && EmailInput.Text.Contains('.');
            EmailErrorText.Text = "Email is invalid";
            EmailErrorText.Visibility = isEmailValid ? Visibility.Collapsed : Visibility.Visible;

            bool isPasswordValid = PasswordInput.Password.Length >= 6;
            PasswordErrorText.Text = "Password must be at least 6 characters";
            PasswordErrorText.Visibility = isPasswordValid ? Visibility.Collapsed : Visibility.Visible;

            bool doPasswordsMatch = ConfirmPasswordInput.Password == PasswordInput.Password;
            ConfirmPasswordErrorText.Text = "Passwords do not match";
            ConfirmPasswordErrorText.Visibility = doPasswordsMatch ? Visibility.Collapsed : Visibility.Visible;

            RegisterSubmitBtn.IsEnabled =
                isNameValid && isEmailValid && isPasswordValid && doPasswordsMatch;
        }

        private void NameInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void EmailInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateForm();
        }

        private void ConfirmPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateForm();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }

        private void RegisterSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Account successfully created!", "Success");
        }
    }
}

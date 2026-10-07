using System.Windows;

namespace SmartLib
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void ValidateForm()
        {
            bool isEmailValid = EmailInput.Text.Contains('@') && EmailInput.Text.Contains('.');
            EmailErrorText.Text = "Please enter a valid email address.";
            EmailErrorText.Visibility = isEmailValid ? Visibility.Collapsed : Visibility.Visible;

            bool isPasswordValid = PasswordInput.Password.Length >= 6;
            PasswordErrorText.Text = "Password must be at least 6 characters.";
            PasswordErrorText.Visibility = isPasswordValid ? Visibility.Collapsed : Visibility.Visible;

            LoginSubmitBtn.IsEnabled = isEmailValid && isPasswordValid;
        }

        private void EmailInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ValidateForm();
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateForm();
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }

        private void LoginSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            
            MessageBox.Show("Successfully logged in!", "Success");
        }
    }
}

using System.Windows;
using System.Net.Mail;
using Domain.Enums;
using Npgsql;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();

            DataContext = new LoginViewModel(this);
        }

        private void Credentials_Changed(object sender, RoutedEventArgs e)
        {
            LoginSubmitBtn.IsEnabled = !string.IsNullOrWhiteSpace(EmailInput.Text)
                && !string.IsNullOrEmpty(PasswordInput.Password);
            LoginErrorText.Visibility = Visibility.Collapsed;
        }

        private async void LoginSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            var email = EmailInput.Text.Trim();
            var password = PasswordInput.Password;

            if (!MailAddress.TryCreate(email, out _))
            {
                EmailErrorText.Visibility = Visibility.Visible;
                LoginErrorText.Visibility = Visibility.Collapsed;
                return;
            }

            EmailErrorText.Visibility = Visibility.Collapsed;
            PasswordErrorText.Visibility = string.IsNullOrEmpty(password)
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (string.IsNullOrEmpty(password))
            {
                return;
            }

            LoginSubmitBtn.IsEnabled = false;
            try
            {
                var user = await ((LoginViewModel)DataContext).AuthenticateAsync(email, password);
                if (user is null)
                {
                    LoginErrorText.Text = "Incorrect email or password, or the account is inactive.";
                    LoginErrorText.Visibility = Visibility.Visible;
                    return;
                }

                if (user.Role != UserRole.Admin)
                {
                    LoginErrorText.Text = "This account does not have administrator access.";
                    LoginErrorText.Visibility = Visibility.Visible;
                    return;
                }

                var dashboard = new AdminDashboardWindow(user.FullName);
                dashboard.Show();
                Application.Current.MainWindow = dashboard;
                Close();
            }
            catch (NpgsqlException)
            {
                LoginErrorText.Text = "Could not connect to the database. Check the connection and try again.";
                LoginErrorText.Visibility = Visibility.Visible;
            }
            catch (InvalidOperationException)
            {
                LoginErrorText.Text = "Could not initialize sign-in. Check the application database configuration.";
                LoginErrorText.Visibility = Visibility.Visible;
            }
            finally
            {
                if (IsVisible)
                {
                    LoginSubmitBtn.IsEnabled = !string.IsNullOrWhiteSpace(EmailInput.Text)
                        && !string.IsNullOrEmpty(PasswordInput.Password);
                }
            }
        }
    }
}

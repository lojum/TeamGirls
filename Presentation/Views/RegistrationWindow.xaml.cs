using System.Net.Mail;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Presentation.ViewModels;

namespace Presentation.Views
{
    public partial class RegistrationWindow : Window
    {
        public RegistrationWindow()
        {
            InitializeComponent();

            DataContext = new RegistrationViewModel(this);
        }

        private async void RegisterSubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            var name = NameInput.Text.Trim();
            var email = EmailInput.Text.Trim();
            var password = PasswordInput.Password;
            var confirmPassword = ConfirmPasswordInput.Password;

            SetError(NameErrorText, name.Length is 0 or > 150);
            SetError(EmailErrorText, !MailAddress.TryCreate(email, out _));
            SetError(PasswordErrorText, password.Length < 8);
            SetError(ConfirmPasswordErrorText, password != confirmPassword);

            if (name.Length is 0 or > 150
                || !MailAddress.TryCreate(email, out _)
                || password.Length < 8
                || password != confirmPassword)
            {
                return;
            }

            RegisterSubmitBtn.IsEnabled = false;
            try
            {
                await ((RegistrationViewModel)DataContext).RegisterAsync(name, email, password);
                MessageBox.Show(
                    this,
                    "Reader account created. Reader sign-in will be available later.",
                    "Registration complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                var welcomeWindow = new MainWindow();
                welcomeWindow.Show();
                Application.Current.MainWindow = welcomeWindow;
                Close();
            }
            catch (NpgsqlException exception)
            {
                if (IsUniqueEmailViolation(exception))
                {
                    MessageBox.Show(this, "An account with this email address already exists.", "Registration error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(this, "Could not connect to or write to the database. Check the database connection and try again.", "Database error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (DbUpdateException exception)
            {
                if (IsUniqueEmailViolation(exception))
                {
                    MessageBox.Show(this, "An account with this email address already exists.", "Registration error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show(this, "The database rejected the registration. Check the entered information and try again.", "Registration error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show(this, "Could not initialize registration. Check the application database configuration.", "Registration error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (IsVisible)
                {
                    RegisterSubmitBtn.IsEnabled = true;
                }
            }
        }

        private static void SetError(System.Windows.Controls.TextBlock errorText, bool hasError) =>
            errorText.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;

        private static bool IsUniqueEmailViolation(Exception exception)
        {
            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is PostgresException postgresException
                    && postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
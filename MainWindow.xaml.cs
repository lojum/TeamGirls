using System.Windows;

namespace SmartLib 
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            Close();
        }

        private void RegisterBtn_Click(object sender, RoutedEventArgs e)
        {
       
        RegistrationWindow regWindow = new RegistrationWindow();
        regWindow.Show();
       
        this.Close();
        }
    }
}
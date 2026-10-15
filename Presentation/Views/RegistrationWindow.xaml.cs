using System.Windows;
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
    }
}
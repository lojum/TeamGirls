using System.Windows;
using System.Windows.Input;
using Presentation.Commands;
using Presentation.Services;

namespace Presentation.ViewModels
{
    public class MainViewModel
    {
        private readonly IWindowNavigationService _navigationService;

        public ICommand OpenLoginCommand { get; }
        public ICommand OpenRegisterCommand { get; }

        public MainViewModel(IWindowNavigationService navigationService)
        {
            _navigationService = navigationService;
            OpenLoginCommand = new RelayCommand(_ => _navigationService.ShowLogin());
            OpenRegisterCommand = new RelayCommand(_ => _navigationService.ShowRegistration());
        }
    }
}

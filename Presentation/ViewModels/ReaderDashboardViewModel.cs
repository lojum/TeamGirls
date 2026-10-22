using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using BusinessLogic.Services; 
using Domain.Entities;
using Presentation.Commands;
using Presentation.Views;

namespace Presentation.ViewModels
{
    public class ReaderDashboardViewModel : INotifyPropertyChanged
    {
        private readonly Window _currentWindow;
        private readonly IBookService _bookService;
        
        private string _readerName;
        private string _searchText = "";

        public string ReaderName { get => _readerName; set { _readerName = value; OnPropertyChanged(); } }
        
        public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }
        
        public ObservableCollection<BookCatalogView> BooksList { get; set; } = new ObservableCollection<BookCatalogView>();

        public ICommand LogOutCommand { get; }
        public ICommand SearchCommand { get; }

        public ReaderDashboardViewModel(Window currentWindow, string readerName, IBookService bookService)
        {
            _currentWindow = currentWindow;
            _readerName = readerName;
            _bookService = bookService;

            LogOutCommand = new RelayCommand(ExecuteLogOut);
            SearchCommand = new RelayCommand(ExecuteSearch);
            
            ExecuteSearch(null);
        }

        private async void ExecuteSearch(object? obj)
        {
            try
            {
                var query = SearchText ?? string.Empty;
                
                var results = await _bookService.SearchBooksAsync(query);
                
                BooksList.Clear();
                foreach (var book in results)
                {
                    BooksList.Add(book);
                }

                if (results.Count == 0 && !string.IsNullOrWhiteSpace(query))
                {
                    MessageBox.Show("За вашим запитом нічого не знайдено. Спробуйте змінити критерії пошуку.", 
                                    "Результат пошуку", 
                                    MessageBoxButton.OK, 
                                    MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Помилка завантаження каталогу:\n{ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteLogOut(object? obj)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Application.Current.MainWindow = mainWindow;
            _currentWindow.Close();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
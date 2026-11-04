using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Presentation.Commands;
using Presentation.Services;

namespace Presentation.ViewModels
{
    public class ReaderDashboardViewModel : INotifyPropertyChanged
    {
        private readonly IWindowNavigationService _navigationService;
        private readonly List<BookCardViewModel> _books = CreateSampleBooks();
        private string _readerName = string.Empty;
        private string _searchText = string.Empty;
        private string _selectedCategory = "All Books";
        private string _selectedSection = "Book Search";
        private string _selectedSort = "Relevance";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string ReaderName
        {
            get => _readerName;
            set
            {
                if (SetProperty(ref _readerName, value))
                {
                    OnPropertyChanged(nameof(ReaderInitials));
                }
            }
        }

        public string ReaderInitials => string.Concat(
            ReaderName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(name => char.ToUpperInvariant(name[0])));

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    RefreshBooks();
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            private set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    foreach (var category in Categories)
                    {
                        category.IsSelected = category.Name == value;
                    }

                    OnPropertyChanged(nameof(ResultSummary));
                    RefreshBooks();
                }
            }
        }

        public string SelectedSection
        {
            get => _selectedSection;
            private set
            {
                if (SetProperty(ref _selectedSection, value))
                {
                    OnPropertyChanged(nameof(SectionTitle));
                    OnPropertyChanged(nameof(IsBookSearch));
                    OnPropertyChanged(nameof(IsAccount));
                    OnPropertyChanged(nameof(ShowCategories));
                    OnPropertyChanged(nameof(ResultSummary));
                    RefreshBooks();
                }
            }
        }

        public string SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (SetProperty(ref _selectedSort, value))
                {
                    RefreshBooks();
                }
            }
        }

        public string SectionTitle => SelectedSection switch
        {
            "Favorites" => "Your favorites",
            "My Loans" => "Your loans",
            "My Account" => "My account",
            _ => "Book Search"
        };

        public bool IsBookSearch => SelectedSection != "My Account";
        public bool IsAccount => SelectedSection == "My Account";
        public bool ShowCategories => SelectedSection == "Book Search";
        public bool ShowNoBooksMessage => IsBookSearch && VisibleBooks.Count == 0;
        public string ResultSummary { get; private set; } = string.Empty;

        public ObservableCollection<CategoryOptionViewModel> Categories { get; } = new(
        [
            new("All Books", true),
            new("Fiction"),
            new("Non-Fiction"),
            new("Science"),
            new("History"),
            new("Biography"),
            new("Art")
        ]);

        public ObservableCollection<BookCardViewModel> VisibleBooks { get; } = new();

        public ICommand LogOutCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand SelectSectionCommand { get; }
        public ICommand SelectCategoryCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public ReaderDashboardViewModel(IWindowNavigationService navigationService)
        {
            _navigationService = navigationService;
            LogOutCommand = new RelayCommand(_ => _navigationService.ShowMain());
            SearchCommand = new RelayCommand(_ => RefreshBooks());
            SelectSectionCommand = new RelayCommand(section =>
            {
                if (section is string value)
                {
                    SelectedSection = value;
                    if (value != "Book Search")
                    {
                        SelectedCategory = "All Books";
                    }
                }
            });
            SelectCategoryCommand = new RelayCommand(category =>
            {
                if (category is string value)
                {
                    SelectedCategory = value;
                }
            });
            ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
            foreach (var book in _books)
            {
                book.FavoriteChanged += (_, _) => RefreshBooks();
            }

            RefreshBooks();
        }

        public void SetReaderName(string readerName)
        {
            ReaderName = readerName;
        }

        private void RefreshBooks()
        {
            IEnumerable<BookCardViewModel> filtered = _books;

            if (SelectedSection == "Favorites")
            {
                filtered = filtered.Where(book => book.IsFavorite);
            }
            else if (SelectedSection == "My Loans")
            {
                filtered = filtered.Where(book => book.IsBorrowed);
            }
            else if (SelectedSection == "Book Search" && SelectedCategory != "All Books")
            {
                filtered = filtered.Where(book => book.Category == SelectedCategory);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(book =>
                    book.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                    || book.Author.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            filtered = SelectedSort switch
            {
                "Title A-Z" => filtered.OrderBy(book => book.Title),
                "Author" => filtered.OrderBy(book => book.Author),
                _ => filtered.OrderBy(book => book.SortOrder)
            };

            var results = filtered.ToList();
            VisibleBooks.Clear();
            foreach (var book in results)
            {
                VisibleBooks.Add(book);
            }
            OnPropertyChanged(nameof(ShowNoBooksMessage));

            ResultSummary = SelectedSection switch
            {
                "Favorites" => $"Showing {results.Count} favorite {(results.Count == 1 ? "book" : "books")}",
                "My Loans" => $"Showing {results.Count} borrowed {(results.Count == 1 ? "book" : "books")}",
                _ => $"Showing {results.Count} results"
            };
            OnPropertyChanged(nameof(ResultSummary));
        }

        private static List<BookCardViewModel> CreateSampleBooks() =>
        [
            new(1, "The Gilded Mist", "Dr. Alistair Gray", "Science", "Available", "#153B37", "#D7B978", true),
            new(2, "Structural Futures", "Mira Solis", "Science", "Available", "#193B58", "#D4E1E9", false),
            new(3, "The Lost Cities", "Eleanor West", "History", "Available", "#765C3D", "#F0D8A5", true),
            new(4, "Ocean's Edge", "Eliza Reed", "Biography", "Borrowed", "#17636A", "#F1D58B", false),
            new(5, "Mapping the Stars", "Dr. Alistair Gray", "Science", "Available", "#122A44", "#DFC68B", true),
            new(6, "Echoes of the Revolution", "Hana Mercier", "History", "Borrowed", "#523A36", "#F0D8CB", false),
            new(7, "The Art of Slow Living", "Clara Finch", "Non-Fiction", "Available", "#C9B89A", "#554536", true),
            new(8, "Beyond the Horizon", "Robert Vance", "Biography", "Available", "#592E58", "#FFD7A1", false),
            new(9, "The Red Nights", "Elif Shafak", "Fiction", "Available", "#743D3A", "#F3D0C2", true),
            new(10, "A Room of Blue", "Mara Ellis", "Art", "Available", "#2B536F", "#E0C89A", false)
        ];

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed class CategoryOptionViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Name { get; }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public CategoryOptionViewModel(string name, bool isSelected = false)
        {
            Name = name;
            _isSelected = isSelected;
        }
    }

    public sealed class BookCardViewModel : INotifyPropertyChanged
    {
        private bool _isFavorite;

        public int SortOrder { get; }
        public string Title { get; }
        public string Author { get; }
        public string Category { get; }
        public string Status { get; }
        public bool IsBorrowed => Status == "Borrowed";
        public Brush CoverBrush { get; }
        public Brush AccentBrush { get; }
        public bool IsFavorite
        {
            get => _isFavorite;
            private set
            {
                _isFavorite = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFavorite)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FavoriteGlyph)));
                FavoriteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string FavoriteGlyph => IsFavorite ? "♥" : "♡";
        public ICommand ToggleFavoriteCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? FavoriteChanged;

        public BookCardViewModel(
            int sortOrder,
            string title,
            string author,
            string category,
            string status,
            string coverColor,
            string accentColor,
            bool isFavorite)
        {
            SortOrder = sortOrder;
            Title = title;
            Author = author;
            Category = category;
            Status = status;
            CoverBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(coverColor));
            AccentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColor));
            _isFavorite = isFavorite;
            ToggleFavoriteCommand = new RelayCommand(_ => IsFavorite = !IsFavorite);
        }
    }
}

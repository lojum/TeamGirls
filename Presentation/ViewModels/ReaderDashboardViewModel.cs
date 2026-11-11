using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BusinessLogic.Services;
using Domain.Entities;
using Presentation.Commands;
using Presentation.Services;

namespace Presentation.ViewModels
{
    public class ReaderDashboardViewModel : INotifyPropertyChanged
    {
        private const string AllBooks = "All Books";

        private readonly IBookService _bookService;
        private readonly ILoanService _loanService;
        private readonly IWindowNavigationService _navigationService;
        private readonly List<BookCatalogView> _catalog = new();
        private readonly List<BookCardViewModel> _books = new();
        private string _readerName = string.Empty;
        private long _readerId;
        private bool _isLoaded;
        private string _searchText = string.Empty;
        private string _selectedCategory = AllBooks;
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
        public bool ShowNoBooksMessage => _isLoaded && IsBookSearch && VisibleBooks.Count == 0;
        public string ResultSummary { get; private set; } = string.Empty;

        // Категорії будуються з жанрів, які є в каталозі
        public ObservableCollection<CategoryOptionViewModel> Categories { get; } = new([new(AllBooks, true)]);

        public ObservableCollection<BookCardViewModel> VisibleBooks { get; } = new();

        public ICommand LogOutCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand SelectSectionCommand { get; }
        public ICommand SelectCategoryCommand { get; }
        public ICommand ClearSearchCommand { get; }

        public ReaderDashboardViewModel(
            IBookService bookService,
            ILoanService loanService,
            IWindowNavigationService navigationService)
        {
            _bookService = bookService;
            _loanService = loanService;
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
                        SelectedCategory = AllBooks;
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
            ResultSummary = "Loading catalog...";
        }

        public void SetReader(string readerName, long readerId)
        {
            ReaderName = readerName;
            _readerId = readerId;
        }

        public async Task LoadAsync()
        {
            try
            {
                var catalog = await _bookService.SearchBooksAsync(string.Empty);
                var loans = await _loanService.GetActiveLoansAsync(_readerId);
                var dueByBook = loans
                    .GroupBy(loan => loan.BookId)
                    .ToDictionary(group => group.Key, group => group.Min(loan => loan.DueDate));

                _catalog.Clear();
                _catalog.AddRange(catalog);
                _books.Clear();
                var sortOrder = 0;
                foreach (var book in catalog.OrderBy(book => book.Title))
                {
                    var card = BookCardViewModel.From(book, ++sortOrder,
                        dueByBook.TryGetValue(book.BookId, out var dueDate) ? dueDate : null);
                    card.FavoriteChanged += (_, _) => RefreshBooks();
                    _books.Add(card);
                }

                Categories.Clear();
                Categories.Add(new CategoryOptionViewModel(AllBooks, SelectedCategory == AllBooks));
                foreach (var genre in _books.SelectMany(book => book.Genres).Distinct().Order())
                {
                    Categories.Add(new CategoryOptionViewModel(genre, SelectedCategory == genre));
                }

                _isLoaded = true;
                RefreshBooks();
            }
            catch (Exception)
            {
                ResultSummary = "Could not load the catalog from the database. Try again later.";
                OnPropertyChanged(nameof(ResultSummary));
            }
        }

        private void RefreshBooks()
        {
            // Пошук використовує ту саму логіку, що й BookService (назва, автор, жанр, рік)
            var matchingIds = BookService.ApplySearch(_catalog.AsQueryable(), SearchText)
                .Select(book => book.BookId)
                .ToHashSet();
            IEnumerable<BookCardViewModel> filtered = _books.Where(book => matchingIds.Contains(book.BookId));

            if (SelectedSection == "Favorites")
            {
                filtered = filtered.Where(book => book.IsFavorite);
            }
            else if (SelectedSection == "My Loans")
            {
                filtered = filtered.Where(book => book.IsBorrowed);
            }
            else if (SelectedSection == "Book Search" && SelectedCategory != AllBooks)
            {
                filtered = filtered.Where(book => book.Genres.Contains(SelectedCategory));
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
        // Палітра обкладинок: колір обирається за BookId, тож у книги він завжди однаковий
        private static readonly (string Cover, string Accent)[] CoverPalette =
        [
            ("#153B37", "#D7B978"),
            ("#193B58", "#D4E1E9"),
            ("#765C3D", "#F0D8A5"),
            ("#17636A", "#F1D58B"),
            ("#122A44", "#DFC68B"),
            ("#523A36", "#F0D8CB"),
            ("#C9B89A", "#554536"),
            ("#592E58", "#FFD7A1"),
            ("#743D3A", "#F3D0C2"),
            ("#2B536F", "#E0C89A")
        ];

        private bool _isFavorite;

        public long BookId { get; }
        public int SortOrder { get; }
        public string Title { get; }
        public string Author { get; }
        public IReadOnlyList<string> Genres { get; }
        public string Category => Genres.Count > 0 ? Genres[0] : "Other";
        public string Status { get; }

        // Книга, яку цей читач зараз тримає (розділ «My Loans»)
        public bool IsBorrowed { get; }

        // Сірий бейдж: книгу взяв цей читач або вільних примірників немає
        public bool IsMuted { get; }
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
            long bookId,
            int sortOrder,
            string title,
            string author,
            IReadOnlyList<string> genres,
            string status,
            bool isBorrowed,
            bool isMuted)
        {
            BookId = bookId;
            SortOrder = sortOrder;
            Title = title;
            Author = string.IsNullOrWhiteSpace(author) ? "Unknown author" : author;
            Genres = genres;
            Status = status;
            IsBorrowed = isBorrowed;
            IsMuted = isMuted;
            var (cover, accent) = CoverPalette[(int)(Math.Abs(bookId) % CoverPalette.Length)];
            CoverBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(cover));
            AccentBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accent));
            ToggleFavoriteCommand = new RelayCommand(_ => IsFavorite = !IsFavorite);
        }

        public static BookCardViewModel From(BookCatalogView book, int sortOrder, DateTime? myDueDate)
        {
            var genres = book.Genres
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var (status, isMuted) = ReaderBookStatus.Describe(book.AvailableCopies, myDueDate);
            return new BookCardViewModel(
                book.BookId, sortOrder, book.Title, book.Authors, genres, status, myDueDate is not null, isMuted);
        }
    }

    public static class ReaderBookStatus
    {
        public static (string Text, bool IsMuted) Describe(long availableCopies, DateTime? myDueDate) =>
            myDueDate is not null
                ? ($"Borrowed · due {myDueDate:yyyy-MM-dd}", true)
                : availableCopies > 0
                    ? ($"Available ({availableCopies})", false)
                    : ("Not available", true);
    }
}

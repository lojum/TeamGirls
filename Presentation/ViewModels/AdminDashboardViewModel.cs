using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using DataAccess;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Commands;
using Presentation.Views;

namespace Presentation.ViewModels
{
    public sealed class AdminDashboardViewModel : INotifyPropertyChanged
    {
        private const int PageSize = 10;
        private readonly Window _currentWindow;
        private readonly string _currentAdminName;
        private int _currentPage;
        private string _searchText = string.Empty;
        private string _selectedOverdueFilter = "0";
        private string _activeSection = "Dashboard";

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<DebtorRow> Debtors { get; } = new();
        public ObservableCollection<DebtorRow> DebtorsPage { get; } = new();
        public ObservableCollection<CatalogRow> Catalog { get; } = new();
        public ObservableCollection<ReaderRow> Readers { get; } = new();
        public ICollectionView CatalogView { get; }
        public ICollectionView ReadersView { get; }

        public ICommand ShowSectionCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LogOutCommand { get; }

        public string TotalBooks { get; private set; } = "0";
        public string ActiveReaders { get; private set; } = "0";
        public string OverdueItems { get; private set; } = "0";
        public string CurrentAdminName => _currentAdminName;

        public string ActiveSection
        {
            get => _activeSection;
            private set
            {
                if (_activeSection == value)
                {
                    return;
                }

                _activeSection = value;
                OnPropertyChanged(nameof(ActiveSection));
                OnPropertyChanged(nameof(IsDashboard));
                OnPropertyChanged(nameof(IsDebtorsSection));
                OnPropertyChanged(nameof(IsCatalog));
                OnPropertyChanged(nameof(IsReaders));
                OnPropertyChanged(nameof(IsSettings));
            }
        }

        public bool IsDashboard => ActiveSection == "Dashboard";
        public bool IsDebtorsSection => ActiveSection is "Dashboard" or "Debtors";
        public bool IsCatalog => ActiveSection == "Catalog";
        public bool IsReaders => ActiveSection == "Readers";
        public bool IsSettings => ActiveSection == "Settings";

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value;
                _currentPage = 0;
                CatalogView.Refresh();
                ReadersView.Refresh();
                UpdateDebtorPage();
                OnPropertyChanged(nameof(SearchText));
            }
        }

        public string SelectedOverdueFilter
        {
            get => _selectedOverdueFilter;
            set
            {
                if (_selectedOverdueFilter == value)
                {
                    return;
                }

                _selectedOverdueFilter = value;
                _currentPage = 0;
                UpdateDebtorPage();
                OnPropertyChanged(nameof(SelectedOverdueFilter));
            }
        }

        public bool CanGoToPreviousPage => _currentPage > 0;
        public bool CanGoToNextPage => (_currentPage + 1) * PageSize < FilteredDebtors.Count();

        public string DebtorSummary
        {
            get
            {
                var count = FilteredDebtors.Count();
                var first = count == 0 ? 0 : _currentPage * PageSize + 1;
                var last = Math.Min((_currentPage + 1) * PageSize, count);
                return $"Showing {first} to {last} of {count} debtors";
            }
        }

        private IEnumerable<DebtorRow> FilteredDebtors =>
            Debtors.Where(MatchesSearch)
                .Where(d => !int.TryParse(SelectedOverdueFilter, out var threshold)
                    || d.DaysOverdue >= threshold);

        public AdminDashboardViewModel(Window currentWindow, string currentAdminName)
        {
            _currentWindow = currentWindow;
            _currentAdminName = currentAdminName;
            CatalogView = CollectionViewSource.GetDefaultView(Catalog);
            ReadersView = CollectionViewSource.GetDefaultView(Readers);
            CatalogView.Filter = FilterCatalog;
            ReadersView.Filter = FilterReader;

            ShowSectionCommand = new RelayCommand(parameter =>
            {
                if (parameter is string section && IsSupportedSection(section))
                {
                    ActiveSection = section;
                    SearchText = string.Empty;
                }
            });
            PreviousPageCommand = new RelayCommand(_ => ChangePage(-1), _ => CanGoToPreviousPage);
            NextPageCommand = new RelayCommand(_ => ChangePage(1), _ => CanGoToNextPage);
            LogOutCommand = new RelayCommand(_ =>
            {
                var welcomeWindow = new MainWindow();
                welcomeWindow.Show();
                Application.Current.MainWindow = welcomeWindow;
                _currentWindow.Close();
            });
        }

        public async Task LoadAsync()
        {
            using var scope = App.ServiceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SmartLibDbContext>();
            var today = DateTime.UtcNow.Date;

            var totalBooks = await db.Books.AsNoTracking().CountAsync();
            var activeReaders = await db.Users.AsNoTracking()
                .CountAsync(user => user.Role == UserRole.Reader && user.IsActive);
            var overdueItems = await db.Loans.AsNoTracking()
                .CountAsync(loan => loan.ReturnedAt == null && loan.DueDate < today);

            var overdueLoans = await db.Loans.AsNoTracking()
                .Where(loan => loan.ReturnedAt == null && loan.DueDate < today)
                .Select(loan => new
                {
                    ReaderName = loan.Reader.FullName,
                    BookTitle = loan.Copy.Book.Title,
                    loan.DueDate
                })
                .ToListAsync();

            var catalogBooks = await db.Books.AsNoTracking()
                .Include(book => book.Authors)
                .Include(book => book.Copies)
                .OrderBy(book => book.Title)
                .ToListAsync();

            var readers = await db.Users.AsNoTracking()
                .Where(user => user.Role == UserRole.Reader)
                .OrderBy(user => user.FullName)
                .Select(user => new ReaderRow(
                    user.FullName,
                    user.Email,
                    user.IsActive ? "Active" : "Inactive",
                    user.LoansAsReader.Count(loan => loan.ReturnedAt == null)))
                .ToListAsync();

            TotalBooks = totalBooks.ToString("N0");
            ActiveReaders = activeReaders.ToString("N0");
            OverdueItems = overdueItems.ToString("N0");
            OnPropertyChanged(nameof(TotalBooks));
            OnPropertyChanged(nameof(ActiveReaders));
            OnPropertyChanged(nameof(OverdueItems));

            Debtors.Clear();
            foreach (var loan in overdueLoans)
            {
                Debtors.Add(new DebtorRow(
                    loan.ReaderName,
                    loan.BookTitle,
                    Math.Max(0, (today - loan.DueDate.Date).Days)));
            }

            Catalog.Clear();
            foreach (var book in catalogBooks)
            {
                Catalog.Add(new CatalogRow(
                    book.Title,
                    string.Join(", ", book.Authors.Select(author => author.FullName)),
                    book.PublicationYear,
                    book.Copies.Count));
            }

            Readers.Clear();
            foreach (var reader in readers)
            {
                Readers.Add(reader);
            }

            CatalogView.Refresh();
            ReadersView.Refresh();
            UpdateDebtorPage();
        }

        private bool MatchesSearch(DebtorRow debtor) =>
            string.IsNullOrWhiteSpace(SearchText)
            || debtor.ReaderName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || debtor.BookTitle.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

        private bool FilterCatalog(object item) =>
            item is CatalogRow book
            && (string.IsNullOrWhiteSpace(SearchText)
                || book.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || book.Authors.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        private bool FilterReader(object item) =>
            item is ReaderRow reader
            && (string.IsNullOrWhiteSpace(SearchText)
                || reader.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || reader.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        private void ChangePage(int offset)
        {
            var pageCount = Math.Max(1, (int)Math.Ceiling(FilteredDebtors.Count() / (double)PageSize));
            _currentPage = Math.Clamp(_currentPage + offset, 0, pageCount - 1);
            UpdateDebtorPage();
        }

        private void UpdateDebtorPage()
        {
            var visible = FilteredDebtors
                .Skip(_currentPage * PageSize)
                .Take(PageSize)
                .ToList();

            DebtorsPage.Clear();
            foreach (var debtor in visible)
            {
                DebtorsPage.Add(debtor);
            }

            OnPropertyChanged(nameof(DebtorSummary));
            OnPropertyChanged(nameof(CanGoToPreviousPage));
            OnPropertyChanged(nameof(CanGoToNextPage));
            CommandManager.InvalidateRequerySuggested();
        }

        private static bool IsSupportedSection(string section) =>
            section is "Dashboard" or "Catalog" or "Readers" or "Debtors" or "Settings";

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public sealed record DebtorRow(string ReaderName, string BookTitle, int DaysOverdue);
    public sealed record CatalogRow(string Title, string Authors, short PublicationYear, int CopiesCount);
    public sealed record ReaderRow(string FullName, string Email, string Status, int ActiveLoans);
}

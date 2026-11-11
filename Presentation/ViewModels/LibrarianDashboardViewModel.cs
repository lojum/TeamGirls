using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using BusinessLogic.Services;
using Domain.Enums;
using Presentation.Commands;
using Presentation.Services;

namespace Presentation.ViewModels;

public sealed class LibrarianDashboardViewModel : INotifyPropertyChanged
{
    private const string AllStatuses = "All Statuses";

    private readonly ILibrarianDashboardService _dashboardService;
    private readonly ILoanService _loanService;
    private readonly IWindowNavigationService _navigationService;
    private readonly TimeProvider _timeProvider;
    private readonly List<CopySummary> _allCopies = new();

    private string _librarianName = "Librarian";
    private long _librarianId;
    private string _searchText = string.Empty;
    private string _selectedStatus = AllStatuses;
    private ReaderOption? _selectedReader;
    private LoanDurationOption _selectedDuration;
    private LibrarianBookRow? _selectedBook;
    private string _statusMessage = "Loading data...";
    private bool _isError;
    private bool _isBusy;

    public event PropertyChangedEventHandler? PropertyChanged;

    public LibrarianDashboardViewModel(
        ILibrarianDashboardService dashboardService,
        ILoanService loanService,
        IWindowNavigationService navigationService,
        TimeProvider timeProvider)
    {
        _dashboardService = dashboardService;
        _loanService = loanService;
        _navigationService = navigationService;
        _timeProvider = timeProvider;
        _selectedDuration = LoanDurations.Single(d => d.Days == LoanService.LoanPeriodDays);

        IssueBookCommand = new RelayCommand(async _ => await IssueBookAsync());
        ResetFormCommand = new RelayCommand(_ => ResetForm());
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
        LogOutCommand = new RelayCommand(_ => _navigationService.ShowMain());
    }

    public string LibrarianName
    {
        get => _librarianName;
        private set
        {
            if (SetProperty(ref _librarianName, value))
            {
                OnPropertyChanged(nameof(LoggedInAs));
            }
        }
    }

    public string LoggedInAs => $"Logged in as: Librarian ({LibrarianName})";

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                ApplyFilter();
            }
        }
    }

    public ReaderOption? SelectedReader
    {
        get => _selectedReader;
        set => SetProperty(ref _selectedReader, value);
    }

    public LoanDurationOption SelectedDuration
    {
        get => _selectedDuration;
        set
        {
            if (SetProperty(ref _selectedDuration, value))
            {
                OnPropertyChanged(nameof(CalculatedDueDate));
            }
        }
    }

    public LibrarianBookRow? SelectedBook
    {
        get => _selectedBook;
        set => SetProperty(ref _selectedBook, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsError
    {
        get => _isError;
        private set => SetProperty(ref _isError, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string CalculatedDueDate =>
        _timeProvider.GetLocalNow().Date.AddDays(SelectedDuration.Days).ToString("yyyy-MM-dd");

    public string PageSummary => $"Showing {Books.Count} of {_allCopies.Count} copies";
    public string PageNumber => "1";

    // Усі примірники після фільтра (таблиця) і лише доступні (список для видачі)
    public ObservableCollection<LibrarianBookRow> Books { get; } = new();
    public ObservableCollection<LibrarianBookRow> AvailableBooks { get; } = new();
    public ObservableCollection<ReaderOption> Readers { get; } = new();
    public ObservableCollection<ActivityEntry> RecentActivity { get; } = new();

    public IReadOnlyList<LoanDurationOption> LoanDurations { get; } =
    [
        new(7, "7 Days"),
        new(LoanService.LoanPeriodDays, $"{LoanService.LoanPeriodDays} Days (Standard Term)"),
        new(21, "21 Days"),
        new(30, "30 Days")
    ];

    public IReadOnlyList<string> StatusFilters { get; } =
    [
        AllStatuses,
        LibrarianBookRow.AvailableText,
        LibrarianBookRow.CheckedOutText,
        LibrarianBookRow.ReservedText,
        LibrarianBookRow.WrittenOffText
    ];

    public ICommand IssueBookCommand { get; }
    public ICommand ResetFormCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand LogOutCommand { get; }

    public void SetLibrarian(string librarianName, long librarianId)
    {
        LibrarianName = librarianName;
        _librarianId = librarianId;
    }

    public async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var copies = await _dashboardService.GetCopiesAsync();
            var readers = await _dashboardService.GetActiveReadersAsync();
            var activity = await _dashboardService.GetRecentActivityAsync();

            _allCopies.Clear();
            _allCopies.AddRange(copies);
            Replace(Readers, readers);
            Replace(RecentActivity, activity.Select(ActivityEntry.From));
            ApplyFilter();

            if (SelectedReader is not null)
            {
                SelectedReader = Readers.FirstOrDefault(r => r.UserId == SelectedReader.UserId);
            }

            ShowStatus($"Loaded {_allCopies.Count} copies and {Readers.Count} active readers.", isError: false);
        }
        catch (Exception)
        {
            ShowStatus("Could not load data from the database. Try again later.", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task IssueBookAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (SelectedReader is null || SelectedBook is null)
        {
            ShowStatus("Select a reader and an available copy first.", isError: true);
            return;
        }

        IsBusy = true;
        _loanService.BookLoaned += OnBookLoaned;
        try
        {
            await _loanService.IssueBookAsync(
                SelectedBook.BookId, SelectedReader.Email, _librarianId, SelectedDuration.Days);
        }
        catch (LoanException ex)
        {
            // Зрозумілі користувачеві помилки бізнес-логіки: примірник недоступний, читач неактивний тощо
            ShowStatus(ex.Message, isError: true);
            return;
        }
        catch (Exception)
        {
            ShowStatus("Could not save the loan to the database. Try again later.", isError: true);
            return;
        }
        finally
        {
            _loanService.BookLoaned -= OnBookLoaned;
            IsBusy = false;
        }

        // Після видачі оновлюємо таблицю й історію, але лишаємо повідомлення про успіх
        var successMessage = StatusMessage;
        SelectedBook = null;
        await LoadAsync();
        if (!IsError)
        {
            ShowStatus(successMessage, isError: false);
        }
    }

    private void OnBookLoaned(object sender, LoanEventArgs e)
    {
        ShowStatus($"\"{e.BookTitle}\" issued to {e.ReaderName}. Due date: {e.DueDate:yyyy-MM-dd}.", isError: false);
    }

    private void ResetForm()
    {
        SelectedReader = null;
        SelectedBook = null;
        SelectedDuration = LoanDurations.Single(d => d.Days == LoanService.LoanPeriodDays);
        ShowStatus("Form cleared.", isError: false);
    }

    private void ApplyFilter()
    {
        CopyStatus? status = SelectedStatus == AllStatuses ? null : LibrarianBookRow.ParseStatus(SelectedStatus);
        var filtered = LibrarianDashboardService.Filter(_allCopies, SearchText, status)
            .Select(LibrarianBookRow.From)
            .ToList();

        Replace(Books, filtered);
        Replace(AvailableBooks, _allCopies.Where(c => c.Status == CopyStatus.Available).Select(LibrarianBookRow.From));
        OnPropertyChanged(nameof(PageSummary));
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
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

public sealed record LoanDurationOption(int Days, string Label);

// BookId у таблиці — інвентарний номер примірника: саме його приймає LoanService
public sealed record LibrarianBookRow(
    string BookId,
    string Title,
    string Author,
    string Isbn,
    string Category,
    string Status,
    string DueDate)
{
    public const string AvailableText = "Available";
    public const string CheckedOutText = "Checked Out";
    public const string ReservedText = "Reserved";
    public const string WrittenOffText = "Written Off";

    public static LibrarianBookRow From(CopySummary copy) => new(
        copy.InventoryNumber,
        copy.Title,
        copy.Authors,
        copy.Isbn ?? "--",
        string.IsNullOrEmpty(copy.Genres) ? "--" : copy.Genres,
        StatusText(copy.Status),
        copy.DueDate?.ToString("yyyy-MM-dd") ?? "--");

    public static string StatusText(CopyStatus status) => status switch
    {
        CopyStatus.Available => AvailableText,
        CopyStatus.OnLoan => CheckedOutText,
        CopyStatus.Reserved => ReservedText,
        _ => WrittenOffText
    };

    public static CopyStatus? ParseStatus(string text) => text switch
    {
        AvailableText => CopyStatus.Available,
        CheckedOutText => CopyStatus.OnLoan,
        ReservedText => CopyStatus.Reserved,
        WrittenOffText => CopyStatus.WrittenOff,
        _ => null
    };
}

public sealed record ActivityEntry(string Time, string Reader, string BookTitle, string Action)
{
    public static ActivityEntry From(LoanActivity activity) => new(
        activity.Time.ToLocalTime().ToString("dd.MM HH:mm"),
        activity.ReaderName,
        $"{activity.BookTitle} ({activity.InventoryNumber})",
        activity.IsReturned ? "RETURNED" : "ISSUED");
}

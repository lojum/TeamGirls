using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Presentation.ViewModels;

public sealed class LibrarianDashboardViewModel : INotifyPropertyChanged
{
    private string _librarianName = "Librarian";
    private string _searchText = string.Empty;
    private string _selectedStatus = "All Statuses";
    private string _selectedReader = "ID: PR-8821 | David Brent (Staff Member)";
    private string _selectedDuration = "14 Days (Standard Term)";
    private LibrarianBookRow? _selectedBook;

    public event PropertyChangedEventHandler? PropertyChanged;

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
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }
    public string SelectedStatus { get => _selectedStatus; set => SetProperty(ref _selectedStatus, value); }
    public string SelectedReader { get => _selectedReader; set => SetProperty(ref _selectedReader, value); }
    public string SelectedDuration { get => _selectedDuration; set => SetProperty(ref _selectedDuration, value); }

    public LibrarianBookRow? SelectedBook
    {
        get => _selectedBook;
        set
        {
            if (SetProperty(ref _selectedBook, value))
            {
                OnPropertyChanged(nameof(CalculatedDueDate));
            }
        }
    }

    public string CalculatedDueDate => DateTime.Today.AddDays(14).ToString("yyyy-MM-dd");
    public string PageSummary => $"Showing {Books.Count} sample books (Page 1 of 1)";
    public string PageNumber => "1";
    public string StatusMessage => "Sample data only — actions are not connected.";

    public ObservableCollection<LibrarianBookRow> Books { get; } = new(
    [
        new("BK-2041", "C# 10.0 in a Nutshell", "Joseph Albahari", "9781098121952", "Technology", "Available", "--"),
        new("BK-1984", "Nineteen Eighty-Four", "George Orwell", "9780451524935", "Fiction", "Checked Out", "2026-03-12"),
        new("BK-1102", "The Great Gatsby", "F. Scott Fitzgerald", "9780743273565", "Classics", "Available", "--"),
        new("BK-1150", "Dune (Deluxe Edition)", "Frank Herbert", "9780441172719", "Sci-Fi", "Reserved", "2026-02-28"),
        new("BK-0921", "CLR via C# (4th Edition)", "Jeffrey Richter", "9780735667457", "Technology", "Available", "--"),
        new("BK-1201", "Educated: A Memoir", "Tara Westover", "9780399590504", "Biography", "Checked Out", "2026-03-15"),
        new("BK-1240", "The Hobbit", "J.R.R. Tolkien", "9780547928227", "Fantasy", "Available", "--"),
        new("BK-1256", "Atomic Habits", "James Clear", "9780735211292", "Self-Help", "Available", "--"),
        new("BK-1302", "Thinking, Fast and Slow", "Daniel Kahneman", "9780374533557", "Psychology", "Reserved", "2026-03-01"),
        new("BK-1344", "Silent Spring", "Rachel Carson", "9780618249060", "Science", "Available", "--"),
        new("BK-1380", "Deep Work", "Cal Newport", "9781455586691", "Business", "Checked Out", "2026-03-18"),
        new("BK-1411", "A Brief History of Time", "Stephen Hawking", "9780553380163", "Science", "Available", "--"),
        new("BK-1450", "The Alchemist", "Paulo Coelho", "9780062315007", "Fiction", "Available", "--"),
        new("BK-1492", "Pride and Prejudice", "Jane Austen", "9780141439518", "Classics", "Checked Out", "2026-03-20"),
        new("BK-1510", "Clean Code", "Robert C. Martin", "9780132350884", "Technology", "Available", "--"),
        new("BK-1553", "The Name of the Wind", "Patrick Rothfuss", "9780756404741", "Fantasy", "Available", "--"),
        new("BK-1598", "Sapiens", "Yuval Noah Harari", "9780062316097", "History", "Reserved", "2026-03-08"),
        new("BK-1632", "The Design of Everyday Things", "Don Norman", "9780465050659", "Art", "Available", "--"),
        new("BK-1670", "The Martian", "Andy Weir", "9780553418026", "Sci-Fi", "Available", "--"),
        new("BK-1704", "Becoming", "Michelle Obama", "9781524763138", "Biography", "Checked Out", "2026-03-25")
    ]);

    public ObservableCollection<string> Readers { get; } = new(
    [
        "ID: PR-8821 | David Brent (Staff Member)",
        "ID: PR-1024 | Emma Wilson",
        "ID: PR-1568 | James Park",
        "ID: PR-2091 | Sarah Chen",
        "ID: PR-3042 | Michael Torres"
    ]);

    public ObservableCollection<string> LoanDurations { get; } = new(
    [
        "7 Days",
        "14 Days (Standard Term)",
        "21 Days",
        "30 Days"
    ]);

    public ObservableCollection<string> StatusFilters { get; } = new(
    [
        "All Statuses",
        "Available",
        "Checked Out",
        "Reserved"
    ]);

    public ObservableCollection<ActivityEntry> RecentActivity { get; } = new(
    [
        new("10:14 AM", "Emma Wilson", "CLR via C# (BK-0921)", "RETURNED"),
        new("09:55 AM", "James Park", "Dune (BK-1150)", "RESERVED"),
        new("09:41 AM", "Sarah Chen", "Clean Code (BK-1510)", "ISSUED"),
        new("08:30 AM", "Michael Torres", "Atomic Habits (BK-1256)", "ISSUED")
    ]);

    public void SetLibrarianName(string librarianName)
    {
        LibrarianName = librarianName;
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

public sealed record LibrarianBookRow(
    string BookId,
    string Title,
    string Author,
    string Isbn,
    string Category,
    string Status,
    string DueDate);

public sealed record ActivityEntry(string Time, string Reader, string BookTitle, string Action);

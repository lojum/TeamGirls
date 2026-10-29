namespace Domain.Entities
{
    public class BookCatalogView
    {
        public long BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? ISBN { get; set; } // У базі може бути NULL
        public short PublicationYear { get; set; }
        public string? Publisher { get; set; } // У базі може бути NULL
        public string Authors { get; set; } = string.Empty; // Вже зібрані через кому
        public string Genres { get; set; } = string.Empty;  // Вже зібрані через кому
        public long TotalCopies { get; set; }
        public long AvailableCopies { get; set; }
    }
}
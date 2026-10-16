using System.Collections.Generic;

namespace Domain.Entities
{
    public class Author
    {
        public long AuthorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public short? BirthYear { get; set; } // Може бути null у базі Софії
        public string? Country { get; set; }

        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
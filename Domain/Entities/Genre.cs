using System.Collections.Generic;

namespace Domain.Entities
{
    public class Genre
    {
        public int GenreId { get; set; }
        public string Name { get; set; } = string.Empty;

        public virtual ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
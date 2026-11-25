using System.Collections.Generic;

namespace Domain.Entities
{
    // Місце зберігання примірників: зал і стелаж (таблиця locations)
    public class Location
    {
        public int LocationId { get; set; }
        public string Room { get; set; } = string.Empty;
        public string Shelf { get; set; } = string.Empty;

        public virtual ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();
    }
}

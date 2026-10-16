using System;
using System.Collections.Generic;

namespace Domain.Entities
{
    public class Book
    {
        public long BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Isbn { get; set; }
        public short PublicationYear { get; set; }
        public string? Publisher { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Author> Authors { get; set; } = new List<Author>();
        public virtual ICollection<Genre> Genres { get; set; } = new List<Genre>();
        public virtual ICollection<BookCopy> Copies { get; set; } = new List<BookCopy>();
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
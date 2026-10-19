using System;

namespace Domain.Entities
{
    public class Loan
    {
        public long LoanId { get; set; }
        public long CopyId { get; set; }
        public long ReaderId { get; set; }
        public long? LibrarianId { get; set; } // Може бути null
        public DateTime LoanedAt { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public DateTime? ReturnedAt { get; set; }

        public virtual BookCopy Copy { get; set; } = null!;
        public virtual User Reader { get; set; } = null!;
        public virtual User? Librarian { get; set; }
    }
}
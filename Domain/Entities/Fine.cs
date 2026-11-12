using System;

namespace Domain.Entities
{
    // Штраф за прострочену видачу (таблиця fines)
    public class Fine
    {
        public long FineId { get; set; }
        public long LoanId { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        public virtual Loan Loan { get; set; } = null!;
    }
}

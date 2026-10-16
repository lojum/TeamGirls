using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities
{
    public class BookCopy
    {
        [Key]
        public long CopyId { get; set; }
        public long BookId { get; set; }
        public string InventoryNumber { get; set; } = string.Empty;
        public CopyStatus Status { get; set; } = CopyStatus.Available;
        public DateTime AcquiredAt { get; set; }
        public DateTime? WrittenOffAt { get; set; }
        public string? WriteOffReason { get; set; }

        public virtual Book Book { get; set; } = null!;
        public virtual ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }
}
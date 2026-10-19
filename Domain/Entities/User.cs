using System;
using System.Collections.Generic;
using Domain.Enums;

namespace Domain.Entities
{
    public class User
    {
        public long UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Reader;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Loan> LoansAsReader { get; set; } = new List<Loan>();
        public virtual ICollection<Loan> LoansAsLibrarian { get; set; } = new List<Loan>();
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
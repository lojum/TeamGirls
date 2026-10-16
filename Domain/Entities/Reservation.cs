using System;
using Domain.Enums;

namespace Domain.Entities
{
    public class Reservation
    {
        public long ReservationId { get; set; }
        public long BookId { get; set; }
        public long ReaderId { get; set; }
        public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Active;

        public virtual Book Book { get; set; } = null!;
        public virtual User Reader { get; set; } = null!;
    }
}
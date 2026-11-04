namespace Domain.Entities
{
    public class Fine
    {
        public long FineId { get; set; }
        public long LoanId { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
    }
}
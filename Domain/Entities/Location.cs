namespace Domain.Entities
{
    public class Location
    {
        public int LocationId { get; set; }
        public string Section { get; set; } = string.Empty;
        public string Shelf { get; set; } = string.Empty;
    }
}
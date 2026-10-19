using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Enums;

namespace DataAccess
{
    public class SmartLibDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<BookCopy> BookCopies { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        public SmartLibDbContext(DbContextOptions<SmartLibDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresEnum<UserRole>("user_role");
            modelBuilder.HasPostgresEnum<CopyStatus>("copy_status");
            modelBuilder.HasPostgresEnum<ReservationStatus>("reservation_status");

            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Author>().ToTable("authors");
            modelBuilder.Entity<Genre>().ToTable("genres");
            modelBuilder.Entity<Book>().ToTable("books");
            modelBuilder.Entity<BookCopy>().ToTable("book_copies");
            modelBuilder.Entity<Loan>().ToTable("loans");
            modelBuilder.Entity<Reservation>().ToTable("reservations");

            modelBuilder.Entity<Book>()
                .HasMany(b => b.Authors)
                .WithMany(a => a.Books)
                .UsingEntity(j => j.ToTable("book_authors")); 

            modelBuilder.Entity<Book>()
                .HasMany(b => b.Genres)
                .WithMany(g => g.Books)
                .UsingEntity(j => j.ToTable("book_genres"));

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Reader)
                .WithMany(u => u.LoansAsReader)
                .HasForeignKey(l => l.ReaderId);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Librarian)
                .WithMany(u => u.LoansAsLibrarian)
                .HasForeignKey(l => l.LibrarianId);
        }
    }
}
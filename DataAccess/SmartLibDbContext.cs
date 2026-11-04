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
        public virtual DbSet<BookCatalogView> BookCatalogViews { get; set; }

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

            // Явно задаємо колонки таблиць зв'язку: інакше EF шукає
            // authors_author_id / books_book_id, яких у schema.sql немає
            modelBuilder.Entity<Book>()
                .HasMany(b => b.Authors)
                .WithMany(a => a.Books)
                .UsingEntity<Dictionary<string, object>>(
                    "book_authors",
                    right => right.HasOne<Author>().WithMany().HasForeignKey("author_id"),
                    left => left.HasOne<Book>().WithMany().HasForeignKey("book_id"),
                    join => join.ToTable("book_authors"));

            modelBuilder.Entity<Book>()
                .HasMany(b => b.Genres)
                .WithMany(g => g.Books)
                .UsingEntity<Dictionary<string, object>>(
                    "book_genres",
                    right => right.HasOne<Genre>().WithMany().HasForeignKey("genre_id"),
                    left => left.HasOne<Book>().WithMany().HasForeignKey("book_id"),
                    join => join.ToTable("book_genres"));

            // У базі ці колонки мають тип DATE, а не timestamptz
            modelBuilder.Entity<Loan>().Property(l => l.DueDate).HasColumnType("date");
            modelBuilder.Entity<BookCopy>().Property(c => c.AcquiredAt).HasColumnType("date");
            modelBuilder.Entity<BookCopy>().Property(c => c.WrittenOffAt).HasColumnType("date");


            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Reader)
                .WithMany(u => u.LoansAsReader)
                .HasForeignKey(l => l.ReaderId);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Librarian)
                .WithMany(u => u.LoansAsLibrarian)
                .HasForeignKey(l => l.LibrarianId);

            modelBuilder.Entity<BookCatalogView>()
                .HasNoKey()
                .ToView("v_book_catalog");
        }
    }
}
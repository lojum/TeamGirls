using BusinessLogic.Services;
using Npgsql;
using SmartLib.IntegrationTests.Infrastructure;
using SmartLib.IntegrationTests.TestData;
using static SmartLib.IntegrationTests.TestData.SeedData;

namespace SmartLib.IntegrationTests.Tests
{
    // Тригер trg_loans_sync_copy та індекс «одна активна видача на примірник»
    [Trait("Category", "Integration")]
    public class LoanTriggerTests : IntegrationTestBase
    {
        private const string CopyStatusSql =
            "SELECT status::text FROM book_copies WHERE inventory_number = @inv";

        public LoanTriggerTests(PostgresServer server) : base(server)
        {
        }

        [Fact]
        public async Task IssueBook_ViaLoanService_TriggerMarksCopyOnLoan()
        {
            // Arrange
            await using var context = Db.CreateContext();
            var service = new LoanService(context, TimeProvider.System);
            var librarianId = await UserIdAsync(LibrarianEmail);

            // Act
            await service.IssueBookAsync(AvailableInventory, ReaderEmail, librarianId);

            // Assert: читаємо результат окремим запитом, а не через EF
            Assert.Equal("on_loan", await Db.ScalarAsync<string>(CopyStatusSql, ("inv", AvailableInventory)));
            var dueDate = await Db.ScalarAsync<DateTime>(
                "SELECT l.due_date FROM loans l JOIN book_copies c USING (copy_id) " +
                "WHERE c.inventory_number = @inv AND l.returned_at IS NULL", ("inv", AvailableInventory));
            Assert.Equal(DateTime.UtcNow.Date.AddDays(LoanService.LoanPeriodDays), dueDate);
        }

        [Fact]
        public async Task ReturnBook_TriggerMarksCopyAvailable()
        {
            // Act
            await Db.ExecuteAsync(
                "UPDATE loans SET returned_at = now() WHERE returned_at IS NULL AND copy_id = " +
                "(SELECT copy_id FROM book_copies WHERE inventory_number = @inv)", ("inv", LoanedInventory));

            // Assert
            Assert.Equal("available", await Db.ScalarAsync<string>(CopyStatusSql, ("inv", LoanedInventory)));
        }

        [Fact]
        public async Task LoanOfWrittenOffCopy_IsRejectedByTrigger()
        {
            // Act: вставляємо видачу напряму, в обхід перевірок LoanService
            var ex = await Assert.ThrowsAsync<PostgresException>(() => Db.ExecuteAsync(
                "INSERT INTO loans (copy_id, reader_id, due_date) " +
                "SELECT c.copy_id, u.user_id, CURRENT_DATE + 14 FROM book_copies c, users u " +
                "WHERE c.inventory_number = @inv AND u.email = @email",
                ("inv", WrittenOffInventory), ("email", ReaderEmail)));

            // Assert
            Assert.Equal(PostgresErrorCodes.RaiseException, ex.SqlState);
            Assert.Contains("списано", ex.MessageText);
        }

        [Fact]
        public async Task SecondActiveLoanOnSameCopy_IsRejectedByUniqueIndex()
        {
            // Arrange: примірник уже виданий; статус вручну «доступний», щоб тригер не зупинив вставку
            await Db.ExecuteAsync(
                "UPDATE book_copies SET status = 'available' WHERE inventory_number = @inv", ("inv", LoanedInventory));

            // Act
            var ex = await Assert.ThrowsAsync<PostgresException>(() => Db.ExecuteAsync(
                "INSERT INTO loans (copy_id, reader_id, due_date) " +
                "SELECT c.copy_id, u.user_id, CURRENT_DATE + 14 FROM book_copies c, users u " +
                "WHERE c.inventory_number = @inv AND u.email = @email",
                ("inv", LoanedInventory), ("email", ReaderEmail)));

            // Assert
            Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
            Assert.Equal("ux_loans_one_active_per_copy", ex.ConstraintName);
        }

        [Fact]
        public async Task IssueBook_CopyAlreadyOnLoan_LoanServiceRejectsWithoutNewLoan()
        {
            // Arrange
            await using var context = Db.CreateContext();
            var service = new LoanService(context, TimeProvider.System);
            var librarianId = await UserIdAsync(LibrarianEmail);
            var loansBefore = await Db.ScalarAsync<long>("SELECT count(*) FROM loans");

            // Act
            var ex = await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(LoanedInventory, ReaderEmail, librarianId));

            // Assert
            Assert.Contains("not available", ex.Message);
            Assert.Equal(loansBefore, await Db.ScalarAsync<long>("SELECT count(*) FROM loans"));
        }

        // Два бібліотекарі видають той самий примірник одночасно: друга видача відхиляється базою,
        // а користувач отримує зрозуміле повідомлення замість технічної помилки PostgreSQL
        [Fact]
        public async Task IssueBook_CopyIssuedConcurrently_SecondLoanRejectedWithFriendlyMessage()
        {
            // Arrange: перед збереженням нашої видачі «інший бібліотекар» встигає видати той самий примірник
            var competingLoan = new BeforeSaveInterceptor(() => Db.ExecuteAsync(
                "INSERT INTO loans (copy_id, reader_id, due_date) " +
                "SELECT c.copy_id, u.user_id, CURRENT_DATE + 14 FROM book_copies c, users u " +
                "WHERE c.inventory_number = @inv AND u.email = @email",
                ("inv", AvailableInventory), ("email", OtherReaderEmail)));
            await using var context = Db.CreateContext(competingLoan);
            var service = new LoanService(context, TimeProvider.System);
            var librarianId = await UserIdAsync(LibrarianEmail);

            // Act
            var ex = await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(AvailableInventory, ReaderEmail, librarianId));

            // Assert: лишилася лише видача «іншого бібліотекаря»
            Assert.Contains("has just been issued", ex.Message);
            Assert.Equal(1L, await Db.ScalarAsync<long>(
                "SELECT count(*) FROM loans l JOIN book_copies c USING (copy_id) " +
                "WHERE c.inventory_number = @inv AND l.returned_at IS NULL", ("inv", AvailableInventory)));
        }

        [Fact]
        public async Task IssueBook_ByAdminInsteadOfLibrarian_IsRejected()
        {
            // Arrange
            await using var context = Db.CreateContext();
            var service = new LoanService(context, TimeProvider.System);
            var adminId = await UserIdAsync(AdminEmail);

            // Act
            var ex = await Assert.ThrowsAsync<LoanException>(
                () => service.IssueBookAsync(AvailableInventory, ReaderEmail, adminId));

            // Assert
            Assert.Contains("Only an active librarian", ex.Message);
            Assert.Equal("available", await Db.ScalarAsync<string>(CopyStatusSql, ("inv", AvailableInventory)));
        }

        private Task<long> UserIdAsync(string email) =>
            Db.ScalarAsync<long>("SELECT user_id FROM users WHERE email = @email", ("email", email));
    }
}

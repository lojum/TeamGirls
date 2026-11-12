using Microsoft.EntityFrameworkCore;
using SmartLib.IntegrationTests.Infrastructure;
using static SmartLib.IntegrationTests.TestData.SeedData;

namespace SmartLib.IntegrationTests.Tests
{
    // Сутності Fine і Location читаються зі справжніх таблиць fines і locations
    [Trait("Category", "Integration")]
    public class DomainMappingIntegrationTests : IntegrationTestBase
    {
        public DomainMappingIntegrationTests(PostgresServer server) : base(server)
        {
        }

        [Fact]
        public async Task Fines_SeedFineBelongsToOverdueLoan()
        {
            // Arrange
            await using var context = Db.CreateContext();

            // Act
            var fine = await context.Fines.Include(f => f.Loan).ThenInclude(l => l.Copy).SingleAsync();

            // Assert
            Assert.Equal(OverdueFineAmount, fine.Amount);
            Assert.False(fine.IsPaid);
            Assert.Null(fine.PaidAt);
            Assert.Equal(OverdueInventory, fine.Loan.Copy.InventoryNumber);
        }

        [Fact]
        public async Task BookCopy_HasLocationFromSeed()
        {
            // Arrange
            await using var context = Db.CreateContext();

            // Act
            var copy = await context.BookCopies.Include(c => c.Location)
                .SingleAsync(c => c.InventoryNumber == AvailableInventory);

            // Assert
            Assert.Equal(LocationsCount, await context.Locations.CountAsync());
            Assert.NotNull(copy.Location);
            Assert.Equal(AvailableCopyRoom, copy.Location.Room);
            Assert.Equal(AvailableCopyShelf, copy.Location.Shelf);
        }
    }
}

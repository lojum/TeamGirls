using BusinessLogic;
using BusinessLogic.Services;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartLib.Tests.TestData;
using static SmartLib.Tests.TestData.ServiceTestData;

namespace SmartLib.Tests.Services
{
    // Перевіряє конфігурацію: усі сервіси зареєстровані й створюються без підключення до бази
    [Trait("Category", "Unit")]
    public class DependencyInjectionTests
    {
        private static ServiceProvider BuildProvider(string? connectionString)
        {
            var settings = new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connectionString };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            return new ServiceCollection().AddBusinessLogic(configuration).BuildServiceProvider();
        }

        [Theory]
        [MemberData(nameof(ServiceTestData.RegisteredServices), MemberType = typeof(ServiceTestData))]
        public void AddBusinessLogic_ResolvesService(Type contract, Type implementation)
        {
            // Arrange
            using var provider = BuildProvider(FakeConnectionString);
            using var scope = provider.CreateScope();

            // Act
            var service = scope.ServiceProvider.GetRequiredService(contract);

            // Assert
            Assert.IsType(implementation, service);
        }

        [Fact]
        public void AddBusinessLogic_DbContextUsesPostgreSql()
        {
            // Arrange
            using var provider = BuildProvider(FakeConnectionString);
            using var scope = provider.CreateScope();

            // Act
            var context = scope.ServiceProvider.GetRequiredService<SmartLibDbContext>();

            // Assert
            Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        }

        [Fact]
        public void AddBusinessLogic_RegistersSystemClock()
        {
            using var provider = BuildProvider(FakeConnectionString);

            Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        }

        [Fact]
        public void AddBusinessLogic_MissingConnectionString_Throws()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => BuildProvider(connectionString: null));

            Assert.Contains("DefaultConnection", ex.Message);
        }
    }
}

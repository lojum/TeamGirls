using Testcontainers.PostgreSql;

namespace SmartLib.IntegrationTests.Infrastructure
{
    public sealed class PostgresServer : IAsyncLifetime
    {
        public const string ConnectionStringVariable = "SMARTLIB_TEST_DB";

        private PostgreSqlContainer? _container;

        public string AdminConnectionString { get; private set; } = string.Empty;

        public async Task InitializeAsync()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable(ConnectionStringVariable);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                AdminConnectionString = fromEnvironment;
                return;
            }

            try
            {
                _container = new PostgreSqlBuilder()
                    .WithImage("postgres:16-alpine")
                    .Build();
                await _container.StartAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"PostgreSQL for integration tests is not available. Start Docker Desktop " +
                    $"or set the {ConnectionStringVariable} environment variable to a test PostgreSQL server " +
                    "(never the production database).", ex);
            }

            AdminConnectionString = _container.GetConnectionString();
        }

        public async Task DisposeAsync()
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
            }
        }
    }

    [CollectionDefinition(Name)]
    public sealed class PostgresCollection : ICollectionFixture<PostgresServer>
    {
        public const string Name = "PostgreSQL";
    }
}

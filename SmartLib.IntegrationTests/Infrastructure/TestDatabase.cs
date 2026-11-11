using BusinessLogic;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SmartLib.IntegrationTests.Infrastructure
{
    public sealed class TestDatabase
    {
        private readonly string _adminConnectionString;
        private NpgsqlDataSource? _dataSource;

        public TestDatabase(string adminConnectionString)
        {
            _adminConnectionString = adminConnectionString;
            ConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = Name }.ConnectionString;
        }

        public string Name { get; } = $"smartlib_tests_{Guid.NewGuid():N}";
        public string ConnectionString { get; }

        public async Task CreateAsync()
        {
            await ExecuteAdminAsync($"CREATE DATABASE \"{Name}\"");
            try
            {
                await RunSchemaAsync();
                _dataSource = DependencyInjection.CreateDataSource(ConnectionString);
            }
            catch
            { 
                await DropAsync();
                throw;
            }
        }

        public async Task DropAsync()
        {
            if (_dataSource is not null)
            {
                await _dataSource.DisposeAsync();
            }

            NpgsqlConnection.ClearAllPools();
            await ExecuteAdminAsync($"DROP DATABASE IF EXISTS \"{Name}\" WITH (FORCE)");
        }

        public async Task RunSchemaAsync()
        {
            var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "schema.sql"));
            await ExecuteAsync(script);
        }

        public SmartLibDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<SmartLibDbContext>()
                .UseSmartLibDatabase(_dataSource ?? throw new InvalidOperationException("Database is not created."))
                .Options);

        public async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = CreateCommand(connection, sql, parameters);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<T> ScalarAsync<T>(string sql, params (string Name, object Value)[] parameters)
        {
            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();
            await using var command = CreateCommand(connection, sql, parameters);
            return (T)(await command.ExecuteScalarAsync())!;
        }

        private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, string sql, (string Name, object Value)[] parameters)
        {
            var command = new NpgsqlCommand(sql, connection);
            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value);
            }

            return command;
        }

        private async Task ExecuteAdminAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}

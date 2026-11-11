namespace SmartLib.IntegrationTests.Infrastructure
{
    [Collection(PostgresCollection.Name)]
    public abstract class IntegrationTestBase : IAsyncLifetime
    {
        protected IntegrationTestBase(PostgresServer server)
        {
            Db = new TestDatabase(server.AdminConnectionString);
        }

        protected TestDatabase Db { get; }

        public Task InitializeAsync() => Db.CreateAsync();

        public Task DisposeAsync() => Db.DropAsync();
    }
}

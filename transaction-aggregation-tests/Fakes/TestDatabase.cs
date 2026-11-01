using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using transaction_aggregation_infra.DataAccess;

namespace transaction_aggregation_tests.Fakes
{
    public sealed class TestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public TestDatabase()
        {
            _connection.Open();

            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        public AppDbContext CreateContext()
        {
            return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}

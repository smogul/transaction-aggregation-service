using Microsoft.EntityFrameworkCore;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_infra.SeedData;

namespace transaction_aggregation_infra.DataAccess
{
    // Our SQLite database. Each DbSet is one table.
    // Example: _dbContext.Accounts is the Accounts table, and each Account object is one row in it.
    //
    // Where the database lives comes from ConnectionStrings:Database in appsettings.
    // Today it is in memory, so no .db file is made. Change the connection string to use a file or another server.
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<Statement> Statements => Set<Statement>();
        public DbSet<DownloadLink> DownloadLinks => Set<DownloadLink>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // An index is like the index at the back of a book: it makes finding a link by its token fast.
            modelBuilder.Entity<DownloadLink>().HasIndex(l => l.TokenHash).IsUnique();

            // Seed data: rows that are put into the tables when the database is created.
            modelBuilder.Entity<Account>().HasData(DatabaseSeedData.Accounts);
            modelBuilder.Entity<Transaction>().HasData(DatabaseSeedData.Transactions);
        }
    }
}

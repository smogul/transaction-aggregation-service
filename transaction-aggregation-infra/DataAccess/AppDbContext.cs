using Microsoft.EntityFrameworkCore;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_infra.SeedData;

namespace transaction_aggregation_infra.DataAccess
{
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
            modelBuilder.Entity<Statement>().Property(s => s.CreatedAt).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
            modelBuilder.Entity<DownloadLink>().Property(l => l.ExpiresAt).HasConversion(value => value, value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

            modelBuilder.Entity<Account>().HasData(DatabaseSeedData.Accounts);
            modelBuilder.Entity<Transaction>().HasData(DatabaseSeedData.Transactions);
        }
    }
}

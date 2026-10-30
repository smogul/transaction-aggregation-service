using Microsoft.EntityFrameworkCore;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;
using transaction_aggregation_infra.SeedData;

namespace transaction_aggregation_infra.DataAccess
{
    // Data source 4 of 5: a database table.
    // The Transactions table in our database holds 25 transactions from July 2026.
    // They are put there when the app starts (see SeedData/DatabaseSeedData.cs).
    public class DatabaseTransactionSource : ITransactionSource
    {
        private readonly AppDbContext _dbContext;

        public DatabaseTransactionSource(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public string SourceSystem => DatabaseSeedData.SourceSystem;

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(string accountId)
        {
            // AsNoTracking means "just read, I am not going to change these rows".
            return await _dbContext.Transactions
                .AsNoTracking()
                .Where(t => t.AccountId == accountId)
                .ToListAsync();
        }
    }
}

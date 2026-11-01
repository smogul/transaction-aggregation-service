using Microsoft.EntityFrameworkCore;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;
using transaction_aggregation_infra.SeedData;

namespace transaction_aggregation_infra.DataAccess
{
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
            return await _dbContext.Transactions
                .AsNoTracking()
                .Where(t => t.AccountId == accountId)
                .ToListAsync();
        }
    }
}

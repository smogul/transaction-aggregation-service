using Microsoft.EntityFrameworkCore;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_infra.DataAccess
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _dbContext;

        public AccountRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Account?> GetAccountAsync(string accountId)
        {
            return await _dbContext.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId);
        }
    }
}

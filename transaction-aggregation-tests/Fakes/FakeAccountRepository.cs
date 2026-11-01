using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_tests.Fakes
{
    public class FakeAccountRepository : IAccountRepository
    {
        public List<Account> Accounts { get; } = new()
        {
            new Account { Id = "1000000001", CustomerId = "CUST-001", AccountName = "Everyday", OpeningBalance = 1000m },
            new Account { Id = "1000000002", CustomerId = "CUST-001", AccountName = "Savings", OpeningBalance = 500m }
        };

        public Task<Account?> GetAccountAsync(string accountId)
        {
            return Task.FromResult(Accounts.FirstOrDefault(a => a.Id == accountId));
        }
    }
}

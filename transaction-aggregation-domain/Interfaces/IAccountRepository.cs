using transaction_aggregation_domain.Entities;

namespace transaction_aggregation_domain.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account?> GetAccountAsync(string accountId);
    }
}

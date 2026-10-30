using transaction_aggregation_domain.Entities;

namespace transaction_aggregation_domain.Interfaces
{
    // Finds accounts in the database.
    public interface IAccountRepository
    {
        // Example: GetAccountAsync("1000000001") returns the Everyday Account, or null if there is no such account.
        Task<Account?> GetAccountAsync(string accountId);
    }
}

using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;
using transaction_aggregation_infra.SeedData;

namespace transaction_aggregation_infra.DataAccess
{
    // Data source 1 of 5: a dummy data class, pretending to be Absa.
    // The rows themselves live in SeedData/AbsaSeedData.cs. This class only hands out the ones for one account.
    // Example: asking for "1000000002" returns just the 2 savings account rows.
    public class MockAbsaStatementSource : ITransactionSource
    {
        public string SourceSystem => AbsaSeedData.SourceSystem;

        public Task<IEnumerable<Transaction>> GetTransactionsAsync(string accountId)
        {
            var transactions = AbsaSeedData.CreateTransactions().Where(t => t.AccountId == accountId).ToList();

            return Task.FromResult<IEnumerable<Transaction>>(transactions);
        }
    }
}

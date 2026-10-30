using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;
using transaction_aggregation_infra.SeedData;

namespace transaction_aggregation_infra.DataAccess
{
    // Data source 2 of 5: a dummy data class, pretending to be GoTyme.
    // The rows themselves live in SeedData/GoTymeSeedData.cs. This class only hands out the ones for one account.
    // Example: asking for "1000000002" returns just the 2 savings account rows.
    public class MockGoTymeStatementSource : ITransactionSource
    {
        public string SourceSystem => GoTymeSeedData.SourceSystem;

        public Task<IEnumerable<Transaction>> GetTransactionsAsync(string accountId)
        {
            var transactions = GoTymeSeedData.CreateTransactions().Where(t => t.AccountId == accountId).ToList();

            return Task.FromResult<IEnumerable<Transaction>>(transactions);
        }
    }
}

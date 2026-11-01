using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;
using transaction_aggregation_infra.SeedData;

namespace transaction_aggregation_infra.DataAccess
{
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

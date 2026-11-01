using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;
using transaction_aggregation_infra.SeedData;

namespace transaction_aggregation_infra.DataAccess
{
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

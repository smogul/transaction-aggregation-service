using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_tests.Fakes
{
    // A pretend data source that returns whatever transactions the test gives it.
    public class FakeTransactionSource : ITransactionSource
    {
        private readonly List<Transaction> _transactions;

        public FakeTransactionSource(string sourceSystem, params Transaction[] transactions)
        {
            SourceSystem = sourceSystem;
            _transactions = transactions.ToList();
        }

        public string SourceSystem { get; }

        public Task<IEnumerable<Transaction>> GetTransactionsAsync(string accountId)
        {
            return Task.FromResult<IEnumerable<Transaction>>(_transactions.Where(t => t.AccountId == accountId).ToList());
        }
    }
}

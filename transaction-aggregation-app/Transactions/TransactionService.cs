using transaction_aggregation_app.Accounts;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Enums;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_app.Transactions
{
    public class TransactionService
    {
        private readonly IEnumerable<ITransactionSource> _sources;
        private readonly TransactionCategoriser _categoriser;
        private readonly AccountService _accountService;

        public TransactionService(IEnumerable<ITransactionSource> sources, TransactionCategoriser categoriser, AccountService accountService)
        {
            _sources = sources;
            _categoriser = categoriser;
            _accountService = accountService;
        }

        public async Task<List<Transaction>> GetAllTransactionsAsync(string accountId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (fromDate > toDate)
            {
                throw new ArgumentException("fromDate must be on or before toDate.");
            }

            var account = await _accountService.GetAccountAsync(accountId);

            var transactions = new List<Transaction>();

            foreach (var source in _sources)
            {
                transactions.AddRange(await source.GetTransactionsAsync(account.Id));
            }

            foreach (var transaction in transactions)
            {
                if (transaction.Category == Categories.Uncategorized)
                {
                    transaction.Category = _categoriser.Categorise(transaction.Description);
                }
            }

            return transactions
                .Where(t => fromDate == null || t.TransactionDate.Date >= fromDate.Value.Date)
                .Where(t => toDate == null || t.TransactionDate.Date <= toDate.Value.Date)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();
        }
    }
}

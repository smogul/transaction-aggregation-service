using transaction_aggregation_app.Accounts;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Enums;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_app.Transactions
{
    // The Transactions module.
    // It asks every data source for an account's transactions, puts them all in one pile,
    // gives each one a category, and then answers questions about the pile.
    //
    // Example: for 1000000001 it collects rows from the two dummy classes, the Excel file,
    // the database and the mock bank API, then tells you "R5 120 went on Food in September".
    public class TransactionService
    {
        private const int MaxPageSize = 100;

        private readonly IEnumerable<ITransactionSource> _sources;
        private readonly TransactionCategoriser _categoriser;
        private readonly AccountService _accountService;

        public TransactionService(IEnumerable<ITransactionSource> sources, TransactionCategoriser categoriser, AccountService accountService)
        {
            _sources = sources;
            _categoriser = categoriser;
            _accountService = accountService;
        }

        // Gets every transaction for the account, newest first.
        // If you give dates, only transactions on or between those dates are kept.
        public async Task<List<Transaction>> GetAllTransactionsAsync(string accountId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            if (fromDate > toDate)
            {
                throw new ArgumentException("fromDate must be on or before toDate.");
            }

            // Stop early if the account number is wrong or the account does not exist.
            // From here on we use account.Id, the cleaned number. Example: " 1000000001 " is now "1000000001".
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

        // Same as above, but filtered by category and source, and cut into pages.
        public async Task<PagedResultDTO<Transaction>> GetTransactionsAsync(string accountId, TransactionFilter filter)
        {
            if (filter.Page < 1)
            {
                throw new ArgumentException("page must be 1 or more.");
            }

            if (filter.PageSize < 1 || filter.PageSize > MaxPageSize)
            {
                throw new ArgumentException($"pageSize must be between 1 and {MaxPageSize}.");
            }

            // Example: " excel " is cleaned to "excel", which still matches the "Excel" source.
            var source = filter.Source?.Trim();

            var transactions = (await GetAllTransactionsAsync(accountId, filter.FromDate, filter.ToDate))
                .Where(t => filter.Category == null || t.Category == filter.Category)
                .Where(t => string.IsNullOrEmpty(source) || t.SourceSystem.Equals(source, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new PagedResultDTO<Transaction>
            {
                // Page 1 skips 0 rows, page 2 skips 20 rows, page 3 skips 40 rows, and so on.
                Items = transactions.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList(),
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalCount = transactions.Count
            };
        }

        public async Task<TransactionSummaryDTO> GetSummaryAsync(string accountId, DateTime? fromDate, DateTime? toDate)
        {
            var account = await _accountService.GetAccountAsync(accountId);
            var transactions = await GetAllTransactionsAsync(account.Id, fromDate, toDate);

            var totalCredit = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);

            // Money out is stored as negative numbers, so we flip the sign to report it as a positive total.
            var totalDebit = transactions.Where(t => t.Amount < 0).Sum(t => -t.Amount);

            return new TransactionSummaryDTO
            {
                AccountId = account.Id,
                FromDate = fromDate,
                ToDate = toDate,
                TotalTransactions = transactions.Count,
                TotalCredit = totalCredit,
                TotalDebit = totalDebit,
                NetAmount = totalCredit - totalDebit,
                TotalsByCategory = transactions
                    .GroupBy(t => t.Category.ToString())
                    .ToDictionary(group => group.Key, group => group.Sum(t => t.Amount))
            };
        }

        public Task<List<GroupTotalDTO>> GetTotalsByCategoryAsync(string accountId, DateTime? fromDate, DateTime? toDate)
        {
            return GetGroupTotalsAsync(accountId, fromDate, toDate, t => t.Category.ToString());
        }

        // Example: every transaction in September 2026 goes into the "2026-09" group.
        public Task<List<GroupTotalDTO>> GetTotalsByMonthAsync(string accountId, DateTime? fromDate, DateTime? toDate)
        {
            return GetGroupTotalsAsync(accountId, fromDate, toDate, t => t.TransactionDate.ToString("yyyy-MM"));
        }

        public Task<List<GroupTotalDTO>> GetTotalsBySourceAsync(string accountId, DateTime? fromDate, DateTime? toDate)
        {
            return GetGroupTotalsAsync(accountId, fromDate, toDate, t => t.SourceSystem);
        }

        // Puts transactions into groups and adds up each group.
        // "groupBy" says what name a transaction's group has, e.g. its category or its month.
        private async Task<List<GroupTotalDTO>> GetGroupTotalsAsync(string accountId, DateTime? fromDate, DateTime? toDate, Func<Transaction, string> groupBy)
        {
            var transactions = await GetAllTransactionsAsync(accountId, fromDate, toDate);

            return transactions
                .GroupBy(groupBy)
                .Select(group => new GroupTotalDTO
                {
                    Name = group.Key,
                    TransactionCount = group.Count(),
                    MoneyIn = group.Where(t => t.Amount > 0).Sum(t => t.Amount),
                    MoneyOut = group.Where(t => t.Amount < 0).Sum(t => -t.Amount)
                })
                .OrderBy(group => group.Name)
                .ToList();
        }
    }
}

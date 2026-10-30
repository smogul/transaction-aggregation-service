using transaction_aggregation_app.Accounts;
using transaction_aggregation_app.Transactions;

namespace transaction_aggregation_app.Balances
{
    // The Balances module. It works out how much money is in an account right now.
    //
    // Example: you start with R5 000 in your piggy bank, put in R75 000 and take out R70 000.
    // Your balance is 5 000 + 75 000 - 70 000 = R10 000.
    public class BalanceService
    {
        private readonly AccountService _accountService;
        private readonly TransactionService _transactionService;

        public BalanceService(AccountService accountService, TransactionService transactionService)
        {
            _accountService = accountService;
            _transactionService = transactionService;
        }

        public async Task<BalanceDTO> GetBalanceAsync(string accountId)
        {
            var account = await _accountService.GetAccountAsync(accountId);
            var transactions = await _transactionService.GetAllTransactionsAsync(account.Id);

            var totalCredit = transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var totalDebit = transactions.Where(t => t.Amount < 0).Sum(t => -t.Amount);

            return new BalanceDTO
            {
                AccountId = account.Id,
                AccountName = account.AccountName,
                Currency = account.Currency,
                OpeningBalance = account.OpeningBalance,
                TotalCredit = totalCredit,
                TotalDebit = totalDebit,
                CurrentBalance = account.OpeningBalance + totalCredit - totalDebit
            };
        }
    }
}

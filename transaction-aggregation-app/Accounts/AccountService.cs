using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Exceptions;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_app.Accounts
{
    // The Accounts module. It is the front door for every account number.
    // The other modules (Transactions, Balances, Statements) always come here first,
    // so an account number is checked in one place only.
    //
    // Example: " 1000000001 " is cleaned to "1000000001" and found.
    // Example: "12345" is stopped right here with "The account number must be exactly 10 digits".
    // Example: "9999999999" is a good number, but it stops here with "Account '9999999999' was not found."
    public class AccountService
    {
        public const int AccountNumberLength = 10;

        private readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<Account> GetAccountAsync(string accountId)
        {
            var accountNumber = CleanAccountNumber(accountId);
            var account = await _accountRepository.GetAccountAsync(accountNumber);

            if (account == null)
            {
                throw new NotFoundException($"Account '{accountNumber}' was not found.");
            }

            return account;
        }

        // Takes away spaces at the start and the end, then checks that only 10 digits are left.
        // Example: " 1000000001 " becomes "1000000001", but "10000000AB" and "123" are refused.
        public static string CleanAccountNumber(string? accountId)
        {
            var accountNumber = accountId?.Trim() ?? string.Empty;

            if (accountNumber.Length != AccountNumberLength || !accountNumber.All(char.IsAsciiDigit))
            {
                throw new ArgumentException($"The account number must be exactly {AccountNumberLength} digits, for example 1000000001. You sent '{accountId}'.");
            }

            return accountNumber;
        }
    }
}

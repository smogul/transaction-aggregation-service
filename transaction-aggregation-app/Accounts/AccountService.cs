using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Exceptions;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_app.Accounts
{
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

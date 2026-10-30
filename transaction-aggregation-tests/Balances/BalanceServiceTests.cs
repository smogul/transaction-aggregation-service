using transaction_aggregation_app.Accounts;
using transaction_aggregation_app.Balances;
using transaction_aggregation_app.Transactions;
using transaction_aggregation_domain.Exceptions;
using transaction_aggregation_tests.Fakes;

namespace transaction_aggregation_tests.Balances
{
    public class BalanceServiceTests
    {
        private readonly BalanceService _service;

        public BalanceServiceTests()
        {
            var source = new FakeTransactionSource("Fake",
                TestData.Transaction("T1", "1000000001", "2026-09-01", "SALARY", 3000m),
                TestData.Transaction("T2", "1000000001", "2026-09-02", "CHECKERS", -400m),
                TestData.Transaction("T3", "1000000002", "2026-09-03", "INTEREST EARNED", 10m));

            var accountService = new AccountService(new FakeAccountRepository());
            var transactionService = new TransactionService(new[] { source }, new TransactionCategoriser(), accountService);

            _service = new BalanceService(accountService, transactionService);
        }

        [Fact]
        public async Task GetBalance_IsOpeningBalancePlusMoneyInMinusMoneyOut()
        {
            var balance = await _service.GetBalanceAsync("1000000001");

            // 1000 to start, + 3000 in, - 400 out = 3600.
            Assert.Equal(1000m, balance.OpeningBalance);
            Assert.Equal(3000m, balance.TotalCredit);
            Assert.Equal(400m, balance.TotalDebit);
            Assert.Equal(3600m, balance.CurrentBalance);
        }

        [Fact]
        public async Task GetBalance_OnlyCountsThatAccountsTransactions()
        {
            var balance = await _service.GetBalanceAsync("1000000002");

            // 500 to start, + 10 interest = 510. The salary and Checkers belong to the other account.
            Assert.Equal(510m, balance.CurrentBalance);
        }

        [Fact]
        public async Task GetBalance_UnknownAccount_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetBalanceAsync("9999999999"));
        }
    }
}

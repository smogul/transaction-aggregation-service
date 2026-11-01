using transaction_aggregation_app.Accounts;
using transaction_aggregation_app.Transactions;
using transaction_aggregation_domain.Enums;
using transaction_aggregation_domain.Exceptions;
using transaction_aggregation_tests.Fakes;

namespace transaction_aggregation_tests.Transactions
{
    public class TransactionServiceTests
    {
        private readonly TransactionService _service;

        public TransactionServiceTests()
        {
            var sourceA = new FakeTransactionSource("SourceA",
                TestData.Transaction("A1", "1000000001", "2026-09-01", "SALARY ACME", 1000m, "SourceA"),
                TestData.Transaction("A2", "1000000001", "2026-09-05", "CHECKERS", -200m, "SourceA"),
                TestData.Transaction("A3", "1000000002", "2026-09-05", "CHECKERS", -50m, "SourceA"));

            var sourceB = new FakeTransactionSource("SourceB",
                TestData.Transaction("B1", "1000000001", "2026-09-20", "NETFLIX", -100m, "SourceB"),
                TestData.Transaction("B2", "1000000001", "2026-10-02", "SPAR", -50m, "SourceB"),
                TestData.Transaction("B3", "1000000001", "2026-10-03", "SOMETHING ODD", -25m, "SourceB"));

            _service = new TransactionService(
                new[] { sourceA, sourceB },
                new TransactionCategoriser(),
                new AccountService(new FakeAccountRepository()));
        }

        [Fact]
        public async Task GetAllTransactions_CollectsFromEverySource_AndCategorisesThem()
        {
            var transactions = await _service.GetAllTransactionsAsync("1000000001");

            Assert.Equal(5, transactions.Count);
            Assert.Equal(Categories.Food, transactions.Single(t => t.Id == "A2").Category);
            Assert.Equal(Categories.Entertainment, transactions.Single(t => t.Id == "B1").Category);
            Assert.Equal(Categories.Uncategorized, transactions.Single(t => t.Id == "B3").Category);
        }

        [Fact]
        public async Task GetAllTransactions_ReturnsNewestFirst()
        {
            var transactions = await _service.GetAllTransactionsAsync("1000000001");

            Assert.Equal("B3", transactions.First().Id);
            Assert.Equal("A1", transactions.Last().Id);
        }

        [Fact]
        public async Task GetAllTransactions_KeepsOnlyDatesInRange_IncludingBothEnds()
        {
            var transactions = await _service.GetAllTransactionsAsync("1000000001", new DateTime(2026, 9, 5), new DateTime(2026, 9, 20));

            Assert.Equal(new[] { "B1", "A2" }, transactions.Select(t => t.Id));
        }

        [Fact]
        public async Task GetAllTransactions_UnknownAccount_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAllTransactionsAsync("9999999999"));
        }

        [Fact]
        public async Task GetAllTransactions_FromDateAfterToDate_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetAllTransactionsAsync("1000000001", new DateTime(2026, 10, 1), new DateTime(2026, 9, 1)));
        }

        [Fact]
        public async Task GetAllTransactions_AlreadyCategorised_KeepsItsCategory()
        {
            var transaction = TestData.Transaction("C1", "1000000001", "2026-09-01", "CHECKERS", -100m);
            transaction.Category = Categories.Transfer;
            var service = new TransactionService(
                new[] { new FakeTransactionSource("Fake", transaction) },
                new TransactionCategoriser(),
                new AccountService(new FakeAccountRepository()));

            var transactions = await service.GetAllTransactionsAsync("1000000001");

            Assert.Equal(Categories.Transfer, Assert.Single(transactions).Category);
        }

        [Fact]
        public async Task GetAllTransactions_AccountNumberWithSpaces_AsksTheSourcesForTheCleanNumber()
        {
            var transactions = await _service.GetAllTransactionsAsync(" 1000000001 ");

            Assert.Equal(5, transactions.Count);
        }
    }
}

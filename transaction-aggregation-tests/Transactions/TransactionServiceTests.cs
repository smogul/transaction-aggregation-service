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

        // Two pretend sources with five transactions for account 1000000001, and one for account 1000000002.
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
        public async Task GetTransactions_CutsTheListIntoPages()
        {
            var page2 = await _service.GetTransactionsAsync("1000000001", new TransactionFilter { Page = 2, PageSize = 2 });

            Assert.Equal(5, page2.TotalCount);
            Assert.Equal(new[] { "B1", "A2" }, page2.Items.Select(t => t.Id));
        }

        [Fact]
        public async Task GetTransactions_FiltersByCategoryAndSource_IgnoringCaseAndSpaces()
        {
            var result = await _service.GetTransactionsAsync("1000000001", new TransactionFilter { Category = Categories.Food, Source = " sourceb " });

            Assert.Equal("B2", Assert.Single(result.Items).Id);
        }

        [Theory]
        [InlineData(0, 20)]
        [InlineData(1, 0)]
        [InlineData(1, 101)]
        public async Task GetTransactions_BadPageOrPageSize_ThrowsArgumentException(int page, int pageSize)
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetTransactionsAsync("1000000001", new TransactionFilter { Page = page, PageSize = pageSize }));
        }

        [Fact]
        public async Task GetSummary_AddsUpMoneyInAndOut()
        {
            var summary = await _service.GetSummaryAsync("1000000001", null, null);

            // In: 1000. Out: 200 + 100 + 50 + 25 = 375.
            Assert.Equal(5, summary.TotalTransactions);
            Assert.Equal(1000m, summary.TotalCredit);
            Assert.Equal(375m, summary.TotalDebit);
            Assert.Equal(625m, summary.NetAmount);
            Assert.Equal(-250m, summary.TotalsByCategory["Food"]);
        }

        [Fact]
        public async Task GetTotalsByMonth_GroupsTransactionsByYearAndMonth()
        {
            var months = await _service.GetTotalsByMonthAsync("1000000001", null, null);

            Assert.Equal(new[] { "2026-09", "2026-10" }, months.Select(m => m.Name));
            Assert.Equal(3, months[0].TransactionCount);
            Assert.Equal(1000m, months[0].MoneyIn);
            Assert.Equal(300m, months[0].MoneyOut);
            Assert.Equal(75m, months[1].MoneyOut);
        }

        [Fact]
        public async Task GetTotalsBySource_HasOneRowPerSource()
        {
            var sources = await _service.GetTotalsBySourceAsync("1000000001", null, null);

            Assert.Equal(new[] { "SourceA", "SourceB" }, sources.Select(s => s.Name));
            Assert.Equal(2, sources[0].TransactionCount);
            Assert.Equal(3, sources[1].TransactionCount);
        }
    }
}

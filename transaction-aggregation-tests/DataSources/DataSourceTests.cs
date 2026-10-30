using transaction_aggregation_infra.DataAccess;
using transaction_aggregation_infra.SeedData;

namespace transaction_aggregation_tests.DataSources
{
    // Checks that each data source turns its own format into our Transaction shape correctly.
    // They all read the real mock data in the SeedData folder.
    public class DataSourceTests
    {
        private static readonly string ExcelFilePath = Path.Combine(AppContext.BaseDirectory, "SeedData", "transactions.xlsx");
        private static readonly string MockBankFilePath = Path.Combine(AppContext.BaseDirectory, "SeedData", "mock-bank-transactions.json");

        private static readonly string[] SeedAccounts = DatabaseSeedData.Accounts.Select(a => a.Id).ToArray();

        [Fact]
        public async Task ExcelSource_ReadsTheSeedFile_AndTurnsDebitAndCreditIntoOneAmount()
        {
            var source = new ExcelTransactionSource(ExcelFilePath);

            var transactions = (await source.GetTransactionsAsync("1000000001")).ToList();

            Assert.Equal(19, transactions.Count);
            Assert.Equal(25000.00m, transactions.Single(t => t.Id == "EXCEL-001").Amount);
            Assert.Equal(-1104.85m, transactions.Single(t => t.Id == "EXCEL-002").Amount);
            Assert.All(transactions, t => Assert.Equal("Excel", t.SourceSystem));
        }

        [Fact]
        public async Task ApiSource_GetsOnlyThatAccountsRowsFromTheMockBank_InRands()
        {
            var source = CreateApiSource();

            var transactions = (await source.GetTransactionsAsync("2000000001")).ToList();

            Assert.Equal(15, transactions.Count);
            Assert.All(transactions, t => Assert.Equal("2000000001", t.AccountId));

            var kfc = transactions.Single(t => t.Id == "MOCKBANK-001");
            Assert.Equal(-79.90m, kfc.Amount);
            Assert.Equal("KFC SOWETO", kfc.Description);
            Assert.Equal(new DateTime(2026, 8, 12), kfc.TransactionDate);
        }

        [Fact]
        public async Task DummySource_GivesNewObjectsEachTime_SoTheSeedDataNeverChanges()
        {
            var source = new MockAbsaStatementSource();

            var first = (await source.GetTransactionsAsync("1000000001")).First();
            first.Description = "CHANGED BY A TEST";
            var second = (await source.GetTransactionsAsync("1000000001")).First();

            Assert.Equal("SALARY ACME HOLDINGS", second.Description);
        }

        [Fact]
        public void SeedAccounts_AllHave10DigitAccountNumbers()
        {
            Assert.All(SeedAccounts, accountId => Assert.Matches("^[0-9]{10}$", accountId));
        }

        [Fact]
        public async Task SeedData_HasAtLeast100Transactions_AcrossAllFiveSources()
        {
            var excel = new ExcelTransactionSource(ExcelFilePath);
            var api = CreateApiSource();
            var count = DatabaseSeedData.Transactions.Count(t => SeedAccounts.Contains(t.AccountId));

            foreach (var accountId in SeedAccounts)
            {
                count += (await new MockAbsaStatementSource().GetTransactionsAsync(accountId)).Count();
                count += (await new MockGoTymeStatementSource().GetTransactionsAsync(accountId)).Count();
                count += (await excel.GetTransactionsAsync(accountId)).Count();
                count += (await api.GetTransactionsAsync(accountId)).Count();
            }

            // 25 rows in each of the 5 sources = 125.
            Assert.True(count >= 100, $"Expected at least 100 seed transactions but found {count}.");
        }

        // The same set-up as Program.cs: an HttpClient whose calls are answered by the pretend bank.
        private static ApiTransactionSource CreateApiSource()
        {
            var httpClient = new HttpClient(new MockBankApiHandler(MockBankFilePath)) { BaseAddress = new Uri("https://mock-bank.local/") };

            return new ApiTransactionSource(httpClient);
        }
    }
}

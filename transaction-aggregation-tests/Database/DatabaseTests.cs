using transaction_aggregation_domain.Entities;
using transaction_aggregation_infra.DataAccess;
using transaction_aggregation_tests.Fakes;

namespace transaction_aggregation_tests.Database
{
    public class DatabaseTests : IDisposable
    {
        private readonly TestDatabase _database = new();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public async Task AccountRepository_FindsASeedAccount()
        {
            using var context = _database.CreateContext();

            var account = await new AccountRepository(context).GetAccountAsync("1000000002");

            Assert.Equal("Savings Account", account?.AccountName);
            Assert.Equal(20000.00m, account?.OpeningBalance);
        }

        [Fact]
        public async Task AccountRepository_UnknownAccount_ReturnsNull()
        {
            using var context = _database.CreateContext();

            Assert.Null(await new AccountRepository(context).GetAccountAsync("9999999999"));
        }

        [Fact]
        public async Task DatabaseSource_ReturnsOnlyThatAccountsSeedRows()
        {
            using var context = _database.CreateContext();

            var source = new DatabaseTransactionSource(context);

            var transactions = (await source.GetTransactionsAsync("1000000001")).ToList();

            Assert.Equal(18, transactions.Count);
            Assert.All(transactions, t => Assert.Equal("1000000001", t.AccountId));
            Assert.All(transactions, t => Assert.Equal(source.SourceSystem, t.SourceSystem));
        }

        [Fact]
        public async Task Statement_IsSavedAndFound_WithItsTimeStillInUtc()
        {
            var statement = await AddStatement("1000000001", DateTime.UtcNow);

            using var context = _database.CreateContext();
            var found = await new StatementRepository(context).GetStatementAsync(statement.Id);

            Assert.NotNull(found);
            Assert.Null(found.FromDate);
            Assert.Equal(statement.CreatedAt, found.CreatedAt);
            Assert.Equal(DateTimeKind.Utc, found.CreatedAt.Kind);
        }

        [Fact]
        public async Task Statement_Unknown_ReturnsNull()
        {
            using var context = _database.CreateContext();

            Assert.Null(await new StatementRepository(context).GetStatementAsync(Guid.NewGuid().ToString()));
        }

        [Fact]
        public async Task AccountStatements_OnlyThatAccount_NewestFirst()
        {
            var older = await AddStatement("1000000001", new DateTime(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc));
            var newer = await AddStatement("1000000001", new DateTime(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc));
            await AddStatement("1000000002", new DateTime(2026, 10, 3, 8, 0, 0, DateTimeKind.Utc));

            using var context = _database.CreateContext();
            var statements = await new StatementRepository(context).GetAccountStatementsAsync("1000000001");

            Assert.Equal(new[] { newer.Id, older.Id }, statements.Select(s => s.Id));
        }

        [Fact]
        public async Task LatestDownloadLink_IsTheOneThatExpiresLast_InUtc()
        {
            var statement = await AddStatement("1000000001", DateTime.UtcNow);
            var other = await AddStatement("1000000001", DateTime.UtcNow);

            await AddDownloadLink(statement.Id, DateTime.UtcNow.AddMinutes(5));
            var latest = await AddDownloadLink(statement.Id, DateTime.UtcNow.AddMinutes(15));
            await AddDownloadLink(other.Id, DateTime.UtcNow.AddMinutes(60));

            using var context = _database.CreateContext();
            var found = await new StatementRepository(context).GetLatestDownloadLinkAsync(statement.Id);

            Assert.Equal(latest.Id, found?.Id);
            Assert.Equal(DateTimeKind.Utc, found?.ExpiresAt.Kind);
        }

        [Fact]
        public async Task LatestDownloadLink_NoLinks_ReturnsNull()
        {
            var statement = await AddStatement("1000000001", DateTime.UtcNow);

            using var context = _database.CreateContext();

            Assert.Null(await new StatementRepository(context).GetLatestDownloadLinkAsync(statement.Id));
        }

        private async Task<Statement> AddStatement(string accountId, DateTime createdAt)
        {
            var statement = new Statement { Id = Guid.NewGuid().ToString(), AccountId = accountId, FileName = "a.pdf", CreatedAt = createdAt };

            using var context = _database.CreateContext();
            await new StatementRepository(context).AddStatementAsync(statement);

            return statement;
        }

        private async Task<DownloadLink> AddDownloadLink(string statementId, DateTime expiresAt)
        {
            var downloadLink = new DownloadLink { Id = Guid.NewGuid().ToString(), StatementId = statementId, ExpiresAt = expiresAt };

            using var context = _database.CreateContext();
            await new StatementRepository(context).AddDownloadLinkAsync(downloadLink);

            return downloadLink;
        }
    }
}

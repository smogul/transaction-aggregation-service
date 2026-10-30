using transaction_aggregation_app.Accounts;
using transaction_aggregation_app.Statements;
using transaction_aggregation_app.Transactions;
using transaction_aggregation_domain.Exceptions;
using transaction_aggregation_infra.Statements;
using transaction_aggregation_tests.Fakes;

namespace transaction_aggregation_tests.Statements
{
    public class StatementServiceTests
    {
        private const string DownloadBaseUrl = "https://bank.test/api/statements/download";

        private static readonly DateTime September1 = new(2026, 9, 1);
        private static readonly DateTime September30 = new(2026, 9, 30);

        private readonly FakeStatementRepository _repository = new();
        private readonly InMemoryStatementFileStore _fileStore = new();
        private readonly FakeStatementPdfGenerator _pdfGenerator = new();

        private StatementService CreateService(int linkLifetimeMinutes = 15, int maxUploadSizeMegabytes = 10)
        {
            var source = new FakeTransactionSource("Fake",
                TestData.Transaction("T1", "1000000001", "2026-08-31", "SALARY", 3000m),
                TestData.Transaction("T2", "1000000001", "2026-09-10", "CHECKERS", -400m),
                TestData.Transaction("T3", "1000000001", "2026-10-01", "SPAR", -50m));

            var accountService = new AccountService(new FakeAccountRepository());
            var transactionService = new TransactionService(new[] { source }, new TransactionCategoriser(), accountService);

            return new StatementService(accountService, transactionService, _pdfGenerator, _fileStore, _repository,
                new StatementOptions { LinkLifetimeMinutes = linkLifetimeMinutes, MaxUploadSizeMegabytes = maxUploadSizeMegabytes });
        }

        [Fact]
        public async Task CreateStatement_SavesThePdfAndTheStatement_WithOnlyTransactionsInThePeriod()
        {
            var service = CreateService();

            var statement = await service.CreateStatementAsync("1000000001", September1, September30);

            Assert.Equal("T2", Assert.Single(_pdfGenerator.LastTransactions).Id);
            Assert.Equal(FakeStatementPdfGenerator.PdfBytes, await _fileStore.ReadAsync(statement.FileName));
            Assert.Same(statement, Assert.Single(_repository.Statements));
        }

        [Fact]
        public async Task CreateStatement_AccountNumberWithSpaces_IsSavedWithTheCleanNumber()
        {
            var service = CreateService();

            var statement = await service.CreateStatementAsync(" 1000000001 ", September1, September30);

            Assert.Equal("1000000001", statement.AccountId);
        }

        [Fact]
        public async Task CreateStatement_UnknownAccount_ThrowsNotFound()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<NotFoundException>(() => service.CreateStatementAsync("9999999999", September1, September30));
        }

        [Fact]
        public async Task UploadStatement_TurnsTheFileIntoAPdfStatement()
        {
            var service = CreateService();

            var statement = await service.UploadStatementAsync("1000000001", September1, September30, " september.docx ", new byte[] { 9, 9, 9 });

            Assert.Equal("september.docx", _pdfGenerator.LastUploadedFileName);
            Assert.Equal(FakeStatementPdfGenerator.PdfBytes, await _fileStore.ReadAsync(statement.FileName));
            Assert.Same(statement, Assert.Single(_repository.Statements));
        }

        [Fact]
        public async Task UploadStatement_FolderNamesInTheFileName_AreRemoved()
        {
            var service = CreateService();

            await service.UploadStatementAsync("1000000001", September1, September30, "../../secret/september.docx", new byte[] { 9 });

            Assert.Equal("september.docx", _pdfGenerator.LastUploadedFileName);
        }

        [Fact]
        public async Task UploadStatement_EmptyFile_ThrowsArgumentException()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UploadStatementAsync("1000000001", September1, September30, "empty.txt", Array.Empty<byte>()));
        }

        [Fact]
        public async Task UploadStatement_FileTooBig_ThrowsArgumentException()
        {
            // With a limit of 1 MB, a file of 1 MB plus 1 byte is one byte too many.
            var service = CreateService(maxUploadSizeMegabytes: 1);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UploadStatementAsync("1000000001", September1, September30, "big.txt", new byte[1024 * 1024 + 1]));
        }

        [Fact]
        public async Task UploadStatement_FromDateAfterToDate_ThrowsArgumentException()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UploadStatementAsync("1000000001", September30, September1, "september.txt", new byte[] { 9 }));
        }

        [Fact]
        public async Task DownloadLink_WorksBeforeItExpires()
        {
            var service = CreateService();
            var statement = await service.CreateStatementAsync("1000000001", September1, September30);

            var link = await service.CreateDownloadLinkAsync(statement.Id, DownloadBaseUrl);
            var (fileName, content) = await service.DownloadStatementAsync(TokenFrom(link.Url));

            Assert.Equal("statement-1000000001-2026-09-01-to-2026-09-30.pdf", fileName);
            Assert.Equal(FakeStatementPdfGenerator.PdfBytes, content);
        }

        [Fact]
        public async Task DownloadLink_StatementIdAndTokenWithSpaces_StillWork()
        {
            var service = CreateService();
            var statement = await service.CreateStatementAsync("1000000001", September1, September30);

            var link = await service.CreateDownloadLinkAsync($" {statement.Id} ", DownloadBaseUrl);
            var (_, content) = await service.DownloadStatementAsync($" {TokenFrom(link.Url)} ");

            Assert.Equal(FakeStatementPdfGenerator.PdfBytes, content);
        }

        [Fact]
        public async Task DownloadLink_OnlyTheHashOfTheTokenIsSaved()
        {
            var service = CreateService();
            var statement = await service.CreateStatementAsync("1000000001", September1, September30);

            var link = await service.CreateDownloadLinkAsync(statement.Id, DownloadBaseUrl);

            // If someone stole the database, the saved value would not work as a link.
            Assert.NotEqual(TokenFrom(link.Url), Assert.Single(_repository.DownloadLinks).TokenHash);
        }

        [Fact]
        public async Task DownloadLink_AfterItExpires_ThrowsNotFound()
        {
            // A lifetime of -1 minute means the link is already expired the moment it is made.
            var service = CreateService(linkLifetimeMinutes: -1);
            var statement = await service.CreateStatementAsync("1000000001", September1, September30);

            var link = await service.CreateDownloadLinkAsync(statement.Id, DownloadBaseUrl);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DownloadStatementAsync(TokenFrom(link.Url)));
        }

        [Fact]
        public async Task DownloadLink_MadeUpToken_ThrowsNotFound()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<NotFoundException>(() => service.DownloadStatementAsync("NOT-A-REAL-TOKEN"));
        }

        [Fact]
        public async Task CreateDownloadLink_UnknownStatement_ThrowsNotFound()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.CreateDownloadLinkAsync("3f2b6c1e-8d4a-4c3b-9e51-2a7d0f6b1c9e", DownloadBaseUrl));
        }

        [Fact]
        public async Task CreateDownloadLink_StatementIdThatIsNotAnId_ThrowsArgumentException()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDownloadLinkAsync("hello", DownloadBaseUrl));
        }

        // The token is the last part of the link: https://bank.test/api/statements/download/{token}
        private static string TokenFrom(string url)
        {
            return url.Split('/').Last();
        }
    }
}

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
        private const string StatementsBaseUrl = "https://bank.test/api/statements";

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

            var statement = await service.UploadStatementAsync("1000000001", " september.docx ", new byte[] { 9, 9, 9 });

            Assert.Equal("september.docx", _pdfGenerator.LastUploadedFileName);
            Assert.Equal(FakeStatementPdfGenerator.PdfBytes, await _fileStore.ReadAsync(statement.FileName));
            Assert.Same(statement, Assert.Single(_repository.Statements));
        }

        [Fact]
        public async Task UploadStatement_FolderNamesInTheFileName_AreRemoved()
        {
            var service = CreateService();

            await service.UploadStatementAsync("1000000001", "../../secret/september.docx", new byte[] { 9 });

            Assert.Equal("september.docx", _pdfGenerator.LastUploadedFileName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("folder/")]
        public async Task UploadStatement_FileWithNoName_ThrowsArgumentException(string fileName)
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() => service.UploadStatementAsync("1000000001", fileName, new byte[] { 9 }));
        }

        [Fact]
        public async Task UploadStatement_FileNameMissingAltogether_ThrowsArgumentException()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() => service.UploadStatementAsync("1000000001", null!, new byte[] { 9 }));
        }

        [Fact]
        public async Task CreateDownloadLink_StatementIdMissingAltogether_ThrowsArgumentException()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDownloadLinkAsync(null!, StatementsBaseUrl));
        }

        [Fact]
        public async Task UploadStatement_EmptyFile_ThrowsArgumentException()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UploadStatementAsync("1000000001", "empty.txt", Array.Empty<byte>()));
        }

        [Fact]
        public async Task UploadStatement_FileTooBig_ThrowsArgumentException()
        {
            var service = CreateService(maxUploadSizeMegabytes: 1);

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UploadStatementAsync("1000000001", "big.txt", new byte[1024 * 1024 + 1]));
        }

        [Fact]
        public async Task UploadStatement_HasNoPeriod_AndDownloadsUnderItsUploadDate()
        {
            var service = CreateService();
            var statement = await service.UploadStatementAsync("1000000001", "september.txt", new byte[] { 9 });

            var link = await service.CreateDownloadLinkAsync(statement.Id, StatementsBaseUrl);
            var (fileName, _) = await service.DownloadStatementAsync(statement.Id);

            Assert.Null(statement.FromDate);
            Assert.Null(statement.ToDate);
            Assert.Null(link.FromDate);
            Assert.Null(link.ToDate);
            Assert.Equal($"statement-1000000001-{statement.CreatedAt:yyyy-MM-dd}.pdf", fileName);
        }

        [Fact]
        public async Task CreateStatementDownloadLink_MakesAStatementForThePeriod_WithALinkToIt()
        {
            var service = CreateService();

            var link = await service.CreateStatementDownloadLinkAsync("1000000001", September1, September30, StatementsBaseUrl);

            var statement = Assert.Single(_repository.Statements);
            Assert.Equal(statement.Id, link.StatementId);
            Assert.Equal(September1, link.FromDate);
            Assert.Equal(September30, link.ToDate);
            Assert.Equal($"https://bank.test/api/statements/{statement.Id}/download", link.Url);
            Assert.Equal("T2", Assert.Single(_pdfGenerator.LastTransactions).Id);
        }

        [Fact]
        public async Task CreateStatementDownloadLink_DownloadIsNamedAfterThePeriod()
        {
            var service = CreateService();

            var link = await service.CreateStatementDownloadLinkAsync(" 1000000001 ", September1, September30, StatementsBaseUrl);
            var (fileName, content) = await service.DownloadStatementAsync(link.StatementId);

            Assert.Equal("statement-1000000001-2026-09-01-to-2026-09-30.pdf", fileName);
            Assert.Equal(FakeStatementPdfGenerator.PdfBytes, content);
        }

        [Fact]
        public async Task CreateStatementDownloadLink_FromDateAfterToDate_ThrowsArgumentException()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateStatementDownloadLinkAsync("1000000001", September30, September1, StatementsBaseUrl));
            Assert.Empty(_repository.Statements);
        }

        [Fact]
        public async Task CreateStatementDownloadLink_UnknownAccount_ThrowsNotFound()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.CreateStatementDownloadLinkAsync("9999999999", September1, September30, StatementsBaseUrl));
        }

        [Fact]
        public async Task CreateDownloadLink_PointsAtTheStatement_WithoutAToken()
        {
            var service = CreateService();
            var statement = await service.CreateStatementAsync("1000000001", September1, September30);

            var link = await service.CreateDownloadLinkAsync(statement.Id, StatementsBaseUrl);

            Assert.Equal($"https://bank.test/api/statements/{statement.Id}/download", link.Url);
        }

        [Fact]
        public async Task Download_WorksBeforeTheLinkExpires()
        {
            var service = CreateService();
            var statement = await service.CreateStatementAsync("1000000001", September1, September30);

            await service.CreateDownloadLinkAsync(statement.Id, StatementsBaseUrl);
            var (fileName, content) = await service.DownloadStatementAsync(statement.Id);

            Assert.Equal("statement-1000000001-2026-09-01-to-2026-09-30.pdf", fileName);
            Assert.Equal(FakeStatementPdfGenerator.PdfBytes, content);
        }

        [Fact]
        public async Task Download_StatementIdWithSpaces_StillWorks()
        {
            var service = CreateService();
            var statement = await service.CreateStatementAsync("1000000001", September1, September30);

            await service.CreateDownloadLinkAsync($" {statement.Id} ", StatementsBaseUrl);
            var (_, content) = await service.DownloadStatementAsync($" {statement.Id} ");

            Assert.Equal(FakeStatementPdfGenerator.PdfBytes, content);
        }

        [Fact]
        public async Task Download_WithoutADownloadLink_ThrowsNotFound()
        {
            var service = CreateService();
            var statement = await service.CreateStatementAsync("1000000001", September1, September30);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DownloadStatementAsync(statement.Id));
        }

        [Fact]
        public async Task Download_AfterTheLinkExpires_ThrowsNotFound()
        {
            var service = CreateService(linkLifetimeMinutes: -1);
            var statement = await service.CreateStatementAsync("1000000001", September1, September30);

            await service.CreateDownloadLinkAsync(statement.Id, StatementsBaseUrl);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DownloadStatementAsync(statement.Id));
        }

        [Fact]
        public async Task Download_UnknownStatement_ThrowsNotFound()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<NotFoundException>(() => service.DownloadStatementAsync("3f2b6c1e-8d4a-4c3b-9e51-2a7d0f6b1c9e"));
        }

        [Fact]
        public async Task CreateDownloadLink_UnknownStatement_ThrowsNotFound()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.CreateDownloadLinkAsync("3f2b6c1e-8d4a-4c3b-9e51-2a7d0f6b1c9e", StatementsBaseUrl));
        }

        [Fact]
        public async Task CreateDownloadLink_StatementIdThatIsNotAnId_ThrowsArgumentException()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateDownloadLinkAsync("hello", StatementsBaseUrl));
        }
    }
}

using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_tests.Fakes
{
    // Pretend versions of the statement database and the PDF maker.
    // Everything is kept in memory, so the tests never touch the disk.

    public class FakeStatementRepository : IStatementRepository
    {
        public List<Statement> Statements { get; } = new();
        public List<DownloadLink> DownloadLinks { get; } = new();

        public Task AddStatementAsync(Statement statement)
        {
            Statements.Add(statement);
            return Task.CompletedTask;
        }

        public Task<Statement?> GetStatementAsync(string statementId)
        {
            return Task.FromResult(Statements.FirstOrDefault(s => s.Id == statementId));
        }

        public Task<IEnumerable<Statement>> GetAccountStatementsAsync(string accountId)
        {
            return Task.FromResult<IEnumerable<Statement>>(Statements.Where(s => s.AccountId == accountId).ToList());
        }

        public Task AddDownloadLinkAsync(DownloadLink downloadLink)
        {
            DownloadLinks.Add(downloadLink);
            return Task.CompletedTask;
        }

        public Task<DownloadLink?> GetDownloadLinkAsync(string tokenHash)
        {
            return Task.FromResult(DownloadLinks.FirstOrDefault(l => l.TokenHash == tokenHash));
        }
    }

    public class FakeStatementPdfGenerator : IStatementPdfGenerator
    {
        public static readonly byte[] PdfBytes = { 1, 2, 3 };

        public List<Transaction> LastTransactions { get; private set; } = new();
        public string? LastUploadedFileName { get; private set; }

        public byte[] CreatePdf(Account account, DateTime fromDate, DateTime toDate, IEnumerable<Transaction> transactions)
        {
            LastTransactions = transactions.ToList();
            return PdfBytes;
        }

        public byte[] CreatePdfFromDocument(Account account, DateTime fromDate, DateTime toDate, string fileName, byte[] content)
        {
            LastUploadedFileName = fileName;
            return PdfBytes;
        }
    }
}

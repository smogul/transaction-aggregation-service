using System.Security.Cryptography;
using System.Text;
using transaction_aggregation_app.Accounts;
using transaction_aggregation_app.Transactions;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Exceptions;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_app.Statements
{
    // Makes PDF statements and hands out download links that stop working after a while.
    //
    // How a customer gets a statement:
    //   1. CreateStatementAsync or UploadStatementAsync -> we make the PDF once and keep it in the file store.
    //   2. CreateDownloadLinkAsync -> we give the customer a secret link, like a movie ticket.
    //   3. DownloadStatementAsync  -> the customer uses the link, and we check the ticket is real and not expired.
    public class StatementService
    {
        private readonly AccountService _accountService;
        private readonly TransactionService _transactionService;
        private readonly IStatementPdfGenerator _pdfGenerator;
        private readonly IStatementFileStore _fileStore;
        private readonly IStatementRepository _statementRepository;
        private readonly StatementOptions _options;

        public StatementService(
            AccountService accountService,
            TransactionService transactionService,
            IStatementPdfGenerator pdfGenerator,
            IStatementFileStore fileStore,
            IStatementRepository statementRepository,
            StatementOptions options)
        {
            _accountService = accountService;
            _transactionService = transactionService;
            _pdfGenerator = pdfGenerator;
            _fileStore = fileStore;
            _statementRepository = statementRepository;
            _options = options;
        }

        // Example: CreateStatementAsync("1000000001", 1 Sep 2026, 30 Sep 2026) makes a PDF of September.
        public async Task<Statement> CreateStatementAsync(string accountId, DateTime fromDate, DateTime toDate)
        {
            var account = await _accountService.GetAccountAsync(accountId);
            var transactions = await _transactionService.GetAllTransactionsAsync(account.Id, fromDate, toDate);

            var pdf = _pdfGenerator.CreatePdf(account, fromDate.Date, toDate.Date, transactions);

            return await SaveStatementAsync(account, fromDate, toDate, pdf);
        }

        // Turns a file the customer uploaded into a new PDF statement.
        // Example: UploadStatementAsync("1000000001", 1 Sep 2026, 30 Sep 2026, "september.docx", ...) makes a PDF of that Word file.
        public async Task<Statement> UploadStatementAsync(string accountId, DateTime fromDate, DateTime toDate, string fileName, byte[] content)
        {
            if (fromDate > toDate)
            {
                throw new ArgumentException("fromDate must be on or before toDate.");
            }

            var cleanFileName = Path.GetFileName(fileName?.Trim() ?? string.Empty);

            if (cleanFileName.Length == 0)
            {
                throw new ArgumentException("The uploaded file needs a name, for example september.docx.");
            }

            if (content.Length == 0)
            {
                throw new ArgumentException($"'{cleanFileName}' is empty.");
            }

            if (content.Length > _options.MaxUploadSizeMegabytes * 1024 * 1024)
            {
                throw new ArgumentException($"'{cleanFileName}' is too big. The biggest file you can upload is {_options.MaxUploadSizeMegabytes} MB.");
            }

            var account = await _accountService.GetAccountAsync(accountId);
            var pdf = _pdfGenerator.CreatePdfFromDocument(account, fromDate.Date, toDate.Date, cleanFileName, content);

            return await SaveStatementAsync(account, fromDate, toDate, pdf);
        }

        public async Task<IEnumerable<Statement>> GetAccountStatementsAsync(string accountId)
        {
            var account = await _accountService.GetAccountAsync(accountId);

            return await _statementRepository.GetAccountStatementsAsync(account.Id);
        }

        // downloadBaseUrl is the start of the link, e.g. "https://localhost:7194/api/statements/download".
        public async Task<DownloadLinkDTO> CreateDownloadLinkAsync(string statementId, string downloadBaseUrl)
        {
            // Example: " 3f2b6c1e-... " is cleaned to "3f2b6c1e-...". "hello" is refused, because it is not a statement id.
            var cleanStatementId = statementId?.Trim() ?? string.Empty;

            if (!Guid.TryParse(cleanStatementId, out _))
            {
                throw new ArgumentException($"The statement id must look like 3f2b6c1e-8d4a-4c3b-9e51-2a7d0f6b1c9e. You sent '{statementId}'.");
            }

            var statement = await _statementRepository.GetStatementAsync(cleanStatementId);

            if (statement == null)
            {
                throw new NotFoundException($"Statement '{cleanStatementId}' was not found.");
            }

            // 32 random bytes written as 64 letters and numbers. Nobody can guess it.
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            var downloadLink = new DownloadLink
            {
                Id = Guid.NewGuid().ToString(),
                StatementId = statement.Id,
                TokenHash = HashToken(token),
                ExpiresAt = DateTime.UtcNow.AddMinutes(_options.LinkLifetimeMinutes)
            };
            await _statementRepository.AddDownloadLinkAsync(downloadLink);

            return new DownloadLinkDTO
            {
                StatementId = statement.Id,
                Url = $"{downloadBaseUrl}/{token}",
                ExpiresAt = downloadLink.ExpiresAt
            };
        }

        // Returns the PDF if the link is real and has not expired.
        public async Task<(string FileName, byte[] Content)> DownloadStatementAsync(string token)
        {
            var downloadLink = await _statementRepository.GetDownloadLinkAsync(HashToken(token?.Trim() ?? string.Empty));

            // A fake link and an expired link get the same answer, so nobody learns which links once existed.
            if (downloadLink == null || downloadLink.ExpiresAt < DateTime.UtcNow)
            {
                throw new NotFoundException("This download link is not valid or has expired.");
            }

            var statement = await _statementRepository.GetStatementAsync(downloadLink.StatementId);

            if (statement == null)
            {
                throw new NotFoundException("This download link is not valid or has expired.");
            }

            var content = await _fileStore.ReadAsync(statement.FileName);
            var fileName = $"statement-{statement.AccountId}-{statement.FromDate:yyyy-MM-dd}-to-{statement.ToDate:yyyy-MM-dd}.pdf";

            return (fileName, content);
        }

        // Both kinds of statement are kept the same way: the PDF goes into the file store,
        // and the facts about it (which account, which dates) go into the database.
        private async Task<Statement> SaveStatementAsync(Account account, DateTime fromDate, DateTime toDate, byte[] pdf)
        {
            var statement = new Statement
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = account.Id,
                FromDate = fromDate.Date,
                ToDate = toDate.Date,
                CreatedAt = DateTime.UtcNow
            };
            statement.FileName = $"{statement.Id}.pdf";

            await _fileStore.SaveAsync(statement.FileName, pdf);
            await _statementRepository.AddStatementAsync(statement);

            return statement;
        }

        // Scrambles the token the same way every time, but it cannot be unscrambled.
        // Example: "A1B2..." always becomes "9F86...", and "9F86..." cannot be turned back into "A1B2...".
        private static string HashToken(string token)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        }
    }
}

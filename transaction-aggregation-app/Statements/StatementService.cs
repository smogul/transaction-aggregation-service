using transaction_aggregation_app.Accounts;
using transaction_aggregation_app.Transactions;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Exceptions;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_app.Statements
{
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

        public async Task<Statement> CreateStatementAsync(string accountId, DateTime fromDate, DateTime toDate)
        {
            var account = await _accountService.GetAccountAsync(accountId);
            var transactions = await _transactionService.GetAllTransactionsAsync(account.Id, fromDate, toDate);

            var pdf = _pdfGenerator.CreatePdf(account, fromDate.Date, toDate.Date, transactions);

            return await SaveStatementAsync(account, fromDate, toDate, pdf);
        }

        public async Task<Statement> UploadStatementAsync(string accountId, string fileName, byte[] content)
        {
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
            var pdf = _pdfGenerator.CreatePdfFromDocument(account, cleanFileName, content);

            return await SaveStatementAsync(account, null, null, pdf);
        }

        public async Task<IEnumerable<Statement>> GetAccountStatementsAsync(string accountId)
        {
            var account = await _accountService.GetAccountAsync(accountId);

            return await _statementRepository.GetAccountStatementsAsync(account.Id);
        }

        public async Task<DownloadLinkDTO> CreateDownloadLinkAsync(string statementId, string statementsBaseUrl)
        {
            var statement = await GetStatementAsync(statementId);

            var downloadLink = new DownloadLink
            {
                Id = Guid.NewGuid().ToString(),
                StatementId = statement.Id,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_options.LinkLifetimeMinutes)
            };
            await _statementRepository.AddDownloadLinkAsync(downloadLink);

            return new DownloadLinkDTO
            {
                StatementId = statement.Id,
                FromDate = statement.FromDate,
                ToDate = statement.ToDate,
                Url = $"{statementsBaseUrl}/{statement.Id}/download",
                ExpiresAt = downloadLink.ExpiresAt
            };
        }

        public async Task<DownloadLinkDTO> CreateStatementDownloadLinkAsync(string accountId, DateTime fromDate, DateTime toDate, string statementsBaseUrl)
        {
            var statement = await CreateStatementAsync(accountId, fromDate, toDate);

            return await CreateDownloadLinkAsync(statement.Id, statementsBaseUrl);
        }

        public async Task<(string FileName, byte[] Content)> DownloadStatementAsync(string statementId)
        {
            var statement = await GetStatementAsync(statementId);
            var downloadLink = await _statementRepository.GetLatestDownloadLinkAsync(statement.Id);

            if (downloadLink == null || downloadLink.ExpiresAt < DateTime.UtcNow)
            {
                throw new NotFoundException("This statement has no valid download link. Ask for a new download link first.");
            }

            var content = await _fileStore.ReadAsync(statement.FileName);
            var fileName = statement.FromDate == null
                ? $"statement-{statement.AccountId}-{statement.CreatedAt:yyyy-MM-dd}.pdf"
                : $"statement-{statement.AccountId}-{statement.FromDate:yyyy-MM-dd}-to-{statement.ToDate:yyyy-MM-dd}.pdf";

            return (fileName, content);
        }

        private async Task<Statement> SaveStatementAsync(Account account, DateTime? fromDate, DateTime? toDate, byte[] pdf)
        {
            var statement = new Statement
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = account.Id,
                FromDate = fromDate?.Date,
                ToDate = toDate?.Date,
                CreatedAt = DateTime.UtcNow
            };
            statement.FileName = $"{statement.Id}.pdf";

            await _fileStore.SaveAsync(statement.FileName, pdf);
            await _statementRepository.AddStatementAsync(statement);

            return statement;
        }

        private async Task<Statement> GetStatementAsync(string statementId)
        {
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

            return statement;
        }
    }
}

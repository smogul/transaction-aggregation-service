using transaction_aggregation_domain.Entities;

namespace transaction_aggregation_domain.Interfaces
{
    // Saves and finds statements and their download links in the database.
    public interface IStatementRepository
    {
        Task AddStatementAsync(Statement statement);
        Task<Statement?> GetStatementAsync(string statementId);
        Task<IEnumerable<Statement>> GetAccountStatementsAsync(string accountId);

        Task AddDownloadLinkAsync(DownloadLink downloadLink);

        // We look a link up by the scrambled copy of its token, because that is all we saved.
        Task<DownloadLink?> GetDownloadLinkAsync(string tokenHash);
    }
}

using transaction_aggregation_domain.Entities;

namespace transaction_aggregation_domain.Interfaces
{
    public interface IStatementRepository
    {
        Task AddStatementAsync(Statement statement);
        Task<Statement?> GetStatementAsync(string statementId);
        Task<IEnumerable<Statement>> GetAccountStatementsAsync(string accountId);

        Task AddDownloadLinkAsync(DownloadLink downloadLink);

        Task<DownloadLink?> GetLatestDownloadLinkAsync(string statementId);
    }
}

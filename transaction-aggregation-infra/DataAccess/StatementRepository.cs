using Microsoft.EntityFrameworkCore;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_infra.DataAccess
{
    public class StatementRepository : IStatementRepository
    {
        private readonly AppDbContext _dbContext;

        public StatementRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddStatementAsync(Statement statement)
        {
            _dbContext.Statements.Add(statement);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Statement?> GetStatementAsync(string statementId)
        {
            return await _dbContext.Statements.AsNoTracking().FirstOrDefaultAsync(s => s.Id == statementId);
        }

        public async Task<IEnumerable<Statement>> GetAccountStatementsAsync(string accountId)
        {
            return await _dbContext.Statements
                .AsNoTracking()
                .Where(s => s.AccountId == accountId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task AddDownloadLinkAsync(DownloadLink downloadLink)
        {
            _dbContext.DownloadLinks.Add(downloadLink);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<DownloadLink?> GetDownloadLinkAsync(string tokenHash)
        {
            return await _dbContext.DownloadLinks.AsNoTracking().FirstOrDefaultAsync(l => l.TokenHash == tokenHash);
        }
    }
}

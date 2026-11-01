using transaction_aggregation_domain.Entities;

namespace transaction_aggregation_domain.Interfaces
{
    public interface IStatementPdfGenerator
    {
        byte[] CreatePdf(Account account, DateTime fromDate, DateTime toDate, IEnumerable<Transaction> transactions);

        byte[] CreatePdfFromDocument(Account account, string fileName, byte[] content);
    }
}

using transaction_aggregation_domain.Entities;

namespace transaction_aggregation_domain.Interfaces
{
    // Makes the bytes of a statement PDF file.
    public interface IStatementPdfGenerator
    {
        // Draws a table of an account's transactions.
        byte[] CreatePdf(Account account, DateTime fromDate, DateTime toDate, IEnumerable<Transaction> transactions);

        // Turns a file the customer uploaded into a PDF. Example: "september.docx" in, a PDF out.
        // Throws an ArgumentException when the kind of file is not supported or the file is damaged.
        byte[] CreatePdfFromDocument(Account account, DateTime fromDate, DateTime toDate, string fileName, byte[] content);
    }
}

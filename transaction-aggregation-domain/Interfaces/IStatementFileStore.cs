namespace transaction_aggregation_domain.Interfaces
{
    // A cupboard for PDF files.
    // Today the cupboard is the computer's memory. Later it could be cloud storage,
    // and nothing else in the code would need to change.
    public interface IStatementFileStore
    {
        Task SaveAsync(string fileName, byte[] content);
        Task<byte[]> ReadAsync(string fileName);
    }
}

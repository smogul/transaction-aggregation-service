namespace transaction_aggregation_domain.Interfaces
{
    public interface IStatementFileStore
    {
        Task SaveAsync(string fileName, byte[] content);
        Task<byte[]> ReadAsync(string fileName);
    }
}

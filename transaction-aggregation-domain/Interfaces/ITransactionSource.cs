namespace transaction_aggregation_domain.Interfaces
{
    public interface ITransactionSource
    {
        string SourceSystem { get; }
        Task<IEnumerable<Entities.Transaction>> GetTransactionsAsync(string accountId);
    }
}

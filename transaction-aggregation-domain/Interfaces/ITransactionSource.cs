namespace transaction_aggregation_domain.Interfaces
{
    // A place we can collect transactions from.
    // Every source stores its data differently (a C# list, an Excel file, a database, another API),
    // but they all answer the same question: "give me the transactions for this account".
    //
    // Example: the Excel source opens transactions.xlsx and returns the rows for "1000000001".
    public interface ITransactionSource
    {
        string SourceSystem { get; }
        Task<IEnumerable<Entities.Transaction>> GetTransactionsAsync(string accountId);
    }
}

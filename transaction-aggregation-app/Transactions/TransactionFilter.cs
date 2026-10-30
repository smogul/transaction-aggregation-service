using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_app.Transactions
{
    // The questions you can ask when listing transactions. Every one of them is optional.
    // Example: ?fromDate=2026-09-01&toDate=2026-09-30&category=Food&page=1&pageSize=20
    // means "show me the first 20 Food transactions from September".
    public class TransactionFilter
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Categories? Category { get; set; }

        // Only show one data source, e.g. "Excel" or "MockBankApi".
        public string? Source { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

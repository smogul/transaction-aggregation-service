using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_domain.Entities
{
    // A Transaction is one line on a bank statement: money that came in or went out.
    // Example: on 2 Aug you paid R845.30 at Checkers, so Amount is -845.30.
    //
    // The sign of Amount tells you the direction:
    //   positive (+25000.00) = money IN, like a salary
    //   negative (-845.30)   = money OUT, like buying groceries
    public class Transaction
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Currency Currency { get; set; } = Currency.ZAR;
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public Categories Category { get; set; } = Categories.Uncategorized;

        // Which data source this transaction came from, e.g. "Excel" or "MockBankApi".
        public string SourceSystem { get; set; } = string.Empty;
    }
}

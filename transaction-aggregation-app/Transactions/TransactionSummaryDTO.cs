namespace transaction_aggregation_app.Transactions
{
    // A short report card for an account.
    // Example: "in September 50 transactions happened, R27 000 came in, R24 500 went out,
    // and R3 200 of that was spent on Medical".
    public class TransactionSummaryDTO
    {
        public string AccountId { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }

        // Money in minus money out. Example: 27 000 - 24 500 = 2 500.
        public decimal NetAmount { get; set; }

        // How much money went in or out for each category, e.g. { "Food": -2 403.50 }.
        public Dictionary<string, decimal> TotalsByCategory { get; set; } = new();
    }
}

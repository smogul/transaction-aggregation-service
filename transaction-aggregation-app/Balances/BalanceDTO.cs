using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_app.Balances
{
    // How much money is in an account right now.
    // Example: started with 5 000, then 75 000 came in and 70 000 went out,
    // so the CurrentBalance is 5 000 + 75 000 - 70 000 = 10 000.
    public class BalanceDTO
    {
        public string AccountId { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public Currency Currency { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal CurrentBalance { get; set; }
    }
}

using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_app.Balances
{
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

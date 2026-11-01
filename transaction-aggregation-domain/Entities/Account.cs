using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_domain.Entities
{
    public class Account
    {
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public Currency Currency { get; set; } = Currency.ZAR;

        public decimal OpeningBalance { get; set; }
    }
}

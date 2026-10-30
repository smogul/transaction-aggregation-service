using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_domain.Entities
{
    // An Account is like a piggy bank that belongs to one customer.
    // Example: account "1000000001" belongs to customer "CUST-001" and started with R5 000 in it.
    public class Account
    {
        // The account number. It is always exactly 10 digits, e.g. "1000000001".
        public string Id { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public Currency Currency { get; set; } = Currency.ZAR;

        // How much money was in the account before any of our transactions happened.
        public decimal OpeningBalance { get; set; }
    }
}

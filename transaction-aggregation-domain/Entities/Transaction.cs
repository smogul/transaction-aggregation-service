using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_domain.Entities
{
    public class Transaction
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public Currency Currency { get; set; } = Currency.ZAR;
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public Categories Category { get; set; } = Categories.Uncategorized;
        public string SourceSystem { get; set; } = string.Empty;
    }
}

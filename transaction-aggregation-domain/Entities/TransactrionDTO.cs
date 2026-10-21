using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace transaction_aggregation_domain.Entities
{
    public class TransactrionDTO
    {
        public string AccountId { get; set; } = string.Empty;
        public int TotalTransactions { get; set; }
        public decimal TotalCredit { get; set; } 
        public decimal TotalDebit { get; set; }
        public Dictionary<string, decimal> SortByCategory { get; set; } = new();
        public IEnumerable<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}

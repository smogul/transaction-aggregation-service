using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace transaction_aggregation_domain.Interfaces
{
    public interface ITransactionSource
    {
        string SourceSystem { get; }
        Task<IEnumerable<Entities.Transaction>> GetTransactionsAsync(string accountId);
    }
}

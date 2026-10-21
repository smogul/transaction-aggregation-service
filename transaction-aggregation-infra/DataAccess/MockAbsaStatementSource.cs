using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Enums;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_infra.DataAccess
{
    public class MockAbsaStatementSource : ITransactionSource
    {
        public string SourceSystem => "AbsaStatements";

        public Task<IEnumerable<Transaction>> GetTransactionsAsync(string accountId)
        {
            return Task.FromResult<IEnumerable<Transaction>>(new List<Transaction>
            {
                new ()
                {
                    Id = Guid.NewGuid().ToString(),
                    AccountId = accountId,
                    Amount = 100.00m,
                    Currency = Currency.USD,
                    TransactionDate = DateTime.UtcNow.AddDays(-1),
                    Description = "Mock Transaction 1",
                    Category = Categories.Uncategorized,
                    SourceSystem = SourceSystem
                },
                new ()
                {
                    Id = Guid.NewGuid().ToString(),
                    AccountId = accountId,
                    Amount = 200.00m,
                    Currency = Currency.USD,
                    TransactionDate = DateTime.UtcNow.AddDays(-2),
                    Description = "Mock Transaction 2",
                    Category = Categories.Uncategorized,
                    SourceSystem = SourceSystem
                }
            }
            );
        }
    }
}

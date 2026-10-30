using ClosedXML.Excel;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Enums;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_infra.DataAccess
{
    // Data source 3 of 5: an Excel spreadsheet (SeedData/transactions.xlsx), 25 transactions from October 2026.
    //
    // The spreadsheet looks like a normal bank export:
    //   | TransactionId | AccountId  | Date       | Description          | Debit  | Credit   |
    //   | EXCEL-001     | 1000000001 | 2026-10-01 | SALARY ACME HOLDINGS |        | 25000.00 |
    //   | EXCEL-002     | 1000000001 | 2026-10-02 | CHECKERS HYPER       | 1104.85|          |
    //
    // Money out sits in the Debit column and money in sits in the Credit column.
    // We turn the two columns into one Amount: Credit - Debit.
    // Example: Debit 1104.85 and an empty Credit becomes Amount = -1104.85.
    public class ExcelTransactionSource : ITransactionSource
    {
        private readonly string _filePath;

        public ExcelTransactionSource(string filePath)
        {
            _filePath = filePath;
        }

        public string SourceSystem => "Excel";

        public Task<IEnumerable<Transaction>> GetTransactionsAsync(string accountId)
        {
            var transactions = new List<Transaction>();

            using var workbook = new XLWorkbook(_filePath);
            var sheet = workbook.Worksheet(1);

            // Skip(1) jumps over the header row with the column names.
            foreach (var row in sheet.RowsUsed().Skip(1))
            {
                if (row.Cell(2).GetString() != accountId)
                {
                    continue;
                }

                var debit = row.Cell(5).IsEmpty() ? 0 : row.Cell(5).GetValue<decimal>();
                var credit = row.Cell(6).IsEmpty() ? 0 : row.Cell(6).GetValue<decimal>();

                transactions.Add(new Transaction
                {
                    Id = row.Cell(1).GetString(),
                    AccountId = accountId,
                    TransactionDate = row.Cell(3).GetDateTime(),
                    Description = row.Cell(4).GetString(),
                    Amount = credit - debit,
                    Currency = Currency.ZAR,
                    Category = Categories.Uncategorized,
                    SourceSystem = SourceSystem
                });
            }

            return Task.FromResult<IEnumerable<Transaction>>(transactions);
        }
    }
}

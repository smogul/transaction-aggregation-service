using System.Net.Http.Json;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Enums;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_infra.DataAccess
{
    public class ApiTransactionSource : ITransactionSource
    {
        private readonly HttpClient _httpClient;

        public ApiTransactionSource(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public string SourceSystem => "MockBankApi";

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(string accountId)
        {
            var url = $"accounts/{Uri.EscapeDataString(accountId)}/transactions";
            var records = await _httpClient.GetFromJsonAsync<List<MockBankTransaction>>(url) ?? new List<MockBankTransaction>();

            return records.Select(record => new Transaction
            {
                Id = record.Reference,
                AccountId = accountId,
                Amount = record.Amount,
                Currency = Currency.ZAR,
                TransactionDate = record.Date,
                Description = record.Narrative,
                Category = Categories.Uncategorized,
                SourceSystem = SourceSystem
            }).ToList();
        }

        private class MockBankTransaction
        {
            public string Reference { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
            public string Narrative { get; set; } = string.Empty;
        }
    }
}

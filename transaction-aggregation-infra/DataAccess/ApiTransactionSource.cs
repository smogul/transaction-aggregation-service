using System.Net.Http.Json;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Enums;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_infra.DataAccess
{
    // Data source 5 of 5: another bank's web API.
    // We call GET accounts/{accountId}/transactions over HTTP, just like a real bank feed.
    // The "other bank" is pretend: MockBankApiHandler answers the call using SeedData/mock-bank-transactions.json.
    //
    // That bank uses its own words: it calls the description a "narrative" and the id a "reference".
    // So we translate each row into our own Transaction shape.
    // Example: { "reference": "MOCKBANK-001", "amount": -79.90, "narrative": "KFC SOWETO" }
    // becomes a Transaction with Id "MOCKBANK-001", Amount -79.90 and Description "KFC SOWETO".
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

        // One row exactly as the other bank sends it to us. Amount is in rands, e.g. -79.90.
        private class MockBankTransaction
        {
            public string Reference { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime Date { get; set; }
            public string Narrative { get; set; } = string.Empty;
        }
    }
}

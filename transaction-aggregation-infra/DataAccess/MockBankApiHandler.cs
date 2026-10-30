using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace transaction_aggregation_infra.DataAccess
{
    // Pretends to be the other bank's web server, so ApiTransactionSource has someone to talk to.
    //
    // Normally an HttpClient sends a request over the internet. This handler catches the request
    // before it leaves the app and answers it straight away from SeedData/mock-bank-transactions.json.
    // Example: GET accounts/2000000001/transactions gets back the 15 rows for account 2000000001.
    //
    // To use a real bank instead, point DataSources:MockBankApiUrl at it and stop using this handler in Program.cs.
    public class MockBankApiHandler : HttpMessageHandler
    {
        private readonly string _filePath;

        public MockBankApiHandler(string filePath)
        {
            _filePath = filePath;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // The address looks like ".../accounts/2000000001/transactions", so the account is the second-last part.
            var parts = request.RequestUri!.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var accountId = Uri.UnescapeDataString(parts[^2]);

            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            var rows = JsonNode.Parse(json)!.AsArray()
                .Where(row => row!["accountId"]!.GetValue<string>() == accountId)
                .ToList();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(rows), Encoding.UTF8, "application/json")
            };
        }
    }
}

using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace transaction_aggregation_infra.DataAccess
{
    public class MockBankApiHandler : HttpMessageHandler
    {
        private readonly string _filePath;

        public MockBankApiHandler(string filePath)
        {
            _filePath = filePath;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
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

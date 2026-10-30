namespace transaction_aggregation_app.Statements
{
    // What we hand back to the customer when they ask for a statement download link.
    // Example: Url = "https://localhost:7194/api/statements/download/9F86...", ExpiresAt = 10:15.
    public class DownloadLinkDTO
    {
        public string StatementId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}

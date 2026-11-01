namespace transaction_aggregation_app.Statements
{
    public class DownloadLinkDTO
    {
        public string StatementId { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}

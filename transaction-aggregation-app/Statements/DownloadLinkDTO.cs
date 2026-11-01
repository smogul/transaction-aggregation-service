namespace transaction_aggregation_app.Statements
{
    public class DownloadLinkDTO
    {
        public string StatementId { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Url { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}

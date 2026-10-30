namespace transaction_aggregation_domain.Entities
{
    // A Statement is a PDF we made for an account, covering a range of dates.
    // Example: "all of 1000000001's transactions from 1 Sep to 30 Sep".
    //
    // We only keep facts about the file here. The PDF itself is kept in the statement file store.
    // A statement is either made by us from transactions, or made from a file the customer uploaded.
    public class Statement
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // The name of the PDF in the file store, e.g. "3f2b....pdf".
        public string FileName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

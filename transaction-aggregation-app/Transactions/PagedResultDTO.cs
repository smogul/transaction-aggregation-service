namespace transaction_aggregation_app.Transactions
{
    // A list that is cut into pages, like the pages of a book.
    // Example: 120 transactions with PageSize 20 gives 6 pages.
    // Page 2 holds transactions 21 to 40, and TotalCount is still 120.
    public class PagedResultDTO<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}

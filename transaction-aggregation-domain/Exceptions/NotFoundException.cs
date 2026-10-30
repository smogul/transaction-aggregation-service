namespace transaction_aggregation_domain.Exceptions
{
    // We throw this when someone asks for something that does not exist.
    // Example: asking for account "9999999999" when we only have 1000000001, 1000000002 and 2000000001.
    // The API turns it into a "404 Not Found" answer.
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }
}

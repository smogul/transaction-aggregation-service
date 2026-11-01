using System.Globalization;
using transaction_aggregation_domain.Entities;

namespace transaction_aggregation_tests.Fakes
{
    public static class TestData
    {
        public static Transaction Transaction(string id, string accountId, string date, string description, decimal amount, string source = "Fake")
        {
            return new Transaction
            {
                Id = id,
                AccountId = accountId,
                TransactionDate = DateTime.Parse(date, CultureInfo.InvariantCulture),
                Description = description,
                Amount = amount,
                SourceSystem = source
            };
        }
    }
}

using transaction_aggregation_app.Transactions;
using transaction_aggregation_domain.Enums;

namespace transaction_aggregation_tests.Transactions
{
    public class TransactionCategoriserTests
    {
        private readonly TransactionCategoriser _categoriser = new();

        // Each line below is its own little test: "this description should land in this category".
        [Theory]
        [InlineData("SALARY ACME HOLDINGS", Categories.Income)]
        [InlineData("CHECKERS SANDTON CITY", Categories.Food)]
        [InlineData("UBER TRIP JOHANNESBURG", Categories.Transport)]
        [InlineData("NETFLIX SUBSCRIPTION", Categories.Entertainment)]
        [InlineData("VODACOM CONTRACT", Categories.Communication)]
        [InlineData("HOME LOAN REPAYMENT", Categories.Loans)]
        [InlineData("EDGARS STORE ACCOUNT", Categories.Accounts)]
        [InlineData("OUTSURANCE CAR INSURANCE", Categories.Insurance)]
        [InlineData("DISCOVERY HEALTH MEDICAL AID", Categories.Medical)]
        [InlineData("PREPAID ELECTRICITY", Categories.Household)]
        [InlineData("TRANSFER TO SAVINGS", Categories.Transfer)]
        [InlineData("MONTHLY ACCOUNT FEE", Categories.Fees)]
        public void Categorise_KnownKeyword_ReturnsMatchingCategory(string description, Categories expected)
        {
            Assert.Equal(expected, _categoriser.Categorise(description));
        }

        [Fact]
        public void Categorise_UberEats_IsFoodNotTransport()
        {
            // "UBER EATS" also contains "UBER", so the Food rule must be checked before the Transport rule.
            Assert.Equal(Categories.Food, _categoriser.Categorise("UBER EATS ORDER"));
        }

        [Fact]
        public void Categorise_IgnoresUpperAndLowerCase()
        {
            Assert.Equal(Categories.Food, _categoriser.Categorise("checkers sandton"));
        }

        [Fact]
        public void Categorise_NoKeywordMatches_ReturnsUncategorized()
        {
            Assert.Equal(Categories.Uncategorized, _categoriser.Categorise("PAYPAL *ONLINESTORE"));
        }
    }
}

using transaction_aggregation_app.Accounts;
using transaction_aggregation_domain.Exceptions;
using transaction_aggregation_tests.Fakes;

namespace transaction_aggregation_tests.Accounts
{
    public class AccountServiceTests
    {
        private readonly AccountService _service = new(new FakeAccountRepository());

        [Fact]
        public async Task GetAccount_KnownNumber_ReturnsTheAccount()
        {
            var account = await _service.GetAccountAsync("1000000001");

            Assert.Equal("Everyday", account.AccountName);
        }

        [Fact]
        public async Task GetAccount_SpacesAroundTheNumber_AreTrimmedAway()
        {
            var account = await _service.GetAccountAsync("  1000000001 ");

            Assert.Equal("1000000001", account.Id);
        }

        // Each line is a bad account number that must be refused before we even look in the database.
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("123456789")]       // 9 digits, one too short
        [InlineData("10000000011")]     // 11 digits, one too long
        [InlineData("10000000AB")]      // letters
        [InlineData("1000 00001")]      // a space in the middle
        [InlineData("ACC-1001")]        // the old style of id
        public async Task GetAccount_NotExactly10Digits_ThrowsArgumentException(string accountId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAccountAsync(accountId));
        }

        [Fact]
        public async Task GetAccount_GoodNumberButNoSuchAccount_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAccountAsync("9999999999"));
        }
    }
}

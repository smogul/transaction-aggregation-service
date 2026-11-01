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

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("123456789")]
        [InlineData("10000000011")]
        [InlineData("10000000AB")]
        [InlineData("1000 00001")]
        [InlineData("ACC-1001")]
        public async Task GetAccount_NotExactly10Digits_ThrowsArgumentException(string accountId)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAccountAsync(accountId));
        }

        [Fact]
        public async Task GetAccount_GoodNumberButNoSuchAccount_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAccountAsync("9999999999"));
        }

        [Fact]
        public async Task GetAccount_NoNumberAtAll_ThrowsArgumentException()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetAccountAsync(null!));
        }
    }
}

using transaction_aggregation_domain.Exceptions;
using transaction_aggregation_infra.Statements;

namespace transaction_aggregation_tests.Statements
{
    public class InMemoryStatementFileStoreTests
    {
        private readonly InMemoryStatementFileStore _store = new();

        [Fact]
        public async Task SavedFile_CanBeReadBack()
        {
            await _store.SaveAsync("a.pdf", new byte[] { 1, 2, 3 });

            Assert.Equal(new byte[] { 1, 2, 3 }, await _store.ReadAsync("a.pdf"));
        }

        [Fact]
        public async Task UnknownFile_ThrowsNotFound()
        {
            await Assert.ThrowsAsync<NotFoundException>(() => _store.ReadAsync("missing.pdf"));
        }
    }
}

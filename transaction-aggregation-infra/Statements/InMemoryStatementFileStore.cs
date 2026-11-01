using System.Collections.Concurrent;
using transaction_aggregation_domain.Exceptions;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_infra.Statements
{
    public class InMemoryStatementFileStore : IStatementFileStore
    {
        private readonly ConcurrentDictionary<string, byte[]> _files = new();

        public Task SaveAsync(string fileName, byte[] content)
        {
            _files[fileName] = content;
            return Task.CompletedTask;
        }

        public Task<byte[]> ReadAsync(string fileName)
        {
            if (!_files.TryGetValue(fileName, out var content))
            {
                throw new NotFoundException($"The PDF '{fileName}' is no longer available.");
            }

            return Task.FromResult(content);
        }
    }
}

using System.Collections.Concurrent;
using transaction_aggregation_domain.Exceptions;
using transaction_aggregation_domain.Interfaces;

namespace transaction_aggregation_infra.Statements
{
    // Keeps statement PDFs in the computer's memory instead of in a folder on disk.
    // Think of it as a dictionary: the file name is the word, and the PDF is its meaning.
    // Example: "3f2b....pdf" -> the bytes of that PDF.
    //
    // Nothing is written to disk, so the PDFs disappear when the app stops.
    // A real bank would swap this class for cloud storage, and nothing else in the code would change.
    public class InMemoryStatementFileStore : IStatementFileStore
    {
        // ConcurrentDictionary is a dictionary that is safe when many requests use it at the same time.
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

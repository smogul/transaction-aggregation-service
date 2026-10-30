using System.Text;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_infra.Statements;

namespace transaction_aggregation_tests.Statements
{
    // These tests make real PDFs with QuestPDF, so they check the actual upload conversion.
    public class StatementPdfGeneratorTests
    {
        private static readonly Account Account = new() { Id = "1000000001", AccountName = "Everyday" };
        private static readonly DateTime September1 = new(2026, 9, 1);
        private static readonly DateTime September30 = new(2026, 9, 30);

        private readonly StatementPdfGenerator _generator = new();

        [Theory]
        [InlineData("september.txt")]
        [InlineData("september.csv")]
        [InlineData("september.json")]
        public void TextFile_BecomesAPdf(string fileName)
        {
            var content = Encoding.UTF8.GetBytes("Date,Description,Amount\n2026-09-01,SALARY,25000.00");

            var pdf = Upload(fileName, content);

            Assert.True(IsPdf(pdf));
        }

        [Fact]
        public void ExcelFile_BecomesAPdf()
        {
            var content = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "SeedData", "transactions.xlsx"));

            Assert.True(IsPdf(Upload("october.xlsx", content)));
        }

        [Fact]
        public void Picture_BecomesAPdf()
        {
            // The smallest possible PNG picture: one single dot.
            var content = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

            Assert.True(IsPdf(Upload("scan.PNG", content)));
        }

        [Fact]
        public void PdfFile_IsKeptExactlyAsItIs()
        {
            var content = Encoding.ASCII.GetBytes("%PDF-1.4 pretend pdf");

            Assert.Equal(content, Upload("september.pdf", content));
        }

        [Fact]
        public void PdfFile_ThatIsNotReallyAPdf_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Upload("september.pdf", Encoding.ASCII.GetBytes("hello")));
        }

        [Theory]
        [InlineData("virus.exe")]
        [InlineData("no-extension")]
        [InlineData("old-word.doc")]
        public void UnsupportedKindOfFile_ThrowsArgumentException(string fileName)
        {
            Assert.Throws<ArgumentException>(() => Upload(fileName, new byte[] { 1, 2, 3 }));
        }

        [Theory]
        [InlineData("broken.docx")]
        [InlineData("broken.xlsx")]
        [InlineData("broken.png")]
        [InlineData("binary.txt")]
        public void DamagedFile_ThrowsArgumentException(string fileName)
        {
            Assert.Throws<ArgumentException>(() => Upload(fileName, new byte[] { 0, 1, 2, 3 }));
        }

        private byte[] Upload(string fileName, byte[] content)
        {
            return _generator.CreatePdfFromDocument(Account, September1, September30, fileName, content);
        }

        // Every real PDF file starts with the letters "%PDF-".
        private static bool IsPdf(byte[] content)
        {
            return content.AsSpan().StartsWith("%PDF-"u8);
        }
    }
}

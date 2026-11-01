using System.Text;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_infra.Statements;
using transaction_aggregation_tests.Fakes;

namespace transaction_aggregation_tests.Statements
{
    public class StatementPdfGeneratorTests
    {
        private static readonly Account Account = new() { Id = "1000000001", AccountName = "Everyday" };

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

        [Fact]
        public void GeneratedStatement_WithTransactions_IsAPdf()
        {
            var transactions = new[]
            {
                TestData.Transaction("T1", "1000000001", "2026-09-01", "SALARY", 25000m),
                TestData.Transaction("T2", "1000000001", "2026-09-02", "CHECKERS", -845.30m)
            };

            Assert.True(IsPdf(_generator.CreatePdf(Account, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), transactions)));
        }

        [Fact]
        public void GeneratedStatement_WithNoTransactions_IsStillAPdf()
        {
            Assert.True(IsPdf(_generator.CreatePdf(Account, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), Array.Empty<Transaction>())));
        }

        [Fact]
        public void GeneratedStatement_WithManyTransactions_RunsOverSeveralPages()
        {
            var transactions = Enumerable.Range(1, 300)
                .Select(i => TestData.Transaction($"T{i}", "1000000001", "2026-09-01", $"PURCHASE {i}", -10m));

            Assert.True(IsPdf(_generator.CreatePdf(Account, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30), transactions)));
        }

        [Fact]
        public void WordFile_BecomesAPdf()
        {
            Assert.True(IsPdf(Upload("september.docx", TestFiles.CreateWordFile("Absa statement", "01 Sep SALARY 25000.00"))));
        }

        [Fact]
        public void EmptySpreadsheet_BecomesAPdf()
        {
            Assert.True(IsPdf(Upload("empty.xlsx", TestFiles.CreateExcelFile(columns: 0, rows: 0))));
        }

        [Theory]
        [InlineData(12)]
        [InlineData(13)]
        [InlineData(60)]
        [InlineData(250)]
        public void WideSpreadsheet_BecomesAPdf(int columns)
        {
            Assert.True(IsPdf(Upload("wide.xlsx", TestFiles.CreateExcelFile(columns, rows: 5))));
        }

        [Fact]
        public void TextWithAVeryLongLine_BecomesAPdf()
        {
            Assert.True(IsPdf(Upload("minified.json", Encoding.UTF8.GetBytes(new string('A', 5000)))));
        }

        [Fact]
        public void TextWithAByteOrderMark_BecomesAPdf()
        {
            var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes("Date,Amount\n2026-09-01,25000.00")).ToArray();

            Assert.True(IsPdf(Upload("excel-export.csv", content)));
        }

        [Fact]
        public void Utf16Text_BecomesAPdf()
        {
            var content = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("Saved from Notepad as Unicode")).ToArray();

            Assert.True(IsPdf(Upload("notepad.txt", content)));
        }

        private byte[] Upload(string fileName, byte[] content)
        {
            return _generator.CreatePdfFromDocument(Account, fileName, content);
        }

        private static bool IsPdf(byte[] content)
        {
            return content.AsSpan().StartsWith("%PDF-"u8);
        }
    }
}

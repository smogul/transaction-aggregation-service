using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using transaction_aggregation_domain.Entities;
using transaction_aggregation_domain.Interfaces;
using WordParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;

namespace transaction_aggregation_infra.Statements
{
    public class StatementPdfGenerator : IStatementPdfGenerator
    {
        private static readonly string[] SupportedFiles = { ".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg", ".txt", ".csv", ".json", ".xml", ".md" };
        private static readonly string[] PictureFiles = { ".png", ".jpg", ".jpeg" };
        private const int MaxTableColumns = 12;

        public StatementPdfGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;

            QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
        }

        public byte[] CreatePdf(Account account, DateTime fromDate, DateTime toDate, IEnumerable<Transaction> transactions)
        {
            var rows = transactions.OrderBy(t => t.TransactionDate).ToList();
            var moneyIn = rows.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var moneyOut = rows.Where(t => t.Amount < 0).Sum(t => -t.Amount);

            return DrawStatement(account, $"Period: {fromDate:dd MMM yyyy} to {toDate:dd MMM yyyy}", content => content.Column(column =>
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(70);
                        columns.RelativeColumn();
                        columns.ConstantColumn(90);
                        columns.ConstantColumn(80);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Date").Bold();
                        header.Cell().Text("Description").Bold();
                        header.Cell().Text("Category").Bold();
                        header.Cell().AlignRight().Text("Amount").Bold();
                    });

                    foreach (var transaction in rows)
                    {
                        table.Cell().Text(transaction.TransactionDate.ToString("yyyy-MM-dd"));
                        table.Cell().Text(transaction.Description);
                        table.Cell().Text(transaction.Category.ToString());
                        table.Cell().AlignRight().Text(FormatMoney(transaction.Amount));
                    }
                });

                column.Item().PaddingTop(15).Text($"Money in: {FormatMoney(moneyIn)}");
                column.Item().Text($"Money out: {FormatMoney(moneyOut)}");
                column.Item().Text($"Transactions: {rows.Count}");
            }));
        }

        public byte[] CreatePdfFromDocument(Account account, string fileName, byte[] content)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (!SupportedFiles.Contains(extension))
            {
                throw new ArgumentException($"'{fileName}' cannot be turned into a PDF. Please upload one of these kinds of file: {string.Join(", ", SupportedFiles)}.");
            }

            if (extension == ".pdf")
            {
                if (!content.AsSpan().StartsWith("%PDF-"u8))
                {
                    throw new ArgumentException($"'{fileName}' does not look like a real PDF file.");
                }

                return content;
            }

            Image? picture = null;
            var rows = new List<string[]>();

            try
            {
                if (PictureFiles.Contains(extension))
                {
                    picture = Image.FromBinaryData(content);
                }
                else
                {
                    rows = extension switch
                    {
                        ".xlsx" => ReadExcel(content),
                        ".docx" => ReadWord(content),
                        _ => ReadText(content)
                    };
                }
            }
            catch (Exception error)
            {
                throw new ArgumentException($"We could not read '{fileName}'. Please check that the file is not damaged.", error);
            }

            return DrawStatement(account, $"Uploaded file: {fileName}", page =>
            {
                if (picture != null)
                {
                    page.Image(picture).FitArea();
                }
                else if (rows.Count == 0)
                {
                    page.Text("The uploaded file was empty.");
                }
                else
                {
                    DrawRows(page, rows);
                }
            });
        }

        private static byte[] DrawStatement(Account account, string subtitle, Action<IContainer> drawContent)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(text => text.FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("Account Statement").FontSize(20).Bold();
                        column.Item().Text($"{account.AccountName} ({account.Id})");
                        column.Item().Text(subtitle);
                    });

                    page.Content().PaddingVertical(15).Element(drawContent);

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static void DrawRows(IContainer container, List<string[]> rows)
        {
            if (rows.Max(row => row.Length) > MaxTableColumns)
            {
                rows = rows.Select(row => new[] { string.Join(" | ", row) }).ToList();
            }

            var columnCount = rows.Max(row => row.Length);

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    for (var i = 0; i < columnCount; i++)
                    {
                        columns.RelativeColumn();
                    }
                });

                foreach (var row in rows)
                {
                    for (var i = 0; i < columnCount; i++)
                    {
                        table.Cell().PaddingRight(5).Text(i < row.Length ? row[i] : string.Empty);
                    }
                }
            });
        }

        private static List<string[]> ReadExcel(byte[] content)
        {
            using var workbook = new XLWorkbook(new MemoryStream(content));
            var usedCells = workbook.Worksheet(1).RangeUsed();

            if (usedCells == null)
            {
                return new List<string[]>();
            }

            return usedCells.Rows()
                .Select(row => Enumerable.Range(1, usedCells.ColumnCount()).Select(i => row.Cell(i).GetFormattedString(CultureInfo.InvariantCulture)).ToArray())
                .ToList();
        }

        private static List<string[]> ReadWord(byte[] content)
        {
            using var document = WordprocessingDocument.Open(new MemoryStream(content), false);
            var body = document.MainDocumentPart?.Document?.Body;

            if (body == null)
            {
                return new List<string[]>();
            }

            return body.Descendants<WordParagraph>().Select(paragraph => new[] { paragraph.InnerText }).ToList();
        }

        private static List<string[]> ReadText(byte[] content)
        {
            using var reader = new StreamReader(new MemoryStream(content), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = reader.ReadToEnd();

            if (text.Contains('\0'))
            {
                throw new InvalidDataException("This is not a text file.");
            }

            return text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').Select(line => new[] { line }).ToList();
        }

        private static string FormatMoney(decimal amount)
        {
            return "R " + amount.ToString("N2", CultureInfo.InvariantCulture);
        }
    }
}

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
    // Draws statement PDFs with the QuestPDF library.
    // There are two kinds of statement, and both get the same page:
    // a heading at the top, the content in the middle and page numbers at the bottom.
    //   1. CreatePdf: a table of the transactions we collected from our five data sources.
    //   2. CreatePdfFromDocument: a file the customer uploaded (Word, Excel, a picture, text...), redrawn as a PDF.
    public class StatementPdfGenerator : IStatementPdfGenerator
    {
        // Every kind of file we can turn into a PDF.
        private static readonly string[] SupportedFiles = { ".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg", ".txt", ".csv", ".json", ".xml", ".md" };
        private static readonly string[] PictureFiles = { ".png", ".jpg", ".jpeg" };

        public StatementPdfGenerator()
        {
            // QuestPDF is free for small companies and personal projects under its Community licence.
            QuestPDF.Settings.License = LicenseType.Community;

            // If an uploaded file has a letter our font cannot draw (like an emoji), show a box instead of crashing.
            QuestPDF.Settings.ThrowOnMissingTextGlyphs = false;
        }

        public byte[] CreatePdf(Account account, DateTime fromDate, DateTime toDate, IEnumerable<Transaction> transactions)
        {
            // On a statement the oldest transaction comes first, like reading a diary from the start.
            var rows = transactions.OrderBy(t => t.TransactionDate).ToList();
            var moneyIn = rows.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var moneyOut = rows.Where(t => t.Amount < 0).Sum(t => -t.Amount);

            return DrawStatement(account, fromDate, toDate, content => content.Column(column =>
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

        // Example: "september.docx" becomes a PDF with the account heading on top and the Word text underneath.
        public byte[] CreatePdfFromDocument(Account account, DateTime fromDate, DateTime toDate, string fileName, byte[] content)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            if (!SupportedFiles.Contains(extension))
            {
                throw new ArgumentException($"'{fileName}' cannot be turned into a PDF. Please upload one of these kinds of file: {string.Join(", ", SupportedFiles)}.");
            }

            // A PDF is already a PDF, so we keep it exactly as it is. Every real PDF starts with "%PDF-".
            if (extension == ".pdf")
            {
                if (!content.AsSpan().StartsWith("%PDF-"u8))
                {
                    throw new ArgumentException($"'{fileName}' does not look like a real PDF file.");
                }

                return content;
            }

            // Step 1: read the file. A picture stays a picture, everything else becomes rows of text.
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
                // Example: a file called "statement.docx" that is really a photo, or a file that got cut off.
                throw new ArgumentException($"We could not read '{fileName}'. Please check that the file is not damaged.", error);
            }

            // Step 2: draw what we read onto the statement page.
            return DrawStatement(account, fromDate, toDate, page => page.Column(column =>
            {
                column.Item().PaddingBottom(10).Text($"Uploaded file: {fileName}").Italic();

                if (picture != null)
                {
                    column.Item().Image(picture).FitArea();
                }
                else if (rows.Count == 0)
                {
                    column.Item().Text("The uploaded file was empty.");
                }
                else
                {
                    column.Item().Element(container => DrawRows(container, rows));
                }
            }));
        }

        // The page every statement shares. "drawContent" fills in the middle part.
        private static byte[] DrawStatement(Account account, DateTime fromDate, DateTime toDate, Action<IContainer> drawContent)
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
                        column.Item().Text($"Period: {fromDate:dd MMM yyyy} to {toDate:dd MMM yyyy}");
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

        // Draws rows of text as a simple table. A text file has one column, a spreadsheet has many.
        private static void DrawRows(IContainer container, List<string[]> rows)
        {
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
                        // A short row gets empty cells at the end, so every row lines up.
                        table.Cell().PaddingRight(5).Text(i < row.Length ? row[i] : string.Empty);
                    }
                }
            });
        }

        // Every filled-in row of the first sheet, cell by cell, as the spreadsheet shows it.
        // Example: [ ["Date", "Description", "Amount"], ["2026-09-01", "SALARY", "25000.00"] ]
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

        // Every paragraph of a Word document becomes one line.
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

        // Every line of a text file (.txt, .csv, .json...) becomes one line.
        private static List<string[]> ReadText(byte[] content)
        {
            var text = Encoding.UTF8.GetString(content);

            // Real text never has a "zero" character in it. If it does, someone renamed another kind of file.
            if (text.Contains('\0'))
            {
                throw new InvalidDataException("This is not a text file.");
            }

            return text.ReplaceLineEndings("\n").TrimEnd('\n').Split('\n').Select(line => new[] { line }).ToList();
        }

        // Example: -845.3 becomes "R -845.30".
        private static string FormatMoney(decimal amount)
        {
            return "R " + amount.ToString("N2", CultureInfo.InvariantCulture);
        }
    }
}

using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace transaction_aggregation_tests.Fakes
{
    public static class TestFiles
    {
        public static byte[] CreateWordFile(params string[] lines)
        {
            using var stream = new MemoryStream();

            using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
            {
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body(lines.Select(line => new Paragraph(new Run(new Text(line))))));
            }

            return stream.ToArray();
        }

        public static byte[] CreateExcelFile(int columns, int rows)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.AddWorksheet("Sheet1");

            for (var row = 1; row <= rows; row++)
            {
                for (var column = 1; column <= columns; column++)
                {
                    sheet.Cell(row, column).Value = $"R{row}C{column}";
                }
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }
    }
}

using AGUICustomSdkServer.Interfaces;
using CsvHelper;
using DocumentFormat.OpenXml.Packaging;
using ExcelDataReader;
using System.Text;
using UglyToad.PdfPig;

namespace AGUICustomSdkServer.Services
{
    public class DocumentExtraction : IDocumentExtraction
    {
        public async Task<string> ExtractText(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => await ExtractPdf(filePath),
                ".docx" => await ExtractDocx(filePath),
                ".xlsx" => await ExtractXlsx(filePath),
                ".csv" => await ExtractCsv(filePath),
                _ => throw new NotSupportedException($"Unsupported file type: {extension}")
            };
        }

        private async Task<string> ExtractPdf(string path)
        {
            var sb = new StringBuilder();
            using (var pdf = PdfDocument.Open(path))
            {
                foreach (var page in pdf.GetPages())
                {
                    sb.AppendLine($"--- [Page {page.Number}] ---");
                    sb.AppendLine(page.Text);
                }
            }
            return sb.ToString();
        }

        private async Task<string> ExtractDocx(string path)
        {
            var sb = new StringBuilder();
            using (var doc = WordprocessingDocument.Open(path, false))
            {
                var body = doc.MainDocumentPart?.Document.Body;
                if (body == null) return string.Empty;

                foreach (var paragraph in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                {
                    if (!string.IsNullOrWhiteSpace(paragraph.InnerText))
                    {
                        sb.AppendLine(paragraph.InnerText);
                    }
                }
            }
            return sb.ToString();
        }

        private async Task<string> ExtractXlsx(string path)
        {
            // Required provider registration for ExcelDataReader encoding support
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var sb = new StringBuilder();

            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            int sheetIndex = 1;
            do
            {
                sb.AppendLine($"--- [Sheet: {reader.Name ?? sheetIndex.ToString()}] ---");
                int rowIndex = 1;
                while (reader.Read())
                {
                    var rowValues = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        rowValues.Add(reader.GetValue(i)?.ToString() ?? "");
                    }
                    sb.AppendLine($"Row {rowIndex}: {string.Join(" | ", rowValues)}");
                    rowIndex++;
                }
                sheetIndex++;
            } while (reader.NextResult());

            return sb.ToString();
        }

        private async Task<string> ExtractCsv(string path)
        {
            var sb = new StringBuilder();
            using var csv = CsvReader.FromFile(path);

            csv.ReadHeader();
            if (csv.Headers.Count > 0)
            {
                sb.AppendLine($"Headers: {string.Join(" | ", csv.Headers)}");

                int rowIndex = 1;
                while (csv.Read())
                {
                    var record = new string[csv.FieldCount];
                    for (int i = 0; i < csv.FieldCount; i++)
                    {
                        record[i] = csv.GetString(i);
                    }
                    sb.AppendLine($"Line {rowIndex}: {string.Join(" | ", record)}");
                    rowIndex++;
                }
            }

            return sb.ToString();
        }
    }
}

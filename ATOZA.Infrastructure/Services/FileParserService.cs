using ATOZA.Application.Abstractions.Services;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ATOZA.Infrastructure.Services
{
    public class FileParserService : IFileParserService
    {
        /// <summary>
        /// Trích xuất văn bản từ file Word (.docx).
        /// Tự động thêm dấu * trước đáp án đúng được tô màu đỏ.
        /// </summary>
        public string ExtractFromWord(Stream stream)
        {
            var text = new StringBuilder();

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            using var wordDoc = WordprocessingDocument.Open(ms, false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            foreach (var paragraph in body.Elements<Paragraph>())
            {
                bool isFirstRun = true;
                foreach (var run in paragraph.Elements<Run>())
                {
                    string runText = run.InnerText;
                    if (string.IsNullOrEmpty(runText)) continue;

                    // Kiểm tra màu đỏ → đáp án đúng
                    string? colorVal = run.RunProperties?.Color?.Val?.Value;
                    bool isRed = colorVal != null && (
                        colorVal.Equals("FF0000", StringComparison.OrdinalIgnoreCase) ||
                        colorVal.Equals("red", StringComparison.OrdinalIgnoreCase));

                    if (isRed && isFirstRun &&
                        Regex.IsMatch(runText.Trim(), @"^[A-D][\.\)]"))
                    {
                        text.Append("*"); // Đánh dấu đáp án đúng
                    }

                    text.Append(runText);
                    isFirstRun = false;
                }
                text.AppendLine();
            }

            return text.ToString();
        }

        /// <summary>Trích xuất văn bản từ file PDF.</summary>
        public string ExtractFromPdf(Stream stream)
        {
            var text = new StringBuilder();
            using var document = PdfDocument.Open(stream);
            foreach (Page page in document.GetPages())
                text.AppendLine(page.Text);
            return text.ToString();
        }

        /// <summary>
        /// Format văn bản thô: chuẩn hóa "Câu 1", "Bài 1", "1." → "Câu N:"
        /// </summary>
        public string FormatExamText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

            var sb = new StringBuilder();
            var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            int questionCount = 1;

            foreach (var line in lines)
            {
                string clean = line.Trim();
                if (string.IsNullOrWhiteSpace(clean)) continue;

                if (Regex.IsMatch(clean, @"^(Câu|Bài|Question)\s*\d+[:.]?|^\d+\."))
                {
                    string content = Regex.Replace(
                        clean, @"^(Câu|Bài|Question)\s*\d+[:.]?|^\d+\.", "").Trim();
                    sb.AppendLine($"\nCâu {questionCount}: {content}");
                    questionCount++;
                }
                else
                {
                    sb.AppendLine(clean);
                }
            }

            return sb.ToString().Trim();
        }
    }
}

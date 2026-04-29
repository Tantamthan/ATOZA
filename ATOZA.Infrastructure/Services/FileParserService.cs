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
        private static readonly Regex QuestionRegex = new(
            @"^(?:(?:Cau|Câu|Bai|Bài|Question|Q)\s*\d+\s*[:\.\-\)]?\s*(?<content>.+)|(?<number>\d+)\s*[\.\)]\s*(?<numberContent>.+))$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex OptionRegex = new(
            @"^(?<marker>\*)?\s*(?<key>[A-Da-d])\s*[\.\):\-]\s*(?<content>.+)$",
            RegexOptions.Compiled);

        private static readonly Regex AnswerRegex = new(
            @"^(?:Dap an|Đáp án|Answer)\s*[:\-]?\s*(?<key>[A-Da-d])\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string ExtractFromWord(Stream stream)
        {
            var lines = new List<string>();

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            using var wordDoc = WordprocessingDocument.Open(ms, false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            foreach (var paragraph in body.Elements<Paragraph>())
            {
                var lineBuilder = new StringBuilder();

                foreach (var run in paragraph.Elements<Run>())
                {
                    var runText = run.InnerText;
                    if (string.IsNullOrWhiteSpace(runText))
                    {
                        continue;
                    }

                    var normalizedRun = NormalizeWhitespace(runText);
                    var colorVal = run.RunProperties?.Color?.Val?.Value;
                    var isRed = colorVal != null && (
                        colorVal.Equals("FF0000", StringComparison.OrdinalIgnoreCase) ||
                        colorVal.Equals("red", StringComparison.OrdinalIgnoreCase));

                    if (isRed &&
                        OptionRegex.IsMatch(normalizedRun) &&
                        !normalizedRun.TrimStart().StartsWith("*", StringComparison.Ordinal))
                    {
                        lineBuilder.Append('*');
                    }

                    lineBuilder.Append(normalizedRun);
                }

                var finalLine = NormalizeWhitespace(lineBuilder.ToString()).Trim();
                if (!string.IsNullOrWhiteSpace(finalLine))
                {
                    lines.Add(finalLine);
                }
            }

            return string.Join('\n', lines);
        }

        public string ExtractFromPdf(Stream stream)
        {
            var lines = new List<string>();

            using var document = PdfDocument.Open(stream);
            foreach (Page page in document.GetPages())
            {
                var pageLines = page.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in pageLines)
                {
                    var normalized = NormalizeWhitespace(line).Trim();
                    if (!string.IsNullOrWhiteSpace(normalized))
                    {
                        lines.Add(normalized);
                    }
                }
            }

            return string.Join('\n', lines);
        }

        public string FormatExamText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return string.Empty;
            }

            var questions = ParseQuestions(rawText);
            if (questions.Count == 0)
            {
                return NormalizeFallbackText(rawText);
            }

            var sb = new StringBuilder();
            for (var i = 0; i < questions.Count; i++)
            {
                var question = questions[i];
                sb.AppendLine($"Câu {i + 1}: {question.Text.Trim()}");

                foreach (var option in question.Options)
                {
                    var marker = question.CorrectKey == option.Key ? "*" : string.Empty;
                    sb.AppendLine($"{marker}{option.Key}. {option.Content.Trim()}");
                }

                if (i < questions.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            return sb.ToString().Trim();
        }

        private static List<ParsedQuestion> ParseQuestions(string rawText)
        {
            var normalizedText = rawText
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace('\u00A0', ' ');

            var rawLines = normalizedText
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => NormalizeWhitespace(line).Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            var questions = new List<ParsedQuestion>();
            ParsedQuestion? currentQuestion = null;
            ParsedOption? currentOption = null;

            foreach (var line in rawLines)
            {
                var questionMatch = QuestionRegex.Match(line);
                if (questionMatch.Success)
                {
                    currentQuestion = new ParsedQuestion
                    {
                        Text = GetQuestionContent(questionMatch)
                    };
                    currentOption = null;
                    questions.Add(currentQuestion);
                    continue;
                }

                if (currentQuestion == null)
                {
                    continue;
                }

                var optionMatch = OptionRegex.Match(line);
                if (optionMatch.Success)
                {
                    var key = optionMatch.Groups["key"].Value.ToUpperInvariant();
                    currentOption = new ParsedOption
                    {
                        Key = key,
                        Content = optionMatch.Groups["content"].Value.Trim()
                    };

                    if (optionMatch.Groups["marker"].Success)
                    {
                        currentQuestion.CorrectKey = key;
                    }

                    currentQuestion.Options.Add(currentOption);
                    continue;
                }

                var answerMatch = AnswerRegex.Match(line);
                if (answerMatch.Success)
                {
                    currentQuestion.CorrectKey = answerMatch.Groups["key"].Value.ToUpperInvariant();
                    currentOption = null;
                    continue;
                }

                if (currentOption != null)
                {
                    currentOption.Content = $"{currentOption.Content} {line}".Trim();
                }
                else
                {
                    currentQuestion.Text = $"{currentQuestion.Text} {line}".Trim();
                }
            }

            return questions
                .Where(q => !string.IsNullOrWhiteSpace(q.Text) && q.Options.Count > 0)
                .ToList();
        }

        private static string GetQuestionContent(Match match)
        {
            if (match.Groups["content"].Success && !string.IsNullOrWhiteSpace(match.Groups["content"].Value))
            {
                return match.Groups["content"].Value.Trim();
            }

            if (match.Groups["numberContent"].Success)
            {
                return match.Groups["numberContent"].Value.Trim();
            }

            return string.Empty;
        }

        private static string NormalizeFallbackText(string rawText)
        {
            var lines = rawText
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => NormalizeWhitespace(line).Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line));

            return string.Join(Environment.NewLine, lines);
        }

        private static string NormalizeWhitespace(string input)
        {
            var cleaned = input
                .Replace('\t', ' ')
                .Replace("•", "-")
                .Replace("–", "-")
                .Replace("—", "-");

            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            return cleaned.Trim();
        }

        private sealed class ParsedQuestion
        {
            public string Text { get; set; } = string.Empty;

            public string? CorrectKey { get; set; }

            public List<ParsedOption> Options { get; } = new();
        }

        private sealed class ParsedOption
        {
            public string Key { get; set; } = string.Empty;

            public string Content { get; set; } = string.Empty;
        }
    }
}

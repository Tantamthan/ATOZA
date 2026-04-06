namespace ATOZA.Application.Abstractions.Services
{
    public interface IFileParserService
    {
        string ExtractFromWord(Stream stream);
        string ExtractFromPdf(Stream stream);
        string FormatExamText(string rawText);
    }
}

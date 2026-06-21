using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;

namespace IncidentApp.Services.KnowledgeBase
{
    public class PdfTextExtractionService : ITextExtractionService
    {
        public string SupportedExtension => ".pdf";

        public bool CanHandle(string fileName)
        {
            return Path.GetExtension(fileName).Equals(SupportedExtension, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
        {
            try
            {
                var text = new StringBuilder();

                using var pdfReader = new PdfReader(fileStream);
                using var pdfDocument = new PdfDocument(pdfReader);

                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i), strategy);
                    text.AppendLine(pageText);
                }

                return await Task.FromResult(text.ToString());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}", ex);
            }
        }
    }
}

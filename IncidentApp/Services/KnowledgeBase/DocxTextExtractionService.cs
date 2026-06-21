using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace IncidentApp.Services.KnowledgeBase
{
    public class DocxTextExtractionService : ITextExtractionService
    {
        public string SupportedExtension => ".docx";

        public bool CanHandle(string fileName)
        {
            return Path.GetExtension(fileName).Equals(SupportedExtension, StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
        {
            try
            {
                using var wordDocument = WordprocessingDocument.Open(fileStream, false);
                var body = wordDocument.MainDocumentPart?.Document.Body;
                
                if (body == null)
                    return string.Empty;

                var text = new StringBuilder();
                
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    foreach (var run in paragraph.Elements<Run>())
                    {
                        foreach (var textElement in run.Elements<Text>())
                        {
                            text.Append(textElement.Text);
                        }
                    }
                    text.AppendLine();
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract text from DOCX: {ex.Message}", ex);
            }
        }
    }
}

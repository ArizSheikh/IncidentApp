using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
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
                using var pdfDocument = PdfReader.Open(fileStream);
                var text = new StringBuilder();

                foreach (PdfPage page in pdfDocument.Pages)
                {
                    // Extract text from PDF page
                    for (int i = 0; i < page.Contents.Elements.Count; i++)
                    {
                        var element = page.Contents.Elements[i];
                        if (element is PdfDictionary dictionary)
                        {
                            ExtractTextFromDictionary(dictionary, text);
                        }
                    }
                }

                return text.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}", ex);
            }
        }

        private void ExtractTextFromDictionary(PdfDictionary dictionary, StringBuilder text)
        {
            try
            {
                foreach (var key in dictionary.Elements.Keys)
                {
                    var element = dictionary.Elements[key];
                    if (element is PdfString pdfString)
                    {
                        text.Append(pdfString.Value);
                        text.Append(" ");
                    }
                    else if (element is PdfDictionary nestedDictionary)
                    {
                        ExtractTextFromDictionary(nestedDictionary, text);
                    }
                    else if (element is PdfArray array)
                    {
                        ExtractTextFromArray(array, text);
                    }
                }
            }
            catch
            {
                // Skip problematic elements
            }
        }

        private void ExtractTextFromArray(PdfArray array, StringBuilder text)
        {
            try
            {
                foreach (var element in array.Elements)
                {
                    if (element is PdfString pdfString)
                    {
                        text.Append(pdfString.Value);
                        text.Append(" ");
                    }
                    else if (element is PdfDictionary dictionary)
                    {
                        ExtractTextFromDictionary(dictionary, text);
                    }
                }
            }
            catch
            {
                // Skip problematic elements
            }
        }
    }
}

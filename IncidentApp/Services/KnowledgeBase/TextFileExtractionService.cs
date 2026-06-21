using System.Text;

namespace IncidentApp.Services.KnowledgeBase
{
    public class TextFileExtractionService : ITextExtractionService
    {
        private readonly string[] _supportedExtensions = { ".txt", ".md", ".markdown" };

        public string SupportedExtension => ".txt";

        public bool CanHandle(string fileName)
        {
            var cleanName = fileName.TrimEnd('\\', '/');
            var extension = Path.GetExtension(cleanName);
            return _supportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<string> ExtractTextAsync(Stream fileStream, string fileName)
        {
            try
            {
                using var reader = new StreamReader(fileStream);
                var text = await reader.ReadToEndAsync();
                return text;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to extract text from text file: {ex.Message}", ex);
            }
        }
    }
}

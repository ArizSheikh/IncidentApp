namespace IncidentApp.Services.KnowledgeBase
{
    public interface ITextExtractionService
    {
        string SupportedExtension { get; }
        Task<string> ExtractTextAsync(Stream fileStream, string fileName);
        bool CanHandle(string fileName);
    }
}

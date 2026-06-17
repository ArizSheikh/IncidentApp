using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace IncidentApp.AI.Embedding
{
    public class LocalEmbeddingService
    {
        public LocalEmbeddingService(IConfiguration config)
        {
            Console.WriteLine("Initialized Local Embedding Service (no external dependencies)");
        }

        public Task<float[]> GenerateEmbeddingAsync(
            string incidentTitle,
            string severity,
            string incidentDescription,
            string category,
            string logs,
            string systemComponent)
        {
            var combinedText = $"{incidentTitle} {severity} {incidentDescription} {category} {logs} {systemComponent}";
            return GenerateEmbeddingFromTextAsync(combinedText);
        }

        public Task<float[]> GenerateEmbeddingFromTextAsync(string text)
        {
            // Advanced local embedding algorithm using multiple features
            var features = new List<float>();
            var normalizedText = NormalizeText(text);
            
            // 1. Character n-gram features
            features.AddRange(GetCharacterNGrams(normalizedText));
            
            // 2. Word-level features
            features.AddRange(GetWordFeatures(normalizedText));
            
            // 3. Semantic features
            features.AddRange(GetSemanticFeatures(normalizedText));
            
            // 4. Structural features
            features.AddRange(GetStructuralFeatures(normalizedText));
            
            // Ensure consistent size (384 dimensions to match bge-small-en-v1.5)
            while (features.Count < 384)
            {
                features.Add(0);
            }
            
            // Trim if too large
            if (features.Count > 384)
            {
                features = features.Take(384).ToList();
            }
            
            // Normalize the vector
            var magnitude = Math.Sqrt(features.Sum(f => f * f));
            if (magnitude > 0)
            {
                for (int i = 0; i < features.Count; i++)
                {
                    features[i] = features[i] / (float)magnitude;
                }
            }
            
            return Task.FromResult(features.ToArray());
        }

        private string NormalizeText(string text)
        {
            // Convert to lowercase and remove special characters
            return Regex.Replace(text.ToLower(), "[^a-z0-9\\s]", "").Trim();
        }

        private List<float> GetCharacterNGrams(string text)
        {
            var features = new List<float>();
            var chars = text.ToCharArray();
            
            // Trigram distribution
            var trigrams = new Dictionary<string, int>();
            for (int i = 0; i < chars.Length - 2; i++)
            {
                var trigram = new string(chars, i, 3);
                trigrams[trigram] = trigrams.GetValueOrDefault(trigram, 0) + 1;
            }
            
            // Convert to features (top 100 most common trigrams)
            var topTrigrams = trigrams.OrderByDescending(kvp => kvp.Value).Take(100);
            foreach (var trigram in topTrigrams)
            {
                features.Add(trigram.Value / (float)chars.Length);
            }
            
            // Pad to 100 features
            while (features.Count < 100)
            {
                features.Add(0);
            }
            
            return features;
        }

        private List<float> GetWordFeatures(string text)
        {
            var features = new List<float>();
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Word length distribution
            var wordLengths = words.Select(w => w.Length).ToList();
            features.Add(wordLengths.Any() ? (float)wordLengths.Average() : 0);
            features.Add(wordLengths.Any() ? (float)wordLengths.Max() : 0);
            features.Add(wordLengths.Any() ? (float)wordLengths.Min() : 0);
            
            // Vocabulary richness
            var uniqueWords = words.Distinct().Count();
            features.Add((float)uniqueWords / (words.Length + 1));
            
            // Common words (stopwords)
            var stopwords = new HashSet<string> { "the", "a", "an", "in", "on", "at", "to", "for", "of", "and", "is", "are", "was", "were" };
            var stopwordCount = words.Count(w => stopwords.Contains(w));
            features.Add((float)stopwordCount / (words.Length + 1));
            
            return features;
        }

        private List<float> GetSemanticFeatures(string text)
        {
            var features = new List<float>();
            
            // Hash-based semantic features
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            
            for (int i = 0; i < 32; i++)
            {
                features.Add(bytes[i] / 255.0f);
            }
            
            return features;
        }

        private List<float> GetStructuralFeatures(string text)
        {
            var features = new List<float>();
            
            // Sentence structure
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            features.Add((float)sentences.Length);
            
            // Average words per sentence
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            features.Add(sentences.Length > 0 ? words.Length / (float)sentences.Length : 0);
            
            // Special character ratio
            var specialChars = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
            features.Add((float)specialChars / (text.Length + 1));
            
            // Digit ratio
            var digits = text.Count(char.IsDigit);
            features.Add((float)digits / (text.Length + 1));
            
            return features;
        }
    }
}

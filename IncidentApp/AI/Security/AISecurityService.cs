using System.Text.RegularExpressions;

namespace IncidentApp.AI.Security
{
    public class PromptInjectionDetector
    {
        private readonly List<string> _injectionPatterns = new()
        {
            @"ignore\s+(all\s+)?(previous|above)?\s*instructions",
            @"forget\s+(all\s+)?(previous|above)?\s*instructions",
            @"override\s+(all\s+)?(previous|above)?\s*instructions",
            @"bypass\s+(all\s+)?(previous|above)?\s*instructions",
            @"system\s*:\s*",
            @"developer\s*:\s*",
            @"admin\s*:\s*",
            @"sudo",
            @"execute\s+command",
            @"run\s+command",
            @"shell\s*:",
            @"bash\s*:",
            @"powershell\s*:",
            @"cmd\s*:",
            @"script\s*:",
            @"javascript\s*:",
            @"eval\s*\(",
            @"exec\s*\(",
            @"__import__",
            @"import\s+os",
            @"subprocess",
            @"system\s*\(",
            @"passthru",
            @"shell_exec",
            @"<script",
            @"javascript:",
            @"onerror\s*=",
            @"onload\s*=",
            @"document\.write",
            @"innerHTML",
            @"fromCharCode",
            @"alert\s*\(",
            @"prompt\s*\(",
            @"confirm\s*\("
        };

        public (bool IsInjection, string Pattern) Detect(string input)
        {
            if (string.IsNullOrEmpty(input))
                return (false, string.Empty);

            var lowerInput = input.ToLower();

            foreach (var pattern in _injectionPatterns)
            {
                if (Regex.IsMatch(lowerInput, pattern, RegexOptions.IgnoreCase))
                {
                    return (true, pattern);
                }
            }

            return (false, string.Empty);
        }

        public bool IsSafe(string input)
        {
            var (isInjection, _) = Detect(input);
            return !isInjection;
        }
    }

    public class PIIRedactionService
    {
        private readonly List<PIIPattern> _piiPatterns = new()
        {
            new PIIPattern(@"\b\d{3}-\d{2}-\d{4}\b", "SSN"), // SSN: 123-45-6789
            new PIIPattern(@"\b\d{4}[- ]?\d{4}[- ]?\d{4}[- ]?\d{4}\b", "CreditCard"), // Credit Card
            new PIIPattern(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", "Email"), // Email
            new PIIPattern(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", "Phone"), // Phone: 123-456-7890
            new PIIPattern(@"\b\d{1,5}\s+\w+\s+\w+\s+\w+\s+\w+\s+\w+\s+\d{5}\b", "Address"), // Address
            new PIIPattern(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", "IP"), // IP Address
            new PIIPattern(@"\b[A-Z]{2}\d{2}[A-Z]{4}\d{4}\b", "Passport"), // Passport
            new PIIPattern(@"\b\d{9}\b", "SSN9"), // SSN 9 digits
            new PIIPattern(@"\b\d{10,12}\b", "Phone10"), // Phone 10-12 digits
        };

        public string Redact(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = input;

            foreach (var pattern in _piiPatterns)
            {
                result = Regex.Replace(result, pattern.Pattern, match => $"[REDACTED_{pattern.Label}]");
            }

            return result;
        }

        public (string RedactedText, int Count) RedactWithCount(string input)
        {
            if (string.IsNullOrEmpty(input))
                return (input, 0);

            var result = input;
            var count = 0;

            foreach (var pattern in _piiPatterns)
            {
                var matches = Regex.Matches(result, pattern.Pattern);
                count += matches.Count;
                result = Regex.Replace(result, pattern.Pattern, match => $"[REDACTED_{pattern.Label}]");
            }

            return (result, count);
        }
    }

    public class AIInputSanitizer
    {
        private readonly PromptInjectionDetector _injectionDetector;
        private readonly PIIRedactionService _piiRedactionService;

        public AIInputSanitizer()
        {
            _injectionDetector = new PromptInjectionDetector();
            _piiRedactionService = new PIIRedactionService();
        }

        public SanitizationResult Sanitize(string input, bool redactPII = true)
        {
            var result = new SanitizationResult
            {
                OriginalInput = input,
                SanitizedInput = input
            };

            // Check for prompt injection
            var (isInjection, pattern) = _injectionDetector.Detect(input);
            result.HasPromptInjection = isInjection;
            result.InjectionPattern = pattern;

            if (isInjection)
            {
                result.IsSafe = false;
                return result;
            }

            // Remove potentially dangerous characters
            result.SanitizedInput = RemoveDangerousCharacters(input);

            // Redact PII if requested
            if (redactPII)
            {
                var (redactedText, piiCount) = _piiRedactionService.RedactWithCount(result.SanitizedInput);
                result.SanitizedInput = redactedText;
                result.PIIRedactedCount = piiCount;
            }

            // Normalize whitespace
            result.SanitizedInput = NormalizeWhitespace(result.SanitizedInput);

            result.IsSafe = true;
            return result;
        }

        private string RemoveDangerousCharacters(string input)
        {
            // Remove null bytes and other control characters except common ones
            return Regex.Replace(input, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);
        }

        private string NormalizeWhitespace(string input)
        {
            // Normalize multiple whitespace to single space
            return Regex.Replace(input, @"\s+", " ").Trim();
        }
    }

    public class PIIPattern
    {
        public PIIPattern(string pattern, string label)
        {
            Pattern = pattern;
            Label = label;
        }

        public string Pattern { get; set; }
        public string Label { get; set; }
    }

    public class SanitizationResult
    {
        public string OriginalInput { get; set; } = string.Empty;
        public string SanitizedInput { get; set; } = string.Empty;
        public bool IsSafe { get; set; }
        public bool HasPromptInjection { get; set; }
        public string? InjectionPattern { get; set; }
        public int PIIRedactedCount { get; set; }
    }
}

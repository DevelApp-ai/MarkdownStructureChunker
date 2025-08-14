using System.Text.RegularExpressions;

namespace MarkdownStructureChunker.Core.Utilities;

/// <summary>
/// Utility class for validating and sanitizing custom keywords.
/// </summary>
public static class KeywordValidator
{
    /// <summary>
    /// Validates a collection of custom keywords.
    /// </summary>
    /// <param name="keywords">The keywords to validate</param>
    /// <returns>A list of validation errors, empty if all keywords are valid</returns>
    public static IReadOnlyList<string> ValidateKeywords(IEnumerable<string> keywords)
    {
        var errors = new List<string>();
        
        if (keywords == null)
        {
            errors.Add("Keywords collection cannot be null");
            return errors;
        }

        var keywordList = keywords.ToList();
        
        for (int i = 0; i < keywordList.Count; i++)
        {
            var keyword = keywordList[i];
            
            if (string.IsNullOrWhiteSpace(keyword))
            {
                errors.Add($"Keyword at index {i} is null or empty");
                continue;
            }

            if (keyword.Length > 100)
            {
                errors.Add($"Keyword '{keyword}' exceeds maximum length of 100 characters");
            }

            if (keyword.Contains('\n') || keyword.Contains('\r'))
            {
                errors.Add($"Keyword '{keyword}' contains line breaks");
            }

            if (keyword.Trim() != keyword)
            {
                errors.Add($"Keyword '{keyword}' has leading or trailing whitespace");
            }
        }

        // Check for duplicates (case-insensitive)
        var duplicates = keywordList
            .GroupBy(k => k?.ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .Where(k => !string.IsNullOrEmpty(k));

        foreach (var duplicate in duplicates)
        {
            errors.Add($"Duplicate keyword found: '{duplicate}'");
        }

        return errors;
    }

    /// <summary>
    /// Validates section keyword mappings.
    /// </summary>
    /// <param name="mappings">The section keyword mappings to validate</param>
    /// <returns>A list of validation errors, empty if all mappings are valid</returns>
    public static IReadOnlyList<string> ValidateSectionMappings(IReadOnlyDictionary<string, IReadOnlyList<string>> mappings)
    {
        var errors = new List<string>();
        
        if (mappings == null)
        {
            errors.Add("Section mappings cannot be null");
            return errors;
        }

        foreach (var mapping in mappings)
        {
            // Validate regex pattern
            if (string.IsNullOrWhiteSpace(mapping.Key))
            {
                errors.Add("Section mapping pattern cannot be null or empty");
                continue;
            }

            try
            {
                var regex = new Regex(mapping.Key, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                
                // Test the regex with a simple string to ensure it works
                _ = regex.IsMatch("test");
            }
            catch (ArgumentException ex)
            {
                errors.Add($"Invalid regex pattern '{mapping.Key}': {ex.Message}");
                continue;
            }
            catch (RegexMatchTimeoutException)
            {
                errors.Add($"Regex pattern '{mapping.Key}' is too complex and may cause timeouts");
                continue;
            }

            // Validate keywords for this pattern
            var keywordErrors = ValidateKeywords(mapping.Value);
            foreach (var error in keywordErrors)
            {
                errors.Add($"Pattern '{mapping.Key}': {error}");
            }
        }

        return errors;
    }

    /// <summary>
    /// Sanitizes a keyword by trimming whitespace and normalizing case.
    /// </summary>
    /// <param name="keyword">The keyword to sanitize</param>
    /// <returns>The sanitized keyword, or null if the input was invalid</returns>
    public static string? SanitizeKeyword(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return null;

        var sanitized = keyword.Trim();
        
        // Remove multiple consecutive spaces
        sanitized = Regex.Replace(sanitized, @"\s+", " ");
        
        // Convert to lowercase for consistency
        sanitized = sanitized.ToLowerInvariant();
        
        return sanitized.Length > 0 ? sanitized : null;
    }

    /// <summary>
    /// Sanitizes a collection of keywords, removing invalid ones and normalizing valid ones.
    /// </summary>
    /// <param name="keywords">The keywords to sanitize</param>
    /// <returns>A list of sanitized, valid keywords</returns>
    public static IReadOnlyList<string> SanitizeKeywords(IEnumerable<string> keywords)
    {
        if (keywords == null)
            return new List<string>();

        return keywords
            .Select(SanitizeKeyword)
            .Where(k => !string.IsNullOrEmpty(k))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Creates a safe regex pattern with timeout protection.
    /// </summary>
    /// <param name="pattern">The regex pattern</param>
    /// <param name="timeout">The timeout for regex operations</param>
    /// <returns>A compiled regex with timeout protection</returns>
    public static Regex CreateSafeRegex(string pattern, TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? TimeSpan.FromSeconds(1);
        return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, actualTimeout);
    }
}


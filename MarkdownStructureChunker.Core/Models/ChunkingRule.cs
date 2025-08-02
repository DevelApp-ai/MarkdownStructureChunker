using System.Text.RegularExpressions;

namespace MarkdownStructureChunker.Core.Models;

/// <summary>
/// Represents a rule for identifying and parsing document structure patterns.
/// </summary>
public class ChunkingRule
{
    public string Type { get; }
    public Regex Pattern { get; }
    public int? FixedLevel { get; }
    public int Priority { get; }

    /// <summary>
    /// Creates a chunking rule with a fixed level.
    /// </summary>
    /// <param name="type">The type identifier for this rule (e.g., "MarkdownH1", "Legal")</param>
    /// <param name="pattern">Regular expression pattern to match</param>
    /// <param name="level">Fixed hierarchical level for matches</param>
    /// <param name="priority">Priority order (lower numbers = higher priority)</param>
    public ChunkingRule(string type, string pattern, int level, int priority = 0)
    {
        Type = type;
        Pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Multiline);
        FixedLevel = level;
        Priority = priority;
    }

    /// <summary>
    /// Creates a chunking rule with dynamic level calculation.
    /// </summary>
    /// <param name="type">The type identifier for this rule (e.g., "Numeric")</param>
    /// <param name="pattern">Regular expression pattern to match</param>
    /// <param name="priority">Priority order (lower numbers = higher priority)</param>
    public ChunkingRule(string type, string pattern, int priority = 0)
    {
        Type = type;
        Pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Multiline);
        FixedLevel = null;
        Priority = priority;
    }

    /// <summary>
    /// Attempts to match the pattern against a line and extract heading information.
    /// </summary>
    /// <param name="line">The line to test</param>
    /// <returns>Match result with extracted information, or null if no match</returns>
    public ChunkingMatch? TryMatch(string line)
    {
        var match = Pattern.Match(line);
        if (!match.Success)
            return null;

        var level = FixedLevel ?? CalculateDynamicLevel(match);
        var rawTitle = match.Value.Trim();
        var cleanTitle = ExtractCleanTitle(match);

        return new ChunkingMatch(Type, level, rawTitle, cleanTitle);
    }

    private int CalculateDynamicLevel(Match match)
    {
        // For numeric patterns like "1.2.3", count the dots + 1
        if (Type == "Numeric" && match.Groups.Count > 1)
        {
            var numericPart = match.Groups[1].Value;
            return numericPart.Count(c => c == '.') + 1;
        }

        // Default to level 1 for other dynamic patterns
        return 1;
    }

    private string ExtractCleanTitle(Match match)
    {
        // Extract the title part from the match
        // For most patterns, the title is in the last capture group
        if (match.Groups.Count > 1)
        {
            return match.Groups[match.Groups.Count - 1].Value.Trim();
        }

        return match.Value.Trim();
    }
}

/// <summary>
/// Represents the result of a successful pattern match.
/// </summary>
public record ChunkingMatch(string Type, int Level, string RawTitle, string CleanTitle);


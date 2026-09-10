using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MarkdownStructureChunker.Core;
using MarkdownStructureChunker.Core.Configuration;
using MarkdownStructureChunker.Core.Extractors;
using MarkdownStructureChunker.Core.Models;
using MarkdownStructureChunker.Core.Strategies;

namespace MarkdownStructureChunker.Benchmarks;

/// <summary>
/// Performance baselines for the hot paths exercised by the structure-first and
/// pattern-based chunking pipelines. Run with: dotnet run -c Release --project Benchmarks
/// These baselines protect the hot-path optimizations (regex caching, O(1) parent/child
/// and index lookups, delimiter-aware offsets) from regressing silently.
/// </summary>
[MemoryDiagnoser]
public class ChunkingBenchmarks
{
    private string _small = "";
    private string _medium = "";
    private string _large = "";
    private PatternBasedStrategy _strategy = null!;
    private PatternBasedStrategy _strategyWithOffsets = null!;
    private StructureChunker _chunker = null!;

    [GlobalSetup]
    public void Setup()
    {
        _small = BuildDocument(sections: 10, paragraphsPerSection: 2);
        _medium = BuildDocument(sections: 100, paragraphsPerSection: 3);
        _large = BuildDocument(sections: 500, paragraphsPerSection: 4);

        var rules = PatternBasedStrategy.CreateDefaultRules();
        _strategy = new PatternBasedStrategy(rules);
        _strategyWithOffsets = new PatternBasedStrategy(rules, new ChunkerConfiguration
        {
            CalculateOffsets = true,
            PreserveOriginalMarkdown = true
        });
        _chunker = new StructureChunker(new ChunkerConfiguration
        {
            ExtractKeywords = true,
            MaxKeywordsPerChunk = 10
        });
    }

    [Params("Small", "Medium", "Large")]
    public string Size { get; set; } = "Medium";

    private string Document => Size switch
    {
        "Small" => _small,
        "Large" => _large,
        _ => _medium
    };

    [Benchmark(Description = "PatternBasedStrategy.ProcessText (no offsets)")]
    public IReadOnlyList<ChunkNode> ProcessText_NoOffsets() =>
        _strategy.ProcessText(Document, "bench");

    [Benchmark(Description = "PatternBasedStrategy.ProcessText (offsets + original markdown)")]
    public IReadOnlyList<ChunkNode> ProcessText_WithOffsets() =>
        _strategyWithOffsets.ProcessText(Document, "bench");

    [Benchmark(Description = "StructureChunker.ProcessAsync (full pipeline + keywords)")]
    public Task<DocumentGraph> ProcessAsync_FullPipeline() =>
        _chunker.ProcessAsync(Document, "bench");

    private static string BuildDocument(int sections, int paragraphsPerSection)
    {
        var sb = new System.Text.StringBuilder();
        for (int s = 1; s <= sections; s++)
        {
            sb.Append('#').Append(' ').Append("Section ").AppendLine(s.ToString());
            for (int p = 0; p < paragraphsPerSection; p++)
            {
                sb.Append("This is paragraph ").Append(p).Append(" of section ").Append(s)
                  .AppendLine(" with some machine learning and document structure vocabulary.");
            }
            if (s % 5 == 0)
            {
                sb.Append("## Subsection ").Append(s).AppendLine(".1")
                  .AppendLine("Nested content discussing data protection compliance requirements.");
            }
        }
        return sb.ToString();
    }
}

public class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

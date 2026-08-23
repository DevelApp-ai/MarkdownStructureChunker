namespace MarkdownStructureChunker.Core.Interfaces;

/// <summary>
/// A minimal logging abstraction used by the library to report diagnostic,
/// warning, and error messages without taking a hard dependency on a logging
/// framework or writing directly to <see cref="System.Console"/>. Hosts (for
/// example an ASP.NET Core app with its own <c>ILogger</c>) can provide an
/// adapter implementation; in its absence the <see cref="NullChunkerLogger"/>
/// discards all output.
/// </summary>
public interface IChunkerLogger
{
    /// <summary>Logs an informational message.</summary>
    /// <param name="message">The formatted message.</param>
    void LogInformation(string message);

    /// <summary>Logs a warning message.</summary>
    /// <param name="message">The formatted message.</param>
    void LogWarning(string message);

    /// <summary>Logs an error message.</summary>
    /// <param name="message">The formatted message.</param>
    void LogError(string message);
}

/// <summary>
/// Default no-op implementation. Discards all log output so library
/// consumers see no console noise unless they inject their own logger.
/// </summary>
public sealed class NullChunkerLogger : IChunkerLogger
{
    /// <summary>Gets the shared no-op instance.</summary>
    public static NullChunkerLogger Instance { get; } = new NullChunkerLogger();

    /// <inheritdoc />
    public void LogInformation(string message) { }

    /// <inheritdoc />
    public void LogWarning(string message) { }

    /// <inheritdoc />
    public void LogError(string message) { }
}

namespace MarkdownStructureChunker.Core.Interfaces;

/// <summary>
/// A component for converting text chunks into vector embeddings.
/// </summary>
public interface ILocalVectorizer
{
    /// <summary>
    /// Converts text into vector embeddings using a local model.
    /// </summary>
    /// <param name="text">The text to vectorize</param>
    /// <param name="isQuery">Whether this is a query (true) or passage (false)</param>
    /// <returns>Vector embedding as an array of floats</returns>
    Task<float[]> VectorizeAsync(string text, bool isQuery = false);

    /// <summary>
    /// Gets the dimension of the vector embeddings produced by this vectorizer.
    /// </summary>
    int VectorDimension { get; }
}


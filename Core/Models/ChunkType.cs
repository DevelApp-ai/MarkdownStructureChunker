namespace MarkdownStructureChunker.Core.Models;

/// <summary>
/// Enumeration that classifies different types of document chunks.
/// </summary>
public enum ChunkType
{
    /// <summary>
    /// Regular content chunk containing body text.
    /// </summary>
    Content,

    /// <summary>
    /// Header or heading chunk containing title information.
    /// </summary>
    Header,

    /// <summary>
    /// Section chunk representing a major document section.
    /// </summary>
    Section,

    /// <summary>
    /// Appendix chunk containing supplementary information.
    /// </summary>
    Appendix,

    /// <summary>
    /// Legal document chunk with legal formatting patterns.
    /// </summary>
    Legal,

    /// <summary>
    /// Numeric outline chunk with numbered sections.
    /// </summary>
    Numeric,

    /// <summary>
    /// List item chunk containing bulleted or numbered list content.
    /// </summary>
    ListItem,

    /// <summary>
    /// Code block chunk containing programming code or technical content.
    /// </summary>
    CodeBlock,

    /// <summary>
    /// Table chunk containing tabular data.
    /// </summary>
    Table,

    /// <summary>
    /// Quote or blockquote chunk containing quoted text.
    /// </summary>
    Quote,

    /// <summary>
    /// Mixed content chunk containing multiple content types.
    /// </summary>
    Mixed
}


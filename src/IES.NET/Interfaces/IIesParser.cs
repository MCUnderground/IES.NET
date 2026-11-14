using IES.NET.Models;

namespace IES.NET.Interfaces;

/// <summary>
/// Parse IES LM-63 format into <see cref="IesData"/>.
/// Implementations should be thread-safe.
/// </summary>
public interface IIesParser
{
    /// <summary>
    /// Parse a file from disk. Throws <see cref="IesParsingException"/> on parse error.
    /// </summary>
    IesData Parse(string path);
}

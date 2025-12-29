using System;
using System.Collections.Generic;

namespace Neatoo;

/// <summary>
/// Stores diagnostic information in an equatable format suitable for incremental generator caching.
/// Location objects are not equatable, so we store the file path and line/column information instead.
/// </summary>
internal sealed record DiagnosticInfo : IEquatable<DiagnosticInfo>
{
    /// <summary>
    /// The diagnostic ID (e.g., "NF0101").
    /// </summary>
    public string DiagnosticId { get; }

    /// <summary>
    /// The file path where the diagnostic occurred.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The starting line number (0-indexed).
    /// </summary>
    public int StartLine { get; }

    /// <summary>
    /// The starting column number (0-indexed).
    /// </summary>
    public int StartColumn { get; }

    /// <summary>
    /// The ending line number (0-indexed).
    /// </summary>
    public int EndLine { get; }

    /// <summary>
    /// The ending column number (0-indexed).
    /// </summary>
    public int EndColumn { get; }

    /// <summary>
    /// The text span start position.
    /// </summary>
    public int TextSpanStart { get; }

    /// <summary>
    /// The text span length.
    /// </summary>
    public int TextSpanLength { get; }

    /// <summary>
    /// The message format arguments.
    /// </summary>
    public EquatableArray<string> MessageArgs { get; }

    public DiagnosticInfo(
        string diagnosticId,
        string filePath,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn,
        int textSpanStart,
        int textSpanLength,
        params string[] messageArgs)
    {
        DiagnosticId = diagnosticId;
        FilePath = filePath;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
        TextSpanStart = textSpanStart;
        TextSpanLength = textSpanLength;
        MessageArgs = new EquatableArray<string>(messageArgs);
    }

    public bool Equals(DiagnosticInfo? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return DiagnosticId == other.DiagnosticId
            && FilePath == other.FilePath
            && StartLine == other.StartLine
            && StartColumn == other.StartColumn
            && EndLine == other.EndLine
            && EndColumn == other.EndColumn
            && TextSpanStart == other.TextSpanStart
            && TextSpanLength == other.TextSpanLength
            && MessageArgs.Equals(other.MessageArgs);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(DiagnosticId);
        hash.Add(FilePath);
        hash.Add(StartLine);
        hash.Add(StartColumn);
        hash.Add(EndLine);
        hash.Add(EndColumn);
        hash.Add(TextSpanStart);
        hash.Add(TextSpanLength);
        hash.Add(MessageArgs);
        return hash.ToHashCode();
    }
}

// src/Generator/Renderer/CodeWriter.cs
using System;
using System.Text;

namespace Neatoo.RemoteFactory.Generator.Renderer;

/// <summary>
/// Helper for generating formatted C# code with automatic indentation management.
/// </summary>
internal class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent = 0;
    private const string IndentString = "    "; // 4 spaces to match NormalizeWhitespace

    public void Line(string text = "")
    {
        if (string.IsNullOrEmpty(text))
        {
            _sb.AppendLine();
        }
        else
        {
            _sb.Append(GetIndent());
            _sb.AppendLine(text);
        }
    }

    public void OpenBrace()
    {
        Line("{");
        _indent++;
    }

    public void CloseBrace()
    {
        _indent--;
        Line("}");
    }

    public IDisposable Block(string header)
    {
        Line(header);
        OpenBrace();
        return new BlockScope(this);
    }

    public IDisposable Braces()
    {
        OpenBrace();
        return new BlockScope(this);
    }

    private string GetIndent() => new string(' ', _indent * 4);

    public override string ToString() => _sb.ToString();

    private class BlockScope : IDisposable
    {
        private readonly CodeWriter _writer;
        public BlockScope(CodeWriter writer) => _writer = writer;
        public void Dispose() => _writer.CloseBrace();
    }
}

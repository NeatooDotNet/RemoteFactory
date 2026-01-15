// src/Generator/Renderer/FactoryRenderer.cs
// Entry point for rendering factory code from models.

#nullable enable

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Neatoo.RemoteFactory.Generator.Model;

namespace Neatoo.RemoteFactory.Generator.Renderer;

/// <summary>
/// Entry point for rendering factory code from models.
/// Dispatches to specific renderers based on the model type.
/// </summary>
internal static class FactoryRenderer
{
    /// <summary>
    /// Renders a FactoryGenerationUnit to source code.
    /// </summary>
    /// <param name="unit">The factory generation unit containing the model to render.</param>
    /// <returns>The generated C# source code.</returns>
    public static string Render(FactoryGenerationUnit unit)
    {
        try
        {
            string source;

            if (unit.ClassFactory != null)
            {
                source = ClassFactoryRenderer.Render(unit);
            }
            else if (unit.StaticFactory != null)
            {
                source = StaticFactoryRenderer.Render(unit);
            }
            else if (unit.InterfaceFactory != null)
            {
                source = InterfaceFactoryRenderer.Render(unit);
            }
            else
            {
                throw new InvalidOperationException("FactoryGenerationUnit has no factory model set.");
            }

            // Clean up common formatting issues
            source = CleanupSource(source);

            // Normalize whitespace using Roslyn
            source = NormalizeWhitespace(source);

            return source;
        }
        catch (Exception ex)
        {
            return $@"/* Error: {ex.GetType().FullName} {ex.Message} */";
        }
    }

    /// <summary>
    /// Renders ordinal serialization support for a type.
    /// </summary>
    /// <param name="unit">The factory generation unit containing the ordinal serialization model.</param>
    /// <returns>The generated C# source code, or null if no ordinal serialization is needed.</returns>
    public static string? RenderOrdinalSerialization(FactoryGenerationUnit unit)
    {
        if (unit.ClassFactory?.OrdinalSerialization == null)
        {
            return null;
        }

        try
        {
            var source = OrdinalRenderer.Render(unit.ClassFactory.OrdinalSerialization);
            source = NormalizeWhitespace(source);
            return source;
        }
        catch (Exception ex)
        {
            return $@"/* Error: {ex.GetType().FullName} {ex.Message} */";
        }
    }

    /// <summary>
    /// Cleans up common formatting issues in generated source code.
    /// </summary>
    private static string CleanupSource(string source)
    {
        // Fix array and parameter formatting issues
        source = source.Replace("[, ", "[");
        source = source.Replace("(, ", "(");
        source = source.Replace(", )", ")");
        return source;
    }

    /// <summary>
    /// Normalizes whitespace in C# source code using Roslyn.
    /// </summary>
    private static string NormalizeWhitespace(string source)
    {
        return CSharpSyntaxTree.ParseText(source)
            .GetRoot()
            .NormalizeWhitespace()
            .SyntaxTree
            .GetText()
            .ToString();
    }
}

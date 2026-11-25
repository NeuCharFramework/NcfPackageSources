/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SandboxNotebookBuilder.cs
    File description: Related implementation.

    Created by: Senparc - 20260918

    Modified by: Senparc - 20260918
    Change description: v0.3.3 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.3.4 0.3.4 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.4.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.4.4 Localized sandbox setup and runtime surfaces

----------------------------------------------------------------*/

using System.Text;
using System.Text.Json;

namespace Senparc.Xncf.Sandbox.Domain.Services;

/// <summary>
/// Builds bounded Jupyter Notebook JSON for supported Python and C# kernels.
/// </summary>
public static class SandboxNotebookBuilder
{
    public const string PythonLanguage = "python";
    public const string CsharpLanguage = "csharp";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Builds a notebook document from marker-delimited source cells.
    /// </summary>
    /// <param name="language">The supported notebook language.</param>
    /// <param name="title">Optional markdown title.</param>
    /// <param name="cellsSource">The source text containing code/markdown cell markers.</param>
    /// <returns>Indented notebook JSON.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the language or cell content is invalid.</exception>
    public static string Build(string language, string? title, string cellsSource)
    {
        var normalizedLanguage = NormalizeLanguage(language);
        var cells = ParseCells(cellsSource);
        var titleText = (title ?? string.Empty).Trim();
        if (titleText.Length > 0)
        {
            cells.Insert(0, new NotebookCell("markdown", TitleToLines(titleText)));
        }

        var (kernelName, displayName, kernelLanguage) = normalizedLanguage switch
        {
            CsharpLanguage => ("C# .NET SDK", "C# .NET SDK", "CSharp"),
            _ => ("python3", "Python 3", "python")
        };

        var notebook = new
        {
            cells = cells.Select(cell => ToCellPayload(cell)).ToArray(),
            metadata = new
            {
                kernelspec = new { display_name = displayName, language = kernelLanguage, name = kernelName },
                language_info = new { name = kernelLanguage }
            },
            nbformat = 4,
            nbformat_minor = 5
        };

        return JsonSerializer.Serialize(notebook, JsonOptions);
    }

    /// <summary>
    /// Normalizes supported Python and C# language aliases.
    /// </summary>
    /// <param name="language">The requested language alias.</param>
    /// <returns><c>python</c> or <c>csharp</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the language is unsupported.</exception>
    public static string NormalizeLanguage(string? language)
    {
        var value = (language ?? string.Empty).Trim().ToLowerInvariant();
        if (value is CsharpLanguage or "c#" or "csharp" or "dotnet" or "csharp .net")
        {
            return CsharpLanguage;
        }

        if (value is PythonLanguage or "py" or "python3" or "python 3" or "")
        {
            return PythonLanguage;
        }

        throw new InvalidOperationException($"不支持的 Notebook 语言：{language}（可选 python / csharp）");
    }

    private static object ToCellPayload(NotebookCell cell)
    {
        if (string.Equals(cell.Type, "markdown", StringComparison.Ordinal))
        {
            return new
            {
                cell_type = "markdown",
                id = cell.Id,
                metadata = new { },
                source = cell.Source
            };
        }

        return new
        {
            cell_type = "code",
            execution_count = (int?)null,
            id = cell.Id,
            metadata = new { },
            outputs = Array.Empty<object>(),
            source = cell.Source
        };
    }

    private static List<NotebookCell> ParseCells(string? cellsSource)
    {
        var lines = (cellsSource ?? string.Empty)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        var cells = new List<NotebookCell>();
        var buffer = new List<string>();
        var pendingMarkdown = false;
        var hasContent = false;

        foreach (var rawLine in lines)
        {
            var marker = TryParseMarker(rawLine);
            if (marker == null)
            {
                buffer.Add(rawLine);
                if (rawLine.Trim().Length > 0)
                {
                    hasContent = true;
                }

                continue;
            }

            FlushCell(cells, buffer, pendingMarkdown);
            buffer.Clear();
            pendingMarkdown = marker.Value;
        }

        FlushCell(cells, buffer, pendingMarkdown);

        if (!hasContent && cells.Count == 0)
        {
            throw new InvalidOperationException("Notebook 单元内容不能为空。");
        }

        if (cells.Count == 0)
        {
            throw new InvalidOperationException("Notebook 单元内容不能为空。");
        }

        return cells;
    }

    private static void FlushCell(List<NotebookCell> cells, List<string> buffer, bool asMarkdown)
    {
        var source = ToSourceLines(buffer);
        if (source.Length == 0 && cells.Count == 0)
        {
            return;
        }

        cells.Add(new NotebookCell(asMarkdown ? "markdown" : "code", source));
    }

    private static string[] ToSourceLines(List<string> lines)
    {
        while (lines.Count > 0 && lines[^1].Trim().Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        var result = new string[lines.Count];
        for (var index = 0; index < lines.Count; index++)
        {
            result[index] = index == lines.Count - 1 ? lines[index] : lines[index] + "\n";
        }

        return result;
    }

    private static string[] TitleToLines(string title)
    {
        return new[] { $"# {title}" };
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    private static bool? TryParseMarker(string line)
    {
        var trimmed = line.Trim();
        if (trimmed is "# %%" or "#%%")
        {
            return false;
        }

        if (trimmed.StartsWith("# %% [markdown]", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("#%%[markdown]", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return null;
    }

    private readonly record struct NotebookCell(string Type, string[] Source)
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NcfFileTextExtractor.cs
    File description: Related implementation.

    Created by: Senparc - 20260803

    Modified by: Senparc - 20260804
    Change description: v0.5.0-preview5 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.1 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Senparc.Xncf.FileManager.Domain.Services;

public sealed record NcfFileTextSection(string Title, string Text);

/// <summary>
/// Represents one bounded section of text extracted from a plain-text or Open XML file.
/// </summary>
public sealed class NcfFileTextExtractionResult
{
    /// <summary>
    /// Initializes an extraction result.
    /// </summary>
    /// <param name="text">The normalized extracted text.</param>
    /// <param name="extension">The normalized source extension.</param>
    public NcfFileTextExtractionResult(string text, string extension)
    {
        Text = text;
        Extension = extension;
    }

    public string Text { get; }

    public string Extension { get; }
}

/// <summary>
/// Extracts bounded text from supported plain-text, DOCX, PPTX, and XLSX payloads.
/// ZIP/XML relationships are restricted to package-local entries and extracted output
/// is capped by <see cref="MaxExtractedCharacters"/>.
/// </summary>
public static class NcfFileTextExtractor
{
    public const int MaxExtractedCharacters = 2_000_000;
    private const long MaxXmlEntryBytes = 8L * 1024 * 1024;

    private static readonly HashSet<string> PlainTextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".log", ".md", ".markdown", ".csv", ".tsv", ".json", ".xml",
        ".yaml", ".yml", ".html", ".htm", ".css", ".js", ".ts", ".cs", ".sql"
    };

    private static readonly HashSet<string> OpenXmlExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".docx", ".pptx", ".xlsx"
    };

    /// <summary>
    /// Gets the normalized extensions accepted by the extractor.
    /// </summary>
    public static IReadOnlyCollection<string> SupportedExtensions =>
        PlainTextExtensions.Concat(OpenXmlExtensions).OrderBy(z => z).ToArray();

    /// <summary>
    /// Determines whether an extension is supported by the local text extractor.
    /// </summary>
    /// <param name="extension">An extension with or without a leading period.</param>
    /// <returns><see langword="true"/> when the normalized extension is supported.</returns>
    public static bool CanExtractText(string extension)
    {
        var normalized = NormalizeExtension(extension);
        return PlainTextExtensions.Contains(normalized) || OpenXmlExtensions.Contains(normalized);
    }

    /// <summary>
    /// Extracts logically grouped sections from supported text or Open XML content.
    /// DOCX sections represent paragraphs, XLSX sections represent worksheets, and
    /// PPTX sections represent slides rather than physical page numbers.
    /// </summary>
    /// <param name="bytes">The complete file payload.</param>
    /// <param name="extension">The source extension with or without a leading period.</param>
    /// <returns>The extracted sections in source order.</returns>
    /// <exception cref="InvalidDataException">Thrown when the package is malformed, references external parts, or exceeds extraction limits.</exception>
    /// <exception cref="NotSupportedException">Thrown when the extension is not supported.</exception>
    public static IReadOnlyList<NcfFileTextSection> ExtractSections(byte[] bytes, string extension)
    {
        var normalized = NormalizeExtension(extension);
        if (PlainTextExtensions.Contains(normalized))
            return ChunkText(DecodePlainText(bytes), "文本");
        using var archive = OpenArchive(bytes);
        var sections = new List<NcfFileTextSection>();
        if (normalized == ".docx")
        {
            var document = LoadXml(archive.GetEntry("word/document.xml")
                ?? throw new InvalidDataException("DOCX 缺少正文。"));
            var text = string.Join("\n", document.Descendants().Where(element => element.Name.LocalName == "p")
                .Select(paragraph => string.Concat(paragraph.Descendants()
                    .Where(element => element.Name.LocalName is "t" or "tab" or "br")
                    .Select(element => element.Name.LocalName switch { "tab" => "\t", "br" => "\n", _ => element.Value }))));
            return ChunkText(text, "正文分段（非物理页码）");
        }
        if (normalized == ".xlsx")
        {
            var workbook = LoadXml(archive.GetEntry("xl/workbook.xml")
                ?? throw new InvalidDataException("XLSX 缺少工作簿。"));
            var relationships = ReadRelationships(archive, "xl/_rels/workbook.xml.rels", "xl/workbook.xml");
            var sharedStrings = ReadSharedStrings(archive);
            foreach (var sheet in workbook.Descendants().Where(element => element.Name.LocalName == "sheet"))
            {
                var relationshipId = sheet.Attributes().FirstOrDefault(attribute => attribute.Name.LocalName == "id")?.Value;
                if (relationshipId == null || !relationships.TryGetValue(relationshipId, out var part))
                    throw new InvalidDataException("工作表引用无效。");
                var xml = LoadXml(archive.GetEntry(part) ?? throw new InvalidDataException("工作表不存在。"));
                var lines = new List<string>();
                var length = 0;
                foreach (var row in xml.Descendants().Where(element => element.Name.LocalName == "row"))
                {
                    var values = row.Elements().Where(element => element.Name.LocalName == "c").Select(cell =>
                    {
                        var value = ReadCellText(cell, sharedStrings);
                        var formula = cell.Elements().FirstOrDefault(element => element.Name.LocalName == "f")?.Value;
                        if (formula != null) value = $"={formula} [缓存值: {value}]";
                        return $"{cell.Attribute("r")?.Value ?? "?"}={value}";
                    });
                    var line = string.Join("\t", values);
                    length += line.Length;
                    if (length > MaxExtractedCharacters) throw new InvalidDataException("工作表文本超过提取限制。");
                    lines.Add(line);
                }
                sections.Add(new NcfFileTextSection(sheet.Attribute("name")?.Value ?? part, string.Join("\n", lines)));
                if (sections.Sum(section => (long)section.Text.Length) > MaxExtractedCharacters)
                    throw new InvalidDataException("工作簿文本超过提取限制。");
            }
        }
        else if (normalized == ".pptx")
        {
            var presentation = LoadXml(archive.GetEntry("ppt/presentation.xml")
                ?? throw new InvalidDataException("PPTX 缺少演示文稿。"));
            var relationships = ReadRelationships(archive, "ppt/_rels/presentation.xml.rels", "ppt/presentation.xml");
            foreach (var slide in presentation.Descendants().Where(element => element.Name.LocalName == "sldId"))
            {
                var relationshipId = slide.Attributes().FirstOrDefault(attribute =>
                    attribute.Name.LocalName == "id" && attribute.Name.NamespaceName.Length > 0)?.Value;
                if (relationshipId == null || !relationships.TryGetValue(relationshipId, out var part))
                    throw new InvalidDataException("幻灯片引用无效。");
                var document = LoadXml(archive.GetEntry(part) ?? throw new InvalidDataException("幻灯片不存在。"));
                var text = string.Join("\n", document.Descendants().Where(element => element.Name.LocalName == "p")
                    .Select(paragraph => string.Concat(paragraph.Descendants().Where(element => element.Name.LocalName == "t")
                        .Select(element => element.Value))));
                sections.Add(new NcfFileTextSection($"幻灯片 {sections.Count + 1}", text));
                if (sections.Sum(section => (long)section.Text.Length) > MaxExtractedCharacters)
                    throw new InvalidDataException("演示文稿文本超过提取限制。");
            }
        }
        else throw new NotSupportedException("此格式不支持本地文字提取。");
        if (sections.Count > 1000) throw new InvalidDataException("文档分段超过 1000 个。");
        return sections;
    }

    /// <summary>
    /// Splits plain text into bounded sections while avoiding a split between UTF-16 surrogate pairs.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <param name="title">The title prefix assigned to each generated section.</param>
    /// <returns>The ordered text sections.</returns>
    /// <exception cref="InvalidDataException">Thrown when the input exceeds <see cref="MaxExtractedCharacters"/>.</exception>
    private static IReadOnlyList<NcfFileTextSection> ChunkText(string text, string title)
    {
        if (text.Length > MaxExtractedCharacters) throw new InvalidDataException("文本超过提取限制。");
        var result = new List<NcfFileTextSection>();
        for (var offset = 0; offset < text.Length;)
        {
            var count = Math.Min(8000, text.Length - offset);
            if (offset + count < text.Length && char.IsHighSurrogate(text[offset + count - 1])) count--;
            result.Add(new NcfFileTextSection($"{title} {result.Count + 1}", text.Substring(offset, count)));
            offset += count;
        }
        return result;
    }

    /// <summary>
    /// Reads package-local Open XML relationships and resolves their targets without allowing
    /// external or host-escaping references.
    /// </summary>
    /// <param name="archive">The Open XML ZIP archive.</param>
    /// <param name="relationshipsPath">The package-relative relationship-part path.</param>
    /// <param name="sourcePath">The package-relative path of the source part.</param>
    /// <returns>A map from relationship identifiers to package-relative target paths.</returns>
    /// <exception cref="InvalidDataException">Thrown when relationship XML is missing, malformed, or external.</exception>
    private static Dictionary<string, string> ReadRelationships(ZipArchive archive, string relationshipsPath, string sourcePath)
    {
        var document = LoadXml(archive.GetEntry(relationshipsPath)
            ?? throw new InvalidDataException("Office 文档缺少关系定义。"));
        return document.Descendants().Where(element => element.Name.LocalName == "Relationship"
                && !string.Equals(element.Attribute("TargetMode")?.Value, "External", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(element => element.Attribute("Id")?.Value ?? throw new InvalidDataException("关系缺少 Id。"),
                element =>
                {
                    var target = element.Attribute("Target")?.Value ?? throw new InvalidDataException("关系缺少目标。");
                    var uri = new Uri(new Uri("https://office.invalid/" + sourcePath), target);
                    if (uri.Host != "office.invalid" || uri.Scheme != "https")
                        throw new InvalidDataException("Office 关系不允许引用外部文件。");
                    return Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
                });
    }

    /// <summary>
    /// Extracts normalized text for knowledge-base ingestion from a supported file payload.
    /// Empty output and content exceeding the configured character limit are rejected.
    /// </summary>
    /// <param name="fileBytes">The complete file payload.</param>
    /// <param name="extension">The source extension with or without a leading period.</param>
    /// <param name="displayName">Optional display name included in validation errors.</param>
    /// <returns>The normalized extracted text and normalized extension.</returns>
    /// <exception cref="InvalidDataException">Thrown when the file is empty, malformed, binary, empty after extraction, or exceeds the text limit.</exception>
    /// <exception cref="NotSupportedException">Thrown when the extension is not supported.</exception>
    public static NcfFileTextExtractionResult Extract(byte[] fileBytes, string extension, string displayName = null)
    {
        if (fileBytes == null || fileBytes.Length == 0)
        {
            throw new InvalidDataException("文件内容为空，无法提取文本。");
        }

        var normalizedExtension = NormalizeExtension(extension);
        if (!CanExtractText(normalizedExtension))
        {
            throw new NotSupportedException(
                $"文件“{displayName ?? "未命名"}”的格式 {normalizedExtension} 暂不支持知识库文本提取。" +
                $"当前支持：{string.Join("、", SupportedExtensions)}。");
        }

        string text;
        if (PlainTextExtensions.Contains(normalizedExtension))
        {
            text = DecodePlainText(fileBytes);
        }
        else
        {
            text = normalizedExtension switch
            {
                ".docx" => ExtractWordText(fileBytes),
                ".pptx" => ExtractPowerPointText(fileBytes),
                ".xlsx" => ExtractExcelText(fileBytes),
                _ => throw new NotSupportedException($"格式 {normalizedExtension} 暂不支持文本提取。")
            };
        }

        text = NormalizeText(text);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidDataException($"文件“{displayName ?? "未命名"}”中没有可供知识库使用的文本内容。");
        }

        if (text.Length > MaxExtractedCharacters)
        {
            throw new InvalidDataException(
                $"文件“{displayName ?? "未命名"}”提取后的文本超过 {MaxExtractedCharacters:N0} 个字符，请拆分后再导入。");
        }

        return new NcfFileTextExtractionResult(text, normalizedExtension);
    }

    /// <summary>
    /// Normalizes an extension to lowercase and ensures it begins with a period.
    /// </summary>
    /// <param name="extension">The extension to normalize.</param>
    /// <returns>The normalized extension, or an empty string for null or whitespace input.</returns>
    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var normalized = extension.Trim().ToLowerInvariant();
        return normalized.StartsWith('.') ? normalized : "." + normalized;
    }

    /// <summary>
    /// Decodes UTF-8 or UTF-16 text based on an optional BOM and rejects binary null characters.
    /// </summary>
    /// <param name="bytes">The encoded text payload.</param>
    /// <returns>The decoded text.</returns>
    /// <exception cref="InvalidDataException">Thrown when the bytes are invalid text or contain binary null characters.</exception>
    private static string DecodePlainText(byte[] bytes)
    {
        Encoding encoding;
        var offset = 0;

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            encoding = new UTF8Encoding(false, true);
            offset = 3;
        }
        else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            encoding = new UnicodeEncoding(false, true, true);
            offset = 2;
        }
        else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            encoding = new UnicodeEncoding(true, true, true);
            offset = 2;
        }
        else
        {
            encoding = new UTF8Encoding(false, true);
        }

        try
        {
            var text = encoding.GetString(bytes, offset, bytes.Length - offset);
            if (text.IndexOf('\0') >= 0)
            {
                throw new InvalidDataException("文件包含二进制空字符，不能作为纯文本导入知识库。");
            }
            return text;
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException("文本文件不是有效的 UTF-8/UTF-16 编码，请转换编码后再导入。", ex);
        }
    }

    /// <summary>
    /// Extracts paragraph text from the main WordprocessingML document part.
    /// </summary>
    /// <param name="bytes">The DOCX payload.</param>
    /// <returns>The non-empty paragraph text joined by platform newlines.</returns>
    /// <exception cref="InvalidDataException">Thrown when the package is malformed or the main document part is missing.</exception>
    private static string ExtractWordText(byte[] bytes)
    {
        using var archive = OpenArchive(bytes);
        var entry = archive.GetEntry("word/document.xml")
            ?? throw new InvalidDataException("DOCX 文件缺少 word/document.xml。");
        var document = LoadXml(entry);

        return string.Join(Environment.NewLine,
            document.Descendants()
                .Where(z => z.Name.LocalName == "p")
                .Select(z => string.Concat(z.Descendants().Where(t => t.Name.LocalName == "t").Select(t => t.Value)))
                .Where(z => !string.IsNullOrWhiteSpace(z)));
    }

    /// <summary>
    /// Extracts text from PowerPoint slide XML parts ordered by their trailing slide number.
    /// </summary>
    /// <param name="bytes">The PPTX payload.</param>
    /// <returns>The non-empty slide text joined by blank lines.</returns>
    /// <exception cref="InvalidDataException">Thrown when the package contains no readable slide parts or is malformed.</exception>
    private static string ExtractPowerPointText(byte[] bytes)
    {
        using var archive = OpenArchive(bytes);
        var slides = archive.Entries
            .Where(z => z.FullName.StartsWith("ppt/slides/slide", StringComparison.OrdinalIgnoreCase)
                && z.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(z => ParseTrailingNumber(z.Name))
            .ToList();

        if (slides.Count == 0)
        {
            throw new InvalidDataException("PPTX 文件中未找到幻灯片内容。");
        }

        return string.Join(Environment.NewLine + Environment.NewLine,
            slides.Select(entry =>
                    string.Join(Environment.NewLine,
                        LoadXml(entry).Descendants()
                            .Where(z => z.Name.LocalName == "t")
                            .Select(z => z.Value)
                            .Where(z => !string.IsNullOrWhiteSpace(z))))
                .Where(z => !string.IsNullOrWhiteSpace(z)));
    }

    /// <summary>
    /// Extracts non-empty cell values from Excel worksheet parts, resolving shared strings.
    /// </summary>
    /// <param name="bytes">The XLSX payload.</param>
    /// <returns>The worksheet rows joined by tabs and blank lines.</returns>
    /// <exception cref="InvalidDataException">Thrown when the package contains no worksheets or is malformed.</exception>
    private static string ExtractExcelText(byte[] bytes)
    {
        using var archive = OpenArchive(bytes);
        var sharedStrings = ReadSharedStrings(archive);
        var worksheets = archive.Entries
            .Where(z => z.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase)
                && z.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .OrderBy(z => ParseTrailingNumber(z.Name))
            .ToList();

        if (worksheets.Count == 0)
        {
            throw new InvalidDataException("XLSX 文件中未找到工作表内容。");
        }

        var sheetTexts = new List<string>();
        foreach (var worksheet in worksheets)
        {
            var rows = new List<string>();
            foreach (var row in LoadXml(worksheet).Descendants().Where(z => z.Name.LocalName == "row"))
            {
                var cells = row.Elements().Where(z => z.Name.LocalName == "c")
                    .Select(cell => ReadCellText(cell, sharedStrings))
                    .ToList();
                if (cells.Any(z => !string.IsNullOrWhiteSpace(z)))
                {
                    rows.Add(string.Join("\t", cells));
                }
            }

            if (rows.Count > 0)
            {
                sheetTexts.Add(string.Join(Environment.NewLine, rows));
            }
        }

        return string.Join(Environment.NewLine + Environment.NewLine, sheetTexts);
    }

    /// <summary>
    /// Reads the optional shared-string table from an Excel package.
    /// </summary>
    /// <param name="archive">The open XLSX archive.</param>
    /// <returns>The shared strings in their package-defined index order.</returns>
    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
        {
            return Array.Empty<string>();
        }

        return LoadXml(entry).Descendants()
            .Where(z => z.Name.LocalName == "si")
            .Select(z => string.Concat(z.Descendants().Where(t => t.Name.LocalName == "t").Select(t => t.Value)))
            .ToList();
    }

    /// <summary>
    /// Resolves the textual value of an Excel cell, including inline strings and shared-string indices.
    /// </summary>
    /// <param name="cell">The worksheet cell element.</param>
    /// <param name="sharedStrings">The package shared-string table.</param>
    /// <returns>The cell value, or an empty string when no value element is present.</returns>
    /// <exception cref="InvalidDataException">Thrown when a shared-string index is invalid.</exception>
    private static string ReadCellText(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var cellType = cell.Attribute("t")?.Value;
        if (string.Equals(cellType, "inlineStr", StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat(cell.Descendants().Where(z => z.Name.LocalName == "t").Select(z => z.Value));
        }

        var value = cell.Elements().FirstOrDefault(z => z.Name.LocalName == "v")?.Value ?? string.Empty;
        if (string.Equals(cellType, "s", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                && index >= 0 && index < sharedStrings.Count)
                return sharedStrings[index];
            throw new InvalidDataException("Excel 单元格引用了无效的共享字符串，拒绝将索引误当成数据。");
        }

        return value;
    }

    /// <summary>
    /// Opens an Open XML ZIP archive and enforces entry-count and uncompressed-size limits.
    /// </summary>
    /// <param name="bytes">The ZIP payload.</param>
    /// <returns>An open read-only archive.</returns>
    /// <exception cref="InvalidDataException">Thrown when the payload is not a valid archive or exceeds limits.</exception>
    private static ZipArchive OpenArchive(byte[] bytes)
    {
        try
        {
            var archive = new ZipArchive(new MemoryStream(bytes, writable: false), ZipArchiveMode.Read, leaveOpen: false);
            if (archive.Entries.Count > 2000 || archive.Entries.Any(entry => entry.Length > 64L * 1024 * 1024)
                || archive.Entries.Sum(entry => entry.Length) > 64L * 1024 * 1024)
            {
                archive.Dispose();
                throw new InvalidDataException("Office 压缩包超过条目数或解压大小限制。");
            }
            return archive;
        }
        catch (InvalidDataException ex)
        {
            throw new InvalidDataException("Office Open XML 文件结构无效或已损坏。", ex);
        }
    }

    /// <summary>
    /// Loads one package XML entry with DTD processing disabled and document-size limits enabled.
    /// </summary>
    /// <param name="entry">The ZIP entry containing XML.</param>
    /// <returns>The parsed XML document.</returns>
    /// <exception cref="InvalidDataException">Thrown when the entry exceeds limits or is not valid XML.</exception>
    private static XDocument LoadXml(ZipArchiveEntry entry)
    {
        if (entry.Length > MaxXmlEntryBytes)
        {
            throw new InvalidDataException($"压缩包条目 {entry.FullName} 解压后过大，已拒绝解析。");
        }

        using var stream = entry.Open();
        using var reader = XmlReader.Create(stream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxXmlEntryBytes
        });
        return XDocument.Load(reader, LoadOptions.None);
    }

    /// <summary>
    /// Parses the trailing decimal number in a file name for stable part ordering.
    /// </summary>
    /// <param name="fileName">The file name to inspect.</param>
    /// <returns>The trailing number, or <see cref="int.MaxValue"/> when none is present.</returns>
    private static int ParseTrailingNumber(string fileName)
    {
        var digits = new string(Path.GetFileNameWithoutExtension(fileName).Reverse()
            .TakeWhile(char.IsDigit)
            .Reverse()
            .ToArray());
        return int.TryParse(digits, out var value) ? value : int.MaxValue;
    }

    /// <summary>
    /// Normalizes line endings, trims trailing whitespace from each line, and trims the final result.
    /// </summary>
    /// <param name="text">The text to normalize.</param>
    /// <returns>The normalized text, or an empty string for null or empty input.</returns>
    private static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n')
            .Select(z => z.TrimEnd())
            .Aggregate(new StringBuilder(), (builder, line) => builder.AppendLine(line))
            .ToString()
            .Trim();
    }
}

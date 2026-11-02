/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NcfFileTextExtractor.cs
    文件功能描述：为知识库等上层模块提供受控的文件文本提取能力

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.5.0-preview5 新增文件文本提取与文件管理服务

    修改标识：Senparc - 20261103
    修改描述：v0.8.1 add comprehensive unit tests for AdminChatWorkspaceAssetService functionality

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
/// 文件文本提取结果。
/// </summary>
public sealed class NcfFileTextExtractionResult
{
    public NcfFileTextExtractionResult(string text, string extension)
    {
        Text = text;
        Extension = extension;
    }

    public string Text { get; }

    public string Extension { get; }
}

/// <summary>
/// 仅处理无需执行外部程序、无需解析宏的安全文本格式。
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

    public static IReadOnlyCollection<string> SupportedExtensions =>
        PlainTextExtensions.Concat(OpenXmlExtensions).OrderBy(z => z).ToArray();

    public static bool CanExtractText(string extension)
    {
        var normalized = NormalizeExtension(extension);
        return PlainTextExtensions.Contains(normalized) || OpenXmlExtensions.Contains(normalized);
    }

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

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var normalized = extension.Trim().ToLowerInvariant();
        return normalized.StartsWith('.') ? normalized : "." + normalized;
    }

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

    private static int ParseTrailingNumber(string fileName)
    {
        var digits = new string(Path.GetFileNameWithoutExtension(fileName).Reverse()
            .TakeWhile(char.IsDigit)
            .Reverse()
            .ToArray());
        return int.TryParse(digits, out var value) ? value : int.MaxValue;
    }

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

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NcfFileTextExtractor.cs
    文件功能描述：为知识库等上层模块提供受控的文件文本提取能力

    创建标识：Senparc - 20260803

    修改标识：Senparc - 20260804
    修改描述：v0.5.0-preview5 新增文件文本提取与文件管理服务

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
        if (string.Equals(cellType, "s", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
            && index >= 0 && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        return value;
    }

    private static ZipArchive OpenArchive(byte[] bytes)
    {
        try
        {
            return new ZipArchive(new MemoryStream(bytes, writable: false), ZipArchiveMode.Read, leaveOpen: false);
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

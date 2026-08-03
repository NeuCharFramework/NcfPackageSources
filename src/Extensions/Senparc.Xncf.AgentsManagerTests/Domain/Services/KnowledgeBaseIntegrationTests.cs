/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：KnowledgeBaseIntegrationTests.cs
    文件功能描述：文件文本提取与 Agent 知识库绑定的回归测试

    创建标识：Senparc - 20260803

----------------------------------------------------------------*/

using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.FileManager.Domain.Services;
using System.IO.Compression;
using System.Text;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class KnowledgeBaseIntegrationTests
{
    [TestMethod]
    public void ExtractPlainText_ShouldDecodeUtf8AndNormalizeLineEndings()
    {
        var result = NcfFileTextExtractor.Extract(
            Encoding.UTF8.GetBytes("first\r\nsecond  \r\n"),
            ".txt",
            "sample.txt");

        Assert.AreEqual("first\nsecond", result.Text);
        Assert.AreEqual(".txt", result.Extension);
    }

    [TestMethod]
    public void ExtractPlainText_ShouldRejectInvalidUtf8()
    {
        Assert.ThrowsException<InvalidDataException>(() =>
            NcfFileTextExtractor.Extract(new byte[] { 0xC3, 0x28 }, ".txt", "invalid.txt"));
    }

    [TestMethod]
    public void ExtractDocx_ShouldReadParagraphTextWithoutOfficeRuntime()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("word/document.xml");
            using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
            writer.Write("<w:document xmlns:w=\"urn:test\"><w:body>" +
                         "<w:p><w:r><w:t>Knowledge</w:t></w:r></w:p>" +
                         "<w:p><w:r><w:t>Base</w:t></w:r></w:p>" +
                         "</w:body></w:document>");
        }

        var result = NcfFileTextExtractor.Extract(stream.ToArray(), ".docx", "sample.docx");

        Assert.AreEqual($"Knowledge{Environment.NewLine}Base", result.Text);
    }

    [TestMethod]
    public void ExtractPptx_ShouldReadSlidesInNumericOrder()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "ppt/slides/slide2.xml", "<p:sld xmlns:p=\"urn:p\" xmlns:a=\"urn:a\"><a:t>Second</a:t></p:sld>");
            WriteEntry(archive, "ppt/slides/slide1.xml", "<p:sld xmlns:p=\"urn:p\" xmlns:a=\"urn:a\"><a:t>First</a:t></p:sld>");
        }

        var result = NcfFileTextExtractor.Extract(stream.ToArray(), ".pptx", "sample.pptx");

        Assert.AreEqual($"First{Environment.NewLine}{Environment.NewLine}Second", result.Text);
    }

    [TestMethod]
    public void ExtractXlsx_ShouldResolveSharedAndInlineStrings()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "xl/sharedStrings.xml", "<sst xmlns=\"urn:x\"><si><t>Shared</t></si></sst>");
            WriteEntry(archive, "xl/worksheets/sheet1.xml",
                "<worksheet xmlns=\"urn:x\"><sheetData><row>" +
                "<c t=\"s\"><v>0</v></c><c t=\"inlineStr\"><is><t>Inline</t></is></c>" +
                "</row></sheetData></worksheet>");
        }

        var result = NcfFileTextExtractor.Extract(stream.ToArray(), ".xlsx", "sample.xlsx");

        Assert.AreEqual("Shared\tInline", result.Text);
    }

    [TestMethod]
    public void Extract_ShouldRejectFormatsThatNeedPdfOrOcrRuntime()
    {
        Assert.ThrowsException<NotSupportedException>(() =>
            NcfFileTextExtractor.Extract(Encoding.UTF8.GetBytes("pdf"), ".pdf", "sample.pdf"));
    }

    [TestMethod]
    public void AgentTemplate_ShouldKeepKnowledgeBaseBindingWhenUpdated()
    {
        var agent = new AgentTemplate(
            "agent",
            "system",
            true,
            "description",
            null,
            HookRobotType.None,
            null,
            knowledgeBaseId: 10);

        agent.UpdateFromDto(new AgentTemplateDto
        {
            Name = "agent-2",
            SystemMessage = "system-2",
            Enable = true,
            Description = "description-2",
            KnowledgeBaseId = 20
        });

        Assert.AreEqual(20, agent.KnowledgeBaseId);
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}

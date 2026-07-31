using Microsoft.VisualStudio.TestTools.UnitTesting;
using NcfDesktopApp.GUI.Models;
using NcfDesktopApp.GUI.Services;

namespace NcfDesktopApp.GUI.Tests;

[TestClass]
public sealed class VoiceModelCatalogTests
{
    private string _testRoot = null!;

    [TestInitialize]
    public void Initialize()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "ncf-voice-model-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    [TestMethod]
    public void Evaluate_WhenNoModelSelected_RequiresConfiguration()
    {
        var result = VoiceModelCatalog.Evaluate(null, null);

        Assert.AreEqual(VoiceModelReadinessState.NotSelected, result.State);
        Assert.IsFalse(result.IsReady);
        StringAssert.Contains(result.Message, "尚未选择语音模型");
    }

    [TestMethod]
    public void EvaluateFile_WhenDownloadableModelIsMissing_PromptsForDownload()
    {
        var model = VoiceModelCatalog.FindById("base");
        Assert.IsNotNull(model);

        var result = VoiceModelCatalog.EvaluateFile(model, Path.Combine(_testRoot, model.FileName));

        Assert.AreEqual(VoiceModelReadinessState.Missing, result.State);
        StringAssert.Contains(result.Message, "下载所选模型");
    }

    [TestMethod]
    public void EvaluateFile_WhenDownloadIsPartial_RejectsModel()
    {
        var model = VoiceModelCatalog.FindById("tiny");
        Assert.IsNotNull(model);
        var modelPath = Path.Combine(_testRoot, model.FileName);
        File.WriteAllBytes(modelPath, new byte[32]);

        var result = VoiceModelCatalog.EvaluateFile(model, modelPath);

        Assert.AreEqual(VoiceModelReadinessState.Incomplete, result.State);
        Assert.IsFalse(result.IsReady);
        StringAssert.Contains(result.Message, "不完整");
    }

    [TestMethod]
    public void Evaluate_WhenCustomModelPathIsMissing_PromptsForFileSelection()
    {
        var model = VoiceModelCatalog.FindById(VoiceModelCatalog.CustomModelId);
        Assert.IsNotNull(model);

        var result = VoiceModelCatalog.Evaluate(model, null);

        Assert.AreEqual(VoiceModelReadinessState.Missing, result.State);
        StringAssert.Contains(result.Message, "选择本地模型");
    }

    [TestMethod]
    public void EvaluateFile_WhenCustomModelMeetsMinimumSize_MarksItReady()
    {
        var model = new VoiceModelOption(
            VoiceModelCatalog.CustomModelId,
            "测试模型",
            "测试用途",
            "16 bytes",
            LocalVoiceModelKind.Custom,
            string.Empty,
            16,
            16,
            false);
        var modelPath = Path.Combine(_testRoot, "custom-model.bin");
        using (var stream = File.Create(modelPath))
        {
            stream.SetLength(16);
        }

        var result = VoiceModelCatalog.EvaluateFile(model, modelPath);

        Assert.AreEqual(VoiceModelReadinessState.Ready, result.State);
        Assert.IsTrue(result.IsReady);
        Assert.AreEqual(modelPath, result.ModelPath);
    }
}

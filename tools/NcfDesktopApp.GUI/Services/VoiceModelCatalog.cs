/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：VoiceModelCatalog.cs
    文件功能描述：本地 Whisper 模型目录、选择与完整性预检

    创建标识：Senparc - 20260801
----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NcfDesktopApp.GUI.Models;
using Whisper.net.Ggml;

namespace NcfDesktopApp.GUI.Services;

internal static class VoiceModelCatalog
{
    private const long MiB = 1024L * 1024L;

    public const string CustomModelId = "custom";

    public static IReadOnlyList<VoiceModelOption> Options { get; } = new[]
    {
        new VoiceModelOption(
            "tiny",
            "Whisper Tiny（多语言）",
            "体积最小，适合先验证麦克风和离线识别流程。",
            "约 75 MiB",
            LocalVoiceModelKind.Tiny,
            "ggml-tiny.bin",
            75 * MiB,
            60 * MiB,
            true),
        new VoiceModelOption(
            "base",
            "Whisper Base（多语言）",
            "速度与中文识别效果较均衡，推荐作为默认试用模型。",
            "约 142 MiB",
            LocalVoiceModelKind.Base,
            "ggml-base.bin",
            142 * MiB,
            110 * MiB,
            true),
        new VoiceModelOption(
            "small",
            "Whisper Small（多语言）",
            "识别能力更强，但下载、加载和推理开销更大。",
            "约 466 MiB",
            LocalVoiceModelKind.Small,
            "ggml-small.bin",
            466 * MiB,
            360 * MiB,
            true),
        new VoiceModelOption(
            CustomModelId,
            "手动加载本地 GGML 模型",
            "选择已经下载到本机的 whisper.cpp GGML .bin 文件。",
            "用户提供",
            LocalVoiceModelKind.Custom,
            string.Empty,
            0,
            1 * MiB,
            false)
    };

    public static string ModelsDirectory => Path.Combine(NcfService.AppDataPath, "VoiceModels");

    public static VoiceModelOption? FindById(string? id)
    {
        return Options.FirstOrDefault(option =>
            string.Equals(option.Id, id?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static string GetModelPath(VoiceModelOption option, string? customModelPath)
    {
        return option.Kind == LocalVoiceModelKind.Custom
            ? (customModelPath ?? string.Empty).Trim()
            : Path.Combine(ModelsDirectory, option.FileName);
    }

    public static VoiceModelReadiness Evaluate(
        VoiceModelOption? option,
        string? customModelPath)
    {
        if (option == null)
        {
            return new VoiceModelReadiness(
                VoiceModelReadinessState.NotSelected,
                string.Empty,
                "尚未选择语音模型。请先在“工作台设置 → 本地语音输入”中选择模型。");
        }

        var path = GetModelPath(option, customModelPath);
        if (string.IsNullOrWhiteSpace(path))
        {
            return new VoiceModelReadiness(
                VoiceModelReadinessState.Missing,
                string.Empty,
                "尚未选择本地模型文件，请点击“选择本地模型”。");
        }

        return EvaluateFile(option, path);
    }

    public static VoiceModelReadiness EvaluateFile(VoiceModelOption option, string path)
    {
        if (!File.Exists(path))
        {
            var message = option.CanDownload
                ? $"{option.DisplayName} 尚未下载，请点击“下载所选模型”。"
                : "所选本地模型文件不存在，请重新选择。";
            return new VoiceModelReadiness(VoiceModelReadinessState.Missing, path, message);
        }

        long length;
        try
        {
            length = new FileInfo(path).Length;
        }
        catch (Exception ex)
        {
            return new VoiceModelReadiness(
                VoiceModelReadinessState.Incomplete,
                path,
                $"无法读取模型文件：{ex.Message}");
        }

        if (length < option.MinimumExpectedBytes)
        {
            return new VoiceModelReadiness(
                VoiceModelReadinessState.Incomplete,
                path,
                option.CanDownload
                    ? "模型文件不完整，请重新下载。"
                    : "所选文件过小，不像有效的 Whisper GGML 模型。");
        }

        return new VoiceModelReadiness(
            VoiceModelReadinessState.Ready,
            path,
            $"模型已就绪：{option.DisplayName}（{FormatBytes(length)}）");
    }

    public static GgmlType GetGgmlType(VoiceModelOption option)
    {
        return option.Kind switch
        {
            LocalVoiceModelKind.Tiny => GgmlType.Tiny,
            LocalVoiceModelKind.Base => GgmlType.Base,
            LocalVoiceModelKind.Small => GgmlType.Small,
            _ => throw new InvalidOperationException("手动模型不支持自动下载。")
        };
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < MiB)
        {
            return $"{bytes / 1024d:F1} KiB";
        }

        return $"{bytes / (double)MiB:F1} MiB";
    }
}

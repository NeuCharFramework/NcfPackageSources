/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfPreviewPresentation.cs
    文件功能描述：XNCF 预览状态的统一展示定义

    创建标识：Senparc - 20260802
----------------------------------------------------------------*/

using Senparc.Xncf.XncfBuilder.Domain.Services.Preview;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public sealed record XncfPreviewStageDefinition(
        int Value,
        string Name,
        string Label,
        int ProgressPercent,
        bool IsTerminal);

    public sealed record XncfPreviewHostStatusDefinition(
        int Value,
        string Name,
        string Label);

    public static class XncfPreviewPresentation
    {
        private static readonly XncfPreviewStage[] PipelineStages =
        {
            XncfPreviewStage.PreparingSource,
            XncfPreviewStage.Validating,
            XncfPreviewStage.Restoring,
            XncfPreviewStage.Building,
            XncfPreviewStage.Verifying,
            XncfPreviewStage.Starting,
            XncfPreviewStage.HealthChecking,
            XncfPreviewStage.Replacing,
            XncfPreviewStage.Running
        };

        public static IReadOnlyList<XncfPreviewStageDefinition> GetStageDefinitions()
        {
            return Enum.GetValues<XncfPreviewStage>()
                .Select(CreateDefinition)
                .ToArray();
        }

        public static IReadOnlyList<XncfPreviewStageDefinition> GetPipelineStageDefinitions()
        {
            return PipelineStages.Select(CreateDefinition).ToArray();
        }

        public static string GetStageLabel(XncfPreviewStage stage)
        {
            return XncfBuilderResource.Get(
                $"XncfBuilder.Preview.Stage.{stage}",
                stage.ToString());
        }

        public static IReadOnlyList<XncfPreviewHostStatusDefinition> GetHostStatusDefinitions()
        {
            return Enum.GetValues<XncfPreviewHostStatus>()
                .Select(status => new XncfPreviewHostStatusDefinition(
                    (int)status,
                    status.ToString(),
                    GetHostStatusLabel(status)))
                .ToArray();
        }

        public static string GetHostStatusLabel(XncfPreviewHostStatus status)
        {
            return XncfBuilderResource.Get(
                $"XncfBuilder.Preview.HostStatus.{status}",
                status.ToString());
        }

        private static XncfPreviewStageDefinition CreateDefinition(XncfPreviewStage stage)
        {
            return new XncfPreviewStageDefinition(
                (int)stage,
                stage.ToString(),
                GetStageLabel(stage),
                stage.GetProgressPercent(),
                stage.IsTerminal());
        }
    }
}

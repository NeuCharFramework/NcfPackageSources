/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfPreviewPresentation.cs
    File description: Related implementation.

    Created by: Senparc - 20260802

    Modified by: Senparc - 20260804
    Change description: v0.39.0-preview8 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

    Modified by: Senparc - 20261127
    Change description: v0.42.0.0 add MCP endpoint gateway and tool invocation contracts

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

        /// <summary>
        /// Returns display definitions for every preview stage.
        /// </summary>
        /// <returns>The stage definitions ordered by enum value.</returns>
        public static IReadOnlyList<XncfPreviewStageDefinition> GetStageDefinitions()
        {
            return Enum.GetValues<XncfPreviewStage>()
                .Select(CreateDefinition)
                .ToArray();
        }

        /// <summary>
        /// Returns display definitions for the normal preview pipeline stages.
        /// </summary>
        /// <returns>The ordered pipeline-stage definitions.</returns>
        public static IReadOnlyList<XncfPreviewStageDefinition> GetPipelineStageDefinitions()
        {
            return PipelineStages.Select(CreateDefinition).ToArray();
        }

        /// <summary>
        /// Resolves the localized label for a preview stage.
        /// </summary>
        /// <param name="stage">The stage to label.</param>
        /// <returns>The localized stage label.</returns>
        public static string GetStageLabel(XncfPreviewStage stage)
        {
            return XncfBuilderResource.Get(
                $"XncfBuilder.Preview.Stage.{stage}",
                stage.ToString());
        }

        /// <summary>
        /// Returns localized definitions for every preview host status.
        /// </summary>
        /// <returns>The host-status definitions ordered by enum value.</returns>
        public static IReadOnlyList<XncfPreviewHostStatusDefinition> GetHostStatusDefinitions()
        {
            return Enum.GetValues<XncfPreviewHostStatus>()
                .Select(status => new XncfPreviewHostStatusDefinition(
                    (int)status,
                    status.ToString(),
                    GetHostStatusLabel(status)))
                .ToArray();
        }

        /// <summary>
        /// Resolves the localized label for a preview host status.
        /// </summary>
        /// <param name="status">The host status to label.</param>
        /// <returns>The localized host-status label.</returns>
        public static string GetHostStatusLabel(XncfPreviewHostStatus status)
        {
            return XncfBuilderResource.Get(
                $"XncfBuilder.Preview.HostStatus.{status}",
                status.ToString());
        }

        /// <summary>
        /// Creates one stage definition including progress and terminal-state metadata.
        /// </summary>
        /// <param name="stage">The stage to describe.</param>
        /// <returns>The stage definition.</returns>
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

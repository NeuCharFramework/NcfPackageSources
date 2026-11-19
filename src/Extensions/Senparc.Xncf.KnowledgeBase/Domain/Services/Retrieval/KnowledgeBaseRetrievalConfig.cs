/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: KnowledgeBaseRetrievalConfig.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Text.Json;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static class ChunkingStrategies
    {
        ///Legacy comment translated to English.
        public const string Fixed = "Fixed";
        ///Legacy comment translated to English.
        public const string Recursive = "Recursive";
        ///Legacy comment translated to English.
        public const string Paragraph = "Paragraph";

        public static bool TryParse(string value, out string strategy)
        {
            if (string.Equals(value, Fixed, StringComparison.OrdinalIgnoreCase))
            {
                strategy = Fixed;
                return true;
            }
            if (string.Equals(value, Recursive, StringComparison.OrdinalIgnoreCase))
            {
                strategy = Recursive;
                return true;
            }
            if (string.Equals(value, Paragraph, StringComparison.OrdinalIgnoreCase))
            {
                strategy = Paragraph;
                return true;
            }
            strategy = Fixed;
            return false;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static class RerankModes
    {
        ///Legacy comment translated to English.
        public const string None = "None";
        ///Legacy comment translated to English.
        public const string Lexical = "Lexical";
        ///Legacy comment translated to English.
        public const string Llm = "Llm";

        public static bool TryParse(string value, out string mode)
        {
            if (string.Equals(value, None, StringComparison.OrdinalIgnoreCase))
            {
                mode = None;
                return true;
            }
            if (string.Equals(value, Lexical, StringComparison.OrdinalIgnoreCase))
            {
                mode = Lexical;
                return true;
            }
            if (string.Equals(value, Llm, StringComparison.OrdinalIgnoreCase))
            {
                mode = Llm;
                return true;
            }
            mode = None;
            return false;
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class KnowledgeBaseRetrievalConfig
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        ///Legacy comment translated to English.
        public string ChunkingStrategy { get; set; } = ChunkingStrategies.Fixed;

        ///Legacy comment translated to English.
        public int ChunkSize { get; set; } = 800;

        ///Legacy comment translated to English.
        public int ChunkOverlap { get; set; } = 120;

        ///Legacy comment translated to English.
        public string RerankMode { get; set; } = RerankModes.None;

        ///Legacy comment translated to English.
        public int RerankModelId { get; set; }

        ///Legacy comment translated to English.
        public int RerankCandidateCount { get; set; } = 20;

        ///Legacy comment translated to English.
        public double LexicalWeight { get; set; } = 0.35;

        ///Legacy comment translated to English.
        public static KnowledgeBaseRetrievalConfig Default => new();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static KnowledgeBaseRetrievalConfig FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new KnowledgeBaseRetrievalConfig();
            }
            try
            {
                var config = JsonSerializer.Deserialize<KnowledgeBaseRetrievalConfig>(json, JsonOptions);
                return (config ?? new KnowledgeBaseRetrievalConfig()).Sanitized();
            }
            catch
            {
                //Legacy comment translated to English.
                return new KnowledgeBaseRetrievalConfig();
            }
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, JsonOptions);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public KnowledgeBaseRetrievalConfig Sanitized()
        {
            ChunkingStrategies.TryParse(ChunkingStrategy, out var strategy);
            ChunkingStrategy = strategy;
            ChunkSize = Math.Clamp(ChunkSize, 100, 8000);
            ChunkOverlap = Math.Clamp(ChunkOverlap, 0, ChunkSize - 10);
            RerankModes.TryParse(RerankMode, out var mode);
            RerankMode = mode;
            RerankModelId = Math.Max(0, RerankModelId);
            RerankCandidateCount = Math.Clamp(RerankCandidateCount, 5, 100);
            LexicalWeight = Math.Clamp(LexicalWeight, 0d, 1d);
            return this;
        }

        ///Legacy comment translated to English.
        public bool IsDefault()
        {
            var d = Default;
            return string.Equals(ChunkingStrategy, d.ChunkingStrategy, StringComparison.OrdinalIgnoreCase)
                   && ChunkSize == d.ChunkSize
                   && ChunkOverlap == d.ChunkOverlap
                   && string.Equals(RerankMode, d.RerankMode, StringComparison.OrdinalIgnoreCase)
                   && RerankModelId == d.RerankModelId
                   && RerankCandidateCount == d.RerankCandidateCount
                   && Math.Abs(LexicalWeight - d.LexicalWeight) < 1e-9;
        }

        public KnowledgeBaseRetrievalConfig Clone()
        {
            return new KnowledgeBaseRetrievalConfig
            {
                ChunkingStrategy = ChunkingStrategy,
                ChunkSize = ChunkSize,
                ChunkOverlap = ChunkOverlap,
                RerankMode = RerankMode,
                RerankModelId = RerankModelId,
                RerankCandidateCount = RerankCandidateCount,
                LexicalWeight = LexicalWeight
            };
        }
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public sealed class RecallOptions
    {
        ///Legacy comment translated to English.
        public string RerankMode { get; set; }

        ///Legacy comment translated to English.
        public int? CandidateCount { get; set; }

        ///Legacy comment translated to English.
        public int? RerankModelId { get; set; }
    }
}

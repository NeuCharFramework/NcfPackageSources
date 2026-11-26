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
    /// <summary>Defines supported knowledge-base chunking strategies.</summary>
    public static class ChunkingStrategies
    {
        /// Fixed-size chunking with configured overlap.
        public const string Fixed = "Fixed";
        /// Recursive separator-aware chunking.
        public const string Recursive = "Recursive";
        /// Paragraph-first chunking with recursive fallback.
        public const string Paragraph = "Paragraph";

        /// <summary>
        /// Parses a chunking strategy name, defaulting to <see cref="Fixed"/> when unknown.
        /// </summary>
        /// <param name="value">The strategy name.</param>
        /// <param name="strategy">Receives the canonical strategy name.</param>
        /// <returns><see langword="true"/> when the value is recognized.</returns>
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

    /// <summary>Defines supported retrieval reranking modes.</summary>
    public static class RerankModes
    {
        /// Disables reranking after vector retrieval.
        public const string None = "None";
        /// Applies lexical token-overlap reranking.
        public const string Lexical = "Lexical";
        /// Uses a configured language model to rerank candidates.
        public const string Llm = "Llm";

        /// <summary>
        /// Parses a reranking mode, defaulting to <see cref="None"/> when unknown.
        /// </summary>
        /// <param name="value">The mode name.</param>
        /// <param name="mode">Receives the canonical mode name.</param>
        /// <returns><see langword="true"/> when the value is recognized.</returns>
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
    /// Stores bounded chunking and reranking settings for knowledge-base retrieval.
    /// </summary>
    public sealed class KnowledgeBaseRetrievalConfig
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// Gets or sets the selected chunking strategy.
        public string ChunkingStrategy { get; set; } = ChunkingStrategies.Fixed;

        /// Gets or sets the maximum chunk character count.
        public int ChunkSize { get; set; } = 800;

        /// Gets or sets the overlap character count between adjacent chunks.
        public int ChunkOverlap { get; set; } = 120;

        /// Gets or sets the selected reranking mode.
        public string RerankMode { get; set; } = RerankModes.None;

        /// Gets or sets the reranking model identifier.
        public int RerankModelId { get; set; }

        /// Gets or sets the number of candidates passed to reranking.
        public int RerankCandidateCount { get; set; } = 20;

        /// Gets or sets the lexical contribution used by score fusion.
        public double LexicalWeight { get; set; } = 0.35;

        /// Gets the default sanitized retrieval configuration.
        public static KnowledgeBaseRetrievalConfig Default => new();

        /// <summary>
        /// Deserializes retrieval configuration and sanitizes invalid or out-of-range values.
        /// Invalid JSON returns default settings.
        /// </summary>
        /// <param name="json">The JSON configuration.</param>
        /// <returns>A sanitized retrieval configuration.</returns>
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

        /// <summary>
        /// Serializes this retrieval configuration using the module's camel-case JSON options.
        /// </summary>
        /// <returns>The configuration JSON.</returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, JsonOptions);
        }

        /// <summary>
        /// Clamps numeric settings and canonicalizes strategy/mode names in place.
        /// </summary>
        /// <returns>This configuration instance after sanitization.</returns>
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

        /// <summary>
        /// Determines whether all values equal the default retrieval configuration.</summary>
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

        /// <summary>
        /// Creates a detached copy of this retrieval configuration.
        /// </summary>
        /// <returns>A new configuration with the same values.</returns>
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
    /// Carries optional reranking overrides for one retrieval request.
    /// </summary>
    public sealed class RecallOptions
    {
        /// Gets or sets the requested reranking mode.
        public string RerankMode { get; set; }

        /// Gets or sets the requested candidate count.
        public int? CandidateCount { get; set; }

        /// Gets or sets the reranking model identifier.
        public int? RerankModelId { get; set; }
    }
}

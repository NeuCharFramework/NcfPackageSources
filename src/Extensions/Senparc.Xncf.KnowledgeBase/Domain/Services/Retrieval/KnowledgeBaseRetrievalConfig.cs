/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：KnowledgeBaseRetrievalConfig.cs
    文件功能描述：知识库检索配置（切片策略 + Re-ranking 模式），以 JSON 形式保存在知识库上；
    默认值与旧版行为完全一致，保证向下兼容。


    创建标识：Senparc - 20260926

----------------------------------------------------------------*/

using System;
using System.Text.Json;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval
{
    /// <summary>
    /// 文本切片策略（常量集）
    /// </summary>
    public static class ChunkingStrategies
    {
        /// <summary>固定长度（旧版默认行为：按字符窗口切分）</summary>
        public const string Fixed = "Fixed";
        /// <summary>递归结构化（段落 → 行 → 句子 → 字符，兼顾中英文，长文推荐）</summary>
        public const string Recursive = "Recursive";
        /// <summary>按段落（保留段落边界，短段落自动合并）</summary>
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
    /// Re-ranking 模式（常量集）
    /// </summary>
    public static class RerankModes
    {
        /// <summary>关闭（旧版默认行为：仅向量相似度排序）</summary>
        public const string None = "None";
        /// <summary>词法混合重排：本地计算查询-片段词法重合度并与向量分数融合，无额外模型调用</summary>
        public const string Lexical = "Lexical";
        /// <summary>LLM 重排：使用对话模型对候选片段排序，失败时自动回退到词法混合</summary>
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
    /// 知识库检索配置。序列化后存于 <c>KnowledgeBase.RetrievalConfigJson</c>。
    /// 所有默认值与旧版（固定 800/120 字符切片、无重排）保持一致。
    /// </summary>
    public sealed class KnowledgeBaseRetrievalConfig
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>切片策略：Fixed / Recursive / Paragraph</summary>
        public string ChunkingStrategy { get; set; } = ChunkingStrategies.Fixed;

        /// <summary>切片目标大小（字符数）</summary>
        public int ChunkSize { get; set; } = 800;

        /// <summary>相邻切片重叠（字符数）</summary>
        public int ChunkOverlap { get; set; } = 120;

        /// <summary>重排模式：None / Lexical / Llm</summary>
        public string RerankMode { get; set; } = RerankModes.None;

        /// <summary>LLM 重排使用的对话模型 Id；0 表示回退到知识库的对话模型</summary>
        public int RerankModelId { get; set; }

        /// <summary>重排候选池大小（向量检索先取该数量的候选，再重排出 TopK）</summary>
        public int RerankCandidateCount { get; set; } = 20;

        /// <summary>词法混合重排中词法分数的融合权重（0-1）</summary>
        public double LexicalWeight { get; set; } = 0.35;

        /// <summary>旧版等价默认配置</summary>
        public static KnowledgeBaseRetrievalConfig Default => new();

        /// <summary>
        /// 从 JSON 解析；null / 空 / 非法 JSON 均返回默认配置（绝不抛出）。
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
                // 配置损坏时回退默认行为，而不是让召回整体失败
                return new KnowledgeBaseRetrievalConfig();
            }
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, JsonOptions);
        }

        /// <summary>
        /// 把数值收敛到安全范围，并归一化枚举取值；返回自身便于链式调用。
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

        /// <summary>是否为与旧版行为完全一致的默认配置</summary>
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
    /// 单次召回调用的可选覆盖项（不修改知识库保存的配置）。
    /// </summary>
    public sealed class RecallOptions
    {
        /// <summary>重排模式覆盖；null/空 表示跟随知识库配置</summary>
        public string RerankMode { get; set; }

        /// <summary>重排候选池覆盖；null/≤0 表示跟随知识库配置</summary>
        public int? CandidateCount { get; set; }

        /// <summary>LLM 重排模型覆盖；null/≤0 表示跟随知识库配置</summary>
        public int? RerankModelId { get; set; }
    }
}

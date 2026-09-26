/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：TextChunker.cs
    文件功能描述：知识库文本切片器。提供固定长度（旧版行为）、递归结构化、按段落三种策略；
    切片大小与重叠由 KnowledgeBaseRetrievalConfig 配置。


    创建标识：Senparc - 20260926

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval
{
    /// <summary>
    /// 文本切片。所有策略保证：不丢失正文内容、不产生纯空白切片、结果确定（相同输入得到相同输出）。
    /// </summary>
    public static class TextChunker
    {
        /// <summary>
        /// 递归策略的分隔符优先级：段落 → 行 → 中英文句读 → 逗号 → 空格 → 单字符。
        /// </summary>
        private static readonly string[] RecursiveSeparators =
        {
            "\n\n", "\n", "。", "！", "？", "!", "?", "；", ";", "，", ",", " "
        };

        /// <summary>
        /// 按知识库配置的切片策略切分文本。
        /// </summary>
        public static IReadOnlyList<string> Split(string text, KnowledgeBaseRetrievalConfig config)
        {
            var effective = (config ?? KnowledgeBaseRetrievalConfig.Default).Clone().Sanitized();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            List<string> chunks;
            switch (effective.ChunkingStrategy)
            {
                case ChunkingStrategies.Recursive:
                    chunks = SplitRecursive(text, effective.ChunkSize, effective.ChunkOverlap);
                    break;
                case ChunkingStrategies.Paragraph:
                    chunks = SplitParagraph(text, effective.ChunkSize, effective.ChunkOverlap);
                    break;
                default:
                    chunks = SplitFixed(text, effective.ChunkSize, effective.ChunkOverlap);
                    break;
            }

            return chunks
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .ToList();
        }

        /// <summary>
        /// 固定长度切片（与旧版 SplitText 行为完全一致：字符窗口 + 重叠）。
        /// </summary>
        public static List<string> SplitFixed(string text, int chunkSize, int overlap)
        {
            var chunks = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return chunks;
            }

            for (var i = 0; i < text.Length; i += (chunkSize - overlap))
            {
                var length = Math.Min(chunkSize, text.Length - i);
                if (length <= 0)
                {
                    break;
                }

                chunks.Add(text.Substring(i, length));

                // 防止死循环（如果 overlap >= chunkSize）
                if (chunkSize - overlap <= 0)
                {
                    break;
                }
            }

            return chunks;
        }

        /// <summary>
        /// 递归结构化切片：优先在语义边界（段落/行/句子）断开，仅在无法进一步切分时退回字符窗口；
        /// 相邻切片保留 overlap 个字符的重叠，避免上下文在边界处丢失。
        /// </summary>
        public static List<string> SplitRecursive(string text, int chunkSize, int overlap)
        {
            var normalized = NormalizeLineEndings(text);
            var fragments = SplitIntoFragments(normalized, chunkSize, RecursiveSeparators, 0);
            return MergeWithOverlap(fragments, chunkSize, overlap);
        }

        /// <summary>
        /// 按段落切片：以空行划分段落，短段落合并到不超过 chunkSize；
        /// 超长段落内部改用递归结构化切分。
        /// </summary>
        public static List<string> SplitParagraph(string text, int chunkSize, int overlap)
        {
            var normalized = NormalizeLineEndings(text);
            var paragraphs = normalized
                .Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(z => z.Trim())
                .Where(z => z.Length > 0)
                .ToList();

            var fragments = new List<string>();
            foreach (var paragraph in paragraphs)
            {
                if (paragraph.Length <= chunkSize)
                {
                    fragments.Add(paragraph);
                    continue;
                }

                // 单段超长：在段内做递归结构化切分
                fragments.AddRange(SplitIntoFragments(paragraph, chunkSize, RecursiveSeparators, 0));
            }

            return MergeWithOverlap(fragments, chunkSize, overlap);
        }

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        /// <summary>
        /// 按分隔符优先级递归拆分：能容纳在 chunkSize 内的片段保留，更大的片段用下一级分隔符继续拆。
        /// </summary>
        private static List<string> SplitIntoFragments(
            string text,
            int chunkSize,
            IReadOnlyList<string> separators,
            int separatorIndex)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return result;
            }
            if (text.Length <= chunkSize)
            {
                result.Add(text);
                return result;
            }
            if (separatorIndex >= separators.Count)
            {
                // 已到最低优先级：按字符窗口硬切（保留完整内容）
                for (var i = 0; i < text.Length; i += chunkSize)
                {
                    result.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
                }
                return result;
            }

            var separator = separators[separatorIndex];
            var parts = SplitWithSeparator(text, separator);
            foreach (var part in parts)
            {
                if (part.Length <= chunkSize)
                {
                    result.Add(part);
                    continue;
                }
                result.AddRange(SplitIntoFragments(part, chunkSize, separators, separatorIndex + 1));
            }

            return result;
        }

        /// <summary>
        /// 按分隔符拆分，分隔符保留在前一段末尾，避免丢失标点与换行。
        /// </summary>
        private static List<string> SplitWithSeparator(string text, string separator)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(separator))
            {
                for (var i = 0; i < text.Length; i++)
                {
                    result.Add(text[i].ToString());
                }
                return result;
            }

            var index = 0;
            while (true)
            {
                var found = text.IndexOf(separator, index, StringComparison.Ordinal);
                if (found < 0)
                {
                    if (index < text.Length)
                    {
                        result.Add(text.Substring(index));
                    }
                    break;
                }

                result.Add(text.Substring(index, found + separator.Length - index));
                index = found + separator.Length;
            }

            return result;
        }

        /// <summary>
        /// 把碎片贪心合并为不超过 chunkSize 的切片，并在相邻切片之间保留 overlap 字符重叠。
        /// </summary>
        private static List<string> MergeWithOverlap(IReadOnlyList<string> fragments, int chunkSize, int overlap)
        {
            var chunks = new List<string>();
            var current = new StringBuilder();
            foreach (var fragment in fragments)
            {
                if (fragment.Length > chunkSize)
                {
                    if (current.Length > 0)
                    {
                        chunks.Add(current.ToString());
                        current.Clear();
                    }
                    for (var i = 0; i < fragment.Length; i += Math.Max(1, chunkSize - overlap))
                    {
                        chunks.Add(fragment.Substring(i, Math.Min(chunkSize, fragment.Length - i)));
                    }
                    continue;
                }

                if (current.Length > 0 && current.Length + fragment.Length > chunkSize)
                {
                    var completed = current.ToString();
                    chunks.Add(completed);
                    current.Clear();
                    if (overlap > 0)
                    {
                        var tailStart = Math.Max(0, completed.Length - overlap);
                        current.Append(completed, tailStart, completed.Length - tailStart);
                    }
                }

                current.Append(fragment);
            }

            if (current.Length > 0)
            {
                chunks.Add(current.ToString());
            }

            return chunks;
        }
    }
}

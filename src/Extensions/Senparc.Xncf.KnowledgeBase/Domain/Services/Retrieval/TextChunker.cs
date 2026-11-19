/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TextChunker.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

    Modified by: Senparc - 20261119
    Change description: v0.8.4 Localized knowledge base management and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static class TextChunker
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private static readonly string[] RecursiveSeparators =
        {
            "\n\n", "\n", "。", "！", "？", "!", "?", "；", ";", "，", ",", " "
        };

        /// <summary>
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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

                //Legacy comment translated to English.
                if (chunkSize - overlap <= 0)
                {
                    break;
                }
            }

            return chunks;
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        public static List<string> SplitRecursive(string text, int chunkSize, int overlap)
        {
            var normalized = NormalizeLineEndings(text);
            var fragments = SplitIntoFragments(normalized, chunkSize, RecursiveSeparators, 0);
            return MergeWithOverlap(fragments, chunkSize, overlap);
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
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

                //Legacy comment translated to English.
                fragments.AddRange(SplitIntoFragments(paragraph, chunkSize, RecursiveSeparators, 0));
            }

            return MergeWithOverlap(fragments, chunkSize, overlap);
        }

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        /// <summary>
        ///Legacy comment translated to English.
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
                //Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
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

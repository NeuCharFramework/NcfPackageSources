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

    Modified by: Senparc - 20261127
    Change description: v0.8.4.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval
{
    /// <summary>
    /// Splits knowledge-base text into bounded chunks using fixed, recursive, or paragraph-aware strategies.
    /// </summary>
    public static class TextChunker
    {
        /// <summary>
        /// Gets the separators tried from broad paragraph boundaries to narrower punctuation.
        /// </summary>
        private static readonly string[] RecursiveSeparators =
        {
            "\n\n", "\n", "。", "！", "？", "!", "?", "；", ";", "，", ",", " "
        };

        /// <summary>
        /// Splits text according to the sanitized retrieval configuration.
        /// </summary>
        /// <param name="text">The source text to split.</param>
        /// <param name="config">The chunking configuration; null uses the default configuration.</param>
        /// <returns>Non-empty chunks in source order.</returns>
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
        /// Splits text into fixed-size windows with the requested overlap.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <param name="chunkSize">The maximum chunk length.</param>
        /// <param name="overlap">The number of trailing characters reused in the next chunk.</param>
        /// <returns>The fixed-size chunks.</returns>
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

                // Prevent an invalid overlap configuration from creating a zero-progress loop.
                if (chunkSize - overlap <= 0)
                {
                    break;
                }
            }

            return chunks;
        }

        /// <summary>
        /// Recursively splits text at progressively smaller natural separators before applying overlap.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <param name="chunkSize">The maximum chunk length.</param>
        /// <param name="overlap">The trailing overlap length.</param>
        /// <returns>Recursively generated chunks.</returns>
        public static List<string> SplitRecursive(string text, int chunkSize, int overlap)
        {
            var normalized = NormalizeLineEndings(text);
            var fragments = SplitIntoFragments(normalized, chunkSize, RecursiveSeparators, 0);
            return MergeWithOverlap(fragments, chunkSize, overlap);
        }

        /// <summary>
        /// Splits paragraphs first, then recursively splits paragraphs that exceed the chunk size.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <param name="chunkSize">The maximum chunk length.</param>
        /// <param name="overlap">The trailing overlap length.</param>
        /// <returns>Paragraph-oriented chunks.</returns>
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

        /// <summary>
        /// Normalizes CRLF and CR line endings to LF.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <returns>The normalized text.</returns>
        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        /// <summary>
        /// Recursively divides text using the supplied separator hierarchy.
        /// </summary>
        /// <param name="text">The text fragment to divide.</param>
        /// <param name="chunkSize">The maximum fragment length.</param>
        /// <param name="separators">Separators tried from broadest to narrowest.</param>
        /// <param name="separatorIndex">The current separator position.</param>
        /// <returns>Fragments that do not exceed the requested size.</returns>
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
        /// Splits text after each occurrence of a separator while retaining the separator
        /// at the end of the preceding fragment.
        /// </summary>
        /// <param name="text">The text to split.</param>
        /// <param name="separator">The separator string.</param>
        /// <returns>The separator-preserving fragments.</returns>
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
        /// Merges fragments into bounded chunks and carries the configured trailing overlap
        /// into the next chunk.
        /// </summary>
        /// <param name="fragments">The fragments to merge.</param>
        /// <param name="chunkSize">The maximum chunk length.</param>
        /// <param name="overlap">The trailing overlap length.</param>
        /// <returns>The merged chunks.</returns>
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

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: LexicalReranker.cs
    File description: Related implementation.
    Legacy comment translated to English.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261103
    Change description: v0.8.0 upgrade KnowledgeBase retrieval with configurable chunking and re-ranking (backward compatible)

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public static class LexicalReranker
    {
        private static readonly Regex LatinWordRegex = new(
            @"[A-Za-z][A-Za-z0-9_\-]{1,}|[0-9]+(?:\.[0-9]+)?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static double Score(string query, string text)
        {
            if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(text))
            {
                return 0d;
            }

            var queryTokens = Tokenize(query);
            if (queryTokens.Count == 0)
            {
                return 0d;
            }

            var textTokens = Tokenize(text);
            var hits = 0;
            foreach (var token in queryTokens)
            {
                if (textTokens.Contains(token))
                {
                    hits++;
                }
            }

            var coverage = (double)hits / queryTokens.Count;

            var queryBigrams = TokenizeBigrams(query);
            var textBigrams = TokenizeBigrams(text);
            var bigramDice = DiceCoefficient(queryBigrams, textBigrams);

            return Math.Clamp(0.65 * coverage + 0.35 * bigramDice, 0d, 1d);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static double Fuse(double normalizedVectorScore, double lexicalScore, double lexicalWeight)
        {
            var weight = Math.Clamp(lexicalWeight, 0d, 1d);
            return Math.Clamp((1 - weight) * normalizedVectorScore + weight * lexicalScore, 0d, 1d);
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static double[] NormalizeScores(double[] scores)
        {
            var normalized = new double[scores.Length];
            if (scores.Length == 0)
            {
                return normalized;
            }
            var min = double.MaxValue;
            var max = double.MinValue;
            foreach (var score in scores)
            {
                if (score < min) min = score;
                if (score > max) max = score;
            }
            var span = max - min;
            for (var i = 0; i < scores.Length; i++)
            {
                normalized[i] = span <= 1e-9 ? 1d : (scores[i] - min) / span;
            }
            return normalized;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static HashSet<string> Tokenize(string text)
        {
            var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match match in LatinWordRegex.Matches(text))
            {
                var word = match.Value.ToLowerInvariant();
                if (word.Length >= 2)
                {
                    tokens.Add(word);
                }
            }

            foreach (var run in ExtractCjkRuns(text))
            {
                if (run.Length == 1)
                {
                    tokens.Add(run.ToString());
                    continue;
                }
                for (var i = 0; i + 1 < run.Length; i++)
                {
                    tokens.Add(run.AsSpan(i, 2).ToString());
                }
            }

            return tokens;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static HashSet<string> TokenizeBigrams(string text)
        {
            var bigrams = new HashSet<string>(StringComparer.Ordinal);
            foreach (var run in ExtractCjkRuns(text))
            {
                for (var i = 0; i + 1 < run.Length; i++)
                {
                    bigrams.Add(run.AsSpan(i, 2).ToString());
                }
            }
            return bigrams;
        }

        private static double DiceCoefficient(HashSet<string> left, HashSet<string> right)
        {
            if (left.Count == 0 || right.Count == 0)
            {
                return 0d;
            }
            var intersection = 0;
            var (small, large) = left.Count <= right.Count ? (left, right) : (right, left);
            foreach (var item in small)
            {
                if (large.Contains(item))
                {
                    intersection++;
                }
            }
            return 2.0 * intersection / (left.Count + right.Count);
        }

        private static IEnumerable<string> ExtractCjkRuns(string text)
        {
            var buffer = new StringBuilder(16);
            foreach (var ch in text)
            {
                if (IsCjk(ch))
                {
                    buffer.Append(ch);
                    continue;
                }
                if (buffer.Length > 0)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }
            }
            if (buffer.Length > 0)
            {
                yield return buffer.ToString();
            }
        }

        private static bool IsCjk(char ch)
        {
            return (ch >= 0x4E00 && ch <= 0x9FFF)     //Legacy comment translated to English.
                   || (ch >= 0x3400 && ch <= 0x4DBF)   // extension A
                   || (ch >= 0x3040 && ch <= 0x30FF)   //Legacy comment translated to English.
                   || (ch >= 0xAC00 && ch <= 0xD7AF)   //Legacy comment translated to English.
                   || (ch >= 0xF900 && ch <= 0xFAFF);  //Legacy comment translated to English.
        }
    }
}

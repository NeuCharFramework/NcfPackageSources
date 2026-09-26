/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：LexicalReranker.cs
    文件功能描述：本地词法重排器。基于查询-片段词元重合度（拉丁词 + CJK 字符二元组）
    计算确定性相关度分数，并与向量相似度融合；无任何模型调用与第三方依赖。


    创建标识：Senparc - 20260926

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Senparc.Xncf.KnowledgeBase.Domain.Services.Retrieval
{
    /// <summary>
    /// 词法混合重排。适用于“查询包含专有名词、编号、英文术语”时向量召回排序不佳的场景。
    /// </summary>
    public static class LexicalReranker
    {
        private static readonly Regex LatinWordRegex = new(
            @"[A-Za-z][A-Za-z0-9_\-]{1,}|[0-9]+(?:\.[0-9]+)?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// 计算片段对查询的词法相关度，取值 0-1（越大越相关）。
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
        /// 融合向量分数与词法分数。vectorScore 需先做 min-max 归一化（可先用 <see cref="NormalizeScores"/>）。
        /// </summary>
        public static double Fuse(double normalizedVectorScore, double lexicalScore, double lexicalWeight)
        {
            var weight = Math.Clamp(lexicalWeight, 0d, 1d);
            return Math.Clamp((1 - weight) * normalizedVectorScore + weight * lexicalScore, 0d, 1d);
        }

        /// <summary>
        /// 对一组分数做 min-max 归一化；全部相同时统一返回 1。
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
        /// 提取词元：拉丁/数字词（小写、长度≥2）+ CJK 字符二元组（单字 CJK 段取一元组）。
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
        /// 仅提取 CJK 字符二元组集合（用于 Dice 系数）。
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
            return (ch >= 0x4E00 && ch <= 0x9FFF)     // CJK 统一表意文字
                   || (ch >= 0x3400 && ch <= 0x4DBF)   // 扩展 A
                   || (ch >= 0x3040 && ch <= 0x30FF)   // 日文假名
                   || (ch >= 0xAC00 && ch <= 0xD7AF)   // 韩文音节
                   || (ch >= 0xF900 && ch <= 0xFAFF);  // 兼容表意文字
        }
    }
}

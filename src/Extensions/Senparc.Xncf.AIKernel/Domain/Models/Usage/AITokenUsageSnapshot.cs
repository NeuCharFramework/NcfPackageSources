/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenUsageSnapshot.cs
    File description: Related implementation.

    Created by: Senparc - 20260904

    Modified by: Senparc - 20260915
    Change description: v0.16.0 Updated module behavior and related support.

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.AIKernel.Domain.Models.Usage
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    [Serializable]
    public class AITokenUsageSnapshot
    {
        /// <summary>
        /// input (Prompt) token count
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        /// output (Completion) token count
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        /// total token count
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        /// cached input token count
        /// </summary>
        public long CachedInputTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long ReasoningTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static AITokenUsageSnapshot Empty => new AITokenUsageSnapshot();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsEmpty =>
            InputTokens == 0 && OutputTokens == 0 && TotalTokens == 0
            && CachedInputTokens == 0 && ReasoningTokens == 0;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public void Normalize()
        {
            InputTokens = Math.Max(0, InputTokens);
            OutputTokens = Math.Max(0, OutputTokens);
            CachedInputTokens = Math.Max(0, CachedInputTokens);
            ReasoningTokens = Math.Max(0, ReasoningTokens);
            if (TotalTokens <= 0)
            {
                TotalTokens = InputTokens + OutputTokens;
            }
        }
    }
}

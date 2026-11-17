/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenModelUsage.cs
    File description: Related implementation.

    Created by: Senparc - 20260905

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
    public class AITokenModelUsage
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long Calls { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime? LastUseTime { get; set; }
    }
}

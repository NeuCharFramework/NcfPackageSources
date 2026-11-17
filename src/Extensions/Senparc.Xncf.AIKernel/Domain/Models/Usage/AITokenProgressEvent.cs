/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AITokenProgressEvent.cs
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
    public enum AITokenProgressStatus
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Running = 0,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Completed = 1,
        /// <summary>
        /// failure
        /// </summary>
        Failed = 2,
    }

    /// <summary>
    ///Legacy comment translated to English.
    /// <para>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </para>
    /// </summary>
    [Serializable]
    public class AITokenProgressEvent
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public Guid RunId { get; set; }

        /// <summary>
        /// model code
        /// </summary>
        public string ModelAlias { get; set; }

        /// <summary>
        /// model name
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public AITokenProgressStatus Status { get; set; } = AITokenProgressStatus.Running;

        /// <summary>
        /// input (Prompt) token count
        /// </summary>
        public long InputTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public long OutputTokens { get; set; }

        /// <summary>
        /// total token count
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string OutputPreview { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int ElapsedMs { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool IsComplete => Status == AITokenProgressStatus.Completed || Status == AITokenProgressStatus.Failed;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Error { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}

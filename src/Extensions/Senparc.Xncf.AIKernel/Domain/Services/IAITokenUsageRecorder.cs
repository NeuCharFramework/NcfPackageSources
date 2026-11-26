/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IAITokenUsageRecorder.cs
    File description: Related implementation.

    Created by: Senparc - 20261011

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add AITokenUsageRecorder and related services for token usage tracking

    Modified by: Senparc - 20261119
    Change description: v0.17.4 Localized AI fine-tuning and monitoring surfaces

----------------------------------------------------------------*/

using Senparc.Xncf.AIKernel.Domain.Models.Usage;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    /// <summary>
    /// Accepts normalized AI token-usage records for live monitoring and asynchronous persistence.
    /// </summary>
    public interface IAITokenUsageRecorder
    {
        /// <summary>
        /// Queues one token-usage record for monitoring and persistence.
        /// </summary>
        /// <param name="record">The normalized usage record.</param>
        void Record(AITokenUsageRecord record);
    }
}

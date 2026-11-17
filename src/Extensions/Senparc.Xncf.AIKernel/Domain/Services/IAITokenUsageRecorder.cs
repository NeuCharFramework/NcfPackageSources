/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IAITokenUsageRecorder.cs
    File description: Related implementation.

    Created by: Senparc - 20261011

    Modified by: Senparc - 20261021
    Change description: v0.17.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.17.0 add AITokenUsageRecorder and related services for token usage tracking

----------------------------------------------------------------*/

using Senparc.Xncf.AIKernel.Domain.Models.Usage;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    /// <summary>
    ///Legacy comment translated to English.
    ///Legacy comment translated to English.
    /// </summary>
    public interface IAITokenUsageRecorder
    {
        void Record(AITokenUsageRecord record);
    }
}

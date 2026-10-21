/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：IAITokenUsageRecorder.cs
    文件功能描述：统一 Token 用量记录接口

    创建标识：Senparc - 20261011

    修改标识：Senparc - 20261021
    修改描述：v0.17.0 定义 Token 用量记录入口

----------------------------------------------------------------*/

using Senparc.Xncf.AIKernel.Domain.Models.Usage;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
    /// <summary>
    /// AI 调用 Token 用量记录入口。
    /// <para>调用方只负责提交记录，持久化由后台队列异步完成。</para>
    /// </summary>
    public interface IAITokenUsageRecorder
    {
        void Record(AITokenUsageRecord record);
    }
}

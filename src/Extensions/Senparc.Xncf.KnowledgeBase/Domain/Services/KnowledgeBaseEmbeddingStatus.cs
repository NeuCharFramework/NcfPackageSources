/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：KnowledgeBaseEmbeddingStatus.cs
    文件功能描述：知识库向量发布状态

    修改标识：Senparc - 20260815
    修改描述：v0.7.0 增强知识库向量发布状态识别

----------------------------------------------------------------*/

namespace Senparc.Xncf.KnowledgeBase.Domain.Services
{
    /// <summary>
    /// <para>Published：当前隔离集合已完整发布，可供 Agent RAG 使用。</para>
    /// <para>Legacy：旧版切片已有向量，但尚未发布到新版隔离集合。</para>
    /// <para>Pending：没有已知的向量化结果。</para>
    /// </summary>
    public enum KnowledgeBaseEmbeddingStatus
    {
        Pending,
        Legacy,
        Published
    }
}

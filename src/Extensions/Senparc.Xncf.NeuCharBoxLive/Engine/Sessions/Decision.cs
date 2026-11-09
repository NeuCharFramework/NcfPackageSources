/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：Decision.cs
    文件功能描述：访客对计划或重新规划卡片的回答（批准或拒绝）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.Engine.Sessions;

/// <summary>The visitor's answer to a plan or re-plan card.</summary>
public enum Decision { Approve, Decline }

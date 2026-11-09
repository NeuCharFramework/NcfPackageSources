/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：MSTestSettings.cs
    文件功能描述：MSTest 全局设置（并行方式等）


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

[assembly: Parallelize(Workers = 2, Scope = ExecutionScope.MethodLevel)]

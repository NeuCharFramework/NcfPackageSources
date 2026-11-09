/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：MSTestSettings.cs
    文件功能描述：NeuCharBox Live 测试：MSTestSettings


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

[assembly: Parallelize(Workers = 2, Scope = ExecutionScope.MethodLevel)]

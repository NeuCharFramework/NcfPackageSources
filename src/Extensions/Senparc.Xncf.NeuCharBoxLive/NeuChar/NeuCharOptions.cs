/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NeuCharOptions.cs
    文件功能描述：NeuCharBox Live NeuChar 模型客户端：NeuCharOptions


    创建标识：Senparc - 20261108

    修改标识：Senparc - 20261108
    修改描述：v0.1.0-preview1 首次加入 NcfPackageSources（从 ncb/live 迁移）

----------------------------------------------------------------*/

namespace Senparc.Xncf.NeuCharBoxLive.NeuChar;

/// <summary>
/// One NeuChar deployment: the endpoint (https://www.neuchar.com/19194/) and the model id (gpt-5-chat). The key is
/// never a member, so ToString() and logs can't carry it; it is passed to the factory separately.
/// </summary>
public record NeuCharOptions(string Endpoint, string Model);

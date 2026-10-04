/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：WeixinClawQrCodeRenderer.cs
    文件功能描述：WeixinClawQrCodeRenderer.cs implementation and project behavior.


    创建标识：Senparc - 20260926

    修改标识：Senparc - 20261005
    修改描述：v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

----------------------------------------------------------------*/

using QRCoder;
using System;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public static class WeixinClawQrCodeRenderer
{
    public static string RenderDataUri(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("QR content is required.", nameof(content));
        }

        var pngBytes = PngByteQRCodeHelper.GetQRCode(
            content,
            QRCodeGenerator.ECCLevel.Q,
            12);
        return "data:image/png;base64," + Convert.ToBase64String(pngBytes);
    }
}

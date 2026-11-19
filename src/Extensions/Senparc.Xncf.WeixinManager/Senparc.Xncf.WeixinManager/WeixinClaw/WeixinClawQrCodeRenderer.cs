/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinClawQrCodeRenderer.cs
    File description: WeixinClawQrCodeRenderer.cs implementation and project behavior.

    Created by: Senparc - 20260926

    Modified by: Senparc - 20261005
    Change description: v0.24.9 0.24.9 Merge branch 'Developer-MAF-V3-Spark' of https://github.com/NeuCharFramework/NcfPackageSources into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.24.16 enhance WeixinClaw integration with new message handling and UI improvements

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

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

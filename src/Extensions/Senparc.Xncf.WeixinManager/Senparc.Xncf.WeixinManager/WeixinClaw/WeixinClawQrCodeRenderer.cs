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

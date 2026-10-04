using System;
using System.IO;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public static class WeixinClawMediaFormat
{
    public static bool IsSilk(ReadOnlySpan<byte> bytes) =>
        bytes.StartsWith("#!SILK_V3"u8)
        || (bytes.Length > 1 && bytes[0] == 2 && bytes[1..].StartsWith("#!SILK_V3"u8));

    public static string GetContentType(string kind, string fileName, ReadOnlySpan<byte> bytes)
    {
        if (IsSilk(bytes)) return "audio/silk";
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return "image/jpeg";
        if (bytes.StartsWith("ID3"u8)
            || (bytes.Length >= 2 && bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0))
        {
            return "audio/mpeg";
        }
        if (bytes.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 })) return "image/png";
        if (bytes.StartsWith("GIF8"u8)) return "image/gif";
        if (bytes.Length >= 12)
        {
            if (bytes.StartsWith("RIFF"u8))
            {
                if (bytes[8..].StartsWith("WAVE"u8)) return "audio/wav";
                if (bytes[8..].StartsWith("WEBP"u8)) return "image/webp";
            }
            if (bytes[4..].StartsWith("ftyp"u8))
            {
                return kind == "voice" ? "audio/mp4" : "video/mp4";
            }
        }

        return Path.GetExtension(fileName)?.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".silk" => "audio/silk",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".mp4" => "video/mp4",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }
}

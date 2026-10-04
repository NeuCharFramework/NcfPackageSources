using SilkSharp.Codec;
using SilkSharp.Exception;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.WeixinClaw;

public sealed class WeixinClawMediaPlaybackService
{
    public const int VoiceSampleRate = 24000;
    private const int MaximumVoiceBytes = 50 * 1024 * 1024;
    private const int MaximumPacketBytes = 1024 * 5;
    private static readonly SemaphoreSlim ConversionGate = new(1, 1);

    public async Task<WeixinClawPlaybackFile> GetPlayableFileAsync(
        string path,
        string kind,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var header = new byte[16];
        int headerLength;
        await using (var stream = File.OpenRead(path))
        {
            headerLength = await stream.ReadAtLeastAsync(
                header, header.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
        }
        var detectedType = WeixinClawMediaFormat.GetContentType(kind, path, header.AsSpan(0, headerLength));
        if (kind != "voice" || !WeixinClawMediaFormat.IsSilk(header.AsSpan(0, headerLength)))
        {
            if (kind == "voice" && (detectedType == "audio/silk" || contentType == "audio/silk"))
            {
                throw new InvalidDataException("微信语音不是有效的 SILK 文件。");
            }
            return new WeixinClawPlaybackFile(
                path, detectedType == "application/octet-stream" ? contentType ?? detectedType : detectedType);
        }

        if (new FileInfo(path).Length > MaximumVoiceBytes)
        {
            throw new InvalidDataException("微信语音播放转换的输入不能超过 50 MB。");
        }
        var wavPath = path + ".playback.wav";
        await ConversionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var sourceWriteTime = File.GetLastWriteTimeUtc(path);
            if (File.Exists(wavPath) && new FileInfo(wavPath).Length is > 44 and <= MaximumVoiceBytes + 44
                && File.GetLastWriteTimeUtc(wavPath) >= sourceWriteTime)
            {
                return new WeixinClawPlaybackFile(wavPath, "audio/wav");
            }

            var silk = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            ValidateSilkPackets(silk);
            byte[] pcm;
            var temporaryId = Guid.NewGuid().ToString("N");
            var silkPath = wavPath + "." + temporaryId + ".silk.tmp";
            var pcmPath = wavPath + "." + temporaryId + ".pcm.tmp";
            try
            {
                await File.WriteAllBytesAsync(silkPath, silk, cancellationToken).ConfigureAwait(false);
                // SilkSharp 2.0.8's POSIX buffer API reads an output-only open_memstream.
                // Its file API avoids that defect and decodes the validated snapshot.
                await new SilkDecoder { FS_API = VoiceSampleRate, Loss = 0 }
                    .DecodeAsync(silkPath, pcmPath).ConfigureAwait(false);
                if (new FileInfo(pcmPath).Length > MaximumVoiceBytes)
                {
                    throw new InvalidDataException("微信语音解码后的 PCM 数据不能超过 50 MB。");
                }
                pcm = await File.ReadAllBytesAsync(pcmPath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
            {
                throw new InvalidOperationException(
                    "当前运行平台无法加载 SILK 解码器，请检查 SilkSharp 原生运行库是否随宿主发布。", ex);
            }
            catch (SilkDecoderException ex)
            {
                throw new InvalidDataException("微信 SILK 语音解码失败。", ex);
            }
            finally
            {
                if (File.Exists(pcmPath)) File.Delete(pcmPath);
                if (File.Exists(silkPath)) File.Delete(silkPath);
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (pcm.Length == 0 || pcm.Length % 2 != 0)
            {
                throw new InvalidDataException($"微信语音解码后没有有效的 16 位 PCM 数据（{pcm.Length} 字节）。");
            }

            var temporaryPath = wavPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                await using (var stream = new FileStream(
                    temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                {
                    await stream.WriteAsync(CreateWaveHeader(pcm.Length), cancellationToken).ConfigureAwait(false);
                    await stream.WriteAsync(pcm, cancellationToken).ConfigureAwait(false);
                }
                File.SetLastWriteTimeUtc(temporaryPath, sourceWriteTime);
                File.Move(temporaryPath, wavPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            return new WeixinClawPlaybackFile(wavPath, "audio/wav");
        }
        finally
        {
            ConversionGate.Release();
        }
    }

    private static void ValidateSilkPackets(ReadOnlySpan<byte> silk)
    {
        if (silk.Length > MaximumVoiceBytes)
        {
            throw new InvalidDataException("微信语音播放转换的输入不能超过 50 MB。");
        }
        var offset = silk[0] == 2 ? 10 : 9;
        var packetCount = 0;
        while (offset < silk.Length)
        {
            if (silk.Length - offset < 2)
            {
                throw new InvalidDataException("微信 SILK 语音包头不完整。");
            }
            var length = BinaryPrimitives.ReadInt16LittleEndian(silk[offset..]);
            offset += 2;
            if (length == -1 && offset == silk.Length)
            {
                break;
            }
            if (length <= 0 || length > MaximumPacketBytes || length > silk.Length - offset)
            {
                throw new InvalidDataException("微信 SILK 语音包长度无效或文件已截断。");
            }
            offset += length;
            packetCount++;
            if ((long)packetCount * 5 * (VoiceSampleRate / 50) * 2 > MaximumVoiceBytes)
            {
                throw new InvalidDataException("微信语音预计解码后的 PCM 数据超过 50 MB。");
            }
        }
        if (packetCount < 3)
        {
            // The native decoder preloads two packets and needs a third to initialize its jitter buffer.
            throw new InvalidDataException("当前 SILK 解码器至少需要 3 个完整语音数据包。");
        }
    }

    private static byte[] CreateWaveHeader(int pcmLength)
    {
        var header = new byte[44];
        "RIFF"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4), checked((uint)pcmLength + 36));
        "WAVEfmt "u8.CopyTo(header.AsSpan(8));
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(16), 16);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(20), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(22), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(24), VoiceSampleRate);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(28), VoiceSampleRate * 2);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(32), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(34), 16);
        "data"u8.CopyTo(header.AsSpan(36));
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(40), (uint)pcmLength);
        return header;
    }
}

public sealed record WeixinClawPlaybackFile(string Path, string ContentType);

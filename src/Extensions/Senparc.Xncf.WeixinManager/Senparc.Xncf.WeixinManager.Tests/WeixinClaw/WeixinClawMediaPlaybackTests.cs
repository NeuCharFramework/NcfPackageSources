using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Xncf.WeixinManager.WeixinClaw;
using SilkSharp.Codec;
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Xncf.WeixinManager.Tests.WeixinClaw;

[TestClass]
public class WeixinClawMediaPlaybackTests
{
    [TestMethod]
    public async Task GetPlayableFile_DecodesTencentSilkToCachedMonoPcmWaveAndPreservesOriginal()
    {
        var root = Path.Combine(Path.GetTempPath(), "ncf-claw-playback-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "voice.bin");
            var pcm = new byte[WeixinClawMediaPlaybackService.VoiceSampleRate * 2 / 5];
            for (var index = 0; index < pcm.Length / 2; index++)
            {
                BinaryPrimitives.WriteInt16LittleEndian(pcm.AsSpan(index * 2),
                    (short)(Math.Sin(index * 2 * Math.PI * 440 / WeixinClawMediaPlaybackService.VoiceSampleRate) * 10000));
            }
            var encoded = await new SilkEncoder
            {
                FS_API = WeixinClawMediaPlaybackService.VoiceSampleRate, Tencent = true
            }.EncodeAsync(pcm);
            var silk = encoded.Data;
            await File.WriteAllBytesAsync(path, silk);
            var playback = new WeixinClawMediaPlaybackService();

            var results = await Task.WhenAll(
                playback.GetPlayableFileAsync(path, "voice", "application/octet-stream"),
                playback.GetPlayableFileAsync(path, "voice", "audio/silk"));

            Assert.AreEqual(results[0].Path, results[1].Path);
            Assert.AreEqual("audio/wav", results[0].ContentType);
            var wave = await File.ReadAllBytesAsync(results[0].Path);
            AssertWave(wave, expectedMilliseconds: 200);
            Assert.IsTrue(wave[44..].Any(value => value != 0));
            CollectionAssert.AreEqual(silk, await File.ReadAllBytesAsync(path));
            var cached = await playback.GetPlayableFileAsync(path, "voice", "audio/silk");
            CollectionAssert.AreEqual(wave, await File.ReadAllBytesAsync(cached.Path));
            Assert.AreEqual(0, Directory.GetFiles(root, "*.tmp").Length);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task GetPlayableFile_ProvidedLocalSampleDecodesWithoutUploadingOrChangingOriginal()
    {
        var path = Environment.GetEnvironmentVariable("WEIXINCLAW_VOICE_SAMPLE");
        if (string.IsNullOrWhiteSpace(path))
        {
            Assert.Inconclusive("Set WEIXINCLAW_VOICE_SAMPLE to validate a private local recording.");
        }
        var originalHash = SHA256.HashData(await File.ReadAllBytesAsync(path));

        var result = await new WeixinClawMediaPlaybackService().GetPlayableFileAsync(
            path, "voice", "audio/silk");

        var wave = await File.ReadAllBytesAsync(result.Path);
        AssertWave(wave, expectedMilliseconds: 4200);
        Assert.IsTrue(wave[44..].Any(value => value != 0));
        CollectionAssert.AreEqual(originalHash, SHA256.HashData(await File.ReadAllBytesAsync(path)));
        Console.WriteLine($"Local SILK playback: 24000 Hz, mono, 16-bit PCM, {wave.Length - 44} PCM bytes.");
    }

    [DataTestMethod]
    [DataRow("02232153494c4b5f5633")]
    [DataRow("02232153494c4b5f563303")]
    [DataRow("02232153494c4b5f563310000102")]
    [DataRow("02232153494c4b5f56330000")]
    public async Task GetPlayableFile_RejectsTruncatedOrEmptySilkBeforeNativeDecode(string hex)
    {
        var path = Path.Combine(Path.GetTempPath(), "ncf-claw-corrupt-" + Guid.NewGuid().ToString("N") + ".bin");
        await File.WriteAllBytesAsync(path, Convert.FromHexString(hex));
        try
        {
            await Assert.ThrowsExceptionAsync<InvalidDataException>(() =>
                new WeixinClawMediaPlaybackService().GetPlayableFileAsync(path, "voice", "audio/silk"));
            Assert.IsFalse(File.Exists(path + ".playback.wav"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task GetPlayableFile_CancellationDoesNotCreatePartialCache()
    {
        var path = Path.Combine(Path.GetTempPath(), "ncf-claw-canceled-" + Guid.NewGuid().ToString("N") + ".bin");
        await File.WriteAllBytesAsync(path, new byte[16]);
        try
        {
            using var canceled = new CancellationTokenSource();
            canceled.Cancel();
            await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
                new WeixinClawMediaPlaybackService().GetPlayableFileAsync(
                    path, "voice", "audio/silk", canceled.Token));
            Assert.IsFalse(File.Exists(path + ".playback.wav"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [DataTestMethod]
    [DataRow("voice", "voice.bin", "494433040000000000000000", "audio/mpeg")]
    [DataRow("voice", "voice.bin", "524946461000000057415645", "audio/wav")]
    [DataRow("voice", "voice.bin", "02232153494c4b5f56331400", "audio/silk")]
    [DataRow("video", "video.bin", "000000186674797069736f6d", "video/mp4")]
    [DataRow("image", "image.bin", "ffd8ffe000104a4649460001", "image/jpeg")]
    public void MediaFormat_DetectsActualHeadersRatherThanBinExtension(
        string kind, string fileName, string hex, string expected)
    {
        Assert.AreEqual(expected, WeixinClawMediaFormat.GetContentType(kind, fileName, Convert.FromHexString(hex)));
    }

    [TestMethod]
    public async Task GetPlayableFile_VideoKeepsOriginalBytesAndUsesMp4ContentType()
    {
        var path = Path.Combine(Path.GetTempPath(), "ncf-claw-video-" + Guid.NewGuid().ToString("N") + ".bin");
        var bytes = Convert.FromHexString("000000186674797069736f6d00000000");
        await File.WriteAllBytesAsync(path, bytes);
        try
        {
            var result = await new WeixinClawMediaPlaybackService().GetPlayableFileAsync(
                path, "video", "application/octet-stream");
            Assert.AreEqual(path, result.Path);
            Assert.AreEqual("video/mp4", result.ContentType);
            CollectionAssert.AreEqual(bytes, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void AssertWave(byte[] wave, int expectedMilliseconds)
    {
        Assert.IsTrue(wave.Length > 44);
        Assert.AreEqual("RIFF", Encoding.ASCII.GetString(wave, 0, 4));
        Assert.AreEqual("WAVE", Encoding.ASCII.GetString(wave, 8, 4));
        Assert.AreEqual((uint)wave.Length - 8, BinaryPrimitives.ReadUInt32LittleEndian(wave.AsSpan(4)));
        Assert.AreEqual((ushort)1, BinaryPrimitives.ReadUInt16LittleEndian(wave.AsSpan(20)));
        Assert.AreEqual((ushort)1, BinaryPrimitives.ReadUInt16LittleEndian(wave.AsSpan(22)));
        Assert.AreEqual((uint)24000, BinaryPrimitives.ReadUInt32LittleEndian(wave.AsSpan(24)));
        Assert.AreEqual((ushort)16, BinaryPrimitives.ReadUInt16LittleEndian(wave.AsSpan(34)));
        Assert.AreEqual((uint)wave.Length - 44, BinaryPrimitives.ReadUInt32LittleEndian(wave.AsSpan(40)));
        Assert.AreEqual(expectedMilliseconds, (wave.Length - 44) * 1000 / (24000 * 2));
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcTraceHelperTests.cs
    File description: Related implementation.

    Created by: Senparc - 20260809

----------------------------------------------------------------*/

using Senparc.Areas.Admin.SenparcTraceManager;

namespace Senparc.Areas.Admin.Tests.Domain.Services;

[TestClass]
public class SenparcTraceHelperTests
{
    [TestMethod]
    public async Task QueryLogsAsync_ShouldFilterPageAndPreserveMultilineStackTrace()
    {
        var originalPath = SenparcTraceHelper.DefaultLogPath;
        var tempDir = Path.Combine(Path.GetTempPath(), "ncf-trace-query-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            SenparcTraceHelper.DefaultLogPath = tempDir;
            const string date = "20260809";
            var logPath = Path.Combine(tempDir, $"SenparcTrace-{date}.log");
            await File.WriteAllTextAsync(logPath, """
[[[BaseException]]]
[2026/08/09 00:28:37.0674]
[线程：1]
	Message：demo
	StackTrace：at First()
	at Second()

[[[Normal]]]
[2026/08/09 01:00:00.0000]
[线程：2]
normal content

[[[Another]]]
[2026/08/09 02:00:00.0000]
[线程：2]
more content

""");

            var result = await SenparcTraceHelper.QueryLogsAsync(
                serviceProvider: null!,
                date,
                pageIndex: 1,
                pageSize: 1,
                isException: true);
            var detail = await SenparcTraceHelper.GetLogDetailAsync(
                serviceProvider: null!,
                date,
                line: 1);

            Assert.AreEqual(1, result.Total);
            Assert.AreEqual(1, result.Items.Count);
            Assert.AreEqual(SenparcTraceType.Exception, result.Items[0].SenparcTraceType);
            StringAssert.Contains(detail.ExceptionStackTrace, "at First()");
            StringAssert.Contains(detail.ExceptionStackTrace, "at Second()");
        }
        finally
        {
            SenparcTraceHelper.DefaultLogPath = originalPath;
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void GetLogFiles_ShouldTakeOnlyRequestedPage()
    {
        var originalPath = SenparcTraceHelper.DefaultLogPath;
        var tempDir = Path.Combine(Path.GetTempPath(), "ncf-trace-files-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            SenparcTraceHelper.DefaultLogPath = tempDir;
            for (var day = 1; day <= 12; day++)
            {
                File.WriteAllText(Path.Combine(tempDir, $"SenparcTrace-202608{day:D2}.log"), "test");
            }
            File.WriteAllText(Path.Combine(tempDir, "SenparcTrace-invalid.log"), "ignore");

            var result = SenparcTraceHelper.GetLogFiles(pageIndex: 2, pageSize: 5);

            Assert.AreEqual(12, result.Total);
            Assert.AreEqual(5, result.Items.Count);
        }
        finally
        {
            SenparcTraceHelper.DefaultLogPath = originalPath;
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void RedactSensitiveData_ShouldMaskCommonSecrets()
    {
        var content = """{"accessToken":"secret-token","password":"123456"} URL：https://example.com?apiKey=abc Authorization=Bearer jwt.token Cookie=session=xyz""";

        var redacted = SenparcTraceHelper.RedactSensitiveData(content);

        StringAssert.Contains(redacted, @"""accessToken"":""***");
        StringAssert.Contains(redacted, @"""password"":""***");
        StringAssert.Contains(redacted, "apiKey=***");
        StringAssert.Contains(redacted, "Authorization=***");
        StringAssert.Contains(redacted, "Cookie=***");
        Assert.IsFalse(redacted.Contains("secret-token"));
        Assert.IsFalse(redacted.Contains("jwt.token"));
    }

    [TestMethod]
    public async Task QueryLogsAsync_ShouldRejectInvalidDate()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => SenparcTraceHelper.QueryLogsAsync(null!, "../20260809"));
    }

    [TestMethod]
    public async Task GetLogScanResultAsync_ShouldCountWithoutLoadingBodies()
    {
        var originalPath = SenparcTraceHelper.DefaultLogPath;
        var tempDir = Path.Combine(Path.GetTempPath(), "ncf-trace-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            SenparcTraceHelper.DefaultLogPath = tempDir;
            const string date = "20260809";
            var logPath = Path.Combine(tempDir, $"SenparcTrace-{date}.log");

            await File.WriteAllTextAsync(logPath, """
[[[ScanAssambles]]]
[2026/08/09 00:28:36.3626]
[线程：1]
RegisterAllAreas 用时：305.891ms

[[[系统启动]]]
[2026/08/09 00:28:36.3898]
[线程：1]
完成 Area:Admin 注册

[[[BaseException]]]
[2026/08/09 00:28:37.0674]
[线程：1]
	BaseException
	Message：demo

[[[API]]]
[2026/08/09 01:00:00.0000]
[线程：2]
URL：https://example.com/api
Result：
{"ok":true}

[[[Post]]]
[2026/08/09 01:01:00.0000]
[线程：3]
URL：https://example.com/post
Post Data：
{"a":1}

""");

            //Legacy comment translated to English.
            await using (var writer = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            await using (var sw = new StreamWriter(writer))
            {
                await sw.WriteLineAsync("[[[RuntimeAppend]]]");
                await sw.WriteLineAsync("[2026/08/09 01:02:00.0000]");
                await sw.FlushAsync();

                var scan = await SenparcTraceHelper.GetLogScanResultAsync(serviceProvider: null!, date, useCache: false);

                Assert.AreEqual(6, scan.TotalLogCount);
                Assert.AreEqual(1, scan.ExceptionLogCount);
                Assert.AreEqual(5, scan.NormalLogCount);
                Assert.AreEqual(1, scan.TypeCounts[SenparcTraceType.Exception]);
                Assert.AreEqual(1, scan.TypeCounts[SenparcTraceType.GetRequest]);
                Assert.AreEqual(1, scan.TypeCounts[SenparcTraceType.PostRequest]);
                Assert.IsTrue(scan.TypeCounts[SenparcTraceType.Normal] >= 3);
            }
        }
        finally
        {
            SenparcTraceHelper.DefaultLogPath = originalPath;
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // ignore cleanup
            }
        }
    }
}

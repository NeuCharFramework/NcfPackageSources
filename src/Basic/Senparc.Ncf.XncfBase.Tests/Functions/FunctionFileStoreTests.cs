/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FunctionFileStoreTests.cs
    文件功能描述：FunctionRender 文件存储测试

----------------------------------------------------------------*/

using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.Ncf.Core.MultiTenant;
using Senparc.Ncf.XncfBase.Functions.Files;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Tests.Functions;

[TestClass]
public class FunctionFileStoreTests
{
    [TestMethod]
    public async Task SaveManyAsync_ShouldStoreStreamsWithoutLoadingAllFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "ncf-function-file-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = Options.Create(new FunctionFileStoreOptions
            {
                RootPath = root,
                MaxFileBytes = 1024,
                MaxBatchBytes = 2048,
                MaxFilesPerBatch = 3,
                Retention = TimeSpan.FromMinutes(5)
            });
            var store = new LocalFunctionFileStore(options);
            await using var first = new MemoryStream(Encoding.UTF8.GetBytes("first"));
            await using var second = new MemoryStream(Encoding.UTF8.GetBytes("second"));

            var references = await store.SaveManyAsync(new[]
            {
                new FunctionFileUpload { Content = first, FileName = "first.txt", Length = first.Length },
                new FunctionFileUpload { Content = second, FileName = "second.txt", Length = second.Length }
            });

            Assert.AreEqual(2, references.Count);
            Assert.AreEqual("first.txt", references[0].FileName);
            Assert.AreEqual(5, references[0].Length);
            Assert.AreEqual(
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("first"))).ToLowerInvariant(),
                references[0].Sha256);

            await using var content = await store.OpenReadAsync(references[1].FileId);
            using var reader = new StreamReader(content.Content, Encoding.UTF8, leaveOpen: true);
            Assert.AreEqual("second", await reader.ReadToEndAsync());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task SaveManyAsync_ShouldRejectActualBatchSizeForUnknownLengths()
    {
        var root = Path.Combine(Path.GetTempPath(), "ncf-function-file-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalFunctionFileStore(Options.Create(new FunctionFileStoreOptions
            {
                RootPath = root,
                MaxFileBytes = 4,
                MaxBatchBytes = 5,
                MaxFilesPerBatch = 3,
                Retention = TimeSpan.FromMinutes(5)
            }));

            await using var first = new MemoryStream(Encoding.UTF8.GetBytes("123"));
            await using var second = new MemoryStream(Encoding.UTF8.GetBytes("456"));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                store.SaveManyAsync(new[]
                {
                    new FunctionFileUpload { Content = first, FileName = "first.txt" },
                    new FunctionFileUpload { Content = second, FileName = "second.txt" }
                }));

            Assert.IsFalse(Directory.EnumerateFiles(root, "*.bin").Any());
            Assert.IsFalse(Directory.EnumerateFiles(root, "*.json").Any());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [TestMethod]
    public async Task Store_ShouldIsolateFilesByTenant()
    {
        var root = Path.Combine(Path.GetTempPath(), "ncf-function-file-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var options = Options.Create(new FunctionFileStoreOptions
            {
                RootPath = root,
                MaxFileBytes = 1024,
                MaxBatchBytes = 2048,
                MaxFilesPerBatch = 3,
                Retention = TimeSpan.FromMinutes(5)
            });
            var tenant11 = new RequestTenantInfo { Id = 11, TenantKey = "tenant-11" };
            tenant11.TryMatch(true);
            var tenant22 = new RequestTenantInfo { Id = 22, TenantKey = "tenant-22" };
            tenant22.TryMatch(true);
            var firstStore = new LocalFunctionFileStore(options, tenant11);
            var secondStore = new LocalFunctionFileStore(options, tenant22);

            await using var content = new MemoryStream(Encoding.UTF8.GetBytes("tenant-private"));
            var reference = await firstStore.SaveAsync(new FunctionFileUpload
            {
                Content = content,
                FileName = "private.txt",
                Length = content.Length
            });

            Assert.AreEqual(11, reference.TenantId);
            Assert.IsNull(await secondStore.GetAsync(reference.FileId));
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(() =>
                secondStore.OpenReadAsync(reference.FileId));

            await secondStore.DeleteAsync(reference.FileId);
            Assert.IsNotNull(await firstStore.GetAsync(reference.FileId));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

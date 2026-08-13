/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：FileResourceBoundaryTests.cs
    文件功能描述：FileManager 资源用途与公开 URL 回归测试

----------------------------------------------------------------*/

using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.FileManager.Domain.Services;
using System;
using System.Linq;

namespace Senparc.Xncf.AgentsManager.Domain.Services.Tests;

[TestClass]
public class FileResourceBoundaryTests
{
    [TestMethod]
    public void ResourceProfiles_ShouldKeepKnowledgeBaseAndSiteAssetsSeparated()
    {
        Assert.IsTrue(NcfFileResourcePolicy.IsAllowedExtension(NcfFileResourceScope.KnowledgeBase, ".docx"));
        Assert.IsFalse(NcfFileResourcePolicy.IsAllowedExtension(NcfFileResourceScope.KnowledgeBase, ".png"));
        Assert.IsTrue(NcfFileResourcePolicy.IsAllowedExtension(NcfFileResourceScope.SiteAsset, ".webp"));
        Assert.IsFalse(NcfFileResourcePolicy.IsAllowedExtension(NcfFileResourceScope.SiteAsset, ".js"));
        Assert.IsFalse(NcfFileResourcePolicy.IsAllowedExtension(NcfFileResourceScope.SiteAsset, ".html"));
    }

    [TestMethod]
    public void PublicAssetUrl_ShouldRequirePublishedStaticAssetAndFingerprint()
    {
        const string hash = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";
        var asset = new NcfFile
        {
            Id = 42,
            ResourceScope = NcfFileResourceScope.SiteAsset,
            AccessLevel = NcfFileAccessLevel.Public,
            ContentHash = hash
        };

        Assert.AreEqual("/assets/42/0123456789abcdef", NcfFileService.GetPublicAssetUrl(asset));

        asset.AccessLevel = NcfFileAccessLevel.Private;
        Assert.IsNull(NcfFileService.GetPublicAssetUrl(asset));

        asset.AccessLevel = NcfFileAccessLevel.Public;
        asset.ResourceScope = NcfFileResourceScope.KnowledgeBase;
        Assert.IsNull(NcfFileService.GetPublicAssetUrl(asset));
    }

    [TestMethod]
    public void FolderUploadPath_ShouldKeepRelativeSegmentsAndRejectTraversal()
    {
        var folders = NcfFolderUploadPath.GetFolderSegments("产品资料/2026/入门/说明.docx", "说明.docx");
        CollectionAssert.AreEqual(new[] { "产品资料", "2026", "入门" }, folders.ToArray());

        Assert.ThrowsException<ArgumentException>(() =>
            NcfFolderUploadPath.GetFolderSegments("产品资料/../说明.docx", "说明.docx"));
        Assert.ThrowsException<ArgumentException>(() =>
            NcfFolderUploadPath.GetFolderSegments("产品资料/伪造名称.docx", "说明.docx"));
    }
}

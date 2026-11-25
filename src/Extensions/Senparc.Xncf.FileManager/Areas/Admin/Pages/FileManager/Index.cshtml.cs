/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Index.cshtml.cs
    File description: Index.cshtml related implementation

    Created by: Senparc - 20250105

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260729
    Change description: v0.3.1-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20260813
    Change description: v0.6.0-preview1 Updated module behavior and related support.

    Modified by: Senparc - 20260915
    Change description: v0.7.0 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.8.1 add FileId reference to AIFineTuningDataset and implement FineTuningFileReferenceProvider

    Modified by: Senparc - 20261119
    Change description: v0.8.5 Localized file management and upload surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileManager.Domain.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.FileManager.Areas.FileManager.Pages
{
    [AutoValidateAntiforgeryToken]
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly NcfFileService _fileService;
        private readonly NcfFolderService _folderService;

        public string UpFileUrl { get; set; }
        public string DelFileUrl { get; set; }
        public string BaseUrl { get; set; }

        public Index(Lazy<XncfModuleService> xncfModuleService, NcfFileService fileService, NcfFolderService folderService)
            : base(xncfModuleService)
        {
            CurrentMenu = "FileManager";
            _fileService = fileService;
            _folderService = folderService;
        }

        /// <summary>Initializes upload and delete endpoint URLs for the admin page.</summary>
        public Task OnGetAsync()
        {
            UpFileUrl = $"{BaseUrl}/api/FileManager/Index/OnPostUploadAsync";
            DelFileUrl = $"{BaseUrl}/api/FileManager/Index/OnPostDeleteAsync";
            return Task.CompletedTask;
        }

        /// <summary>Returns a paged file list filtered by folder and resource scope.</summary>
        /// <param name="page">The one-based page number.</param><param name="pageSize">The page size.</param><param name="folderId">Optional folder filter.</param><param name="resourceScope">The resource boundary.</param>
        public async Task<IActionResult> OnGetListAsync(
            int page = 1,
            int pageSize = 10,
            int? folderId = null,
            NcfFileResourceScope resourceScope = NcfFileResourceScope.KnowledgeBase)
        {
            var result = await _fileService.GetFilesAsync(
                Math.Max(page, 1),
                Math.Clamp(pageSize, 1, 100),
                folderId,
                resourceScope);
            // PagedList<T> inherits List<T>; serializing it alone becomes a bare JSON
            // array and drops TotalCount. Return an explicit page envelope for the UI.
            return Ok(new
            {
                items = result.ToList(),
                totalCount = result.TotalCount,
                pageIndex = result.PageIndex,
                pageCount = result.PageCount
            });
        }

        /// <summary>Returns dashboard totals and capacity trends for the selected date range.</summary>
        /// <param name="startDate">Optional inclusive start date.</param><param name="endDate">Optional inclusive end date.</param>
        public async Task<IActionResult> OnGetDashboardStatsAsync(
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var stats = await _fileService.GetDashboardStatsAsync(startDate, endDate);
            return Ok(stats);
        }

        /// <summary>Returns a paged report of file references and physical state.</summary>
        /// <param name="page">The one-based report page.</param><param name="pageSize">The report page size.</param><param name="resourceScope">Optional scope filter.</param><param name="onlyUnreferenced">Whether to return only unreferenced files.</param>
        public async Task<IActionResult> OnGetReferenceReportAsync(
            int page = 1,
            int pageSize = 50,
            NcfFileResourceScope? resourceScope = null,
            bool onlyUnreferenced = false)
        {
            var report = await _fileService.ScanReferenceReportAsync(
                page,
                pageSize,
                resourceScope,
                onlyUnreferenced);
            return Ok(report);
        }

        /// <summary>Lists child folders under an optional parent and resource scope.</summary>
        /// <param name="parentId">Optional parent folder identifier.</param><param name="resourceScope">The resource boundary.</param>
        public async Task<IActionResult> OnGetFoldersAsync(
            int? parentId = null,
            NcfFileResourceScope resourceScope = NcfFileResourceScope.KnowledgeBase)
        {
            var folders = await _folderService.GetFoldersAsync(parentId, resourceScope);
            return Ok(folders);
        }

        /// <summary>Returns the ancestor path for a folder.</summary>
        /// <param name="folderId">The folder identifier.</param><param name="resourceScope">The resource boundary.</param>
        public async Task<IActionResult> OnGetFolderPathAsync(
            int folderId,
            NcfFileResourceScope resourceScope = NcfFileResourceScope.KnowledgeBase)
        {
            if (folderId <= 0)
            {
                return BadRequest("文件夹编号无效。");
            }

            var folders = await _folderService.GetFolderPathAsync(folderId, resourceScope);
            return Ok(folders);
        }

        public record FileUploadModel
        {
            public List<IFormFile> files { get; set; }
            public List<string> descriptions { get; set; }
            /// <summary>
            /// Browser folder uploads provide one relative path per file. The
            /// value is validated server-side and never used as a disk path.
            /// </summary>
            public List<string> relativePaths { get; set; }
            public int? folderId { get; set; }
            public NcfFileResourceScope resourceScope { get; set; } = NcfFileResourceScope.KnowledgeBase;
        }

        [ApiBind("FileManager", ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        [RequestSizeLimit(NcfFileService.MaxTotalUploadBytes)]
        /// <summary>Validates and stores one bounded batch of uploaded files.</summary>
        /// <param name="model">Files, descriptions, folder paths, and resource scope.</param>
        public async Task<IActionResult> OnPostUploadAsync([FromForm] FileUploadModel model)
        {
            if (model.files == null || !model.files.Any())
                return BadRequest("No files uploaded");

            if (model.files.Count > NcfFileService.MaxFilesPerUpload)
            {
                return BadRequest($"一次最多上传 {NcfFileService.MaxFilesPerUpload} 个文件。");
            }

            if (model.relativePaths?.Count > 0 && model.relativePaths.Count != model.files.Count)
            {
                return BadRequest("文件夹上传的路径信息不完整，请重新选择文件夹。");
            }

            if (model.files.Sum(file => file?.Length ?? 0L) > NcfFileService.MaxTotalUploadBytes)
            {
                return BadRequest($"单次上传总大小不能超过 {NcfFileService.MaxTotalUploadBytes / 1024 / 1024} MB。");
            }

            var results = new List<NcfFileDto>();

            for (int i = 0; i < model.files.Count; i++)
            {
                var file = model.files[i];
                var description = model.descriptions != null && model.descriptions.Count > i ? model.descriptions[i] : null;

                if (file?.Length > 0)
                {
                    var relativePath = model.relativePaths != null && model.relativePaths.Count > i
                        ? model.relativePaths[i]
                        : null;
                    var targetFolderId = model.folderId;
                    if (!string.IsNullOrWhiteSpace(relativePath))
                    {
                        var folderSegments = NcfFolderUploadPath.GetFolderSegments(relativePath, file.FileName);
                        targetFolderId = await _folderService.GetOrCreateFolderPathAsync(
                            folderSegments,
                            model.folderId,
                            model.resourceScope);
                    }

                    var entity = await _fileService.UploadFileAsync(file, model.resourceScope, targetFolderId);
                    if (!string.IsNullOrEmpty(description))
                    {
                        await _fileService.UpdateFileNoteAsync(entity.Id, description);
                    }
                    results.Add(_fileService.Mapper.Map<NcfFileDto>(entity));
                }
            }

            return Ok(results);
        }

        public record UpdateFileNoteRequest(int Id, string Note);

        /// <summary>Updates one file's administrative note.</summary>
        /// <param name="request">The file identifier and note.</param>
        public async Task<IActionResult> OnPostEditNoteAsync([FromBody] UpdateFileNoteRequest request)
        {
            await _fileService.UpdateFileNoteAsync(request.Id, request.Note);
            return Ok(true);
        }

        public record SetSiteAssetPublicationRequest(int Id, bool Publish);

        /// <summary>Publishes or unpublishes a public SiteAsset file.</summary>
        /// <param name="request">The file identifier and publication flag.</param>
        public async Task<IActionResult> OnPostSetSiteAssetPublicationAsync([FromBody] SetSiteAssetPublicationRequest request)
        {
            await _fileService.SetSiteAssetPublicationAsync(request.Id, request.Publish);
            return Ok(true);
        }

        [ApiBind("FileManager", ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        /// <summary>Moves one file through the configured deletion/recycle workflow.</summary>
        /// <param name="id">The file identifier.</param>
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _fileService.DeleteFileAsync(id);
            return Ok(true);
        }

        public record EmptyRecycleRequest(List<int> Ids);

        /// <summary>
        /// Permanently deletes files currently held in the recycle bin UI.
        /// Missing ids are treated as already removed.
        /// </summary>
        public async Task<IActionResult> OnPostEmptyRecycleAsync([FromBody] EmptyRecycleRequest request)
        {
            var ids = (request?.Ids ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return Ok(new { deletedCount = 0, failedIds = Array.Empty<int>(), message = "回收站为空" });
            }

            var failedIds = new List<int>();
            var deletedCount = 0;
            string lastError = null;

            foreach (var id in ids)
            {
                try
                {
                    await _fileService.DeleteFileAsync(id);
                    deletedCount++;
                }
                catch (Exception ex)
                {
                    failedIds.Add(id);
                    lastError = ex.Message;
                }
            }

            return Ok(new
            {
                deletedCount,
                failedIds,
                message = failedIds.Count == 0
                    ? "一键清空成功"
                    : $"已删除 {deletedCount} 个，失败 {failedIds.Count} 个" + (string.IsNullOrEmpty(lastError) ? "" : $"：{lastError}")
            });
        }

        /// <summary>Returns a file as an attachment after physical-path validation.</summary>
        /// <param name="id">The file identifier.</param>
        public async Task<IActionResult> OnGetDownloadAsync(int id)
        {
            var located = await _fileService.TryGetPhysicalPathAsync(id);
            if (located == null)
            {
                return NotFound();
            }

            var (file, fullPath) = located.Value;
            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

            return PhysicalFile(fullPath, contentType, file.FileName);
        }

        /// <summary>Returns a file inline after physical-path validation.</summary>
        /// <param name="id">The file identifier.</param>
        public async Task<IActionResult> OnGetPreviewAsync(int id)
        {
            var located = await _fileService.TryGetPhysicalPathAsync(id);
            if (located == null)
            {
                return NotFound();
            }

            var (file, fullPath) = located.Value;
            var contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType;

            Response.Headers[HeaderNames.ContentDisposition] = new ContentDispositionHeaderValue("inline")
            {
                FileNameStar = file.FileName
            }.ToString();

            return PhysicalFile(fullPath, contentType);
        }

        /// <summary>Extracts bounded preview text from a supported file.</summary>
        /// <param name="id">The file identifier.</param>
        public async Task<IActionResult> OnGetPreviewContentAsync(int id)
        {
            try
            {
                var result = await _fileService.GetPreviewTextAsync(id);
                return Ok(new
                {
                    content = result.Text,
                    extension = result.Extension
                });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidDataException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public record CreateFolderRequest
        {
            [Required]
            public string Name { get; init; }

            public int? ParentId { get; init; }

            public string Description { get; init; }

            public NcfFileResourceScope ResourceScope { get; init; } = NcfFileResourceScope.KnowledgeBase;
        }

        // Folder handlers
        /// <summary>Creates a folder after model-state and resource-scope validation.</summary>
        /// <param name="request">The folder name, parent, description, and resource scope.</param>
        public async Task<IActionResult> OnPostCreateFolderAsync([FromBody] CreateFolderRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(kv => kv.Value?.Errors?.Count > 0)
                    .Select(kv => new { Field = kv.Key, Errors = kv.Value.Errors.Select(e => e.ErrorMessage).ToArray() })
                    .ToArray();
                return BadRequest(new { message = "ModelState invalid", errors });
            }

            var folder = await _folderService.CreateFolderAsync(
                request.Name,
                request.ParentId,
                request.Description,
                request.ResourceScope);
            return Ok(folder);
        }

        public record UpdateFolderRequest(int Id, string Name, string Description);

        /// <summary>Updates one folder's name and description.</summary>
        /// <param name="request">The folder identifier and new metadata.</param>
        public async Task<IActionResult> OnPostUpdateFolderAsync([FromBody] UpdateFolderRequest request)
        {
            await _folderService.UpdateFolderAsync(request.Id, request.Name, request.Description);
            return Ok(true);
        }

        /// <summary>Deletes one folder through the FileManager folder service.</summary>
        /// <param name="id">The folder identifier.</param>
        public async Task<IActionResult> OnPostDeleteFolderAsync(int id)
        {
            await _folderService.DeleteFolderAsync(id);
            return Ok(true);
        }
    }
}

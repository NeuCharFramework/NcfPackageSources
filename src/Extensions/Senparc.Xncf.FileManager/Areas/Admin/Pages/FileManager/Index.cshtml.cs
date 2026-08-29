/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：Index.cshtml.cs
    文件功能描述：Index.cshtml 相关实现


    创建标识：Senparc - 20250105

    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260729
    修改描述：v0.3.1-preview3 加强文件上传校验和物理路径安全

    修改标识：Senparc - 20260813
    修改描述：v0.6.0-preview1 完善文件资源边界、安全删除策略与静态资源管理

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileManager.Domain.Services;
using System;
using System.Collections.Generic;
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

        public Task OnGetAsync()
        {
            UpFileUrl = $"{BaseUrl}/api/FileManager/Index/OnPostUploadAsync";
            DelFileUrl = $"{BaseUrl}/api/FileManager/Index/OnPostDeleteAsync";
            return Task.CompletedTask;
        }

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

        public async Task<IActionResult> OnGetFoldersAsync(
            int? parentId = null,
            NcfFileResourceScope resourceScope = NcfFileResourceScope.KnowledgeBase)
        {
            var folders = await _folderService.GetFoldersAsync(parentId, resourceScope);
            return Ok(folders);
        }

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

        public async Task<IActionResult> OnPostEditNoteAsync([FromBody] UpdateFileNoteRequest request)
        {
            await _fileService.UpdateFileNoteAsync(request.Id, request.Note);
            return Ok(true);
        }

        public record SetSiteAssetPublicationRequest(int Id, bool Publish);

        public async Task<IActionResult> OnPostSetSiteAssetPublicationAsync([FromBody] SetSiteAssetPublicationRequest request)
        {
            await _fileService.SetSiteAssetPublicationAsync(request.Id, request.Publish);
            return Ok(true);
        }

        [ApiBind("FileManager", ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _fileService.DeleteFileAsync(id);
            return Ok(true);
        }

        public async Task<IActionResult> OnGetDownloadAsync(int id)
        {
            var fileInfo = await _fileService.OpenReadAsync(id);

            if (fileInfo == null)
            {
                return NotFound();
            }

            return new FileStreamResult(
                fileInfo.Stream,
                string.IsNullOrWhiteSpace(fileInfo.File.ContentType) ? "application/octet-stream" : fileInfo.File.ContentType)
            {
                FileDownloadName = fileInfo.File.FileName,
                EnableRangeProcessing = true
            };
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

        public async Task<IActionResult> OnPostUpdateFolderAsync([FromBody] UpdateFolderRequest request)
        {
            await _folderService.UpdateFolderAsync(request.Id, request.Name, request.Description);
            return Ok(true);
        }

        public async Task<IActionResult> OnPostDeleteFolderAsync(int id)
        {
            await _folderService.DeleteFolderAsync(id);
            return Ok(true);
        }
    }
}

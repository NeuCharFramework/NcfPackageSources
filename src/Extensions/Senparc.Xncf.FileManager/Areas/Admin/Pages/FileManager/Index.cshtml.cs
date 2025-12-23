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
    [IgnoreAntiforgeryToken]
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
            _fileService = fileService;
            _folderService = folderService;
        }

        public Task OnGetAsync()
        {
            UpFileUrl = $"{BaseUrl}/api/FileManager/Index/OnPostUploadAsync";
            DelFileUrl = $"{BaseUrl}/api/FileManager/Index/OnPostDeleteAsync";
            return Task.CompletedTask;
        }

        public async Task<IActionResult> OnGetListAsync(int page = 1, int pageSize = 10, int? folderId = null)
        {
            var result = await _fileService.GetFilesAsync(page, pageSize, folderId);
            return Ok(result);
        }

        public async Task<IActionResult> OnGetFoldersAsync(int? parentId = null)
        {
            var folders = await _folderService.GetFoldersAsync(parentId);
            return Ok(folders);
        }

        public record FileUploadModel
        {
            public List<IFormFile> files { get; set; }
            public List<string> descriptions { get; set; }
            public int? folderId { get; set; }
        }

        [ApiBind("FileManager", ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<IActionResult> OnPostUploadAsync([FromForm] FileUploadModel model)
        {
            if (model.files == null || !model.files.Any())
                return BadRequest("No files uploaded");

            var results = new List<NcfFileDto>();

            for (int i = 0; i < model.files.Count; i++)
            {
                var file = model.files[i];
                var description = model.descriptions != null && model.descriptions.Count > i ? model.descriptions[i] : null;

                if (file.Length > 0)
                {
                    var entity = await _fileService.UploadFileAsync(file, model.folderId);
                    if (!string.IsNullOrEmpty(description))
                    {
                        entity.Description = description;
                        await _fileService.SaveObjectAsync(entity);
                    }
                    results.Add(_fileService.Mapper.Map<NcfFileDto>(entity));
                }
            }

            return Ok(results);
        }

        public async Task<IActionResult> OnPostEditNoteAsync(int id, string note)
        {
            await _fileService.UpdateFileNoteAsync(id, note);
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
            var fileInfo = await _fileService.GetFileBytes(id);

            if (fileInfo.FileBytes.Length == 0)
            {
                return Ok(false, fileInfo.FileName);
            }

            return File(fileInfo.FileBytes, "application/octet-stream", fileInfo.FileName);
        }

        public record CreateFolderRequest
        {
            [Required]
            public string Name { get; init; }

            public int? ParentId { get; init; }

            public string Description { get; init; }
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

            var folder = await _folderService.CreateFolderAsync(request.Name, request.ParentId, request.Description);
            return Ok(folder);
        }

        public async Task<IActionResult> OnPostUpdateFolderAsync(int id, string name, string description)
        {
            await _folderService.UpdateFolderAsync(id, name, description);
            return Ok(true);
        }

        public async Task<IActionResult> OnPostDeleteFolderAsync(int id)
        {
            await _folderService.DeleteFolderAsync(id);
            return Ok(true);
        }
    }
}

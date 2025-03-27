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

namespace Senparc.Xncf.FileManager.Areas.FileManager.Pages
{
    public class Index : Senparc.Ncf.AreaBase.Admin.AdminXncfModulePageModelBase
    {
        private readonly NcfFileService _fileService;

        public string UpFileUrl { get; set; }
        public string DelFileUrl {  get; set; }
        public string BaseUrl { get; set; }

        public Index(Lazy<XncfModuleService> xncfModuleService, NcfFileService fileService)
            : base(xncfModuleService)
        {
            _fileService = fileService;
        }

        public Task OnGetAsync()
        {
            UpFileUrl = $"{BaseUrl}/api/FileManager/Index/OnPostUploadAsync";
            UpFileUrl = $"{BaseUrl}/api/FileManager/Index/OnPostDeleteAsync";
            return Task.CompletedTask;
        }

        public async Task<IActionResult> OnGetListAsync(int page = 1, int pageSize = 10)
        {
            var result = (await _fileService.GetObjectListAsync(page, pageSize, z => true, z => z.Id, OrderingType.Ascending, null))
            .ToDtoPagedList<NcfFile, NcfFileDto>(_fileService);

            return Ok(new PagedList<NcfFileDto>(result, page, pageSize, result.TotalCount));
        }

        public record FileUploadModel
        {
            public List<IFormFile> files { get; set; }
            public string descriptions { get; set; }
        }

        [ApiBind("FileManager",ApiRequestMethod = CO2NET.WebApi.ApiRequestMethod.Post)]
        public async Task<IActionResult> OnPostUploadAsync([FromForm] FileUploadModel model)
        {
            if (model.files == null || !model.files.Any())
                return BadRequest("No files uploaded");

            var results = new List<NcfFileDto>();

            for (int i = 0; i < model.files.Count; i++)
            {
                var file = model.files[i];
                var description = model.descriptions?.Length > i ? model.descriptions : "";

                if (file.Length > 0)
                {
                    var result = await _fileService.UploadFileAsync(file);
                    results.Add(_fileService.Mapper.Map<NcfFileDto>(result));
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
    }
}

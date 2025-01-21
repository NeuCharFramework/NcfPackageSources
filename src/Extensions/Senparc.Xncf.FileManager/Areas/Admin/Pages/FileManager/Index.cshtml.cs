using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public Index(Lazy<XncfModuleService> xncfModuleService, NcfFileService fileService)
            : base(xncfModuleService)
        {
            _fileService = fileService;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnGetListAsync(int page = 1, int pageSize = 10)
        {
            var result = (await _fileService.GetObjectListAsync(page, pageSize, z => true, z => z.Id, OrderingType.Ascending, null))
            .ToDtoPagedList<NcfFile, NcfFileDto>(_fileService);

            return Ok(new PagedList<NcfFileDto>(result, page, pageSize, result.TotalCount));
        }

        public class FileUploadModel
        {
            public IFormFile File { get; set; }
            public string Description { get; set; }
        }

        public async Task<IActionResult> OnPostUploadAsync([FromForm] List<IFormFile> files, [FromForm] string[] descriptions)
        {
            if (files == null || !files.Any())
                return BadRequest("No files uploaded");

            var results = new List<NcfFileDto>();
            
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var description = descriptions?.Length > i ? descriptions[i] : "";
                
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

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _fileService.DeleteFileAsync(id);
            return Ok(true);
        }
    }
}

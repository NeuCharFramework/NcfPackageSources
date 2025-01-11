using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Service;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.FileManager.Domain.Services;
using System;
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

        public async Task<IActionResult> OnPostUploadAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            var result = await _fileService.UploadFileAsync(file);
            return Ok(result);
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

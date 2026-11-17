/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FileTemplate_GetListResponse.cs
    File description: FileTemplate_GetListResponse related implementation

    Created by: Senparc - 20250710

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.FileManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.FileManager.OHS.Local.PL.Response
{
    public class FileTemplate_GetListResponse
    {
        public PagedList<NcfFileDto> List { get; set; }
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
    }
}

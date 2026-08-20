/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FileTemplate_GetListResponse.cs
    文件功能描述：FileTemplate_GetListResponse 相关实现
    
    
    创建标识：Senparc - 20250710
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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

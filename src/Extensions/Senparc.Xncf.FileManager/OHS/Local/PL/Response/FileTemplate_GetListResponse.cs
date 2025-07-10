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
    }
}

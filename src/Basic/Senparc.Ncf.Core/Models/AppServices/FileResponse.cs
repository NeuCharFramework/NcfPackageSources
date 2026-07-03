/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FileResponse.cs
    文件功能描述：FileResponse 相关实现
    
    
    创建标识：Senparc - 20221027
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Senparc.Ncf.Core.Models.AppServices
{
    public class FileResponse
    {
        public Stream Stream { get; set; }
        public string ContentType { get; set; }
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：BuildXncfRequest.AI.MCP.cs
    文件功能描述：BuildXncfRequest.AI.MCP 相关实现
    
    
    创建标识：Senparc - 20250524
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using Senparc.Xncf.AIKernel.OHS.Local.AppService;
using Senparc.AI.Exceptions;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public class BuildXncf_GetFileResponse
    {
        public bool Success{get; set;}
        public string Message { get; set; }
        public string FileName{get; set;}
        public string FileContent{get; set;}
        public string FilePath{get; set;}
    }

    public class BuildXncf_CreateOrUpdateFileResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FileName { get; set; }

        public bool IsNewFile { get; set; }
        //public string OldFileContent { get; set; }
    }

}
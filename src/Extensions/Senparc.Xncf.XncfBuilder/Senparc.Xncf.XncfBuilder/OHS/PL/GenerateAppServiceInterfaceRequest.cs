/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：GenerateAppServiceInterfaceRequest.cs
    文件功能描述：GenerateAppServiceInterfaceRequest 相关实现
    
    
    创建标识：Senparc - 20220205
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public class GenerateAppServiceInterface_GenerateRequest : FunctionAppRequestBase
    {
        [Required]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Interface.TargetPath")]
        public string TargetProjectPath { get; set; }

        [Required]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Interface.Namespace")]
        public string NamespacePrefix { get; set; }

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Interface.ClassPattern")]
        public string ClassNamePattern { get; set; }

        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Interface.MethodPattern")]
        public string MethodNamePattern { get; set; }

        [Required]
        [LocalizedDescription(typeof(XncfBuilderResource), "Parameter.XncfBuilder.Interface.DestinationPath")]
        public string DestinationProjectPath { get; set; }

     
    }
}

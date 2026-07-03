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
        [Description("目标项目路径||需要生成接口的项目路径，如：E:\\Senparc项目\\NeuCharFramework\\NCF\\src\\back-end\\Senparc.Xncf.Installer")]
        public string TargetProjectPath { get; set; }

        [Required]
        [Description("命名空间前缀||需要生成的类的命名空间前缀，如：Senparc.Xncf.Installer")]
        public string NamespacePrefix { get; set; }

        [Description("类名正则||需要匹配的类名的正则表达式")]
        public string ClassNamePattern { get; set; }

        [Description("方法名正则||需要匹配的类名的方法名正则表达式")]
        public string MethodNamePattern { get; set; }

        [Required]
        [Description("生成目的地项目路径||需要生成接口的项目路径，如：E:\\Senparc项目\\NeuCharFramework\\NCF\\src\\back-end\\Senparc.Xncf.MyNewProject")]
        public string DestinationProjectPath { get; set; }

     
    }
}

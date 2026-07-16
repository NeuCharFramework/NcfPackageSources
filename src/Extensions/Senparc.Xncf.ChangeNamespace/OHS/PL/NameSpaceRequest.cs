/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NameSpaceRequest.cs
    文件功能描述：NameSpaceRequest 相关实现
    
    
    创建标识：Senparc - 20211016
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Senparc.Ncf.XncfBase.Functions.Parameters;
using System.Text.Json.Serialization;

namespace Senparc.Xncf.ChangeNamespace.OHS.PL
{
    public class NameSpace_ChangeRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(300)]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.Path")]
        public string Path { get; set; }
        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.NewNamespace")]
        public string NewNamespace { get; set; }

        public string OldNamespaceKeyword = "Senparc.";//此参数不设置为属性，不需要在前端显示
    }

    public class NameSpace_DownloadSourceCodeRequest : FunctionAppRequestBase
    {
        /// <summary>
        /// 提供选项
        /// <para>注意：string[]类型的默认值为选项的备选值，如果没有提供备选值，此参数将别忽略</para>
        /// </summary>z
        [Required]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.Source")]
        [FunctionParameterUi(ParameterType.DropDownList, nameof(SiteOptions))]
        public string Site { get; set; }

        [JsonIgnore]
        public SelectionList SiteOptions { get; set; } = new SelectionList(SelectionType.DropDownList, new[]
        {
                new SelectionItem(Parameters_Site.GitHub.ToString(),Parameters_Site.GitHub.ToString()),
                new SelectionItem(Parameters_Site.Gitee.ToString(),Parameters_Site.Gitee.ToString())
            });

        public enum Parameters_Site
        {
            GitHub,
            Gitee
        }
    }


    public class NameSpace_RestoreRequest : FunctionAppRequestBase
    {
        [Required]
        [MaxLength(300)]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.Path")]
        public string Path { get; set; }
        [Required]
        [MaxLength(100)]
        [LocalizedDescription(typeof(ChangeNamespaceResource), "Parameter.ChangeNamespace.CurrentNamespace")]
        public string MyNamespace { get; set; }
    }
}

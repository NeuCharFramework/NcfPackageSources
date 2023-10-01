using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Xncf.ChangeNamespace.OHS.PL
{
    public class NameSpace_ChangeRequest: FunctionAppRequestBase
    {
        [Required]
        [MaxLength(300)]
        [Description("路径||本地物理路径，如：E:\\Senparc\\Ncf\\")]
        public string Path { get; set; }
        [Required]
        [MaxLength(100)]
        [Description("新命名空间||命名空间根，必须以.结尾，用于替换[Senparc.Ncf.]")]
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
        [Description("源码来源||目前更新最快的是 GitHub，Gitee（码云）在国内下载速度更快，但是不能确定是最新代码，下载前请注意核对最新 GitHub 上的版本。")]
        public SelectionList Site { get; set; } = new SelectionList(SelectionType.DropDownList, new[]
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
        [Description("路径||本地物理路径，如：E:\\Senparc\\Ncf\\")]
        public string Path { get; set; }
        [Required]
        [MaxLength(100)]
        [Description("当前自定义的命名空间||命名空间根，一般以.结尾，如：[My.Namespace.]，最终将替换为例如[Senparc.Ncf.]或[Senparc.]")]
        public string MyNamespace { get; set; }
    }
}

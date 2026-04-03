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

        public string OldNamespaceKeyword = "Senparc.";//This parameter is not set as an attribute and does not need to be displayed on the front end
    }

    public class NameSpace_DownloadSourceCodeRequest : FunctionAppRequestBase
    {
        /// <summary>
        /// Provide options
        /// <para>Note: string[]The default value of the type is the alternative value of the option. If no alternative value is provided, this parameter will not be ignored.</para>
        /// </summary>z
        [Required]
        [Description("Source code source||Currently, GitHub is the fastest updated, and Gitee (code cloud) has faster download speed in China, but it cannot be sure that it is the latest code. Please check the latest version on GitHub before downloading.")]
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
        [Description("path||Local physical path, such as: E:\\Senparc\\Ncf\\")]
        public string Path { get; set; }
        [Required]
        [MaxLength(100)]
        [Description("Current customized namespace||The namespace root is usually.ending, such as:[My.Namespace.]，will eventually be replaced by e.g.[Senparc.Ncf.]or[Senparc.]")]
        public string MyNamespace { get; set; }
    }
}

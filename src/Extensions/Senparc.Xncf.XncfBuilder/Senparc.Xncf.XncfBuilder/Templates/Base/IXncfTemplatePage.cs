using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.XncfBuilder.Templates
{

    public interface IXncfTemplatePage
    {
        /// <summary>
        /// Create the template output
        /// </summary>
        string TransformText();

        /// <summary>
        /// 相对地址
        /// </summary>
        string RelativeFilePath { get; }

        string OrgName { get; set; }
        string XncfName { get; set; }
    }
}

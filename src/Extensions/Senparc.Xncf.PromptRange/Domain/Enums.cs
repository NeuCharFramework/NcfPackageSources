using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain
{
    /// <summary>
    /// XncfBuilder 的 Prompt 类型
    /// </summary>
    public enum XncfBuilderPromptType
    {
        EntityClass,
        Repository,
        Service,
        AppService,
        PL,
        DbContext
    }
}

using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.VD;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Areas.Admin.Domain.Models.VD
{
    public interface IBasePageModel : IPageModelBase
    { }

    /// <summary>
    /// The current project's PageModel global base class for front-end (non-Areas) use
    /// </summary>
    public class BasePageModel : PageModelBase, IBasePageModel
    {
    }
}

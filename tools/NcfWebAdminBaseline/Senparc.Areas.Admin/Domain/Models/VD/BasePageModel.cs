/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: BasePageModel.cs
    File description: BasePageModel related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

    Modified by: Senparc - 20261127
    Change description: v0.12.30 Added StartChatToolTask for admin-driven chat-tool runs with async task monitoring

----------------------------------------------------------------*/

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
    /// Provides the common page-model contract for administration Razor Pages.
    /// </summary>
    public class BasePageModel : PageModelBase, IBasePageModel
    {
    }
}

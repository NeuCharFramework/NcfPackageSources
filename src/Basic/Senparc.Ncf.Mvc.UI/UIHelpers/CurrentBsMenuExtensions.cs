/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CurrentBsMenuExtensions.cs
    File description: CurrentBsMenuExtensions related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.22.9-preview13 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.Rendering;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models.VD;

namespace System.Web.Mvc
{
    /// <summary>
    /// Provides Bootstrap-style current-menu state helpers for NCF view models.
    /// </summary>
    public static class CurrentBsMenuExtensions
    {
        /// <summary>
        /// Returns the Bootstrap <c>active</c> class when the current menu matches
        /// the requested menu or its top-level parent.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper whose view model is inspected.</param>
        /// <param name="menuName">The menu name or top-level menu segment to compare.</param>
        /// <returns>
        /// <c>active</c> for a match; otherwise an empty string.
        /// </returns>
        /// <remarks>
        /// Only models implementing <see cref="IBaseUiVD"/> participate. The helper
        /// does not mutate the model or view data.
        /// </remarks>
        public static string CurrentBsMenu(this IHtmlHelper htmlHelper, string menuName)
        {
            if (htmlHelper.ViewData.Model is IBaseUiVD)
            {
                IBaseUiVD model = htmlHelper.ViewData.Model as IBaseUiVD;
                if (!model.CurrentMenu.IsNullOrEmpty())
                {
                    var parentMenuMane = model.CurrentMenu.Split('.')[0];
                    if (model.CurrentMenu.Equals(menuName, StringComparison.OrdinalIgnoreCase)
                           || parentMenuMane.Equals(menuName, StringComparison.OrdinalIgnoreCase))
                    {
                        return "active";
                    }
                    else
                    {
                        return "";
                    }
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }
    }
}

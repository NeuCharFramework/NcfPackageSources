/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CurrentMenuExtensions.cs
    File description: CurrentMenuExtensions related implementation

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
    public static class CurrentMenuExtensions
    {
        /// <summary>
        /// Returns the active CSS class when the current view model's menu value matches
        /// the requested menu name or its top-level menu segment.
        /// </summary>
        /// <param name="htmlHelper">The HTML helper containing the current view model.</param>
        /// <param name="menuName">The menu name or top-level segment to compare.</param>
        /// <param name="currentClassName">The CSS class returned for an active menu.</param>
        /// <returns>The active class when matched; otherwise an empty string.</returns>
        public static string CurrentMenu(this IHtmlHelper htmlHelper, string menuName, string currentClassName = "current active")
        {
            if (htmlHelper.ViewData.Model is IBaseUiVD)
            {
                IBaseUiVD model = htmlHelper.ViewData.Model as IBaseUiVD;
                if (!model.CurrentMenu.IsNullOrEmpty())
                {
                    //int indexOf = model.CurrentMenu.LastIndexOf('.');
                    //string parentMenuMane = model.CurrentMenu.Substring(0, indexOf);
                    var parentMenuNane = model.CurrentMenu.Split('.')[0];
                    if (model.CurrentMenu.StartsWith(menuName, StringComparison.OrdinalIgnoreCase)
                           || parentMenuNane.Equals(menuName, StringComparison.OrdinalIgnoreCase))
                    {
                        return currentClassName;
                        //return "active";
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

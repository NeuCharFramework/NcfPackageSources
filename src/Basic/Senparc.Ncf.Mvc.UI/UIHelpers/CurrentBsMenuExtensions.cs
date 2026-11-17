/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: CurrentBsMenuExtensions.cs

    File description: CurrentBsMenuExtensions related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.Rendering;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models.VD;

namespace System.Web.Mvc
{
    public static class CurrentBsMenuExtensions
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="menuName"></param>
        /// <returns></returns>
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

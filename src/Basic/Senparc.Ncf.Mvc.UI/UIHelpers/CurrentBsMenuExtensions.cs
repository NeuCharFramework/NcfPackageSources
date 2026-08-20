/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：CurrentBsMenuExtensions.cs
    文件功能描述：CurrentBsMenuExtensions 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.Rendering;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models.VD;

namespace System.Web.Mvc
{
    public static class CurrentBsMenuExtensions
    {
        /// <summary>
        /// Bootstrap当前菜单
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

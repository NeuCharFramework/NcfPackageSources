/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：CurrentMenuExtensions.cs
    文件功能描述：CurrentMenuExtensions 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.Rendering;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models.VD;

namespace System.Web.Mvc
{
    public static class CurrentMenuExtensions
    {
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

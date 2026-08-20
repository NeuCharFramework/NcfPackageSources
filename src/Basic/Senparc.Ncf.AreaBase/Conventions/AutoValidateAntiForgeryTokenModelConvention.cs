/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AutoValidateAntiForgeryTokenModelConvention.cs
    文件功能描述：AutoValidateAntiForgeryTokenModelConvention 相关实现
    
    
    创建标识：Senparc - 20211215
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.AreaBase.Conventions
{
    public class AutoValidateAntiForgeryTokenModelConvention : IPageConvention// IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            if (IsConventionApplicable(action))
            {
                action.Filters.Add(new ValidateAntiForgeryTokenAttribute());
            }
        }

        public bool IsConventionApplicable(ActionModel action)
        {
            if (action.Attributes.Any(f => f.GetType() == typeof(HttpPostAttribute)) &&
                !action.Attributes.Any(f => f.GetType() == typeof(ValidateAntiForgeryTokenAttribute)))
            {
                return true;
            }
            return false;
        }
    }
}

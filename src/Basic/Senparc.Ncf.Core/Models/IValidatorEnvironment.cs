/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IValidatorEnvironment.cs
    文件功能描述：IValidatorEnvironment 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 作为需要进行视图验证的基础接口（如Controller、PageModel）
    /// </summary>
    public interface IValidatorEnvironment
    {
        /// <summary>
        /// Controller 及 PageModel 中的 ModelState 对象
        /// </summary>
        ModelStateDictionary ModelState { get; }

        /// <summary>
        /// HttpContext
        /// </summary>
        HttpContext HttpContext { get; }
    }
}

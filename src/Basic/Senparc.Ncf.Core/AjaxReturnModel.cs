/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AjaxReturnModel.cs
    文件功能描述：AjaxReturnModel 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core
{

    /// <summary>
    /// ajax返回模型
    /// </summary>
    public class AjaxReturnModel
    {
        public bool Success { get; set; }

        public string Msg { get; set; }
    }

    /// <summary>
    /// ajax返回模型
    /// </summary>
    /// <typeparam name="T">返回的对象</typeparam>
    public class AjaxReturnModel<T> : AjaxReturnModel
    {
        public T Data { get; set; }

        public AjaxReturnModel()
        {

        }

        public AjaxReturnModel(T data) : this()
        {
            this.Data = data;
        }
    }
}

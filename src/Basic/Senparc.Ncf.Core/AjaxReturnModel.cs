/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: AjaxReturnModel.cs

    File description: AjaxReturnModel related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core
{

    /// <summary>
    /// ajaxreturnmodel
    /// </summary>
    public class AjaxReturnModel
    {
        public bool Success { get; set; }

        public string Msg { get; set; }
    }

    /// <summary>
    /// ajaxreturnmodel
    /// </summary>
    /// <typeparam name="T">Object to return</typeparam>
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

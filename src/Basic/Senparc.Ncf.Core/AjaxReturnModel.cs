using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core
{

    /// <summary>
    ///ajax return model
    /// </summary>
    public class AjaxReturnModel
    {
        public bool Success { get; set; }

        public string Msg { get; set; }
    }

    /// <summary>
    ///ajax return model
    /// </summary>
    /// <typeparam name="T">Returned object</typeparam>
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

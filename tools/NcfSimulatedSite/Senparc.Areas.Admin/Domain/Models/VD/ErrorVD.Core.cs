
using System;

namespace Senparc.Areas.Admin.Domain.Models.VD
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IError_BaseVD : IBaseVD
    {
        ///// <summary>
        ///Legacy comment translated to English.
        ///// </summary>
        //HandleErrorInfo HandleErrorInfo { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        string Url { get; set; }
    }


    public class Error_BaseVD : BaseVD, IError_BaseVD
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        //public HandleErrorInfo HandleErrorInfo { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Url { get; set; }
    }

    public class Error_ExceptionVD : Error_BaseVD
    {
        public string Message { get; set; }
        public Exception Exception { get; set; }
    }


    public class Error_Error404VD : Error_BaseVD
    {
    }
}
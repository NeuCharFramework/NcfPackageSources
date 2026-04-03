
using System;

namespace Senparc.Areas.Admin.Domain.Models.VD
{
    /// <summary>
    /// All Error_ExceptionVD globally must implement this interface
    /// </summary>
    public interface IError_BaseVD : IBaseVD
    {
        ///// <summary>
        ///// For Error_ExceptionVD
        ///// </summary>
        //HandleErrorInfo HandleErrorInfo { get; set; }

        /// <summary>
        /// for Error_Error404VD
        /// </summary>
        string Url { get; set; }
    }


    public class Error_BaseVD : BaseVD, IError_BaseVD
    {
        /// <summary>
        /// For Error_ExceptionVD
        /// </summary>
        //public HandleErrorInfo HandleErrorInfo { get; set; }

        /// <summary>
        /// for Error_Error404VD
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
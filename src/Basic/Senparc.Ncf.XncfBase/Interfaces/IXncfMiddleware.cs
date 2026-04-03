using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// middleware interface
    /// </summary>
    public interface IXncfMiddleware
    {
        /// <summary>
        /// use middleware
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        IApplicationBuilder UseMiddleware(IApplicationBuilder app);
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// As a basic interface that requires view verification (such as Controller, PageModel)
    /// </summary>
    public interface IValidatorEnvironment
    {
        /// <summary>
        /// ModelState objects in Controller and PageModel
        /// </summary>
        ModelStateDictionary ModelState { get; }

        /// <summary>
        /// HttpContext
        /// </summary>
        HttpContext HttpContext { get; }
    }
}

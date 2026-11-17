/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IValidatorEnvironment.cs
    File description: IValidatorEnvironment related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public interface IValidatorEnvironment
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        ModelStateDictionary ModelState { get; }

        /// <summary>
        /// HttpContext
        /// </summary>
        HttpContext HttpContext { get; }
    }
}

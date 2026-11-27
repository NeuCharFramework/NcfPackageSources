/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: IValidatorEnvironment.cs
    File description: IValidatorEnvironment related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// Exposes request validation state to services that need to add model errors.
    /// </summary>
    public interface IValidatorEnvironment
    {
        /// <summary>Gets the current MVC model-state dictionary.</summary>
        ModelStateDictionary ModelState { get; }

        /// <summary>
        /// HttpContext
        /// </summary>
        HttpContext HttpContext { get; }
    }
}

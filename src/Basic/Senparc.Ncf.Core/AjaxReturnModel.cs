/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AjaxReturnModel.cs
    File description: AjaxReturnModel related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core
{

    /// <summary>
    /// Represents a non-generic AJAX operation result with success state and message.
    /// </summary>
    public class AjaxReturnModel
    {
        /// <summary>Gets or sets whether the represented AJAX operation succeeded.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the human-readable operation message.</summary>
        public string Msg { get; set; }
    }

    /// <summary>
    /// Represents an AJAX operation result carrying a typed response payload.
    /// </summary>
    /// <typeparam name="T">The response payload type.</typeparam>
    public class AjaxReturnModel<T> : AjaxReturnModel
    {
        /// <summary>Gets or sets the typed payload returned by the AJAX operation.</summary>
        public T Data { get; set; }

        /// <summary>
        /// Initializes an empty typed response result.
        /// </summary>
        public AjaxReturnModel()
        {

        }

        /// <summary>
        /// Initializes a typed response result with a payload.
        /// </summary>
        /// <param name="data">The response payload.</param>
        public AjaxReturnModel(T data) : this()
        {
            this.Data = data;
        }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AutoValidateAntiForgeryTokenModelConvention.cs
    File description: AutoValidateAntiForgeryTokenModelConvention related implementation

    Created by: Senparc - 20211215

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.12 Improved administration page and authorization support

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Senparc.Ncf.AreaBase.Conventions
{
    /// <summary>
    /// Adds antiforgery validation to page handlers that expose POST actions without an explicit validator.
    /// </summary>
    public class AutoValidateAntiForgeryTokenModelConvention : IPageConvention// IActionModelConvention
    {
        /// <summary>
        /// Applies <see cref="ValidateAntiForgeryTokenAttribute"/> when the action is eligible.
        /// </summary>
        /// <param name="action">The action model being conventionally configured.</param>
        public void Apply(ActionModel action)
        {
            if (IsConventionApplicable(action))
            {
                action.Filters.Add(new ValidateAntiForgeryTokenAttribute());
            }
        }

        /// <summary>
        /// Determines whether an action has a POST attribute but no antiforgery validation attribute.
        /// </summary>
        /// <param name="action">The action model to inspect.</param>
        /// <returns><see langword="true"/> when the convention should add validation.</returns>
        public bool IsConventionApplicable(ActionModel action)
        {
            if (action.Attributes.Any(f => f.GetType() == typeof(HttpPostAttribute)) &&
                !action.Attributes.Any(f => f.GetType() == typeof(ValidateAntiForgeryTokenAttribute)))
            {
                return true;
            }
            return false;
        }
    }
}

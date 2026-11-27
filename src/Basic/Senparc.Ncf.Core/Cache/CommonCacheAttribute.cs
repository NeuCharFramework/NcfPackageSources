/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: CommonCacheAttribute.cs
    File description: CommonCacheAttribute related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class CommonCacheAttribute : Attribute
    {

        /// <inheritdoc />
        public override bool IsDefaultAttribute()
        {
            return base.IsDefaultAttribute();
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <inheritdoc />
        public override bool Match(object obj)
        {
            return base.Match(obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Initializes the method-level common-cache marker attribute.
        /// </summary>
        public CommonCacheAttribute()
        {
            //System.Web.HttpContext.Current.Response.Write(this.GetType());
        }
    }
}

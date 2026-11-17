/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: CommonCacheAttribute.cs

    File description: CommonCacheAttribute related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Cache
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class CommonCacheAttribute : Attribute
    {

        public override bool IsDefaultAttribute()
        {
            return base.IsDefaultAttribute();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override bool Match(object obj)
        {
            return base.Match(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public CommonCacheAttribute()
        {
            //System.Web.HttpContext.Current.Response.Write(this.GetType());
        }
    }
}

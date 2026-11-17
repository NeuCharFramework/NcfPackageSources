/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AutoSetCacheAttribute.cs
    File description: AutoSetCacheAttribute related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Utility
{
    [AttributeUsageAttribute(AttributeTargets.Property)]
    public class AutoSetCacheAttribute : Attribute
    {
        public AutoSetCacheAttribute()
        {
        }
    }
}
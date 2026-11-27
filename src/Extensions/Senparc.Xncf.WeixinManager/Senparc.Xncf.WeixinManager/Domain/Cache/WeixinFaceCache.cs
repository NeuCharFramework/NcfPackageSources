/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: WeixinFaceCache.cs
    File description: WeixinFaceCache related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

    Modified by: Senparc - 20261127
    Change description: v0.25.1.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System.ComponentModel;
using System.Reflection;
using System;
using System.Collections.Generic;
using Senparc.Ncf.Core.Cache;
using Senparc.Xncf.WeixinManager.Domain.Models;

namespace Senparc.Xncf.WeixinManager.Domain.Cache
{
    //public interface IWeixinFaceCache : IBaseCache<List<KeyValuePair<string, WeixinFace>>>
    //{
    //}

    /// <summary>
    /// Caches the localized descriptions of available Weixin face values.
    /// </summary>
    public class WeixinFaceCache : BaseCache<List<KeyValuePair<string, WeixinFace>>>/*, IWeixinFaceCache*/
    {
        /// <summary>Gets the cache key used for face descriptions.</summary>
        public const string CACHE_KEY = "WeixinFaceCache";
        private const int timeout = 1440;

        /// <summary>
        /// Initializes the Weixin-face description cache with its daily timeout.
        /// </summary>
        public WeixinFaceCache()
            : base(CACHE_KEY)
        {
            TimeOut = timeout;
        }

        /// <summary>
        /// Rebuilds the cache from <see cref="WeixinFace"/> enum descriptions.
        /// </summary>
        /// <returns>The refreshed face-description pairs.</returns>
        public override List<KeyValuePair<string, WeixinFace>> Update()
        {
            var data = new List<KeyValuePair<string, WeixinFace>>();

            var enumType = typeof(WeixinFace);
            var names = enumType.GetEnumNames();
            foreach (var name in names)
            {
                var fi = enumType.GetField(name);
                var dna = (DescriptionAttribute)Attribute.GetCustomAttribute(fi, typeof(DescriptionAttribute));
                if (dna != null)
                {
                    var description = dna.Description;
                    data.Add(new KeyValuePair<string, WeixinFace>(description, (WeixinFace)Enum.Parse(enumType, name)));
                }
            }

            base.SetData(data, TimeOut, null);
            return base.Data;
        }
    }
}

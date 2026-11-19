/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AIPluginHub.cs
    File description: AIPluginHub related implementation

    Created by: Senparc - 20250121

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#nullable enable
namespace Senparc.Ncf.Core
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class AIPluginHub
    {
        #region Legacy section

        AIPluginHub() { }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static AIPluginHub Instance
        {
            get
            {
                return Nested.instance;
            }
        }

        class Nested
        {
            static Nested() { }

            internal static readonly AIPluginHub instance = new AIPluginHub();
        }

        #endregion


        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();


        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="pluginType"></param>

        public void Add(Type pluginType)
        {
            var key = pluginType.FullName;
            if (_types.ContainsKey(key))
            {
                return;
            }
            _types[key] = pluginType;
        }

        /// <summary>
        /// get Plugin type
        /// </summary>
        /// <param name="fullName"></param>
        /// <param name="patternSearch"></param>
        /// <returns></returns>
        public Type? GetPluginType(string fullName, bool patternSearch = false)
        {
            if (!patternSearch)
            {
                if (_types.ContainsKey(fullName))
                {
                    return _types[fullName];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var regex = new System.Text.RegularExpressions.Regex(fullName);
                foreach (var key in _types.Keys)
                {
                    if (regex.Match(key).Success)
                    {
                        return _types[key];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// getall Plugin type
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllPluginNames()
        {
            return _types.Values.Select(z=>z.FullName).OrderBy(z => z).ToList();
        }
    }
}

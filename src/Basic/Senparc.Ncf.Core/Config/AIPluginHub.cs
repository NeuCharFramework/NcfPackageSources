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
    /// Maintains the process-wide registry of AI plugin types discoverable by NCF.
    /// </summary>
    public class AIPluginHub
    {
        #region Legacy section

        AIPluginHub() { }

        /// <summary>
        /// Gets the singleton plugin registry for the current process.
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
        /// Registers a plugin type by its assembly-qualified name.
        /// Duplicate full names are ignored so repeated discovery passes remain idempotent.
        /// </summary>
        /// <param name="pluginType">The plugin type to register.</param>
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
        /// Resolves one registered plugin type by exact name or regular-expression pattern.
        /// </summary>
        /// <param name="fullName">The full type name or regular-expression pattern.</param>
        /// <param name="patternSearch">Whether <paramref name="fullName"/> should be treated as a regex.</param>
        /// <returns>The first matching plugin type, or <see langword="null"/> when none is registered.</returns>
        /// <exception cref="ArgumentException">Thrown when pattern search receives an invalid regular expression.</exception>
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
        /// Returns the full names of all registered plugin types in ordinal order.
        /// </summary>
        /// <returns>A new list containing the registered plugin names.</returns>
        public List<string> GetAllPluginNames()
        {
            return _types.Values.Select(z=>z.FullName).OrderBy(z => z).ToList();
        }
    }
}

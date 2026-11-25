/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FunctionRenderCollection.cs
    File description: FunctionRenderCollection related implementation

    Created by: Senparc - 20211012

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.XncfBase.FunctionRenders
{
    /// <summary>
    /// Indexes function-render metadata by registering module type and method group key.
    /// </summary>
    public class FunctionRenderCollection : ConcurrentDictionary<Type, ConcurrentDictionary<string, FunctionRenderBag>>
    {
        /// <summary>
        /// Builds the stable group key used for a declaring type and method.
        /// </summary>
        /// <param name="methodInfo">The reflected method to identify.</param>
        /// <returns>A key composed of the declaring type full name and method name.</returns>
        public static string GeGrouptKey(MethodInfo methodInfo)
        {
            return $"{methodInfo.DeclaringType.FullName}-{methodInfo.Name}";
        }

        /// <summary>
        /// Adds or replaces a function-render bag under the register type and method group.
        /// </summary>
        /// <param name="methodInfo">The reflected function method.</param>
        /// <param name="functionRenderAttribute">The metadata describing the function render.</param>
        /// <returns>The created function-render bag.</returns>
        public FunctionRenderBag Add(MethodInfo methodInfo, FunctionRenderAttribute functionRenderAttribute)
        {
            Type registerType = functionRenderAttribute.RegisterType;
            if (!this.ContainsKey(registerType))
            {
                this[registerType] = new ConcurrentDictionary<string, FunctionRenderBag>();
            }

            var registerGroup = this[registerType];

            var groupKey = GeGrouptKey(methodInfo);
            var functionRenderBag = new FunctionRenderBag(groupKey, methodInfo, functionRenderAttribute);
            registerGroup[groupKey] = functionRenderBag;
            return functionRenderBag;
        }

        /// <summary>
        /// Returns all function-render bags registered for a module register type.
        /// </summary>
        /// <param name="registerType">The module register type to query.</param>
        /// <returns>A snapshot of the registered function-render bags.</returns>
        public IReadOnlyList<FunctionRenderBag> GetByRegisterType(Type registerType)
        {
            if (registerType == null)
            {
                return Array.Empty<FunctionRenderBag>();
            }

            if (!TryGetValue(registerType, out var registerGroup) || registerGroup == null)
            {
                return Array.Empty<FunctionRenderBag>();
            }

            return registerGroup.Values.ToList();
        }

        /// <summary>
        /// Resolves a module UID to its function-render bags.
        /// </summary>
        /// <param name="moduleUid">The module UID to query.</param>
        /// <returns>A snapshot of the module's function-render bags.</returns>
        public IReadOnlyList<FunctionRenderBag> GetByModuleUid(string moduleUid)
        {
            if (string.IsNullOrWhiteSpace(moduleUid))
            {
                return Array.Empty<FunctionRenderBag>();
            }

            var register = XncfRegisterManager.RegisterList.FirstOrDefault(z => z.Uid.Equals(moduleUid, StringComparison.OrdinalIgnoreCase));
            if (register == null)
            {
                return Array.Empty<FunctionRenderBag>();
            }

            return GetByRegisterType(register.GetType());
        }

        /// <summary>
        /// Creates plugin instances for function-render symbols belonging to selected modules.
        /// Existing service registrations are preferred before reflection-based construction.
        /// </summary>
        /// <param name="serviceProvider">Provider used to resolve registered plugin instances.</param>
        /// <param name="symbolExpression">The expression that must contain a function-render symbol.</param>
        /// <param name="moduleUids">Module UIDs whose function-render plugins may be resolved.</param>
        /// <returns>A case-insensitive map keyed by declaring plugin type full name.</returns>
        public IReadOnlyDictionary<string, object> ResolveSymbolPlugins(IServiceProvider serviceProvider, string symbolExpression, IEnumerable<string> moduleUids)
        {
            if (!FunctionRenderSymbolHelper.HasFunctionRenderSymbol(symbolExpression) || serviceProvider == null || moduleUids == null)
            {
                return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            var pluginObjects = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var uidList = moduleUids.Where(z => !string.IsNullOrWhiteSpace(z)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var uid in uidList)
            {
                var functionBags = GetByModuleUid(uid);
                if (functionBags.Count == 0)
                {
                    continue;
                }

                foreach (var declaringType in functionBags.Select(z => z.MethodInfo?.DeclaringType).Where(z => z != null).Distinct())
                {
                    var key = declaringType.FullName;
                    if (pluginObjects.ContainsKey(key))
                    {
                        continue;
                    }

                    var plugin = serviceProvider.GetService(declaringType) ?? Activator.CreateInstance(declaringType);
                    if (plugin != null)
                    {
                        pluginObjects[key] = plugin;
                    }
                }
            }

            return pluginObjects;
        }
    }
}

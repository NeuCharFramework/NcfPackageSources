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
    ///FunctionRender collection. Key: unique identifier, value: MethodInfo
    /// </summary>
    public class FunctionRenderCollection : ConcurrentDictionary<Type, ConcurrentDictionary<string, FunctionRenderBag>>
    {
        /// <summary>
        /// Get the Key of a single Group
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static string GeGrouptKey(MethodInfo methodInfo)
        {
            return $"{methodInfo.DeclaringType.FullName}-{methodInfo.Name}";
        }

        /// <summary>
        ///Set FunctionRenderBag
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
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
        /// Get the FunctionRender collection under the specified registration type.
        /// </summary>
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
        /// Get the FunctionRender collection by module UID.
        /// </summary>
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
        /// When the input contains [#sym:FunctionRender], automatically convert the FunctionRender method in the specified module into an importable plug-in object.
        /// </summary>
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

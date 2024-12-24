using Senparc.Ncf.Core.AppServices;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.XncfBase.FunctionRenders
{
    /// <summary>
    /// FunctionRender 集合。Key：唯一标识，value：MethodInfo
    /// </summary>
    public class FunctionRenderCollection : ConcurrentDictionary<Type, ConcurrentDictionary<string, FunctionRenderBag>>
    {
        /// <summary>
        /// 获取 单个 Group 的 Key
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static string GeGrouptKey(MethodInfo methodInfo)
        {
            return $"{methodInfo.DeclaringType.FullName}-{methodInfo.Name}";
        }

        /// <summary>
        /// 设置 FunctionRenderBag
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
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Senparc.Ncf.XncfBase.FunctionRenders
{
    /// <summary>
    /// FunctionRender 集合。Key：唯一标识，value：MethodInfo
    /// </summary>
    public class FunctionRenderCollection : ConcurrentDictionary<string, MethodInfo>
    {
        /// <summary>
        /// 获取 Key
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public static string GetKey(MethodInfo methodInfo)
        {
            return $"{methodInfo.DeclaringType.FullName}-{methodInfo.Name}";
        }

        /// <summary>
        /// 设置项
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <returns></returns>
        public string SetItem(MethodInfo methodInfo)
        {
            var key = GetKey(methodInfo);
            base[key] = methodInfo;
            return key;
        }
    }
}

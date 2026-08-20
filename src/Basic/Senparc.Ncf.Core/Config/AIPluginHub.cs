/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：AIPluginHub.cs
    文件功能描述：AIPluginHub 相关实现
    
    
    创建标识：Senparc - 20250121
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
    /// 注册 AI 插件
    /// </summary>
    public class AIPluginHub
    {
        #region 单例

        AIPluginHub() { }

        /// <summary>
        /// AIPluginHub 的全局单例
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
        /// 添加 Plugin 类型
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
        /// 获取 Plugin 类型
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
        /// 获取所有 Plugin 类型
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllPluginNames()
        {
            return _types.Values.Select(z=>z.FullName).OrderBy(z => z).ToList();
        }
    }
}

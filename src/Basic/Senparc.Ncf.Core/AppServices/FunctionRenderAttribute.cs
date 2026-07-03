/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FunctionRenderAttribute.cs
    文件功能描述：FunctionRenderAttribute 相关实现
    
    
    创建标识：Senparc - 20211012
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AppServices
{
    [AttributeUsage(/*AttributeTargets.Class |*/ AttributeTargets.Method)]
    public class FunctionRenderAttribute : Attribute
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 说明
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 分类到 XNCF 模块的 Regster 类型
        /// </summary>
        public Type RegisterType { get; set; }

        public FunctionRenderAttribute(string name, string description, Type registerType/*TODO：可提供系统模块的默认值*/)
        {
            Name = name;
            Description = description;
            RegisterType = registerType;
        }

    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfMethodAttribute.cs
    文件功能描述：XncfMethodAttribute 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Xncf 模块特性 - 扩展方法
    /// </summary>
    public class XncfMethodAttribute : Attribute
    {
        public string Name { get; set; }

        public XncfMethodAttribute(string name)
        {
            Name = name;
        }
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：XncfAutoConfigurationMappingAttribute.cs
    文件功能描述：XncfAutoConfigurationMappingAttribute 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.Attributes
{
    /// <summary>
    /// 自动配置 ConfigurationMapping 特性
    /// <para>注意：添加此属性后，ConfigurationMapping 中的配置会被优先注入到 SenparcEntities 系统对象，<br></br>
    /// 否则，当某实体没有创建 ConfigurationMapping 时，会将其默认属性注入到 SenparcEntities 系统对象。</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class XncfAutoConfigurationMappingAttribute : Attribute
    {
    }
}

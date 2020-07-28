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

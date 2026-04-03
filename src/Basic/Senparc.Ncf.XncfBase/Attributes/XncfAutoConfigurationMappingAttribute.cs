using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.Attributes
{
    /// <summary>
    /// Automatically configure the ConfigurationMapping attribute
    /// <para>Note: After adding this attribute, the configuration in ConfigurationMapping will be injected into the SenparcEntities system object first,<br></br>
    /// Otherwise, when no ConfigurationMapping is created for an entity, its default properties will be injected into the SenparcEntities system object. </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class XncfAutoConfigurationMappingAttribute : Attribute
    {
    }
}

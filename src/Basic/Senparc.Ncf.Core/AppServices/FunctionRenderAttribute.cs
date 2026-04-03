using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AppServices
{
    [AttributeUsage(/*AttributeTargets.Class |*/ AttributeTargets.Method)]
    public class FunctionRenderAttribute : Attribute
    {
        /// <summary>
        ///name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// illustrate
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Regster type classified to XNCF module
        /// </summary>
        public Type RegisterType { get; set; }

        public FunctionRenderAttribute(string name, string description, Type registerType/*TODO: Default values ​​for system modules can be provided*/)
        {
            Name = name;
            Description = description;
            RegisterType = registerType;
        }

    }
}

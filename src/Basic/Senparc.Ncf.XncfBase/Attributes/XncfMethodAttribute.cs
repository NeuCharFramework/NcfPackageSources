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

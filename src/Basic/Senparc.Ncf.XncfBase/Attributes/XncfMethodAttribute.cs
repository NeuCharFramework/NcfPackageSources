using System;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    ///Xncf module attributes - extension methods
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

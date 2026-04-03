using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    /// enum type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumType<T> where T : struct
    {
        public T Value { get; set; }
        public EnumType() { }

        public EnumType(T value)
        {
            Value = value;
        }
    }

}

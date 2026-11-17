/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: EnumType.cs

    File description: EnumType related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase.Functions
{
    /// <summary>
    ///Legacy comment translated to English.
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

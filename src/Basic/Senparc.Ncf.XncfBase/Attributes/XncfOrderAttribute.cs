/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: XncfOrderAttribute.cs

    File description: XncfOrderAttribute related implementation

    

    

    Created by: Senparc - 20260704

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class XncfOrderAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        ///Legacy comment translated to English.
        public XncfOrderAttribute(int order)
        {
            Order = order;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int Order { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.WeixinManager
{
    /// <summary>
    /// Use MessageHandler to automatically identify the official account bound in the system 
    /// </summary>
    public class MpMessageHandlerAttribute : Attribute
    {
        public MpMessageHandlerAttribute(string name)
        {
            Name = name;
        }


        public string Name { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.WeixinManager
{
    /// <summary>
    /// 用自动识别在系统中绑定的公众号 MessageHandler 
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

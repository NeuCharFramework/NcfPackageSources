/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MpMessageHandlerAttribute.cs
    文件功能描述：MpMessageHandlerAttribute 相关实现
    
    
    创建标识：Senparc - 20250712
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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

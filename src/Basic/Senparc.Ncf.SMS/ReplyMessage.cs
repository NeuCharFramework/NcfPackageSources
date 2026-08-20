/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ReplyMessage.cs
    文件功能描述：ReplyMessage 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.SMS
{
    public interface IReplyMessage
    {
        int State { get; set; }
        int Id { get; set; }
        string PhoneNumber { get; set; }
        DateTime DateTime { get; set; }
    }

    public class ReplyMessage : IReplyMessage
    {
        public int State { get; set; }
        public int Id { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateTime { get; set; }
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ReplyMessageCollection.cs
    文件功能描述：ReplyMessageCollection 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Ncf.SMS
{
    public class ReplyMessageCollection
    {
        public List<IReplyMessage> MsgCollection { get; set; }

        public ReplyMessageCollection()
        {
            MsgCollection=new List<IReplyMessage>();
        }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ReplyMessageCollection.cs

    File description: ReplyMessageCollection related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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

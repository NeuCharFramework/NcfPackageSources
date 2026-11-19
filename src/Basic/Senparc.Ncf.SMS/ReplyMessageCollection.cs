/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ReplyMessageCollection.cs
    File description: ReplyMessageCollection related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview13 Updated SMS integration support and related localization metadata

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

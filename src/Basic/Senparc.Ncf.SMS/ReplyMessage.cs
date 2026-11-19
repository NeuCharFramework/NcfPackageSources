/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ReplyMessage.cs
    File description: ReplyMessage related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview13 Updated SMS integration support and related localization metadata

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

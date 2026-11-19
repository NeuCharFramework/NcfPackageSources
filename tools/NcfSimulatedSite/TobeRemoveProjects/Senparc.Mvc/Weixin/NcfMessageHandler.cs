/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: NcfMessageHandler.cs
    File description: NcfMessageHandler related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using Senparc.NeuChar.Entities;
using Senparc.NeuChar.Entities.Request;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageHandlers;
using System.IO;

namespace Senparc.Mvc.Weixin
{
    /// <summary>
    /// 
    /// </summary>
    public partial class NcfMessageHandler : MessageHandler<NcfMessageContext>
    {
        public NcfMessageHandler(Stream stream, PostModel postModel, int maxRecordCount)
            : base(stream, postModel, maxRecordCount)
        {
        }

        public override void OnExecuting()
        {

        }

        public override IResponseMessageBase OnTextRequest(RequestMessageText requestMessage)
        {
            var defaultResponseMessage = base.CreateResponseMessage<ResponseMessageText>();
            var requestHandler =
                requestMessage.StartHandler()
                    //Legacy comment translated to English.
                    .Keyword("code", () =>
                    {
                        defaultResponseMessage.Content =
                            @"code";
                        return defaultResponseMessage;
                    });


            return requestHandler.GetResponseMessage() as IResponseMessageBase;
        }

        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = @"感谢您关注！";
            return responseMessage;
        }
    }
}

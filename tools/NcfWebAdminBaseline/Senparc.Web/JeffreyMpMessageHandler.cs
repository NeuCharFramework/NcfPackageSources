/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: JeffreyMpMessageHandler.cs
    File description: JeffreyMpMessageHandler related implementation

    Created by: Senparc - 20241121

    Modified by: Senparc - 20261119
    Change description: v0.39.9 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.NeuChar.App.AppStore;
using Senparc.NeuChar.Entities;
using Senparc.NeuChar.MessageHandlers;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageContexts;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Xncf.WeixinManager;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Web
{
    [MpMessageHandlerAttribute("Conf")]
    public class JeffreyMpMessageHandler : MessageHandler<DefaultMpMessageContext>
    {
        private readonly MpAccountDto _mpAccountDto;
        private readonly ServiceProvider _services;


        //Legacy comment translated to English.
        public JeffreyMpMessageHandler(MpAccountDto mpAccountDto, Stream stream, PostModel postModel, int maxRecordCount, ServiceProvider services) : this(stream, postModel, maxRecordCount)
        {
            this._mpAccountDto = mpAccountDto;
            this._services = services;
        }

        public JeffreyMpMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0, bool onlyAllowEncryptMessage = false, DeveloperInfo developerInfo = null, IServiceProvider serviceProvider = null) : base(inputStream, postModel, maxRecordCount, onlyAllowEncryptMessage, developerInfo, serviceProvider)
        { 
        }
        public override Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
        {
            var requestContent = requestMessage.Content;
            var responseMessage = this.CreateResponseMessage<ResponseMessageText>();

            responseMessage.Content = requestContent.Replace("?", "!").Replace("？", "！").Replace("吗", "");

            return Task.FromResult<IResponseMessageBase>(responseMessage);
        }


        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "欢迎关注 Jeffrey Su 的公众号";
            return responseMessage;
        }
    }
}

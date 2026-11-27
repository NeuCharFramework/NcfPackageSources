/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: JeffreyMpMessageHandler.cs
    File description: JeffreyMpMessageHandler related implementation

    Created by: Senparc - 20241121

    Modified by: Senparc - 20261119
    Change description: v0.39.9 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.39.10 Improve source documentation, headers, and module metadata consistency.

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


        /// <summary>
        /// Initializes the message handler with the public-account metadata and
        /// the request context used by the base Weixin message handler.
        /// </summary>
        /// <param name="mpAccountDto">The public-account metadata.</param>
        /// <param name="stream">The raw request stream.</param>
        /// <param name="postModel">The parsed request model.</param>
        /// <param name="maxRecordCount">The maximum context record count.</param>
        /// <param name="services">The request service provider.</param>
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

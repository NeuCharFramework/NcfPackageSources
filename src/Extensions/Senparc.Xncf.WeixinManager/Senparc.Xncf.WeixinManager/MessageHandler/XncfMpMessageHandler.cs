/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfMpMessageHandler.cs
    File description: XncfMpMessageHandler related implementation

    Created by: Senparc - 20250712

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261021
    Change description: v0.24.16 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.24.16 add AITokenUsageRecorder and related services for token usage tracking

    Modified by: Senparc - 20261119
    Change description: v0.25.1 Localized Weixin account and administration surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Entities;
using Senparc.AI.AgentKernel;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.MessageQueue;
using Senparc.NeuChar.App.AppStore;
using Senparc.NeuChar.Context;
using Senparc.NeuChar.Entities;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.AI.AgentKernel.Handlers;

namespace Senparc.Xncf.WeixinManager
{
    public class XncfMpMessageHandler<TMC> : MessageHandler<TMC>
        where TMC : WechatAiContext, new()
    {
        protected readonly MpAccountDto _mpAccountDto;
        protected readonly ServiceProvider _services;

        public XncfMpMessageHandler(MpAccountDto mpAccountDto, Stream stream, PostModel postModel, int maxRecordCount, ServiceProvider services) : this(stream, postModel, maxRecordCount)
        {
            this._mpAccountDto = mpAccountDto;
            this._services = services;
        }

        public XncfMpMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0, bool onlyAllowEncryptMessage = false, DeveloperInfo developerInfo = null, IServiceProvider serviceProvider = null) : base(inputStream, postModel, maxRecordCount, onlyAllowEncryptMessage, developerInfo, serviceProvider)
        {
        }

        protected virtual Action RunBot(IServiceProvider services, RequestMessageText requestMessage) => async () =>
        {
            var dt = SystemTime.Now;
            //Legacy comment translated to English.

            ///Legacy comment translated to English.
            //var modelName = "gpt-35-turbo";//text-davinci-003

            //Legacy comment translated to English.
            var iWantToRun = await WechatAiContext.GetIWantToRunAsync(services, _mpAccountDto, requestMessage.FromUserName);
            AgentAiHandler agentAiHandler = iWantToRun.AgentAiHandler;

            //sendto AI model，getresult
            var aiModelService = services.GetService<AIModelService>();
            var result = aiModelService == null
                ? await iWantToRun.RunChatAsync(requestMessage.Content)
                : await aiModelService.RunChatWithUsageAsync(
                    iWantToRun,
                    requestMessage.Content,
                    modelAlias: "WeixinManager.XncfMpMessageHandler",
                    source: "WeixinManager.XncfMpMessageHandler");
            await Console.Out.WriteLineAsync("AI result.Output:" + result.OutputString);
            if (result == null)
            {
                await Console.Out.WriteLineAsync("result is null");
            }


            //Legacy comment translated to English.
            //Legacy comment translated to English.

            //var sendresult = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(_mpAccountDto.AppId, requestMessage.FromUserName, resultMsg);
            //await Console.Out.WriteLineAsync(sendresult.ToJson());


            //Legacy comment translated to English.
            await AfterRunBotAsync(services, requestMessage, _mpAccountDto, result, dt);
        };

        protected virtual async Task AfterRunBotAsync(IServiceProvider serviceProvider, RequestMessageText requestMessage, MpAccountDto mpAccountDto, SenparcAiResult senparcAiResult, DateTimeOffset startTime)
        {
            //asyncsend AI resulttouser

            Console.WriteLine("MpAccountDTO：" + mpAccountDto.ToJson(true));
            var resultMsg = $"{senparcAiResult.OutputString}\r\n -- AI 计算耗时：{SystemTime.DiffTotalMS(startTime)}毫秒";
            Console.WriteLine("公众号客服消息2：" + resultMsg);

            var result = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(mpAccountDto.AppId, requestMessage.FromUserName, resultMsg);
            await Console.Out.WriteLineAsync(result.ToJson());
            //Legacy comment translated to English.
        }

        public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
        {
            //var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            //Legacy comment translated to English.
            //return responseMessage;

            var services = base.ServiceProvider;

            var smq = new SenparcMessageQueue();
            var smqKey = "WechatBot:" + requestMessage.MsgId;
            smq.Add(smqKey, RunBot(services, requestMessage));

            //Legacy comment translated to English.
            return this.CreateResponseMessage<ResponseMessageNoResponse>();

            //Legacy comment translated to English.
            var reponseMessage = this.CreateResponseMessage<ResponseMessageText>();
            reponseMessage.Content = "消息已收到，正在思考中……";
            return reponseMessage;
        }


        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = "欢迎访问公众号：" + _mpAccountDto.Name;
            return responseMessage;
        }
    }
}

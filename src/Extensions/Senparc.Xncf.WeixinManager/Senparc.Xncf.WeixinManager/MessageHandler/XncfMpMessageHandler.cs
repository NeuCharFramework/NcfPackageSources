using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Senparc.AI.Entities;
using Senparc.AI.Kernel;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.MessageQueue;
using Senparc.NeuChar.App.AppStore;
using Senparc.NeuChar.Context;
using Senparc.NeuChar.Entities;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageHandlers;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;

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
            //TODO: A request to determine whether it is a "Vincent Picture"

            ////Define AI model
            //var modelName = "gpt-35-turbo";//text-davinci-003

            //Get AI Processor
            var iWantToRun = await WechatAiContext.GetIWantToRunAsync(services, _mpAccountDto, requestMessage.FromUserName);
            SemanticAiHandler semanticAiHandler = iWantToRun.SemanticAiHandler;

            //Send to AI model and get results
            var result = await semanticAiHandler.ChatAsync(iWantToRun, requestMessage.Content);
            await Console.Out.WriteLineAsync("AI result.Output:" + result.OutputString);
            if (result == null)
            {
                await Console.Out.WriteLineAsync("result is null");
            }


            //var resultMsg = $"{result.Output}\r\n -- AI calculation time: {SystemTime.DiffTotalMS(dt)} milliseconds";
            //Console.WriteLine("Official account customer service message: " + resultMsg);

            //var sendresult = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(_mpAccountDto.AppId, requestMessage.FromUserName, resultMsg);
            //await Console.Out.WriteLineAsync(sendresult.ToJson());


            //Execute the method after getting the result
            await AfterRunBotAsync(services, requestMessage, _mpAccountDto, result, dt);
        };

        protected virtual async Task AfterRunBotAsync(IServiceProvider serviceProvider, RequestMessageText requestMessage, MpAccountDto mpAccountDto, SenparcAiResult senparcAiResult, DateTimeOffset startTime)
        {
            //Asynchronously send AI results to users

            Console.WriteLine("MpAccountDTO：" + mpAccountDto.ToJson(true));
            var resultMsg = $"{senparcAiResult.OutputString}\r\n -- AI 计算耗时：{SystemTime.DiffTotalMS(startTime)}毫秒";
            Console.WriteLine("公众号客服消息2：" + resultMsg);

            var result = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(mpAccountDto.AppId, requestMessage.FromUserName, resultMsg);
            await Console.Out.WriteLineAsync(result.ToJson());
            //_ = Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(_mpAccountDto.AppId, requestMessage.FromUserName, $"Total time spent: {SystemTime.DiffTotalMS(dt)}ms");
        }

        public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
        {
            //var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            //responseMessage.Content = "You sent text:" + requestMessage.Content;
            //return responseMessage;

            var services = base.ServiceProvider;

            var smq = new SenparcMessageQueue();
            var smqKey = "WechatBot:" + requestMessage.MsgId;
            smq.Add(smqKey, RunBot(services, requestMessage));

            //Do not return any information
            return this.CreateResponseMessage<ResponseMessageNoResponse>();

            //Return clear text prompt
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

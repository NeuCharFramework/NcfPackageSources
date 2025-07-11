using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
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
            //TODO：判断是否为“文生图”的请求

            ////定义 AI 模型
            //var modelName = "gpt-35-turbo";//text-davinci-003

            //获取 AI 处理器
            var iWantToRun = await WechatAiContext.GetIWantToRunAsync(services, _mpAccountDto, requestMessage.FromUserName);
            SemanticAiHandler semanticAiHandler = iWantToRun.SemanticAiHandler;

            //发送到 AI 模型，获取结果
            var result = await semanticAiHandler.ChatAsync(iWantToRun, requestMessage.Content);
            await Console.Out.WriteLineAsync("AI result.Output:" + result.OutputString);
            if (result == null)
            {
                await Console.Out.WriteLineAsync("result is null");
            }


            //var resultMsg = $"{result.Output}\r\n -- AI 计算耗时：{SystemTime.DiffTotalMS(dt)}毫秒";
            //Console.WriteLine("公众号客服消息：" + resultMsg);

            //var sendresult = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(_mpAccountDto.AppId, requestMessage.FromUserName, resultMsg);
            //await Console.Out.WriteLineAsync(sendresult.ToJson());


            //获取到结果后执行方法
            await AfterRunBotAsync(services, requestMessage, _mpAccountDto, result, dt);
        };

        protected virtual async Task AfterRunBotAsync(IServiceProvider serviceProvider, RequestMessageText requestMessage, MpAccountDto mpAccountDto, SenparcAiResult senparcAiResult, DateTimeOffset startTime)
        {
            //异步发送 AI 结果到用户

            Console.WriteLine("MpAccountDTO：" + mpAccountDto.ToJson(true));
            var resultMsg = $"{senparcAiResult.OutputString}\r\n -- AI 计算耗时：{SystemTime.DiffTotalMS(startTime)}毫秒";
            Console.WriteLine("公众号客服消息2：" + resultMsg);

            var result = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(mpAccountDto.AppId, requestMessage.FromUserName, resultMsg);
            await Console.Out.WriteLineAsync(result.ToJson());
            //_ = Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(_mpAccountDto.AppId, requestMessage.FromUserName, $"总共耗时：{SystemTime.DiffTotalMS(dt)}ms");
        }

        public override async Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
        {
            //var responseMessage = this.CreateResponseMessage<ResponseMessageText>();
            //responseMessage.Content = "您发送了文字：" + requestMessage.Content;
            //return responseMessage;

            var services = base.ServiceProvider;

            var smq = new SenparcMessageQueue();
            var smqKey = "WechatBot:" + requestMessage.MsgId;
            smq.Add(smqKey, RunBot(services, requestMessage));

            //不返回任何信息
            return this.CreateResponseMessage<ResponseMessageNoResponse>();

            //返回明文提示
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

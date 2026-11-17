/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: MyMessageHandler.cs

    File description: MyMessageHandler related implementation

    

    

    Created by: Senparc - 20241028

    

    Modified by: Senparc - 20260702

    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling



----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.TextToImage;
using Senparc.AI;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Handlers;
using Senparc.NeuChar.Entities;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Xncf.WeixinManager;
using Senparc.Xncf.WeixinManager.Domain.Models.DatabaseModel.Dto;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Senparc.Web
{
    [MpMessageHandler("JeffreyMp")]
    public class MyMessageHandler : XncfMpMessageHandler<WechatAiContext>
    {
        public MyMessageHandler(MpAccountDto mpAccountDto, Stream stream, PostModel postModel, int maxRecordCount, ServiceProvider services) : base(mpAccountDto, stream, postModel, maxRecordCount, services)
        {

        }

        protected override Action RunBot(IServiceProvider services, RequestMessageText requestMessage)
        {
            return base.RunBot(services, requestMessage);
        }

        public override async Task<IResponseMessageBase> OnVoiceRequestAsync(RequestMessageVoice requestMessage)
        {
            var content = requestMessage.Recognition;
            var mediaId = requestMessage.MediaId;

            var responseMessage = base.CreateResponseMessage<ResponseMessageText>();
            responseMessage.Content = $"您刚才发送了语音：{content}。MediaId：{mediaId}";

            return responseMessage;
        }

        protected override async Task AfterRunBotAsync(IServiceProvider serviceProvider, RequestMessageText requestMessage, MpAccountDto mpAccountDto, SenparcAiResult senparcAiResult, DateTimeOffset startTime)
        {
            /* Legacy comment translated to English.*/

            /*

            var aiResultContent = senparcAiResult.OutputString;

            if (aiResultContent == "Img=True")

            {

                Legacy comment translated to English.



                var dalleSetting = ((SenparcAiSetting)Senparc.AI.Config.SenparcAiSetting)["AzureDallE3"];



                Legacy comment translated to English.

                var userId = "Jeffrey";

                var agentAiHandler = serviceProvider.GetService<AgentAiHandler>();

                var iWantTo = agentAiHandler.IWantTo(dalleSetting)

                                    .ConfigModel(ConfigModel.ImageGeneration, userId)

                                    .BuildKernel();



Legacy comment translated to English.

Legacy comment translated to English.

                var dallE = iWantTo.GetRequiredService<ITextToImageService>();





                var imageUrl = await dallE.GenerateImageAsync(requestMessage.Content, 1024, 1024);

Legacy comment translated to English.



                Legacy comment translated to English.



                Legacy comment translated to English.

                var filePath = Senparc.CO2NET.Utilities.ServerUtility.ContentRootMapPath($"~/Senparc.AI.Dalle-{SystemTime.NowTicks}.jpg");



                using (var fs = new FileStream(filePath, FileMode.OpenOrCreate))

                {

                    await Senparc.CO2NET.HttpUtility.Get.DownloadAsync(serviceProvider, imageUrl, fs);

                    await fs.FlushAsync();



                    fs.Close();

                    Legacy comment translated to English.

                }



                try

                {

                    Legacy comment translated to English.

                    var uploadResult = await Senparc.Weixin.MP.AdvancedAPIs.MediaApi.UploadTemporaryMediaAsync(mpAccountDto.AppId, Senparc.Weixin.MP.UploadMediaFileType.image, filePath, timeOut: 50000000);



                    Legacy comment translated to English.

                    await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendImageAsync(mpAccountDto.AppId, requestMessage.FromUserName, uploadResult.media_id);

                }

                catch (Exception ex)

                {

                    await Console.Out.WriteLineAsync(ex.Message);

                    await Console.Out.WriteLineAsync(ex.StackTrace?.ToString());

                    await Console.Out.WriteLineAsync(ex.InnerException?.Message);



                    Legacy comment translated to English.



                }

Legacy comment translated to English.

            }

            else

            {

                await base.AfterRunBotAsync(serviceProvider, requestMessage, mpAccountDto, senparcAiResult, startTime);

            }

            */
        }
    }
}

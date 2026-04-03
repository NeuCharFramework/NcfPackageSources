using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.TextToImage;
using Senparc.AI;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
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
            var aiResultContent = senparcAiResult.OutputString;
            if (aiResultContent == "Img=True")
            {
                _ = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(mpAccountDto.AppId, requestMessage.FromUserName, $"我开始画画啦！");

                var dalleSetting = ((SenparcAiSetting)Senparc.AI.Config.SenparcAiSetting)["AzureDallE3"];

                //Draw the picture and return
                var userId = "Jeffrey";
                var semanticAiHandler = serviceProvider.GetService<SemanticAiHandler>();
                var iWantTo = semanticAiHandler.IWantTo(dalleSetting)
                                    .ConfigModel(ConfigModel.ImageGeneration, userId)
                                    .BuildKernel();

#pragma warning disable SKEXP0002 // Types are for evaluation only and may be changed or removed in future updates. Cancel this diagnostic to continue.
#pragma warning disable SKEXP0001 // Types are for evaluation only and may be changed or removed in future updates. Cancel this diagnostic to continue.
                var dallE = iWantTo.GetRequiredService<ITextToImageService>();


                var imageUrl = await dallE.GenerateImageAsync(requestMessage.Content, 1024, 1024);
#pragma warning restore SKEXP0001 // Types are for evaluation only and may be changed or removed in future updates. Cancel this diagnostic to continue.

                _ = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(mpAccountDto.AppId, requestMessage.FromUserName, $"图片已生成，正在保存并推送（{imageUrl}）");

                //Start saving pictures
                var filePath = Senparc.CO2NET.Utilities.ServerUtility.ContentRootMapPath($"~/Senparc.AI.Dalle-{SystemTime.NowTicks}.jpg");

                using (var fs = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    await Senparc.CO2NET.HttpUtility.Get.DownloadAsync(serviceProvider, imageUrl, fs);
                    await fs.FlushAsync();

                    fs.Close();
                    await Console.Out.WriteLineAsync("图片已保存：" + filePath);
                }

                try
                {
                    //Save WeChat picture material
                    var uploadResult = await Senparc.Weixin.MP.AdvancedAPIs.MediaApi.UploadTemporaryMediaAsync(mpAccountDto.AppId, Senparc.Weixin.MP.UploadMediaFileType.image, filePath, timeOut: 50000000);

                    //Push pictures
                    await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendImageAsync(mpAccountDto.AppId, requestMessage.FromUserName, uploadResult.media_id);
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync(ex.Message);
                    await Console.Out.WriteLineAsync(ex.StackTrace?.ToString());
                    await Console.Out.WriteLineAsync(ex.InnerException?.Message);

                    _ = await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(mpAccountDto.AppId, requestMessage.FromUserName, $"图片生成失败：" + ex.Message);

                }
#pragma warning restore SKEXP0002 // Types are for evaluation only and may be changed or removed in future updates. Cancel this diagnostic to continue.
            }
            else
            {
                await base.AfterRunBotAsync(serviceProvider, requestMessage, mpAccountDto, senparcAiResult, startTime);
            }
        }
    }
}

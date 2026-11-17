using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Senparc.CO2NET.AspNet.HttpUtility;
using Senparc.CO2NET.HttpUtility;
using Senparc.CO2NET.Trace;
using Senparc.CO2NET.Utilities;
using Senparc.Core.Utility;
using Senparc.Mvc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.MP;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MvcExtension;
using System;
using System.IO;

namespace Senparc.Mvc.Controllers
{
    public class WeixinController : Controller
    {
        private readonly SenparcWeixinSetting _senparcWeixinSetting;
        private readonly Func<string> _getRandomFileName = () => DateTime.Now.ToString("yyyyMMdd-HHmmss") + Guid.NewGuid().ToString("n").Substring(0, 6);
        public WeixinController(IOptions<SenparcWeixinSetting> options)
        {
            _senparcWeixinSetting = options.Value;
        }
        [HttpGet]
        [ActionName("Index")]
        public ActionResult Get(string signature, string timestamp, string nonce, string echostr)
        {
            if (CheckSignature.Check(signature, timestamp, nonce, _senparcWeixinSetting.Token))
            {
                return Content(echostr); //Legacy comment translated to English.
            }
            else
            {
                return Content("failed:" + signature + "," + CheckSignature.GetSignature(timestamp, nonce, _senparcWeixinSetting.Token));
            }
        }

        /// <summary>
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        ///Legacy comment translated to English.
        /// </summary>
        [HttpPost]
        [ActionName("Index")]
        public ActionResult Post(PostModel postModel)
        {
            if (!CheckSignature.Check(postModel.Signature, postModel.Timestamp, postModel.Nonce, _senparcWeixinSetting.Token))
            {
                return Content("参数错误！");
            }

            #region Legacy section

            postModel.Token = _senparcWeixinSetting.Token;//Legacy comment translated to English.
            postModel.EncodingAESKey = "";//Legacy comment translated to English.
            postModel.AppId = _senparcWeixinSetting.WeixinAppId;//Legacy comment translated to English.

            #endregion

            //Legacy comment translated to English.
            var maxRecordCount = 10;

            //Legacy comment translated to English.
            var messageHandler = new NcfMessageHandler(Request.GetRequestMemoryStream(), postModel, maxRecordCount);

            #region Legacy section

            /* Legacy comment translated to English.

             * Legacy comment translated to English.*/
            messageHandler.OmitRepeatedMessage = true;//Legacy comment translated to English.

            #endregion

            try
            {
                #region Legacy section

                var logPath = ServerUtility.ContentRootMapPath(string.Format("~/logs/mp/{0}/", DateTime.Now.ToString("yyyy-MM-dd")));
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }

                //Legacy comment translated to English.
                messageHandler.RequestDocument.Save(Path.Combine(logPath, string.Format("{0}_Request_{1}_{2}.txt", _getRandomFileName(),
                    messageHandler.RequestMessage.FromUserName,
                    messageHandler.RequestMessage.MsgType)));
                if (messageHandler.UsingEncryptMessage)
                {
                    messageHandler.EcryptRequestDocument.Save(Path.Combine(logPath, string.Format("{0}_Request_Ecrypt_{1}_{2}.txt", _getRandomFileName(),
                        messageHandler.RequestMessage.FromUserName,
                        messageHandler.RequestMessage.MsgType)));
                }

                #endregion

                //Legacy comment translated to English.
                messageHandler.Execute();

                #region Legacy section

                //Legacy comment translated to English.

                //if (messageHandler.ResponseDocument == null)
                //{
                //    throw new Exception(messageHandler.RequestDocument.ToString());
                //}
                if (messageHandler.ResponseDocument != null)
                {
                    messageHandler.ResponseDocument.Save(Path.Combine(logPath, string.Format("{0}_Response_{1}_{2}.txt", _getRandomFileName(),
                        messageHandler.ResponseMessage.ToUserName,
                        messageHandler.ResponseMessage.MsgType)));
                }

                if (messageHandler.UsingEncryptMessage && messageHandler.FinalResponseDocument != null)
                {
                    //Legacy comment translated to English.
                    messageHandler.FinalResponseDocument.Save(Path.Combine(logPath, string.Format("{0}_Response_Final_{1}_{2}.txt", _getRandomFileName(),
                        messageHandler.ResponseMessage.ToUserName,
                        messageHandler.ResponseMessage.MsgType)));
                }

                #endregion

                //return Content(messageHandler.ResponseDocument.ToString());//v0.7-
                //return new WeixinResult(messageHandler);//v0.8+
                return new FixWeixinBugWeixinResult(messageHandler);//Legacy comment translated to English.
            }
            catch (Exception ex)
            {
                #region Legacy section
                SenparcTrace.Log($"MessageHandler错误：{ex.Message}");

                using (TextWriter tw = new StreamWriter(ServerUtility.ContentRootMapPath("~/App_Data/Error_" + _getRandomFileName() + ".txt")))
                {
                    tw.WriteLine("ExecptionMessage:" + ex.Message);
                    tw.WriteLine(ex.Source);
                    tw.WriteLine(ex.StackTrace);
                    //tw.WriteLine("InnerExecptionMessage:" + ex.InnerException.Message);

                    if (messageHandler.ResponseDocument != null)
                    {
                        tw.WriteLine(messageHandler.ResponseDocument.ToString());
                    }

                    if (ex.InnerException != null)
                    {
                        tw.WriteLine("========= InnerException =========");
                        tw.WriteLine(ex.InnerException.Message);
                        tw.WriteLine(ex.InnerException.Source);
                        tw.WriteLine(ex.InnerException.StackTrace);
                    }

                    tw.Flush();
                    tw.Close();
                }
                return Content("");
                #endregion
            }
        }
    }
}

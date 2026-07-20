/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：OperationQueueService.cs
    文件功能描述：OperationQueueService 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260717
    修改描述：v0.3.0 为账户模块接入多语言资源与功能文案本地化

----------------------------------------------------------------*/
using System;
using System.IO;
using System.Threading.Tasks;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Log;
using Senparc.Ncf.Utility;
using Senparc.Ncf.Service;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Xncf.Accounts.Domain.Models;
using Senparc.Service;
using Senparc.Xncf.Accounts.Domain.Services;

namespace Senparc.Xncf.Accounts.Domain.OperationQueue
{
    public class OperationQueueService
    {
        /// <summary>
        /// 下载图片到指定文件
        /// </summary>
        /// <param name="picUrl"></param>
        /// <param name="fileName"></param>
        public async Task DownLoadPicAsync(string picUrl, string fileName)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var serviceProvider = SenparcDI.GetServiceProvider();
                await CO2NET.HttpUtility.Get.DownloadAsync(serviceProvider, picUrl, stream);
                using (var fs = new FileStream(Server.GetMapPath("~" + fileName), FileMode.CreateNew))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fs);
                    fs.Flush();
                }
            }
        }

        /// <summary>
        /// 更新用户头像
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public async Task<Account> UpdateAccountHeadImgAsync(IServiceProvider serviceProvider, int accountId, string headImgUrl)
        {
            if (headImgUrl.IsNullOrEmpty())
            {
                throw new Exception(AccountsResource.Format(
                    "Accounts.HeadImageUrlMissing",
                    "头像地址为空，账户 ID：{0}",
                    accountId));
            }

            var accountService = serviceProvider.GetService<AccountService>();
            using (var wrap = accountService.InstanceAutoDetectChangeContextWrap())
            {
                var account = accountService.GetObject(z => z.Id == accountId);
                if (account == null)
                {
                    throw new Exception(AccountsResource.Format(
                        "Accounts.UserIdMissing",
                        "用户 ID 不存在！ID：{0}，头像地址：{1}",
                        accountId,
                        headImgUrl));
                }

                var fileName = $@"/Upload/User/headimgurl.{DateTime.Now.Ticks + Guid.NewGuid().ToString("n").Substring(0, 8)}.jpg";

                //下载图片
                var t3 = DateTime.Now;
                await DownLoadPicAsync(headImgUrl, fileName);
                var t4 = DateTime.Now;
                LogUtility.Account.Debug($"更新用户头像（ID：{accountId}，UserName：{account.UserName}），下载图片耗时：{(t4 - t3).TotalMilliseconds}ms");//测试耗时：4226.5869ms

                account.PicUrl = fileName;//不使用七牛或其他云储存，就使用本地路径
                accountService.SaveObject(account);
                return account;
            }
        }
    }
}

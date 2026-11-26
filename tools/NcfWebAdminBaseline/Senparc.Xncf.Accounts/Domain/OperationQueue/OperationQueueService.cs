/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: OperationQueueService.cs
    File description: Related implementation.

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260724
    Change description: v0.3.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.3.14 Updated module behavior, localization metadata, and English source comments

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
    /// <summary>Implements deferred account-avatar download and update operations.</summary>
    public class OperationQueueService
    {
        /// <summary>Downloads a remote avatar image to an application-relative path.</summary>
        /// <param name="picUrl">The remote image URL.</param>
        /// <param name="fileName">The destination path.</param>
        /// <exception cref="IOException">Thrown when the destination cannot be written.</exception>
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

        /// <summary>Downloads and persists a user's avatar image.</summary>
        /// <param name="serviceProvider">The scoped service provider.</param>
        /// <param name="accountId">The account identifier.</param>
        /// <param name="headImgUrl">The remote avatar URL.</param>
        /// <returns>The updated account.</returns>
        /// <exception cref="Exception">Thrown when the URL is empty or the account is missing.</exception>
        public async Task<Account> UpdateAccountHeadImgAsync(IServiceProvider serviceProvider, int accountId, string headImgUrl)
        {
            if (headImgUrl.IsNullOrEmpty())
            {
                throw new Exception("headImgUrl为null，accountId：" + accountId);
            }

            var accountService = serviceProvider.GetService<AccountService>();
            using (var wrap = accountService.InstanceAutoDetectChangeContextWrap())
            {
                var account = accountService.GetObject(z => z.Id == accountId);
                if (account == null)
                {
                    throw new Exception($"UserId不存在！ID：{accountId}，HeadImgUrl：{headImgUrl}");
                }

                var fileName = $@"/Upload/User/headimgurl.{DateTime.Now.Ticks + Guid.NewGuid().ToString("n").Substring(0, 8)}.jpg";

                //Legacy comment translated to English.
                var t3 = DateTime.Now;
                await DownLoadPicAsync(headImgUrl, fileName);
                var t4 = DateTime.Now;
                LogUtility.Account.Debug($"更新用户头像（ID：{accountId}，UserName：{account.UserName}），下载图片耗时：{(t4 - t3).TotalMilliseconds}ms");//Legacy comment translated to English.

                account.PicUrl = fileName;//Legacy comment translated to English.
                accountService.SaveObject(account);
                return account;
            }
        }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using Senparc.CO2NET;
using Senparc.CO2NET.Extensions;
using Senparc.Core.Models;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using Senparc.Ncf.Log;
using Senparc.Ncf.Utility;
using Senparc.Weixin.HttpUtility;
using Senparc.Ncf.Service;

namespace Senparc.Service.OperationQueue
{
    /// <summary>
    /// Implements deferred account-image download and update operations.
    /// </summary>
    public class OperationQueueService
    {
        /// <summary>
        /// Downloads a remote image and writes it below the application content root.
        /// </summary>
        /// <param name="picUrl">The remote image URL.</param>
        /// <param name="fileName">The application-relative destination path.</param>
        /// <exception cref="IOException">Thrown when the destination cannot be created or written.</exception>
        public async Task DownLoadPicAsync(string picUrl, string fileName)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                await CO2NET.HttpUtility.Get.DownloadAsync(picUrl, stream);
                using (var fs = new FileStream(Server.GetMapPath("~" + fileName), FileMode.CreateNew))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    stream.CopyTo(fs);
                    fs.Flush();
                }
            }
        }

        /// <summary>
        /// Downloads a new avatar image, updates the account picture path, and persists
        /// the account change.
        /// </summary>
        /// <param name="accountId">The account identifier to update.</param>
        /// <param name="headImgUrl">The remote avatar image URL.</param>
        /// <returns>The updated account entity.</returns>
        /// <exception cref="Exception">
        /// Thrown when the image URL is empty or the account cannot be found.
        /// </exception>
        public async Task<Account> UpdateAccountHeadImgAsync(int accountId, string headImgUrl)
        {
            if (headImgUrl.IsNullOrEmpty())
            {
                throw new Exception("headImgUrl为null，accountId：" + accountId);
            }

            var accountService = SenparcDI.GetService<AccountService>();
            using (var wrap = accountService.InstanceAutoDetectChangeContextWrap())
            {
                var account = accountService.GetObject(z => z.Id == accountId);
                if (account == null)
                {
                    throw new Exception($"UserId不存在！ID：{accountId}，HeadImgUrl：{headImgUrl}");
                }

                var fileName = $@"/Upload/User/headimgurl.{DateTime.Now.Ticks + Guid.NewGuid().ToString("n").Substring(0, 8)}.jpg";

                var t3 = DateTime.Now;
                await DownLoadPicAsync(headImgUrl, fileName);
                var t4 = DateTime.Now;
                LogUtility.Account.Debug($"更新用户头像（ID：{accountId}，UserName：{account.UserName}），下载图片耗时：{(t4 - t3).TotalMilliseconds}ms");

                account.PicUrl = fileName;
                accountService.SaveObject(account);
                return account;
            }
        }
    }
}

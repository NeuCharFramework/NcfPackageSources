/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SendEmailCache.cs
    File description: SendEmailCache related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.12.16 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.12.16.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Cache;
using Senparc.Ncf.Core.Config;
using Senparc.Ncf.Core.DI;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Utility;
using Senparc.Xncf.EmailExtension.Domain.Models;

namespace Senparc.Xncf.EmailExtension.Domain.Cache
{
    public interface ISendEmailCache : IBaseCache<List<AutoSendEmail>>
    {
        /// <summary>
        /// Returns all persisted automatic-email records.
        /// </summary>
        List<AutoSendEmail> GetAllList();
        /// <summary>
        /// Inserts one email record into persistent storage.
        /// </summary>
        /// <param name="email">The record to insert.</param>
        /// <returns><see langword="true"/> when insertion succeeds.</returns>
        bool InsertEmail(AutoSendEmail email);
        /// <summary>
        /// Removes an email after successful delivery.
        /// </summary>
        /// <param name="email">The delivered record.</param>
        /// <returns><see langword="true"/> when removal succeeds.</returns>
        bool SendSuccess(AutoSendEmail email);
        /// <summary>
        /// Records a failed delivery attempt.
        /// </summary>
        /// <param name="email">The record whose failure count should increase.</param>
        /// <returns><see langword="true"/> when the update succeeds.</returns>
        bool SendFail(AutoSendEmail email);
        /// <summary>
        /// Replaces send counts for selected records.
        /// </summary>
        /// <param name="idList">The record identifiers to update.</param>
        /// <param name="sendCount">The replacement count.</param>
        /// <returns><see langword="true"/> when persistence succeeds.</returns>
        bool SetSendCount(List<int> idList, int sendCount);
        /// <summary>
        /// Deletes records matching the supplied identifiers.
        /// </summary>
        /// <param name="ids">The record identifiers to delete.</param>
        void DeleteEmail(List<int> ids);
    }

    [AutoDIType(DILifecycleType.Singleton)]
    public class SendEmailCache : BaseCache<List<AutoSendEmail>>, ISendEmailCache
    {
        public const string CACHE_KEY = "SendEmailCache";

        //private string filePath;
        private XmlDataContext xmlCtx;
        //private HttpContext _context;
        /// <summary>
        /// Initializes the singleton email cache and its XML persistence context.
        /// </summary>
        public SendEmailCache(/*HttpContext context*/)
            : base(CACHE_KEY, null)
        {
            base.TimeOut = 1440;
            xmlCtx = new XmlDataContext();//Legacy comment translated to English.
            //filePath = ServerUtility.ContentRootMapPath("~/APP_Data/SendEmail.config");//context.Server.MapPath("~/APP_Data/SendEmail.config");
            //HttpContext.Current.Response.Write(filePath);
        }


        /// <inheritdoc/>
        /// <remarks>
        /// Refreshes the base cache with records whose send count is below the configured retry limit.
        /// </remarks>
        public override List<AutoSendEmail> Update()
        {
            int maxSendCount = Register.MaxSendEmailTimes;//Legacy comment translated to English.
            List<AutoSendEmail> data = this.GetAllList()
                                        .Where(z => z.SendCount < maxSendCount).OrderBy(z => z.SendCount).OrderBy(z => z.Id).ToList();
            base.SetData(data, base.TimeOut, null);
            return data;
        }

        /// <summary>
        /// Returns all persisted automatic-email records.
        /// </summary>
        /// <returns>The current XML-backed email list.</returns>
        public List<AutoSendEmail> GetAllList()
        {
            return xmlCtx.GetXmlList<AutoSendEmail>();
        }

        /// <summary>
        /// Inserts an email into XML storage and refreshes the filtered cache on success.
        /// </summary>
        /// <param name="email">The email record to persist.</param>
        /// <returns><see langword="true"/> when persistence succeeds.</returns>
        public bool InsertEmail(AutoSendEmail email)
        {
            bool success = xmlCtx.Insert(email);
            if (success)
            {
                this.Update();
            }
            return success;
        }

        /// <summary>
        /// Deletes email records matching the supplied identifiers and refreshes the cache.
        /// </summary>
        /// <param name="ids">The persisted email identifiers to delete.</param>
        public void DeleteEmail(List<int> ids)
        {
            var allList = this.GetAllList();
            foreach (var id in ids)
            {
                var email = allList.FirstOrDefault(z => z.Id == id);
                if (email == null)
                {
                    continue;
                }
                xmlCtx.Delete(email);
            }
            this.Update();
        }

        /// <summary>
        /// Removes a successfully sent email from XML storage and the in-memory cache.
        /// </summary>
        /// <param name="email">The sent email record.</param>
        /// <returns><see langword="true"/> when the record was removed.</returns>
        public bool SendSuccess(AutoSendEmail email)
        {
            bool success = xmlCtx.Delete(email);
            if (success)
            {
                success = this.Data.Remove(email);
            }
            return success;
        }

        /// <summary>
        /// Increments a failed email's send count, stores the latest send time, and refreshes the cache.
        /// </summary>
        /// <param name="email">The email record whose send failed.</param>
        /// <returns><see langword="true"/> when the updated record is persisted.</returns>
        public bool SendFail(AutoSendEmail email)
        {
            email.SendCount++;
            email.LastSendTime = DateTime.Now;
            bool success = xmlCtx.Save(new[] { email });
            if (success)
            {
                this.Update();
            }
            return success;
        }

        /// <summary>
        /// Sets the send count for the selected email records and refreshes the cache.
        /// </summary>
        /// <param name="idList">The email identifiers to update.</param>
        /// <param name="sendCount">The replacement send count.</param>
        /// <returns><see langword="true"/> when the updated records are persisted.</returns>
        public bool SetSendCount(List<int> idList, int sendCount)
        {
            var changeList = this.GetAllList().FindAll(z => idList.Contains(z.Id));
            changeList.ForEach(z => z.SendCount = sendCount);
            bool success = xmlCtx.Save(changeList);
            if (success)
            {
                this.Update();
            }
            return success;
        }
    }
}

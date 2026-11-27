/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: FeedBackService.cs
    File description: FeedBackService related implementation

    Created by: Senparc - 20211128

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.15.14 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.15.14.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using Senparc.Ncf.Log;
using Senparc.Ncf.Service;
using Senparc.Xncf.SystemManager.ACL.Repository;
using Senparc.Xncf.SystemManager.Domain.DatabaseModel;

namespace Senparc.Xncf.SystemManager.Domain.Services
{
    public class FeedBackService : ClientServiceBase<FeedBack>
    {
        /// <summary>
        /// Initializes feedback persistence.
        /// </summary>
        /// <param name="repo">The feedback repository.</param>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public FeedBackService(FeedBackRepository repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

        /// <summary>
        /// Creates a feedback record or updates the record identified by <paramref name="id"/>.
        /// </summary>
        /// <param name="content">The feedback content.</param>
        /// <param name="userId">The account identifier associated with the feedback.</param>
        /// <param name="id">The existing feedback identifier, or zero to create.</param>
        /// <returns>The persisted feedback entity.</returns>
        public FeedBack CreateOrUpdate( string content, int userId, int id = 0)
        {
            var obj = GetObject(z => z.Id == id) ?? new FeedBack()
            {
                AccountId = userId,
                Content = content,
                AddTime = DateTime.Now
            };
            SaveObject(obj);
            return obj;
        }

        /// <inheritdoc />
        public override void SaveObject(FeedBack obj)
        {
            var isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("{1}ݣID{0}", obj.Id, isInsert ? "" : "༭");
        }

        /// <inheritdoc />
        public override void DeleteObject(FeedBack obj)
        {
            obj.Flag = true;
            base.SaveObject(obj);
            LogUtility.WebLogger.Info($"ɾݣID{obj.Id}");
        }
    }
}

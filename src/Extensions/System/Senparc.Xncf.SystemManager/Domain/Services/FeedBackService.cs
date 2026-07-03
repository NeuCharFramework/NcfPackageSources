/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：FeedBackService.cs
    文件功能描述：FeedBackService 相关实现
    
    
    创建标识：Senparc - 20211128
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        public FeedBackService(FeedBackRepository repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {
        }

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

        public override void SaveObject(FeedBack obj)
        {
            var isInsert = base.IsInsert(obj);
            base.SaveObject(obj);
            LogUtility.WebLogger.InfoFormat("{1}ݣID{0}", obj.Id, isInsert ? "" : "༭");
        }

        public override void DeleteObject(FeedBack obj)
        {
            obj.Flag = true;
            base.SaveObject(obj);
            LogUtility.WebLogger.Info($"ɾݣID{obj.Id}");
        }
    }
}
/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ClientRepositoryBase.cs
    文件功能描述：ClientRepositoryBase 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using Senparc.CO2NET;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;

namespace Senparc.Ncf.Repository
{
    public interface IClientRepositoryBase<T> : IRepositoryBase<T> where T : EntityBase // global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        INcfDbData DB { get; }
    }

    public class ClientRepositoryBase<T> : RepositoryBase<T>, IClientRepositoryBase<T> where T : EntityBase // global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        public INcfDbData DB
        {
            get
            {
                return base.BaseDB; //as INcfDbData;
            }
        }

        //public ClientRepositoryBase() : this(null) { }

        public ClientRepositoryBase(INcfDbData db) : base(db)
        {
            //System.Web.HttpContext.Current.Response.Write("-"+this.GetType().Name + "<br />");
            var keys = EntitySetKeys.GetAllEntitySetInfo();
            _entitySetName = keys[typeof(T)].SetName;
        }
    }
}

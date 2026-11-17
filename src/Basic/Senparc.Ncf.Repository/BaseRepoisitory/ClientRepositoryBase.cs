/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ClientRepositoryBase.cs

    File description: ClientRepositoryBase related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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

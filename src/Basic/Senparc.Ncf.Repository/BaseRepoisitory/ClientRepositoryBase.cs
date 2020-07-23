using Senparc.CO2NET;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;

namespace Senparc.Ncf.Repository
{
    public interface IClientRepositoryBase<T> : IRepositoryBase<T> where T : EntityBase // global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        ISqlBaseFinanceData DB { get; }
    }

    public class ClientRepositoryBase<T> : RepositoryBase<T>, IClientRepositoryBase<T> where T : EntityBase // global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        public ISqlBaseFinanceData DB
        {
            get
            {
                return base.BaseDB; //as ISqlClientFinanceData;
            }
        }

        //public ClientRepositoryBase() : this(null) { }

        public ClientRepositoryBase(ISqlBaseFinanceData db) : base(db)
        {
            //System.Web.HttpContext.Current.Response.Write("-"+this.GetType().Name + "<br />");
            var keys = EntitySetKeys.GetAllEntitySetInfo();
            _entitySetName = keys[typeof(T)].SetName;
        }


    }
}

﻿
using Microsoft.EntityFrameworkCore;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;

namespace Senparc.Xncf.SystemCore.Domain.Database
{
    public interface INcfClientDbData : INcfDbData
    {
        BasePoolEntities DataContext { get; }
    }

    //[Pluggable("ClientDatabase")]
    public class NcfClientDbData : NcfDbData, INcfDbData, INcfClientDbData
    {
        private BasePoolEntities dataContext;

        //public NcfClientDbData() { }

        public NcfClientDbData(BasePoolEntities senparcEntities)
        {
            dataContext = senparcEntities;
        }


        public BasePoolEntities DataContext
        {
            get
            {
                return BaseDataContext as BasePoolEntities;
            }
        }

        /// <summary>
        /// 关闭连接（长时间保持一个连接操作会导致数据库操作时间逐渐变长）
        /// </summary>
        public override void CloseConnection()
        {
            //COCONET 升级到.net core的过程中注释掉
            //BaseDataContext.Database.Connection.Close();
            dataContext = null;
        }

        public override DbContext BaseDataContext
        {
            get
            {
                if (dataContext == null)
                {
                    //var connectionString = Ncf.Core.Config.SenparcDatabaseConfigs.ClientConnectionString;

                    //dataContext = SenparcDI.GetService<SenparcEntities>();
                    //TODO:当前采用注入可以保证HttpContext单例，如果要全局单例，可采用单件模式（需要先解决释放的问题）
                }
                //var hashCode = dataContext.GetHashCode();
                //System.Web.HttpContext.Current.Response.Write(dataContext.GetHashCode() + "<br />");//测试同一Request只有一个SenparcEntities实例
                return dataContext;
            }

        }
    }
}

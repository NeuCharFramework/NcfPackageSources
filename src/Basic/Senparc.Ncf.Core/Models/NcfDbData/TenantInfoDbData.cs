using Microsoft.EntityFrameworkCore;
using Senparc.Ncf.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    public interface ITenantInfoDbData : INcfDbData
    {
        SenparcEntitiesMultiTenant DataContext { get; }
    }

    public class TenantInfoDbData : NcfDbData, INcfDbData, ITenantInfoDbData
    {
        private SenparcEntitiesMultiTenant dataContext;

        public TenantInfoDbData(SenparcEntitiesMultiTenant senparcEntities)
        {
            dataContext = senparcEntities;
        }


        public SenparcEntitiesMultiTenant DataContext
        {
            get
            {
                return BaseDataContext as SenparcEntitiesMultiTenant;
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
                return dataContext;
            }

        }
    }
}

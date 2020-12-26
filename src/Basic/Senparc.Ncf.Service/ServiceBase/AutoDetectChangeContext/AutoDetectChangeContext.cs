using System;

namespace Senparc.Ncf.Service
{
    /// <summary>
    /// 临时开放BaseDataContext.Configuration.AutoDetectChangesEnabled属性，用于大批量更新数据的环境，结束后还原到false状态。
    /// </summary>
    public class AutoDetectChangeContextWrap : IDisposabled
    {
        public IServiceDataBase ServiceData { get; set; }
        //private INcfDbData _ncfDbData;


        public AutoDetectChangeContextWrap(IServiceDataBase serviceData)
        {
            //_service = service;
            //_service.BaseRepository.BaseDB.BaseDataContext.Configuration.AutoDetectChangesEnabled = true;
            //_ncfDbData =  ncfDbData;
            ServiceData = serviceData;
            ServiceData.BaseData.BaseDB.BaseDataContext.ChangeTracker.AutoDetectChangesEnabled= false;
            ServiceData.BaseData.BaseDB.ManualDetectChangeObject = true;
        }

        public void Dispose()
        {
            //_service.BaseRepository.BaseDB.BaseDataContext.Configuration.AutoDetectChangesEnabled = false;
            ServiceData.BaseData.BaseDB.BaseDataContext.ChangeTracker.AutoDetectChangesEnabled = true;
            ServiceData.BaseData.BaseDB.ManualDetectChangeObject = false;
        }
    }

    public class CloseAutoDetectChangeContext : IDisposable
    {
        AutoDetectChangeContextWrap _wrap;
        public CloseAutoDetectChangeContext(AutoDetectChangeContextWrap wrap)
        {
            _wrap = wrap;
            _wrap.ServiceData.BaseData.BaseDB.BaseDataContext.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        public void Dispose()
        {
            _wrap.ServiceData.BaseData.BaseDB.BaseDataContext.ChangeTracker.AutoDetectChangesEnabled = false;

        }
    }
}

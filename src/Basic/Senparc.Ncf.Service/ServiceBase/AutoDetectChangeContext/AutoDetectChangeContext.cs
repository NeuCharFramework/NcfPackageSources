using System;

namespace Senparc.Ncf.Service
{
    /// <summary>
    /// Temporarily open the BaseDataContext.Configuration.AutoDetectChangesEnabled property, which is used in environments where data is updated in large batches. It will be restored to the false state after completion.
    /// </summary>
    public class AutoDetectChangeContextWrap : IDisposable
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

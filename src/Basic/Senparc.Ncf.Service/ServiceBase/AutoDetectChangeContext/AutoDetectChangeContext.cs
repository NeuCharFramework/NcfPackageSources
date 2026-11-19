/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AutoDetectChangeContext.cs
    File description: AutoDetectChangeContext related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Service
{
    /// <summary>
    ///Legacy comment translated to English.
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

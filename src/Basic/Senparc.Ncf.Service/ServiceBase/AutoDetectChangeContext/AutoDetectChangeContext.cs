/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AutoDetectChangeContext.cs
    File description: AutoDetectChangeContext related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.23.13.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Service
{
    /// <summary>
    /// Disposable scope that switches an EF Core context from automatic change detection
    /// to NCF's explicit/manual entity change tracking mode.
    /// </summary>
    public class AutoDetectChangeContextWrap : IDisposable
    {
        public IServiceDataBase ServiceData { get; set; }
        //private INcfDbData _ncfDbData;


        /// <summary>
        /// Disables automatic change detection and enables NCF manual change tracking.
        /// </summary>
        /// <param name="serviceData">The service database boundary whose context is reconfigured.</param>
        public AutoDetectChangeContextWrap(IServiceDataBase serviceData)
        {
            //_service = service;
            //_service.BaseRepository.BaseDB.BaseDataContext.Configuration.AutoDetectChangesEnabled = true;
            //_ncfDbData =  ncfDbData;
            ServiceData = serviceData;
            ServiceData.BaseData.BaseDB.BaseDataContext.ChangeTracker.AutoDetectChangesEnabled= false;
            ServiceData.BaseData.BaseDB.ManualDetectChangeObject = true;
        }

        /// <summary>
        /// Restores automatic change detection and disables manual change tracking.
        /// </summary>
        public void Dispose()
        {
            //_service.BaseRepository.BaseDB.BaseDataContext.Configuration.AutoDetectChangesEnabled = false;
            ServiceData.BaseData.BaseDB.BaseDataContext.ChangeTracker.AutoDetectChangesEnabled = true;
            ServiceData.BaseData.BaseDB.ManualDetectChangeObject = false;
        }
    }

    /// <summary>
    /// Disposable nested scope that temporarily re-enables automatic change detection
    /// while an outer manual-detection wrapper remains active.
    /// </summary>
    public class CloseAutoDetectChangeContext : IDisposable
    {
        AutoDetectChangeContextWrap _wrap;
        /// <summary>
        /// Temporarily re-enables automatic change detection within an existing wrapper.
        /// </summary>
        /// <param name="wrap">The wrapper whose database context is being temporarily adjusted.</param>
        public CloseAutoDetectChangeContext(AutoDetectChangeContextWrap wrap)
        {
            _wrap = wrap;
            _wrap.ServiceData.BaseData.BaseDB.BaseDataContext.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        /// <summary>
        /// Restores the wrapper's manual change-detection mode.
        /// </summary>
        public void Dispose()
        {
            _wrap.ServiceData.BaseData.BaseDB.BaseDataContext.ChangeTracker.AutoDetectChangesEnabled = false;
        }
    }
}

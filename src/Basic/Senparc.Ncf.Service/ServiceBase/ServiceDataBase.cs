/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: ServiceDataBase.cs

    File description: ServiceDataBase related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Senparc.Ncf.Repository;

namespace Senparc.Ncf.Service
{
    public interface IServiceDataBase
    {
        IServiceProvider ServiceProvider { get; set; }
        IDataBase BaseData { get; set; }
        void CloseConnection();

        T GetService<T>();
    }


    public class ServiceDataBase : IServiceDataBase
    {
        #region Legacy section

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static Action<IDataBase, object>? AfterSaveObject { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static Action<IDataBase, object>? AfterDeleteObject { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static Action<IDataBase>? AfterSaveChanges { get; set; }

        #endregion


        public IServiceProvider ServiceProvider { get; set; }
        public IDataBase BaseData { get; set; }

        public ServiceDataBase(IDataBase baseData,IServiceProvider serviceProvider=null)
        {
            BaseData = baseData;
            ServiceProvider = serviceProvider;
        }

        public virtual void CloseConnection()
        {
            BaseData.CloseConnection();
        }

        public T GetService<T>()
        {
            return ServiceProvider.GetService<T>();
        }

        public T GetRequiredService<T>()
        {
            return ServiceProvider.GetRequiredService<T>();
        }

    }
}
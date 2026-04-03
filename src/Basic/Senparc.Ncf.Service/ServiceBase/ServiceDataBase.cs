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
        #region 保存或删除后的操作，将影响全局

        /// <summary>
        /// Executed after the specified object is saved (whether successful or not), it will affect all save processes globally.
        /// </summary>
        public static Action<IDataBase, object>? AfterSaveObject { get; set; }

        /// <summary>
        /// Executed after the specified object is deleted (whether successful or not), it will affect all global save processes
        /// </summary>
        public static Action<IDataBase, object>? AfterDeleteObject { get; set; }

        /// <summary>
        /// Executed after all objects are saved (whether successful or not), it will affect all save processes globally
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
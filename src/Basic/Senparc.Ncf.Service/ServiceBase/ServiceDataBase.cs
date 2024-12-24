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
        /// 在指定对象保存后执行（无论是否成功），将影响全局所有保存过程
        /// </summary>
        public static Action<IDataBase, object>? AfterSaveObject { get; set; }

        /// <summary>
        /// 在指定对象删除后执行（无论是否成功），将影响全局所有保存过程
        /// </summary>
        public static Action<IDataBase, object>? AfterDeleteObject { get; set; }

        /// <summary>
        /// 在所有对象保存后执行（无论是否成功），将影响全局所有保存过程
        /// </summary>
        public static Action<IDataBase>? AfterSaveChanges { get; set; }

        #endregion


        public IServiceProvider ServiceProvider { get; set; }
        public IDataBase BaseData { get; set; }

        public ServiceDataBase(IDataBase baseData)
        {
            BaseData = baseData;
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
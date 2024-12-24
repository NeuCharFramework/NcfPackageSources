using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Ncf.Service
{
    public interface IClientServiceBase<T> : IServiceBase<T> where T : EntityBase//global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        IClientRepositoryBase<T> BaseClientRepository { get; }

        ///// <summary>
        ///// 开启事务
        ///// </summary>
        ///// <returns></returns>
        //IDbContextTransaction BeginTransaction();
    }


    public class ClientServiceBase<T> : ServiceBase<T>, IClientServiceBase<T> where T : EntityBase//global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        public IClientRepositoryBase<T> BaseClientRepository
        {
            get
            {
                return RepositoryBase as IClientRepositoryBase<T>;
            }
        }

        public ClientServiceBase(IClientRepositoryBase<T> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {

        }
    }
}
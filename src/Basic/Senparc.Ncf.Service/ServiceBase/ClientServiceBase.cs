/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ClientServiceBase.cs
    文件功能描述：ClientServiceBase 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

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
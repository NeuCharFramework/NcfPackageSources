/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ClientServiceBase.cs
    File description: ClientServiceBase related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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
        ///// enable transactions
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
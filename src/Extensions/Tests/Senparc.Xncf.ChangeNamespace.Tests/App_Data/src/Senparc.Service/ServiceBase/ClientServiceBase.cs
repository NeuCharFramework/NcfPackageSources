using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using System;

namespace Senparc.Service
{
    public interface IClientServiceBase<T> : IServiceBase<T> where T : EntityBase//global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        /// <summary>Gets the client-specific repository used by this service.</summary>
        IClientRepositoryBase<T> BaseClientRepository { get; }
    }


    /// <summary>Provides service operations backed by a client repository.</summary>
    public class ClientServiceBase<T> : ServiceBase<T>, IClientServiceBase<T> where T : EntityBase//global::System.Data.Objects.DataClasses.EntityObject, new()
    {
        /// <summary>Gets the underlying client repository when available.</summary>
        public IClientRepositoryBase<T> BaseClientRepository
        {
            get
            {
                return RepositoryBase as IClientRepositoryBase<T>;
            }
        }

        /// <summary>Initializes the client service with its repository and service provider.</summary>
        /// <param name="repo">The client repository.</param>
        /// <param name="serviceProvider">The dependency-injection service provider.</param>
        public ClientServiceBase(IClientRepositoryBase<T> repo, IServiceProvider serviceProvider)
            : base(repo, serviceProvider)
        {

        }
    }
}

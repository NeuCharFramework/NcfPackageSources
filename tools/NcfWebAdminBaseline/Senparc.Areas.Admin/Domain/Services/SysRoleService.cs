using Senparc.Ncf.Core.Models.DataBaseModel;
using Senparc.Ncf.Repository;
using System;
using System.Text;

namespace Senparc.Areas.Admin.Domain.Services
{
    public class SysRoleService : BaseClientService<SysRole>
    {
        /// <summary>
        /// Initializes the role service with its repository and request service provider.
        /// </summary>
        /// <param name="repo">The repository used for role persistence.</param>
        /// <param name="serviceProvider">The request service provider.</param>
        public SysRoleService(ClientRepositoryBase<SysRole> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
    }
}

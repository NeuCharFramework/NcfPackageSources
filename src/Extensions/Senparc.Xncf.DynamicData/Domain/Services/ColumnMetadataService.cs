using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;

namespace Senparc.Xncf.DynamicData.Domain.Services
{
    public class ColumnMetadataService : ServiceBase<ColumnMetadata>
    {
        public ColumnMetadataService(IRepositoryBase<ColumnMetadata> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
    }
}

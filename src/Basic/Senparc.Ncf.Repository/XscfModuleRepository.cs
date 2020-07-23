using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Repository
{
    public class XscfModuleRepository : ClientRepositoryBase<XscfModule>
    {
        public XscfModuleRepository(ISqlBaseFinanceData db) : base(db)
        {
        }
    }
}

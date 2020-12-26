using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Core.Models.DataBaseModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Repository
{
    public class XncfModuleRepository : ClientRepositoryBase<XncfModule>
    {
        public XncfModuleRepository(INcfDbData db) : base(db)
        {
        }
    }
}

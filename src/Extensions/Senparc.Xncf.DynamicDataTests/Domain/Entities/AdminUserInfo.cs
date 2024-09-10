using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Ncf.Core.Models;

namespace Senparc.Xncf.DynamicDataTests.Domain.Entities
{
    public class AdminUserInfo : EntityBase<int>
    {
        public Guid Guid { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public DateTime LastLoginTime { get; set; }
    }
}

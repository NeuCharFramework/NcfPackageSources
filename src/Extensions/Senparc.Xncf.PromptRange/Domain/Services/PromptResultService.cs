using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.PromptRange.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptResultService : ServiceBase<PromptResult>
    {
        public PromptResultService(IRepositoryBase<PromptResult> repo, IServiceProvider serviceProvider) : base(repo, serviceProvider)
        {
        }
    }
}

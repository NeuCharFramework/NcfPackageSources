
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Xncf.AIKernel.Models;

namespace Senparc.Xncf.AIKernel.Domain.Services
{
	public class AIModelService : ServiceBase<AIModel>
	{
		public AIModelService(IRepositoryBase<AIModel> repo, IServiceProvider serviceProvider)
		  : base(repo, serviceProvider)
		{
		}

	}
}


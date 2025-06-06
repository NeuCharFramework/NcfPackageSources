using Senparc.Ncf.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Exceptions
{
    public class AgentsManagerException : NcfExceptionBase
    {
        public AgentsManagerException(string message, bool logged = false) : base(message, logged)
        {
        }

        public AgentsManagerException(string message, Exception inner, bool logged = false) : base(message, inner, logged)
        {
        }
    }
}

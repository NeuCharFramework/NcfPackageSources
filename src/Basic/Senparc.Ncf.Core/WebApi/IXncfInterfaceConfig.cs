using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.Core.WebApi
{
    public interface IXncfInterfaceConfig
    {
        static string XncfName { get; }
        static string XncfAspireProjectName { get; }
    }
}

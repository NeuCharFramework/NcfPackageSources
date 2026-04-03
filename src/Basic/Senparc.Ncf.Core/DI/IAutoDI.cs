using Senparc.Ncf.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.DI
{
    /// <summary>
    /// Base interface for all types that require auto-scanned dependency injection
    /// <para>By default, DI uses AddScoped. To force a different lifecycle, add the [AutoDIType(typeName)] attribute on the implementation class.</para>
    /// </summary>
    public interface IAutoDI
    {
    }
}

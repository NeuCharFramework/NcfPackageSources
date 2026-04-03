using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Xncf module execution order. The larger the Order number, the earlier the execution. If it is not a system-critical module, try to go as far back as possible.
    /// </summary>
    public class XncfOrderAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="order">Loading order, the larger the number, the higher the loading order. Please strictly follow the reference values: 0: normal (default), 1-5000: important modules that need to be preloaded, >5000: system and basic modules</param>
        public XncfOrderAttribute(int order)
        {
            Order = order;
        }

        /// <summary>
        /// Loading order, the larger the number, the higher the loading order. Please strictly follow the reference values: 0: normal (default), 1-5000: important modules that need to be preloaded, >5000: system and basic modules
        /// </summary>
        public int Order { get; set; }
    }
}

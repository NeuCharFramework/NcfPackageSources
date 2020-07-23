using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.XncfBase
{
    /// <summary>
    /// Xscf 模块执行顺序，Order 数字越大，执行越靠前，如果非系统关键模块，尽量靠后
    /// </summary>
    public class XscfOrderAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="order">加载顺序，数字越大加载顺序越靠前。请严格按照参考数值：0：普通（默认），1-5000：需要预加载的重要模块，>5000：系统及基础模块</param>
        public XscfOrderAttribute(int order)
        {
            Order = order;
        }

        /// <summary>
        /// 加载顺序，数字越大加载顺序越靠前。请严格按照参考数值：0：普通（默认），1-5000：需要预加载的重要模块，>5000：系统及基础模块
        /// </summary>
        public int Order { get; set; }
    }
}

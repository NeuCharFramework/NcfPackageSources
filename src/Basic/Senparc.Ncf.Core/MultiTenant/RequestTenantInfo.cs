using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    /// 某一个请求对应的租户信息
    /// </summary>
    public class RequestTenantInfo
    {
        public int Id { get; set; }
        /// <summary>
        /// 唯一名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 匹配条件
        /// </summary>
        public string TenantKey { get; set; }
        /// <summary>
        /// 初始化开始时间
        /// </summary>
        public DateTime BeginTime { get; }
        /// <summary>
        /// 是否匹配成功
        /// </summary>
        public bool MatchSuccess { get; private set; }

        /// <summary>
        /// 是否已经尝试过匹配
        /// </summary>
        public bool TriedMatching { get; private set; }

        public RequestTenantInfo()
        {
            BeginTime = SystemTime.Now.DateTime;
        }

       /// <summary>
       /// 尝试匹配
       /// </summary>
       /// <param name="success">是否成功</param>
        public void TryMatch(bool success)
        {
            TriedMatching = true;
            MatchSuccess = success;
        }
    }
}

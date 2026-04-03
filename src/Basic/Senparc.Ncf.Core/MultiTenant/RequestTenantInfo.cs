using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    /// Tenant information for a specific request
    /// </summary>
    public class RequestTenantInfo
    {
        public int Id { get; set; }
        /// <summary>
        /// Unique name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// match condition
        /// </summary>
        public string TenantKey { get; set; }
        /// <summary>
        ///Initialization start time
        /// </summary>
        public DateTime BeginTime { get; }
        /// <summary>
        /// Whether the match is successful
        /// </summary>
        public bool MatchSuccess { get; private set; }

        /// <summary>
        /// Whether matching has already been attempted
        /// </summary>
        public bool TriedMatching { get; private set; }

        public RequestTenantInfo()
        {
            BeginTime = SystemTime.Now.DateTime;
        }

       /// <summary>
       /// try to match
       /// </summary>
       /// <param name="success">Whether it was successful</param>
        public void TryMatch(bool success)
        {
            TriedMatching = true;
            MatchSuccess = success;
        }
    }
}

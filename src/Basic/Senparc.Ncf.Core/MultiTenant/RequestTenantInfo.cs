/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RequestTenantInfo.cs
    File description: RequestTenantInfo related implementation

    Created by: Senparc - 20210212

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    /// Stores the tenant selected for the current request and the result of tenant matching.
    /// </summary>
    public class RequestTenantInfo
    {
        /// <summary>Gets or sets the resolved tenant identifier.</summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the resolved tenant name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the tenant lookup key.
        /// </summary>
        public string TenantKey { get; set; }
        /// <summary>
        /// Gets the time at which this request tenant context was created.
        /// </summary>
        public DateTime BeginTime { get; }
        /// <summary>
        /// Gets whether tenant matching succeeded.
        /// </summary>
        public bool MatchSuccess { get; private set; }

        /// <summary>
        /// Gets whether tenant matching has been attempted.
        /// </summary>
        public bool TriedMatching { get; private set; }

        /// <summary>
        /// Initializes tenant context with the current system time as its begin time.
        /// </summary>
        public RequestTenantInfo()
        {
            BeginTime = SystemTime.Now.DateTime;
        }

       /// <summary>
       /// Records whether the current request tenant was successfully matched.
       /// </summary>
       /// <param name="success">Whether tenant matching succeeded.</param>
        public void TryMatch(bool success)
        {
            TriedMatching = true;
            MatchSuccess = success;
        }
    }
}

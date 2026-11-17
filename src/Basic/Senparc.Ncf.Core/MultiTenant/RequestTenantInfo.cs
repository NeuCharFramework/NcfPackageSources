/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: RequestTenantInfo.cs

    File description: RequestTenantInfo related implementation

    

    

    Created by: Senparc - 20210212

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.MultiTenant
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class RequestTenantInfo
    {
        public int Id { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string TenantKey { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime BeginTime { get; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool MatchSuccess { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public bool TriedMatching { get; private set; }

        public RequestTenantInfo()
        {
            BeginTime = SystemTime.Now.DateTime;
        }

       /// <summary>
       ///Legacy comment translated to English.
       /// </summary>
       /// <param name="success">whethersuccess</param>
        public void TryMatch(bool success)
        {
            TriedMatching = true;
            MatchSuccess = success;
        }
    }
}

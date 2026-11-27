/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SiteConfig.Core.cs
    File description: SiteConfig.Core related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260729
    Change description: v0.26.0-preview3 Updated module behavior and related support.

    Modified by: Senparc - 20261103
    Change description: v0.30.3-preview9 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;
using Senparc.Ncf.Core.Utility;
using System.IO;

namespace Senparc.Ncf.Core.Config
{
    public static partial class SiteConfig
    {
        /// <summary>
        /// Gets or sets the application content-root path.
        /// </summary>
        public static string ApplicationPath { get; set; }
        /// <summary>
        /// Gets or sets the application web-root path.
        /// </summary>
        public static string WebRootPath { get; set; }

        /// <summary>
        /// Gets or sets the central NCF runtime settings object.
        /// </summary>
        public static SenparcCoreSetting SenparcCoreSetting { get; set; } = new SenparcCoreSetting();
        //{
        //    get
        //    {
        //        IServiceProvider serviceProvider = SenparcDI.GlobalServiceCollection.BuildServiceProvider();
        //        var scs = serviceProvider.GetService<IOptions<SenparcCoreSetting>>();
        //        return scs.Value;
        //    }
        //}

        /// <summary>
        /// Gets whether NCF is running in debug mode.
        /// </summary>
        public static bool IsDebug => SenparcCoreSetting.IsDebug;

        /// <summary>
        /// Gets whether the current host is configured as a test site.
        /// </summary>
        public static bool IsTestSite => SenparcCoreSetting.IsTestSite;

        public static Dictionary<string, int> _memcachedAddressesDic;
        /// <summary>Gets the marker used to bypass Weixin message filtering.</summary>
        public const string WEIXIN_FILTER_IGNORE = "senparcnofilter1";
        /// <summary>Gets the configured key used for the official Weixin avatar resource.</summary>
        public const string WEIXIN_OFFICIAL_AVATAR_KEY = "WXoDOkC8A";
        /// <summary>Gets the configured key used for the official Weixin QR-code resource.</summary>
        public const string WEIXIN_OFFICIAL_QR_CODE_KEY = "WX631IC8A";
        /// <summary>Gets the configuration key used for the NCF Weixin application token.</summary>
        public const string WEIXIN_APP_TOKEN_KEY = "WEIXIN_APP_TOKEN_KEY_FOR_NCF";
        /// <summary>Gets the lower bound reserved for Weixin user identifiers.</summary>
        public const long MIN_WEIXINUSERINFO_ID = 10000000000000;
        /// <summary>Gets the legacy project demand deposit amount.</summary>
        public const decimal PROJECTDMANDDEPOSIT = 1000;
        /// <summary>Gets the default drive path for the Weixin Pay PKCS#12 certificate.</summary>
        public const string CERT_P12_ADDRESS = @"E:\";

        /* Suggested UID format: NCF company ID - project type - reserved field (randomized if needed) - internal category 1 - internal category 2*/
        public const string SYSTEM_XNCF_TANENT_UID = "00000000-0000-0000-0000-000000000001";
        public const string SYSTEM_XNCF_MODULE_SYSTEM_CORE_UID = "00000000-0000-0000-0001-000000000001";
        public const string SYSTEM_XNCF_MODULE_SERVICE_MANAGER_UID = "00000000-0000-0000-0001-000000000002";
        public const string SYSTEM_XNCF_MODULE_SYSTEM_PERMISSION_UID = "00000000-0000-0000-0001-000000000003";
        public const string SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID = "00000000-0000-0000-0001-000000000004";
        public const string SYSTEM_XNCF_MODULE_MENU_UID = "00000000-0000-0000-0001-000000000005";
        public const string SYSTEM_XNCF_BASE_AREAS = "00000000-0000-0000-0001-000000000006";
        public const string SYSTEM_XNCF_MODULE_INSTALLER_UID = "00000000-0000-0000-0001-000000000007";


        public const string SYSTEM_XNCF_MODULE_AREAS_ADMIN_UID = "00000000-0000-0001-0001-000000000001";
        public const string SYSTEM_XNCF_MODULE_ACCOUNTS_UID = "00000000-0000-0001-0001-000000000002";

        public const string TENANT_DEFAULT_NAME = "DEFAULT";//多租户 TenantName 默认值（不作为任何一个特殊租户）

        /// <summary>
        /// Developer revenue share
        /// </summary>
        public static readonly long DeveloperIncomRate = (long)0.5;

        /// <summary>
        /// Cache type
        /// </summary>
        public static CacheType CacheType
        {
            get => SenparcCoreSetting.CacheType;
            set => SenparcCoreSetting.CacheType = value;
        }

        //Move the following parameters to SiteConfig.cs
        //public readonly static string VERSION = "1.3.2";
        //public const string GLOBAL_PASSWORD_SALT = "senparc@20131113";

        public const string VERSION = "0.0.1";
        public static string SenparcConfigDirctory = "~/App_Data/Database/";
        public const string AntiForgeryTokenSalt = "SOUIDEA__SENPARC";
        public const string WEIXIN_USER_AVATAR_KEY = "SENPARC_"; //Use the first 8 characters
        public const string DomainName = "https://ncf.senparc.com";
        public const string DefaultTemplate = "default";
        /// <summary>Gets the default wait interval between SMS sends.</summary>
        public const int SMSSENDWAITSECONDS = 60;
        /// <summary>Gets the fallback avatar path used when no user avatar exists.</summary>
        public const string DEFAULT_AVATAR = "/Content/Images/userinfonopic.png";

        public const string DEFAULT_MEMCACHED_ADDRESS_1 = "192.168.184.91";
        public const int DEFAULT_MEMCACHED_PORT_1 = 11210;

        /// <summary>
        /// Gets the zero-padded work-break suffix format.
        /// </summary>
        public static readonly string WBSFormat = "000";

        /// <summary>
        /// Gets the inactivity threshold used for online-user tracking.
        /// </summary>
        public static readonly int UserOnlineTimeoutMinutes = 10;
        /// <summary>
        /// Gets the maximum number of login attempts allowed by legacy login policy.
        /// </summary>
        public static readonly int TryLoginTimes = 1;
        /// <summary>
        /// Gets the maximum number of user-login attempts allowed by legacy login policy.
        /// </summary>
        public static readonly int TryUserLoginTimes = 3;
        /// <summary>
        /// Gets the maximum number of retained database backups.
        /// </summary>
        public static readonly int MaxBackupDatabaseCount = 200;
        /// <summary>
        /// Gets whether the process is running under unit-test configuration.
        /// </summary>
        public static readonly bool IsUnitTest = false;
        private static bool _isInstalling = false;

        /// <summary>
        /// Gets or sets whether NCF is currently in installation mode.
        /// </summary>
        public static bool IsInstalling
        {
            get
            {
                return _isInstalling || !CheckInstallFinishedFileExisted();
            }
            set
            {
                _isInstalling = value;
            }
        }

        /// <summary>
        /// Determines whether the installation completion marker exists under <c>App_Data</c>.
        /// </summary>
        /// <returns><see langword="true"/> when the marker file exists; otherwise, <see langword="false"/>.</returns>
        public static bool CheckInstallFinishedFileExisted()
        {
            var fileExist = File.Exists(Server.GetMapPath("~/App_Data/install-finished.txt"));
            return fileExist;
        }

        /// <summary>
        /// Completes installation through the synchronous compatibility wrapper.
        /// </summary>
        [Obsolete($"Use {nameof(SetInstallFinishedAsync)} so persistence failures can be observed.")]
        public static void SetInstallFinished()
        {
            SetInstallFinishedAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Clears the installation state and writes the installation completion marker
        /// when it does not already exist.
        /// </summary>
        /// <returns>A task that completes after the marker has been written or confirmed.</returns>
        public static async Task SetInstallFinishedAsync()
        {
            _isInstalling = false;
            var filePath = Server.GetMapPath("~/App_Data/install-finished.txt");
            if (!CheckInstallFinishedFileExisted())
            {
                var text = @$"After this file is successfully installed by the system, it should only be modified or removed when you need to reinstall the entire system! Please operate with caution!!

此文件由系统成功安装后，仅在您需要全部重新安装系统时将其修改或移除！请谨慎操作！！";
                await File.WriteAllTextAsync(filePath, text);
            }
        }

        /// <summary>
        /// Gets or sets the current page-view counter.
        /// </summary>
        public static int PageViewCount { get; set; }


        /// <summary>
        /// Gets or sets whether the database-backed XNCF modules have been loaded.
        /// </summary>
        public static bool DatabaseXncfLoaded { get; set; }

        /// <summary>
        /// Gets the singleton core state object.
        /// </summary>
        public static NcfCoreState NcfCoreState { get; } = NcfCoreState.Instance;

        //asyncthread
        public static Dictionary<string, Thread> AsynThread = new Dictionary<string, Thread>(); //Legacy comment translated to English.

        /// <summary>
        /// Gets the authorization scheme name used for administrator requests.
        /// </summary>
        public readonly static string NcfAdminAuthorizeScheme = "NcfAdminAuthorizeScheme";
        /// <summary>
        /// Gets the authorization scheme name used for user requests.
        /// </summary>
        public readonly static string NcfUserAuthorizeScheme = "NcfUserAuthorizeScheme";
    }
}

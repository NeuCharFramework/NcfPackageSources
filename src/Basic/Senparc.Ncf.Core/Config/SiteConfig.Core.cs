using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.Ncf.Core.Enums;
using Senparc.Ncf.Core.Models;
using Senparc.Ncf.Utility;
using System.Collections.Generic;
using System.Threading;
using System;
using Senparc.Ncf.Core.Utility;
using System.IO;

namespace Senparc.Ncf.Core.Config
{
    public static partial class SiteConfig
    {
        /// <summary>
        /// Website physical path
        /// </summary>
        public static string ApplicationPath { get; set; }
        public static string WebRootPath { get; set; }

        /// <summary>
        /// Settings
        /// <para>Must be injected when system starts</para>
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
        /// Whether in Debug mode (manually defined)
        /// </summary>
        public static bool IsDebug => SenparcCoreSetting.IsDebug;

        /// <summary>
        /// Whether this is a test site
        /// </summary>
        public static bool IsTestSite => SenparcCoreSetting.IsTestSite;

        public static Dictionary<string, int> _memcachedAddressesDic;
        public const string WEIXIN_FILTER_IGNORE = "senparcnofilter1";
        public const string WEIXIN_OFFICIAL_AVATAR_KEY = "WXoDOkC8A"; //Take first 8 chars
        public const string WEIXIN_OFFICIAL_QR_CODE_KEY = "WX631IC8A"; //Take first 8 chars
        public const string WEIXIN_APP_TOKEN_KEY = "WEIXIN_APP_TOKEN_KEY_FOR_NCF"; //WeChat APP Token encryption
        public const long MIN_WEIXINUSERINFO_ID = 10000000000000; //Minimum custom WeixinUserInfo Id
        public const decimal PROJECTDMANDDEPOSIT = 1000; //Default project deposit
        public const string CERT_P12_ADDRESS = @"E:\";//Storage path for WeChat Pay certificate

        /* UID Recommended format: NCF company ID-project type-reserved field (can be random)-internal category 1-internal category 2*/
        public const string SYSTEM_XNCF_TANENT_UID = "00000000-0000-0000-0000-000000000001";
        public const string SYSTEM_XNCF_MODULE_SYSTEM_CORE_UID = "00000000-0000-0000-0001-000000000001";
        public const string SYSTEM_XNCF_MODULE_SERVICE_MANAGER_UID = "00000000-0000-0000-0001-000000000002";
        public const string SYSTEM_XNCF_MODULE_SYSTEM_PERMISSION_UID = "00000000-0000-0000-0001-000000000003";
        public const string SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID = "00000000-0000-0000-0001-000000000004";
        public const string SYSTEM_XNCF_MODULE_MENU_UID = "00000000-0000-0000-0001-000000000005";
        public const string SYSTEM_XNCF_BASE_AREAS = "00000000-0000-0000-0001-000000000006";


        public const string SYSTEM_XNCF_MODULE_AREAS_ADMIN_UID = "00000000-0000-0001-0001-000000000001";
        public const string SYSTEM_XNCF_MODULE_ACCOUNTS_UID = "00000000-0000-0001-0001-000000000002";

        public const string TENANT_DEFAULT_NAME = "DEFAULT";//Default multi-tenant TenantName (not treated as any special tenant)

        /// <summary>
        /// Developer income ratio
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

        //Put the following parameters into SiteConfig.csmiddle//public readonly static string VERSION = "1.3.2";
        //public const string GLOBAL_PASSWORD_SALT = "senparc@20131113";

        public const string VERSION = "0.0.1";
        public static string SenparcConfigDirctory = "~/App_Data/Database/";
        public const string AntiForgeryTokenSalt = "SOUIDEA__SENPARC";
        public const string WEIXIN_USER_AVATAR_KEY = "SENPARC_"; //Take first 8 chars
        public const string DomainName = "https://ncf.senparc.com";
        public const string DefaultTemplate = "default";
        public const int SMSSENDWAITSECONDS = 60; //Phone verification duration
        public const string DEFAULT_AVATAR = "/Content/Images/userinfonopic.png"; //Default avatar

        public const string DEFAULT_MEMCACHED_ADDRESS_1 = "192.168.184.91";
        public const int DEFAULT_MEMCACHED_PORT_1 = 11210;

        /// <summary>
        /// WBS format
        /// </summary>
        public static readonly string WBSFormat = "000";

        /// <summary>
        /// User online inactivity timeout (minutes)
        /// </summary>
        public static readonly int UserOnlineTimeoutMinutes = 10;
        /// <summary>
        /// Max login attempts without verification code
        /// </summary>
        public static readonly int TryLoginTimes = 1;
        /// <summary>
        /// Max user login attempts without verification code
        /// </summary>
        public static readonly int TryUserLoginTimes = 3;
        /// <summary>
        /// Maximum number of database backup files
        /// </summary>
        public static readonly int MaxBackupDatabaseCount = 200;
        /// <summary>
        /// Whether running unit test
        /// </summary>
        public static readonly bool IsUnitTest = false;
        ///// <summary>
        ///// AI plug-in registration
        ///// </summary>
        //public static readonly AIPlugins AIPlugins = new AIPlugins();

        private static bool _isInstalling = false;

        /// <summary>
        /// Whether installation is in progress; if true, installation-check exception is not thrown
        /// </summary>
        public static bool IsInstalling
        {
            get
            {
                return _isInstalling || !CheckInstallFinishedFileExisted();
            }
            set
            {
                _isInstalling = false;
            }
        }

        /// <summary>
        /// Check whether installation-finished status file exists
        /// </summary>
        /// <returns></returns>
        public static bool CheckInstallFinishedFileExisted()
        {
            var fileExist = File.Exists(Server.GetMapPath("~/App_Data/install-finished.txt"));
            return fileExist;
        }

        /// <summary>
        /// Manually set installation status to finished
        /// </summary>
        public static async void SetInstallFinished()
        {
            _isInstalling = false;
            var filePath = Server.GetMapPath("~/App_Data/install-finished.txt");
            if (!CheckInstallFinishedFileExisted())
            {
                var text = @$"After this file is successfully installed by the system, it should only be modified or removed when you need to reinstall the entire system! Please operate with caution!!

This file is created after successful system installation. Modify or remove it only when you need to reinstall the whole system. Please operate with caution!!";
                await File.WriteAllTextAsync(filePath, text);
            }
        }

        public static int PageViewCount { get; set; } //Frontend page views after site startup


        /// <summary>
        /// Whether database module should be loaded
        /// </summary>
        public static bool DatabaseXncfLoaded { get; set; }

        /// <summary>
        /// System state
        /// </summary>
        public static NcfCoreState NcfCoreState { get; } = NcfCoreState.Instance;

        //Async threads
        public static Dictionary<string, Thread> AsynThread = new Dictionary<string, Thread>(); //Background running threads

        /// <summary>
        /// Cookie login scheme for Admin
        /// </summary>
        public readonly static string NcfAdminAuthorizeScheme = "NcfAdminAuthorizeScheme";
        /// <summary>
        /// Cookie login scheme for User
        /// </summary>
        public readonly static string NcfUserAuthorizeScheme = "NcfUserAuthorizeScheme";
    }
}
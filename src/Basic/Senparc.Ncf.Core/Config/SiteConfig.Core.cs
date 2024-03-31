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

namespace Senparc.Ncf.Core.Config
{
    public static partial class SiteConfig
    {
        /// <summary>
        /// 网站物理路径
        /// </summary>
        public static string ApplicationPath { get; set; }
        public static string WebRootPath { get; set; }

        /// <summary>
        /// 设置
        /// <para>需要在系统启动时注入</para>
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
        /// 是否处于Debug状态（人为手动定义）
        /// </summary>
        public static bool IsDebug => SenparcCoreSetting.IsDebug;

        /// <summary>
        /// 是否是测试站
        /// </summary>
        public static bool IsTestSite => SenparcCoreSetting.IsTestSite;

        public static Dictionary<string, int> _memcachedAddressesDic;
        public const string WEIXIN_FILTER_IGNORE = "senparcnofilter1";
        public const string WEIXIN_OFFICIAL_AVATAR_KEY = "WXoDOkC8A"; //将取前8位
        public const string WEIXIN_OFFICIAL_QR_CODE_KEY = "WX631IC8A"; //将取前8位
        public const string WEIXIN_APP_TOKEN_KEY = "WEIXIN_APP_TOKEN_KEY_FOR_NCF"; //微信APP Token加密
        public const long MIN_WEIXINUSERINFO_ID = 10000000000000; //最小自定义WeixinUserInfo的Id
        public const decimal PROJECTDMANDDEPOSIT = 1000; //任务默认押金
        public const string CERT_P12_ADDRESS = @"E:\";//微信支付数字证书存放地址

        /* UID 建议格式： NCF公司ID-项目类型-保留字段(可随机)-内部类别1-内部类别2*/
        public const string SYSTEM_XNCF_TANENT_UID = "00000000-0000-0000-0000-000000000001";
        public const string SYSTEM_XNCF_MODULE_SYSTEM_CORE_UID = "00000000-0000-0000-0001-000000000001";
        public const string SYSTEM_XNCF_MODULE_SERVICE_MANAGER_UID = "00000000-0000-0000-0001-000000000002";
        public const string SYSTEM_XNCF_MODULE_SYSTEM_PERMISSION_UID = "00000000-0000-0000-0001-000000000003";
        public const string SYSTEM_XNCF_MODULE_XNCF_MODULE_MANAGER_UID = "00000000-0000-0000-0001-000000000004";
        public const string SYSTEM_XNCF_MODULE_MENU_UID = "00000000-0000-0000-0001-000000000005";
        public const string SYSTEM_XNCF_BASE_AREAS = "00000000-0000-0000-0001-000000000006";


        public const string SYSTEM_XNCF_MODULE_AREAS_ADMIN_UID = "00000000-0000-0001-0001-000000000001";
        public const string SYSTEM_XNCF_MODULE_ACCOUNTS_UID = "00000000-0000-0001-0001-000000000002";

        public const string TENANT_DEFAULT_NAME = "DEFAULT";//多租户 TenantName 默认值（不作为任何一个特殊租户）

        /// <summary>
        /// 开发者收入比例
        /// </summary>
        public static readonly long DeveloperIncomRate = (long)0.5;

        /// <summary>
        /// 缓存类型
        /// </summary>
        public static CacheType CacheType
        {
            get => SenparcCoreSetting.CacheType;
            set => SenparcCoreSetting.CacheType = value;
        }

        //以下参数放到SiteConfig.cs中
        //public readonly static string VERSION = "1.3.2";
        //public const string GLOBAL_PASSWORD_SALT = "senparc@20131113";

        public const string VERSION = "0.0.1";
        public static string SenparcConfigDirctory = "~/App_Data/Database/";
        public const string AntiForgeryTokenSalt = "SOUIDEA__SENPARC";
        public const string WEIXIN_USER_AVATAR_KEY = "SENPARC_"; //将取前8位
        public const string DomainName = "https://ncf.senparc.com";
        public const string DefaultTemplate = "default";
        public const int SMSSENDWAITSECONDS = 60; //手机验证持续时间
        public const string DEFAULT_AVATAR = "/Content/Images/userinfonopic.png"; //默认头像

        public const string DEFAULT_MEMCACHED_ADDRESS_1 = "192.168.184.91";
        public const int DEFAULT_MEMCACHED_PORT_1 = 11210;

        /// <summary>
        /// WBS格式
        /// </summary>
        public static readonly string WBSFormat = "000";

        /// <summary>
        /// 用户在线不活动过期时间(分钟)
        /// </summary>
        public static readonly int UserOnlineTimeoutMinutes = 10;
        /// <summary>
        /// 最多免验证码尝试登录次数
        /// </summary>
        public static readonly int TryLoginTimes = 1;
        /// <summary>
        /// 最多免验证码尝试登录次数
        /// </summary>
        public static readonly int TryUserLoginTimes = 3;
        /// <summary>
        /// 最大数据库备份文件个数
        /// </summary>
        public static readonly int MaxBackupDatabaseCount = 200;
        /// <summary>
        /// 是否是单元测试
        /// </summary>
        public static readonly bool IsUnitTest = false;

        /// <summary>
        /// 是否正在进行安装，如果是，则不抛出监测安装的异常
        /// </summary>
        public static bool IsInstalling { get; set; } = false;

        public static int PageViewCount { get; set; } //网站启动后前台页面浏览量


        /// <summary>
        /// 是否应有数据库模块载入
        /// </summary>
        public static bool DatabaseXncfLoaded { get; set; }

        /// <summary>
        /// 系统状态
        /// </summary>
        public static NcfCoreState NcfCoreState { get; } = NcfCoreState.Instance;

        //异步线程
        public static Dictionary<string, Thread> AsynThread = new Dictionary<string, Thread>(); //后台运行线程

        /// <summary>
        /// Admin 管理员的 Cookie 登录 Scheme
        /// </summary>
        public readonly static string NcfAdminAuthorizeScheme = "NcfAdminAuthorizeScheme";
        /// <summary>
        /// User 管理员的 Cookie 登录 Scheme
        /// </summary>
        public readonly static string NcfUserAuthorizeScheme = "NcfUserAuthorizeScheme";
    }
}
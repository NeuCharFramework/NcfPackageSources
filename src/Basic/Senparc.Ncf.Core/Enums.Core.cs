namespace Senparc.Ncf.Core.Enums
{
    /// <summary>
    /// Message type (level)
    /// </summary>
    public enum MessageType
    {
        danger,
        warning,
        info,
        success
    }


    /// <summary>
    /// Email account
    /// </summary>
    public enum EmailAccountType
    {
        Default,
        _163,
        Souidea
    }

    /// <summary>
    /// Ordering type
    /// </summary>
    public enum OrderingType
    {
        Ascending = 0,
        Descending = 1
    }

    /// <summary>
    /// Install or update
    /// </summary>
    public enum InstallOrUpdate
    {
        Install,
        Update
    }

    /// <summary>
    /// Email setting type
    /// </summary>
    public enum SendEmailType
    {
        Test = 0,
        CustomEmail,
        LiveCode,
        PassLiveCode,
        ResetPassword,
        InviteCode,
        OrderCreate,
        OrderPaySuccess,
        OrderCancelled,
        WeixinStat, //WeChat statistics
        AppStatusChanged, //Application status changed
    }


    /// <summary>
    /// Meta type
    /// </summary>
    public enum MetaType
    {
        keywords,
        description
    }

    #region Entity properties


    public enum Account_RegisterWay
    {
        官网注册 = 0,
        快捷注册 = 1,
        QQ注册 = 2,
        微博注册 = 3,
        微信自动注册 = 4
    }

    public enum AccountPayLog_PayType
    {
        网银支付 = 0,
        支付宝 = 1,
        微信支付 = 2
    }

    public enum AccountPayLog_Status
    {
        未支付 = 0,
        已支付 = 1,
        已取消 = 2,
        已冻结 = 3
    }


    public enum XncfModules_State
    {
        关闭 = 0,
        开放 = 1,
        新增待审核 = 2,
        更新待审核 = 3,
    }

    #endregion
}

//#region Temporary workaround for MySQL library bug

//#if RELEASE
//namespace Microsoft.EntityFrameworkCore.Metadata
//{
//    /// <summary>
//    /// Note: due to an insufficient decoupling issue in Pomelo.EntityFrameworkCore.MySql, this temporary reference is used and will be removed after upstream upgrade.
//    /// <para>Official feedback: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1205</para>
//    /// </summary>
//    public enum MySqlValueGenerationStrategy
//    {
//        None = 0,
//        IdentityColumn = 1,
//        ComputedColumn = 2
//    }
//}
//#endif
//#endregion
namespace Senparc.Ncf.Core.Enums
{


    public enum MessageType
    {
        danger,
        warning,
        info,
        success
    }


    /// <summary>
    /// Email账户
    /// </summary>
    public enum EmailAccountType
    {
        Default,
        _163,
        Souidea
    }

    /// <summary>
    /// 排序类型
    /// </summary>
    public enum OrderingType
    {
        Ascending = 0,
        Descending = 1
    }

    /// <summary>
    /// 安装或更新
    /// </summary>
    public enum InstallOrUpdate
    {
        Install,
        Update
    }

    /// <summary>
    /// Email设置类型
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
        WeixinStat, //微信统计
        AppStatusChanged, //应用状态改变
    }


    /// <summary>
    /// Meta类型
    /// </summary>
    public enum MetaType
    {
        keywords,
        description
    }

    #region 实体属性


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


    public enum XscfModules_State
    {
        关闭 = 0,
        开放 = 1,
        新增待审核 = 2,
        更新待审核 = 3,
    }

    #endregion
}
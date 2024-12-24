namespace Senparc.Ncf.Core.Enums
{
    /// <summary>
    /// 消息类型（级别）
    /// </summary>
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


    public enum XncfModules_State
    {
        关闭 = 0,
        开放 = 1,
        新增待审核 = 2,
        更新待审核 = 3,
    }

    #endregion
}

//#region 弥补 MySQL 库暂时的 bug

//#if RELEASE
//namespace Microsoft.EntityFrameworkCore.Metadata
//{
//    /// <summary>
//    /// 说明：因为 Pomelo.EntityFrameworkCore.MySql 一个未充分解耦的问题，这里暂时先引用，待其升级后会取消，和具体数据库充分解耦
//    /// <para>官方反馈：https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1205</para>
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
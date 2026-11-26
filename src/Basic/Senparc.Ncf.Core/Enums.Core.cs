/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Enums.Core.cs
    File description: Enums.Core related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

----------------------------------------------------------------*/

namespace Senparc.Ncf.Core.Enums
{
    /// <summary>
    /// Identifies the message severity rendered by NCF UI responses.
    /// </summary>
    public enum MessageType
    {
        danger,
        warning,
        info,
        success
    }


    /// <summary>
    /// Identifies the configured SMTP account used for email delivery.
    /// </summary>
    public enum EmailAccountType
    {
        Default,
        _163,
        Souidea
    }

    /// <summary>
    /// Specifies the direction used by ordering helpers.
    /// </summary>
    public enum OrderingType
    {
        Ascending = 0,
        Descending = 1
    }

    /// <summary>
    /// Identifies whether a module lifecycle operation installs or updates a module.
    /// </summary>
    public enum InstallOrUpdate
    {
        Install,
        Update
    }

    /// <summary>
    /// Emailsettingstype
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
        WeixinStat, //Legacy comment translated to English.
        AppStatusChanged, //Legacy comment translated to English.
    }


    /// <summary>
    /// Metatype
    /// </summary>
    public enum MetaType
    {
        keywords,
        description
    }

    #region Legacy section


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

//Legacy section

//#if RELEASE
//namespace Microsoft.EntityFrameworkCore.Metadata
//{
//    /// <summary>
//Legacy comment translated to English.
//Legacy comment translated to English.
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

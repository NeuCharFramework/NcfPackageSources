/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Enums.cs
    File description: Enums related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.13-preview13 Updated SMS integration support and related localization metadata

    Modified by: Senparc - 20261127
    Change description: v0.20.13.2-preview14 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.SMS
{

    /// <summary>
    /// Identifies the SMS provider implementation used by NCF.
    /// </summary>
    public enum SmsPlatformType
    {
        /// <summary>Unknown or unsupported provider.</summary>
        Unknow = -1,
        /// <summary>JunMei provider.</summary>
        JunMei = 0,
        /// <summary>Fissoft provider.</summary>
        Fissoft = 1,
        /// <summary>First custom SMS provider slot.</summary>
        SMS1 = 10,
        /// <summary>Second custom SMS provider slot.</summary>
        SMS2 = 20,
        /// <summary>Third custom SMS provider slot.</summary>
        SMS3 = 30,
        /// <summary>Fourth custom SMS provider slot.</summary>
        SMS4 = 40
    }


    /// <summary>
    /// Result codes returned by SMS provider implementations.
    /// </summary>
    public enum SmsResult
    {
        /// <summary>The provider returned an unknown failure.</summary>
        未知错误 = 0,
        /// <summary>The message was accepted successfully.</summary>
        成功 = 1,
        访问数据库写入数据错误 = -1,
        一次发送的手机号码过多 = -3,
        内容包含不合法文字 = -4,
        登录账户错误 = -5,
        手机号码不合法黑名单 = -9,
        号码太长不能超过100条一次提交 = -10,
        内容太长 = -11,
        余额不足 = -13,
        子号号不正确 = -14,
        参数不全 = -999,
        超过当天请求限制 = -998,
        请在60秒后重试 = -997
    }
}

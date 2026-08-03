/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：NcfDatabaseUpgradeRequiredException.cs
    文件功能描述：区分数据库架构待升级与系统尚未安装

    创建标识：Senparc - 20260803

----------------------------------------------------------------*/

using System;

namespace Senparc.Ncf.Core.Exceptions
{
    /// <summary>
    /// 数据库本身存在，但当前代码所需的迁移尚未应用。
    /// 此异常不得被安装程序当作空数据库处理。
    /// </summary>
    public sealed class NcfDatabaseUpgradeRequiredException : NcfExceptionBase
    {
        public NcfDatabaseUpgradeRequiredException(string message, Exception inner = null)
            : base(message, inner)
        {
        }
    }
}

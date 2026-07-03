/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：MultipleDatabaseType.cs
    文件功能描述：MultipleDatabaseType 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.Models
{
    /// <summary>
    /// 多数据库系统中，特定的某个数据库类型
    /// </summary>
    public enum MultipleDatabaseType
    {
        Sqlite,
        SqlServer,
        MySql,
        InMemory,
        AzureCosmos,
        Oracle,
        PostgreSQL,
        Dm,
        Other = 99999,
        //UnitTest = 100000,
        //TODO:更多
    }
}

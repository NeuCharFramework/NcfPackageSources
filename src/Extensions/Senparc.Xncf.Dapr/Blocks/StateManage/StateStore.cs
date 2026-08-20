/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：StateStore.cs
    文件功能描述：StateStore 相关实现
    
    
    创建标识：Senparc - 20260704
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr.Blocks.StateManage
{
    public class StateStore
    {
        public string Key { get; set; }
        public object? Value { get; set; }

        public StateStore(string key, object? value)
        {
            Key = key;
            Value = value;
        }
    }
}

/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：IStateManage.cs
    文件功能描述：IStateManage 相关实现
    
    
    创建标识：Senparc - 20230925
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.Dapr.Blocks.StateManage.Interface
{
    internal interface IStateManage
    {
        Task<TResult> GetStateAsync<TResult>(string stateStore, string key);
        Task SetStateAsync<TValue>(string stateStore, string key, TValue value);
        Task SetStatesAsync(string stateStore, IEnumerable<StateStore> values);
        Task DelStateAsync(string stateStore, string key);
    }
}

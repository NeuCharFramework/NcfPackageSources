/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: IStateManage.cs

    File description: IStateManage related implementation

    

    

    Created by: Senparc - 20230925

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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

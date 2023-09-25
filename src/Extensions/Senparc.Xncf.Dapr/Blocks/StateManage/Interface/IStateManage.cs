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

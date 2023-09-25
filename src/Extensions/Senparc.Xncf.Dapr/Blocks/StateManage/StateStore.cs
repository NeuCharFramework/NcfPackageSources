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

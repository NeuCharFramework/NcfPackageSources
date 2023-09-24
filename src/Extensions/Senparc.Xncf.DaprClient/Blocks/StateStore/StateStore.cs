using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.DaprClient.Blocks.StateStore
{
    public class StateStore
    {
        public string Key { get; set; }
        public object Value { get; set; }
        public int TtlInSeconds { get; set; }

        public StateStore(string key, object value, int ttlInSeconds)
        {
            Key = key;
            Value = value;
            TtlInSeconds = ttlInSeconds;
        }
    }
}

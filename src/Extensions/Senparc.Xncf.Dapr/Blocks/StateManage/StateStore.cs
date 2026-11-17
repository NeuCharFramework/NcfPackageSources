/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: StateStore.cs
    File description: StateStore related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

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

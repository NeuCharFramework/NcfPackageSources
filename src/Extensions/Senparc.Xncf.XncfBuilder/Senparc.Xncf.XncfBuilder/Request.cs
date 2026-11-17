/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: Request.cs
    File description: Request related implementation

    Created by: Senparc - 20250624

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260705
    Change description: v0.36.3-preview2 Updated module behavior and related support.

    Modified by: Senparc - 20260705
    Change description: v0.36.4-preview3 Updated module behavior and related support.

----------------------------------------------------------------*/

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.XncfBuilder
{
    public class Request
    {
        public string? Method { get; set; }
        public string? Path { get; set; }
        public string? Body { get; set; }

        //Legacy comment translated to English.
        public Dictionary<string, string>? Headers { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

}

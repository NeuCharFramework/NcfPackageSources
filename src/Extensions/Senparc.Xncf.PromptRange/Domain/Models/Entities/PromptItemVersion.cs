/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptItemVersion.cs
    File description: PromptItemVersion related implementation

    Created by: Senparc - 20240713

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.16 Localized PromptRange administration and runtime surfaces

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.PromptRange.Domain.Models
{
    public record class PromptItemVersion
    {
        public string RangeName { get; set; }
        public string Tactic { get; set; }
        public int Aim { get; set; }

        /// <summary>Initializes a parsed prompt-item version.</summary>
        /// <param name="rangeName">The range name.</param>
        /// <param name="tactic">The tactic segment.</param>
        /// <param name="aim">The aiming number.</param>
        public PromptItemVersion(string rangeName, string tactic, int aim)
        {
            RangeName = rangeName;
            Tactic = tactic;
            Aim = aim;
        }
    }
}

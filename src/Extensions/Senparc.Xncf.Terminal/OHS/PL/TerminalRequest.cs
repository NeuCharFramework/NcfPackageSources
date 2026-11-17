/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: TerminalRequest.cs
    File description: TerminalRequest related implementation

    Created by: Senparc - 20211016

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.22.0-preview2 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Senparc.Xncf.Terminal.OHS.PL
{
    public class Terminal_RunRequest : FunctionAppRequestBase
    {
        [MaxLength(300)]
        [LocalizedDescription(typeof(TerminalResource), "Parameter.Terminal.Command")]
        public string CommandLine { get; set; }
    }
}

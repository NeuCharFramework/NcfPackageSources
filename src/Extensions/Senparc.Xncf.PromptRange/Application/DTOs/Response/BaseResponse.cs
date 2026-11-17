/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: BaseResponse.cs
    File description: BaseResponse related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using System;

namespace Senparc.Xncf.PromptRange.OHS.Local.PL.Response
{
    public class BaseResponse
    {
        public int Id { get; set; }

        // public DateTime CreateTime { get; set; }

        public DateTime LastRunTime { get; set; } = DateTime.Now;
    }
}
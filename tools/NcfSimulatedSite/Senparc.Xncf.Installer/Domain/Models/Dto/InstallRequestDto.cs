/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: InstallRequestDto.cs
    File description: Related implementation.

    Created by: Senparc - 20260916

    Modified by: Senparc - 20260916
    Change description: v0.5.7 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.5.9 0.5.9 Merge remote-tracking branch 'origin/Developer-MAF-V3' into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.5.12 Updated module behavior and related support.

----------------------------------------------------------------*/

using System.Collections.Generic;

namespace Senparc.Xncf.Installer.Domain.Dto
{
    public class InstallRequestDto
    {
        public string SystemName { get; set; }
        public string AdminUserName { get; set; }
        public string DbConnectionString { get; set; }
        public List<string> NeedModelList { get; set; }//Legacy comment translated to English.
    }
}

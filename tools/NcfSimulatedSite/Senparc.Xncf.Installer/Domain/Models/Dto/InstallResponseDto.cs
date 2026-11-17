/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: InstallResponseDto.cs
    File description: Related implementation.


    Created by: Senparc - 20260916

    Modified by: Senparc - 20260916
    Change description: v0.5.7 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.5.9 0.5.9 Merge remote-tracking branch 'origin/Developer-MAF-V3' into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.5.12 Updated module behavior and related support.

----------------------------------------------------------------*/

using Senparc.Xncf.Tenant.Domain.DataBaseModel;
using System.Collections.Generic;

namespace Senparc.Xncf.Installer.Domain.Dto
{
    public class InstallResponseDto
    {
        public int StatCode { get; set; }
        public string SystemName { get; set; }
        public string AdminUserName { get; set; }
        public string AdminPassword { get; set; }
        public int Step { get; set; }
        public TenantInfoDto TenantInfoDto { get; set; }
        public List<XncfRegisterDto> NeedModelList { get; set; }
    }
}

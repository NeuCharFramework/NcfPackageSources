/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: XncfRegisterDto.cs
    File description: Related implementation.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260724
    Change description: v0.1.1 Updated module behavior and related support.

    Modified by: Senparc - 20260916
    Change description: v0.5.7 Updated module behavior and related support.

    Modified by: Senparc - 20261005
    Change description: v0.5.9 0.5.9 Merge remote-tracking branch 'origin/Developer-MAF-V3' into Developer-MAF-V3-Spark

    Modified by: Senparc - 20261103
    Change description: v0.5.12 Updated module behavior and related support.

----------------------------------------------------------------*/
namespace Senparc.Xncf.Installer.Domain.Dto
{
    /* Legacy comment translated to English.*/
    public class XncfRegisterDto
    {
        public bool IgnoreInstall { get; set; }
        public bool SelectedByDefault { get; set; }
        public string Name { get; set; }
        public string Uid { get; set; }
        public string Version { get; set; }
        public string MenuName { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
    }
}

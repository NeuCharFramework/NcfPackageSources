using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Xncf.PromptRange.Domain.Models
{
    /// <summary>
    /// 版本号信息
    /// </summary>
    public class VersionInfo
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public int Build { get; set; }

        public VersionInfo(int major, int minor, int patch, int build)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Build = build;
        }
    }
}

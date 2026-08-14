/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    文件名：XncfDevelopmentJobRequest.cs
    文件功能描述：受控 XNCF 开发工作流 Function 请求模型

    创建标识：Senparc - 20260814

    修改标识：Senparc - 20260815
    修改描述：v0.41.0-preview11 增强隔离开发任务与 Sandbox 预览流程

----------------------------------------------------------------*/

using Senparc.Ncf.XncfBase.FunctionRenders;
using Senparc.Xncf.XncfBuilder.Domain.Services.Development;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Xncf.XncfBuilder.OHS.PL
{
    public sealed class XncfDevelopmentStartRequest : FunctionAppRequestBase
    {
        [Required, MaxLength(1200)]
        public string SlnFilePath { get; set; }

        [Required]
        public XncfDevelopmentJobMode Mode { get; set; }

        /// <summary>Create mode: complete name is derived from OrgName + XncfName.</summary>
        [MaxLength(256)]
        public string ModuleProjectName { get; set; }

        [MaxLength(100)]
        public string OrgName { get; set; }

        [MaxLength(100)]
        public string XncfName { get; set; }

        [MaxLength(50)]
        public string TargetFramework { get; set; } = "net10.0";

        [MaxLength(50)]
        public string Version { get; set; } = "0.1.0";

        [MaxLength(100)]
        public string MenuName { get; set; }

        [MaxLength(100)]
        public string Icon { get; set; } = "fa fa-puzzle-piece";

        [MaxLength(400)]
        public string Description { get; set; }

        [Required, MaxLength(4000)]
        public string Requirement { get; set; }

        public bool IncludeFunction { get; set; }
        public bool IncludeDatabase { get; set; }
        public bool IncludeWeb { get; set; }
        public bool IncludeWebApi { get; set; }
        public bool IncludeSample { get; set; }
    }

    public class XncfDevelopmentJobIdRequest : FunctionAppRequestBase
    {
        [Required, MaxLength(64)]
        public string JobId { get; set; }
    }

    public class XncfDevelopmentReadFileRequest : XncfDevelopmentJobIdRequest
    {
        [Required, MaxLength(500)]
        public string RelativeFilePath { get; set; }
    }

    public sealed class XncfDevelopmentWriteFileRequest : XncfDevelopmentReadFileRequest
    {
        [Required, MaxLength(4 * 1024 * 1024)]
        public string Content { get; set; }

        [MaxLength(64)]
        public string ExpectedSha256 { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Senparc.Areas.Admin.Domain.Models.DatabaseModel.Dto;

public sealed class AdminChatAttachmentInput
{
    [Required, MaxLength(300)]
    public string RelativePath { get; set; } = "";
    [Required, MaxLength(150)]
    public string ETag { get; set; } = "";
    [MaxLength(150)]
    public string VisionETag { get; set; } = "";
    public bool UseVision { get; set; }
}

public sealed class AdminChatAttachment
{
    public string RelativePath { get; set; } = "";
    public string Name { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Size { get; set; }
    public string ETag { get; set; } = "";
    public string VisionETag { get; set; } = "";
    public bool UseVision { get; set; }
    public bool CanUseVision => !string.IsNullOrEmpty(VisionETag);
    public string LocalStatus { get; set; } = "";
    public int SectionCount { get; set; }
    public int ExtractedCharacters { get; set; }
    public string Notice { get; set; } = "";
}

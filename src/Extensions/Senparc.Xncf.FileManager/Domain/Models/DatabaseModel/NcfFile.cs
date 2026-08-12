/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：NcfFile.cs
    文件功能描述：NcfFile 相关实现


    创建标识：Senparc - 20250112

    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

    修改标识：Senparc - 20260813
    修改描述：v0.6.0-preview1 完善文件资源边界、安全删除策略与静态资源管理

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.FileManager.Domain.Models.DatabaseModel
{
    //[Table(Register.DATABASE_PREFIX + nameof(NcfFile))]//必须添加前缀，防止全系统中发生冲突

    [Table(Register.DATABASE_PREFIX + nameof(NcfFile))]//必须添加前缀，防止全系统中发生冲突
    public class NcfFile : EntityBase<int>
    {
        [Required]
        [MaxLength(250)]
        public string FileName { get; set; }

        [Required]
        public string StorageFileName { get; set; } // Guid 格式的文件名

        [Required]
        public string FilePath { get; set; } // 相对路径，例如：App_Data/NcfFiles/yyyy/MM/

        public long FileSize { get; set; } // 文件大小（字节）

        [MaxLength(100)]
        public string FileExtension { get; set; } // 文件扩展名

        public FileType FileType { get; set; } // 文件类型枚举

        [MaxLength(300)]
        public string Description { get; set; } // 文件描述

        public DateTime UploadTime { get; set; } // 上传时间

        public int? FolderId { get; set; } // 所属文件夹，可为空表示根目录

        /// <summary>
        /// 文件的业务用途。用途一经确定不可在管理页面中直接转换，避免把知识库源文件误发布为站点资源。
        /// </summary>
        public NcfFileResourceScope ResourceScope { get; set; } = NcfFileResourceScope.KnowledgeBase;

        /// <summary>
        /// 仅站点静态资源允许发布为公开访问；知识库源文件始终保持私有。
        /// </summary>
        public NcfFileAccessLevel AccessLevel { get; set; } = NcfFileAccessLevel.Private;

        [MaxLength(200)]
        public string ContentType { get; set; }

        /// <summary>
        /// 文件写入完成后计算的 SHA-256，用于完整性标识及公开资源 URL 的缓存指纹。
        /// </summary>
        [MaxLength(64)]
        public string ContentHash { get; set; }
    }

    public enum NcfFileResourceScope
    {
        /// <summary>可供 KnowledgeBase 提取和切片的资料文件。</summary>
        KnowledgeBase = 100,

        /// <summary>站点可引用的图片、音视频和字体等静态资源。</summary>
        SiteAsset = 200
    }

    public enum NcfFileAccessLevel
    {
        Private = 0,
        Public = 100
    }

    public enum FileType
    {
        Text = 0,
        Word = 1,
        PowerPoint = 2,
        Excel = 3,
        Code = 4,
        Other = 999
    }

}

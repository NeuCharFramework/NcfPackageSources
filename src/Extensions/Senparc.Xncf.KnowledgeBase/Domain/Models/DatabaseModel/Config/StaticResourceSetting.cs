/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：StaticResourceSetting.cs
    文件功能描述：StaticResourceSetting 相关实现
    
    
    创建标识：Senparc - 20260213
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.KnowledgeBase.Domain.Models.DatabaseModel.Config
{
    public class StaticResourceSetting
    {
        /// <summary>
        /// 上传根目录，可用软链接分离
        /// </summary>
        public string RootDir { get; set; }

        /// <summary>
        /// 请求根目录
        /// </summary>
        public string RequestPath { get; set; }

        /// <summary>
        /// 上传文件大小，单位：MB
        /// </summary>
        public int MaxSize { get; set; }

        /// <summary>
        /// 访问的域名
        /// </summary>
        public string AccessUrl { get; set; }

        /// <summary>
        /// 允许上传的文件
        /// </summary>
        public string[] AllowFile { get; set; }
    }
}

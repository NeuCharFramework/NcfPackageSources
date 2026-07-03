/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：SenMapicTaskItemDto.cs
    文件功能描述：SenMapicTaskItemDto 相关实现
    
    
    创建标识：Senparc - 20250114
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.SenMapic.Domain.Models.DatabaseModel.Dto
{
    public class SenMapicTaskItem_ListItemDto
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string Url { get; set; }
        public string ParentUrl { get; set; }
        public int Deep { get; set; }
        public DateTime CrawlTime { get; set; }
        public long PageSize { get; set; }
        public string Title { get; set; }
        public int ResponseMillionSeconds { get; private set; }
        public string Html { get; private set; }
        public int Result { get; private set; }
        public string TitleHtml { get; private set; }
        public string MarkDownHtmlContent { get; private set; }
        public double SizeKB { get; private set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public string ContentType { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}

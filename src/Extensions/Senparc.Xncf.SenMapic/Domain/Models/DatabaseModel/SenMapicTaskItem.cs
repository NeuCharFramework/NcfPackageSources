using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.SenMapic
{
    [Table(Register.DATABASE_PREFIX + nameof(SenMapicTaskItem))]
    public class SenMapicTaskItem : EntityBase<int>
    {
        public int TaskId { get; set; }
        [ForeignKey("TaskId")]
        public virtual SenMapicTask Task { get; set; }
        
        public string Url { get; set; }
        public string ParentUrl { get; set; }
        public int Deep { get; set; }
        public DateTime CrawlTime { get; set; }
        public long PageSize { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public string ContentType { get; set; }
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; }

        public SenMapicTaskItem() { }

        public SenMapicTaskItem(SenMapicTask task, UrlData urlData)
        {
            TaskId = task.Id;
            Url = urlData.Url;
            ParentUrl = urlData.ParentUrl;
            Deep = urlData.Deep;
            CrawlTime = urlData.LastUpdateTime;
            PageSize = urlData.PageSize;
            Title = urlData.Title;
            Description = urlData.Description;
            Keywords = urlData.Keywords;
            ContentType = urlData.ContentType;
            StatusCode = urlData.StatusCode;
            ErrorMessage = urlData.ErrorMessage;
        }
    }
} 
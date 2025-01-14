using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.SenMapic
{
    /// <summary>
    /// 任务状态枚举
    /// </summary>
    public enum TaskStatus
    {
        /// <summary>
        /// 出错
        /// </summary>
        Error = -1,
        /// <summary>
        /// 等待开始
        /// </summary>
        Waiting = 0,
        /// <summary>
        /// 进行中
        /// </summary>
        Running = 1,
        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 2
    }

    /// <summary>
    /// SenMapic 爬虫任务实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(SenMapicTask))]
    [Serializable]
    public class SenMapicTask : EntityBase<int>
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 起始URL
        /// </summary>
        public string StartUrl { get; private set; }

        /// <summary>
        /// 最大线程数
        /// </summary>
        public int MaxThread { get; private set; }

        /// <summary>
        /// 单站点最大爬取时间(分钟)
        /// </summary>
        public int MaxBuildMinutes { get; private set; }

        /// <summary>
        /// 最大爬取深度
        /// </summary>
        public int MaxDeep { get; private set; }

        /// <summary>
        /// 最大页面数量
        /// </summary>
        public int MaxPageCount { get; private set; }

        /// <summary>
        /// 任务状态
        /// </summary>
        public TaskStatus Status { get; private set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; private set; }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? EndTime { get; private set; }

        /// <summary>
        /// 已爬取的页面数量
        /// </summary>
        public int CrawledPages { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; private set; }

        private SenMapicTask() { }

        public SenMapicTask(string name, string startUrl, int maxThread = 4, int maxBuildMinutes = 10, 
            int maxDeep = 5, int maxPageCount = 500)
        {
            Name = name;
            StartUrl = startUrl;
            MaxThread = maxThread;
            MaxBuildMinutes = maxBuildMinutes;
            MaxDeep = maxDeep;
            MaxPageCount = maxPageCount;
            Status = TaskStatus.Waiting;
            CrawledPages = 0;
        }

        public void Start()
        {
            Status = TaskStatus.Running;
            StartTime = DateTime.Now;
        }

        public void Complete()
        {
            Status = TaskStatus.Completed;
            EndTime = DateTime.Now;
        }

        public void Error(string message)
        {
            Status = TaskStatus.Error;
            EndTime = DateTime.Now;
            ErrorMessage = message;
        }

        public void IncrementCrawledPages()
        {
            CrawledPages++;
        }
    }
} 
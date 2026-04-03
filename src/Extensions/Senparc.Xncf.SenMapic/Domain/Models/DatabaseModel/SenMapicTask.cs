using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.SenMapic
{
    /// <summary>
    ///Task status enumeration
    /// </summary>
    public enum SenMapicTaskStatus
    {
        /// <summary>
        /// error
        /// </summary>
        Error = -1,
        /// <summary>
        ///wait to start
        /// </summary>
        Waiting = 0,
        /// <summary>
        /// in progress
        /// </summary>
        Running = 1,
        /// <summary>
        /// Completed
        /// </summary>
        Completed = 2
    }

    /// <summary>
    /// SenMapic crawler task entity class
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(SenMapicTask))]
    [Serializable]
    public class SenMapicTask : EntityBase<int>
    {
        /// <summary>
        /// task name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///Start URL
        /// </summary>
        public string StartUrl { get; private set; }

        /// <summary>
        ///Maximum number of threads
        /// </summary>
        public int MaxThread { get; private set; }

        /// <summary>
        /// Maximum crawling time of a single site (minutes)
        /// </summary>
        public int MaxBuildMinutes { get; private set; }

        /// <summary>
        ///Maximum crawl depth
        /// </summary>
        public int MaxDeep { get; private set; }

        /// <summary>
        ///Maximum number of pages
        /// </summary>
        public int MaxPageCount { get; private set; }

        /// <summary>
        ///task status
        /// </summary>
        public SenMapicTaskStatus Status { get; private set; }

        /// <summary>
        /// start time
        /// </summary>
        public DateTime? StartTime { get; private set; }

        /// <summary>
        ///completion time
        /// </summary>
        public DateTime? EndTime { get; private set; }

        /// <summary>
        ///Number of pages crawled
        /// </summary>
        public int CrawledPages { get; private set; }

        /// <summary>
        /// error message
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
            Status = SenMapicTaskStatus.Waiting;
            CrawledPages = 0;
        }

        public void Start()
        {
            Status = SenMapicTaskStatus.Running;
            StartTime = DateTime.Now;
        }

        public void Complete()
        {
            Status = SenMapicTaskStatus.Completed;
            EndTime = DateTime.Now;
        }

        public void Error(string message)
        {
            Status = SenMapicTaskStatus.Error;
            EndTime = DateTime.Now;
            ErrorMessage = message;
        }

        public void IncrementCrawledPages()
        {
            CrawledPages++;
        }
    }
} 
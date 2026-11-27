/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapicTask.cs
    File description: SenMapicTask related implementation

    Created by: Senparc - 20250114

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.4.13 Updated module behavior, localization metadata, and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.4.13.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.SenMapic
{
    /// <summary>Identifies the lifecycle state of a SenMapic crawl task.</summary>
    public enum SenMapicTaskStatus
    {
        /// <summary>The crawl failed.</summary>
        Error = -1,
        /// <summary>
        /// waitstart
        /// </summary>
        Waiting = 0,
        /// <summary>
        /// in progress
        /// </summary>
        Running = 1,
        /// <summary>The task is waiting to start.</summary>
        Completed = 2
    }

    /// <summary>Stores bounded crawl settings, progress, lifecycle state, and errors.</summary>
    [Table(Register.DATABASE_PREFIX + nameof(SenMapicTask))]
    [Serializable]
    public class SenMapicTask : EntityBase<int>
    {
        /// <summary>
        /// task name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>Gets the initial URL to crawl.</summary>
        public string StartUrl { get; private set; }

        /// <summary>Gets the maximum crawler concurrency.</summary>
        public int MaxThread { get; private set; }

        /// <summary>Gets the maximum build duration in minutes.</summary>
        public int MaxBuildMinutes { get; private set; }

        /// <summary>Gets the maximum crawl depth.</summary>
        public int MaxDeep { get; private set; }

        /// <summary>Gets the maximum page count.</summary>
        public int MaxPageCount { get; private set; }

        /// <summary>
        /// taskstatus
        /// </summary>
        public SenMapicTaskStatus Status { get; private set; }

        /// <summary>Gets the task start timestamp.</summary>
        public DateTime? StartTime { get; private set; }

        /// <summary>Gets the task end timestamp.</summary>
        public DateTime? EndTime { get; private set; }

        /// <summary>Gets the number of crawled pages.</summary>
        public int CrawledPages { get; private set; }

        /// <summary>
        /// errorinformation
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Initializes an empty task for ORM materialization.
        /// </summary>
        private SenMapicTask() { }

        /// <summary>
        /// Creates a waiting crawl task with bounded execution settings supplied by the caller.
        /// </summary>
        /// <param name="name">The task display name.</param>
        /// <param name="startUrl">The initial crawl URL.</param>
        /// <param name="maxThread">The maximum crawler concurrency.</param>
        /// <param name="maxBuildMinutes">The maximum build duration.</param>
        /// <param name="maxDeep">The maximum crawl depth.</param>
        /// <param name="maxPageCount">The maximum pages to crawl.</param>
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

        /// <summary>
        /// Marks the crawl task as running and records its start time.
        /// </summary>
        public void Start()
        {
            Status = SenMapicTaskStatus.Running;
            StartTime = DateTime.Now;
        }

        /// <summary>
        /// Marks the crawl task as completed and records its end time.
        /// </summary>
        public void Complete()
        {
            Status = SenMapicTaskStatus.Completed;
            EndTime = DateTime.Now;
        }

        /// <summary>
        /// Marks the crawl task as failed and stores its error message.
        /// </summary>
        /// <param name="message">The failure message.</param>
        public void Error(string message)
        {
            Status = SenMapicTaskStatus.Error;
            EndTime = DateTime.Now;
            ErrorMessage = message;
        }

        /// <summary>
        /// Increments the count of pages successfully processed by the crawler.
        /// </summary>
        public void IncrementCrawledPages()
        {
            CrawledPages++;
        }
    }
}

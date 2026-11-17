/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenMapicTask.cs
    File description: SenMapicTask related implementation

    Created by: Senparc - 20250114

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

using Senparc.Ncf.Core.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.SenMapic
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public enum SenMapicTaskStatus
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Error = -1,
        /// <summary>
        /// waitstart
        /// </summary>
        Waiting = 0,
        /// <summary>
        /// in progress
        /// </summary>
        Running = 1,
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        Completed = 2
    }

    /// <summary>
    ///Legacy comment translated to English.
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
        ///Legacy comment translated to English.
        /// </summary>
        public string StartUrl { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxThread { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxBuildMinutes { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxDeep { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int MaxPageCount { get; private set; }

        /// <summary>
        /// taskstatus
        /// </summary>
        public SenMapicTaskStatus Status { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime? StartTime { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public DateTime? EndTime { get; private set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public int CrawledPages { get; private set; }

        /// <summary>
        /// errorinformation
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
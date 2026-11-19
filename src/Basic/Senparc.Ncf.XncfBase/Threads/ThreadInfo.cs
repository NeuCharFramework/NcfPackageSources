/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: ThreadInfo.cs
    File description: ThreadInfo related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260813
    Change description: v0.25.0-preview7 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.29.1 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Threads
{
    /// <summary>
    /// ThreadInfo
    /// </summary>
    public class ThreadInfo
    {
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public TimeSpan IntervalTime { get; set; }
        /// <summary>
        /// executetask
        /// </summary>
        public Func<IApplicationBuilder, ThreadInfo, Task> Task { get; set; }
        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public Func<Exception, Task> ExceptionHandler { get; set; }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public CancellationToken StoppingToken { get; internal set; } = CancellationToken.None;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private List<string> Stories { get; set; } = new List<string>();

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <returns></returns>
        public string StoryHtml => string.Join("<br /><br />", Stories.Select(z => z.HtmlEncode()).ToArray());

        public ThreadInfo(string name, TimeSpan intervalTime, Func<IApplicationBuilder, ThreadInfo, Task> task, Func<Exception, Task> exceptionHandler = null)
        {
            Name = name;
            IntervalTime = intervalTime;
            Task = task;
            ExceptionHandler = exceptionHandler;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public string RecordStory(string msg)
        {
            while (Stories.Count > 10)
            {
                Stories.RemoveAt(0);
            }
            var story = $@"{SystemTime.Now.ToString()}
{msg}";
            Stories.Add(story);
            return story;
        }

    
    }
}

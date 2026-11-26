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
        /// Describes one registered XNCF background task, its interval, lifecycle token,
        /// execution callback, and recent diagnostic stories.
        /// </summary>
    public class ThreadInfo
    {
        /// <summary>
        /// Gets or sets the display name used in thread diagnostics and logs.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the delay between scheduled task iterations.
        /// </summary>
        public TimeSpan IntervalTime { get; set; }
        /// <summary>
        /// Gets or sets the asynchronous callback executed by the thread runner.
        /// </summary>
        public Func<IApplicationBuilder, ThreadInfo, Task> Task { get; set; }
        /// <summary>
        /// Gets or sets the optional callback used to handle unhandled task exceptions.
        /// </summary>
        public Func<Exception, Task> ExceptionHandler { get; set; }

        /// <summary>
        /// Gets the cancellation token signaled when the application is stopping.
        /// </summary>
        public CancellationToken StoppingToken { get; internal set; } = CancellationToken.None;

        /// <summary>
        /// Stores the bounded diagnostic story list for this thread.
        /// </summary>
        private List<string> Stories { get; set; } = new List<string>();

        /// <summary>
        /// Returns recent thread stories as HTML-encoded line breaks.
        /// </summary>
        /// <returns>The encoded story history.</returns>
        public string StoryHtml => string.Join("<br /><br />", Stories.Select(z => z.HtmlEncode()).ToArray());

        /// <summary>
        /// Initializes a background thread definition.
        /// </summary>
        /// <param name="name">The display name.</param>
        /// <param name="intervalTime">The delay between task iterations.</param>
        /// <param name="task">The asynchronous task callback.</param>
        /// <param name="exceptionHandler">Optional callback for unhandled task exceptions.</param>
        public ThreadInfo(string name, TimeSpan intervalTime, Func<IApplicationBuilder, ThreadInfo, Task> task, Func<Exception, Task> exceptionHandler = null)
        {
            Name = name;
            IntervalTime = intervalTime;
            Task = task;
            ExceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// Adds a timestamped diagnostic story and retains only the most recent entries.
        /// </summary>
        /// <param name="msg">The diagnostic message.</param>
        /// <returns>The timestamped story entry.</returns>
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

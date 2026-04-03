using Microsoft.AspNetCore.Builder;
using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Ncf.XncfBase.Threads
{
    /// <summary>
    /// ThreadInfo
    /// </summary>
    public class ThreadInfo
    {
        /// <summary>
        /// is used to identify Thread, please ensure it is unique in a single XNCF module
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        ///interval time
        /// </summary>
        public TimeSpan IntervalTime { get; set; }
        /// <summary>
        /// perform tasks
        /// </summary>
        public Func<IApplicationBuilder, ThreadInfo, Task> Task { get; set; }
        /// <summary>
        /// Handling when an exception occurs
        /// </summary>
        public Func<Exception, Task> ExceptionHandler { get; set; }
        /// <summary>
        ///Last story record
        /// </summary>
        private List<string> Stories { get; set; } = new List<string>();

        /// <summary>
        /// Get story HTML code
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
        ///record story
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

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
        /// 用于识别 Thread，请确保单个 XNCF 模块中唯一
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 间隔时间
        /// </summary>
        public TimeSpan IntervalTime { get; set; }
        /// <summary>
        /// 执行任务
        /// </summary>
        public Func<IApplicationBuilder, ThreadInfo, Task> Task { get; set; }
        /// <summary>
        /// 发生异常时的处理
        /// </summary>
        public Func<Exception, Task> ExceptionHandler { get; set; }
        /// <summary>
        /// 最后故事记录
        /// </summary>
        private List<string> Stories { get; set; } = new List<string>();

        /// <summary>
        /// 获取故事 HTML代码
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
        /// 记录故事
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

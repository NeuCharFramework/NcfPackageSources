/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：ThreadInfo.cs
    文件功能描述：ThreadInfo 相关实现
    
    
    创建标识：Senparc - 20200724
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

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
        /// 应用正在停止时取消的令牌，由 <see cref="XncfThreadBuilder"/> 在启动线程时提供。
        /// </summary>
        public CancellationToken StoppingToken { get; internal set; } = CancellationToken.None;

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

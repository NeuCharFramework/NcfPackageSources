/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc
  
    文件名：WurflThread.cs
    文件功能描述：WurflThread 相关实现
    
    
    创建标识：Senparc - 20211124
    
    修改标识：Senparc - 20260704
    修改描述：vNext 补充标准化文件头注释

----------------------------------------------------------------*/

namespace Senparc.Utility
{
    ///// <summary>
    ///// 异步初始化的线程
    ///// </summary>
    //public class WurflThread
    //{
    //    public WurflThread()
    //    {
    //    }

    //    public void Run()
    //    {
    //        try
    //        {
    //            lock (WurflUtility.RegisterLock)
    //            {
    //                Thread.Sleep(10 * 1000);//停止10秒，先等第一个请求的页面通过

    //                WurflUtility.RegisterState = "begin";
    //                var startTime = DateTime.Now;
    //                Senparc.Utility.WurflUtility.RegisterWurfl();
    //                var endTime = DateTime.Now;
    //                Senparc.Utility.WurflUtility.WurflStartupTime = (endTime - startTime).TotalSeconds.ToString("##.##");
    //                WurflUtility.RegisterState = "finish";
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            WurflUtility.RegisterState = "failed";
    //            LogUtility.WebLogger.Error(ex.Message, ex);
    //        }
    //    }
    //}
}

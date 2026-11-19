/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: CacheFilterAttribute.cs
    File description: CacheFilterAttribute related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

namespace Senparc.Mvc
{
    //Legacy comment translated to English.

    //public class CacheFilterAttribute : ActionFilterAttribute
    //{
    //    /// <summary>
    //    /// Gets or sets the cache duration in seconds. The default is 10 seconds.
    //    /// </summary>
    //    /// <value>The cache duration in seconds.</value>
    //    public int Duration
    //    {
    //        get;
    //        set;
    //    }

    //    public CacheFilterAttribute()
    //    {
    //        Duration = 10;
    //    }

    //    public override void OnActionExecuted(ActionExecutedContext filterContext)
    //    {

    //        if (Duration <= 0) return;

    //        HttpCachePolicyBase cache = filterContext.HttpContext.Response.Cache;
    //        TimeSpan cacheDuration = TimeSpan.FromSeconds(Duration);

    //        cache.SetCacheability(HttpCacheability.Public);
    //        cache.SetExpires(DateTime.Now.Add(cacheDuration));
    //        cache.SetMaxAge(cacheDuration);
    //        cache.AppendCacheExtension("must-revalidate, proxy-revalidate");
    //    }
    //}

}
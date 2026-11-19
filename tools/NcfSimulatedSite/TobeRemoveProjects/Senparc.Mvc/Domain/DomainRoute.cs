/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: DomainRoute.cs
    File description: DomainRoute related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

//Legacy comment translated to English.


namespace Senparc.Mvc
{
    //public class DomainRoute : Route
    //{
    //    private Regex domainRegex;
    //    private Regex pathRegex;

    //    public string Domain { get; set; }
    //    public string AreaName { get; set; }
    //    public string[] Namespaces { get; set; }
    //    public DomainRoute(string domain, string areaName, string url, RouteValueDictionary defaults, string[] namespaces = null)
    //        : base(url, defaults, new MvcRouteHandler())
    //    {
    //        Domain = domain;
    //        AreaName = areaName;
    //        Namespaces = namespaces;
    //    }

    //    public DomainRoute(string domain, string areaName, string url, RouteValueDictionary defaults, IRouteHandler routeHandler, string[] namespaces = null)
    //        : base(url, defaults, routeHandler)
    //    {
    //        Domain = domain;
    //        AreaName = areaName;
    //        Namespaces = namespaces;
    //    }

    //    public DomainRoute(string domain, string areaName, string url, object defaults, string[] namespaces = null)
    //        : base(url, new RouteValueDictionary(defaults), new MvcRouteHandler())
    //    {
    //        Domain = domain;
    //        AreaName = areaName;
    //        Namespaces = namespaces;
    //    }

    //    public DomainRoute(string domain, string areaName, string url, object defaults, IRouteHandler routeHandler, string[] namespaces = null)
    //        : base(url, new RouteValueDictionary(defaults), routeHandler)
    //    {
    //        Domain = domain;
    //        AreaName = areaName;
    //        Namespaces = namespaces;
    //    }

    //    public override RouteData GetRouteData(HttpContextBase httpContext)
    //    {
    //        // buildregex
    //        domainRegex = CreateRegex(Domain);
    //        pathRegex = CreateRegex(Url);

    //        // requestinformation
    //        string requestDomain = httpContext.Request.Headers["host"];
    //        if (!string.IsNullOrEmpty(requestDomain))
    //        {
    //            if (requestDomain.IndexOf(":") > 0)
    //            {
    //                requestDomain = requestDomain.Substring(0, requestDomain.IndexOf(":"));
    //            }
    //        }
    //        else
    //        {
    //            requestDomain = httpContext.Request.Url.Host;
    //        }
    //        string requestPath = httpContext.Request.AppRelativeCurrentExecutionFilePath.Substring(2) + httpContext.Request.PathInfo;

    //Legacy comment translated to English.

    //        Match domainMatch = domainRegex.Match(requestDomain);
    //        Match pathMatch = pathRegex.Match(requestPath);

    //        // Route data

    //        RouteData data = null;
    //        if (domainMatch.Success && pathMatch.Success)
    //        {
    //            data = new RouteData(this, RouteHandler);

    //Legacy comment translated to English.
    //            if (Defaults != null)
    //            {
    //                foreach (KeyValuePair<string, object> item in Defaults)
    //                {
    //                    data.Values[item.Key] = item.Value;
    //                }
    //            }

    //Legacy comment translated to English.
    //            for (int i = 1; i < domainMatch.Groups.Count; i++)
    //            {
    //                Group group = domainMatch.Groups[i];
    //                if (group.Success)
    //                {
    //                    string key = domainRegex.GroupNameFromNumber(i);
    //                    if (!string.IsNullOrEmpty(key) && !char.IsNumber(key, 0))
    //                    {
    //                        if (!string.IsNullOrEmpty(group.Value))
    //                        {
    //                            data.Values[key] = group.Value;
    //                        }
    //                    }
    //                }
    //            }

    //Legacy comment translated to English.
    //            for (int i = 1; i < pathMatch.Groups.Count; i++)
    //            {
    //                Group group = pathMatch.Groups[i];
    //                if (group.Success)
    //                {
    //                    string key = pathRegex.GroupNameFromNumber(i);
    //                    if (!string.IsNullOrEmpty(key) && !char.IsNumber(key, 0))
    //                    {
    //                        if (!string.IsNullOrEmpty(group.Value))
    //                        {
    //                            data.Values[key] = group.Value;
    //                        }
    //                    }
    //                }
    //            }

    //Legacy comment translated to English.
    //            if ((Namespaces != null) && (Namespaces.Length > 0))
    //            {
    //                data.DataTokens["Namespaces"] = Namespaces;
    //            }

    //            bool flag = (Namespaces == null) || (Namespaces.Length == 0);
    //            data.DataTokens["UseNamespaceFallback"] = flag;
    //            data.DataTokens["area"] = this.AreaName;

    //            //data.Values["namespaces"] = Namespaces;
    //            //data.Values["namespace"] = Namespaces;
    //        }
    //        return data;
    //    }

    //    public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
    //    {
    //        return base.GetVirtualPath(requestContext, RemoveDomainTokens(values));
    //    }

    //    public DomainData GetDomainData(RequestContext requestContext, RouteValueDictionary values)
    //    {
    //Legacy comment translated to English.
    //        string hostname = Domain;
    //        foreach (KeyValuePair<string, object> pair in values)
    //        {
    //            hostname = hostname.Replace("{" + pair.Key + "}", pair.Value.ToString());
    //        }

    //Legacy comment translated to English.
    //        return new DomainData
    //        {
    //            Protocol = "http",
    //            HostName = hostname,
    //            Fragment = ""
    //        };
    //    }

    //    private Regex CreateRegex(string source)
    //    {
    //Legacy comment translated to English.
    //        source = source.Replace("/", @"/?");
    //        source = source.Replace(".", @".?");
    //        source = source.Replace("-", @"-?");
    //        source = source.Replace("{", @"(?<");
    //        source = source.Replace("}", @">([a-zA-Z0-9_]*))");

    //        return new Regex("^" + source + "$");
    //    }

    //    private RouteValueDictionary RemoveDomainTokens(RouteValueDictionary values)
    //    {
    //        Regex tokenRegex = new Regex(@"({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?({[a-zA-Z0-9_]*})*-?.?/?");
    //        Match tokenMatch = tokenRegex.Match(Domain);
    //        for (int i = 0; i < tokenMatch.Groups.Count; i++)
    //        {
    //            Group group = tokenMatch.Groups[i];
    //            if (group.Success)
    //            {
    //                string key = group.Value.Replace("{", "").Replace("}", "");
    //                if (values.ContainsKey(key))
    //                    values.Remove(key);
    //            }
    //        }

    //        return values;
    //    }
    //}
}

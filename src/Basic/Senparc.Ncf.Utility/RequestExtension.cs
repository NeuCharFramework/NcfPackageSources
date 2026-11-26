/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: RequestExtension.cs
    File description: RequestExtension related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.21.3-preview5 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Net;

namespace Senparc.Ncf.Utility
{
    /// <summary>
    /// Provides helpers for constructing request URLs, classifying user agents,
    /// and reading the remote connection address.
    /// </summary>
    public static class RequestExtension
    {
        private const string NullIpAddress = "::1";

        /// <summary>
        /// Determines whether an address is present and is not the IPv6 loopback
        /// representation treated as an unset address by this legacy helper.
        /// </summary>
        /// <param name="address">The address to inspect.</param>
        /// <returns>
        /// <see langword="true"/> when the address is non-null and not
        /// <c>::1</c>; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool IsSet(this IPAddress address)
        {
            return address != null && address.ToString() != NullIpAddress;
        }

        /// <summary>
        /// Builds the absolute URI represented by the request scheme, host, path base,
        /// path, and query string.
        /// </summary>
        /// <param name="request">The HTTP request to convert.</param>
        /// <returns>The absolute request URI.</returns>
        /// <exception cref="NullReferenceException">
        /// Thrown when <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        public static string AbsoluteUri(this HttpRequest request)
        {
            var absoluteUri = string.Concat(
                          request.Scheme,
                          "://",
                          request.Host.ToUriComponent(),
                          request.PathBase.ToUriComponent(),
                          request.Path.ToUriComponent(),
                          request.QueryString.ToUriComponent());

            return absoluteUri;
        }

        /// <summary>
        /// Gets the User-Agent header supplied by the client.
        /// </summary>
        /// <param name="request">The HTTP request whose headers are inspected.</param>
        /// <returns>The User-Agent value, or an empty string when the header is absent.</returns>
        /// <exception cref="NullReferenceException">
        /// Thrown when <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        public static string UserAgent(this HttpRequest request)
        {
            return request.Headers["User-Agent"].ToString();
        }


        /// <summary>
        /// Classifies the request using simple User-Agent substring matching.
        /// Unknown or missing user agents fall back to <see cref="AgentType.Android"/>.
        /// </summary>
        /// <param name="request">The HTTP request whose User-Agent header is inspected.</param>
        /// <returns>The first matching <see cref="AgentType"/> classification.</returns>
        /// <exception cref="NullReferenceException">
        /// Thrown when <paramref name="request"/> is <see langword="null"/>.
        /// </exception>
        public static AgentType UserAgentType(this HttpRequest request)
        {
            var userAgent = request.Headers["User-Agent"].ToString();
            switch (userAgent)
            {
                case string android when android.Contains("MicroMessenger"):
                    return AgentType.Wechat;
                case string android when android.Contains("Android"):
                    return AgentType.Android;
                case string android when android.Contains("iPhone"):
                    return AgentType.IPhone;
                case string android when android.Contains("iPad"):
                    return AgentType.IPad;
                case string android when android.Contains("Windows Phone"):
                    return AgentType.WindowsPhone;
                case string android when android.Contains("Windows NT"):
                    return AgentType.Windows;
                case string android when android.Contains("Mac OS"):
                    return AgentType.MacOS;
            }
            return AgentType.Android;
        }

        /// <summary>
        /// The coarse client-platform classifications recognized by
        /// <see cref="UserAgentType(HttpRequest)"/>.
        /// </summary>
        public enum AgentType
        {
            /// <summary>Android or an unrecognized client.</summary>
            Android = 0,
            /// <summary>Apple iPhone.</summary>
            IPhone = 1,
            /// <summary>Apple iPad.</summary>
            IPad = 2,
            /// <summary>Windows Phone.</summary>
            WindowsPhone = 3,
            /// <summary>Windows desktop.</summary>
            Windows = 4,
            /// <summary>Weixin embedded browser.</summary>
            Wechat = 6,
            /// <summary>macOS desktop.</summary>
            MacOS = 7

        }


        /// <summary>
        /// Gets the remote IP address exposed by the HTTP connection feature.
        /// </summary>
        /// <param name="httpContext">The HTTP context whose connection feature is inspected.</param>
        /// <returns>The remote address, or <see langword="null"/> when the feature is unavailable.</returns>
        /// <exception cref="NullReferenceException">
        /// Thrown when <paramref name="httpContext"/> is <see langword="null"/>.
        /// </exception>
        public static IPAddress UserHostAddress(this HttpContext httpContext)
        {
            return httpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress;
        }
    }

}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SignalrTicker.cs
    File description: SignalrTicker related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.23.13-preview12 Updated service infrastructure and related localization metadata

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Senparc.Ncf.Service.SignalrHubs
{
    /// <summary>
    /// Provides a process-wide ticker that invokes a callback at a configurable interval
    /// for a SignalR hub type.
    /// </summary>
    public class SignalrTicker<THub>
        where THub : Hub
    {
        private static /*readonly*/ SignalrTicker<THub> _instance;

        private readonly IHubContext<THub> m_context;

        /// <summary>
        /// Gets the SignalR hub context used by the ticker.
        /// </summary>
        public IHubContext<THub> GlobalContext
        {
            get { return m_context; }
        }

        /// <summary>
        /// Gets or creates the shared ticker instance for the hub type using the request's
        /// service provider.
        /// </summary>
        /// <param name="httpContext">The HTTP context used to resolve the hub context.</param>
        /// <returns>The shared ticker, or <see langword="null"/> when no context is supplied.</returns>
        public static SignalrTicker<THub> Instance(HttpContext httpContext)
        {
            if (_instance == null)
            {
                if (httpContext!=null)
                {
                    var hubContext = httpContext.RequestServices.GetRequiredService<IHubContext<THub>>();
                    _instance = new SignalrTicker<THub>(hubContext);
                }
            }
            return _instance;
        }

        /// <summary>
        /// Gets or sets the delay between callback invocations, in milliseconds.
        /// </summary>
        public int SleepMillionSeconds { get; set; } = 500;

        /// <summary>
        /// Gets or sets the callback invoked on each ticker iteration, with the ticker and iteration count.
        /// </summary>
        public Action<SignalrTicker<THub>, int> SenderAction { get; set; }

        /// <summary>
        /// Initializes a ticker with the supplied SignalR hub context and starts its background sender loop.
        /// </summary>
        /// <param name="context">The hub context used by consumers of <see cref="GlobalContext"/>.</param>
        private SignalrTicker(IHubContext<THub> context)
        {
            m_context = context;
            Task.Run(() => Sender());
        }

        /// <summary>
        /// Reserved registration hook for compatibility with existing callers.
        /// </summary>
        public static void Register()
        {
        }

        /// <summary>
        /// Runs the ticker loop, invoking <see cref="SenderAction"/> and sleeping for
        /// <see cref="SleepMillionSeconds"/> between iterations.
        /// </summary>
        public void Sender()
        {
            int count = 0;
            while (true)
            {
                SenderAction?.Invoke(this, count);
                count++;
                Thread.Sleep(SleepMillionSeconds);
            }
        }
    }
}

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

  

    File name: SignalrTicker.cs

    File description: SignalrTicker related implementation

    

    

    Created by: Senparc - 20200724

    

    Modified by: Senparc - 20260704

    Change description: vNext: add the standardized file header comment



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
    ///Legacy comment translated to English.
    /// </summary>
    public class SignalrTicker<THub>
        where THub : Hub
    {
        private static /*readonly*/ SignalrTicker<THub> _instance;

        private readonly IHubContext<THub> m_context;

        public IHubContext<THub> GlobalContext
        {
            get { return m_context; }
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
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
        ///Legacy comment translated to English.
        /// </summary>
        public int SleepMillionSeconds { get; set; } = 500;

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public Action<SignalrTicker<THub>, int> SenderAction { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="context"></param>
        private SignalrTicker(IHubContext<THub> context)
        {
            m_context = context;
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            Task.Run(() => Sender());
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static void Register()
        {
            //Legacy comment translated to English.
        }

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

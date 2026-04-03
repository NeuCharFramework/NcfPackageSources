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
    /// Class that records the SignalR global context and can be polled to perform operations
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
        /// static instance object
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
        /// Polling interval (milliseconds), default value: 500
        /// </summary>
        public int SleepMillionSeconds { get; set; } = 500;

        /// <summary>
        /// Parameter: SignalrTicker<THub>, the current number of polls
        /// </summary>
        public Action<SignalrTicker<THub>, int> SenderAction { get; set; }

        /// <summary>
        ///Constructor
        /// </summary>
        /// <param name="context"></param>
        private SignalrTicker(IHubContext<THub> context)
        {
            m_context = context;
            //Sender cannot be called directly here because Sender is an "infinite loop" that does not exit, otherwise the constructor will not exit.  
            //Other processes will no longer be executed. So use an asynchronous approach.  
            Task.Run(() => Sender());
        }

        /// <summary>
        ///Register SignalrTicker, activate static variables
        /// </summary>
        public static void Register()
        {
            //You can write some initialization
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

using NLog;
using Senparc.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Log
{
  public static  class NLogExtension
    {
        /// <summary>
        /// Extension method for recording error information
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="stringFormat"></param>
        /// <param name="args"></param>
        public static void ErrorFormat(this Logger logger, string stringFormat, params object[] args)
        {
            logger.Error(stringFormat.With(args));
        }
    }
}

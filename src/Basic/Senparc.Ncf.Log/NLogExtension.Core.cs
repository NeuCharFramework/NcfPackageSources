/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NLogExtension.Core.cs
    File description: NLogExtension.Core related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

----------------------------------------------------------------*/

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
        ///Legacy comment translated to English.
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

/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NLogExtension.Core.cs
    File description: NLogExtension.Core related implementation

    Created by: Senparc - 20200724

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.20.2-preview2 Updated module behavior, localization metadata, and English source comments

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
        /// Formats the supplied arguments with the Senparc string-format helper
        /// and writes the resulting message at NLog error level.
        /// </summary>
        /// <param name="logger">The NLog logger that receives the error entry.</param>
        /// <param name="stringFormat">The composite format string.</param>
        /// <param name="args">The values used to populate the format string.</param>
        /// <exception cref="NullReferenceException">
        /// Thrown when <paramref name="logger"/> is <see langword="null"/>.
        /// </exception>
        public static void ErrorFormat(this Logger logger, string stringFormat, params object[] args)
        {
            logger.Error(stringFormat.With(args));
        }
    }
}

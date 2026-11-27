/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: AppServiceLogger.cs
    File description: AppServiceLogger related implementation

    Created by: Senparc - 20211014

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.31.1-preview12 Improved core module metadata, localization support, and source comments

    Modified by: Senparc - 20261127
    Change description: v0.31.1.2-preview13 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using Senparc.CO2NET.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Senparc.Ncf.Core.AppServices
{
    /// <summary>
    /// AppService Log Handling
    /// </summary>
    public class AppServiceLogger
    {
        StringBuilder stringBuilder = new StringBuilder();

        /// <summary>Appends a blank log line and returns an empty string.</summary>
        public string Append()
        {
            stringBuilder.AppendLine("");
            return "";
        }

        /// <summary>
        /// Appends a timestamped message to the in-memory log.
        /// </summary>
        /// <param name="msg">The message to append.</param>
        /// <returns>The original message.</returns>
        public string Append(string msg)
        {
            stringBuilder.AppendLine($"[{SystemTime.Now.ToString()}]\t{msg}");
            return msg;
        }

        /// <summary>
        /// Gets all accumulated log text.
        /// </summary>
        /// <returns>The log text including line breaks.</returns>
        public string GetLogs()
        {
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Publishes the accumulated log text through SenparcTrace.
        /// </summary>
        /// <param name="name">The custom-log category.</param>
        public void SaveLogs(string name)
        {
            string logs = GetLogs();

            //Ensure that Chinese characters can be handled
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(logs);
            string utf8String = Encoding.UTF8.GetString(utf8Bytes);

            SenparcTrace.SendCustomLog(name, utf8String);
        }

        /// <summary>Returns the accumulated log text.</summary>
        public override string ToString()
        {
            return GetLogs();
        }
    }
}

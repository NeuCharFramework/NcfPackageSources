/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: SenparcTraceHelper.cs
    File description: Related implementation.

    Created by: Senparc - 20241028

    Modified by: Senparc - 20260813
    Change description: v0.5.0 Updated module behavior and related support.

    Modified by: Senparc - 20261119
    Change description: v0.12.29 Localized administration, chat, and workflow surfaces

----------------------------------------------------------------*/

using Microsoft.Extensions.DependencyInjection;
using Senparc.CO2NET;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Senparc.Areas.Admin.SenparcTraceManager
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class SenparcTraceLogScanResult
    {
        public int TotalLogCount { get; set; }
        public int ExceptionLogCount { get; set; }
        public int NormalLogCount => TotalLogCount - ExceptionLogCount;
        public Dictionary<SenparcTraceType, int> TypeCounts { get; set; } = new Dictionary<SenparcTraceType, int>();
    }

    public static class SenparcTraceHelper
    {
        public static string DefaultLogPath { get; set; } = Path.Combine(Senparc.CO2NET.Config.RootDirectoryPath, "App_Data", "SenparcTraceLog");

        private static readonly Regex ExceptionStartRegex = new Regex(@"(?<=\[{3})(\S+)(?=Exception(\]{3}))", RegexOptions.Compiled);
        private static readonly Regex SegmentStartRegex = new Regex(@"(?<=\[{3})([^\]\n\r]+)(?=\]{3})", RegexOptions.Compiled);
        private static readonly Regex ThreadRegex = new Regex(@"(?<=\[{1}线程：)(\d+)(?=\]{1})", RegexOptions.Compiled);
        private static readonly Regex TimeRegex = new Regex(@"(?<=\[{1})([\s\S]{8,30})(?=\]{1})", RegexOptions.Compiled);

        private const string DayStatCacheKeyPrefix = "Admin:SenparcTrace:Scan:";

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static List<string> GetLogDate()
        {
            if (!Directory.Exists(DefaultLogPath))
            {
                return new List<string>();
            }

            var files = Directory.GetFiles(DefaultLogPath, "SenparcTrace-*.log");
            return files
                .Select(z => Path.GetFileNameWithoutExtension(z).Replace("SenparcTrace-", ""))
                .Where(z => !string.IsNullOrEmpty(z))
                .OrderByDescending(z => z)
                .ToList();
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static async Task<SenparcTraceLogScanResult> GetLogScanResultAsync(
            IServiceProvider serviceProvider,
            string date,
            bool useCache = true)
        {
            var today = SystemTime.Now.ToString("yyyyMMdd");
            var cacheKey = DayStatCacheKeyPrefix + date;
            var cache = serviceProvider?.GetService<IBaseObjectCacheStrategy>();

            if (useCache && cache != null)
            {
                var cached = await cache.GetAsync<SenparcTraceLogScanResult>(cacheKey);
                if (cached != null)
                {
                    return cached;
                }
            }

            var logFile = GetLogFilePath(date);
            if (!File.Exists(logFile))
            {
                throw new Exception("微信日志文件不存在：" + logFile);
            }

            var result = await ScanLogFileAsync(logFile);

            if (useCache && cache != null)
            {
                //Legacy comment translated to English.
                var ttl = string.Equals(date, today, StringComparison.Ordinal)
                    ? TimeSpan.FromSeconds(30)
                    : TimeSpan.FromHours(6);
                await cache.SetAsync(cacheKey, result, ttl);
            }

            return result;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        public static async Task<List<SenparcTraceItem>> GetAllLogsAsync(IServiceProvider serviceProvider, string date)
        {
            var logFile = GetLogFilePath(date);

            if (!File.Exists(logFile))
            {
                throw new Exception("微信日志文件不存在：" + logFile);
            }

            var logList = new List<SenparcTraceItem>();
            var cache = serviceProvider.GetService<IBaseObjectCacheStrategy>();

            using (var cacheLock = await cache.BeginCacheLockAsync("GetAllLogsAsync", logFile, 100, TimeSpan.FromMilliseconds(100)))
            {
                await using (var readerOwner = await OpenLogReaderAsync(logFile))
                {
                    var sr = readerOwner.Reader;
                    string lineText = null;
                    int line = 0;
                    var readPostData = false;
                    var readResult = false;
                    var readExceptionStackTrace = false;

                    SenparcTraceItem log = new SenparcTraceItem();
                    while ((lineText = await sr.ReadLineAsync()) != null)
                    {
                        line++;
                        lineText = NormalizeLine(lineText);

                        var startExceptionRegex = ExceptionStartRegex.Match(lineText);
                        if (startExceptionRegex.Success)
                        {
                            log = new SenparcTraceItem();
                            logList.Add(log);
                            log.Title = "【{0}Exception】异常！".FormatWith(startExceptionRegex.Value);
                            log.Line = line;
                            log.IsException = true;
                            log.SenparcTraceType = SenparcTraceType.Exception;

                            readPostData = false;
                            readResult = false;
                            readExceptionStackTrace = false;
                            continue;
                        }

                        var startRegex = SegmentStartRegex.Match(lineText);
                        if (startRegex.Success)
                        {
                            log = new SenparcTraceItem();
                            logList.Add(log);
                            log.Title = startRegex.Value;
                            log.Line = line;

                            readPostData = false;
                            readResult = false;
                            readExceptionStackTrace = false;
                            continue;
                        }

                        var threadRegex = ThreadRegex.Match(lineText);
                        if (threadRegex.Success)
                        {
                            log.ThreadId = int.Parse(threadRegex.Value);
                            continue;
                        }

                        var timeRegex = TimeRegex.Match(lineText);
                        if (timeRegex.Success && string.IsNullOrEmpty(log.DateTime))
                        {
                            log.DateTime = timeRegex.Value;
                            continue;
                        }

                        log.Result.TotalResult += lineText + Environment.NewLine;

                        if (readPostData)
                        {
                            log.Result.PostData += lineText + Environment.NewLine;
                            continue;
                        }

                        if (lineText.StartsWith("URL："))
                        {
                            log.Result.Url = lineText.Replace("URL：", "");

                            if (SenparcTraceType.Normal == log.SenparcTraceType)
                            {
                                log.SenparcTraceType = SenparcTraceType.API;
                            }
                        }
                        else if (lineText == "Post Data：")
                        {
                            log.SenparcTraceType = SenparcTraceType.PostRequest;
                            readPostData = true;
                        }
                        else if (lineText == "Result：" || readResult)
                        {
                            log.Result.Result += lineText.Replace("Result：", "") + "\r\n";
                            readResult = true;

                            if (SenparcTraceType.PostRequest != log.SenparcTraceType)
                            {
                                log.SenparcTraceType = SenparcTraceType.GetRequest;
                            }
                        }

                        if (log.IsException)
                        {
                            if (lineText.StartsWith("AccessTokenOrAppId："))
                            {
                                log.Result.ExceptionAccessTokenOrAppId = lineText.Replace("AccessTokenOrAppId：", "");
                            }
                            else if (lineText.StartsWith("Message：") || lineText.StartsWith("errcode："))
                            {
                                log.Result.ExceptionMessage = lineText.Replace("Message：", "");
                            }
                            else if (lineText.StartsWith("StackTrace："))
                            {
                                log.Result.ExceptionStackTrace = lineText.Replace("StackTrace：", "");
                                readExceptionStackTrace = true;
                            }
                            else if (readExceptionStackTrace)
                            {
                                log.Result.ExceptionStackTrace = "\r\n" + lineText;
                            }
                        }
                    }
                }
            }

            logList.Reverse();
            return logList;
        }

        private static string GetLogFilePath(string date)
        {
            return Path.Combine(DefaultLogPath, string.Format("SenparcTrace-{0}.log", date));
        }

        private static string NormalizeLine(string lineText)
        {
            return lineText == null ? string.Empty : lineText.Trim().TrimStart('\uFEFF');
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private static async Task<SenparcTraceLogScanResult> ScanLogFileAsync(string logFile)
        {
            var result = new SenparcTraceLogScanResult();
            var hasCurrent = false;
            var currentType = SenparcTraceType.Normal;
            var readPostData = false;
            var readResult = false;

            void CommitCurrent()
            {
                if (!hasCurrent)
                {
                    return;
                }

                result.TotalLogCount++;
                if (currentType == SenparcTraceType.Exception)
                {
                    result.ExceptionLogCount++;
                }

                if (!result.TypeCounts.ContainsKey(currentType))
                {
                    result.TypeCounts[currentType] = 0;
                }
                result.TypeCounts[currentType]++;
                hasCurrent = false;
            }

            await using (var readerOwner = await OpenLogReaderAsync(logFile))
            {
                var sr = readerOwner.Reader;
                string lineText;
                while ((lineText = await sr.ReadLineAsync()) != null)
                {
                    lineText = NormalizeLine(lineText);
                    if (lineText.Length == 0)
                    {
                        continue;
                    }

                    var exceptionMatch = ExceptionStartRegex.Match(lineText);
                    if (exceptionMatch.Success)
                    {
                        CommitCurrent();
                        hasCurrent = true;
                        currentType = SenparcTraceType.Exception;
                        readPostData = false;
                        readResult = false;
                        continue;
                    }

                    var startMatch = SegmentStartRegex.Match(lineText);
                    if (startMatch.Success)
                    {
                        CommitCurrent();
                        hasCurrent = true;
                        currentType = SenparcTraceType.Normal;
                        readPostData = false;
                        readResult = false;
                        continue;
                    }

                    if (!hasCurrent)
                    {
                        continue;
                    }

                    if (readPostData)
                    {
                        continue;
                    }

                    if (lineText.StartsWith("URL："))
                    {
                        if (currentType == SenparcTraceType.Normal)
                        {
                            currentType = SenparcTraceType.API;
                        }
                    }
                    else if (lineText == "Post Data：")
                    {
                        currentType = SenparcTraceType.PostRequest;
                        readPostData = true;
                    }
                    else if (lineText == "Result：" || readResult)
                    {
                        readResult = true;
                        if (currentType != SenparcTraceType.PostRequest)
                        {
                            currentType = SenparcTraceType.GetRequest;
                        }
                    }
                }
            }

            CommitCurrent();
            return result;
        }

        /// <summary>
        ///Legacy comment translated to English.
        /// </summary>
        private static async Task<LogReaderOwner> OpenLogReaderAsync(string logFile)
        {
            try
            {
                var stream = new FileStream(
                    logFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    bufferSize: 64 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan);

                return new LogReaderOwner(new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true), bakFilePath: null);
            }
            catch (IOException)
            {
                //Legacy comment translated to English.
                var bakFilename = logFile + ".bak";
                await Task.Run(() =>
                {
                    if (File.Exists(bakFilename))
                    {
                        File.Delete(bakFilename);
                    }
                    File.Copy(logFile, bakFilename, true);
                });

                var bakStream = new FileStream(
                    bakFilename,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 64 * 1024,
                    options: FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);

                return new LogReaderOwner(new StreamReader(bakStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true), bakFilename);
            }
        }

        private sealed class LogReaderOwner : IAsyncDisposable, IDisposable
        {
            private readonly string _bakFilePath;
            private bool _disposed;

            public StreamReader Reader { get; }

            public LogReaderOwner(StreamReader reader, string bakFilePath)
            {
                Reader = reader;
                _bakFilePath = bakFilePath;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                Reader.Dispose();
                TryDeleteBak();
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            private void TryDeleteBak()
            {
                if (string.IsNullOrEmpty(_bakFilePath))
                {
                    return;
                }

                try
                {
                    //Legacy comment translated to English.
                    if (File.Exists(_bakFilePath))
                    {
                        File.Delete(_bakFilePath);
                    }
                }
                catch
                {
                    //Legacy comment translated to English.
                }
            }
        }
    }
}

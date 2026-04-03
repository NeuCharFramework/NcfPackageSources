using Microsoft.Extensions.DependencyInjection;
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
    public static class SenparcTraceHelper
    {
        public static string DefaultLogPath { get; set; } = Path.Combine(Senparc.CO2NET.Config.RootDirectoryPath, "App_Data", "SenparcTraceLog");// Path.Combine(Senparc.CO2NET.Config.RootDictionaryPath, "App_Data", "SenparcTraceLog");

        /// <summary>
        /// Get a list of all dates
        /// </summary>
        /// <returns></returns>
        public static List<string> GetLogDate()
        {
            var files = System.IO.Directory.GetFiles(DefaultLogPath, "*.log");
            return files.Select(z => Path.GetFileNameWithoutExtension(z).Replace("SenparcTrace-", "")).OrderByDescending(z => z).ToList();
        }

        /// <summary>
        /// Get logs for a specified date
        /// </summary>
        /// <returns></returns>
        public static async Task<List<SenparcTraceItem>> GetAllLogsAsync(IServiceProvider serviceProvider, string date)
        {
            var logFile = Path.Combine(DefaultLogPath, string.Format("SenparcTrace-{0}.log", date));

            if (!File.Exists(logFile))
            {
                throw new Exception("微信日志文件不存在：" + logFile);
            }

            var logList = new List<SenparcTraceItem>();
            var cache = serviceProvider.GetService<IBaseObjectCacheStrategy>();

            using (var cacheLock = await cache.BeginCacheLockAsync("GetAllLogsAsync", logFile, 100, TimeSpan.FromMilliseconds(100)))
            {
                string bakFilename = logFile + ".bak";//Backup file name
                System.IO.File.Delete(bakFilename);
                System.IO.File.Copy(logFile, bakFilename, true);//Read backup files to avoid resource usage

                using (StreamReader sr = new StreamReader(bakFilename, Encoding.UTF8))
                {
                    string lineText = null;
                    int line = 0;
                    var readPostData = false;
                    var readResult = false;
                    var readExceptionStackTrace = false;

                    SenparcTraceItem log = new SenparcTraceItem();
                    while ((lineText = await sr.ReadLineAsync()) != null)
                    {
                        line++;

                        lineText = lineText.Trim();


                        var startExceptionRegex = Regex.Match(lineText, @"(?<=\[{3})(\S+)(?=Exception(\]{3}))");

                        if (startExceptionRegex.Success)
                        {
                            //Beginning of a fragment (Exception)
                            log = new SenparcTraceItem();
                            logList.Add(log);
                            log.Title = "【{0}Exception】异常！".FormatWith(startExceptionRegex.Value);//record title
                            log.Line = line;
                            log.IsException = true;
                            log.SenparcTraceType = SenparcTraceType.Exception;

                            readPostData = false;
                            readResult = false;
                            readExceptionStackTrace = false;
                            continue;
                        }

                        //Other custom types
                        var startRegex = Regex.Match(lineText, @"(?<=\[{3})([^\]\n\r]+)(?=\]{3})");
                        if (startRegex.Success)
                        {
                            //start of a segment
                            log = new SenparcTraceItem();
                            logList.Add(log);
                            log.Title = startRegex.Value;//record title
                            log.Line = line;

                            readPostData = false;
                            readResult = false;
                            readExceptionStackTrace = false;
                            continue;
                        }


                        var threadRegex = Regex.Match(lineText, @"(?<=\[{1}线程：)(\d+)(?=\]{1})");
                        if (threadRegex.Success)
                        {
                            //thread
                            log.ThreadId = int.Parse(threadRegex.Value);
                            continue;
                        }

                        var timeRegex = Regex.Match(lineText, @"(?<=\[{1})([\s\S]{8,30})(?=\]{1})");
                        if (timeRegex.Success && string.IsNullOrEmpty(log.DateTime))
                        {
                            //time
                            log.DateTime = timeRegex.Value;
                            continue;
                        }


                        //content
                        log.Result.TotalResult += lineText + Environment.NewLine;

                        if (readPostData)
                        {
                            log.Result.PostData += lineText + Environment.NewLine;
                            continue;//Read to the end
                        }


                        if (lineText.StartsWith("URL："))
                        {
                            log.Result.Url = lineText.Replace("URL：", "");

                            if (SenparcTraceType.Normal == log.SenparcTraceType)
                            {
                                log.SenparcTraceType = SenparcTraceType.API;
                            }
                            //log.weixinTraceType = log.weixinTraceType | WeixinTraceType.API;
                        }
                        else if (lineText == "Post Data：")
                        {
                            log.SenparcTraceType = SenparcTraceType.PostRequest;//POST request
                            readPostData = true;
                        }
                        else if (lineText == "Result：" || readResult)
                        {
                            log.Result.Result += lineText.Replace("Result：", "") + "\r\n";
                            readResult = true;

                            if (SenparcTraceType.PostRequest != log.SenparcTraceType)
                            {
                                log.SenparcTraceType = SenparcTraceType.GetRequest;//GET request
                            }
                        }

                        if (log.IsException)
                        {
                            //Exception information processing
                            if (lineText.StartsWith("AccessTokenOrAppId："))
                            {
                                log.Result.ExceptionAccessTokenOrAppId = lineText.Replace("AccessTokenOrAppId：", "");
                            }
                            else if (lineText.StartsWith("Message：") || lineText.StartsWith("errcode："))
                            {
                                log.Result.ExceptionMessage = lineText.Replace("Message：", "");//"errcode:" reserved
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

                System.IO.File.Delete(bakFilename);//Delete backup files
            }

            logList.Reverse();//flip sequence
            return logList;
        }
    }
}

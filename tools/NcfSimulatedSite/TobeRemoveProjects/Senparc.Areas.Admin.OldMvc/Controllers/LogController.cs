/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: LogController.cs
    File description: LogController related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc;
using Senparc.Areas.Admin.Models.VD;
using Senparc.Ncf.Core.Extensions;
using Senparc.Ncf.Core.Models;
using Senparc.Core.Utility;
using Senparc.Mvc.Filter;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Senparc.CO2NET.Utilities;

namespace Senparc.Areas.Admin.Controllers
{
    [MenuFilter("Log")]
    public class LogController : BaseAdminController
    {
        private const string ROLE_MODULE_NAME = "系统日志";

        public ActionResult Index()
        {
            return RedirectToAction("WebLogList");
        }

        public ActionResult WebLogList([DefaultValue(1)]int pageIndex)
        {
            int pageCount = 31;
            int skipRecord = Senparc.Ncf.Core.Utility.Extensions.GetSkipRecord(pageIndex, pageCount);
            string logFileDir = ServerUtility.ContentRootMapPath("~/App_Data/Log/");
            var dateDirs = Directory.GetDirectories(logFileDir, "Logs_*", SearchOption.TopDirectoryOnly);

            Log_WebLogListVD vd = new Log_WebLogListVD()
            {
                DateList = new PagedList<string>(
                    dateDirs.OrderByDescending(z => z)
                            .Select(z => Path.GetFileName(z).Split('_')[1])
                            .Skip(skipRecord)
                            .Take(pageCount).ToList(),
                        pageIndex, pageCount, dateDirs.Length, skipRecord)
            };
            return View(vd);
        }

        public ActionResult WebLogDate(string date, [DefaultValue(1)]int pageIndex)
        {
            if (!date.Contains("-") && !date.Contains("/"))//20100102
            {
                date = $"{date.Substring(0, 4)}-{date.Substring(4, 2)}-{date.Substring(6, 2)}";
            }
            DateTime logDate = Convert.ToDateTime(date);
            string logFileDir = ServerUtility.ContentRootMapPath($"~/App_Data/Log/Logs_{logDate:yyyyMMdd}/");

            if (!Directory.Exists(logFileDir))
            {
                return RenderError($"{date}日志不存在");
            }

            List<string> filePathNames = Directory.GetFiles(logFileDir).OrderBy(z => z).ToList();
            List<WebLog> logList = new List<WebLog>();
            int fileStartIndex = pageIndex > 0 ? pageIndex - 1 : 0;//Legacy comment translated to English.
            int fileEndIndex = pageIndex > 0 ? pageIndex : filePathNames.Count;//Legacy comment translated to English.

            for (int i = fileStartIndex; i < fileEndIndex; i++)
            {
                string filePathName = filePathNames[i];
                var insertListIndex = logList.Count;
                string newFilename = filePathName + ".bak";
                System.IO.File.Delete(newFilename);
                System.IO.File.Copy(filePathName, newFilename, true);//Legacy comment translated to English.

                using (StreamReader sr = new StreamReader(newFilename, Encoding.Default))
                {
                    string lineText = null;
                    int line = 0;
                    //Legacy comment translated to English.
                    WebLog log = new WebLog();
                    while ((lineText = sr.ReadLine()) != null)
                    {
                        line++;
                        if (lineText.StartsWith("-------") /* || (!inOneLog && line.IsNullOrEmpty())*/)//Legacy comment translated to English.
                        {
                            continue;
                        }

                        if (lineText.StartsWith(logDate.ToString("yyyy-MM-dd")))
                        {
                            logList.Insert(insertListIndex, log);
                            //Legacy comment translated to English.
                            string[] data = lineText.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries);
                            log = new WebLog()
                            {
                                PageIndex = i,
                                Line = line,
                                DateTime = DateTime.Parse(data[0] + " " + data[1]),
                                ThreadName = data[3],
                                Level = data[4],
                                LoggerName = data[5],
                                Message = lineText.Substring(lineText.IndexOf(" - ") + 3, lineText.Length - lineText.IndexOf(" - ") - 3)//Legacy comment translated to English.
                            };
                            //Legacy comment translated to English.
                        }
                        //else if (!log.Level.IsNullOrEmpty())
                        //{
                        //Legacy comment translated to English.
                        //}
                    }
                    logList.Insert(insertListIndex, log);
                }
                System.IO.File.Delete(newFilename);
            }

            Log_WebLogDateVD vd = new Log_WebLogDateVD
            {
                TotalPageCount = filePathNames.Count,
                PageIndex = pageIndex,
                CurrentDate = logDate,
                LogList = logList
            };
            return View(vd);
        }

        public ActionResult WebLogDetail(string date, int line, [DefaultValue(1)]int pageIndex)
        {
            bool success = true;
            string msg = null;

            if (!date.Contains("-") && !date.Contains("/"))//20100102
            {
                date = $"{date.Substring(0, 4)}-{date.Substring(4, 2)}-{date.Substring(6, 2)}";
            }
            DateTime logDate = Convert.ToDateTime(date);
            string logFileDir = ServerUtility.ContentRootMapPath($"~/App_Data/Log/Logs_{logDate:yyyyMMdd}/");
            WebLog log = new WebLog();
            if (!Directory.Exists(logFileDir))
            {
                success = false;
                msg = $"{date}日志不存在";
            }
            else
            {
                List<string> filePathNames = Directory.GetFiles(logFileDir).OrderBy(z => z).ToList();
                string filePathName = filePathNames[pageIndex];

                string newFilename = filePathName + ".bak";
                System.IO.File.Delete(newFilename);
                System.IO.File.Copy(filePathName, newFilename, true);//Legacy comment translated to English.
                using (StreamReader sr = new StreamReader(newFilename, Encoding.Default))
                {
                    string lineText = null;
                    int currentLine = 0;
                    bool recordStarted = false;

                    while ((lineText = sr.ReadLine()) != null)
                    {
                        currentLine++;
                        if (recordStarted &&
                            (lineText.StartsWith(logDate.ToString("yyyy-MM-dd")) || lineText.StartsWith("-------")))
                        {
                            break;
                        }

                        if (currentLine == line)
                        {
                            string[] data = lineText.Split(new[] { " ", "," }, StringSplitOptions.RemoveEmptyEntries);
                            log = new WebLog()
                            {
                                PageIndex = line,
                                Line = line,
                                DateTime = DateTime.Parse(data[0] + " " + data[1]),
                                ThreadName = data[3],
                                Level = data[4],
                                LoggerName = data[5],
                                Message = lineText.Substring(lineText.IndexOf(" - ") + 3, lineText.Length - lineText.IndexOf(" - ") - 3)//Legacy comment translated to English.
                            };
                            recordStarted = true;
                        }
                        else if (recordStarted)
                        {
                            //startrecordcontent
                            log.Details += lineText + "<br />";
                        }
                    }
                }
                System.IO.File.Delete(newFilename);
                success = true;
                msg = log.Message;
            }
            return Json(new { success = success, msg = msg, date = log.DateTime.ToString(), detail = log.Details });
        }
    }
}

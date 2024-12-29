using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Senparc.Ncf.UnitTestExtension
{
    public static class UnitTestHelper
    {
        /// <summary>
        /// 根目录地址
        /// </summary>
        public static string RootPath => Path.GetFullPath("..\\..\\..\\");


        /// <summary>
        /// 检查关键字存在
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="keywords"></param>
        /// <returns></returns>
        public static bool CheckKeywordsExist(string filePath, params string[] keywords)
        {
            using (var fs = new FileStream(filePath, FileMode.Open))
            {
                using (var sr = new StreamReader(fs))
                {
                    var content = sr.ReadToEnd();
                    foreach (var item in keywords)
                    {
                        if (!content.Contains(item))
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// Get AppSettings file name.
        /// </summary>
        /// <returns></returns>
        public static string GetAppSettingsFile(IHostEnvironment env=null)
        {
            var envStr = env?.EnvironmentName ?? "Test";
            var appsettingFile = $"appsettings.{envStr}.json";
            if (File.Exists(appsettingFile))
            {
                Console.WriteLine($"use {appsettingFile}");
                return appsettingFile;
            }

            Console.WriteLine("use appsettings.json");
            return "appsettings.json";
        }

        public static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> dataList) where T : class
        {
            var queryable = dataList.AsQueryable();

            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

            return mockSet;
        }
    }
}

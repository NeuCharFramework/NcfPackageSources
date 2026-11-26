/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: UnitTestHelper.cs
    File description: UnitTestHelper related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20261119
    Change description: v0.3.29 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

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
    /// <summary>
    /// Provides file, configuration, and Entity Framework test-double helpers.
    /// </summary>
    public static class UnitTestHelper
    {
        /// <summary>
        /// Gets the repository-relative root path used by the test host.
        /// </summary>
        public static string RootPath => Path.GetFullPath("..\\..\\..\\");


        /// <summary>
        /// Determines whether a file contains every supplied literal keyword.
        /// </summary>
        /// <param name="filePath">The file to read.</param>
        /// <param name="keywords">The case-sensitive substrings that must be present.</param>
        /// <returns>
        /// <see langword="true"/> when all keywords are found; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// Thrown when <paramref name="filePath"/> does not identify a file.
        /// </exception>
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
        /// Selects the environment-specific appsettings file when it exists,
        /// otherwise falling back to <c>appsettings.json</c>.
        /// </summary>
        /// <param name="env">
        /// Optional host environment whose name selects the file suffix. When
        /// omitted, the <c>Test</c> environment name is used.
        /// </param>
        /// <returns>The selected appsettings file name.</returns>
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

        /// <summary>
        /// Creates a Moq <see cref="DbSet{TEntity}"/> that exposes the supplied
        /// in-memory list through the standard <see cref="IQueryable{T}"/> members.
        /// </summary>
        /// <typeparam name="T">The entity type represented by the mock set.</typeparam>
        /// <param name="dataList">The in-memory source list used by the query provider.</param>
        /// <returns>A mocked entity set backed by <paramref name="dataList"/>.</returns>
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

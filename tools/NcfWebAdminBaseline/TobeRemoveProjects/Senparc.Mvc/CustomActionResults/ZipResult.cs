/*----------------------------------------------------------------
    Copyright (C) 2026 Senparc

    File name: ZipResult.cs
    File description: ZipResult related implementation

    Created by: Senparc - 20241028

    Modified by: Senparc - 20261119
    Change description: v0.0.1 Updated legacy MVC integration metadata and English source comments

    Modified by: Senparc - 20261127
    Change description: v0.0.2 Improve source documentation, headers, and module metadata consistency.

----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Senparc.Ncf.Utility.StreamExtensions;
using System.Net.Http;

namespace Senparc.Mvc.CustomActionResults
{
    /// <summary>
    ///Legacy comment translated to English.
    /// </summary>
    public class ZipResult : FileResult
    {
        //Legacy comment translated to English.
        private static HttpClient Client { get; } = new HttpClient();

        private Func<Stream, ActionContext, Task> _callback;

        public ZipResult(Microsoft.Net.Http.Headers.MediaTypeHeaderValue contentType, string zipFileName,
            IEnumerable<string> filenames,
             //Dictionary<string, string> files,
            Dictionary<string, string> newFilesFromString,
            Func<Stream, ActionContext, Task> callback)
            : base((contentType ?? new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream")).ToString())
        {
            if (callback == null)
            {
                callback = async (outputStream, _) =>
                {
                    using (var zipArchive = new ZipArchive(new WriteOnlyStreamWrapper(outputStream), ZipArchiveMode.Create))
                    {
                        Dictionary<string, string> files = new Dictionary<string, string>();
                        foreach (var item in filenames)
                        {
                            files[Path.GetFileName(item)] = item;
                        }
                        foreach (var item in newFilesFromString)
                        {
                            //Legacy comment translated to English.
                        }

                        foreach (var kvp in files)
                        {
                            var zipEntry = zipArchive.CreateEntry(kvp.Key);
                            using (var zipStream = zipEntry.Open())
                            {
                                //Legacy comment translated to English.
                                using (var stream = await Client.GetStreamAsync(kvp.Value))
                                {
                                    await stream.CopyToAsync(zipStream);
                                }
                            }
                        }
                    }
                };
            }
            //throw new ArgumentNullException(nameof(callback));

            _callback = callback;
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));


            var executor = new FileCallbackResultExecutor(context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>());
            return executor.ExecuteAsync(context, this);
        }

        private sealed class FileCallbackResultExecutor : FileResultExecutorBase
        {
            public FileCallbackResultExecutor(ILoggerFactory loggerFactory)
                : base(CreateLogger<FileCallbackResultExecutor>(loggerFactory))
            {
            }

            /// <summary>
            /// Applies file-result headers and invokes the ZIP response callback
            /// against the current response body.
            /// </summary>
            /// <param name="context">The MVC action context.</param>
            /// <param name="result">The ZIP result being executed.</param>
            /// <returns>A task that completes after the callback is invoked.</returns>
            public Task ExecuteAsync(ActionContext context, ZipResult result)
            {
                SetHeadersAndLog(context, result,
                    //Legacy comment translated to English.
                    context.HttpContext.Response.Body.Length,
                    true);
                return result._callback(context.HttpContext.Response.Body, context);
            }
        }
    }
}

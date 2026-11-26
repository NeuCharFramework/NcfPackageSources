/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: NameSpaceAppService.cs
    File description: NameSpaceAppService related implementation

    Created by: Senparc - 20260704

    Modified by: Senparc - 20260704
    Change description: vNext: add the standardized file header comment

    Modified by: Senparc - 20260717
    Change description: v0.22.0-preview2 integrate shared resource localization and improve feature text for the ChangeNamespace module

    Modified by: Senparc - 20261119
    Change description: v0.22.14 Updated module behavior, localization metadata, and English source comments

----------------------------------------------------------------*/

using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.ChangeNamespace.OHS.PL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.ChangeNamespace.OHS.Local
{
    public class NameSpaceAppService : AppServiceBase
    {
        /// <summary>
        /// Initializes the namespace transformation application service.
        /// </summary>
        /// <param name="serviceProvider">The dependency provider used by the base service.</param>
        public NameSpaceAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        #region Change

        /// <summary>
        /// Describes one namespace replacement discovered while scanning a source file.
        /// </summary>
        public class MatchNamespace
        {
            /// <summary>
            /// Gets or sets the source syntax prefix associated with the match.
            /// </summary>
            public string Prefix { get; set; }

            /// <summary>
            /// Gets or sets the complete matched text before replacement.
            /// </summary>
            public string OldNamespace { get; set; }

            /// <summary>
            /// Gets or sets the complete replacement text.
            /// </summary>
            public string NewNamespace { get; set; }
        }


        /// <summary>
        /// Recursively scans supported source files below a directory and replaces matching
        /// namespace, Razor model, and tag-helper references with the requested namespace.
        /// </summary>
        /// <param name="request">The source path, old namespace keyword, and replacement namespace.</param>
        /// <returns>A response containing the scan and replacement log.</returns>
        /// <exception cref="DirectoryNotFoundException">Reported as an unsuccessful response when the path does not exist.</exception>
        [FunctionRender(typeof(ChangeNamespaceResource), "Function.ChangeNamespace.Change.Name", "Function.ChangeNamespace.Change.Description", typeof(Register))]
        public async Task<StringAppResponse> Change(NameSpace_ChangeRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                logger.Append("开始运行 ChangeNamespace");

                var path = request.Path;
                var newNamespace = request.NewNamespace;
                var OldNamespaceKeyword = request.OldNamespaceKeyword;

                if (!Directory.Exists(path))
                {
                    logger.Append($"path:{path} not exist");
                    response.Success = false;
                    return logger.Append("路径不存在！");
                }

                logger.Append($"path:{path} newNamespace:{newNamespace}");

                var meetRules = new List<MeetRule>() {
                new MeetRule("namespace",OldNamespaceKeyword,$"{newNamespace}","*.cs"),
                new MeetRule("@model",OldNamespaceKeyword,$"{newNamespace}","*.cshtml"),
                new MeetRule("@addTagHelper *,",OldNamespaceKeyword,$"{newNamespace}","*.cshtml"),
            };

                //Legacy comment translated to English.

                Dictionary<string, List<MatchNamespace>> namespaceCollection = new Dictionary<string, List<MatchNamespace>>(StringComparer.OrdinalIgnoreCase);

                //Legacy comment translated to English.
                foreach (var item in meetRules)
                {
                    var files = Directory.GetFiles(path, item.FileType, SearchOption.AllDirectories);

                    //Legacy comment translated to English.
                    foreach (var file in files)
                    {
                        logger.Append($"扫描文件类型:{item.FileType} 数量:{files.Length}");

                        //string content = null;
                        using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            using (var sr = new StreamReader(fs))
                            {
                                var line = sr.ReadLine();
                                while (null != line)
                                {
                                    line = sr.ReadLine()?.Trim();
                                    var oldNamespaceFull = $"{item.Prefix} {item.OrignalKeyword}";
                                    if (line != null && line.StartsWith(oldNamespaceFull))
                                    {
                                        if (!namespaceCollection.ContainsKey(file))
                                        {
                                            namespaceCollection[file] = new List<MatchNamespace>();
                                        }

                                        //Legacy comment translated to English.
                                        //var oldNamespaceArr = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                        var getOld = line.Replace(item.Prefix + " ", "");//Legacy comment translated to English.
                                        var getNew = getOld.Replace(item.OrignalKeyword, item.ReplaceWord);
                                        namespaceCollection[file].Add(new MatchNamespace()
                                        {
                                            Prefix = item.Prefix,//prefix
                                            OldNamespace = getOld,
                                            NewNamespace = getNew
                                        });

                                        namespaceCollection[file].Add(new MatchNamespace()
                                        {
                                            Prefix = "using",
                                            OldNamespace = getOld,
                                            NewNamespace = getNew
                                        });
                                    }

                                    //content += Environment.NewLine + line;
                                }
                                sr.ReadLine();
                            }
                            fs.Close();
                        }
                    }

                    //Legacy comment translated to English.
                    foreach (var file in files)
                    {
                        string content = null;
                        using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            using (var sr = new StreamReader(fs))
                            {
                                content = sr.ReadToEnd();
                            }
                            fs.Close();
                        }

                        foreach (var namespaceInfos in namespaceCollection)
                        {
                            foreach (var namespaceInfo in namespaceInfos.Value)
                            {
                                var oldNamespaceFull = $"{namespaceInfo.Prefix} {namespaceInfo.OldNamespace}";

                                //Legacy comment translated to English.
                                if (content.IndexOf(oldNamespaceFull) > -1)
                                {
                                    logger.Append($"文件命中:{file} -> {oldNamespaceFull}");
                                    var newNameSpaceFull = $"{namespaceInfo.Prefix} {namespaceInfo.NewNamespace}";
                                    content = content.Replace(oldNamespaceFull, newNameSpaceFull);
                                }
                            }
                        }

                        using (var fs = new FileStream(file, FileMode.Truncate, FileAccess.ReadWrite))
                        {
                            using (var sw = new StreamWriter(fs))
                            {
                                sw.Write(content);
                                sw.Flush();
                            }
                            fs.Close();
                        }
                    }

                }

                return logger.Append("更新成功！您还可以使用【还原命名空间】功能进行还原！");

            }, saveLogAfterFinished: true);
        }

        #endregion

        /// <summary>
        /// Restores a previously transformed namespace by applying the inverse namespace operation.
        /// </summary>
        /// <param name="request">The source path and module namespace to restore.</param>
        /// <returns>A response containing the restore operation result.</returns>
        [FunctionRender(typeof(ChangeNamespaceResource), "Function.ChangeNamespace.Restore.Name", "Function.ChangeNamespace.Restore.Description", typeof(Register))]
        public async Task<StringAppResponse> Restore(NameSpace_RestoreRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                var changeNamespaceParam = new NameSpace_ChangeRequest()
                {
                    NewNamespace = "Senparc.",
                    Path = request.Path
                };

                changeNamespaceParam.OldNamespaceKeyword = request.MyNamespace;
                var newesult = await this.Change(changeNamespaceParam);
                if (newesult.Success == true)
                {
                    response.Data = "还原命名空间成功！";
                }
                return null;
            });
        }


        /// <summary>
        /// Returns the official archive URL for the selected source hosting site.
        /// </summary>
        /// <param name="request">The requested source hosting site.</param>
        /// <returns>A response containing the archive URL or an unsuccessful diagnostic.</returns>
        [FunctionRender(typeof(ChangeNamespaceResource), "Function.ChangeNamespace.Download.Name", "Function.ChangeNamespace.Download.Description", typeof(Register))]
        public async Task<StringAppResponse> DownloadSourceCode(NameSpace_DownloadSourceCodeRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                if (Enum.TryParse<NameSpace_DownloadSourceCodeRequest.Parameters_Site>(request.Site, out var siteType))
                {
                    switch (siteType)
                    {
                        case NameSpace_DownloadSourceCodeRequest.Parameters_Site.GitHub:
                            response.Data = "https://github.com/NeuCharFramework/NCF/archive/master.zip";
                            break;
                        case NameSpace_DownloadSourceCodeRequest.Parameters_Site.Gitee:
                            response.Data = "https://gitee.com/NeuCharFramework/NCF/repository/archive/master.zip";
                            break;
                        default:
                            response.Data = "未知的下载地址";
                            response.Success = false;
                            break;
                    }
                }
                else
                {
                    response.Data = "未知的下载参数";
                    response.Success = false;
                }
                return null;
            });
        }

    }
}

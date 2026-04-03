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
        public NameSpaceAppService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        #region Change

        public class MatchNamespace
        {
            public string Prefix { get; set; }
            public string OldNamespace { get; set; }
            public string NewNamespace { get; set; }
        }


        [FunctionRender("修改命名空间", "修改所有源码在 .cs, .cshtml 中的命名空间", typeof(Register))]
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

                //TODO:Use regular records and modify them globally

                Dictionary<string, List<MatchNamespace>> namespaceCollection = new Dictionary<string, List<MatchNamespace>>(StringComparer.OrdinalIgnoreCase);

                //Scan all rules
                foreach (var item in meetRules)
                {
                    var files = Directory.GetFiles(path, item.FileType, SearchOption.AllDirectories);

                    //Scan all files and record the objects that meet the replacement conditions of this rule.
                    foreach (var file in files)
                    {
                        logger.Append($"Scan file types:{item.FileType} quantity:{files.Length}");

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

                                        //Split cannot be used, there may be spaces in the middle
                                        //var oldNamespaceArr = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                        var getOld = line.Replace(item.Prefix + " ", "");//You can also use IndexOf+LengthCome and do it
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

                    //Traverse all files and replace the old namespace that has been unlocked
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

                                //Replace old NameSpace
                                if (content.IndexOf(oldNamespaceFull) > -1)
                                {
                                    logger.Append($"file hit:{file} -> {oldNamespaceFull}");
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

                return logger.Append("Update successful! You can also use the [Restore Namespace] function to restore!");

            }, saveLogAfterFinished: true);
        }

        #endregion

        [FunctionRender("Restore namespace", "Restore all source code in.cs, .cshtml The namespace in is NCF by default (recommended before cliff updates).", typeof(Register))]
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
                    response.Data = "Namespace restoration successful!";
                }
                return null;
            });
        }


        [FunctionRender("Download official NCF source code", "Modify all source code in.cs, .cshtml namespace in .", typeof(Register))]
        public async Task<StringAppResponse> DownloadSourceCode(NameSpace_DownloadSourceCodeRequest request)
        {
            return await this.GetResponseAsync<StringAppResponse, string>(async (response, logger) =>
            {
                if (Enum.TryParse<NameSpace_DownloadSourceCodeRequest.Parameters_Site>(request.Site.SelectedValues.FirstOrDefault(), out var siteType))
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
                            response.Data = "Unknown download address";
                            response.Success = false;
                            break;
                    }
                }
                else
                {
                    response.Data = "Unknown download parameters";
                    response.Success = false;
                }
                return null;
            });
        }

    }
}

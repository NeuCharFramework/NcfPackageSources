using Senparc.Ncf.Core.AppServices;
using Senparc.Xncf.ChangeNamespace.OHS.PL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
        public AppResponseBase<string> Change(NameSpace_ChangeRequest request)
        {
            return this.GetResponse<AppResponseBase<string>, string>((response, logger) =>
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

                //TODO:使用正则记录，并全局修改

                Dictionary<string, List<MatchNamespace>> namespaceCollection = new Dictionary<string, List<MatchNamespace>>(StringComparer.OrdinalIgnoreCase);

                //扫描所有规则
                foreach (var item in meetRules)
                {
                    var files = Directory.GetFiles(path, item.FileType, SearchOption.AllDirectories);

                    //扫描所有文件，将满足这一条规则替换条件的对象记录下来
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

                                        //不能使用Split，中间可能还有空格
                                        //var oldNamespaceArr = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                        var getOld = line.Replace(item.Prefix + " ", "");//也可以用IndexOf+Length来做
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

                    //遍历所有文件，替换已经解锁出来的旧命名空间
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

                                //替换旧的NameSpace
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

        [FunctionRender("还原命名空间", "还原所有源码在 .cs, .cshtml 中的命名空间为 NCF 默认（建议在断崖式更新之前进行此操作）。", typeof(Register))]
        public AppResponseBase<string> Restore(NameSpace_RestoreRequest request)
        {
            return this.GetResponse<AppResponseBase<string>, string>((response, logger) =>
            {
                var changeNamespaceParam = new NameSpace_ChangeRequest()
                {
                    NewNamespace = "Senparc.",
                    Path = request.Path
                };

                changeNamespaceParam.OldNamespaceKeyword = request.MyNamespace;
                var newesult = this.Change(changeNamespaceParam);
                if (newesult.Success == true)
                {
                    response.Data = "还原命名空间成功！";
                }
                return null;
            });
        }


        [FunctionRender("下载官方 NCF 源码", "修改所有源码在 .cs, .cshtml 中的命名空间。", typeof(Register))]
        public AppResponseBase<string> DownloadSourceCode(NameSpace_DownloadSourceCodeRequest request)
        {
            return this.GetResponse<AppResponseBase<string>, string>((response, logger) =>
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

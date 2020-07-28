using Senparc.CO2NET.Extensions;
using Senparc.Ncf.XncfBase;
using Senparc.Ncf.XncfBase.Functions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;

namespace Senparc.Xncf.ChangeNamespace.Functions
{
    public class ChangeNamespace : FunctionBase
    {
        public class MatchNamespace
        {
            public string Prefix { get; set; }
            public string OldNamespace { get; set; }
            public string NewNamespace { get; set; }
        }

        public class ChangeNamespace_Parameters : IFunctionParameter
        {
            [Required]
            [MaxLength(300)]
            [Description("路径||本地物理路径，如：E:\\Senparc\\Ncf\\")]
            public string Path { get; set; }
            [Required]
            [MaxLength(100)]
            [Description("新命名空间||命名空间根，必须以.结尾，用于替换[Senparc.Ncf.]")]
            public string NewNamespace { get; set; }
        }


        //注意：Name 必须在单个 Xncf 模块中唯一！
        public override string Name => "修改命名空间";

        public override string Description => "修改所有源码在 .cs, .cshtml 中的命名空间";

        public override Type FunctionParameterType => typeof(ChangeNamespace_Parameters);

        public ChangeNamespace(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public string OldNamespaceKeyword { get; set; } = "Senparc.";

        /// <summary>
        /// 运行
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public override FunctionResult Run(IFunctionParameter param)
        {
            return FunctionHelper.RunFunction<ChangeNamespace_Parameters>(param, (typeParam, sb, result) =>
            {
                base.RecordLog(sb, "开始运行 ChangeNamespace");

                var path = typeParam.Path;
                var newNamespace = typeParam.NewNamespace;

                if (!Directory.Exists(path))
                {
                    base.RecordLog(sb, $"path:{path} not exist");
                    result.Success = false;
                    result.Message = "路径不存在！";
                    return;
                }

                base.RecordLog(sb, $"path:{path} newNamespace:{newNamespace}");

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
                        base.RecordLog(sb, $"扫描文件类型:{item.FileType} 数量:{files.Length}");

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
                                    base.RecordLog(sb, $"文件命中:{file} -> {oldNamespaceFull}");
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

                result.Log = sb.ToString();
                result.Message = "更新成功！您还可以使用【还原命名空间】功能进行还原！";
            });
        }
    }
}

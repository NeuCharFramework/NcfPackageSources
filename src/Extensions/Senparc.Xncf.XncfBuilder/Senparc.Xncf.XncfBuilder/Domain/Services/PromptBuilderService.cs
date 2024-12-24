using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.SemanticKernel;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Entities;
using Senparc.AI.Kernel.KernelConfigExtensions;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.XncfBuilder.Domain.Services.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Senparc.AI.Kernel.Handlers;
using System.Linq;
using Npgsql.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;

namespace Senparc.Xncf.XncfBuilder.Domain.Services
{
    public class PromptBuilderService
    {
        private readonly SemanticAiHandler _aiHandler;
        private readonly PromptService _promptService;
        private readonly PromptRangeService _promptRangeService;
        private readonly PromptItemService _promptItemService;
        private readonly PromptResultService _promptResultService;

        public PromptBuilderService(/*IAiHandler aiHandler,*/ PromptService promptService, PromptRangeService promptRangeService, PromptItemService promptItemService, PromptResultService promptResultService)
        {
            //this._aiHandler = (SemanticAiHandler)aiHandler;
            this._aiHandler = promptService.IWantToRun.SemanticAiHandler;
            this._promptService = promptService;
            this._promptRangeService = promptRangeService;
            this._promptItemService = promptItemService;
            this._promptResultService = promptResultService;
        }

        /// <summary>
        /// 运行提示内容
        /// </summary>
        /// <param name="senparcAiSetting"></param>
        /// <param name="buildType"></param>
        /// <param name="input"></param>
        /// <param name="className"></param>
        /// <param name="context"></param>
        /// <param name="projectPath"></param>
        /// <param name="namespace"></param>
        /// <returns></returns>
        public async Task<(FilePlugin.FileSaveResult FileResult, string Log, string ResponseText, SenparcAiArguments Context)> RunPromptAsync(ISenparcAiSetting senparcAiSetting, PromptBuildType buildType, string input, string className = null, SenparcAiArguments context = null, string projectPath = null, string @namespace = null)
        {
            StringBuilder sb = new StringBuilder();
            context ??= new SenparcAiArguments();
            string responseText = string.Empty;
            FilePlugin.FileSaveResult fileResult = null;

            sb.AppendLine();
            sb.AppendLine($"[{SystemTime.Now.ToString()}]");
            sb.AppendLine($"开始生成，任务类型：{buildType.ToString()}");

            string createFilePath = projectPath;

            var plugins = new Dictionary<string, List<string>>();

            //选择需要执行的生成方式
            switch (buildType)
            {
                case PromptBuildType.EntityClass:
                case PromptBuildType.EntityDtoClass:
                    {
                        plugins["XncfBuilderPlugin"] = new List<string>();
                        if (buildType == PromptBuildType.EntityClass)
                        {
                            plugins["XncfBuilderPlugin"].Add("GenerateEntityClass");
                        }
                        else
                        {
                            plugins["XncfBuilderPlugin"].Add("GenerateEntityDtoClass");
                            context.KernelArguments["className"] = className;
                        }

                        if (!projectPath.IsNullOrEmpty())
                        {
                            createFilePath = Path.Combine(createFilePath, "Domain", "Models", "DatabaseModel");
                        }

                        context.KernelArguments["input"] = input;
                        context.KernelArguments["namespace"] = @namespace;

                        var pluginDir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Domain", "PromptPlugins");

                        var promptResult = await _promptService.GetPromptResultAsync<string>(senparcAiSetting, input, context, plugins, null);

                        if (buildType == PromptBuildType.EntityDtoClass)
                        {
                            //可能会生成转义后的注释
                            promptResult = promptResult.Replace("&lt;", "<").Replace("&gt;", ">");
                        }

                        responseText = promptResult;

                        sb.AppendLine(promptResult);

                        await Console.Out.WriteLineAsync($"{buildType.ToString()} 信息：");
                        await Console.Out.WriteLineAsync(promptResult);

                        //需要保存文件
                        if (!projectPath.IsNullOrEmpty())
                        {
                            #region 创建文件

                            //输入生成文件的项目路径

                            //var context = _promptService.IWantToRun.Kernel.CreateNewContext();//TODO：简化
                            var fileContext = new AI.Kernel.Entities.SenparcAiArguments();//TODO：简化

                            fileContext.KernelArguments["fileBasePath"] = createFilePath;
                            fileContext.KernelArguments["fileGenerateResult"] = promptResult;

                            var fileGenerateResult = promptResult.GetObject<List<FileGenerateResult>>();

                            //添加保存文件的 Plugin
                            var filePlugin = new FilePlugin(_promptService.IWantToRun);
                            var kernelPlugin = _promptService.IWantToRun.ImportPluginFromObject(filePlugin, "FilePlugin").kernelPlugin;

                            KernelFunction[] functionPiple = new[] { kernelPlugin[nameof(filePlugin.CreateFile)] };

                            var createFileResult = await _promptService.GetPromptResultAsync<FilePlugin.FileSaveResult>(senparcAiSetting, "", fileContext, null, null, null, null, functionPiple);
                            fileResult = createFileResult;

                            sb.AppendLine();
                            sb.AppendLine($"[{SystemTime.Now.ToString()}]");
                            sb.AppendLine(string.Join("\r\n# File\r\n", createFileResult.FileContents.Select(z => z.Key + " | " + z.Value).ToArray()));
                            await Console.Out.WriteLineAsync("创建文件 createFileResult:" + createFileResult.ToJson(true));

                            #endregion
                        }
                    }
                    break;
                case PromptBuildType.UpdateSenparcEntities:
                    {
                        #region 获取单词复数

                        var pluginDir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Domain", "PromptPlugins");
                        context.KernelArguments["input"] = className;
                        plugins["XncfBuilderPlugin"] = new List<string>() { "Pluralize" };
                        var pluralEntityName = await _promptService.GetPromptResultAsync<string>(senparcAiSetting, null, context, plugins, null);

                        pluralEntityName = pluralEntityName.Trim();

                        #endregion

                        #region 更新 SenparcEntities
                        //添加保存文件的 Plugin
                        var filePlugin = new FilePlugin(_promptService.IWantToRun);
                        //var skills = _promptService.IWantToRun.Kernel.ImportPluginFromPromptDirectory("FilePlugin");
                        var kernelPlugin = _promptService.IWantToRun.ImportPluginFromObject(filePlugin, "FilePlugin").kernelPlugin;

                        var updateFunctionPiple = new[] { kernelPlugin[nameof(filePlugin.UpdateSenparcEntities)] };

                        var fileContext = context;
                        fileContext.KernelArguments["projectPath"] = projectPath;
                        fileContext.KernelArguments["entityName"] = className;// fileGenerateResult[0].FileName.Split('.')[0]; ;
                        fileContext.KernelArguments["pluralEntityName"] = pluralEntityName;// fileGenerateResult[0].FileName.Split('.')[0]; ;

                        var updateSenparcEntitiesResult = await _promptService.GetPromptResultAsync<FilePlugin.FileSaveResult>(senparcAiSetting, "", fileContext, null, null, null, null, updateFunctionPiple);
                        responseText = updateSenparcEntitiesResult.Log;
                        fileResult = updateSenparcEntitiesResult;

                        sb.AppendLine();
                        sb.AppendLine($"[{SystemTime.Now.ToString()}]");
                        sb.AppendLine(responseText);
                        await Console.Out.WriteLineAsync(responseText);

                        #endregion
                    }
                    break;
                case PromptBuildType.Repository:
                    break;
                case PromptBuildType.Service:
                    break;
                case PromptBuildType.AppService:
                    break;
                case PromptBuildType.PL:
                    break;
                case PromptBuildType.DbContext:
                    break;
                default:
                    break;
            }

            return (FileResult: fileResult, Log: sb.ToString(), ResponseText: responseText, Context: context);
        }

        /// <summary>
        /// 初始化数据库中 PromptRange 的 XncfBuilderPlugin 靶场信息
        /// </summary>
        public async Task<string> InitPromptAsync(string promptRangeName, bool needOverride, string selectedAiModelId)
        {
            var promptRange = await _promptRangeService.GetObjectAsync(z => z.Alias == promptRangeName || z.RangeName == promptRangeName);

            if (promptRange == null || needOverride)
            {
                var log = new StringBuilder();
                if (promptRange != null && needOverride)
                {
                    try
                    {
                        //需要删除
                        log.AppendLine(promptRangeName + " 已存在，需要删除");

                        //删除所有 PromptItem
                        var promptItems = await _promptItemService.GetFullListAsync(z => z.RangeId == promptRange.Id);

                        foreach (var promptItem in promptItems)
                        {
                            var promptResults = await _promptResultService.GetFullListAsync(z => z.PromptItemId == promptItem.Id);

                            await _promptResultService.DeleteAllAsync(promptResults);
                            await _promptResultService.SaveChangesAsync();

                            await _promptItemService.DeleteObjectAsync(promptItem);
                            await _promptItemService.SaveChangesAsync();

                            log.AppendLine($"{promptItem.GetAvailableName()} 已删除");
                        }

                        await _promptRangeService.DeleteObjectAsync(promptRange);
                        await _promptRangeService.SaveChangesAsync();

                        log.AppendLine($"{promptRange.GetAvailableName()} 已删除");

                    }
                    catch (Exception ex)
                    {
                        log.AppendLine($"删除过程中发生异常：{ex.Message}");
                        log.AppendLine(ex.ToString());
                        log.AppendLine(ex.StackTrace);
                        log.AppendLine(promptRangeName + " 删除失败");
                        return log.ToString();
                    }
                }

                //初始化
                int aiModelId = 0;
                if (selectedAiModelId != "Default")
                {
                    int.TryParse(selectedAiModelId, out  aiModelId);
                }

                if (promptRangeName == "XncfBuilderPlugin")
                {
                    log.AppendLine("开始初始化 XncfBuilderPlugin 靶场信息");

                    //添加 PromptRange
                    var promptRangeDto = await _promptRangeService.AddAsync(promptRangeName);
                    var promptRangeId = promptRangeDto.Id;

                    //添加 PromptItem
                    var promptItemDto = new PromptItemDto()
                    {
                        RangeId = promptRangeId,
                        RangeName = promptRangeDto.RangeName,
                        NickName = "GenerateEntityClass",
                        ParentTac = "",
                        Tactic = "1",
                        Aiming = 1,
                        EvalAvgScore = 10,
                        EvalMaxScore = 10,
                        ModelId = aiModelId,
                        TopP = 1,
                        Temperature = 0.2f,
                        MaxToken = 4500,
                        FrequencyPenalty = 0,
                        PresencePenalty = 0,
                        StopSequences = "[\"#JsonResultEnd\"]",
                        FullVersion = "2024.05.12.1-T1-A1",//注意：仍然会被自动重置
                        Prefix = "{{$",
                        Suffix = "}}",
                        VariableDictJson = @"{""namespace"":"""",""input"":"""",""className"":""""}",
                        LastRunTime = SystemTime.Now.DateTime,
                        Content = @"[背景]
1. 这是一个代码生成任务。
2. 生成后的 Entity 代码将填充在 JSON 结果中的“EntityCode”参数中返回。
3. 生成代码的要求见[要求]。

[规则]
生成任务的规则如下：
1. 根据下方[要求]中的提示，生成 Entity 类的内容。
2. 将生成的类，整理成 JSON 格式并返回，JSON 示例代码如 #JsonResultStart 和 #JsonResultEnd 之间代码所示(示例中假设当前 Entity 名称为 Color，生成内容中请根据 3.2 中的要求生成)。
 2.1.第一条数据 FileName 用于设置 Entity 对应的文件名；EntityCode 用于设值此 Entity 类的代码（代码生成规则见 4.1）。
3. FileName 的文件名命名规则：
 3.1. Entity：<Entity 类名>.cs
 3.2. [Entity 文件名] 和 <Entity 类名> 相等，当[参数中的]className参数不为空时，使用className；否则，根据[要求]中的内容自动生成此名称。
4. EntityCode 生成规则：
 4.1. 返回数据的 EntityCode：分析[需求]中的代码生成要求，参考下方“[Entity 代码]”模板，生成新的代码。生成的代码必须根据要求对其类名、属性、方法进行重构。最后将生成的代码插入到 EntityCode 的值中。
6. [新 Entity 代码] 的生成需要进行如下的约束：
 6.1. 所有的属性都为只读，即设置 set 属性为 private。
 6.2. 提供一个不带参数的 private 构造函数。
 6.3. 所有的数据默认在一个 public 的构造函数中进行初始化。
 6.4. 实体内部包含了必要的方法，形成一个“充血实体”。
 6.5. 所有的类、参数、方法，都必须添加注释，注释内容必须结合生成要求，能明确描述当前对象的作用。
 6.5. 如果代码中出现了使用了新的枚举类型，请按为该枚举创建对应的代码（附加在 <Entity 类名> 定义之后）。如果没有明确指定枚举的值，请按照上下文理解需求后自动生成枚举代码。枚举值同样需要加入注释。
 6.6. 生成的同时请检查返回的 JSON 是否符合标准的 JSON 格式，尤其注意标签的闭合情况。

-----------------

[返回结果模板]
#JsonResultStart
[
{""FileName"":""[Entity 文件名]"",""EntityCode"": ""[新 Entity 代码]""}
]
#JsonResultEnd

-----------------

[Entity 代码]

using Senparc.Ncf.Core.Models;
using {{$namespace}}.Models.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace {{$namespace}}.Models
{
    /// <summary>
    /// Color 数据库实体
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Color))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class Color : EntityBase<int>
    {
        /// <summary>
        /// 红色，取值范围：0~255
        /// </summary>
        public int Red { get; private set; }

        public int Green { get; private set; }

        public int Blue { get; private set; }

        private Color() { }

        public Color(int red, int green, int blue)
        {
            if (red < 0 || green < 0 || blue < 0)
            {
                Random();//随机
            }
            else
            {
                Red = red;
                Green = green;
                Blue = blue;
            }
        }

        public Color(ColorDto colorDto)
        {
            Red = colorDto.Red;
            Green = colorDto.Green;
            Blue = colorDto.Blue;
        }

        public void Brighten()
        {
            Red = Math.Min(255, Red + 10);
            Green = Math.Min(255, Green + 10);
            Blue = Math.Min(255, Blue + 10);
        }
    }
}


-----------------


[要求]
{{$input}}

-----------------

[参数]
namespace:{{$namespace}}
className:{{$className}}

-----------------

[结果]
#JsonResultStart
",
                        Note = "生成 Entity 类的内容",

                        AddTime = SystemTime.Now.DateTime,
                        LastUpdateTime = SystemTime.Now.DateTime,

                    };

                    var promptItemGenerateEntityClass = new PromptItem(promptItemDto);
                    await this._promptItemService.SaveObjectAsync(promptItemGenerateEntityClass);
                    log.AppendLine($"完成 {promptRangeName}-{promptItemDto.NickName} 添加");

                    //添加 PromptItem-GenerateEntityDtoClass
                    promptItemDto.NickName = "GenerateEntityDtoClass";
                    promptItemDto.Content = @"[背景]
1. 这是一个代码生成任务。
2. 生成后的 DTO Entity 代码将填充在 JSON 结果中的“EntityCode”参数中返回。
3. 生成代码的要求见[要求]。

[规则]
生成任务的规则如下：
1. 根据下方[源码]中的代码，生成对应的 Dto Entity 类的内容。
2. 将生成的类，整理成 JSON 格式并返回，JSON 示例代码如 #JsonResultStart 和 #JsonResultEnd 之间代码所示(示例中假设当前 Entity 名称为 {{{$className}})。
 2.1. FileName 用于设置 DTO Entity 对应的文件名，文件名的命名规则见 3.1。
 2.2. EntityCode 用于设值此 DTO Entity 类的代码（代码生成规则见 4.1）。
3. FileName 的文件名命名规则：
 3.1. DTO Entity：Dto/{{$className}}Dto.cs
4. EntityCode 生成规则：
 4.1. 数据的 EntityCode：分析[需求]中的代码生成要求，参考下方“[DTO Entity 代码]”模板，生成新的代码，生成的代码必须根据要求对其类名、属性、方法进行重构。最后将生成的代码插入到 EntityCode 的值中。
6. [新 DTO Entity 代码] 的生成需要进行如下的约束：
 6.1. 所有的属性都为只读，即设置 set 属性为 private。
 6.2. 提供一个不带参数的 private 构造函数。
 6.3. 所有的数据默认在一个 public 的构造函数中进行初始化。
 6.4. [源码]中如果有枚举，在 DTO Entity 中不需要再次生成。
 6.5. 所有的类、参数、方法的注释都和[源码]中的属性保持一致。
 6.6. 生成的同时请检查返回的 JSON 是否符合标准的 JSON 格式，尤其注意标签的闭合情况。
 6.7. 生成代码不需要包含 Markdown 的代码块标签（```）。
 6.8. 注释请完全复制，不要对 < > 等符号进行转义。

-----------------

[返回结果模板]
#JsonResultStart
[
{""FileName"":""[DTO Entity 文件名]"",""EntityCode"":""[新 DTO Entity 代码]""}
]
#JsonResultEnd

-----------------

[DTO Entity 代码转换示例]

源码：

```
using Senparc.Ncf.Core.Models;

namespace {{$namespace}}
{
    /// <summary>
    /// {{$className}} 数据库实体
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof({{$className}}))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class {{$className}} : EntityBase<int>
    {
        //更多属性和构造函数
    }
}
```


新 DTO Entity 代码：

```
#JsonResultStart
using Senparc.Ncf.Core.Models;
using {{$namespace}}.Models;

namespace {{$namespace}}.Models.Dto
{
    /// <summary>
    /// {{$className}} 数据库实体 DTO
    /// </summary>
    public class {{$className}}Dto : DtoBase
    {
        //此处添加 [源码] 类中的所有属性及构造函数，删除所有额外的方法，保留完整注释内容
    }
}

```
-----------------


[源码]
```
{{$input}}
```

namespace:{{$namespace}}

className:{{$className}}

-----------------

[结果]
#JsonResultStart
";
                    promptItemDto.Tactic = "2";
                    promptItemDto.FullVersion = "2024.05.12.1-T2-A1";//注意：仍然会被自动重置
                    promptItemDto.StopSequences = "[\"#JsonResultEnd\"]";
                    promptItemDto.VariableDictJson = @"{""className"":"""",""namespace"":"""",""input"":""""}";
                    promptItemDto.Note = "生成 DTO Entity 类的内容";
                    var promptItemGenerateEntityDtoClass = new PromptItem(promptItemDto);
                    await this._promptItemService.SaveObjectAsync(promptItemGenerateEntityDtoClass);
                    log.AppendLine($"完成 {promptRangeName}-{promptItemDto.NickName} 添加");

                    //添加 PromptItem-Pluralize
                    promptItemDto.NickName = "Pluralize";
                    promptItemDto.Content = @"[要求]
[要求]
请将 INPUT: 中提供的单词转换为复数，在 [输出] 跟着输出结果。

[示例]
Entity
> Entities #END

Apple
> Apples #END

[输出]
{{$input}}
>";
                    promptItemDto.Tactic = "3";
                    promptItemDto.FullVersion = "2024.05.12.1-T3-A1";//注意：仍然会被自动重置
                    promptItemDto.StopSequences = "[\"INPUT\",\"OUTPUT\",\" #END\"]";
                    promptItemDto.VariableDictJson = @"{""input"":""""}";
                    promptItemDto.Note = "将单词转换为复数";
                    promptItemDto.MaxToken = 200;
                    var promptItemPluralize = new PromptItem(promptItemDto);
                    await this._promptItemService.SaveObjectAsync(promptItemPluralize);
                    log.AppendLine($"完成 {promptRangeName}-{promptItemDto.NickName} 添加");

                    log.AppendLine("全部初始化完成！");

                    return log.ToString();
                }

                return $"未知的 PromptRangeName：{promptRangeName}";
            }
            else
            {
                return "记录已存在，未覆盖";
            }
        }
    }
}

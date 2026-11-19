/*----------------------------------------------------------------

    Copyright (C) 2026 Senparc

    File name: PromptBuilderService.cs
    File description: Related implementation.

    Created by: Senparc - 20231003

    Modified by: Senparc - 20260702
    Change description: v0.11.0-preview2 synchronize changes within the master/main baseline and complete recursive dependency version handling

    Modified by: Senparc - 20261119
    Change description: v0.41.10 Localized XNCF Builder tools and template surfaces

----------------------------------------------------------------*/

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Microsoft.SemanticKernel;
using Senparc.AI.Interfaces;
using Senparc.AI.AgentKernel;
using Senparc.AI.AgentKernel.Entities;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.XncfBuilder.Domain.Services.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Senparc.AI.AgentKernel.Handlers;
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
        private readonly AgentAiHandler _aiHandler;
        private readonly PromptService _promptService;
        private readonly PromptRangeService _promptRangeService;
        private readonly PromptItemService _promptItemService;
        private readonly PromptResultService _promptResultService;

        public PromptBuilderService(/*IAiHandler aiHandler,*/ PromptService promptService, PromptRangeService promptRangeService, PromptItemService promptItemService, PromptResultService promptResultService)
        {
            //this._aiHandler = (AgentAiHandler)aiHandler;
            this._aiHandler = promptService.IWantToRun.AgentAiHandler;
            this._promptService = promptService;
            this._promptRangeService = promptRangeService;
            this._promptItemService = promptItemService;
            this._promptResultService = promptResultService;
        }

        /* Legacy comment translated to English. */
        /*



        /// <summary>

        Legacy comment translated to English.

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

            sb.AppendLine($"startgenerate，tasktype：{buildType.ToString()}");



            string createFilePath = projectPath;



            var plugins = new Dictionary<string, List<string>>();



            Legacy comment translated to English.

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

                            Legacy comment translated to English.

                            promptResult = promptResult.Replace("&lt;", "<").Replace("&gt;", ">");

                        }



                        responseText = promptResult;



                        sb.AppendLine(promptResult);



                        await Console.Out.WriteLineAsync($"{buildType.ToString()} information：");

                        await Console.Out.WriteLineAsync(promptResult);



                        //needsave files

                        if (!projectPath.IsNullOrEmpty())

                        {

                            #region create files



                            Legacy comment translated to English.



                            Legacy comment translated to English.

                            Legacy comment translated to English.



                            fileContext.KernelArguments["fileBasePath"] = createFilePath;

                            fileContext.KernelArguments["fileGenerateResult"] = promptResult;



                            var fileGenerateResult = promptResult.GetObject<List<FileGenerateResult>>();



                            Legacy comment translated to English.

                            var filePlugin = new FilePlugin(_promptService.IWantToRun);

                            var kernelPlugin = _promptService.IWantToRun.ImportPluginFromObject(filePlugin, "FilePlugin").kernelPlugin;



                            KernelFunction[] functionPiple = new[] { kernelPlugin[nameof(filePlugin.CreateFile)] };



                            var createFileResult = await _promptService.GetPromptResultAsync<FilePlugin.FileSaveResult>(senparcAiSetting, "", fileContext, null, null, null, null, functionPiple);

                            fileResult = createFileResult;



                            sb.AppendLine();

                            sb.AppendLine($"[{SystemTime.Now.ToString()}]");

                            sb.AppendLine(string.Join("\r\n# File\r\n", createFileResult.FileContents.Select(z => z.Key + " | " + z.Value).ToArray()));

                            await Console.Out.WriteLineAsync("create files createFileResult:" + createFileResult.ToJson(true));



                            #endregion

                        }

                    }

                    break;

                case PromptBuildType.UpdateSenparcEntities:

                    {

                        #region get the plural form



                        var pluginDir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Domain", "PromptPlugins");

                        context.KernelArguments["input"] = className;

                        plugins["XncfBuilderPlugin"] = new List<string>() { "Pluralize" };

                        var pluralEntityName = await _promptService.GetPromptResultAsync<string>(senparcAiSetting, null, context, plugins, null);



                        pluralEntityName = pluralEntityName.Trim();



                        #endregion



                        #region update SenparcEntities

                        Legacy comment translated to English.

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

        Legacy comment translated to English.

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

                        //needdelete

                        Legacy comment translated to English.



                        //deleteall PromptItem

                        var promptItems = await _promptItemService.GetFullListAsync(z => z.RangeId == promptRange.Id);



                        foreach (var promptItem in promptItems)

                        {

                            var promptResults = await _promptResultService.GetFullListAsync(z => z.PromptItemId == promptItem.Id);



                            await _promptResultService.DeleteAllAsync(promptResults);

                            await _promptResultService.SaveChangesAsync();



                            await _promptItemService.DeleteObjectAsync(promptItem);

                            await _promptItemService.SaveChangesAsync();



                            Legacy comment translated to English.

                        }



                        await _promptRangeService.DeleteObjectAsync(promptRange);

                        await _promptRangeService.SaveChangesAsync();



                        Legacy comment translated to English.



                    }

                    catch (Exception ex)

                    {

                        Legacy comment translated to English.

                        log.AppendLine(ex.ToString());

                        log.AppendLine(ex.StackTrace);

                        log.AppendLine(promptRangeName + " deletefailure");

                        return log.ToString();

                    }

                }



                //initialize

                int aiModelId = 0;

                if (selectedAiModelId != "Default")

                {

                    int.TryParse(selectedAiModelId, out  aiModelId);

                }



                if (promptRangeName == "XncfBuilderPlugin")

                {

                    log.AppendLine("startinitialize XncfBuilderPlugin rangeinformation");



                    Legacy comment translated to English.

                    var promptRangeDto = await _promptRangeService.AddAsync(promptRangeName);

                    var promptRangeId = promptRangeDto.Id;



                    Legacy comment translated to English.

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

                        Legacy comment translated to English.

                        Prefix = "{{$",

                        Suffix = "}}",

                        VariableDictJson = @"{""namespace"":"""",""input"":"""",""className"":""""}",

                        LastRunTime = SystemTime.Now.DateTime,

                        Legacy comment translated to English.

Legacy comment translated to English.

Legacy comment translated to English.

Legacy comment translated to English.



Legacy comment translated to English.

Legacy comment translated to English.

Legacy comment translated to English.

Legacy comment translated to English.

 Legacy comment translated to English.

Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

Legacy comment translated to English.

 Legacy comment translated to English.

Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.



-----------------



Legacy comment translated to English.

#JsonResultStart

[

Legacy comment translated to English.

]

#JsonResultEnd



-----------------



[Entity code]



using Senparc.Ncf.Core.Models;

using {{$namespace}}.Models.Dto;

using System;

using System.ComponentModel.DataAnnotations.Schema;



namespace {{$namespace}}.Models

{

    /// <summary>

    Legacy comment translated to English.

    /// </summary>

    [Table(Register.DATABASE_PREFIX + nameof(Color))]//a prefix is required to prevent system-wide conflicts

    [Serializable]

    public class Color : EntityBase<int>

    {

        /// <summary>

        Legacy comment translated to English.

        /// </summary>

        public int Red { get; private set; }



        public int Green { get; private set; }



        public int Blue { get; private set; }



        private Color() { }



        public Color(int red, int green, int blue)

        {

            if (red < 0 || green < 0 || blue < 0)

            {

                Legacy comment translated to English.

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





Legacy comment translated to English.

{{$input}}



-----------------



[parameters]

namespace:{{$namespace}}

className:{{$className}}



-----------------



[result]

#JsonResultStart

",

                        Legacy comment translated to English.



                        AddTime = SystemTime.Now.DateTime,

                        LastUpdateTime = SystemTime.Now.DateTime,



                    };



                    var promptItemGenerateEntityClass = new PromptItem(promptItemDto);

                    await this._promptItemService.SaveObjectAsync(promptItemGenerateEntityClass);

                    Legacy comment translated to English.



                    Legacy comment translated to English.

                    promptItemDto.NickName = "GenerateEntityDtoClass";

                    Legacy comment translated to English.

Legacy comment translated to English.

Legacy comment translated to English.

Legacy comment translated to English.



Legacy comment translated to English.

Legacy comment translated to English.

Legacy comment translated to English.

Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

Legacy comment translated to English.

 3.1. DTO Entity：Dto/{{$className}}Dto.cs

Legacy comment translated to English.

 Legacy comment translated to English.

Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.

 Legacy comment translated to English.



-----------------



Legacy comment translated to English.

#JsonResultStart

[

Legacy comment translated to English.

]

#JsonResultEnd



-----------------



Legacy comment translated to English.



Legacy comment translated to English.



```

using Senparc.Ncf.Core.Models;



namespace {{$namespace}}

{

    /// <summary>

    Legacy comment translated to English.

    /// </summary>

    [Table(Register.DATABASE_PREFIX + nameof({{$className}}))]//a prefix is required to prevent system-wide conflicts

    [Serializable]

    public class {{$className}} : EntityBase<int>

    {

        Legacy comment translated to English.

    }

}

```





Legacy comment translated to English.



```

#JsonResultStart

using Senparc.Ncf.Core.Models;

using {{$namespace}}.Models;



namespace {{$namespace}}.Models.Dto

{

    /// <summary>

    Legacy comment translated to English.

    /// </summary>

    public class {{$className}}Dto : DtoBase

    {

        Legacy comment translated to English.

    }

}



```

-----------------





Legacy comment translated to English.

```

{{$input}}

```



namespace:{{$namespace}}



className:{{$className}}



-----------------



[result]

#JsonResultStart

";

                    promptItemDto.Tactic = "2";

                    Legacy comment translated to English.

                    promptItemDto.StopSequences = "[\"#JsonResultEnd\"]";

                    promptItemDto.VariableDictJson = @"{""className"":"""",""namespace"":"""",""input"":""""}";

                    Legacy comment translated to English.

                    var promptItemGenerateEntityDtoClass = new PromptItem(promptItemDto);

                    await this._promptItemService.SaveObjectAsync(promptItemGenerateEntityDtoClass);

                    Legacy comment translated to English.



                    Legacy comment translated to English.

                    promptItemDto.NickName = "Pluralize";

                    Legacy comment translated to English.

Legacy comment translated to English.

Legacy comment translated to English.



Legacy comment translated to English.

Entity

> Entities #END



Apple

> Apples #END



[output]

{{$input}}

>";

                    promptItemDto.Tactic = "3";

                    Legacy comment translated to English.

                    promptItemDto.StopSequences = "[\"INPUT\",\"OUTPUT\",\" #END\"]";

                    promptItemDto.VariableDictJson = @"{""input"":""""}";

                    Legacy comment translated to English.

                    promptItemDto.MaxToken = 200;

                    var promptItemPluralize = new PromptItem(promptItemDto);

                    await this._promptItemService.SaveObjectAsync(promptItemPluralize);

                    Legacy comment translated to English.



                    Legacy comment translated to English.



                    return log.ToString();

                }



                Legacy comment translated to English.

            }

            else

            {

                Legacy comment translated to English.

            }

        }

        */
    }
}

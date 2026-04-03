using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using Senparc.CO2NET.Helpers.Serializers;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public partial class PromptItemService
    {
        public async Task<string> ExportPluginsAsync(string fullVersion)
        {
            var item = await this.GetObjectAsync(p => p.FullVersion == fullVersion) ??
                       throw new NcfExceptionBase($"未找到{fullVersion}对应的提示词靶道");
            var rangePath = await this.ExportPluginWithItemAsync(item);

            return rangePath;
        }

        public async Task<string> ExportPluginsAsync(IEnumerable<int> rangeIds, List<int> ids)
        {
            var rangeFilePaths = new List<string>();
            foreach (var rangeId in rangeIds)
            {
                var pluginFilePath = await this.ExportPluginsAsync(rangeId, ids);
                rangeFilePaths.Add(pluginFilePath);
            }

            // According to rangeFilePaths, find the path of their common parent folder
            var commonParentPath = this.FindCommonParentPath(rangeFilePaths);

            return commonParentPath;
        }

        private string FindCommonParentPath(IEnumerable<string> paths)
        {
            var pathList = paths.ToList();
            if (pathList.Count == 0)
            {
                throw new NcfExceptionBase("没有找到需要导出的路径");
            }
            
            if (pathList.Count == 1)
            {
                // There is only one path, returning to its parent directory
                return Directory.GetParent(pathList[0])?.FullName ?? pathList[0];
            }
            
            // Standardized path separators (Windows and Unix compatible)
            var normalizedPaths = pathList.Select(p => 
                p.Replace('\\', Path.DirectorySeparatorChar)
                 .Replace('/', Path.DirectorySeparatorChar)
            ).ToList();
            
            var splitPaths = normalizedPaths.Select(p => 
                p.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
            ).ToList();
            
            var minLen = splitPaths.Min(sp => sp.Length);

            var commonPath = new List<string>();
            for (var i = 0; i < minLen - 1; i++) // minLen - 1 ensures that the last folder name is not included
            {
                var dir = splitPaths[0][i];
                // Case-insensitive comparison (compatible with Windows and Unix)
                if (splitPaths.All(sp => string.Equals(sp[i], dir, StringComparison.OrdinalIgnoreCase)))
                {
                    commonPath.Add(dir);
                }
                else
                {
                    break;
                }
            }

            // Unix systems need to retain the / of the root path
            var result = Path.Combine(commonPath.ToArray());
            if (normalizedPaths[0].StartsWith(Path.DirectorySeparatorChar.ToString()) && 
                !result.StartsWith(Path.DirectorySeparatorChar.ToString()))
            {
                result = Path.DirectorySeparatorChar + result;
            }
            
            return result;
        }

        /// <summary>
        /// Based on the shooting range ID, export all the shooting ranges under the shooting range and return to the folder path
        /// </summary>
        /// <param name="rangeId"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<string> ExportPluginsAsync(int rangeId, List<int> ids)
        {
            // Get the shooting range based on the shooting range name
            var promptRange = await _promptRangeService.GetAsync(rangeId);

            // Get the folder path of the output range
            var rangePath = this.GetRangePath(promptRange);

            // Get the target lane based on the shooting range name
            var promptItemList = await this.GetFullListAsync(
                p => p.RangeName == promptRange.RangeName
                     && (ids == null || ids.Contains(p.Id))
            );

            // //Use version number as key, mapping dictionary
            // var itemMapByVersion = promptItemList.ToDictionary(p => p.FullVersion, p => p);

            // //Extract the first bit of T and group it
            // Dictionary<string, List<PromptItem>> itemGroupByT = promptItemList.GroupBy(p => p.Tactic.Substring(0, 1))
            //     .ToDictionary(p => p.Key, p => p.ToList());

            // Each target lane needs to be exported
            foreach (var item in promptItemList)
            {
                // // Find the best item
                // var bestItem = itemList.MaxBy(p => isAvg ? p.EvalAvgScore : p.EvalMaxScore);

                await ExportPluginWithItemAsync(item, rangePath);
            }

            return rangePath;
        }

        /// <summary>
        ///Export the specified single target lane and return the folder path
        /// </summary>
        /// <param name="promptItem"></param>
        /// <param name="rangePath"></param>
        /// <returns></returns>
        public async Task<string> ExportPluginWithItemAsync(PromptItem promptItem, string rangePath = null)
        {
            var range = await _promptRangeService.GetAsync(promptItem.RangeId);

            rangePath ??= this.GetRangePath(range);

            #region 根据模板构造 Root 对象

            var data = new Root()
            {
                schema = 1,
                description = "Generated by Senparc.Xncf.PromptRange",
                execution_settings = new ExecutionSettings()
                {
                    Default = new Default()
                    {
                        max_tokens = promptItem.MaxToken,
                        temperature = promptItem.Temperature,
                        top_p = promptItem.TopP,
                        presence_penalty = promptItem.PresencePenalty,
                        frequency_penalty = promptItem.FrequencyPenalty,
                        stop_sequences = (promptItem.StopSequences ?? "[]").GetObject<List<string>>()
                    }
                },
                input_variables = new List<PromptInputVariable>()
            };

            //Add input object
            var inputVarialbes = promptItem.GetInputValiableObject();
            data.input_variables.AddRange(inputVarialbes.Select(z => new PromptInputVariable(z)));

            #endregion

            //  Current plugin folder directory, target channel name/alias
            var curPluginPath = Path.Combine(rangePath, promptItem.NickName ?? promptItem.FullVersion);
            if (!Directory.Exists(curPluginPath))
            {
                Directory.CreateDirectory(curPluginPath);
            }
            else
            {
                // If the alias already exists, add a suffix
                curPluginPath += $"_{DateTime.Now:yyyyMMddHHmmss}";
                Directory.CreateDirectory(curPluginPath);
            }

            // Full JSON file path
            // string jsonFullPath = Path.Combine(curPluginPath, "config.json");

            await using (var jsonFs = new FileStream(
                             Path.Combine(curPluginPath, "config.json"),
                             FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                jsonFs.Seek(0, SeekOrigin.Begin);
                jsonFs.SetLength(0); // Clear file contents
                await using (var jsonSw = new StreamWriter(jsonFs, Encoding.UTF8))
                {
                    // Write and keep format
                    await jsonSw.WriteLineAsync(JsonConvert.SerializeObject(data, Formatting.Indented));
                }
            }

            // In the same way, construct the skprompt.txt file with content
            await using (var txtFs = new FileStream(
                             Path.Combine(curPluginPath, "skprompt.txt"),
                             FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                txtFs.Seek(0, SeekOrigin.Begin);
                txtFs.SetLength(0); // Clear file contents
                await using (var jsonSw = new StreamWriter(txtFs, Encoding.UTF8))
                {
                    await jsonSw.WriteLineAsync(promptItem.Content);
                }
            }

            return rangePath;
        }

        /// <summary>
        /// Based on the shooting range, generate a folder and return the folder path
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        private string GetRangePath(PromptRangeDto range)
        {
            #region 根据靶场别名，生成文件夹

            // If there is an alias, use the alias; if not, use the shooting range name.

            // Get the root directory first
            var curDir = Directory.GetCurrentDirectory();

            var filePathPrefix = Path.Combine(curDir, "App_Data", "Files");


            // Generate folder
            var rangePath = Path.Combine(filePathPrefix, "ExportedPluginsTemp", $"{range.Alias ?? range.RangeName}_{range.RangeName}");

            if (Directory.Exists(rangePath))
            {
                // If it exists, clean the specified folder first
                Directory.Delete(rangePath, true);
            }
            Directory.CreateDirectory(rangePath);

            #endregion

            return rangePath;
        }

        #region Inner Class

        class ExecutionSettings
        {
            [JsonProperty] public Default Default { get; set; }
        }

        class Default
        {
            public int max_tokens { get; set; }
            public float temperature { get; set; }
            public float top_p { get; set; }
            public float presence_penalty { get; set; }
            public float frequency_penalty { get; set; }
            public List<string> stop_sequences { get; set; }
        }

        class Root
        {
            public int schema { get; set; }
            public string description { get; set; }
            public ExecutionSettings execution_settings { get; set; }

            public List<PromptInputVariable> input_variables { get; set; }
        }

        class PromptInputVariable
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Default { get; set; }

            public PromptInputVariable(InputVariable inputVariable)
            {
                Name = inputVariable.Name;
                Description = inputVariable.Description;
                Default = inputVariable.Default?.ToString();
                //TODO: Add more
            }
        }

        #endregion

        public async Task UploadPluginsAsync(IFormFile uploadedFile)
        {
            #region 验证文件

            if (uploadedFile == null || uploadedFile.Length == 0)
                throw new NcfExceptionBase("文件未找到");
            // Limit file upload size to 50M
            if (uploadedFile.Length > 1024 * 1024 * 50)
            {
                throw new NcfExceptionBase("文件大小超过限制（50 M）");
            }

            if (!uploadedFile.FileName.EndsWith(".zip"))
            {
                throw new NcfExceptionBase("文件格式错误");
            }

            #endregion

            #region 保存文件

            var toSaveDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "Files", "toImportFileTemp");
            if (!Directory.Exists(toSaveDir))
            {
                Directory.CreateDirectory(toSaveDir);
            }

            // File save path
            var zipFilePath = Path.Combine(toSaveDir, uploadedFile.FileName);

            using (var stream = new FileStream(zipFilePath, FileMode.Create))
            {
                await uploadedFile.CopyToAsync(stream);
            }

            #endregion



            // Read zip file
            using var zip = ZipFile.OpenRead(zipFilePath);

            //Unzip
            var extractDir = $"{zipFilePath}-{SystemTime.NowTicks}";
            zip.ExtractToDirectory(extractDir);

            //Determine file structure
            //Assume full path
            var topDirs = Directory.GetDirectories(extractDir);
            List<PromptItem> promptItems = new List<PromptItem>();
            foreach (var topDir in topDirs)
            {
                // Create a shooting range first
                Console.WriteLine("topDir:" + topDir);
                var rangeAlias = Path.GetFileName(topDir);
                var promptRangeDto = await _promptRangeService.AddAsync(rangeAlias);

                var pluginDirs = Directory.GetDirectories(topDir);
                var tacticIndex = 0;
                foreach (var pluginDir in pluginDirs)
                {
                    var configFilePath = Path.Combine(pluginDir, "config.json");
                    var skpromptFilePath = Path.Combine(pluginDir, "skprompt.txt");

                    if (!File.Exists(configFilePath) && !File.Exists(skpromptFilePath))
                    {
                        //TODO: Give a failure message
                        continue;
                    }

                    var promptItemAlias = Path.GetFileName(pluginDir);
                    var promptItem = new PromptItem(promptRangeDto, promptItemAlias, (++tacticIndex).ToString());
                    promptItems.Add(promptItem);

                    if (File.Exists(configFilePath))
                    {
                        // Read all files into a string
                        await using Stream stream = new FileStream(configFilePath, FileMode.Open);
                        using StreamReader reader = new StreamReader(stream);

                        string text = await reader.ReadToEndAsync();

                        var rootConfig = text.GetObject<Root>();
                        var executionSettings = rootConfig.execution_settings!;
                        var variableDictJson = rootConfig.input_variables != null && rootConfig.input_variables.Count > 0
                            ? rootConfig.input_variables.ToDictionary(z => z.Name, z => "").ToJson()
                            : null;

                        promptItem.UpdateModelParam(
                            topP: executionSettings.Default.top_p,
                            maxToken: executionSettings.Default.max_tokens,
                            temperature: executionSettings.Default.temperature,
                            presencePenalty: executionSettings.Default.presence_penalty,
                            frequencyPenalty: executionSettings.Default.frequency_penalty,
                            stopSequences: executionSettings.Default.stop_sequences.ToJson(),
                            variableDictJson: variableDictJson
                        );
                    }

                    if (File.Exists(skpromptFilePath))
                    {
                        // Read all files into a string
                        await using Stream stream = new FileStream(skpromptFilePath, FileMode.Open);
                        using StreamReader reader = new StreamReader(stream);

                        string skPrompt = await reader.ReadToEndAsync();

                        promptItem.UpdateContent(skPrompt);

                        // Extract prompt request parameters
                        var pattern = @"\{\{\$(.*?)\}\}";//TODO: Support more formats

                        // no parameters
                        if (!Regex.IsMatch(skPrompt, pattern))
                        {
                            continue;
                        }

                        MatchCollection matches = Regex.Matches(skPrompt, pattern);
                        Dictionary<string, string> varDict = new();
                        foreach (Match match in matches)
                        {
                            string varKey = match.Groups[1].Value;
                            varDict[varKey] = "";
                        }

                        promptItem.UpdateVariablesJson(varDict.ToJson(), "{{$", "}}");
                    }

                }
            }


            #region 老方法
            /*
            // #region can choose to decompress first

            // zip.ExtractToDirectory(Path.Combine(toSaveDir, zipFile.FileName.Split(".")[0]), true);

            // Unzip the file
            // var unzippedFilePath = Path.Combine(toSaveDir, zipFile.FileName.Split(".")[0], "");
            // if (!Directory.Exists(unzippedFilePath))
            // {
            //     Directory.CreateDirectory(unzippedFilePath);
            // }

            // ZipFile.ExtractToDirectory(toSaveFilePath, unzippedFilePath, Encoding.UTF8, true);
            // ZipFile.ExtractToDirectory(zipFile.OpenReadStream(), unzippedFilePath, Encoding.UTF8, true);

            // #endregion

            // Start reading
            Dictionary<string, PromptItem> zipIdxDict = new();
            int tacticCnt = 0;
            foreach (var curFile in zip.Entries)
            {
                // var curFilePath = Path.Combine(extractPath, entry.FullName);
                var curDirName = Path.GetDirectoryName(curFile.FullName)!;
                if (curDirName.Contains('/') || curDirName.Contains('\\'))
                {
                    throw new NcfExceptionBase($"{curFile.FullName} file format error");
                }

                if (curFile.Name == "") // is a directory
                {
                    var promptItem = new PromptItem(promptRange, curDirName, ++tacticCnt);

                    zipIdxDict[curDirName] = promptItem;
                }
                else
                {
                    // var directoryName = curDirName!;
                    // if (directoryName.Contains('/') || directoryName.Contains('\\'))
                    // {
                    // throw new NcfExceptionBase($"{curFile.FullName} file format error");
                    // }

                    //Read from cache
                    var promptItem = zipIdxDict[curDirName];

                    //Update different fields based on different file names
                    if (curFile.Name == "config.json") // Update configuration file
                    {
                        // Read all files into a string
                        await using Stream stream = curFile.Open();
                        using StreamReader reader = new StreamReader(stream);

                        string text = await reader.ReadToEndAsync();


                        var executionSettings = text.GetObject<Root>().ExecutionSettings!;

                        promptItem.UpdateModelParam(
                            topP: executionSettings.Default.TopP,
                            maxToken: executionSettings.Default.MaxTokens,
                            temperature: executionSettings.Default.Temperature,
                            presencePenalty: executionSettings.Default.PresencePenalty,
                            frequencyPenalty: executionSettings.Default.FrequencyPenalty,
                            stopSequences: executionSettings.Default.StopSequences.ToJson()
                        );
                    }
                    else if (curFile.Name == "skprompt.txt")
                    {
                        // Read all files into a string
                        await using Stream stream = curFile.Open();
                        using StreamReader reader = new StreamReader(stream);

                        string skPrompt = await reader.ReadToEndAsync();

                        promptItem.UpdateContent(skPrompt);

                        //Extract prompt request parameters
                        var pattern = @"\{\{\$(.*?)\}\}";

                        // no parameters
                        if (!Regex.IsMatch(skPrompt, pattern))
                        {
                            continue;
                        }

                        MatchCollection matches = Regex.Matches(skPrompt, pattern);
                        Dictionary<string, string> varDict = new();
                        foreach (Match match in matches)
                        {
                            string varKey = match.Groups[1].Value;
                            varDict[varKey] = "";
                        }

                        promptItem.UpdateVariablesJson(varDict.ToJson());
                    }
                    else
                    {
                        continue;
                        throw new NcfExceptionBase($"{curFile.FullName} does not meet the upload requirements");
                    }
                }
            }

            await this.SaveObjectListAsync(zipIdxDict.Values.ToList());
            */
            #endregion


            // save
            await this.SaveObjectListAsync(promptItems);
        }
    }
}

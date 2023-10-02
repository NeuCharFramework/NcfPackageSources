using Microsoft.SemanticKernel.Memory;
using Senparc.AI.Entities;
using Senparc.AI;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.AI.Kernel.Helpers;
using Senparc.CO2NET.Extensions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Senparc.Xncf.PromptRange.Domain.Services.PromptPlugins;
using Microsoft.SemanticKernel;
using System.IO;
using Microsoft.SemanticKernel.SkillDefinition;

namespace Senparc.Xncf.PromptRange.Domain.Services
{
    public class PromptService /*: ServiceDataBase*/
    {
        private readonly SemanticAiHandler _aiHandler;

        //public PromptService(IDataBase baseData) : base(baseData)
        //{
        //}

        /// <summary>
        /// 获取最终 Prompt 文字
        /// </summary>
        /// <param name="promptType"></param>
        /// <param name="prompt"></param>
        /// <returns></returns>
        private string GetPrompt(XncfBuilderPromptType promptType, string prompt)
        {
            return promptType switch
            {
                XncfBuilderPromptType.EntityClass => @"请按照以下示例，根据[Prompt]生成全新的类：
using Senparc.Ncf.Core.Models;
using Senparc.Xncf.PromptRange.Models.DatabaseModel.Dto;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Senparc.Xncf.PromptRange
{
    /// <summary>
    /// Color 实体类
    /// </summary>
    [Table(Register.DATABASE_PREFIX + nameof(Color))]//必须添加前缀，防止全系统中发生冲突
    [Serializable]
    public class Color : EntityBase<int>
    {
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Red { get; private set; }
        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Green { get; private set; }

        /// <summary>
        /// 颜色码，0-255
        /// </summary>
        public int Blue { get; private set; }

        /// <summary>
        /// 附加列，测试多次数据库 Migrate
        /// </summary>
        public string AdditionNote { get; private set; }

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

        public void Random()
        {
            //随机产生颜色代码
            var radom = new Random();
            Func<int> getRadomColorCode = () => radom.Next(0, 255);
            Red = getRadomColorCode();
            Green = getRadomColorCode();
            Blue = getRadomColorCode();
        }

        public void Brighten()
        {
            Red = Math.Min(255, Red + 10);
            Green = Math.Min(255, Green + 10);
            Blue = Math.Min(255, Blue + 10);
        }

        public void Darken()
        {
            Red = Math.Max(0, Red - 10);
            Green = Math.Max(0, Green - 10);
            Blue = Math.Max(0, Blue - 10);
        }
    }
}" +
@$"

[Prompt]:{prompt}
",
                _ => $"{prompt}"
            };
        }

        public PromptService(IAiHandler aiHandler)
        {
            this._aiHandler = (SemanticAiHandler)aiHandler;
        }

        ///// <summary>
        ///// 获取 Prompt 结果
        ///// </summary>
        ///// <param name="promptType"></param>
        ///// <param name="prompt"></param>
        ///// <returns></returns>
        //public async Task<string> GetPromptResultAsync(XncfBuilderPromptType promptType, string prompt)
        //{
        //    //创建一个 Kernel
        //    var iWantToRun = this._aiHandler.IWantTo()
        //        //TODO:model-name 可以使用工厂配置
        //        .ConfigModel(AI.ConfigModel.TextCompletion, "XncfPrompt", "text-davinci-003")
        //        .ConfigModel(AI.ConfigModel.TextEmbedding, "XncfPrompt", "text-embedding-ada-002")
        //        .BuildKernel(b => b.WithMemoryStorage(new VolatileMemoryStore()));

        //    //创建一个请求对象
        //    var request = iWantToRun.CreateRequest(true);
        //    //填充 Prompt
        //    request.RequestContent = GetPrompt(promptType, prompt);
        //    //执行
        //    var result = await iWantToRun.RunAsync(request);
        //    return result.Output;
        //}

        //public async Task<string> GetPromptResultAsync(XncfBuilderPromptType promptType, string input)
        //{
        //    var handler = new SemanticAiHandler();

        //    //定义 AI 接口调用参数和 Token 限制等
        //    var promptParameter = new PromptConfigParameter()
        //    {
        //        MaxTokens = 2000,
        //        Temperature = 0.7,
        //        TopP = 0.5,
        //    };

        //    //准备运行
        //    var userId = "JeffreySu";//区分用户
        //    var modelName = "text-davinci-003";//默认使用模型
        //    var iWantToRun =
        //         handler.IWantTo()
        //               .ConfigModel(AI.ConfigModel.TextCompletion, "XncfPrompt", "text-davinci-003")
        //                .ConfigModel(ConfigModel.TextCompletion, userId, modelName)
        //    .BuildKernel();

        //    //TODO:外部输入
        //    var xncfBuilderPlugin = new XncfBuilderPlugin();
        //    //输入 skill
        //    var skills = iWantToRun.Kernel.ImportSkill(xncfBuilderPlugin);

        //    //var function = iWantToRun.Kernel.Skills.GetFunction(nameof(xncfBuilderPlugin.BuildEntityClass));
        //    var function = skills[nameof(xncfBuilderPlugin.BuildEntityClass)];

        //    //构建请求对象
        //    var request = iWantToRun.CreateRequest(input, true, new[] { function });
        //    //请求
        //    var result = await iWantToRun.RunAsync(request);
        //    return result.Output;
        //}

        /// <summary>
        /// 根据 Plugin 的 Prompt 获取结果
        /// </summary>
        /// <param name="input">用户输入</param>
        /// <param name="skills">所有需要引用的 Skill（Plugin） 和 Function 的清单
        /// <para>Key：Skill Name</para>
        /// <para>Value：Function Name List</para>
        /// </param>
        /// <returns></returns>
        public async Task<string> GetPromptResultAsync(string input, Dictionary<string, List<string>> skills)
        {
            var handler = new SemanticAiHandler();

            //准备运行
            var userId = "XncfBuilder";//区分用户
            var modelName = "text-davinci-003";//默认使用模型
            var iWantToRun =
                 handler.IWantTo()
                        .ConfigModel(ConfigModel.TextCompletion, userId, modelName)
                        .BuildKernel();

            List<ISKFunction> functionPiple = new List<ISKFunction>();
            var pluginDir = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Domain", "PromptPlugins");
            foreach (var skillName in skills)
            {
                var functionResults = iWantToRun.ImportSkillFromDirectory(pluginDir, skillName.Key);

                foreach (var functionName in skillName.Value)
                {
                    functionPiple.Add(functionResults.skillList[functionName]);
                }
            }


            //构建请求对象
            var request = iWantToRun.CreateRequest(input, true, functionPiple.ToArray());
            //请求
            var result = await iWantToRun.RunAsync(request);
            return result.Output;
        }
    }
}

using AutoGen.Core;
using AutoGen.Ollama;
using AutoGen.SemanticKernel;
using AutoGen.SemanticKernel.Extension;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Senparc.AI;
using Senparc.AI.Agents.AgentExtensions;
using Senparc.AI.Agents.AgentUtility;
using Senparc.AI.Entities;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.AI.Kernel.KernelConfigExtensions;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.ACL;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Domain.Services.AIPlugins;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using Senparc.Xncf.XncfBuilder.Domain.Services.Plugins;
using Senparc.Xncf.XncfBuilder.OHS.Local;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Senparc.Xncf.AgentsManager.Domain.Services;

public class ChatGroupService : ServiceBase<ChatGroup>
{

    //临时使用本机线程

    public static List<Task> TaskList = new List<Task>();
    private readonly IBaseObjectCacheStrategy _cache;


    public ChatGroupService(IRepositoryBase<ChatGroup> repo, IServiceProvider serviceProvider, IBaseObjectCacheStrategy cache) : base(repo, serviceProvider)
    {
        this._cache = cache;
    }

    /// <summary>
    /// 运行 ChatGroup（等待运行完成）
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="groupId"></param>
    /// <param name="userCommand"></param>
    /// <param name="senparcAiSetting"></param>
    /// <param name="individuation"></param>
    /// <returns></returns>
    public async Task<IAsyncEnumerable<IMessage>> RunChatGroup(AppServiceLogger logger, int groupId, string userCommand, ISenparcAiSetting senparcAiSetting, bool individuation)
    {
        var chatGroupMemberService = base.GetService<ServiceBase<ChatGroupMember>>();
        var agentTemplateService = base.GetService<AgentsTemplateService>();
        var promptItemService = base.GetService<PromptItemService>();

        var chatGroup = await base.GetObjectAsync(x => x.Id == groupId);
        logger.Append($"开始运行 {chatGroup.Name}");

        var groupMemebers = await chatGroupMemberService.GetFullListAsync(z => z.ChatGroupId == groupId);
        var agentsTemplates = new List<AgentTemplateDto>();
        var agents = new List<SemanticKernelAgent>();
        List<MiddlewareAgent<SemanticKernelAgent>> agentsMiddlewares = new List<MiddlewareAgent<SemanticKernelAgent>>();

        var _semanticAiHandler = new SemanticAiHandler(senparcAiSetting);

        var parameter = new PromptConfigParameter()
        {
            MaxTokens = 2000,
            Temperature = 0.3,
            TopP = 0.3,
        };

        var iWantToRun = _semanticAiHandler.IWantTo(senparcAiSetting)
                        .ConfigModel(ConfigModel.Chat, "JeffreySu")
                        .BuildKernel();

        var kernel = iWantToRun.Kernel;//同一外围 Agent

        //作为唯一入口和汇报的关键人
        AgentTemplate enterAgentTemplate = await agentTemplateService.GetObjectAsync(z => z.Id == chatGroup.EnterAgentTemplateId);

        MiddlewareAgent<SemanticKernelAgent> enterAgent = null;

        foreach (var groupMember in groupMemebers)
        {
            var agentTemplate = await agentTemplateService.GetObjectAsync(x => x.Id == groupMember.AgentTemplateId);
            var agentTemplateDto = agentTemplateService.Mapper.Map<AgentTemplateDto>(agentTemplate);
            agentsTemplates.Add(agentTemplateDto);

            //TODO：确认 Prompt 此时是否存在，如果不存在需要给出提示

            var promptResult = await promptItemService.GetWithVersionAsync(agentTemplate.PromptCode, isAvg: true);

            var itemKernel = kernel;

            if (individuation)
            {
                var semanticAiHandler = new SemanticAiHandler(promptResult.SenparcAiSetting);
                var iWantToRunItem = semanticAiHandler.IWantTo(senparcAiSetting)
                            .ConfigModel(ConfigModel.Chat, agentTemplate.Name + groupMember.UID)
                            .BuildKernel();
                itemKernel = iWantToRunItem.Kernel;
            }

            var agent = new SemanticKernelAgent(
                        kernel: itemKernel,
                        name: agentTemplate.Name,
                        systemMessage: promptResult.PromptItem.Content);

            var agentMiddleware =
                agent
                    .RegisterTextMessageConnector()
                    //.RegisterMiddleware(async (messages, ct) =>
                    //{
                    //    return messages;
                    //})
                    .RegisterCustomPrintMessage(new PrintWechatMessageMiddleware((a, m, mStr) =>
                    {
                        PrintWechatMessageMiddlewareExtension.SendWechatMessage.Invoke(a, m, mStr, agentTemplateDto);
                        logger.Append($"[{chatGroup.Name}]组 {a.Name} 发送消息：{mStr}");
                    }));


            if (groupMember.AgentTemplateId == enterAgentTemplate.Id)
            {
                //TODO：添加指定入口对接人员，参考群主
                enterAgentTemplate = agentTemplate;
                enterAgent = agentMiddleware;
            }

            agentsMiddlewares.Add(agentMiddleware);

            agents.Add(agent);
        }

        // Create the hearing member
        //var hearingMember = new UserProxyAgent(name: chatGroup.Name + "群友");
        var hearingMember = new DefaultReplyAgent(name: chatGroup.Name + "群友", GroupChatExtension.TERMINATE);

        // Create the group admin
        var adminAgenttemplate = await agentTemplateService.GetObjectAsync(x => x.Id == chatGroup.AdminAgentTemplateId);
        var adminPromptResult = await promptItemService.GetWithVersionAsync(adminAgenttemplate.PromptCode, isAvg: true);
        var adminKernel = kernel;
        if (individuation)
        {
            var semanticAiHandler = new SemanticAiHandler(adminPromptResult.SenparcAiSetting);
            var iWantToRunItem = semanticAiHandler.IWantTo(senparcAiSetting)
                        .ConfigModel(ConfigModel.Chat, adminAgenttemplate.Name)
                        .BuildKernel();
            adminKernel = iWantToRunItem.Kernel;
        }

        var admin = new SemanticKernelAgent(
            kernel: kernel,
            name: adminAgenttemplate.Name,
            systemMessage: adminPromptResult.PromptItem.Content)
            .RegisterTextMessageConnector();


        var graphConnector = GraphBuilder.Start()
                    .ConnectFrom(hearingMember).TwoWay(enterAgent);

        //遍历所有 agents, 两两之间运行 graphConnector.ConnectFrom(agent1).TwoWay(agent2);

        for (int i = 0; i < agentsMiddlewares.Count; i++)
        {
            for (int j = i + 1; j < agentsMiddlewares.Count; j++)
            {
                graphConnector.ConnectFrom(agentsMiddlewares[i]).TwoWay(agentsMiddlewares[j]);
            }
        }


        var finishedGraph = graphConnector.Finish();

        admin = admin.RegisterMiddleware(async (messages, option, next, ct) =>
        {
            var response = await next.GenerateReplyAsync(messages, option, ct);

            // check response's format
            // if the response's format is not From xxx where xxx is a valid group member
            // use reflection to get it auto-fixed by LLM

            var responseContent = response.GetContent();
            if (responseContent?.StartsWith("From") is false)
            {
                // random pick from agents
                var agent = new Random().Next(0, agents.Count);

                return new TextMessage(Role.User, $"From {agents[agent].Name}", from: next.Name);
            }
            else
            {
                return response;
            }
        });

        var aiTeam = finishedGraph.CreateAiTeam(admin);

        try
        {
            var greetingMessage = await enterAgent.SendAsync($"你好，如果已经就绪，请告诉我们“已就位”，并和 {hearingMember.Name} 打个招呼");

            var commandMessage = new TextMessage(Role.Assistant, userCommand, hearingMember.Name);

            var result = aiTeam.SendAsync(chatHistory: [greetingMessage, commandMessage],
      maxRound: 20);

            //await foreach (var message in )
            //{
            //    // process exit
            //    if (message.GetContent()?.Contains("exit") is true)
            //    {
            //        //Console.WriteLine("您已推出对话");
            //        //return;
            //    }
            //}

            ////IEnumerable<IMessage> result = await enterAgent.SendMessageToGroupAsync(
            ////      groupChat: aiTeam,
            ////      chatHistory: [greetingMessage, commandMessage],
            ////      maxRound: 10);

            //Console.WriteLine("Chat finished.");
            //logger.Append("已完成运行：" + chatGroup.Name);

            return result;
        }
        catch (Exception ex)
        {
            SenparcTrace.BaseExceptionLog(ex);
            throw;
        }
    }

    /// <summary>
    /// 在独立进程中运行 ChatGroup（UI 界面中进行）
    /// </summary>
    /// <returns></returns>
    public Task RunChatGroupInThread(ChatGroup_RunGroupRequest request)
    {
        var task = Task.Factory.StartNew(async () =>
        {
            //base.ServiceProvider = base._serviceProvider;
            var scope = Senparc.CO2NET.SenparcDI.GetServiceProvider(true).CreateScope(); //base.ServiceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var groupId = request.ChatGroupId;
            var aiModelId = request.AiModelId;
            var personality = request.Personality;
            var userCommand = request.PromptCommand;

            var logger = new StringBuilder();

            var chatGroupMemberService = services.GetService<ChatGroupMemberService>();
            var agentTemplateService = services.GetService<AgentsTemplateService>();
            var promptItemService = services.GetService<PromptItemService>();
            var chatTaskService = services.GetService<ChatTaskService>();

            var chatGroupService = services.GetService<ChatGroupService>();

            var chatGroup = await chatGroupService.GetObjectAsync(x => x.Id == groupId);
            var chatGroupDto = chatGroupService.Mapping<ChatGroupDto>(chatGroup);
            var chatTaskDto = new ChatTaskDto(request.Name, groupId, aiModelId, ChatTask_Status.Waiting,
                userCommand, request.Description, personality, request.HookPlatform, request.HookParameter, false,
                DateTime.Now, DateTime.Now, null);
            var chatTask = await chatTaskService.CreateTask(chatTaskDto);
            chatTaskDto = chatTaskService.Mapping<ChatTaskDto>(chatTask);//更新
                                                                         //更新状态
            await chatTaskService.SetStatus(ChatTask_Status.Chatting, chatTask);

            //运行中进行缓存
            var runningKey = chatTaskService.GetChatTaskRunCacheKey(chatTask.Id);
            var cacheTask = new RunningChatTaskDto()
            {
                ChatTaskDto = chatTaskDto
            };
            await _cache.SetAsync(runningKey, cacheTask);

            logger.Append($"开始运行 {chatGroup.Name}");

            #region 确定 AiSetting

            var senparcAiSetting = Senparc.AI.Config.SenparcAiSetting;
            var aiModelService = services.GetRequiredService<AIModelService>();
            if (aiModelId != 0)
            {
                var aiModel = await aiModelService.GetObjectAsync(z => z.Id == aiModelId);
                if (aiModel == null)
                {
                    throw new NcfExceptionBase($"当前选择的 AI 模型不存在：{aiModelId}");
                }

                var aiModelDto = aiModelService.Mapper.Map<AIModelDto>(aiModel);

                senparcAiSetting = aiModelService.BuildSenparcAiSetting(aiModelDto);
            }

            #endregion

            #region 确定默认公共使用的模型
            var _semanticAiHandler = new SemanticAiHandler(senparcAiSetting);

            var parameter = new PromptConfigParameter()
            {
                MaxTokens = 2000,
                Temperature = 0.3,
                TopP = 0.3,
            };

            //全局默认模型
            var iWantToRunGlobal = _semanticAiHandler.IWantTo(senparcAiSetting)
                            .ConfigModel(ConfigModel.Chat, "JeffreySu")
                            .BuildKernel();
            #endregion

            #region 收集所有对话人员

            List<(int AgentTemplateId, string Uid)> memberCollection = new();

            var groupMemebers = await chatGroupMemberService.GetFullListAsync(z => z.ChatGroupId == groupId);
            foreach (var member in groupMemebers)
            {
                memberCollection.Add((member.AgentTemplateId, member.UID));
            }


            //确保已添加 Admin 和 Enter Agent
            if (groupMemebers.All(z => z.AgentTemplateId != chatGroup.AdminAgentTemplateId))
            {
                memberCollection.Add((chatGroup.AdminAgentTemplateId, "Admin"));
            }

            if (groupMemebers.All(z => z.AgentTemplateId != chatGroup.EnterAgentTemplateId))
            {
                memberCollection.Add((chatGroup.EnterAgentTemplateId, "Enter"));
            }

            var agentsTemplates = new List<AgentTemplateDto>();
            var agents = new List<SemanticKernelAgent>();
            List<MiddlewareAgent<SemanticKernelAgent>> agentsMiddlewares = new List<MiddlewareAgent<SemanticKernelAgent>>();

            IWantToRun iWantToRunAdmin = null;// Admin Agent 配置
            IWantToRun iWantToRunEnter = null;// Enter Agent 配置
            MiddlewareAgent<SemanticKernelAgent> enterAgent = null;// Enter Agent 中间件对象

            #endregion

            var aiPlugins = AIPluginHub.Instance;// Function Call 全局对象集合

            //遍历每一个成员
            foreach (var memberInfo in memberCollection)
            {
                var agentTemplateId = memberInfo.AgentTemplateId;
                var uid = memberInfo.Uid;

                var agentTemplate = await agentTemplateService.GetObjectAsync(x => x.Id == agentTemplateId);//TODO: + .ToDto<>()
                if (!agentTemplate.Enable)
                {
                    logger.AppendLine($"智能体【{agentTemplate.Name}】目前为关闭状态，跳过对话");
                    //不启用的智能体不参与对话
                    continue;
                }

                var agentTemplateDto = agentTemplateService.Mapper.Map<AgentTemplateDto>(agentTemplate);
                agentsTemplates.Add(agentTemplateDto);

                var isPromptCodeVersion = PromptItem.IsPromptVersion(agentTemplateDto.PromptCode);
                var agentSystemMessagePrompt = string.Empty;
                ISenparcAiSetting currentAgentAiSetting = null;

                if (isPromptCodeVersion)
                {
                    var promptResult = await promptItemService.GetWithVersionAsync(agentTemplate.PromptCode, isAvg: true);
                    agentSystemMessagePrompt = promptResult?.PromptItem.Content;
                    currentAgentAiSetting = promptResult.SenparcAiSetting;
                }
                else
                {
                    agentSystemMessagePrompt = agentTemplateDto.PromptCode;
                    currentAgentAiSetting = senparcAiSetting;
                }

                IWantToConfig iWantToConfig = null;//当前 Agent 配置

                //判断是否需要个性化模型参数
                if (personality)
                {
                    //使用个性化参数创建
                    var personalitySemanticAiHandler = new SemanticAiHandler(currentAgentAiSetting);
                    iWantToConfig = personalitySemanticAiHandler.IWantTo();
                }
                else
                {
                    iWantToConfig = _semanticAiHandler.IWantTo(senparcAiSetting);
                }

                //当前 Agent 配置
                var iWantToRunItem = iWantToConfig
                                   .ConfigModel(ConfigModel.Chat, agentTemplateDto.Name + uid)
                                   .BuildKernel();

                //判断是否需要 Function Call

                var hasFunctionCalls = agentTemplateDto.FunctionCallNames.IsNullOrEmpty()
                                        ? Array.Empty<string>()
                                        : agentTemplateDto.FunctionCallNames.Split(',');

                if (hasFunctionCalls.Length > 0)
                {
                    //添加 Plugins
                    foreach (var functionCall in hasFunctionCalls)
                    {
                        try
                        {
                            var functionCallType = aiPlugins.GetPluginType(functionCall, true);

                            if (functionCallType == null)
                            {
                                throw new NcfExceptionBase($"导入 Plugin 失败，FunctionCall 名称不存在：{functionCall}");
                            }

                            var functionName = functionCallType.Name;
                            var plugin = services.GetService(functionCallType);
                            var kernelPlugin = iWantToRunItem.ImportPluginFromObject(plugin, pluginName: functionName).kernelPlugin;
                            //KernelFunction[] functionPiple = new[] { kernelPlugin[nameof(crawlPlugin.Crawl)] };
                            //iWantToRunItem.ImportPluginFromFunctions("CrawlPlugins", functionPiple);
                        }
                        catch (Exception ex)
                        {
                            SenparcTrace.SendCustomLog("导入 Plugin 失败", ex.Message);
                            Console.WriteLine(ex);
                        }
                    }
                }

                //设置 Agent
                SemanticKernelAgent agent = new SemanticKernelAgent(
                                kernel: iWantToRunItem.Kernel,
                                name: agentTemplate.Name,
                                systemMessage: agentSystemMessagePrompt);

                var agentMiddleware = agent
                    .RegisterTextMessageConnector()
                    .RegisterCustomPrintMessage(
                    new PrintWechatMessageMiddleware(async (a, m, mStr) =>
                    {
                        try
                        {
                            AgentTemplatePrintMessageMiddleware.SendWechatMessage
                                .Invoke(a, m, mStr, agentTemplateDto, chatGroupDto, chatTaskDto);
                        }
                        catch (Exception ex)
                        {
                            SenparcTrace.SendCustomLog("SendWechatMessage 发生异常", ex.Message);
                        }

                        //PrintWechatMessageMiddlewareExtension.SendWechatMessage.Invoke(a, m, mStr, agentTemplateDto);
                        logger.Append($"[{chatGroup.Name}]组 {a.Name} 发送消息：{mStr}");

                        //using (var scope = ServiceProvider.CreateScope())//已关闭
                        using (var scope = Senparc.CO2NET.SenparcDI.GetServiceProvider().CreateScope())
                        {
                            var serviceProvider = scope.ServiceProvider;
                            var chatGroupHistoryService = serviceProvider.GetService<ChatGroupHistoryService>();
                            var chatGroupHistoryDto = new ChatGroupHistoryDto(chatGroupDto.Id, chatTaskDto.Id, null, agentTemplateDto.Id, null, agentTemplateDto.Id, null, mStr, MessageType.Text, Status.Finished);
                            await chatGroupHistoryService.CreateHistory(chatGroupHistoryDto);
                        }
                    }));

                if (agentTemplateId == chatGroup.EnterAgentTemplateId)
                {
                    //TODO：添加指定入口对接人员，参考群主
                    enterAgent = agentMiddleware;
                    iWantToRunEnter = iWantToRunItem;
                }

                if (agentTemplateId == chatGroup.AdminAgentTemplateId)
                {
                    iWantToRunAdmin = iWantToRunItem;
                }

                agentsMiddlewares.Add(agentMiddleware);
                //}


                agents.Add(agent);
            }

            // Create the hearing member
            //var hearingMember = new UserProxyAgent(name: chatGroup.Name + "群友");
            var hearingMember = new DefaultReplyAgent(name: chatGroup.Name + "群友", GroupChatExtension.TERMINATE);

            // Create the group admin
            var adminAgenttemplate = await agentTemplateService.GetObjectAsync(x => x.Id == chatGroup.AdminAgentTemplateId);
            var adminPromptResult = await promptItemService.GetWithVersionAsync(adminAgenttemplate.PromptCode, isAvg: true);

            if (personality)
            {
                var semanticAiHandler = new SemanticAiHandler(adminPromptResult.SenparcAiSetting);
                var iWantToRunItem = semanticAiHandler.IWantTo(senparcAiSetting)
                            .ConfigModel(ConfigModel.Chat, adminAgenttemplate.Name)
                            .BuildKernel();
            }

            var admin = new SemanticKernelAgent(
                kernel: iWantToRunAdmin.Kernel,
                name: "admin"/*adminAgenttemplate.Name*//*,
                    systemMessage: adminPromptResult.PromptItem.Content*/)
                .RegisterMessageConnector();
            //.RegisterTextMessageConnector();


            //var admin1 = admin.RegisterMiddleware(async (messages, option, next, ct) =>
            // {
            //     var response = await next.GenerateReplyAsync(messages, option, ct);

            //     // check response's format
            //     // if the response's format is not From xxx where xxx is a valid group member
            //     // use reflection to get it auto-fixed by LLM

            //     var responseContent = response.GetContent();
            //     if (responseContent?.StartsWith("From") is false)
            //     {
            //         // random pick from agents
            //         var agent = new Random().Next(0, agents.Count);

            //         return new TextMessage(Role.User, $"From {agents[agent].Name}", from: next.Name);
            //     }
            //     else
            //     {
            //         return response;
            //     }
            // });

            var graphConnector = GraphBuilder.Start()
                        .ConnectFrom(hearingMember).TwoWay(enterAgent);

            //遍历所有 agents, 两两之间运行 graphConnector.ConnectFrom(agent1).TwoWay(agent2);
            for (int i = 0; i < agentsMiddlewares.Count; i++)
            {
                for (int j = i + 1; j < agentsMiddlewares.Count; j++)
                {
                    graphConnector.ConnectFrom(agentsMiddlewares[i]).TwoWay(agentsMiddlewares[j]);
                }
            }

            var finishedGraph = graphConnector.Finish();

            var aiTeam = finishedGraph.CreateAiTeam(admin);

            try
            {
                var greetingMessage = await enterAgent.SendAsync($"你好，如果已经就绪，请告诉我们“已就位”，并和 {hearingMember.Name} 打个招呼");

                var commandMessage = new TextMessage(Role.Assistant, userCommand, hearingMember.Name);

                //IEnumerable<IMessage> result = await enterAgent.SendMessageToGroupAsync(
                //      groupChat: aiTeam,
                //      chatHistory: [greetingMessage, commandMessage],
                //      maxRound: 10);

                await foreach (var message in aiTeam.SendAsync(chatHistory: [greetingMessage, commandMessage],
            maxRound: 20))
                {
                    // process exit
                    if (message.GetContent()?.Contains("exit") is true)
                    {
                        //Console.WriteLine("您已推出对话");
                        return;
                    }
                }

                Console.WriteLine("Chat finished.");
                logger.Append("已完成运行：" + chatGroup.Name);

                await chatTaskService.SetStatus(ChatTask_Status.Finished, chatTask);

                //完成后移除缓存
                await _cache.RemoveFromCacheAsync(runningKey);

                SenparcTrace.SendCustomLog($"Agents 运行结果（组：{chatGroup.Name}）", logger.ToString());

                //return result;
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
                SenparcTrace.SendCustomLog("异常详情", ex.StackTrace);
                Console.WriteLine(ex);
                throw;
            }
            finally
            {
                scope.Dispose();
            }
        });

        TaskList.Add(task);

        return Task.CompletedTask;
    }
}
using AutoGen.Core;
using AutoGen.SemanticKernel;
using AutoGen.SemanticKernel.Extension;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using Senparc.AI;
using Senparc.AI.Agents.AgentExtensions;
using Senparc.AI.Agents.AgentUtility;
using Senparc.AI.Entities;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
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
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Domain.Models.DatabaseModel;
using Senparc.Xncf.PromptRange.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services;


public class McpEndpoint
{
    public string url { get; set; }
}

public class ChatGroupService : ServiceBase<ChatGroup>
{

    //Temporarily use native threads

    public static int ChatMaxRound = 20;
    public static List<Task> TaskList = new List<Task>();
    private readonly IBaseObjectCacheStrategy _cache;


    public ChatGroupService(IRepositoryBase<ChatGroup> repo, IServiceProvider serviceProvider, IBaseObjectCacheStrategy cache) : base(repo, serviceProvider)
    {
        this._cache = cache;
    }

    /// <summary>
    ///Run ChatGroup (wait for the run to complete)
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

        var kernel = iWantToRun.Kernel;//The same peripheral Agent

        //As the sole point of entry and reporting
        AgentTemplate enterAgentTemplate = await agentTemplateService.GetObjectAsync(z => z.Id == chatGroup.EnterAgentTemplateId);

        MiddlewareAgent<SemanticKernelAgent> enterAgent = null;

        foreach (var groupMember in groupMemebers)
        {
            var agentTemplate = await agentTemplateService.GetObjectAsync(x => x.Id == groupMember.AgentTemplateId);
            var agentTemplateDto = agentTemplateService.Mapper.Map<AgentTemplateDto>(agentTemplate);
            agentsTemplates.Add(agentTemplateDto);

            //TODO: Confirm whether Prompt exists at this time. If it does not exist, you need to give a prompt.

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
                //TODO: Add designated entrance docking personnel, refer to the group owner
                enterAgentTemplate = agentTemplate;
                enterAgent = agentMiddleware;
            }

            agentsMiddlewares.Add(agentMiddleware);

            agents.Add(agent);
        }

        // Create the hearing member
        //var hearingMember = new UserProxyAgent(name: chatGroup.Name + "Group Friends");
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

        //Traverse all agents, running graphConnector.ConnectFrom(agent1).TwoWay(agent2);

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
            //        //Console.WriteLine("You have exited the conversation");
            //        //return;
            //    }
            //}

            ////IEnumerable<IMessage> result = await enterAgent.SendMessageToGroupAsync(
            ////      groupChat: aiTeam,
            ////      chatHistory: [greetingMessage, commandMessage],
            ////      maxRound: 10);

            //Console.WriteLine("Chat finished.");
            //logger.Append("Completed running: " + chatGroup.Name);

            return result;
        }
        catch (Exception ex)
        {
            SenparcTrace.BaseExceptionLog(ex);
            throw;
        }
    }

    /// <summary>
    /// Run ChatGroup in a separate process (in the UI interface, without waiting for completion)
    /// </summary>
    public Task RunChatGroupInThread(ChatGroup_RunGroupRequest request)
    {
        var task = RunChatGroupExecutionCoreAsync(request);
        TaskList.Add(task);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Run ChatGroup until the end of this round of dialogue (used for Prompt optimization and other scenarios that require synchronous waiting)
    /// </summary>
    public Task RunChatGroupAwaitAsync(ChatGroup_RunGroupRequest request)
    {
        return RunChatGroupExecutionCoreAsync(request);
    }

    private async Task RunChatGroupExecutionCoreAsync(ChatGroup_RunGroupRequest request)
    {
            IDisposable activeOptimizationScope = null;
            if (!string.IsNullOrWhiteSpace(request.CorrelationId))
            {
                activeOptimizationScope = PromptOptimizationAgentBridge.BeginActiveRequestScope(request.CorrelationId);
                PromptOptimizationAgentBridge.SetFallbackCorrelationId(request.CorrelationId);
            }

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
            chatTaskDto = chatTaskService.Mapping<ChatTaskDto>(chatTask);//renew
                                                                         //update status
            await chatTaskService.SetStatus(ChatTask_Status.Chatting, chatTask);

            //Caching on the fly
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

            //Global default model
            var iWantToRunGlobal = _semanticAiHandler.IWantTo(senparcAiSetting)
                            .ConfigModel(ConfigModel.Chat, "JeffreySu")
                            .BuildKernel();
            #endregion

            #region 收集所有对话人员

            List<(int AgentTemplateId, string Uid)> memberCollection = new();

            var groupMemebers = await chatGroupMemberService.GetFullListAsync(z => z.ChatGroupId == groupId, includes: "AgentTemplate");
            foreach (var member in groupMemebers)
            {
                if (member.AgentTemplate.Enable is false)
                {
                    Console.WriteLine($"{member.AgentTemplate.Name} 已被禁用");
                    continue;
                }
                memberCollection.Add((member.AgentTemplateId, member.UID));
            }


            //Make sure Admin and Enter Agent are added
            if (groupMemebers.All(z => z.AgentTemplateId != chatGroup.AdminAgentTemplateId))
            {
                memberCollection.Add((chatGroup.AdminAgentTemplateId, "Admin"));
            }

            if (groupMemebers.All(z => z.AgentTemplateId != chatGroup.EnterAgentTemplateId))
            {
                memberCollection.Add((chatGroup.EnterAgentTemplateId, "Enter"));
            }

            // Only one of the same AgentTemplate is retained (to prevent the Admin/Enter/member from appearing twice in the same model and parallel tool calls to produce repeated side effects)
            memberCollection = memberCollection
                .GroupBy(m => m.AgentTemplateId)
                .Select(g => g.First())
                .ToList();

            var agentsTemplates = new List<AgentTemplateDto>();
            var agents = new List<SemanticKernelAgent>();
            List<MiddlewareAgent<SemanticKernelAgent>> agentsMiddlewares = new List<MiddlewareAgent<SemanticKernelAgent>>();

            IWantToRun iWantToRunAdmin = null;// Admin Agent configuration
            IWantToRun iWantToRunEnter = null;// Enter Agent Configuration
            MiddlewareAgent<SemanticKernelAgent> enterAgent = null;// Enter Agent middleware object

            #endregion

            var aiPlugins = AIPluginHub.Instance;// Function Call global object collection

            //Iterate through each member
            foreach (var memberInfo in memberCollection)
            {
                var agentTemplateId = memberInfo.AgentTemplateId;
                var uid = memberInfo.Uid;

                var agentTemplate = await agentTemplateService.GetObjectAsync(x => x.Id == agentTemplateId);//TODO: + .ToDto<>()
                if (!agentTemplate.Enable)
                {
                    logger.AppendLine($"智能体【{agentTemplate.Name}】目前为关闭状态，跳过对话");
                    //Agents that are not enabled do not participate in the conversation
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

                IWantToConfig iWantToConfig = null;//Current Agent configuration

                //Determine whether personalized model parameters are needed
                if (personality)
                {
                    //Created with personalization parameters
                    var personalitySemanticAiHandler = new SemanticAiHandler(currentAgentAiSetting);
                    iWantToConfig = personalitySemanticAiHandler.IWantTo();

                }
                else
                {
                    iWantToConfig = _semanticAiHandler.IWantTo(senparcAiSetting);
                }

                //Current Agent configuration

                #region 设置 MCP

                var iWantToConfigModel = iWantToConfig.ConfigModel(ConfigModel.Chat, agentTemplateDto.Name + uid);

                // Get the MCP Endpoints of the current Agent
                var mcpEndpoints = agentTemplateDto.McpEndpoints;
                if (!string.IsNullOrEmpty(mcpEndpoints))
                {
                    var endpointsDict = JsonSerializer.Deserialize<Dictionary<string, McpEndpoint>>(mcpEndpoints);

                    // Iterate over each key-value pair in endpointsDict
                    foreach (var endpoint in endpointsDict)
                    {
                        // Get key and value
                        var mcpName = endpoint.Key;

                        var mcpEndpoint = endpoint.Value.url;

                        var clientTransport = new SseClientTransport(new SseClientTransportOptions()
                        {
                            Endpoint = new Uri(mcpEndpoint),
                            Name = mcpName
                        });

                        IList<McpClientTool> tools = new List<McpClientTool>();

                        try
                        {
                            var client = await McpClientFactory.CreateAsync(clientTransport);
                            tools = await client.ListToolsAsync();
                            // Print the list of tools available from the server.
                            foreach (var tool in tools)
                            {
                                Console.WriteLine($"Agent: {memberInfo.AgentTemplateId} MCP: {mcpName} : {tool.Name} ({tool.Description})");
                            }
                        }
                        catch (Exception ex)
                        {
                            SenparcTrace.BaseExceptionLog(ex);
                        }
                      
                        // Operate with key and value
#pragma warning disable SKEXP0001 // Types are for evaluation only and may be changed or removed in future updates. Cancel this diagnostic to continue.
                        iWantToConfigModel.IWantTo.KernelBuilder.Plugins.AddFromFunctions($"SenparcMcp{memberInfo.AgentTemplateId}", tools.Select(z => z.AsKernelFunction()));
#pragma warning restore SKEXP0001 // Types are for evaluation only and may be changed or removed in future updates. Cancel this diagnostic to continue.
                    }
                }
                #endregion


                var iWantToRunItem = iWantToConfigModel.BuildKernel();


                var executionSettings2 = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()// FunctionChoiceBehavior.Auto()
                };
                var ka = new KernelArguments(executionSettings2) { };

                // iWantToRunItem.Kernel.Data["Arguments"] = ka;
                // iWantToRunItem.Kernel.Data["Argument"] = ka;
                // iWantToRunItem.Kernel.Data["KernelArguments"] = ka;
                // iWantToRunItem.Kernel.Data["KernelArgument"] = ka;


                #region Function-calling

                //Determine whether Function Call is needed

                var hasFunctionCalls = agentTemplateDto.FunctionCallNames.IsNullOrEmpty()
                                        ? Array.Empty<string>()
                                        : agentTemplateDto.FunctionCallNames.Split(',');

                if (hasFunctionCalls.Length > 0)
                {
                    //Add Plugins
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

                #endregion

                //Setup Agent
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

                        //using (var scope = ServiceProvider.CreateScope())//Close
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
                    //TODO: Add designated entrance docking personnel, refer to the group owner
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
            //var hearingMember = new UserProxyAgent(name: chatGroup.Name + "Group Friends");
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

            var graphConnector = GraphBuilder.Start()
                        .ConnectFrom(hearingMember).TwoWay(enterAgent);

            //Traverse all agents, running graphConnector.ConnectFrom(agent1).TwoWay(agent2);
            //for (int i = 0; i < agentsMiddlewares.Count; i++)
            //{
            //    for (int j = i + 1; j < agentsMiddlewares.Count; j++)
            //    {
            //        graphConnector.ConnectFrom(agentsMiddlewares[i]).TwoWay(agentsMiddlewares[j]);
            //    }
            //}

            //Use star network
            for (int i = 0; i < agentsMiddlewares.Count; i++)
            {
                var agentMiddleware = agentsMiddlewares[i];
                if (enterAgent == agentMiddleware)
                {
                    continue;
                }
                graphConnector.ConnectFrom(enterAgent).TwoWay(agentMiddleware);
            }


            var finishedGraph = graphConnector.Finish();

            #region 定义 Admin

            var admin = new SemanticKernelAgent(
                kernel: iWantToRunAdmin.Kernel,
                name: "admin",
                systemMessage: @$"You are the administrator and are responsible for managing the conversations in the ChatGroup. However, you cannot participate in any conversation work and cannot respond to any requests from other agents, but determine witch agent can speek in the next round.
You are strictly fobidden to use function-calling, include _tool_use.parallel.

You have to strictly follow the reply format (JSON) as required, each message will use the strickly JSON format with a '//finish suffix':
{{""Speaker"":""<From Agent Name>"", ""Message"":""<Chat Message>""}}//finish

e,g:
{{Speaker:""{{agentNames.First()}}"", Message:""Hi, I'm {{agentNames.First()}}.""}}//finish

Note: parameter From must be strictly equal to the name of the player spokesperson and cannot be modified in any way.
"
                                    /*adminAgenttemplate.Name*//*,
                                        systemMessage: adminPromptResult.PromptItem.Content*/)
                .RegisterMessageConnector();
            //.RegisterTextMessageConnector();


            var admin1 = admin.RegisterMiddleware(async (messages, option, next, ct) =>
            {
                var response = await next.GenerateReplyAsync(messages, option, ct);

                // check response's format
                // if the response's format is not From xxx where xxx is a valid group member
                // use reflection to get it auto-fixed by LLM

                var responseContent = response.GetContent();

                Console.WriteLine($"\t response from admin: {responseContent}");

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

            #endregion

            //var aiTeam = finishedGraph.CreateAiTeam(admin);
            var myRoleOrc = new MyRolePlayOrchestrator(admin, finishedGraph.Graph);
            var aiTeam = finishedGraph.CreateAiTeam(admin1, myRoleOrc);

            try
            {
                var greetingMessage = await enterAgent.SendAsync($"你好，如果已经就绪，请告诉我们“已就位”，并和 {hearingMember.Name} 打个招呼。打招呼请使用用户要求的语言，默认为英文。");

                var commandMessage = new TextMessage(Role.Assistant, userCommand, hearingMember.Name);

                //IEnumerable<IMessage> result = await enterAgent.SendMessageToGroupAsync(
                //      groupChat: aiTeam,
                //      chatHistory: [greetingMessage, commandMessage],
                //      maxRound: 10);

                await foreach (var message in aiTeam.SendAsync(chatHistory: [greetingMessage, commandMessage],
            maxRound: ChatMaxRound))
                {
                    // process exit
                    if (message.GetContent()?.Contains("exit") is true)
                    {
                        //Console.WriteLine("You have exited the conversation");
                        return;
                    }
                }

                Console.WriteLine("Chat finished.");
                logger.Append("已完成运行：" + chatGroup.Name);

                await chatTaskService.SetStatus(ChatTask_Status.Finished, chatTask);

                //Remove cache when finished
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
                activeOptimizationScope?.Dispose();
                if (!string.IsNullOrWhiteSpace(request.CorrelationId))
                {
                    PromptOptimizationAgentBridge.ClearFallbackCorrelationId();
                }
            }
    }
}
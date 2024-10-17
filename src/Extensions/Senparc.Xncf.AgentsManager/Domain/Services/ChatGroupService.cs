using AutoGen.Core;
using AutoGen.SemanticKernel;
using Microsoft.Extensions.DependencyInjection;
using Senaprc.AI.Agents.AgentExtensions;
using Senaprc.AI.Agents.AgentUtility;
using Senparc.AI;
using Senparc.AI.Entities;
using Senparc.AI.Interfaces;
using Senparc.AI.Kernel;
using Senparc.AI.Kernel.Handlers;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Trace;
using Senparc.Ncf.Core.AppServices;
using Senparc.Ncf.Core.Exceptions;
using Senparc.Ncf.Repository;
using Senparc.Ncf.Service;
using Senparc.Xncf.AgentsManager.ACL;
using Senparc.Xncf.AgentsManager.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models;
using Senparc.Xncf.AgentsManager.Models.DatabaseModel.Models.Dto;
using Senparc.Xncf.AgentsManager.OHS.Local.PL;
using Senparc.Xncf.AIKernel.Domain.Models.DatabaseModel.Dto;
using Senparc.Xncf.AIKernel.Domain.Services;
using Senparc.Xncf.PromptRange.Domain.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Senparc.Xncf.AgentsManager.Domain.Services
{
    public class ChatGroupService : ServiceBase<ChatGroup>
    {

        //临时使用本机线程

        public static List<Task> TaskList = new List<Task>();
        private readonly IBaseObjectCacheStrategy _cache;


        public ChatGroupService(IRepositoryBase<ChatGroup> repo, IServiceProvider serviceProvider,IBaseObjectCacheStrategy cache) : base(repo, serviceProvider)
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
        public async Task<IEnumerable<IMessage>> RunChatGroup(AppServiceLogger logger, int groupId, string userCommand, ISenparcAiSetting senparcAiSetting, bool individuation)
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
                Temperature = 0.7,
                TopP = 0.5,
            };

            var iWantToRun = _semanticAiHandler.IWantTo(senparcAiSetting)
                            .ConfigModel(ConfigModel.Chat, "JeffreySu")
                            .BuildKernel();

            var kernel = iWantToRun.Kernel;//同一外围 Agent

            //作为唯一入口和汇报的关键人（TODO：需要增加一个设置）
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
                var agentMiddleware = agent
                            .RegisterTextMessageConnector()
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

            var aiTeam = finishedGraph.CreateAiTeam(admin);

            try
            {
                var greetingMessage = await enterAgent.SendAsync($"你好，如果已经就绪，请告诉我们“已就位”，并和 {hearingMember.Name} 打个招呼");

                var commandMessage = new TextMessage(Role.Assistant, userCommand, hearingMember.Name);

                IEnumerable<IMessage> result = await enterAgent.SendMessageToGroupAsync(
                      groupChat: aiTeam,
                      chatHistory: [greetingMessage, commandMessage],
                      maxRound: 10);

                Console.WriteLine("Chat finished.");
                logger.Append("已完成运行：" + chatGroup.Name);

                return result;
            }
            catch (Exception ex)
            {
                SenparcTrace.BaseExceptionLog(ex);
                throw;
            }
        }

        /// <summary>
        /// 在独立进程中运行 ChatGroup
        /// </summary>
        /// <returns></returns>
        public async Task RunChatGroupInThread(ChatGroup_RunGroupRequest request)
        {
            var task = Task.Factory.StartNew(async () =>
            {
                //base.ServiceProvider = base._serviceProvider;
                var scope = Senparc.CO2NET.SenparcDI.GetServiceProvider().CreateScope(); //base.ServiceProvider.CreateScope();
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
                var chatTaskDto = new ChatTaskDto(request.Name, groupId, aiModelId, Models.DatabaseModel.ChatTask_Status.Waiting,
                    userCommand, request.Description, personality, request.HookPlatform, request.HookParameter, false,
                    DateTime.Now, DateTime.Now, null);
                var chatTask = await chatTaskService.CreateTask(chatTaskDto);
                chatTaskDto = chatTaskService.Mapping<ChatTaskDto>(chatTask);//更新
                //更新状态
                await chatTaskService.SetStatus(Models.DatabaseModel.ChatTask_Status.Chatting, chatTask);

                await _cache.Set("")

                logger.Append($"开始运行 {chatGroup.Name}");

                var groupMemebers = await chatGroupMemberService.GetFullListAsync(z => z.ChatGroupId == groupId);
                var agentsTemplates = new List<AgentTemplateDto>();
                var agents = new List<SemanticKernelAgent>();
                List<MiddlewareAgent<SemanticKernelAgent>> agentsMiddlewares = new List<MiddlewareAgent<SemanticKernelAgent>>();

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

                var _semanticAiHandler = new SemanticAiHandler(senparcAiSetting);

                var parameter = new PromptConfigParameter()
                {
                    MaxTokens = 2000,
                    Temperature = 0.7,
                    TopP = 0.5,
                };

                var iWantToRun = _semanticAiHandler.IWantTo(senparcAiSetting)
                                .ConfigModel(ConfigModel.Chat, "JeffreySu")
                                .BuildKernel();

                var kernel = iWantToRun.Kernel;//同一外围 Agent

                //作为唯一入口和汇报的关键人（TODO：需要增加一个设置）
                AgentTemplate enterAgentTemplate = await agentTemplateService.GetObjectAsync(z => z.Id == chatGroup.EnterAgentTemplateId);

                MiddlewareAgent<SemanticKernelAgent> enterAgent = null;

                foreach (var groupMember in groupMemebers)
                {
                    var agentTemplate = await agentTemplateService.GetObjectAsync(x => x.Id == groupMember.AgentTemplateId);
                    if (!agentTemplate.Enable)
                    {
                        logger.AppendLine($"智能体【{agentTemplate.Name}】目前为关闭状态，跳过对话");
                        //不启用的智能体不参与对话
                        continue;
                    }
                    
                    var agentTemplateDto = agentTemplateService.Mapper.Map<AgentTemplateDto>(agentTemplate);
                    agentsTemplates.Add(agentTemplateDto);

                    //TODO：确认 Prompt 此时是否存在，如果不存在需要给出提示

                    var promptResult = await promptItemService.GetWithVersionAsync(agentTemplate.PromptCode, isAvg: true);

                    var itemKernel = kernel;
                    if (personality)
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
                    var agentMiddleware = agent
                                .RegisterTextMessageConnector()
                                .RegisterCustomPrintMessage(new PrintWechatMessageMiddleware((a, m, mStr) =>
                                {
                                    AgentTemplatePrintMessageMiddleware.SendWechatMessage
                                    .Invoke(a, m, mStr, agentTemplateDto, chatGroupDto, chatTaskDto);
                                    //PrintWechatMessageMiddlewareExtension.SendWechatMessage.Invoke(a, m, mStr, agentTemplateDto);
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
                if (personality)
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

                var aiTeam = finishedGraph.CreateAiTeam(admin);

                try
                {
                    var greetingMessage = await enterAgent.SendAsync($"你好，如果已经就绪，请告诉我们“已就位”，并和 {hearingMember.Name} 打个招呼");

                    var commandMessage = new TextMessage(Role.Assistant, userCommand, hearingMember.Name);

                    IEnumerable<IMessage> result = await enterAgent.SendMessageToGroupAsync(
                          groupChat: aiTeam,
                          chatHistory: [greetingMessage, commandMessage],
                          maxRound: 10);

                    Console.WriteLine("Chat finished.");
                    logger.Append("已完成运行：" + chatGroup.Name);

                    await chatTaskService.SetStatus(Models.DatabaseModel.ChatTask_Status.Finished, chatTask);

                    SenparcTrace.SendCustomLog($"Agents 运行结果（组：{chatGroup.Name}）", logger.ToString());



                    //return result;
                }
                catch (Exception ex)
                {
                    SenparcTrace.BaseExceptionLog(ex);
                    throw;
                }
                finally {
                    scope.Dispose();
                }
            });

            TaskList.Add(task);
        }
    }
}

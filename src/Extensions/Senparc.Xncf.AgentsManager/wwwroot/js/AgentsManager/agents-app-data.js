/*
 * AgentsManager 前端：Vue 根实例 data() 返回的数据模型。
 * 注意：本文件与 agents-app-*.js 系列按 three-loader.js 中的顺序加载，
 * 依赖全局 var（classic script 顶层 var 挂到 window），请勿单独引入。
 */

var AgentsAppData = {
      devHost: 'http://pr-felixj.frp.senparc.com',
      elSize: 'medium', // el 组件尺寸大小 默认为空  medium、small、mini
      tabsActiveName: 'first', // first(智能体) second(组) third(任务)
      // 显隐 visible
      visible: {
        drawerAgent: false, // 智能体 新增|编辑
        drawerRemoteAgent: false, // 远程 A2A 智能体管理
        dialogRemoteAgentEditor: false, // 远程 A2A 智能体新增|编辑
        dialogPublishedA2A: false, // 将本地 Agent 发布为 A2A 服务
        dialogGroupAgent: false, // 智能体 新增dialog
        drawerGroup: false, // 组 新增|编辑
        drawerGroupStart: false, // 组 启动 
        dialogAgentParameter: false, // 智能体参数 列表
        dialogTaskDescription: false, // 任务描述
        dialogTaskEvaluation: false, // 任务评价页面
        dialogMcpTools: false, // MCP工具列表对话框
        drawerFunctionBindings: false, // FunctionRender / Workflow 绑定
        drawerMcpSelect: false, // 从 MCP 列表选择
      },
      taskStateText: {
        0: '等待',  // 等待 Waiting stand #3376cd
        1: '聊天', // 聊天 Chatting loading #409EFF
        2: '停顿', // 停顿 Paused loading #409EFF
        3: '完成', // 完成 Finished success #67C23A
        4: '取消', // 取消 Cancelled error #666 
        5: '失败', // 取消 fail error #F56C6C
      },
      taskStateColor: {
        0: 'waitColor',
        1: 'chartColor',
        2: 'chartColor',
        3: 'successColor',
        4: 'cancelledColor',
        5: 'errorColor',
      },
      taskStateIcon: {
        0: 'fas fa-clock',
        1: 'fas fa-spinner fa-pulse',// 动画
        2: 'fas fa-play-circle',
        3: 'fas fa-check-circle', // el-icon-success
        4: 'fal fa-minus-circle fa-rotate-45', // 旋转
        5: 'fas fa-times-circle',// el-icon-error
      },
      agentStateText: {
        1: '待命',
        2: '进行中',
        3: '停用',
      },
      agentStateColor: {
        1: 'standColor',
        2: 'proceColor',
        3: 'stopColor',
      },
      agentAvatarList: [
        '/images/AgentsManager/avatar/avatar1.png',
        '/images/AgentsManager/avatar/avatar2.png',
        '/images/AgentsManager/avatar/avatar3.png',
        '/images/AgentsManager/avatar/avatar4.png',
        '/images/AgentsManager/avatar/avatar5.png',
      ],
      // 智能体 ---start
      agentQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // 筛选文本
        timeSort: false, // 默认降序
        proce: false, // 进行中
        stop: false, // 停用
        stand: false, // 待命
      },
      agentFCPVisible: false, // 筛选条件 popover 显隐
      agentFilterCriteria: [
        {
          label: '全部',
          value: 'all',
          checked: true
        },
        {
          label: '进行中',
          value: 'proce',
          checked: false
        },
        {
          label: '停用',
          value: 'stop',
          checked: false
        },
        {
          label: '待命',
          value: 'stand',
          checked: false
        }
      ],
      agentList: [],
      remoteAgentQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '',
      },
      remoteAgentList: [],
      remoteAgentBatchTesting: false,
      remoteAgentTestingIds: {},
      remoteAgentForm: {
        id: 0,
        name: '',
        description: '',
        enable: true,
        protocol: 0,
        agentCardUrl: '',
        authenticationMode: 0,
        authHeaderName: '',
        authSecretKey: '',
        timeoutSeconds: 60,
      },
      remoteAgentFormRules: {
        name: [{ required: true, message: '请填写远程智能体名称', trigger: 'blur' }],
        agentCardUrl: [{ required: true, message: '请填写 A2A Agent Card 地址', trigger: 'blur' }]
      },
      publishedA2AForm: {
        id: 0,
        agentTemplateId: 0,
        publicAgentKey: '',
        enable: false,
        cardName: '',
        cardDescription: '',
        skillId: 'chat',
        skillName: '',
        skillDescription: '',
        allowFunctionCalls: false,
        maxInputCharacters: 12000,
        authenticationMode: 0,
        authHeaderName: '',
        authSecretKey: '',
        agentCardUrl: ''
      },
      publishedA2AFormRules: {
        publicAgentKey: [{ required: true, message: '请填写公开标识', trigger: 'blur' }]
      },
      knowledgeBaseOptions: [],
      knowledgeBaseOptionsLoaded: false,
      fillCardNum: 0, // 为了保持最后一行的样式 填充的card数量
      agentListElResizeObserver: null,
      scrollbarAgentIndex: '', // 侧边智能体index 默认全部
      agentDetails: '', // 智能体详情数据 查看
      // 智能体详情 tabs
      agentDetailsTabsActiveName: 'first', // first(组) second(任务)
      // 智能体详情 组
      agentDetailsGroupQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // 筛选文本
        timeSort: false, // 默认降序
        proce: false, // 进行中
        stop: false, // 停用
        stand: false, // 待命
      },
      agentDetailsGroupList: [],
      agentDetailsGroupShowType: '1', // 1:组详情 2:任务详情
      agentDetailsGroupIndex: 0, // 侧边组index 默认全部
      agentDetailsGroupDetails: '',
      agentDetailsGroupTaskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // 筛选文本
        timeSort: false, // 默认降序
        proce: false, // 进行中
        stop: false, // 停用
        stand: false, // 待命
      },
      agentGroupTaskSelection: [], // 选中的任务列表
      agentDetailsGroupTaskList: [], // 组 任务列表
      agentDetailsGroupTaskHistoryList: [],
      agentDetailsGroupDetailsTaskDetails: '',
      agentDetailsGroupTaskMemberList: [],
      agentGroupTaskMemberfilter: '',
      agentGroupTaskMemberfilterList: [],
      // 智能体详情 任务
      agentDetailsTaskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // 筛选文本
        timeSort: false, // 默认降序
        proce: false, // 进行中
        stop: false, // 停用
        stand: false, // 待命
      },
      agentDetailsTaskIndex: 0, // 侧边任务index 默认全部
      agentDetailsTaskList: [],
      agentDetailsTaskDetails: '',
      agentDetailsTaskHistoryList: [],
      agentDetailsTaskMemberList: [],
      agentTaskMemberfilter: '',
      agentTaskMemberfilterList: [],
      // 智能体 ---end
      // 组 ---start
      groupQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // 筛选文本
        timeSort: false, // 默认降序
        proce: false, // 进行中
        stop: false, // 停用
        stand: false, // 待命
      },
      groupFCPVisible: false, // 筛选条件 popover 显隐
      groupFilterCriteria: [
        {
          label: '全部',
          value: 'all',
          checked: true
        },
        {
          label: '进行中',
          value: 'proce',
          checked: false
        },
        {
          label: '停用',
          value: 'stop',
          checked: false
        },
        {
          label: '待命',
          value: 'stand',
          checked: false
        }
      ],
      groupTreeDefaultProps: {
        children: 'children',
        label: 'name'
      },
      groupTreeData: [],
      groupSelection: [],
      groupList: [],
      groupShowType: '1', // 1:组列表 2:组详情 3:任务详情
      scrollbarGroupIndex: '', // 侧边任务index 默认全部
      groupDetails: '',
      groupTaskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // 筛选文本
        timeSort: false, // 默认降序
        proce: false, // 进行中
        stop: false, // 停用
        stand: false, // 待命
      },
      groupTaskSelection: [],
      groupTaskList: [],
      groupTaskListLastNew: [],
      groupTaskDetails: '',
      groupTaskHistoryList: [],
      groupTaskMemberList: [],
      groupTaskMemberfilter: '',
      groupTaskMemberfilterList: [],
      // 组 新增|编辑 智能体
      groupAgentQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // 筛选文本
        timeSort: false, // 默认降序
        proce: false, // 进行中
        stop: false, // 停用
        stand: false, // 待命
      },
      isGetGroupAgent: false,
      groupAgentList: [], // 组新增时的智能体列表
      groupAgentTotal: 0,
      isGetGroupRemoteAgent: false,
      groupRemoteAgentList: [],
      groupRemoteAgentQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '',
      },
      // 组 ---end
      // 任务 task ---start
      taskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // 筛选文本
        timeSort: false, // 默认降序
        proce: false, // 进行中
        stop: false, // 停用
        stand: false, // 待命
      },
      taskArchiveScope: 'active', // active | archived | all
      taskArchiveScopeOptions: [
        { label: '活动', value: 'active' },
        { label: '归档', value: 'archived' },
        { label: '全部', value: 'all' }
      ],
      taskArchiveSavingId: 0,
      taskFCPVisible: false, // 任务模块 筛选条件 popover 显隐
      taskFilterCriteria: [
        {
          label: '全部',
          value: 'all',
          checked: true
        },
        {
          label: '进行中',
          value: 'proce',
          checked: false
        },
        {
          label: '停用',
          value: 'stop',
          checked: false
        },
        {
          label: '待命',
          value: 'stand',
          checked: false
        }
      ],
      scrollbarTaskIndex: '', // 侧边任务index 默认全部
      taskSelection: [],
      taskList: [],
      taskDetails: '', // 任务详情数据 查看
      taskHistoryList: [],
      taskMemberList: [],
      taskMemberfilter: '',
      taskMemberfilterList: [],
      // 任务 task ---end
      // 智能体 新增|编辑
      agentForm: {
        id: 0, // 0 是新增
        name: '', // 名称
        systemMessageType: '1',
        systemMessage: '', // 
        enable: true, // 是否启用
        description: '', // 说明
        hookRobotType: 0, // 外接平台
        hookRobotParameter: '', // 外接参数
        avastar: '/images/AgentsManager/avatar/avatar1.png', // 头像
        functionCallNames: '', // Function Call 名称，逗号分隔
        functionBindings: [], // FunctionRender / Workflow / Plugin 结构化绑定
        mcpEndpoints: '', // MCP Endpoints
        knowledgeBaseId: null, // 绑定的知识库
        modelBinding: 0, // 0 PromptRange，1 跟随组任务，2 手动 AIModel
        aiModelId: null,
      },
      // 编辑现有智能体时，等待 PromptRange 候选项返回后再确定“自选”或“手动”。
      agentSystemMessageTypeDetectionPending: false,
      agentFormRules: {
        name: [
          { required: true, message: '请填写', trigger: 'blur' },
        ],
        systemMessage: [
          { required: true, message: '请选择', trigger: 'change' },
        ],
        // description: [
        //     { required: true, message: '请填写', trigger: 'blur' },
        // ],
        hookRobotType: [
          { required: true, message: '请选择', trigger: 'change' },
        ],
        // hookRobotParameter: [
        //     { required: true, message: '请填写', trigger: 'blur' },
        // ],
        avastar: [
          { required: true, message: '请选择', trigger: 'change' },
        ],
        functionCallNames: [
          { required: false, message: '请输入Function Call名称', trigger: 'change' }
        ]
      },
      // 组 新增|编辑
      groupForm: {
        enable: true, // 新建组默认启用
        name: '', // 名称
        members: [], // 成员列表
        remoteMembers: [], // 远程 A2A 成员列表
        description: '', // 说明
        contextSharingMode: null, // null 时本地群沿用旧行为，远程成员默认最小化共享
        adminAgentTemplateId: '', // 群主即agent
        enterAgentTemplateId: '', // 对接人即agent
        includeHumanParticipant: false // 是否加入 Human 文本参与者
      },
      groupFormRules: {
        name: [
          { required: true, message: '请填写', trigger: 'blur' },
        ],
        members: [
          { required: true, message: '请填写', trigger: 'change' },
        ],
        adminAgentTemplateId: [
          { required: true, message: '请填写', trigger: 'change' },
        ],
        enterAgentTemplateId: [
          { required: true, message: '请选择', trigger: 'change' },
        ],
        // description: [
        //     { required: true, message: '请填写', trigger: 'blur' },
        // ],
      },
      // 组 启动
      groupStartForm: {
        groupName: '', // 组名称
        chatGroupId: '', // 组id
        name: '', // 标题
        aiModelId: '', // 模型 id
        promptCommand: '', // 任务描述
        personality: true, // 是否采用个性化
        requireHumanApproval: false, // 工具调用是否需要人工批准
        humanInTheLoopLevel: 0, // 0 自动，1 风险分层，2 工具审批，3 Human 参与者
        pluginToolPermission: 0, // 0 继承，1 自动，2 审批，3 禁止
        mcpToolPermission: 0,
        includeHumanParticipant: false,
        chatMaxRound: 20,
        description: ''
      },
      groupStartParticipants: [],
      groupStartParticipantLoading: false,
      groupStartHumanParticipantTouched: false,
      groupStartPromptCaretStart: 0,
      groupStartPromptCaretEnd: 0,
      groupStartFormRules: {
        chatGroupId: [
          { required: true, message: '请填写', trigger: 'blur' },
        ],
        name: [
          { required: true, message: '请填写', trigger: 'blur' },
        ],
        aiModelId: [
          { required: true, message: '请选择', trigger: 'change' },
        ],
        promptCommand: [
          { required: true, message: '请填写', trigger: 'blur' },
        ],
      },
      // 任务评价
      evaluationForm: {
        score: '',
        evaluation: ''
      },
      evaluationFormRules: {
        // change
        score: [
          { required: true, message: '请填写', trigger: 'blur' },
        ],
        evaluation: [
          { required: true, message: '请填写', trigger: 'blur' },
        ]
      },
      // 对话记录 轮询
      historyTimer: {},
      // 任务列表重试（用于再次执行后等待新 taskId 出现）
      taskListRetryTimer: {},
      // 对话记录实时流
      historyStream: {},
      historyStreamSilentTimer: {},
      historyStreamingDrafts: {},
      humanApprovalRequests: {},
      toolApprovalDialogVisible: false,
      toolApprovalRequest: null,
      toolApprovalArgumentText: '',
      toolApprovalQueue: [],
      toolApprovalSubmitting: false,
      humanReplyDialogVisible: false,
      humanReplyRequest: null,
      humanReplyText: '',
      humanReplySubmitting: false,
      usageAnalyticsVisible: false,
      usageAnalyticsLoading: false,
      usageAnalyticsTaskId: null,
      usageAnalyticsTaskName: '',
      usageAnalyticsDateRange: [],
      usageAnalyticsAgentId: '',
      usageAnalyticsAgentOptions: [],
      usageAnalyticsData: {
        overview: {
          messageCount: 0,
          promptTokens: 0,
          completionTokens: 0,
          totalTokens: 0,
          averageResponseMilliseconds: 0,
          minResponseMilliseconds: 0,
          maxResponseMilliseconds: 0,
          p95ResponseMilliseconds: 0,
        },
        roundStats: [],
        agentStats: [],
        timelineStats: [],
      },
      // 智能体参数列表
      agentParameterTabsValue: '', // tabs选中(使用空字符串，避免和el-tabs内部string name不匹配)
      agentParameterList: [],
      // 描述内容
      describeContent: '',
      taskDescriptionDetails: null,
      functionCallInputVisible: false,
      functionCallInputValue: '',
      functionCallTags: [], // 用于编辑时临时存储标签
      pluginTypes: [], // 存储所有可用的插件类型
      functionBindingCatalog: {
        functions: [],
        plugins: [],
        workflows: [],
        currentBindings: []
      },
      functionBindingTab: 'function',
      functionBindingSearch: '',
      functionBindingLoading: false,
      functionBindingSaving: false,
      agentAutoAttachXncf: false, // 是否自动附加所有 XNCF 功能插件
      editorFormInitialSnapshots: {}, // 打开编辑器时的表单快照，用于避免无变更时仍二次确认
      // MCP Endpoints相关
      mcpEndpointInputVisible: false,
      mcpEndpointNameValue: '',
      mcpEndpointUrlValue: '',
      mcpEndpointEditMode: false,
      mcpEndpointOriginalName: '',
      mcpSelectLoading: false, // 从 MCP 列表选择：加载中
      mcpSelectSearch: '', // 从 MCP 列表选择：搜索关键字
      mcpSelectMessage: '', // 从 MCP 列表选择：提示信息
      mcpModuleAvailable: false, // 从 MCP 列表选择：MCP 模块是否可用
      mcpEndpointOptions: [], // 从 MCP 列表选择：可选端点列表
      currentMcpTools: [], // 当前查看的MCP工具列表
      agentListViewMode: 'panel',
      agentStatisticMetric: 'totalTokens',
      agentStatisticMetricOptions: [
        { value: 'totalTokens', label: 'Token 消耗', unit: 'Token' },
        { value: 'completedConversationRounds', label: '完成对话轮数', unit: '轮' },
        { value: 'chattingCount', label: '进行中会话', unit: '个' },
        { value: 'score', label: '评分', unit: '分' }
      ],
      agentGraphSnapshot: {
        agents: [],
        groups: [],
        links: [],
        collaborations: []
      },
      agentGraphPollingTimer: null,
      hoveredAgentGroupId: null,
      agentGraph3d: null,
      agentGraphFilterGroupId: null,
      agentGraphFilterTaskStatuses: [],
      agentGraphShowOnlyActiveGroup: false,
      agentGraphRequesting: false,
      agentGraphFocus: {
        groupId: null,
        locked: false
      },
      agentGraphFocusedAgent: null,
      agentGraphLastSignature: '',
      agentGraphLastRefreshAt: null,
      agentGraphLastRenderAt: null,
      agentGraphRenderCount: 0,
      quickJumpGroupId: null,
      quickJumpTaskId: null,
      quickJumpTaskOptions: [],
      hashChangeHandler: null,
      isApplyingHashRoute: false,
};


var app = new Vue({
  el: "#app",
  filters: {
    showFormatDate(value) {
      if (!value) return ''
      return formatDate(value)
    },
    showAvatar(val) {
      return val || '/images/AgentsManager/avatar/avatar1.png'
    }
  },
  data() {
    return {
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
  },
  computed: {
    // 计算未被选择的插件类型
    availablePluginTypes() {
      if (!this.agentForm.functionCallNames) {
        return this.pluginTypes;
      }
      // 将逗号分隔的字符串转换为数组进行比较
      const currentNames = this.agentForm.functionCallNames.split(',').filter(x => x);
      return this.pluginTypes.filter(type =>
        !currentNames.includes(type)
      );
    },
    functionBindingCount() {
      return Array.isArray(this.agentForm.functionBindings)
        ? this.agentForm.functionBindings.length
        : 0
    },
    functionBindingSummary() {
      const bindings = Array.isArray(this.agentForm.functionBindings)
        ? this.agentForm.functionBindings
        : []
      const counts = bindings.reduce((result, item) => {
        const kind = item?.kind || item?.Kind || 'plugin'
        result[kind] = (result[kind] || 0) + 1
        return result
      }, {})
      const parts = []
      if (counts.function) parts.push(`FunctionRender ${counts.function}`)
      if (counts.workflow) parts.push(`Workflow ${counts.workflow}`)
      if (counts.plugin) parts.push(`Plugin ${counts.plugin}`)
      return parts.length ? parts.join(' · ') : '未绑定工具或流程'
    },
    filteredFunctionBindingOptions() {
      const catalog = this.functionBindingCatalog || {}
      const tab = this.functionBindingTab
      const options = Array.isArray(catalog[`${tab}s`])
        ? catalog[`${tab}s`]
        : []
      const keyword = String(this.functionBindingSearch || '').trim().toLowerCase()
      if (!keyword) return options
      return options.filter(item => [
        item.name,
        item.description,
        item.moduleName,
        item.key
      ].some(value => String(value || '').toLowerCase().includes(keyword)))
    },
    // 解析 McpEndpoints JSON 字符串
    parsedMcpEndpoints() {
      try {
        if (!this.agentForm.mcpEndpoints) {
          return {};
        }
        return JSON.parse(this.agentForm.mcpEndpoints);
      } catch (e) {
        console.error('Failed to parse mcpEndpoints:', e);
        return {};
      }
    },
    agentStatisticMetricOption() {
      return this.agentStatisticMetricOptions.find(item => item.value === this.agentStatisticMetric)
        || this.agentStatisticMetricOptions[0]
    },
    agentStatisticMetricLabel() {
      return this.agentStatisticMetricOption.label
    },
    agentStatisticMetricUnit() {
      return this.agentStatisticMetricOption.unit
    },
    agentStatisticMetricTotal() {
      return (this.agentList || []).reduce((total, agent) => total + this.getAgentStatisticMetricValue(agent), 0)
    },
    agentStatisticTiles() {
      const agents = this.agentList || []
      const values = agents.map(agent => this.getAgentStatisticMetricValue(agent))
      const maxValue = Math.max(0, ...values)

      return agents
        .map(agent => {
          const value = this.getAgentStatisticMetricValue(agent)
          // 使用平方根缩放，保留大用量的面积差异，同时避免少数极大值吞没整个图面。
          const ratio = maxValue > 0 ? Math.sqrt(value / maxValue) : 0
          const span = Math.max(2, Math.min(6, Math.round(2 + ratio * 4)))
          return {
            agent,
            value,
            span,
            title: `${agent.name || '未命名智能体'}：${this.agentStatisticMetricLabel} ${this.formatAgentStatisticValue(value)} ${this.agentStatisticMetricUnit}。点击编辑。`
          }
        })
        .sort((left, right) => right.value - left.value || String(left.agent.name || '').localeCompare(String(right.agent.name || '')))
    },
    agentGraphGroupOptions() {
      return (this.agentGraphSnapshot.groups || []).map(item => ({
        id: item.id,
        name: item.name
      }))
    },
    agentGraphDebugText() {
      const snapshot = this.agentGraphSnapshot || {}
      const agents = Array.isArray(snapshot.agents) ? snapshot.agents.length : 0
      const groups = Array.isArray(snapshot.groups) ? snapshot.groups.length : 0
      const links = Array.isArray(snapshot.links) ? snapshot.links.length : 0
      const cols = Array.isArray(snapshot.collaborations) ? snapshot.collaborations.length : 0
      const polling = this.agentGraphPollingTimer ? 'ON' : 'OFF'
      const requesting = this.agentGraphRequesting ? 'YES' : 'NO'

      return [
        '3D Debug',
        'Agents: ' + agents + '  Groups: ' + groups,
        'Links: ' + links + '  Collaborations: ' + cols,
        'Polling: ' + polling + '  Requesting: ' + requesting,
        'Rendered: ' + this.agentGraphRenderCount,
        'Refresh: ' + this.formatAgentGraphDebugTime(this.agentGraphLastRefreshAt),
        'Render: ' + this.formatAgentGraphDebugTime(this.agentGraphLastRenderAt)
      ].join('\n')
    },
    agentGraphOverview() {
      const snapshot = this.agentGraphSnapshot || {}
      const agents = Array.isArray(snapshot.agents) ? snapshot.agents : []
      const groups = Array.isArray(snapshot.groups) ? snapshot.groups : []
      const collaborations = Array.isArray(snapshot.collaborations) ? snapshot.collaborations : []
      const activeKeys = new Set(collaborations.flatMap(item => item.participantKeys || []))
      const local = agents.filter(item => item.agentKind !== 'RemoteA2A')
      const remote = agents.filter(item => item.agentKind === 'RemoteA2A')
      const activeTasks = groups.reduce((sum, group) => {
        const counts = group.taskStatusCounts || {}
        return sum + Number(counts[0] || counts['0'] || 0)
          + Number(counts[1] || counts['1'] || 0)
          + Number(counts[2] || counts['2'] || 0)
      }, 0)
      const chattingTasks = groups.reduce((sum, group) => {
        const counts = group.taskStatusCounts || {}
        return sum + Number(counts[1] || counts['1'] || 0)
      }, 0)
      const pausedTasks = groups.reduce((sum, group) => sum + Number(group.pausedTaskCount || 0), 0)
      const hilPending = groups.reduce((sum, group) => sum + Number(group.humanInTheLoopPendingCount || 0), 0)
      return {
        local: local.length,
        localEnabled: local.filter(item => item.enable !== false).length,
        localActive: local.filter(item => activeKeys.has(item.participantKey) || Number(item.chattingCount || 0) > 0).length,
        remote: remote.length,
        remoteEnabled: remote.filter(item => item.enable !== false).length,
        remoteActive: remote.filter(item => activeKeys.has(item.participantKey) || Number(item.chattingCount || 0) > 0).length,
        published: local.filter(item => item.hasPublishedA2A).length,
        publishedEnabled: local.filter(item => item.hasPublishedA2A && item.publishedA2AEnabled).length,
        groups: groups.length,
        groupsEnabled: groups.filter(item => item.enable !== false).length,
        groupsActive: groups.filter(item => Number(item.runningTaskCount || 0) > 0).length,
        activeTasks,
        chattingTasks,
        pausedTasks,
        hilPending
      }
    },
    agentGraphFocusedGroup() {
      const groupId = Number(this.agentGraphFocus?.groupId || 0)
      return (this.agentGraphSnapshot.groups || []).find(item => Number(item.id) === groupId) || null
    },
    agentGraphFocusedAgentSkills() {
      const skills = Array.isArray(this.agentGraphFocusedAgent?.skillKinds)
        ? this.agentGraphFocusedAgent.skillKinds
        : []
      const labels = {
        function: 'FunctionRender',
        workflow: 'Workflow',
        plugin: 'Plugin',
        mcp: 'MCP',
        a2a: 'A2A',
        human: 'Human'
      }
      return skills.map(skill => labels[skill] || skill).join(' · ') || '无额外技能'
    },
    quickJumpGroupOptions() {
      const map = new Map()
      ;(this.agentGraphSnapshot.groups || []).forEach(item => {
        map.set(item.id, { id: item.id, name: item.name })
      })
      ;(this.groupList || []).forEach(item => {
        if (!map.has(item.id)) {
          map.set(item.id, { id: item.id, name: item.name })
        }
      })
      return Array.from(map.values())
    },
    taskUsageSummaryByType() {
      return {
        task: this.buildTaskHistoryUsageSummary(this.taskHistoryList),
        agentTask: this.buildTaskHistoryUsageSummary(this.agentDetailsTaskHistoryList),
        agentGroupTask: this.buildTaskHistoryUsageSummary(this.agentDetailsGroupTaskHistoryList),
        groupTask: this.buildTaskHistoryUsageSummary(this.groupTaskHistoryList),
      }
    }
  },
  watch: {},
  created() {
    // 在组件创建时获取插件类型列表
    this.getPluginTypes();
    this.getKnowledgeBaseOptions();
  },
  mounted() {
    this.tabsActiveName = "first";
    this.agentForm.systemMessageType = "2";
    this.getPluginTypes();

    // 智能体
    if (this.tabsActiveName === 'first') {
      this.getAgentListData('agent')
    }
    // 组
    if (this.tabsActiveName === 'second') {
      this.getGroupListData('group')
    }
    // 任务
    if (this.tabsActiveName === 'third') {
      this.gettaskListData('task')
    }

    this.hashChangeHandler = () => {
      this.applyHashRoute()
    }
    window.addEventListener('hashchange', this.hashChangeHandler)
    this.refreshQuickJumpTaskOptions()
    this.$nextTick(() => {
      this.applyHashRoute()
    })

  },
  beforeDestroy() {
    this.clearHistoryTimer()
    this.stopAgentGraphPolling()
    this.destroyAgentGraph3d()
    if (this.hashChangeHandler) {
      window.removeEventListener('hashchange', this.hashChangeHandler)
      this.hashChangeHandler = null
    }
  },
  methods: {
    //寻找目标字符串
    findDest(arg1) {
      // 待判断的字符串
      //const str = '2025.05.07.1-T1-A1-草稿';
      const str = arg1;

      // 正则表达式：匹配 XXXX.XX.XX.X 的结构（X为数字）
      const regex = /^\d{4}\.\d{2}\.\d{2}\.\d+/;

      // 判断字符串是否符合规则
      if (regex.test(str)) {
        console.log('目标字符串');
        return true;
      } else {
        console.log('非目标字符串');
        return false;
      }
    },
    calculateDuration,
    scoreFormatter,
    escapeHtml(value) {
      return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
    },
    safeMarkdownUrl(value) {
      try {
        const url = new URL(String(value || ''), window.location.origin)
        return url.protocol === 'http:' || url.protocol === 'https:' ? url.href : ''
      } catch (e) {
        return ''
      }
    },
    renderSafeMarkdown(content) {
      const escapedContent = this.escapeHtml(content)
      if (typeof marked === 'undefined') {
        return escapedContent.replace(/\n/g, '<br>')
      }

      const viewModel = this
      const renderer = new marked.Renderer()
      renderer.link = function ({ href, title, tokens }) {
        const safeHref = viewModel.safeMarkdownUrl(href)
        const label = this.parser.parseInline(tokens)
        if (!safeHref) {
          return label
        }
        const safeTitle = title ? ` title="${viewModel.escapeHtml(title)}"` : ''
        return `<a href="${viewModel.escapeHtml(safeHref)}"${safeTitle}>${label}</a>`
      }
      renderer.image = function ({ href, title, text }) {
        const safeHref = viewModel.safeMarkdownUrl(href)
        if (!safeHref) {
          return viewModel.escapeHtml(text)
        }
        const safeTitle = title ? ` title="${viewModel.escapeHtml(title)}"` : ''
        return `<img src="${viewModel.escapeHtml(safeHref)}" alt="${viewModel.escapeHtml(text)}"${safeTitle}>`
      }

      return marked.parse(escapedContent, { renderer })
    },
    formatAgentGraphDebugTime(value) {
      if (!value) {
        return '--'
      }
      const date = value instanceof Date ? value : new Date(value)
      if (Number.isNaN(date.getTime())) {
        return '--'
      }
      const pad = n => String(n).padStart(2, '0')
      return pad(date.getHours()) + ':' + pad(date.getMinutes()) + ':' + pad(date.getSeconds())
    },
    getAgentStatisticMetricValue(agent) {
      const value = Number(agent?.[this.agentStatisticMetric] || 0)
      return Number.isFinite(value) && value > 0 ? value : 0
    },
    formatAgentStatisticValue(value) {
      const numeric = Number(value || 0)
      if (!Number.isFinite(numeric)) return '0'
      if (this.agentStatisticMetric === 'score') {
        return numeric.toLocaleString('en-US', { maximumFractionDigits: 1 })
      }
      return this.formatUsageCount(numeric)
    },
    agentStatisticTileStyle(tile) {
      const span = Math.max(2, Number(tile?.span || 2))
      return {
        gridColumn: `span ${span}`,
        gridRow: `span ${span}`
      }
    },
    async refreshAgentStatistics() {
      await this.getAgentListData('agent')
      this.$message.success('统计数据已刷新')
    },
    handleAgentStatisticTileClick(agent) {
      if (!agent) return
      this.handleEditDrawerOpenBtn('drawerAgent', agent)
    },
    parseHashRoute() {
      const raw = (window.location.hash || '').replace(/^#/, '')
      const route = {
        tab: '',
        view: '',
        agentId: null,
        groupId: null,
        taskId: null,
        remoteAgentId: null
      }
      if (!raw) {
        return route
      }
      const params = new URLSearchParams(raw)
      route.tab = params.get('tab') || ''
      route.view = params.get('view') || ''
      route.agentId = Number(params.get('agentId') || 0) || null
      route.groupId = Number(params.get('groupId') || 0) || null
      route.taskId = Number(params.get('taskId') || 0) || null
      route.remoteAgentId = Number(params.get('remoteAgentId') || 0) || null
      return route
    },
    setHashRoute(route) {
      if (this.isApplyingHashRoute) {
        return
      }
      const params = new URLSearchParams()
      if (route.tab) {
        params.set('tab', route.tab)
      }
      if (route.view) {
        params.set('view', route.view)
      }
      if (route.agentId) {
        params.set('agentId', String(route.agentId))
      }
      if (route.groupId) {
        params.set('groupId', String(route.groupId))
      }
      if (route.taskId) {
        params.set('taskId', String(route.taskId))
      }
      if (route.remoteAgentId) {
        params.set('remoteAgentId', String(route.remoteAgentId))
      }
      const nextHash = params.toString()
      if ((window.location.hash || '').replace(/^#/, '') === nextHash) {
        return
      }
      window.location.hash = nextHash
    },
    buildCurrentRoute(extra = {}) {
      const route = {
        tab: this.tabsActiveName || 'first'
      }
      if (route.tab === 'first') {
        if (['three', 'stats'].includes(this.agentListViewMode)) {
          route.view = this.agentListViewMode
        }
        if (this.scrollbarAgentIndex) {
          route.agentId = this.scrollbarAgentIndex
        }
      }
      if (route.tab === 'second') {
        const groupId = this.groupDetails?.chatGroupDto?.id || this.scrollbarGroupIndex || null
        const taskId = this.groupTaskDetails?.id || null
        if (groupId) {
          route.groupId = groupId
        }
        if (taskId) {
          route.taskId = taskId
        }
      }
      if (route.tab === 'third') {
        const taskId = this.taskDetails?.id || this.scrollbarTaskIndex || null
        if (taskId) {
          route.taskId = taskId
        }
      }
      return Object.assign(route, extra)
    },
    syncHashRoute(extra = {}) {
      this.setHashRoute(this.buildCurrentRoute(extra))
    },
    navigateByHash(route) {
      this.setHashRoute(route)
      this.applyHashRoute()
    },
    async applyHashRoute() {
      if (this.isApplyingHashRoute) {
        return
      }
      const route = this.parseHashRoute()
      if (!route.tab && !route.groupId && !route.taskId && !route.agentId && !route.remoteAgentId) {
        return
      }

      this.isApplyingHashRoute = true
      try {
        if (route.tab === 'remoteA2A') {
          this.visible.drawerRemoteAgent = true
          await this.getRemoteAgentListData()
          if (route.view === 'edit' && route.remoteAgentId) {
            const remoteAgent = (this.remoteAgentList || []).find(item => item.id === route.remoteAgentId)
            if (remoteAgent) {
              this.openRemoteAgentEditor(remoteAgent)
            }
          }
          return
        }

        const tab = ['first', 'second', 'third'].includes(route.tab) ? route.tab : 'first'
        if (this.tabsActiveName !== tab) {
          this.tabsActiveName = tab
          this.handleTabsClick()
        }

        if (tab === 'first') {
          if (route.view === 'edit' && route.agentId) {
            await this.getAgentListData('agent')
            const editAgent = (this.agentList || []).find(item => item.id === route.agentId)
            if (editAgent) {
              await this.handleEditDrawerOpenBtn('drawerAgent', editAgent)
            }
            return
          }
          if (['three', 'stats'].includes(route.view)) {
            this.handleAgentListViewModeChange(route.view, true)
          }
          if (route.agentId) {
            await this.getAgentListData('agent')
            const idx = (this.agentList || []).findIndex(item => item.id === route.agentId)
            if (idx >= 0) {
              this.handleAgentView(this.agentList[idx], idx, true)
            }
          }
          return
        }

        if (tab === 'second') {
          await this.getGroupListData('group')
          if (route.view === 'edit' && route.groupId) {
            const editGroup = (this.groupList || []).find(item => item.id === route.groupId)
            if (editGroup) {
              await this.handleEditDrawerOpenBtn('drawerGroup', editGroup)
            }
            return
          }
          if (route.groupId) {
            const groupItem = (this.groupList || []).find(item => item.id === route.groupId)
            if (groupItem) {
              this.handleGroupView('group', groupItem, 0, true)
            } else {
              this.groupShowType = '2'
              this.scrollbarGroupIndex = route.groupId
              await this.getGroupDetailData('groupTable', route.groupId, { id: route.groupId })
            }
          }
          if (route.taskId) {
            this.groupShowType = '3'
            await this.getTaskDetailData('groupTask', route.taskId, { id: route.taskId })
          }
          return
        }

        if (tab === 'third') {
          await this.gettaskListData('task')
          if (!route.taskId) {
            return
          }
          const idx = (this.taskList || []).findIndex(item => item.id === route.taskId)
          if (idx >= 0) {
            this.handleTaskView('task', this.taskList[idx], idx, true)
          } else {
            this.scrollbarTaskIndex = route.taskId
            await this.getTaskDetailData('task', route.taskId, { id: route.taskId })
          }
        }
      } finally {
        this.isApplyingHashRoute = false
      }
    },
    async refreshQuickJumpTaskOptions() {
      try {
        const res = await serviceAM.get('/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetList?pageIndex=0&pageSize=0')
        const data = res?.data ?? {}
        if (!data.success) {
          return
        }
        const taskList = data?.data?.chatTaskList ?? []
        this.quickJumpTaskOptions = taskList.slice(0, 200).map(item => ({
          id: item.id,
          groupId: item.chatGroupId,
          name: item.name + ' (G' + item.chatGroupId + ')'
        }))
      } catch (e) {
        console.warn('refreshQuickJumpTaskOptions failed', e)
      }
    },
    handleQuickJumpGroup() {
      const groupId = Number(this.quickJumpGroupId || 0)
      if (!groupId) {
        return
      }
      this.navigateByHash({ tab: 'second', groupId: groupId })
    },
    handleQuickJumpTask() {
      const taskId = Number(this.quickJumpTaskId || 0)
      if (!taskId) {
        return
      }
      this.navigateByHash({ tab: 'third', taskId: taskId })
    },
    handleAgentGraphFilterChange() {
      this.renderAgentGraph()
    },
    buildAgentGraphSignature(snapshot) {
      if (!snapshot) {
        return ''
      }
      return JSON.stringify({
        agents: (snapshot.agents || []).map(item => [
          item.participantKey || `local:${item.id}`,
          item.chattingCount,
          item.pausedCount,
          item.humanInTheLoopPausedCount,
          item.score,
          item.enable,
          item.agentKind,
          item.connectionStatus,
          item.skillKinds
        ]),
        groups: (snapshot.groups || []).map(item => [
          item.id,
          item.enable,
          item.runningTaskCount,
          item.pausedTaskCount,
          item.humanInTheLoopPendingCount,
          item.state,
          item.taskStatusCounts
        ]),
        links: (snapshot.links || []).map(item => [item.groupId, item.participantKey || `local:${item.agentId}`]),
        collaborations: (snapshot.collaborations || []).map(item => [item.taskId, item.groupId, item.status, item.participantKeys || item.agentIds]),
        published: (snapshot.agents || []).map(item => [item.participantKey || `local:${item.id}`, item.hasPublishedA2A, item.publishedA2AEnabled])
      })
    },
    buildFilteredAgentGraphSnapshot(snapshot) {
      const source = snapshot || { agents: [], groups: [], links: [], collaborations: [] }
      const allGroups = source.groups || []
      const allLinks = source.links || []
      const allAgents = source.agents || []
      const allCollaborations = source.collaborations || []

      const selectedGroupId = this.agentGraphFilterGroupId
      const selectedStatuses = Array.isArray(this.agentGraphFilterTaskStatuses)
        ? this.agentGraphFilterTaskStatuses.map(item => Number(item))
        : []

      let filteredGroups = allGroups.filter(group => {
        if (selectedGroupId && group.id !== selectedGroupId) {
          return false
        }

        if (this.agentGraphShowOnlyActiveGroup && !(group.runningTaskCount > 0)) {
          return false
        }

        if (selectedStatuses.length > 0) {
          const statusMap = group.taskStatusCounts || {}
          return selectedStatuses.some(status => (statusMap[status] || statusMap[String(status)] || 0) > 0)
        }

        return true
      })

      const groupIdSet = new Set(filteredGroups.map(item => item.id))
      const filteredLinks = allLinks.filter(item => groupIdSet.has(item.groupId))
      const participantKeySet = new Set(filteredLinks.map(item => item.participantKey || `local:${item.agentId}`))

      const hasExplicitGroupConstraint = Boolean(selectedGroupId)
        || this.agentGraphShowOnlyActiveGroup
        || selectedStatuses.length > 0

      // Keep ungrouped agents visible when there is no explicit group/status constraint.
      const filteredAgents = hasExplicitGroupConstraint
        ? allAgents.filter(item => participantKeySet.has(item.participantKey || `local:${item.id}`))
        : allAgents

      const filteredCollaborations = allCollaborations.filter(item => {
        if (!groupIdSet.has(item.groupId)) {
          return false
        }
        if (selectedStatuses.length > 0) {
          return selectedStatuses.includes(Number(item.status))
        }
        return true
      })

      return {
        agents: filteredAgents,
        groups: filteredGroups,
        links: filteredLinks,
        collaborations: filteredCollaborations
      }
    },
    renderAgentGraph(snapshot = null) {
      if (!this.agentGraph3d) {
        return
      }
      const filtered = this.buildFilteredAgentGraphSnapshot(snapshot || this.agentGraphSnapshot)
      this.agentGraph3d.updateGraph(filtered)
      this.agentGraphLastRenderAt = new Date()
      this.agentGraphRenderCount += 1
    },
    handleAgentListViewModeChange(mode, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.agentListViewMode = mode || 'panel'
        this.navigateByHash(this.buildCurrentRoute({
          tab: 'first',
          view: ['three', 'stats'].includes(this.agentListViewMode) ? this.agentListViewMode : null
        }))
        return
      }
      this.agentListViewMode = mode || 'panel'
      if (this.agentListViewMode === 'three' && this.tabsActiveName === 'first' && this.scrollbarAgentIndex === '') {
        this.$nextTick(() => {
          this.ensureAgentGraph3d()
          this.refreshAgentGraphSnapshot(true)
          this.startAgentGraphPolling()
        })
      } else {
        this.stopAgentGraphPolling()
        this.destroyAgentGraph3d()
      }
      this.syncHashRoute({
        tab: 'first',
        view: ['three', 'stats'].includes(this.agentListViewMode) ? this.agentListViewMode : null
      })
    },
    ensureAgentGraph3d() {
      if (!this.$refs.agent3dContainer || typeof AgentGraph3D === 'undefined') {
        return
      }
      if (this.agentGraph3d && this.agentGraph3d.renderer && this.agentGraph3d.renderer.domElement) {
        const currentCanvas = this.agentGraph3d.renderer.domElement
        const container = this.$refs.agent3dContainer
        if (!container.contains(currentCanvas)) {
          this.destroyAgentGraph3d()
        }
      }
      if (!this.agentGraph3d) {
        this.agentGraph3d = new AgentGraph3D(this.$refs.agent3dContainer, {
          onGroupHover: (groupId) => {
            this.hoveredAgentGroupId = groupId
          },
          onGroupLock: (groupId, locked) => {
            this.$set(this, 'agentGraphFocus', {
              groupId: groupId || null,
              locked: !!locked
            })
          },
          onAgentHover: (agent) => {
            this.$set(this, 'agentGraphFocusedAgent', agent || null)
          }
        })
        this.agentGraph3d.init()
        if ((this.agentGraphSnapshot.groups || []).length > 0) {
          this.renderAgentGraph(this.agentGraphSnapshot)
        }
      }
    },
    destroyAgentGraph3d() {
      if (this.agentGraph3d) {
        this.agentGraph3d.dispose()
        this.agentGraph3d = null
      }
    },
    startAgentGraphPolling() {
      this.stopAgentGraphPolling()
      this.agentGraphPollingTimer = setInterval(() => {
        if (this.tabsActiveName !== 'first' || this.scrollbarAgentIndex !== '' || this.agentListViewMode !== 'three') {
          return
        }
        this.refreshAgentGraphSnapshot(false)
      }, 1000)
    },
    stopAgentGraphPolling() {
      if (this.agentGraphPollingTimer) {
        clearInterval(this.agentGraphPollingTimer)
        this.agentGraphPollingTimer = null
      }
    },
    async refreshAgentGraphSnapshot(syncRender = false) {
      if (this.agentGraphRequesting) {
        return
      }
      this.agentGraphRequesting = true
      const query = {
        filter: this.agentQueryList.filter || ''
      }
      try {
        const res = await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetAgentGraphSnapshot?${getInterfaceQueryStr(query)}`)
        const data = res?.data ?? {}
        if (!data.success) {
          return
        }

        const snapshot = data.data || {}
        const normalizedSnapshot = {
          agents: snapshot.agents || [],
          groups: snapshot.groups || [],
          links: snapshot.links || [],
          collaborations: snapshot.collaborations || []
        }
        this.agentGraphSnapshot = normalizedSnapshot
        this.agentGraphLastRefreshAt = new Date()
        this.applyGraphMetricsToAgentList(normalizedSnapshot.agents)

        if (this.agentGraphFilterGroupId && !normalizedSnapshot.groups.some(item => item.id === this.agentGraphFilterGroupId)) {
          this.agentGraphFilterGroupId = null
        }

        const nextSignature = this.buildAgentGraphSignature(normalizedSnapshot)
        const isChanged = nextSignature !== this.agentGraphLastSignature
        this.agentGraphLastSignature = nextSignature

        if (syncRender || isChanged) {
          if (this.agentListViewMode === 'three') {
            this.$nextTick(() => {
              this.ensureAgentGraph3d()
              this.renderAgentGraph(normalizedSnapshot)
            })
          }
        }
      } finally {
        this.agentGraphRequesting = false
      }
    },
    applyGraphMetricsToAgentList(graphAgents) {
      if (!Array.isArray(this.agentList) || !Array.isArray(graphAgents) || graphAgents.length === 0) {
        return
      }

      const graphAgentMap = new Map(graphAgents
        .filter(item => !item.agentKind || item.agentKind === 'Local')
        .map(item => [item.id, item]))
      const mergedList = this.agentList.map(item => {
        const graph = graphAgentMap.get(item.id)
        if (!graph) {
          return item
        }
        return {
          ...item,
          chattingCount: graph.chattingCount,
          score: graph.score,
          promptCode: graph.promptCode
        }
      })
      this.$set(this, 'agentList', mergedList)

      if (this.agentDetails && this.agentDetails.agentTemplateDto) {
        const current = graphAgentMap.get(this.agentDetails.agentTemplateDto.id)
        if (current) {
          this.$set(this.agentDetails.agentTemplateDto, 'chattingCount', current.chattingCount)
          this.$set(this.agentDetails.agentTemplateDto, 'score', current.score)
          this.$set(this.agentDetails.agentTemplateDto, 'promptCode', current.promptCode)
        }
      }
    },
    // 计算 agent列表 需要填充的元素数量
    calcAgentFillNum() {
      // if (this.tabsActiveName === 'first' && this.scrollbarAgentIndex === '') {
      // }
      if (!this.agentListElResizeObserver) {
        // 计算 agent列表 需要填充的元素数量
        this.agentListElResizeObserver = new ResizeObserver(entries => {
          const elWidth = entries[0]?.contentRect?.width ?? 0
          const singleElWidth = 315
          const elSpac = 30
          const num = this.agentList.length
          // 单个元素 最小宽 315
          let rowNum = Math.floor(elWidth / singleElWidth)
          if (rowNum > 1) {
            rowNum = Math.floor((elWidth - ((rowNum - 1) * elSpac)) / singleElWidth)
            if (num > rowNum) {
              let _fillNum = num % rowNum
              this.fillCardNum = _fillNum > 0 ? rowNum - _fillNum : _fillNum
            } else {
              this.fillCardNum = 0
            }
          } else {
            this.fillCardNum = 0
          }
        });
      }
      if (this.agentListElResizeObserver && this.$refs.agentElListBox) {
        this.agentListElResizeObserver?.observe(this.$refs.agentElListBox);
      }

    },
    // 获取 状态文本
    getStatusText(item, showType) {
      // this.taskStateText this.agentStateText
      let statusText = ''
      // 智能体
      if (showType === '1') {
        let detailData = item.agentTemplateDto || item
        statusText = detailData.enable ? '待命' : '停用'
        let resultText = ''
        if (detailData.enable) {
          // state status
          resultText = this.taskStateText[item.status]
        }
        return resultText || statusText
      }
      // 组
      if (showType === '2') {
        let detailData = item.chatGroupDto || item
        statusText = detailData.enable ? '待命' : '停用'
        let resultText = ''
        if (detailData.enable) {
          resultText = this.taskStateText[item.state]
        }
        return resultText || statusText
      }
      // 任务
      if (showType === '3') {
        statusText = this.taskStateText[item.status]
        return statusText
      }
      return ''
    },
    // 获取 状态颜色
    getStatusColor(item, showType) {
      // this.taskStateColor this.agentStateColor
      let statusColor = ''
      // 智能体列表
      if (showType === '1') {
        let detailData = item.agentTemplateDto || item
        statusColor = detailData.enable ? 'standColor' : 'stopColor'
        let resultColor = ''
        if (detailData.enable) {
          resultColor = this.taskStateColor[item.status]
        }
        return resultColor || statusColor
      }
      // 组
      if (showType === '2') {
        let detailData = item.chatGroupDto || item
        statusColor = detailData.enable ? 'standColor' : 'stopColor'
        let resultColor = ''
        if (detailData.enable) {
          resultColor = this.taskStateColor[item.status]
        }
        return resultColor || statusColor
      }
      // 任务 
      if (showType === '3') {
        statusColor = this.taskStateColor[item.status]
        return statusColor
      }
    },
    getTaskStatusAccentColor(status) {
      const statusColorMap = {
        0: '#3376cd',
        1: '#409EFF',
        2: '#409EFF',
        3: '#67C23A',
        4: '#666666',
        5: '#F56C6C'
      }
      return statusColorMap[Number(status)] || '#C0C4CC'
    },
    remoteConnectionStatusText(status) {
      const statusMap = { 0: '未检测', 1: '可用', 2: '不可用' }
      return statusMap[Number(status)] || '未检测'
    },
    remoteConnectionStatusType(status) {
      const statusTypeMap = { 0: 'info', 1: 'success', 2: 'danger' }
      return statusTypeMap[Number(status)] || 'info'
    },
    remoteParticipantAvailabilityText(participant) {
      if (!participant?.enable) return '已停用'
      return this.remoteConnectionStatusText(participant.connectionStatus)
    },
    remoteParticipantAvailabilityType(participant) {
      if (!participant?.enable) return 'info'
      return this.remoteConnectionStatusType(participant.connectionStatus)
    },
    isRemoteAgentTesting(remoteAgentId) {
      return !!this.remoteAgentTestingIds?.[Number(remoteAgentId)]
    },
    setRemoteAgentTesting(remoteAgentIds, testing) {
      ;(remoteAgentIds || []).forEach(remoteAgentId => {
        const id = Number(remoteAgentId)
        if (id > 0) this.$set(this.remoteAgentTestingIds, id, testing)
      })
    },
    applyRemoteConnectionResults(results) {
      const resultById = new Map((results || [])
        .filter(result => Number(result?.remoteAgentId) > 0)
        .map(result => [Number(result.remoteAgentId), result]))
      if (!resultById.size) return

      const updateRemoteAgent = remoteAgent => {
        const remoteAgentId = Number(remoteAgent?.id || remoteAgent?.remoteAgentId || 0)
        const result = resultById.get(remoteAgentId)
        if (!remoteAgent || !result) return
        Object.assign(remoteAgent, result.remoteAgentDto || {})
        if (result.remoteAgentDto?.connectionStatus === undefined) {
          remoteAgent.connectionStatus = result.success ? 1 : 2
          remoteAgent.lastHealthCheckMessage = result.message || ''
        }
      }
      const updateParticipantList = participantList => {
        ;(participantList || []).forEach(participant => {
          if (participant?.agentKind === 'RemoteA2A') updateRemoteAgent(participant)
        })
      }
      const updateGroupDetail = groupDetail => {
        ;(groupDetail?.remoteMemberDtoList || []).forEach(member => updateRemoteAgent(member?.remoteAgentDto))
      }

      ;[this.remoteAgentList, this.groupRemoteAgentList, this.groupForm?.remoteMembers]
        .forEach(remoteAgentList => (remoteAgentList || []).forEach(updateRemoteAgent))
      updateParticipantList(this.groupStartParticipants)
      updateParticipantList(this.taskMemberList)
      updateParticipantList(this.agentDetailsTaskMemberList)
      updateGroupDetail(this.groupDetails)
      updateGroupDetail(this.agentDetailsGroupDetails)
    },
    async testRemoteAgentConnections(remoteAgentIds, options = {}) {
      const requestedIds = [...new Set((remoteAgentIds || []).map(Number).filter(id => id > 0))]
      const testAll = options.testAll === true
      if (!testAll && requestedIds.length === 0) return []

      const loadingIds = testAll
        ? (this.remoteAgentList || []).map(item => item.id)
        : requestedIds
      if (testAll) this.remoteAgentBatchTesting = true
      this.setRemoteAgentTesting(loadingIds, true)
      try {
        const response = await serviceAM.post(
          '/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.TestConnections',
          { remoteAgentIds: testAll ? [] : requestedIds })
        const data = response?.data ?? {}
        if (!data.success) throw new Error(data.errorMessage || data.data || '连接测试失败')

        const results = data?.data?.results ?? []
        this.applyRemoteConnectionResults(results)
        if (options.refreshLists) {
          await this.getRemoteAgentListData()
          if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
        }
        return results
      } catch (err) {
        if (!options.silent) this.$message.error(err?.message || '连接测试失败')
        throw err
      } finally {
        this.setRemoteAgentTesting(loadingIds, false)
        if (testAll) this.remoteAgentBatchTesting = false
      }
    },
    showRemoteAgentTestSummary(results) {
      const failedResults = (results || []).filter(result => !result.success)
      if (!results?.length) {
        this.$message.warning('没有可测试的远程 A2A 智能体')
        return
      }
      if (!failedResults.length) {
        this.$message.success(`全部通过：${results.length} 个远程 A2A 智能体均可用`)
        return
      }
      const failedLines = failedResults.map(result => `${result.name || `#${result.remoteAgentId}`}：${result.message || '不可用'}`)
      this.$alert(`通过 ${results.length - failedResults.length} 个，未通过 ${failedResults.length} 个。\n\n${failedLines.join('\n')}`,
        '远程 A2A 批量测试结果', { type: 'warning', confirmButtonText: '知道了' })
    },
    async getRemoteAgentListData(listType = 'remoteAgent') {
      const query = listType === 'groupRemoteAgent'
        ? { ...this.groupRemoteAgentQueryList }
        : { ...this.remoteAgentQueryList }
      try {
        const response = await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.GetList?${getInterfaceQueryStr(query)}`)
        const data = response?.data ?? {}
        if (!data.success) {
          throw new Error(data.errorMessage || data.data || '加载远程 A2A 智能体失败')
        }

        const list = data?.data?.list ?? []
        if (listType === 'groupRemoteAgent') {
          this.$set(this, 'groupRemoteAgentList', list)
          this.$nextTick(() => {
            this.isGetGroupRemoteAgent = false
            if (!this.visible.drawerGroup || !this.groupForm.remoteMembers?.length) return
            const selected = list.filter(item => this.groupForm.remoteMembers.some(member => member.id === item.id))
            this.toggleRemoteSelection(selected)
          })
        } else {
          this.$set(this, 'remoteAgentList', list)
        }
      } catch (err) {
        console.log('getRemoteAgentListData', err)
        this.$message.error(err?.message || '加载远程 A2A 智能体失败')
      }
    },
    openRemoteAgentManager() {
      this.visible.drawerRemoteAgent = true
      this.getRemoteAgentListData()
    },
    openAgentExecutionManager() {
      window.location.assign('/Admin/AgentsManager/AgentExecutions')
    },
    async openPublishedA2AEditor(item) {
      const agent = item?.agentTemplateDto || item || this.agentForm
      const agentTemplateId = Number(agent?.id || agent?.agentTemplateId || 0)
      if (!agentTemplateId) {
        this.$message.warning('请先保存本地智能体，再配置 A2A 对外发布')
        return
      }

      const defaults = this.$options.data().publishedA2AForm
      try {
        const response = await serviceAM.get(
          `/api/Senparc.Xncf.AgentsManager/PublishedA2AAgentAppService/Xncf.AgentsManager_PublishedA2AAgentAppService.GetByAgentTemplateId?agentTemplateId=${agentTemplateId}`)
        const data = response?.data ?? {}
        if (!data.success) throw new Error(data.errorMessage || data.data || '加载 A2A 发布配置失败')
        const existed = data.data || {}
        this.$set(this, 'publishedA2AForm', {
          ...defaults,
          ...existed,
          agentTemplateId,
          publicAgentKey: existed.publicAgentKey || `agent-${agentTemplateId}`,
          cardName: existed.cardName || agent.name || '',
          cardDescription: existed.cardDescription || agent.description || ''
        })
        this.visible.dialogPublishedA2A = true
        this.$nextTick(() => this.$refs.publishedA2AELForm?.clearValidate())
      } catch (err) {
        this.$message.error(err?.message || '加载 A2A 发布配置失败')
      }
    },
    closePublishedA2AEditor() {
      this.visible.dialogPublishedA2A = false
      this.$set(this, 'publishedA2AForm', this.$options.data().publishedA2AForm)
    },
    async savePublishedA2A() {
      this.$refs.publishedA2AELForm.validate(async (valid) => {
        if (!valid) return

        if (this.publishedA2AForm.enable) {
          const riskItems = [
            '外部系统调用后，输入及 Agent 的正常回复都会跨越本系统边界；请确认 Prompt、知识库和回复内容可对外共享。',
            '每次调用可能产生模型与工具成本；请在 HTTPS 网关配置访问控制、限流、监控和告警。'
          ]
          if (this.publishedA2AForm.authenticationMode === 0) {
            riskItems.push('当前未启用入站鉴权。仅可用于隔离、受控网络；不可直接暴露到公网。')
          }
          if (this.publishedA2AForm.allowFunctionCalls) {
            riskItems.push('已允许本地 Function / MCP 工具调用。外部输入可能间接触发读写或外部访问，请确认工具权限最小化。')
          }

          try {
            await this.$confirm(
              `<div>启用后，此本地 Agent 将作为标准 A2A 服务接受外部调用。</div><ul style="padding-left:20px;margin:10px 0 0;"><li>${riskItems.join('</li><li>')}</li></ul>`,
              '确认启用 A2A 对外服务',
              {
                type: 'warning',
                dangerouslyUseHTMLString: true,
                confirmButtonText: '我已知悉并保存',
                cancelButtonText: '取消'
              })
          } catch (_) {
            return
          }
        }

        try {
          const response = await serviceAM.post(
            '/api/Senparc.Xncf.AgentsManager/PublishedA2AAgentAppService/Xncf.AgentsManager_PublishedA2AAgentAppService.SetPublishedAgent',
            this.publishedA2AForm)
          const data = response?.data ?? {}
          if (!data.success) throw new Error(data.errorMessage || data.data || '保存失败')
          this.$set(this, 'publishedA2AForm', { ...this.publishedA2AForm, ...(data.data || {}) })
          await this.getAgentListData('agent')
          this.$message.success(this.publishedA2AForm.enable ? '本地 Agent 已发布为 A2A 服务' : 'A2A 发布配置已保存（当前未启用）')
        } catch (err) {
          this.$message.error(err?.message || '保存 A2A 发布配置失败')
        }
      })
    },
    async copyPublishedA2AUrl() {
      const url = this.publishedA2AForm.agentCardUrl
      if (!url) return
      try {
        await navigator.clipboard.writeText(url)
        this.$message.success('A2A Agent Card 地址已复制')
      } catch (err) {
        this.$message.warning('复制失败，请手动复制地址')
      }
    },
    openRemoteAgentEditor(item = null) {
      const defaults = this.$options.data().remoteAgentForm
      this.$set(this, 'remoteAgentForm', { ...defaults, ...(item || {}) })
      this.visible.dialogRemoteAgentEditor = true
    },
    closeRemoteAgentEditor() {
      this.visible.dialogRemoteAgentEditor = false
      this.$set(this, 'remoteAgentForm', this.$options.data().remoteAgentForm)
      this.$nextTick(() => this.$refs.remoteAgentELForm?.clearValidate())
    },
    async saveRemoteAgent() {
      this.$refs.remoteAgentELForm.validate(async (valid) => {
        if (!valid) return
        try {
          const response = await serviceAM.post(
            '/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.SetRemoteAgent',
            this.remoteAgentForm)
          const data = response?.data ?? {}
          if (!data.success) throw new Error(data.errorMessage || data.data || '保存失败')
          this.$message.success('远程 A2A 智能体已保存')
          this.closeRemoteAgentEditor()
          await this.getRemoteAgentListData()
          if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
        } catch (err) {
          this.$message.error(err?.message || '保存远程 A2A 智能体失败')
        }
      })
    },
    async testRemoteAgent(item) {
      try {
        const results = await this.testRemoteAgentConnections([item?.id], { silent: true, refreshLists: true })
        const result = results[0]
        if (!result?.success) {
          this.$message.error(result?.message || '连接测试失败')
          return
        }
        this.$message.success(result.message || 'A2A Agent Card 连接成功')
      } catch (err) {
        this.$message.error(err?.message || '连接测试失败')
      }
    },
    async testAllRemoteAgents() {
      try {
        const results = await this.testRemoteAgentConnections([], { testAll: true, silent: true, refreshLists: true })
        this.showRemoteAgentTestSummary(results)
      } catch (err) {
        this.$message.error(err?.message || '批量连接测试失败')
      }
    },
    async autoTestRemoteParticipants(participants) {
      const remoteAgentIds = (participants || [])
        .filter(participant => participant?.agentKind === 'RemoteA2A')
        .map(participant => participant.id)
      if (!remoteAgentIds.length) return []

      try {
        const results = await this.testRemoteAgentConnections(remoteAgentIds, { silent: true })
        const failedCount = results.filter(result => !result.success).length
        if (failedCount) {
          this.$message.warning(`${failedCount} 个远程 A2A 智能体当前不可用，可在成员列表中手动重测`)
        }
        return results
      } catch (err) {
        console.warn('autoTestRemoteParticipants failed', err)
        return []
      }
    },
    async testGroupStartRemoteAgent(participant) {
      try {
        const results = await this.testRemoteAgentConnections([participant?.id], { silent: true })
        const result = results[0]
        if (result?.success) {
          this.$message.success(result.message || 'A2A Agent Card 连接成功')
        } else {
          this.$message.error(result?.message || '连接测试失败')
        }
      } catch (err) {
        this.$message.error(err?.message || '连接测试失败')
      }
    },
    async testTaskRemoteAgent(participant) {
      try {
        const results = await this.testRemoteAgentConnections([participant?.id], { silent: true })
        const result = results[0]
        if (result?.success) {
          this.$message.success(result.message || 'A2A Agent Card 连接成功')
        } else {
          this.$message.error(result?.message || '连接测试失败')
        }
      } catch (err) {
        this.$message.error(err?.message || '连接测试失败')
      }
    },
    async setRemoteAgentEnable(item, enable) {
      try {
        const response = await serviceAM.post(
          `/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.Enable?id=${item.id}&enable=${enable}`,
          {})
        const data = response?.data ?? {}
        if (!data.success) throw new Error(data.errorMessage || data.data || '状态更新失败')
        this.$message.success(data.data || '状态已更新')
        await this.getRemoteAgentListData()
        if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
      } catch (err) {
        this.$message.error(err?.message || '状态更新失败')
      }
    },
    deleteRemoteAgent(item) {
      this.$confirm(`确认删除远程 A2A 智能体“${item.name}”？已加入群组的智能体不可删除。`, '删除确认', { type: 'warning' })
        .then(async () => {
          try {
            const response = await serviceAM.post(
              `/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.Delete?id=${item.id}`,
              {})
            const data = response?.data ?? {}
            if (!data.success) throw new Error(data.errorMessage || data.data || '删除失败')
            this.$message.success(data.data || '已删除')
            await this.getRemoteAgentListData()
            if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
          } catch (err) {
            this.$message.error(err?.message || '删除失败')
          }
        })
        .catch(() => { })
    },
    toggleRemoteSelection(rows) {
      if (rows) {
        rows.forEach(row => this.$refs?.groupRemoteAgentTable?.toggleRowSelection(row))
      } else {
        this.$refs?.groupRemoteAgentTable?.clearSelection()
      }
    },
    handleRemoteSelectionChange(val) {
      if (this.isGetGroupRemoteAgent) return
      const selectedIds = new Set((val || []).map(item => item.id))
      const visibleIds = new Set((this.groupRemoteAgentList || []).map(item => item.id))
      const retained = (this.groupForm.remoteMembers || []).filter(item => !visibleIds.has(item.id))
      const selected = (this.groupRemoteAgentList || []).filter(item => selectedIds.has(item.id))
      this.$set(this.groupForm, 'remoteMembers', [...retained, ...selected])
    },
    groupRemoteMembersCancel(item) {
      const index = this.groupForm.remoteMembers.findIndex(member => member.id === item.id)
      if (index !== -1) this.groupForm.remoteMembers.splice(index, 1)
      const row = this.groupRemoteAgentList.find(agent => agent.id === item.id)
      if (row) this.toggleRemoteSelection([row])
    },
    // 获取 智能体 数据
    async getAgentListData(listType, page = 0) {
      const queryList = {}
      if (listType === 'agent') {
        this.agentQueryList.pageIndex = page ?? 1
        Object.assign(queryList, this.agentQueryList)
      }
      if (listType === 'groupAgent') {
        this.groupAgentQueryList.pageIndex = page ?? 1
        Object.assign(queryList, this.groupAgentQueryList)
      }
      // 接口对接
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetList?${getInterfaceQueryStr(queryList)}`)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const agentData = data?.data?.list ?? []
            if (listType === 'agent') {
              this.$set(this, 'agentList', agentData)
              const agentDetail = this.agentDetails?.agentTemplateDto ?? {}
              // 获取详情 
              if (agentDetail.id) {
                this.getAgentDetailData(agentDetail.id, agentDetail)
              }
              // 计算 agent列表 需要填充的元素数量
              this.calcAgentFillNum()

              if (this.tabsActiveName === 'first' && this.scrollbarAgentIndex === '') {
                this.refreshAgentGraphSnapshot(this.agentListViewMode === 'three')
              }
            }
            if (listType === 'groupAgent') {
              this.$set(this, 'groupAgentList', agentData)
              // 确保更新数据时 不会清空选中
              this.$nextTick(() => {
                this.isGetGroupAgent = false
              })
              // 组成员table 初始选中
              if (this.visible.drawerGroup && this.groupForm.members.length > 0) {
                // this.toggleSelection()
                this.$nextTick(() => {
                  // this.groupAgentTotal = agentData.length
                  const filterList = agentData.filter(i => {
                    return this.groupForm.members.findIndex(item => item.id === i.id) !== -1
                  })
                  this.toggleSelection(filterList)
                })

              }
            }
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
            this.isGetGroupAgent = false
          }
        }).catch((err) => {
          console.log('err', err)
          this.isGetGroupAgent = false
        })
    },
    // 获取 智能体详情 
    async getAgentDetailData(id, detail = {}) {
      let taskList = []
      let groupList = []
      if (this.tabsActiveName === 'first') {
        const groupQuery = {
          pageIndex: 0,
          pageSize: 0,
          agentTemplateId: id
        }
        // 获取组列表
        await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupList?${getInterfaceQueryStr(groupQuery)}`, groupQuery)
          .then(res => {
            const data = res?.data ?? {}
            if (data.success) {
              groupList = data?.data?.chatGroupDtoList ?? []
            }
          })
        const taskQuery = {
          pageIndex: 0,
          pageSize: 0,
          agentTemplateId: id
        }
        //  获取任务列表
        await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetList?${getInterfaceQueryStr(taskQuery)}`, taskQuery)
          .then(res => {
            const data = res?.data ?? {}
            if (data.success) {
              taskList = data?.data?.chatTaskList ?? []
            }
          })
      }
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetItemStatus?id=${id}`)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const agentDetail = data?.data?.agentTemplateStatus ?? ''
            if (agentDetail) {
              if (agentDetail.agentTemplateDto) {
                const agentTemplateDto = Object.assign({}, detail, agentDetail.agentTemplateDto)
                agentDetail.agentTemplateDto = agentTemplateDto
              }
              if (this.tabsActiveName === 'first') {
                agentDetail.participationGroup = groupList.length
                agentDetail.participationInTasks = taskList.length
              }
            }

            this.$set(this, 'agentDetails', agentDetail)
            // this.agentDetails = agentDetail
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
    },
    // 获取 组 数据
    async getGroupListData(listType, id, page = 0) {
      const queryList = {}
      if (listType === 'group') {
        this.groupQueryList.pageIndex = page ?? 1
        Object.assign(queryList, this.groupQueryList)
      }
      if (listType === 'agentGroup') {
        this.agentDetailsGroupQueryList.pageIndex = page ?? 1
        this.agentDetailsGroupQueryList.agentTemplateId = id
        Object.assign(queryList, this.agentDetailsGroupQueryList)
      }
      // debugger
      // 获取agent列表
      let agentAllList = []
      await serviceAM.get('/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetList')
        .then(res => {
          // debugger
          const data = res?.data ?? {}
          if (data.success) {
            agentAllList = data?.data?.list ?? []
          }
        })
      // 获取任务列表
      let taskAllList = []
      await serviceAM.get('/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetList')
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            taskAllList = data?.data?.chatTaskList ?? []
            //设置最新的任务信息
            this.groupTaskListLastNew = taskAllList[0]
          }
        })
      // 获取组列表
      await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupList?${getInterfaceQueryStr(queryList)}`, queryList)
        .then(res => {
          // debugger
          const data = res?.data ?? {}
          if (data.success) {
            const groupData = data?.data?.chatGroupDtoList ?? []
            const handleGroupData = groupData.map(item => {
              const adminAgentTemplateName = agentAllList.find(i => i.id === item.adminAgentTemplateId)?.name ?? ''
              const enterAgentTemplateName = agentAllList.find(i => i.id === item.enterAgentTemplateId)?.name ?? ''
              const numberTasks = taskAllList.filter(i => i.chatGroupId === item.id) || []
              return {
                ...item,
                numberTasks: numberTasks?.length ?? 0,
                adminAgentTemplateName,
                enterAgentTemplateName,
              }
            })
            if (listType === 'group') {
              // this.groupTreeData = [{
              //     id: '0',
              //     name: '全部组',
              //     children: groupData
              // }]
              this.groupSelection = [] // 清空选中
              this.$set(this, 'groupList', handleGroupData)
              const groupDetail = this.groupDetails?.chatGroupDto ?? {}
              // 获取详情 
              if (this.groupShowType === '2' && groupDetail.id) {
                this.getGroupDetailData(listType, groupDetail.id, groupDetail)
              }
            }
            if (listType === 'agentGroup') {
              this.$set(this, 'agentDetailsGroupList', handleGroupData)
              const groupDetail = handleGroupData[this.agentDetailsGroupIndex]
              // 获取详情
              if (groupDetail && groupDetail.id) {
                this.getGroupDetailData(listType, groupDetail.id, groupDetail)
              }
            }
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
    },
    // 获取 组详情 
    async getGroupDetailData(detailType, id, detail = {}) {
      await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${id}`)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const groupDetail = data?.data ?? ''
            if (groupDetail && groupDetail.chatGroupDto) {
              const chatGroupDto = Object.assign({}, detail, groupDetail.chatGroupDto)
              groupDetail.chatGroupDto = chatGroupDto
            }
            if (detailType === 'agentGroup') {
              this.$set(this, 'agentDetailsGroupDetails', groupDetail)
              // 获取任务列表
              this.gettaskListData('agentGroupTask', id)
            }
            if (['group', 'groupTable'].includes(detailType)) {
              this.$set(this, 'groupDetails', groupDetail)
              // 获取任务列表
              this.gettaskListData('groupTask', id)
            }
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
    },
    // 获取 任务 数据
    async gettaskListData(listType, id, page = 0, options = {}) {
      const opts = options || {}
      const preferLatest = !!opts.preferLatest
      const focusChatGroupIdRaw = opts.focusChatGroupId
      const hasFocusChatGroupId = focusChatGroupIdRaw !== undefined
        && focusChatGroupIdRaw !== null
        && focusChatGroupIdRaw !== ''
      const focusChatGroupId = hasFocusChatGroupId ? Number(focusChatGroupIdRaw) : null
      const minTaskIdExclusive = Number(opts.minTaskIdExclusive || 0)
      const hasMinTaskIdExclusive = Number.isFinite(minTaskIdExclusive) && minTaskIdExclusive > 0
      const retryOnMiss = !!opts.retryOnMiss

      const getScopedTaskList = (list) => {
        if (!Array.isArray(list) || list.length === 0) return []
        if (!preferLatest) return list
        if (!hasFocusChatGroupId) return list

        const scopedList = list.filter(task => Number(task.chatGroupId) === focusChatGroupId)
        return scopedList.length > 0 ? scopedList : list
      }

      const hasLatestCandidate = (list) => {
        const candidateList = getScopedTaskList(list)
        if (!hasMinTaskIdExclusive) return candidateList.length > 0
        return candidateList.some(task => Number(task?.id || 0) > minTaskIdExclusive)
      }

      const pickTaskForView = (list, currentTaskId) => {
        if (!Array.isArray(list) || list.length === 0) return null

        if (preferLatest) {
          const candidateList = getScopedTaskList(list)
          const filteredList = hasMinTaskIdExclusive
            ? candidateList.filter(task => Number(task?.id || 0) > minTaskIdExclusive)
            : candidateList
          const sortList = filteredList.length > 0 ? filteredList : candidateList
          return sortList
            .slice()
            .sort((a, b) => {
              const idA = Number(a?.id || 0)
              const idB = Number(b?.id || 0)
              if (idA !== idB) return idB - idA

              const timeA = new Date(a?.startTime || a?.addTime || 0).getTime()
              const timeB = new Date(b?.startTime || b?.addTime || 0).getTime()
              return timeB - timeA
            })[0] || null
        }

        if (currentTaskId !== undefined && currentTaskId !== null && currentTaskId !== '') {
          const matched = list.find(task => String(task.id) === String(currentTaskId))
          if (matched) return matched
        }
        return list[0]
      }

      const queryList = {}
      // 任务
      if (listType === 'task') {
        this.taskQueryList.pageIndex = page ?? 1
        Object.assign(queryList, this.taskQueryList)
        queryList.archiveScope = this.getTaskArchiveScopeCode()
      }
      // 智能体 任务
      if (listType === 'agentTask') {
        this.agentDetailsTaskQueryList.pageIndex = page ?? 1
        this.agentDetailsTaskQueryList.agentTemplateId = id
        Object.assign(queryList, this.agentDetailsTaskQueryList)
      }
      // 智能体 组 任务
      if (listType === 'agentGroupTask') {
        this.agentDetailsGroupTaskQueryList.pageIndex = page ?? 1
        this.agentDetailsGroupTaskQueryList.chatGroupId = id
        Object.assign(queryList, this.agentDetailsGroupTaskQueryList)
      }
      // 组 任务
      if (listType === 'groupTask') {
        this.groupTaskQueryList.pageIndex = page ?? 1
        this.groupTaskQueryList.chatGroupId = id
        Object.assign(queryList, this.groupTaskQueryList)
      }
      let modelList = []
      // 获取模型列表
      await serviceAM.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetListAsync', {
        pageIndex: 0,
        pageSize: 0
      }).then(res => {
        // console.log('this.serviceType === model', res);
        const data = res?.data ?? {}
        if (data.success) {
          //console.log('getModelOptData:', res.data)
          modelList = data?.data ?? []
        } else {
          app.$message({
            message: data.errorMessage || data.data || 'Error',
            type: 'error',
            duration: 5 * 1000
          })
        }
      })
      //  接口对接
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetList?${getInterfaceQueryStr(queryList)}`, queryList)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const taskData = data?.data?.chatTaskList ?? []
            const handleTaskData = taskData.map(item => {
              const modelName = modelList.find(i => i.id === item.aiModelId)?.alias ?? ''
              return {
                ...item,
                modelName
              }
            })

            const needRetryLatestTask = preferLatest && retryOnMiss
            if (needRetryLatestTask && !hasLatestCandidate(handleTaskData)) {
              this.scheduleTaskListRetry(listType, id, page, opts)
            } else {
              this.clearTaskListRetryTimer(listType)
            }

            // 任务
            if (listType === 'task') {
              this.$set(this, 'taskList', handleTaskData)
              if (needRetryLatestTask && !hasLatestCandidate(handleTaskData)) {
                return
              }
              // 默认展示第一个任务详情
              if (handleTaskData && handleTaskData.length) {
                const taskDetail = pickTaskForView(handleTaskData, this.taskDetails?.id)
                if (taskDetail) {
                  if (preferLatest) {
                    const latestIndex = handleTaskData.findIndex(task => String(task.id) === String(taskDetail.id))
                    this.scrollbarTaskIndex = latestIndex > -1 ? latestIndex : 0
                  }
                  this.getTaskDetailData(listType, taskDetail.id, taskDetail)
                }
              }
            }
            // 智能体 任务
            if (listType === 'agentTask') {
              this.$set(this, 'agentDetailsTaskList', handleTaskData)
              if (needRetryLatestTask && !hasLatestCandidate(handleTaskData)) {
                return
              }
              // 默认展示第一个任务详情
              if (handleTaskData && handleTaskData.length) {
                const taskDetail = pickTaskForView(handleTaskData, this.agentDetailsTaskDetails?.id)
                if (taskDetail) {
                  if (preferLatest) {
                    const latestIndex = handleTaskData.findIndex(task => String(task.id) === String(taskDetail.id))
                    this.agentDetailsTaskIndex = latestIndex > -1 ? latestIndex : 0
                    this.agentDetailsTabsActiveName = 'second'
                  }
                  this.getTaskDetailData(listType, taskDetail.id, taskDetail)
                }
              }
            }
            // 智能体 组 任务
            if (listType === 'agentGroupTask') {
              this.$set(this, 'agentDetailsGroupTaskList', handleTaskData)
              if (needRetryLatestTask && !hasLatestCandidate(handleTaskData)) {
                return
              }
              if (preferLatest && handleTaskData.length) {
                const taskDetail = pickTaskForView(handleTaskData, this.agentDetailsGroupDetailsTaskDetails?.id)
                if (taskDetail) {
                  this.agentDetailsGroupShowType = '2'
                  this.getTaskDetailData(listType, taskDetail.id, taskDetail)
                }
              }
            }
            // 组 任务
            if (listType === 'groupTask') {
              this.$set(this, 'groupTaskList', handleTaskData)
              if (needRetryLatestTask && !hasLatestCandidate(handleTaskData)) {
                return
              }
              if (preferLatest && handleTaskData.length) {
                const taskDetail = pickTaskForView(handleTaskData, this.groupTaskDetails?.id)
                if (taskDetail) {
                  this.groupShowType = '3'
                  this.getTaskDetailData(listType, taskDetail.id, taskDetail)
                }
              }
            }
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
    },
    getTaskListByType(listType) {
      if (listType === 'task') return this.taskList || []
      if (listType === 'agentTask') return this.agentDetailsTaskList || []
      if (listType === 'agentGroupTask') return this.agentDetailsGroupTaskList || []
      if (listType === 'groupTask') return this.groupTaskList || []
      return []
    },
    getCurrentTaskDetailByType(listType) {
      if (listType === 'task') return this.taskDetails || null
      if (listType === 'agentTask') return this.agentDetailsTaskDetails || null
      if (listType === 'agentGroupTask') return this.agentDetailsGroupDetailsTaskDetails || null
      if (listType === 'groupTask') return this.groupTaskDetails || null
      return null
    },
    setCurrentTaskDetailByType(listType, detail) {
      if (listType === 'task') this.$set(this, 'taskDetails', detail)
      if (listType === 'agentTask') this.$set(this, 'agentDetailsTaskDetails', detail)
      if (listType === 'agentGroupTask') this.$set(this, 'agentDetailsGroupDetailsTaskDetails', detail)
      if (listType === 'groupTask') this.$set(this, 'groupTaskDetails', detail)
    },
    setCurrentTaskStatusByType(listType, chatTaskId, status) {
      const nextStatus = Number(status)
      if (!Number.isFinite(nextStatus)) return

      const currentDetail = this.getCurrentTaskDetailByType(listType)
      const taskId = Number(chatTaskId || currentDetail?.id || 0)

      if (currentDetail && taskId > 0 && Number(currentDetail.id || 0) === taskId) {
        if (Number(currentDetail.status) !== nextStatus) {
          this.setCurrentTaskDetailByType(listType, Object.assign({}, currentDetail, { status: nextStatus }))
        }
      }

      const list = this.getTaskListByType(listType)
      if (!Array.isArray(list) || taskId <= 0) return

      const listIndex = list.findIndex(item => Number(item?.id || 0) === taskId)
      if (listIndex < 0) return

      const currentItem = list[listIndex] || {}
      if (Number(currentItem.status) === nextStatus) return

      this.$set(list, listIndex, Object.assign({}, currentItem, { status: nextStatus }))
    },
    getMaxTaskIdByType(listType) {
      const list = this.getTaskListByType(listType)
      if (!Array.isArray(list) || list.length === 0) return 0
      return list.reduce((maxId, item) => {
        const currentId = Number(item?.id || 0)
        return currentId > maxId ? currentId : maxId
      }, 0)
    },
    buildTaskRefreshOptions(listType, baseOptions = {}, saveType = '') {
      const options = Object.assign({}, baseOptions)
      const isStartTaskSave = ['drawerTaskStart', 'drawerGroupStart'].includes(saveType)
      if (!isStartTaskSave) {
        return options
      }

      options.retryOnMiss = true
      options.retryAttempt = 0
      options.maxRetry = 20
      options.retryDelayMs = 300

      const detailId = Number(this.getCurrentTaskDetailByType(listType)?.id || 0)
      const maxId = this.getMaxTaskIdByType(listType)
      const baselineTaskId = Math.max(detailId, maxId)

      if (baselineTaskId > 0) {
        options.minTaskIdExclusive = baselineTaskId
      }

      return options
    },
    clearTaskListRetryTimer(listType) {
      if (!listType) return
      const timer = this.taskListRetryTimer[listType]
      if (timer) {
        clearTimeout(timer)
      }
      this.$delete(this.taskListRetryTimer, listType)
    },
    clearTaskListRetryTimers() {
      Object.keys(this.taskListRetryTimer || {}).forEach((key) => {
        this.clearTaskListRetryTimer(key)
      })
    },
    scheduleTaskListRetry(listType, id, page, options = {}) {
      if (!listType) return
      const attempt = Number(options.retryAttempt || 0)
      const maxRetry = Number(options.maxRetry || 0)
      if (attempt >= maxRetry) {
        this.clearTaskListRetryTimer(listType)
        return
      }

      const retryDelayMs = Math.max(120, Number(options.retryDelayMs || 300))
      this.clearTaskListRetryTimer(listType)
      this.taskListRetryTimer[listType] = setTimeout(() => {
        const nextOptions = Object.assign({}, options, {
          retryAttempt: attempt + 1
        })
        this.gettaskListData(listType, id, page, nextOptions)
      }, retryDelayMs)
    },
    // 获取 任务详情 
    async getTaskDetailData(detailType, id, detail = {}, detailsOn = false) {
      //TODO:
      if (id == undefined) {
        app.$message({
          message: '当前还没有可执行的任务',
          type: 'error',
          duration: 5 * 1000
        })
        return
      }
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetItem?id=${id}`)
        .then(async res => {
          const data = res?.data ?? {}
          if (data.success) {
            // 不是仅详情时 清除轮询
            if (!detailsOn) {
              this.clearHistoryTimer()
            }

            if (!detailsOn) {
              if (detailType === 'agentGroupTask') this.$set(this, 'agentDetailsGroupTaskMemberList', [])
              if (detailType === 'groupTask') this.$set(this, 'groupTaskMemberList', [])
            }

            let taskDetail = data?.data?.chatTaskDto ?? ''
            if (taskDetail) {
              taskDetail = Object.assign({}, detail, taskDetail)
            }
            // 智能体 组 任务
            if (detailType === 'agentGroupTask') {
              this.$set(this, 'agentDetailsGroupDetailsTaskDetails', taskDetail)
            }
            // 组 任务
            if (detailType === 'groupTask') {
              this.$set(this, 'groupTaskDetails', taskDetail)
            }
            // 智能体 任务
            if (detailType === 'agentTask') {
              this.$set(this, 'agentDetailsTaskDetails', taskDetail)
            }
            // 任务
            if (detailType === 'task') {
              this.$set(this, 'taskDetails', taskDetail)
            }

            if (!detailsOn && taskDetail) {
              if (detailType === 'task') this.$set(this, 'taskHistoryList', [])
              if (detailType === 'agentTask') this.$set(this, 'agentDetailsTaskHistoryList', [])
              if (detailType === 'agentGroupTask') this.$set(this, 'agentDetailsGroupTaskHistoryList', [])
              if (detailType === 'groupTask') this.$set(this, 'groupTaskHistoryList', [])

              // 打开任务详情时检测一次远程 A2A 成员；后续仅由用户手动触发重测。
              const taskMemberList = await this.getTaskMemberListData(detailType, taskDetail.chatGroupId)
              await this.autoTestRemoteParticipants(taskMemberList)
              // 首次获取历史 + 开启实时流
              this.getTaskRecordListData(detailType, taskDetail.id, '', true)
              this.startTaskHistoryStream(detailType, taskDetail.id, taskDetail.status)
            }
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
    },
    // 获取 任务历史记录
    async getTaskRecordListData(recordType, chatTaskId, nextHistoryId, isFirst = false) {
      const queryList = {
        chatTaskId,
        nextHistoryId
      }
      //  接口对接
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatGroupHistoryAppService/Xncf.AgentsManager_ChatGroupHistoryAppService.GetList?${getInterfaceQueryStr(queryList)}`, queryList)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const chatGroupHistories = data?.data?.chatGroupHistories ?? []
            const historiesData = chatGroupHistories.map(item => {
              //使用 MarkDown 格式，对输出结果进行展示
              item.messageHtml = this.renderSafeMarkdown(item.message);
              return item
            })
            if (historiesData.length > 0) {
              this.clearTaskGeneratingPlaceholder(recordType)
            }
            // 任务
            if (recordType === 'task') {
              const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef('task'), isFirst)
              let historyList = this.taskHistoryList || []
              if (nextHistoryId) {
                // for (let index = 0; index < historiesData.length; index++) {
                //     const element = historiesData[index];
                //     setTimeout(() => {
                //         this.taskHistoryList.push(element)
                //     }, 1000)
                // }
                if (historiesData.length > 0) {
                  historyList = this.taskHistoryList.concat(historiesData);
                  this.$set(this, 'taskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.taskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  historyList = historiesData
                  this.$set(this, 'taskHistoryList', historiesData)
                }
              }
              this.$nextTick(() => {
                if (!shouldAutoFollow) return
                const latestId = historyList.length > 0 ? historyList[historyList.length - 1].id : null
                this.scrollHistoryToItemBottom('task', latestId)
              })
            }
            // 智能体 任务
            if (recordType === 'agentTask') {
              const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef('agentTask'), isFirst)
              let historyList = this.agentDetailsTaskHistoryList || []
              if (nextHistoryId) {
                // for (let index = 0; index < historiesData.length; index++) {
                //     const element = historiesData[index];
                //     setTimeout(() => {
                //         this.taskHistoryList.push(element)
                //     }, 1000)
                // }
                if (historiesData.length > 0) {
                  historyList = this.agentDetailsTaskHistoryList.concat(historiesData);
                  this.$set(this, 'agentDetailsTaskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.agentDetailsTaskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  historyList = historiesData
                  this.$set(this, 'agentDetailsTaskHistoryList', historiesData)
                }
              }
              this.$nextTick(() => {
                if (!shouldAutoFollow) return
                const latestId = historyList.length > 0 ? historyList[historyList.length - 1].id : null
                this.scrollHistoryToItemBottom('agentTask', latestId)
              })
            }
            // 智能体 组 任务
            if (recordType === 'agentGroupTask') {
              const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef('agentGroupTask'), isFirst)
              let historyList = this.agentDetailsGroupTaskHistoryList || []
              if (nextHistoryId) {
                // for (let index = 0; index < historiesData.length; index++) {
                //     const element = historiesData[index];
                //     setTimeout(() => {
                //         this.taskHistoryList.push(element)
                //     }, 1000)
                // }
                if (historiesData.length > 0) {
                  historyList = this.agentDetailsGroupTaskHistoryList.concat(historiesData);
                  this.$set(this, 'agentDetailsGroupTaskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.agentDetailsGroupTaskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  historyList = historiesData
                  this.$set(this, 'agentDetailsGroupTaskHistoryList', historiesData)
                }
              }
              this.$nextTick(() => {
                if (!shouldAutoFollow) return
                const latestId = historyList.length > 0 ? historyList[historyList.length - 1].id : null
                this.scrollHistoryToItemBottom('agentGroupTask', latestId)
              })
            }
            // 组 任务
            if (recordType === 'groupTask') {
              const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef('groupTask'), isFirst)
              let historyList = this.groupTaskHistoryList || []
              if (nextHistoryId) {
                if (historiesData.length > 0) {
                  historyList = this.groupTaskHistoryList.concat(historiesData);
                  this.$set(this, 'groupTaskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.groupTaskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  historyList = historiesData
                  this.$set(this, 'groupTaskHistoryList', historiesData)
                }
              }
              this.$nextTick(() => {
                if (!shouldAutoFollow) return
                const latestId = historyList.length > 0 ? historyList[historyList.length - 1].id : null
                this.scrollHistoryToItemBottom('groupTask', latestId)
              })
            }
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
    },
    // 获取 任务 成员列表
    async getTaskMemberListData(memberType, chatGroupld) {
      try {
        const res = await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${chatGroupld}`)
        const data = res?.data ?? {}
        if (!data.success) {
          throw new Error(data.errorMessage || data.data || '加载任务成员失败')
        }

        const groupDetail = data?.data ?? {}
        const taskMemberList = this.getGroupParticipantList(groupDetail)

        // 任务详情页的成员面板使用组详情状态渲染。启动任务后，任务详情请求可能
        // 先于组详情请求完成；用这里取得的完整组数据回填对应状态，避免右侧面板
        // 因为竞态暂时没有 groupDetails 而显示为空，重新进入页面才恢复。
        const mergeTaskGroupDetail = (currentDetail) => {
          const current = currentDetail || {}
          return Object.assign({}, current, groupDetail, {
            chatGroupDto: Object.assign({}, current.chatGroupDto || {}, groupDetail.chatGroupDto || {})
          })
        }

        if (memberType === 'groupTask') {
          this.$set(this, 'groupDetails', mergeTaskGroupDetail(this.groupDetails))
        }
        if (memberType === 'agentGroupTask') {
          this.$set(this, 'agentDetailsGroupDetails', mergeTaskGroupDetail(this.agentDetailsGroupDetails))
          this.$set(this, 'agentDetailsGroupTaskMemberList', taskMemberList)
        }
        if (memberType === 'groupTask') {
          this.$set(this, 'groupTaskMemberList', taskMemberList)
        }

        // 任务
        if (memberType === 'task') {
          this.$set(this, 'taskMemberList', taskMemberList)
        }
        // 智能体 任务
        if (memberType === 'agentTask') {
          this.$set(this, 'agentDetailsTaskMemberList', taskMemberList)
        }
        return taskMemberList
      } catch (err) {
        app.$message({
          message: err?.message || '加载任务成员失败',
          type: 'error',
          duration: 5 * 1000
        })
        return []
      }
    },
    openUsageAnalytics(detailType, taskDetail) {
      if (!taskDetail || !taskDetail.id) return

      this.usageAnalyticsTaskId = taskDetail.id
      this.usageAnalyticsTaskName = taskDetail.name || `Task-${taskDetail.id}`
      this.usageAnalyticsVisible = true

      let members = []
      if (detailType === 'task') members = this.taskMemberList || []
      if (detailType === 'agentTask') members = this.agentDetailsTaskMemberList || []
      if (['groupTask', 'agentGroupTask'].includes(detailType)) {
        members = this.getTaskParticipantList(detailType)
      }

      this.usageAnalyticsAgentOptions = members.map(item => ({
        id: item.id,
        name: item.name
      }))

      this.loadUsageAnalytics()
    },
    resetUsageAnalyticsFilters() {
      this.usageAnalyticsDateRange = []
      this.usageAnalyticsAgentId = ''
      this.loadUsageAnalytics()
    },
    async loadUsageAnalytics() {
      if (!this.usageAnalyticsTaskId) return
      this.usageAnalyticsLoading = true

      const query = {
        chatTaskId: this.usageAnalyticsTaskId
      }
      if (this.usageAnalyticsAgentId) {
        query.agentTemplateId = this.usageAnalyticsAgentId
      }
      if (this.usageAnalyticsDateRange && this.usageAnalyticsDateRange.length === 2) {
        query.startTime = this.usageAnalyticsDateRange[0]
        query.endTime = this.usageAnalyticsDateRange[1]
      }

      try {
        const res = await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatGroupHistoryAppService/Xncf.AgentsManager_ChatGroupHistoryAppService.GetUsageAnalytics?${getInterfaceQueryStr(query)}`)
        const data = res?.data ?? {}
        if (!data.success) {
          this.$message.error(data.errorMessage || data.data || '获取统计数据失败')
          return
        }

        const payload = data.data || {}
        this.usageAnalyticsData = {
          overview: payload.overview || this.$options.data().usageAnalyticsData.overview,
          roundStats: payload.roundStats || [],
          agentStats: payload.agentStats || [],
          timelineStats: payload.timelineStats || []
        }
      } catch (e) {
        console.error(e)
        this.$message.error('获取统计数据失败')
      } finally {
        this.usageAnalyticsLoading = false
      }
    },
    getDefaultTaskUsageSummary() {
      return {
        messageCount: 0,
        promptTokens: 0,
        completionTokens: 0,
        totalTokens: 0,
        averageResponseMilliseconds: 0,
        maxResponseMilliseconds: 0,
      }
    },
    buildTaskHistoryUsageSummary(historyList = []) {
      const summary = this.getDefaultTaskUsageSummary()
      if (!Array.isArray(historyList) || historyList.length === 0) {
        return summary
      }

      let responseCount = 0
      let responseTotalMs = 0

      historyList.forEach((item) => {
        if (!item || item._generating) {
          return
        }

        summary.messageCount += 1

        const promptTokens = Number(item.promptTokens || 0) || 0
        const completionTokens = Number(item.completionTokens || 0) || 0
        const itemTotalTokens = Number(item.totalTokens || 0) || 0
        const totalTokens = itemTotalTokens > 0 ? itemTotalTokens : (promptTokens + completionTokens)

        summary.promptTokens += promptTokens
        summary.completionTokens += completionTokens
        summary.totalTokens += totalTokens

        const responseMilliseconds = Number(item.responseMilliseconds || 0) || 0
        if (responseMilliseconds > 0) {
          responseCount += 1
          responseTotalMs += responseMilliseconds
          if (responseMilliseconds > summary.maxResponseMilliseconds) {
            summary.maxResponseMilliseconds = responseMilliseconds
          }
        }
      })

      if (responseCount > 0) {
        summary.averageResponseMilliseconds = Math.round(responseTotalMs / responseCount)
      }

      return summary
    },
    formatUsageCount(value) {
      const numeric = Number(value || 0)
      if (!Number.isFinite(numeric)) return '0'
      return numeric.toLocaleString('en-US')
    },
    formatActivityTime(value) {
      return value ? formatDate(value) : '暂无'
    },
    formatResponseMilliseconds(milliseconds, emptyText = '--') {
      const numeric = Number(milliseconds || 0)
      if (!Number.isFinite(numeric) || numeric <= 0) {
        return emptyText
      }

      const rounded = Math.round(numeric)
      if (rounded < 1000) {
        return `${rounded}ms`
      }

      const seconds = Math.floor(rounded / 1000)
      const remainMilliseconds = rounded % 1000
      return `${seconds}s${remainMilliseconds}ms`
    },
    formatTaskHistoryUsage(history) {
      const promptTokens = Number(history?.promptTokens || 0) || 0
      const completionTokens = Number(history?.completionTokens || 0) || 0
      const totalTokens = Number(history?.totalTokens || 0) || (promptTokens + completionTokens)
      const responseMs = history?.responseMilliseconds || 0
      const roundText = history?.roundIndex ? `R${history.roundIndex} - ` : ''
      return `${roundText}Token: ${totalTokens}${responseMs > 0 ? ` · ${this.formatResponseMilliseconds(responseMs, '')}` : ''}`
    },
    getTaskArchiveScopeCode(scope = this.taskArchiveScope) {
      const scopeMap = {
        active: 0,
        archived: 1,
        all: 2
      }
      return scopeMap[scope] ?? 0
    },
    setTaskArchiveScope(scope) {
      if (!scope || this.taskArchiveScope === scope) return
      this.taskArchiveScope = scope
      this.clearHistoryTimer()
      this.scrollbarTaskIndex = ''
      this.taskDetails = ''
      this.taskHistoryList = []
      this.taskMemberList = []
      this.taskMemberfilter = ''
      this.taskMemberfilterList = []
      this.gettaskListData('task')
      this.syncHashRoute({ tab: 'third', taskId: null })
    },
    async handleTaskArchiveToggle(item) {
      if (!item || !item.id) return
      const nextArchived = !item.isArchived
      const actionText = nextArchived ? ncfST('归档') : ncfST('取消归档')
      this.taskArchiveSavingId = item.id
      try {
        const response = await serviceAM.post(
          `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.SetArchiveStatus?id=${item.id}&isArchived=${nextArchived}`
        )
        const data = response?.data ?? {}
        if (!data.success) {
          this.$message.error(data.errorMessage || data.data || ncfST('{0}失败', actionText))
          return
        }

        this.$message.success(ncfST('{0}成功', actionText))
        const currentTaskId = Number(this.taskDetails?.id || 0)
        if (currentTaskId === Number(item.id) && this.taskArchiveScope !== 'all' && this.taskArchiveScope !== (nextArchived ? 'archived' : 'active')) {
          this.scrollbarTaskIndex = ''
          this.taskDetails = ''
          this.taskHistoryList = []
          this.taskMemberList = []
          this.taskMemberfilter = ''
          this.taskMemberfilterList = []
        }
        this.gettaskListData('task')
      } catch (error) {
        this.$message.error(ncfST('{0}失败：{1}', actionText, error?.message || ncfST('未知错误')))
      } finally {
        this.taskArchiveSavingId = 0
      }
    },
    getGroupStartChatGroupId(serviceForm = {}) {
      const candidates = [
        serviceForm?.chatGroupId,
        serviceForm?.chatGroupDto?.id,
        serviceForm?.groupId,
        this.groupStartForm?.chatGroupId,
        this.groupTaskDetails?.chatGroupId,
        this.groupDetails?.chatGroupDto?.id,
        this.scrollbarGroupIndex,
        this.groupTaskQueryList?.chatGroupId,
        this.agentDetailsGroupDetailsTaskDetails?.chatGroupId,
        this.agentDetailsGroupDetails?.chatGroupDto?.id,
        this.agentDetailsGroupTaskQueryList?.chatGroupId,
        this.agentDetailsTaskDetails?.chatGroupId,
        this.taskDetails?.chatGroupId,
        typeof window !== 'undefined'
          ? window.location.hash.match(/(?:#|&)groupId=(\d+)/)?.[1]
          : ''
      ]

      for (const candidate of candidates) {
        const id = Number(candidate)
        if (Number.isInteger(id) && id > 0) {
          return id
        }
      }

      return 0
    },
    // 保存 submitForm 数据
    async saveSubmitFormData(saveType, serviceForm = {}) {
      //debugger
      let serviceURL = ''
      // agent 新增|编辑
      if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
        // 确保 serviceForm 是正确的对象
        serviceForm = serviceForm || {};

        serviceForm.functionBindings = (serviceForm.functionBindings || this.agentForm.functionBindings || [])
          .map(item => this.normalizeFunctionBinding(item))
          .filter(Boolean)
        serviceForm.functionCallNames = serviceForm.functionBindings
          .filter(item => item.kind === 'plugin')
          .map(item => item.key)
          .join(',')

        // 打印日志以便调试
        console.log('Submitting serviceForm:', serviceForm);
        console.log('functionCallTags:', this.functionCallTags);
        console.log('functionCallNames:', serviceForm.functionCallNames);

        serviceURL = '/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.SetItem'
        if (saveType === 'dialogGroupAgent') {
          this.isGetGroupAgent = true
        }
      }
      // 组 新增|编辑
      if (saveType === 'drawerGroup') {
        serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.SetChatGroup'
        const memberAgentTemplateIds = (serviceForm.members || [])
          .filter(item => item && item.isHuman !== true)
          .map(item => item.id)
        const remoteAgentIds = (serviceForm.remoteMembers || []).map(item => item.id)
        serviceForm.memberAgentTemplateIds = memberAgentTemplateIds
        serviceForm.remoteAgentIds = remoteAgentIds
        serviceURL += `?${getInterfaceQueryStr({ memberAgentTemplateIds, remoteAgentIds, includeHumanParticipant: !!serviceForm.includeHumanParticipant })}`
      }
      // 组启动（运行任务） ['drawerGroupStart', 'drawerTaskStart'].includes(btnType)
      if (['drawerGroupStart', 'drawerTaskStart'].includes(saveType)) {
        const chatGroupId = this.getGroupStartChatGroupId(serviceForm)
        if (!chatGroupId) {
          app.$message({
            message: '未找到有效的聊天组，请重新从聊天组或任务详情打开启动窗口。',
            type: 'error',
            duration: 5 * 1000
          })
          return
        }

        // 启动窗口可能由任务详情打开；此时表单对象只携带了部分字段，
        // 统一把当前上下文解析出的 Group ID 写回请求，避免后台收到 0。
        serviceForm.chatGroupId = chatGroupId
        if (serviceForm.requireHumanApproval && Number(serviceForm.humanInTheLoopLevel || 0) === 0) {
          serviceForm.humanInTheLoopLevel = 2
        }
        serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.RunGroup'
        // RunGroup 将聊天组 ID 同时作为查询参数和正文属性提交。查询参数由动态 ApiBind
        // 控制器显式绑定，即使旧脚本或宿主环境异常处理了正文属性，后台仍能可靠取得 Group ID。
        serviceURL += `?${getInterfaceQueryStr({ chatGroupId })}`
      }
      if (saveType === 'dialogTaskEvaluation') {
        serviceURL = ''
      }
      if (!serviceURL) return
      try {
        const response = await serviceAM.post(serviceURL, serviceForm)
        if (response.data.success) {
          let refName = '', formName = ''
          // 智能体
          if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
            refName = 'agentELForm'
            formName = 'agentForm'
          }
          // 组
          if (saveType === 'drawerGroup') {
            refName = 'groupELForm'
            formName = 'groupForm'
            // 重置 组获取智能体query
            this.$set(this, 'groupAgentQueryList', this.$options.data().groupAgentQueryList)
            this.$set(this, 'groupRemoteAgentQueryList', this.$options.data().groupRemoteAgentQueryList)
            this.groupRemoteAgentList = []
          }
          // 组 启动
          if (['drawerGroupStart', 'drawerTaskStart'].includes(saveType)) {
            refName = 'groupStartELForm'
            formName = 'groupStartForm'
          }
          // 任务评价
          if (saveType === 'dialogTaskEvaluation') {
            refName = 'evaluationELForm'
            formName = 'evaluationForm'
          }
          if (formName) {
            this.$set(this, `${formName}`, this.$options.data()[formName])
            // Object.assign(this[formName],this.$options.data()[formName] )
          }
          if (refName) {
            this.$refs[refName].resetFields();
          }
          if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
            this.agentSystemMessageTypeDetectionPending = false
          }
          delete this.editorFormInitialSnapshots[this.getEditorVisibleKey(saveType)]
          this.$nextTick(() => {
            this.visible[this.getEditorVisibleKey(saveType)] = false
          })
          // 重新获取数据
          if (['drawerGroup', 'drawerGroupStart', 'drawerTaskStart'].includes(saveType)) {
            console.log('#***#', this.tabsActiveName, this.agentDetails);
            const isStartTaskSave = ['drawerTaskStart', 'drawerGroupStart'].includes(saveType)

            if (this.tabsActiveName === 'first') {
              // agentTemplateStatus
              if (this.agentDetails) {
                const id = this.agentDetails.agentTemplateDto ? this.agentDetails.agentTemplateDto.id : this.agentDetails.id
                if (this.agentDetailsTabsActiveName === 'first') {
                  if (isStartTaskSave) {
                    const focusChatGroupId = serviceForm?.chatGroupId
                      || this.agentDetailsGroupDetailsTaskDetails?.chatGroupId
                      || this.agentDetailsGroupDetails?.chatGroupDto?.id
                      || ''
                    if (focusChatGroupId) {
                      const refreshOptions = this.buildTaskRefreshOptions('agentGroupTask', {
                        preferLatest: true,
                        focusChatGroupId
                      }, saveType)
                      this.gettaskListData('agentGroupTask', focusChatGroupId, 0, refreshOptions)
                    } else {
                      this.getGroupListData('agentGroup', id)
                    }
                  } else {
                    this.getGroupListData('agentGroup', id)
                  }
                } else {
                  const refreshOptions = this.buildTaskRefreshOptions('agentTask', {
                    preferLatest: isStartTaskSave,
                    focusChatGroupId: serviceForm?.chatGroupId || ''
                  }, saveType)
                  this.gettaskListData('agentTask', id, 0, refreshOptions)
                }
              }
            } else if (this.tabsActiveName === 'second') {
              if (isStartTaskSave) {
                const focusChatGroupId = serviceForm?.chatGroupId
                  || this.groupTaskDetails?.chatGroupId
                  || this.groupDetails?.chatGroupDto?.id
                  || ''
                if (focusChatGroupId) {
                  const refreshOptions = this.buildTaskRefreshOptions('groupTask', {
                    preferLatest: true,
                    focusChatGroupId
                  }, saveType)
                  this.gettaskListData('groupTask', focusChatGroupId, 0, refreshOptions)
                } else {
                  this.getGroupListData('group')
                }
              } else {
                this.getGroupListData('group')
              }
            } else {
              const refreshOptions = this.buildTaskRefreshOptions('task', {
                preferLatest: isStartTaskSave,
                focusChatGroupId: serviceForm?.chatGroupId || ''
              }, saveType)
              this.gettaskListData('task', '', 0, refreshOptions)
            }
          } else if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
            const agentMapStr = {
              'drawerAgent': 'agent',
              'dialogGroupAgent': 'groupAgent'
            }
            this.getAgentListData(agentMapStr[saveType])
            if (saveType === 'drawerAgent') {
              await this.refreshAgentParameterItem(response.data?.data)
            }
          } else if (saveType === 'dialogTaskEvaluation') {
            // 重新获取任务详情 
            let detail = {}
            if (this.tabsActiveName === 'first') {
              if (this.agentDetailsTabsActiveName === 'first') {
                detail.serviceType = 'agentGroupTask'
                detail.id = this.agentDetailsGroupDetailsTaskDetails?.id ?? ''
              } else if (this.agentDetailsTabsActiveName === 'second') {
                detail.serviceType = 'agentTask'
                detail.id = this.agentDetailsTaskDetails?.id ?? ''
              }
            } else if (this.tabsActiveName === 'second') {
              detail.serviceType = 'groupTask'
              detail.id = this.groupTaskDetails?.id ?? ''
            } else {
              detail.serviceType = 'task'
              detail.id = this.taskDetails?.id ?? ''
            }
            if (detail.id) {
              // detailType, id, detail = {} true
              this.getTaskDetailData(detail.serviceType, detail.id, detail, true)
            }
          }

          if (['drawerGroup', 'drawerGroupStart', 'drawerTaskStart'].includes(saveType)) {
            this.refreshQuickJumpTaskOptions()
          }
        } else {
          console.error('API Error:', response.data);
          app.$message({
            message: response.data.errorMessage || response.data.data || 'Error',
            type: 'error',
            duration: 5 * 1000
          })
          this.isGetGroupAgent = false
        }
      } catch (err) {
        console.error('Request Error:', err);
        this.isGetGroupAgent = false
      }
    },
    // 轮询获取 task 历史对话记录
    pollGetTaskHistoryData(listType, fun, id) {
      if (!listType || !fun) return
      fun(listType, id, '', true)
      const interval = () => {
        if (this.historyTimer[listType]) clearTimeout(this.historyTimer[listType])
        this.historyTimer[listType] = setTimeout(() => {
          const nextHistoryId = this.getLatestPersistedHistoryId(listType)
          // 执行代码块
          fun(listType, id, nextHistoryId || '')
          interval()
        }, 1000 * 5)
        // console.log('pollGetTaskHistoryData', this.historyTimer[listType]);
      }
      interval()
    },
    getHistoryListByType(listType) {
      if (listType === 'task') return this.taskHistoryList || []
      if (listType === 'agentTask') return this.agentDetailsTaskHistoryList || []
      if (listType === 'agentGroupTask') return this.agentDetailsGroupTaskHistoryList || []
      if (listType === 'groupTask') return this.groupTaskHistoryList || []
      return []
    },
    shouldShowTaskGenerating(status) {
      return [1, 2].includes(Number(status))
    },
    shouldSubscribeTaskStream(status) {
      return [0, 1, 2].includes(Number(status))
    },
    buildTaskGeneratingItem(listType, chatTaskId) {
      const nowIso = new Date().toISOString()
      return {
        id: `${listType}:generating:${chatTaskId || 'unknown'}`,
        fromAgentTemplateId: 0,
        addTime: nowIso,
        message: 'Generating...',
        messageHtml: this.renderSafeMarkdown('Generating...'),
        promptTokens: 0,
        completionTokens: 0,
        totalTokens: 0,
        responseMilliseconds: 0,
        roundIndex: 0,
        _streaming: true,
        _generating: true,
        _streamAgentName: 'Generating...'
      }
    },
    ensureTaskGeneratingPlaceholder(listType, chatTaskId) {
      if (!listType) return
      const historyList = this.getHistoryListByType(listType).slice()
      const existedIndex = historyList.findIndex(item => item && item._generating)
      if (existedIndex > -1) {
        return historyList[existedIndex]
      }

      const placeholder = this.buildTaskGeneratingItem(listType, chatTaskId)
      historyList.push(placeholder)
      this.setHistoryListByType(listType, historyList)
      this.$nextTick(() => {
        this.scrollHistoryToItemBottom(listType, placeholder.id)
      })
      return placeholder
    },
    clearTaskGeneratingPlaceholder(listType) {
      if (!listType) return
      const historyList = this.getHistoryListByType(listType)
      if (!Array.isArray(historyList) || historyList.length === 0) return

      const filtered = historyList.filter(item => !item || item._generating !== true)
      if (filtered.length !== historyList.length) {
        this.setHistoryListByType(listType, filtered)
      }
    },
    getLatestPersistedHistoryId(listType) {
      const historyList = this.getHistoryListByType(listType)
      if (!Array.isArray(historyList) || historyList.length === 0) return 0

      for (let index = historyList.length - 1; index >= 0; index--) {
        const item = historyList[index]
        const historyId = Number(item?.id || 0)
        if (Number.isFinite(historyId) && historyId > 0) {
          return historyId
        }
      }
      return 0
    },
    pullTaskHistoryAfterStreamClosed(listType, chatTaskId) {
      if (!listType || !chatTaskId) return
      const nextHistoryId = this.getLatestPersistedHistoryId(listType)
      this.getTaskRecordListData(listType, chatTaskId, nextHistoryId || '', false)
    },
    setHistoryListByType(listType, list) {
      if (listType === 'task') this.$set(this, 'taskHistoryList', list)
      if (listType === 'agentTask') this.$set(this, 'agentDetailsTaskHistoryList', list)
      if (listType === 'agentGroupTask') this.$set(this, 'agentDetailsGroupTaskHistoryList', list)
      if (listType === 'groupTask') this.$set(this, 'groupTaskHistoryList', list)
    },
    getHistoryScrollbarRef(listType) {
      const scrollbarMap = {
        task: 'taskHistoryScrollbar',
        agentTask: 'agentDetailsTaskHistoryScrollbar',
        agentGroupTask: 'agentDetailsGroupTaskHistoryScrollbar',
        groupTask: 'groupTaskHistoryScrollbar'
      }
      return scrollbarMap[listType] || ''
    },
    isHistoryNearBottom(refName, isFirst = false) {
      if (isFirst) return true
      if (!refName) return true
      const scrollbar = this.$refs[refName]
      if (!scrollbar || !scrollbar.wrap) return true
      const wrap = scrollbar.wrap
      const scrollTop = wrap.scrollTop
      const scrollHeight = wrap.scrollHeight
      const clientHeight = wrap.clientHeight
      if (scrollHeight <= clientHeight) return true
      return scrollTop + clientHeight + 30 >= scrollHeight
    },
    scrollHistoryToItemBottom(listType, historyId, behavior = 'auto') {
      const refName = this.getHistoryScrollbarRef(listType)
      if (!refName) return
      const scrollbar = this.$refs[refName]
      if (!scrollbar || !scrollbar.wrap) return

      const wrap = scrollbar.wrap
      if (historyId === undefined || historyId === null || historyId === '') {
        wrap.scrollTop = wrap.scrollHeight
        return
      }

      const findTarget = () => {
        const historyItems = wrap.querySelectorAll('.taskrecord-listWrap-item[data-history-id]')
        for (let index = 0; index < historyItems.length; index++) {
          const item = historyItems[index]
          if (String(item.getAttribute('data-history-id')) === String(historyId)) {
            return item
          }
        }
        return null
      }

      let target = findTarget()
      const scrollWrapToTargetBottom = (targetItem) => {
        if (!targetItem) return
        const targetBottom = targetItem.offsetTop + targetItem.offsetHeight
        const nextTop = Math.max(0, targetBottom - wrap.clientHeight)
        if (behavior === 'smooth' && typeof wrap.scrollTo === 'function') {
          wrap.scrollTo({ top: nextTop, behavior: 'smooth' })
        } else {
          wrap.scrollTop = nextTop
        }
      }
      if (target) {
        scrollWrapToTargetBottom(target)
        return
      }

      requestAnimationFrame(() => {
        target = findTarget()
        if (target) {
          scrollWrapToTargetBottom(target)
        }
      })
    },
    startTaskHistoryStream(listType, chatTaskId, taskStatus) {
      if (!listType || !chatTaskId) {
        return
      }

      this.closeTaskHistoryStream(listType)

      this.setCurrentTaskStatusByType(listType, chatTaskId, taskStatus)
      if (!this.shouldSubscribeTaskStream(taskStatus)) {
        this.clearTaskGeneratingPlaceholder(listType)
        return
      }
      if (this.shouldShowTaskGenerating(taskStatus)) {
        this.ensureTaskGeneratingPlaceholder(listType, chatTaskId)
      }

      if (typeof EventSource === 'undefined') {
        this.pollGetTaskHistoryData(listType, this.getTaskRecordListData, chatTaskId)
        return
      }

      const streamUrl = `/api/Senparc.Xncf.AgentsManager/ChatTaskStream/Subscribe?chatTaskId=${chatTaskId}&replayBuffered=false&_ts=${Date.now()}`
      const source = new EventSource(streamUrl, { withCredentials: true })
      this.historyStream[listType] = source
      this.loadPendingHumanRequests(chatTaskId)
      this.resetTaskHistoryStreamSilentTimer(listType, chatTaskId)

      const rearmSilentTimer = () => {
        if (this.historyStream[listType] !== source) return
        this.resetTaskHistoryStreamSilentTimer(listType, chatTaskId)
      }

      const onChunk = (event) => {
        if (this.historyStream[listType] !== source) return
        this.clearTaskHistoryStreamSilentTimer(listType)
        this.clearTaskGeneratingPlaceholder(listType)
        this.upsertTaskStreamChunk(listType, event)
        rearmSilentTimer()
      }
      const onMessage = (event) => {
        if (this.historyStream[listType] !== source) return
        this.clearTaskHistoryStreamSilentTimer(listType)
        this.clearTaskGeneratingPlaceholder(listType)
        this.flushTaskStreamMessage(listType, event)
        rearmSilentTimer()
      }
      const onStatus = (event) => {
        if (this.historyStream[listType] !== source) return
        this.clearTaskHistoryStreamSilentTimer(listType)
        const payload = this.safeParseStreamEvent(event)
        if (!payload || !payload.text) {
          rearmSilentTimer()
          return
        }

        const statusText = String(payload.text).toLowerCase().trim()
        const statusCodeMap = {
          chatting: 1,
          paused: 2,
          finished: 3,
          completed: 3,
          done: 3,
          cancelled: 4,
          canceled: 4,
          failed: 5,
          error: 5
        }
        const nextStatus = statusCodeMap[statusText]
        if (Number.isFinite(nextStatus)) {
          this.setCurrentTaskStatusByType(listType, chatTaskId, nextStatus)
        }

        if ([3, 4, 5].includes(Number(nextStatus))) {
          this.closeTaskHistoryStream(listType)
          this.clearTaskGeneratingPlaceholder(listType)
          this.pullTaskHistoryAfterStreamClosed(listType, chatTaskId)
          return
        }

        if (this.shouldShowTaskGenerating(nextStatus)) {
          this.ensureTaskGeneratingPlaceholder(listType, chatTaskId)
        } else {
          this.clearTaskGeneratingPlaceholder(listType)
        }
        rearmSilentTimer()
      }

      const onHumanRequest = (event) => {
        if (this.historyStream[listType] !== source) return
        this.clearTaskHistoryStreamSilentTimer(listType)
        const payload = this.safeParseStreamEvent(event)
        if (payload) {
          this.setCurrentTaskStatusByType(listType, chatTaskId, 2)
          this.handleHumanApprovalRequest(payload)
        }
        rearmSilentTimer()
      }
      const onHumanResolved = (event) => {
        if (this.historyStream[listType] !== source) return
        const payload = this.safeParseStreamEvent(event)
        const requestId = this.getHumanRequestId(payload)
        if (requestId) {
          this.removeResolvedHumanRequest(requestId)
        }
        rearmSilentTimer()
      }

      source.addEventListener('chunk', onChunk)
      source.addEventListener('message', onMessage)
      source.addEventListener('status', onStatus)
      source.addEventListener('humanRequest', onHumanRequest)
      source.addEventListener('humanResolved', onHumanResolved)

      source.onerror = () => {
        if (this.historyStream[listType] !== source) return
        this.clearTaskHistoryStreamSilentTimer(listType)
        this.closeTaskHistoryStream(listType)
        this.clearTaskGeneratingPlaceholder(listType)
        this.pullTaskHistoryAfterStreamClosed(listType, chatTaskId)
        this.pollGetTaskHistoryData(listType, this.getTaskRecordListData, chatTaskId)
      }
    },
    getHumanRequestId(payload) {
      return String(payload?.humanRequestId || payload?.requestId || '')
    },
    formatToolApprovalArguments(rawArguments) {
      let value = rawArguments
      if (value === undefined || value === null || String(value).trim() === '') {
        return '（未提供参数）'
      }

      for (let index = 0; index < 2 && typeof value === 'string'; index++) {
        const text = value.trim()
        if (!text || !['{', '[', '"'].includes(text[0])) break
        try {
          value = JSON.parse(text)
        } catch (_) {
          break
        }
      }

      if (typeof value === 'string') {
        return value
      }

      try {
        return JSON.stringify(value, null, 2)
      } catch (_) {
        return String(value)
      }
    },
    showNextToolApproval() {
      if (this.toolApprovalRequest || this.toolApprovalQueue.length === 0) return
      const request = this.toolApprovalQueue.shift()
      this.toolApprovalRequest = request
      this.toolApprovalArgumentText = this.formatToolApprovalArguments(
        request?.humanToolArguments ?? request?.toolArguments)
      this.toolApprovalDialogVisible = true
    },
    async handleHumanApprovalRequest(payload) {
      const requestId = this.getHumanRequestId(payload)
      if (!requestId || this.humanApprovalRequests[requestId]) {
        return
      }

      this.$set(this.humanApprovalRequests, requestId, true)
      if (String(payload?.humanRequestType || payload?.requestType || '').toLowerCase() === 'humanturn') {
        this.humanReplyRequest = payload
        this.humanReplyText = ''
        this.humanReplyDialogVisible = true
        return
      }

      this.toolApprovalQueue.push({
        ...payload,
        requestId
      })
      this.showNextToolApproval()
    },
    async resolveToolApproval(approved) {
      const request = this.toolApprovalRequest
      const requestId = this.getHumanRequestId(request)
      if (!requestId || this.toolApprovalSubmitting) return

      this.toolApprovalSubmitting = true
      try {
        const reason = approved ? '用户确认' : '用户拒绝'
        const query = getInterfaceQueryStr({ requestId, approved, reason })
        const response = await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.ResolveHumanRequest?${query}`)
        const data = response?.data ?? {}
        if (!data.success) {
          this.$message.error(data.errorMessage || data.data || '人工审批提交失败')
          return
        }
        this.$message.success(approved ? '已批准工具调用，任务继续执行' : '已拒绝工具调用，任务继续处理')
        this.toolApprovalDialogVisible = false
        this.toolApprovalRequest = null
        this.toolApprovalArgumentText = ''
        this.$delete(this.humanApprovalRequests, requestId)
        this.$nextTick(() => this.showNextToolApproval())
      } catch (error) {
        this.$message.error(error?.message || '人工审批提交失败')
      } finally {
        this.toolApprovalSubmitting = false
      }
    },
    deferToolApproval() {
      const requestId = this.getHumanRequestId(this.toolApprovalRequest)
      this.toolApprovalDialogVisible = false
      this.toolApprovalRequest = null
      this.toolApprovalArgumentText = ''
      if (requestId) {
        this.$delete(this.humanApprovalRequests, requestId)
      }
      this.$nextTick(() => this.showNextToolApproval())
    },
    removeResolvedHumanRequest(requestId) {
      if (!requestId) return
      this.toolApprovalQueue = this.toolApprovalQueue
        .filter(item => this.getHumanRequestId(item) !== requestId)
      if (this.getHumanRequestId(this.toolApprovalRequest) === requestId) {
        this.toolApprovalDialogVisible = false
        this.toolApprovalRequest = null
        this.toolApprovalArgumentText = ''
        this.$nextTick(() => this.showNextToolApproval())
      }
      this.$delete(this.humanApprovalRequests, requestId)
    },
    async submitHumanReply() {
      const requestId = String(this.humanReplyRequest?.humanRequestId || this.humanReplyRequest?.requestId || '')
      const input = String(this.humanReplyText || '').trim()
      if (!requestId || !input) {
        this.$message.warning('请输入 Human 回复')
        return
      }

      this.humanReplySubmitting = true
      try {
        const response = await serviceAM.post(
          '/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.SendHumanMessage',
          { requestId, input })
        const data = response?.data ?? {}
        if (!data.success) {
          this.$message.error(data.errorMessage || data.data || 'Human 回复提交失败')
          return
        }

        const submittedRequestId = requestId
        this.humanReplyDialogVisible = false
        this.humanReplyRequest = null
        this.humanReplyText = ''
        this.$delete(this.humanApprovalRequests, submittedRequestId)
        this.$message.success('Human 回复已提交')
      } catch (error) {
        this.$message.error(error?.message || 'Human 回复提交失败')
      } finally {
        this.humanReplySubmitting = false
      }
    },
    closeHumanReplyDialog() {
      const requestId = String(this.humanReplyRequest?.humanRequestId || this.humanReplyRequest?.requestId || '')
      this.humanReplyDialogVisible = false
      this.humanReplyRequest = null
      this.humanReplyText = ''
      if (requestId) {
        this.$delete(this.humanApprovalRequests, requestId)
      }
    },
    async loadPendingHumanRequests(chatTaskId) {
      if (!chatTaskId) return
      try {
        const response = await serviceAM.get(
          `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetHumanRequests?chatTaskId=${encodeURIComponent(chatTaskId)}`,
          { customAlert: true }
        )
        const pendingRequests = response?.data?.data || []
        pendingRequests.forEach(request => this.handleHumanApprovalRequest(request))
      } catch (error) {
        console.warn('load pending human approval requests failed', chatTaskId, error)
      }
    },
    clearTaskHistoryStreamSilentTimer(listType) {
      const timer = this.historyStreamSilentTimer[listType]
      if (timer) {
        clearTimeout(timer)
      }
      this.$delete(this.historyStreamSilentTimer, listType)
    },
    resetTaskHistoryStreamSilentTimer(listType, chatTaskId) {
      this.clearTaskHistoryStreamSilentTimer(listType)
      this.historyStreamSilentTimer[listType] = setTimeout(async () => {
        const source = this.historyStream[listType]
        if (!source) return

        try {
          const statusRes = await serviceAM.get(
            `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetItem?id=${chatTaskId}`,
            { customAlert: true }
          )
          const taskStatus = Number(statusRes?.data?.data?.chatTaskDto?.status)
          this.setCurrentTaskStatusByType(listType, chatTaskId, taskStatus)
          if ([3, 4, 5].includes(taskStatus)) {
            this.closeTaskHistoryStream(listType)
            this.clearTaskGeneratingPlaceholder(listType)
            this.pullTaskHistoryAfterStreamClosed(listType, chatTaskId)
            return
          }

          if (this.shouldShowTaskGenerating(taskStatus)) {
            this.ensureTaskGeneratingPlaceholder(listType, chatTaskId)
          } else {
            this.clearTaskGeneratingPlaceholder(listType)
          }
        } catch (e) {
          console.warn('stream silent fallback status check failed', listType, chatTaskId, e)
        }

        // 任务仍在运行但暂无流事件，继续观察，避免长时间 pending 无更新。
        this.resetTaskHistoryStreamSilentTimer(listType, chatTaskId)
      }, 4000)
    },
    safeParseStreamEvent(event) {
      if (!event || !event.data) return null
      try {
        return JSON.parse(event.data)
      } catch (e) {
        console.error('stream parse error', e, event.data)
        return null
      }
    },
    upsertTaskStreamChunk(listType, event) {
      const payload = this.safeParseStreamEvent(event)
      if (!payload || !payload.responseId) return

      const draftKey = `${listType}:${payload.responseId}`
      const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef(listType), false)
      const historyList = this.getHistoryListByType(listType).filter(item => !item || item._generating !== true).slice()
      const existedIndex = historyList.findIndex(item => item.id === draftKey)
      const agentInfo = this.getTaskSenderInfo(listType, payload) || {}
      const oldMessage = existedIndex > -1 ? (historyList[existedIndex].message || '') : ''
      const mergedMessage = `${oldMessage}${payload.text || ''}`

      const draftItem = {
        id: draftKey,
        fromAgentTemplateId: payload.fromAgentTemplateId || 0,
        fromParticipantKey: payload.fromParticipantKey || '',
        fromParticipantKind: payload.fromParticipantKind || '',
        fromParticipantName: payload.fromAgentName || '',
        addTime: payload.timestamp ? new Date(payload.timestamp).toISOString() : new Date().toISOString(),
        message: mergedMessage,
        messageHtml: this.renderSafeMarkdown(mergedMessage || ''),
        promptTokens: payload.promptTokens || 0,
        completionTokens: payload.completionTokens || 0,
        totalTokens: payload.totalTokens || 0,
        responseMilliseconds: payload.responseMilliseconds || 0,
        roundIndex: payload.roundIndex || 0,
        _streaming: true,
        _streamAgentName: payload.fromAgentName || agentInfo.name || '',
      }

      if (existedIndex > -1) {
        historyList.splice(existedIndex, 1, draftItem)
      } else {
        historyList.push(draftItem)
      }

      this.historyStreamingDrafts[draftKey] = draftItem
      this.setHistoryListByType(listType, historyList)
      this.$nextTick(() => {
        if (!shouldAutoFollow) return
        this.scrollHistoryToItemBottom(listType, draftItem.id)
      })
    },
    flushTaskStreamMessage(listType, event) {
      const payload = this.safeParseStreamEvent(event)
      if (!payload) return

      const draftKey = payload.responseId ? `${listType}:${payload.responseId}` : ''
      const shouldAutoFollow = this.isHistoryNearBottom(this.getHistoryScrollbarRef(listType), false)
      const historyList = this.getHistoryListByType(listType).filter(item => !item || item._generating !== true).slice()
      if (draftKey) {
        const draftIndex = historyList.findIndex(item => item.id === draftKey)
        if (draftIndex > -1) {
          historyList.splice(draftIndex, 1)
        }
        delete this.historyStreamingDrafts[draftKey]
      }

      const message = payload.text || ''
      const historyId = Number(payload.historyId || 0)
      const existedFinalIndex = historyId > 0
        ? historyList.findIndex(item => Number(item?.id || 0) === historyId)
        : -1
      if (existedFinalIndex > -1) {
        const existedFinal = historyList[existedFinalIndex] || {}
        const mergedFinal = {
          ...existedFinal,
          fromAgentTemplateId: payload.fromAgentTemplateId || existedFinal.fromAgentTemplateId || 0,
          fromParticipantKey: payload.fromParticipantKey || existedFinal.fromParticipantKey || '',
          fromParticipantKind: payload.fromParticipantKind || existedFinal.fromParticipantKind || '',
          fromParticipantName: payload.fromAgentName || existedFinal.fromParticipantName || '',
          addTime: payload.timestamp ? new Date(payload.timestamp).toISOString() : (existedFinal.addTime || new Date().toISOString()),
          message: message || existedFinal.message || '',
          messageHtml: this.renderSafeMarkdown(message || existedFinal.message || ''),
          promptTokens: payload.promptTokens || existedFinal.promptTokens || 0,
          completionTokens: payload.completionTokens || existedFinal.completionTokens || 0,
          totalTokens: payload.totalTokens || existedFinal.totalTokens || 0,
          responseMilliseconds: payload.responseMilliseconds || existedFinal.responseMilliseconds || 0,
          roundIndex: payload.roundIndex || existedFinal.roundIndex || 0
        }
        historyList.splice(existedFinalIndex, 1, mergedFinal)
        this.setHistoryListByType(listType, historyList)
        this.$nextTick(() => {
          if (!shouldAutoFollow) return
          this.scrollHistoryToItemBottom(listType, mergedFinal.id)
        })
        return
      }

      const finalItem = {
        id: payload.historyId || `${draftKey || 'msg'}:${Date.now()}`,
        fromAgentTemplateId: payload.fromAgentTemplateId || 0,
        fromParticipantKey: payload.fromParticipantKey || '',
        fromParticipantKind: payload.fromParticipantKind || '',
        fromParticipantName: payload.fromAgentName || '',
        addTime: payload.timestamp ? new Date(payload.timestamp).toISOString() : new Date().toISOString(),
        message,
        messageHtml: this.renderSafeMarkdown(message),
        promptTokens: payload.promptTokens || 0,
        completionTokens: payload.completionTokens || 0,
        totalTokens: payload.totalTokens || 0,
        responseMilliseconds: payload.responseMilliseconds || 0,
        roundIndex: payload.roundIndex || 0
      }
      historyList.push(finalItem)

      this.setHistoryListByType(listType, historyList)
      this.$nextTick(() => {
        if (!shouldAutoFollow) return
        this.scrollHistoryToItemBottom(listType, finalItem.id)
      })

      // 每轮 message 落地后立即补一个 Generating 占位，确保下一轮也有可见彩虹提示。
      if (this.historyStream[listType]) {
        this.ensureTaskGeneratingPlaceholder(listType, payload.chatTaskId || '')
      }
    },
    closeTaskHistoryStream(listType) {
      this.clearTaskHistoryStreamSilentTimer(listType)
      const source = this.historyStream[listType]
      if (source) {
        source.close()
      }
      this.$delete(this.historyStream, listType)
      this.clearTaskGeneratingPlaceholder(listType)
    },
    clearTaskHistoryStreams() {
      Object.keys(this.historyStream || {}).forEach((key) => {
        this.closeTaskHistoryStream(key)
      })
      Object.keys(this.historyStreamSilentTimer || {}).forEach((key) => {
        this.clearTaskHistoryStreamSilentTimer(key)
      })
      this.historyStreamingDrafts = {}
    },
    // 清除 获取历史对话记录 的轮询
    clearHistoryTimer() {
      for (const key in this.historyTimer) {
        if (Object.prototype.hasOwnProperty.call(this.historyTimer, key)) {
          const element = this.historyTimer[key];
          // console.log('clearHistoryTimer', element);
          if (element) {
            clearTimeout(element)
          }
        }
      }
      this.clearTaskListRetryTimers()
      this.clearTaskHistoryStreams()
    },

    // 编辑 Dailog|抽屉 按钮 
    async handleEditDrawerOpenBtn(btnType, item) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      //console.log('handleEditDrawerOpenBtn', btnType, item);
      let formName = ''
      // 智能体
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        formName = 'agentForm'
      }
      // 组
      if (btnType === 'drawerGroup') {
        formName = 'groupForm'
      }
      // 组 启动 
      if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) {
        formName = 'groupStartForm'
      }
      // 任务 评价
      if (btnType === 'dialogTaskEvaluation') {
        formName = 'evaluationForm'
      }
      if (formName) {
          if (btnType === 'drawerAgent' && item) {
          console.log('item', item);
          // 创建一个新的对象来存储表单数据
          const formData = item.agentTemplateDto ? { ...item.agentTemplateDto } : { ...item };
          console.log('formData', formData);

          // FunctionBindings 是新契约；没有该字段时回退到旧版插件类名列表。
          const loadedBindings = Array.isArray(formData.functionBindings)
            ? formData.functionBindings
            : (formData.functionCallNames
              ? formData.functionCallNames.split(',').filter(Boolean).map(name => ({
                kind: 'plugin',
                key: name,
                name
              }))
              : [])
          this.$set(this.agentForm, 'functionBindings', loadedBindings.map(item => this.normalizeFunctionBinding(item)).filter(Boolean))
          this.syncLegacyFunctionCallNames()
          this.functionCallTags = this.agentForm.functionCallNames ? this.agentForm.functionCallNames.split(',').filter(Boolean) : [];

          // 将数据赋值给表单
          Object.assign(this[formName], formData);

          // 打印日志以便调试
          console.log('Loaded form data:', formData);
          console.log('functionCallTags:', this.functionCallTags);

          } else if (btnType === 'drawerGroup') {
            // 列表/详情对象字段并不完全一致；始终由专用接口取得本地和远程成员，
            // 这样编辑既有 Group 时不会误清除已配置的 A2A 成员。
            const groupId = item.chatGroupDto ? item.chatGroupDto.id : item.id
            await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${groupId}`)
              .then(res => {
                const data = res?.data ?? {}
                if (data.success) {
                  const groupDetail = data?.data ?? {}
                  Object.assign(this[formName], {
                    ...groupDetail.chatGroupDto,
                    members: groupDetail.agentTemplateDtoList || groupDetail.chatGroupMembers || [],
                    includeHumanParticipant: (groupDetail.agentTemplateDtoList || groupDetail.chatGroupMembers || [])
                      .some(member => member && member.isHuman === true),
                    remoteMembers: (groupDetail.remoteMemberDtoList || []).map(member => member.remoteAgentDto || member)
                  })
                }
              })
          // // 获取 全部智能体数据
          // this.getAgentListData('groupAgent')
        } else if (btnType === 'drawerTaskStart') {
          Object.assign(this[formName], {
            ...item
            // groupName: item?.name ?? ''
          })
        } else {
          Object.assign(this[formName], item)
        }
        // 回显 表单值
        // this.$set(this, `${formName}`, deepClone(item))
        if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)
          && (item?.agentTemplateDto?.id || item?.id)) {
          // systemMessageType 不是持久化字段。先挂载“自选”控件并完成候选项加载，
          // 再根据实际是否存在该 PromptCode 判断类型，避免慢网络下被过早判定为“手动”。
          this.agentSystemMessageTypeDetectionPending = true
          this.$set(this.agentForm, 'systemMessageType', '1')
        }
        // 打开 抽屉
        this.handleElVisibleOpenBtn(btnType)
      }
    },
    buildGroupStartParticipants(groupDetail) {
      const participants = this.getGroupParticipantList(groupDetail)
        .filter(participant => participant && participant.name)
        .map(participant => Object.assign({}, participant, { roles: [] }))
      const participantByKey = new Map(participants.map(participant => [participant.participantKey, participant]))
      const chatGroupDto = groupDetail?.chatGroupDto || groupDetail || {}
      const fallbackRoleAgents = [
        {
          roleName: '群主',
          agentTemplateDto: {
            id: chatGroupDto.adminAgentTemplateId,
            name: chatGroupDto.adminAgentTemplateName
          }
        },
        {
          roleName: '对接人',
          agentTemplateDto: {
            id: chatGroupDto.enterAgentTemplateId,
            name: chatGroupDto.enterAgentTemplateName
          }
        }
      ]
      const roleAgents = (groupDetail?.roleAgentTemplateDtoList || []).concat(fallbackRoleAgents)

      roleAgents.forEach(role => {
        const agent = role?.agentTemplateDto
        const roleName = (role?.roleName || '').trim()
        if (!agent?.id || !agent?.name || !roleName) return

        const participantKey = `local:${agent.id}`
        let participant = participantByKey.get(participantKey)
        if (!participant) {
          participant = Object.assign({}, agent, {
            participantKey,
            agentKind: 'Local',
            roles: []
          })
          participants.push(participant)
          participantByKey.set(participantKey, participant)
        }
        if (!participant.roles.includes(roleName)) {
          participant.roles.push(roleName)
        }
      })

      return participants
    },
    async loadGroupStartParticipants(chatGroupId) {
      const requestedGroupId = Number(chatGroupId || 0)
      if (!requestedGroupId) return

      this.groupStartParticipantLoading = true
      try {
        const response = await serviceAM.post(
          `/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${requestedGroupId}`)
        const data = response?.data ?? {}
        if (!data.success) {
          throw new Error(data.errorMessage || data.data || '加载群组成员失败')
        }
        if (Number(this.groupStartForm.chatGroupId) === requestedGroupId) {
          this.groupStartParticipants = this.buildGroupStartParticipants(data.data || {})
          if (!this.groupStartHumanParticipantTouched) {
            this.groupStartForm.includeHumanParticipant = this.groupStartParticipants
              .some(participant => participant.agentKind === 'Human')
          }
          await this.autoTestRemoteParticipants(this.groupStartParticipants)
        }
      } catch (error) {
        console.warn('loadGroupStartParticipants failed', error)
        if (Number(this.groupStartForm.chatGroupId) === requestedGroupId) {
          this.$message.warning('无法刷新完整组员列表，当前仅显示已加载的组员')
        }
      } finally {
        if (Number(this.groupStartForm.chatGroupId) === requestedGroupId) {
          this.groupStartParticipantLoading = false
        }
      }
    },
    markGroupStartHumanParticipantTouched() {
      this.groupStartHumanParticipantTouched = true
    },
    getGroupStartPromptTextarea() {
      const input = this.$refs?.groupStartPromptCommand
      return input?.$refs?.textarea
        || input?.textarea
        || input?.$el?.querySelector?.('textarea')
        || null
    },
    rememberGroupStartPromptCaret(event) {
      const textarea = event?.target?.tagName === 'TEXTAREA'
        ? event.target
        : this.getGroupStartPromptTextarea()
      if (!textarea) return

      this.groupStartPromptCaretStart = Number.isInteger(textarea.selectionStart)
        ? textarea.selectionStart
        : (this.groupStartForm.promptCommand || '').length
      this.groupStartPromptCaretEnd = Number.isInteger(textarea.selectionEnd)
        ? textarea.selectionEnd
        : this.groupStartPromptCaretStart
    },
    insertGroupStartMention(participant) {
      const participantName = (participant?.name || '').trim()
      if (!participantName) return

      const textarea = this.getGroupStartPromptTextarea()
      const promptCommand = this.groupStartForm.promptCommand || ''
      const start = Number.isInteger(textarea?.selectionStart)
        ? textarea.selectionStart
        : Math.min(this.groupStartPromptCaretStart, promptCommand.length)
      const end = Number.isInteger(textarea?.selectionEnd)
        ? textarea.selectionEnd
        : Math.min(Math.max(start, this.groupStartPromptCaretEnd), promptCommand.length)
      const before = promptCommand.slice(0, start)
      const after = promptCommand.slice(end)
      const prefix = before && !/\s$/.test(before) ? ' ' : ''
      const suffix = after && !/^\s/.test(after) ? ' ' : ''
      const mention = `${prefix}@${participantName}${suffix}`
      const caretPosition = before.length + mention.length

      this.groupStartForm.promptCommand = `${before}${mention}${after}`
      this.groupStartPromptCaretStart = caretPosition
      this.groupStartPromptCaretEnd = caretPosition
      this.$nextTick(() => {
        const currentTextarea = this.getGroupStartPromptTextarea()
        currentTextarea?.focus?.()
        currentTextarea?.setSelectionRange?.(caretPosition, caretPosition)
      })
    },
    getEditorFormName(btnType) {
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) return 'agentForm'
      if (btnType === 'drawerGroup') return 'groupForm'
      if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) return 'groupStartForm'
      if (btnType === 'dialogTaskEvaluation') return 'evaluationForm'
      return ''
    },
    getEditorVisibleKey(btnType) {
      return btnType === 'drawerTaskStart' ? 'drawerGroupStart' : btnType
    },
    normalizeEditorSnapshotValue(value, fieldName = '') {
      if (Array.isArray(value)) {
        const normalized = value.map(item => this.normalizeEditorSnapshotValue(item))
        // Group 成员的选择顺序不影响实际保存结果，避免控件回填时造成伪变更。
        if (['members', 'remoteMembers'].includes(fieldName)) {
          return normalized.sort((left, right) => {
            const leftKey = `${left?.id ?? ''}:${left?.name ?? ''}`
            const rightKey = `${right?.id ?? ''}:${right?.name ?? ''}`
            return leftKey.localeCompare(rightKey)
          })
        }
        return normalized
      }
      if (value && typeof value === 'object') {
        return Object.keys(value)
          .sort()
          .reduce((result, key) => {
            // 这两个字段在提交时由成员列表派生，不属于用户编辑内容。
            if (['memberAgentTemplateIds', 'remoteAgentIds'].includes(key)) return result
            result[key] = this.normalizeEditorSnapshotValue(value[key], key)
            return result
          }, {})
      }
      return typeof value === 'undefined' ? null : value
    },
    buildEditorFormSnapshot(btnType) {
      const formName = this.getEditorFormName(btnType)
      if (!formName) return ''
      const snapshot = { form: this[formName] || {} }
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        snapshot.functionCallTags = this.functionCallTags || []
      }
      return JSON.stringify(this.normalizeEditorSnapshotValue(snapshot))
    },
    captureEditorFormSnapshot(btnType) {
      const visibleKey = this.getEditorVisibleKey(btnType)
      if (!this.visible[visibleKey]) return
      this.$set(this.editorFormInitialSnapshots, visibleKey, this.buildEditorFormSnapshot(btnType))
    },
    isEditorFormDirty(btnType) {
      const visibleKey = this.getEditorVisibleKey(btnType)
      const original = this.editorFormInitialSnapshots[visibleKey]
      // 未取得初始快照时保守处理，避免误丢弃刚刚编辑的内容。
      return !original || original !== this.buildEditorFormSnapshot(btnType)
    },
    closeEditorForm(btnType) {
      const visibleKey = this.getEditorVisibleKey(btnType)
      const formName = this.getEditorFormName(btnType)
      let refName = ''

      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        refName = 'agentELForm'
      }
      if (btnType === 'drawerGroup') {
        refName = 'groupELForm'
        this.$set(this, 'groupAgentQueryList', this.$options.data().groupAgentQueryList)
        this.groupAgentList = []
        this.$set(this, 'groupRemoteAgentQueryList', this.$options.data().groupRemoteAgentQueryList)
        this.groupRemoteAgentList = []
      }
      if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) {
        refName = 'groupStartELForm'
        this.groupStartParticipants = []
        this.groupStartParticipantLoading = false
        this.groupStartHumanParticipantTouched = false
        this.groupStartPromptCaretStart = 0
        this.groupStartPromptCaretEnd = 0
      }
      if (btnType === 'dialogTaskEvaluation') {
        refName = 'evaluationELForm'
      }

      if (formName) {
        this.$set(this, formName, this.$options.data()[formName])
      }
      this.$refs[refName]?.resetFields?.()
      delete this.editorFormInitialSnapshots[visibleKey]
      this.$nextTick(() => {
        this.visible[visibleKey] = false
      })

      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        this.functionCallTags = []
        this.functionCallInputVisible = false
        this.functionCallInputValue = ''
        this.agentAutoAttachXncf = false
        this.agentSystemMessageTypeDetectionPending = false
      }
    },
    // Dailog|抽屉 打开 按钮
    async handleElVisibleOpenBtn(btnType, formData) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      // console.log('通用新增按钮:', btnType);
      let visibleKey = btnType
      // 组 启动
      if (btnType === 'drawerGroupStart') {
        // 详情: formData.chatGroupDto 列表: formData
        const chatGroup = formData?.chatGroupDto || formData || {}
        const chatGroupId = chatGroup.id || chatGroup.chatGroupId || formData?.chatGroupId || ''
        this.groupStartForm.groupName = chatGroup.name || ''
        this.groupStartForm.name = chatGroup.name ? `${chatGroup.name}1` : ''
        this.groupStartForm.chatGroupId = chatGroupId
        this.groupStartParticipants = this.buildGroupStartParticipants(formData)
        this.groupStartHumanParticipantTouched = false
        this.groupStartForm.includeHumanParticipant = this.groupStartParticipants.some(participant => participant.agentKind === 'Human')
        this.groupStartPromptCaretStart = 0
        this.groupStartPromptCaretEnd = 0
        this.visible[visibleKey] = true
        await this.loadGroupStartParticipants(this.groupStartForm.chatGroupId)
        this.$nextTick(() => this.captureEditorFormSnapshot(visibleKey))
        return
      }
      if (btnType === 'drawerTaskStart') {
        visibleKey = 'drawerGroupStart'
        const chatGroupId = this.getGroupStartChatGroupId(this.groupStartForm)
        if (!this.groupStartForm.groupName && chatGroupId) {
          const groupDetail = this.groupDetails?.chatGroupDto
            || this.agentDetailsGroupDetails?.chatGroupDto
            || {}
          this.groupStartForm.groupName = groupDetail.name || ''
        }
      }
      let initialSnapshotLoader = null
      if (btnType === 'drawerGroup') {
        // 成员选择控件会在列表到达后回填；待回填完成再建立快照，避免误判为用户修改。
        initialSnapshotLoader = Promise.all([
          this.getAgentListData('groupAgent'),
          this.getRemoteAgentListData('groupRemoteAgent')
        ]).catch(() => { })
      }
      this.visible[visibleKey] = true
      if (initialSnapshotLoader) {
        await initialSnapshotLoader
      }
      this.$nextTick(() => this.captureEditorFormSnapshot(visibleKey))
    },
    // Dailog|抽屉 关闭 按钮
    handleElVisibleClose(btnType) {
      if (btnType === 'dialogAgentParameter') {
        // 清空数据
        this.agentParameterList = []
        this.$nextTick(() => {
          this.visible[btnType] = false
        })
        return
      } else if (btnType === 'dialogTaskDescription') {
        // 清空数据
        this.describeContent = ''
        this.taskDescriptionDetails = null
        this.$nextTick(() => {
          this.visible[btnType] = false
        })
        return
      }
      if (!this.getEditorFormName(btnType)) return

      // 没有任何表单修改时直接关闭；仅在可能丢弃用户输入时询问。
      if (!this.isEditorFormDirty(btnType)) {
        this.closeEditorForm(btnType)
        return
      }

      this.$confirm('当前修改尚未保存，确认关闭？')
        .then(_ => this.closeEditorForm(btnType))
        .catch(_ => { });
    },
    // Dailog|抽屉 提交 按钮
    handleElVisibleSubmit(btnType) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      let refName = '', formName = ''
      // 智能体 
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        refName = 'agentELForm'
        formName = 'agentForm'
      }
      // 组
      if (btnType === 'drawerGroup') {
        refName = 'groupELForm'
        formName = 'groupForm'
      }
      // 组 启动
      if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) {
        refName = 'groupStartELForm'
        formName = 'groupStartForm'
      }
      // 任务评价
      if (btnType === 'dialogTaskEvaluation') {
        refName = 'evaluationELForm'
        formName = 'evaluationForm'
      }
      if (!refName) return
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)
        && !this.validateAgentModelBindingForm()) {
        return
      }
      this.$refs[refName].validate(async (valid) => {
        if (valid) {
          const submitForm = this[formName] ?? {}
          //提交数据给后端
          await this.saveSubmitFormData(btnType, submitForm)
          // “再次执行/启动任务”的跳转交给 saveSubmitFormData 中刷新后的定位逻辑，
          // 避免这里用旧缓存任务二次覆盖到历史任务。
          // this.visible[btnType] = false
        } else {
          console.log('error submit!!');
          return false;
        }
      });
    },
    // 表单 单条校验
    handleFormValidateField(refFormEL, formName, propName, item) {
      // this[formName][propName] = item
      this.$set(this[formName], `${propName}`, item)
      this.$refs[refFormEL]?.validateField(propName, () => { })
    },

    // 识别事件
    handleIdentify(e) {

      //debugger
      let bRes = this.findDest(e)
      if (bRes) {
        console.log('命中')
        //自动选出PromptRange（不做处理）

      } else {
        console.log('未命中')
        //TODO:默认成为新的提示词，zai

      }
      console.log('识别事件', e);
    },

    handleSystemMessageTypeChange(type) {
      // 用户显式切换时，应以用户的选择为准，不再让异步加载结果覆盖它。
      this.agentSystemMessageTypeDetectionPending = false
      if (String(type) === '2') {
        this.$set(this.agentForm, 'modelBinding', 2)
      }
    },

    handleAgentModelBindingChange(value) {
      if (Number(value) !== 2) {
        this.$set(this.agentForm, 'aiModelId', null)
      }
    },

    validateAgentModelBindingForm() {
      const isManualPrompt = String(this.agentForm?.systemMessageType || '') === '2'
      const binding = Number(this.agentForm?.modelBinding ?? 0)
      const aiModelId = Number(this.agentForm?.aiModelId || 0)
      if (isManualPrompt && binding !== 2) {
        this.$message.error('手动 Prompt 没有 PromptRange 模型可继承，请选择“手动选择 AIModel”。')
        return false
      }
      if (binding === 2 && !aiModelId) {
        this.$message.error('手动选择 AIModel 时必须选择一个 Chat 类型模型。')
        return false
      }
      return true
    },

    handleSystemMessageOptionsLoaded(options) {
      if (!this.agentSystemMessageTypeDetectionPending) {
        return
      }

      const systemMessage = typeof this.agentForm?.systemMessage === 'string'
        ? this.agentForm.systemMessage.trim()
        : String(this.agentForm?.systemMessage ?? '').trim()
      const selectedFromPromptRange = (options || []).some(option =>
        String(option?.value ?? '').trim() === systemMessage)

      this.agentSystemMessageTypeDetectionPending = false
      this.$set(this.agentForm, 'systemMessageType', selectedFromPromptRange ? '1' : '2')
    },

    // 切换 tabs 页面
    handleTabsClick(tab, event) {
      if (!this.isApplyingHashRoute) {
        this.navigateByHash(this.buildCurrentRoute({ tab: this.tabsActiveName }))
        return
      }
      this.clearHistoryTimer()
      this.stopAgentGraphPolling()
      // 智能体
      if (this.tabsActiveName === 'first') {
        this.getAgentListData('agent')
        if (this.agentListViewMode === 'three' && this.scrollbarAgentIndex === '') {
          this.$nextTick(() => {
            this.ensureAgentGraph3d()
            this.refreshAgentGraphSnapshot(true)
            this.startAgentGraphPolling()
          })
        }
      }
      // 组
      if (this.tabsActiveName === 'second') {
        this.getGroupListData('group')
      }
      // 任务
      if (this.tabsActiveName === 'third') {
        this.gettaskListData('task')
      }
      this.syncHashRoute()
    },

    // 筛选输入变化
    handleFilterChange(value, filterType) {
      console.log('handleFilterChange', filterType, value)
      if (filterType === 'agent') {

        this.agentQueryList.filter = value
        this.getAgentListData('agent', 1)
        if (this.agentListViewMode === 'three' && this.tabsActiveName === 'first' && this.scrollbarAgentIndex === '') {
          this.refreshAgentGraphSnapshot(true)
        }
      }
      if (filterType === 'groupAgent') {
        this.groupAgentQueryList.filter = value
        this.getAgentListData('groupAgent', 1)
      }
      if (filterType === 'group') {
        this.groupQueryList.filter = value
        this.getGroupListData('group', 1)
      }
      if (filterType === 'agentGroup') {
        this.agentDetailsGroupQueryList.filter = value
        this.getGroupListData('agentGroup', 1)
      }
      if (filterType === 'agentTask') {
        this.agentDetailsTaskQueryList.filter = value
        this.gettaskListData('agentTask', 1)
      }
      if (filterType === 'task') {
        this.taskQueryList.filter = value
        this.gettaskListData('task', 1)
      }
    },
    // 筛选条件事件 agent  group task
    handleFilterCriteria(filterType, fieldType) {
      // 智能体
      if (filterType === 'agent') {
        if (fieldType === 'timeSort') {
          this.agentQueryList.timeSort = !this.agentQueryList.timeSort
        } else {
          this.agentFilterCriteria.forEach(item => {
            if (item.value === fieldType) {
              item.checked = true
            } else {
              item.checked = false
            }
            if (fieldType === 'all') {
              this.agentQueryList[item.value] = true
            } else {
              this.agentQueryList[item.value] = item.checked
            }
          })
        }
        // this.agentFCPVisible = !this.agentFCPVisible
        // to do 调用接口
      }
      // 组
      if (filterType === 'group') {
        if (fieldType === 'timeSort') {
          this.groupQueryList.timeSort = !this.groupQueryList.timeSort
        } else {
          this.groupFilterCriteria.forEach(item => {
            if (item.value === fieldType) {
              item.checked = true
            } else {
              item.checked = false
            }
            if (fieldType === 'all') {
              this.groupQueryList[item.value] = true
            } else {
              this.groupQueryList[item.value] = item.checked
            }
          })
        }
        // this.groupFCPVisible = !this.groupFCPVisible
        // to do 调用接口
      }
      // 任务
      if (filterType === 'task') {
        if (fieldType === 'timeSort') {
          this.taskQueryList.timeSort = !this.taskQueryList.timeSort
        } else {
          this.taskFilterCriteria.forEach(item => {
            if (item.value === fieldType) {
              item.checked = true
            } else {
              item.checked = false
            }
            if (fieldType === 'all') {
              this.taskQueryList[item.value] = true
            } else {
              this.taskQueryList[item.value] = item.checked
            }
          })
        }
        // this.taskFCPVisible = !this.taskFCPVisible
        // to do 调用接口
      }
    },



    // 查看全部智能体 列表 
    handleAgentViewAll(fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash(this.buildCurrentRoute({ tab: 'first', agentId: null }))
        return
      }
      this.clearHistoryTimer()
      this.scrollbarAgentIndex = '' // 清空索引
      this.agentDetails = '' // 清空详情数据
      this.getAgentListData('agent')
      if (this.agentListViewMode === 'three') {
        this.$nextTick(() => {
          this.ensureAgentGraph3d()
          this.refreshAgentGraphSnapshot(true)
          this.startAgentGraphPolling()
        })
      }
      this.syncHashRoute({ tab: 'first', agentId: null })
    },
    // 查看 智能体
    handleAgentView(item, index, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash(this.buildCurrentRoute({ tab: 'first', agentId: item.id }))
        return
      }
      this.clearHistoryTimer()
      this.stopAgentGraphPolling()
      this.scrollbarAgentIndex = item.id ?? ''
      // 重置 数据
      this.resetAgentDetailsQuery()
      // 获取智能体 详情
      this.getAgentDetailData(item.id, item)
      // 重置 组获取智能体query
      if (this.agentDetailsTabsActiveName === 'first') {
        this.getGroupListData('agentGroup', item.id)
      }
      if (this.agentDetailsTabsActiveName === 'second') {
        this.gettaskListData('agentTask', item.id)
      }
      this.syncHashRoute({ tab: 'first', agentId: item.id })
    },
    // 重置 智能体详情下的组和任务数据
    resetAgentDetailsQuery() {
      this.agentDetailsTabsActiveName = 'first'
      this.agentDetailsGroupList = []
      this.agentDetailsGroupShowType = '1'
      this.agentDetailsGroupIndex = 0
      this.agentDetailsGroupDetails = ''
      this.agentGroupTaskSelection = []
      this.agentDetailsGroupTaskList = []
      this.agentDetailsGroupTaskHistoryList = []
      this.agentGroupTaskMemberfilter = ''
      this.agentGroupTaskMemberfilterList = []
      this.agentDetailsGroupDetailsTaskDetails = ''
      this.agentDetailsTaskIndex = 0
      this.agentDetailsTaskList = []
      this.agentDetailsTaskDetails = ''
      this.agentDetailsTaskHistoryList = []
      this.agentDetailsTaskMemberList = []
      this.agentTaskMemberfilter = ''
      this.agentTaskMemberfilterList = []
      this.$set(this, 'agentDetailsGroupTaskQueryList', this.$options.data().agentDetailsGroupTaskQueryList)
      this.$set(this, 'agentDetailsGroupQueryList', this.$options.data().agentDetailsGroupQueryList)
      this.$set(this, 'agentDetailsTaskQueryList', this.$options.data().agentDetailsTaskQueryList)
    },
    // 切换 智能体详情 tabs 页面
    handleAgentDetailsTabsClick(tab, event) {
      this.clearHistoryTimer()
      let id = ''
      if (this.agentDetails) {
        id = this.agentDetails.agentTemplateDto ? this.agentDetails.agentTemplateDto.id : this.agentDetails.id
      }
      if (this.agentDetailsTabsActiveName === 'first') {
        this.getGroupListData('agentGroup', id)
      }
      if (this.agentDetailsTabsActiveName === 'second') {
        this.gettaskListData('agentTask', id)
      }
    },
    // 智能体 状态 切换
    setAgentState(stateType, item) {
      if (!stateType || !item) return
      let messageText = ''
      let serviceURL = ''
      if (stateType === 'stop') {
        let itemData = item.agentTemplateDto || item
        serviceURL = `/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.Enable?id=${itemData.id}&enable=${!itemData.enable}`

        if (itemData.enable) {
          messageText = `<div>是否确认将${itemData.name}智能体停用？</div><div>此智能体将不会参与后续新任务</div>`
        } else {
          messageText = `<div>是否确认将${itemData.name}智能体启用？</div>`
        }
      }
      if (stateType === 'delete') {
        messageText = `<div>是否确认从${item.name}组退出？</div><div>移除出后将无法看到之前参与的任务记录</div>`
      }
      if (!serviceURL) return
      this.$confirm(messageText, '操作确认', {
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
            });
            if (stateType === 'stop') {
              // const agentMapStr = {
              //     'drawerAgent': 'agent',
              //     'dialogGroupAgent': 'groupAgent'
              // }
              this.getAgentListData('agent')
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })

      }).catch(() => {
        this.$message({
          type: 'info',
          message: '已取消操作'
        });
      });
    },
    // 组启用 / 停用
    async setGroupState(item) {
      const group = item?.chatGroupDto || item
      if (!group || !group.id) return

      const willEnable = !group.enable
      const actionText = willEnable ? '启用' : '停用'
      const messageText = willEnable
        ? `<div>是否确认启用组“${group.name}”？</div><div>启用后可再次创建新任务，并可作为 Workflow 节点使用。</div>`
        : `<div>是否确认停用组“${group.name}”？</div><div>停用后不会创建新任务或作为 Workflow 节点使用；已经启动的任务不会被中断。</div>`

      try {
        await this.$confirm(messageText, '操作确认', {
          dangerouslyUseHTMLString: true,
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning'
        })

        const serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.Enable?id=${group.id}&enable=${willEnable}`
        const res = await serviceAM.post(serviceURL)
        if (!res?.data?.success) {
          throw new Error(res?.data?.errorMessage || res?.data?.data || '操作失败')
        }

        this.$message({ type: 'success', message: `组已${actionText}` })
        if (this.tabsActiveName === 'second') {
          await this.getGroupListData('group')
        } else if (this.agentDetails?.agentTemplateDto?.id) {
          await this.getGroupListData('agentGroup', this.agentDetails.agentTemplateDto.id)
        }
        await this.refreshAgentGraphSnapshot(this.agentListViewMode === 'three')
      } catch (error) {
        if (error === 'cancel' || error === 'close') {
          this.$message({ type: 'info', message: '已取消操作' })
          return
        }
        this.$message({
          message: error?.message || '操作失败',
          type: 'error',
          duration: 5 * 1000
        })
      }
    },

    handleAgentDelete(item, { closeEditor = false } = {}) {
      const itemData = item?.agentTemplateDto || item
      if (!itemData || !itemData.id) return

      const groupQuery = {
        agentTemplateId: 0,
        pageIndex: 0,
        pageSize: 0,
        filter: ''
      }
      const memberGroupQuery = {
        agentTemplateId: itemData.id,
        pageIndex: 0,
        pageSize: 0,
        filter: ''
      }

      const serviceURL = `/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.Delete?id=${itemData.id}`

      Promise.all([
        serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupList?${getInterfaceQueryStr(groupQuery)}`, groupQuery),
        serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupList?${getInterfaceQueryStr(memberGroupQuery)}`, memberGroupQuery)
      ]).then(([allGroupRes, memberGroupRes]) => {
        const allGroupList = allGroupRes?.data?.data?.chatGroupDtoList ?? []
        const memberGroupList = memberGroupRes?.data?.data?.chatGroupDtoList ?? []

        const adminGroups = allGroupList.filter(group => group.adminAgentTemplateId === itemData.id).map(group => group.name)
        const enterGroups = allGroupList.filter(group => group.enterAgentTemplateId === itemData.id).map(group => group.name)

        if (adminGroups.length || enterGroups.length) {
          const blockedMessage = [
            `<div>智能体「${itemData.name}」当前不可删除。</div>`,
            adminGroups.length ? `<div style="margin-top:6px;">作为群主的组：${adminGroups.join('、')}</div>` : '',
            enterGroups.length ? `<div style="margin-top:6px;">作为对接人的组：${enterGroups.join('、')}</div>` : '',
            '<div style="margin-top:8px;color:#E6A23C;">请先在对应组中替换群主/对接人后再删除。</div>'
          ].join('')

          this.$alert(blockedMessage, '删除受阻', {
            dangerouslyUseHTMLString: true,
            confirmButtonText: '我知道了'
          })
          return
        }

        const memberGroups = memberGroupList.map(group => group.name)
        const previewMessage = [
          `<div>确认删除智能体「${itemData.name}」吗？</div>`,
          memberGroups.length
            ? `<div style="margin-top:6px;">将移出成员组：${memberGroups.join('、')}</div>`
            : '<div style="margin-top:6px;">该智能体当前不在任何组成员中。</div>',
          '<div style="margin-top:8px;">同时会删除与该智能体相关的历史消息记录，且不可恢复。</div>'
        ].join('')

        this.$confirm(previewMessage, '删除智能体确认', {
          dangerouslyUseHTMLString: true,
          confirmButtonText: '删除智能体',
          cancelButtonText: '取消',
          type: 'warning'
        }).then(() => {
          serviceAM.post(serviceURL).then(res => {
            if (res.data.success) {
              this.$message({
                type: 'success',
                message: '删除成功!'
              })
              if (closeEditor) {
                this.visible.drawerAgent = false
              }
              this.handleAgentViewAll()
            } else {
              app.$message({
                message: res.data.errorMessage || res.data.data || 'Error',
                type: 'error',
                duration: 5 * 1000
              })
            }
          })
        }).catch(() => {
          this.$message({
            type: 'info',
            message: '已取消操作'
          })
        })
      }).catch(() => {
        app.$message({
          message: '删除预检查失败，请稍后重试',
          type: 'error',
          duration: 5 * 1000
        })
      })
    },



    // 侧边 组tree 组件 节点 筛选
    filterGroupTreeNode(value, data) {
      if (!value) return true;
      return data.name.indexOf(value) !== -1;
    },
    // 侧边 组tree 组件 节点 点击
    handleGroupTreeNodeClick(node, data, clickType) {
      // console.log('handleGroupTreeNodeClick', node, data)
      if (clickType === 'agentGroup') {
        this.agentDetailsGroupShowType = node.level.toString()
        if (this.agentDetailsGroupShowType === '1') {
          this.agentDetailsGroupDetails = deepClone(data)
        }
        if (this.agentDetailsGroupShowType === '2') {
          this.agentDetailsGroupDetails = deepClone(data)
        }
      }
      if (clickType === 'group') {
        this.groupShowType = node.level.toString()
        if (this.groupShowType === '1') {
          this.groupDetails = '' // 清空组详情
        }
        if (this.groupShowType === '2') {
          this.groupDetails = deepClone(data)
        }
        if (this.groupShowType === '3') {
          this.groupDetails = deepClone(data)
        }
      }
    },
    // 组 查看详情
    handleGroupDetail(row, clickType) {
      // console.log('handleGroupDetail', row)
      if (clickType === 'agentGroup') {
        this.agentDetailsGroupShowType = '1'
        this.agentDetailsGroupDetails = deepClone(row)
      }
      if (clickType === 'group') {
        this.groupShowType = '2'
        this.groupDetails = deepClone(row)
      }
    },
    // 组 查看全部 列表 
    handleGroupViewAll(fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash({ tab: 'second' })
        return
      }
      this.clearHistoryTimer()
      this.groupShowType = '1'
      // 清空组详情
      this.scrollbarGroupIndex = '' // 清空索引
      this.groupDetails = ''
      this.groupTaskSelection = []
      this.groupTaskList = []
      this.groupTaskDetails = ''
      this.groupTaskHistoryList = []
      this.groupTaskMemberList = []
      this.groupSelection = []
      this.groupTaskMemberfilter = ''
      this.groupTaskMemberfilterList = []
      this.getGroupListData('group')
      this.syncHashRoute({ tab: 'second', groupId: null, taskId: null })
    },
    // 组 查看列表 详情 
    handleGroupView(clickType, item, index = 0, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute && (clickType === 'group' || clickType === 'groupTable')) {
        this.navigateByHash({ tab: 'second', groupId: item.id })
        return
      }
      this.clearHistoryTimer()
      // 智能体下时 查看组详情
      if (clickType === 'agentGroup') {
        // 切换展示类型
        this.agentDetailsGroupShowType = '1'
        this.agentDetailsGroupIndex = index ?? 0
        // 清空组详情
        this.agentDetailsGroupDetails = ''
        this.agentGroupTaskSelection = []
        this.agentDetailsGroupTaskList = []
        this.agentDetailsGroupDetailsTaskDetails = ''
        this.agentDetailsGroupTaskHistoryList = []
        this.agentDetailsGroupTaskMemberList = []
        this.agentGroupTaskMemberfilter = ''
        this.agentGroupTaskMemberfilterList = []
        this.getGroupDetailData(clickType, item.id, item)
      }
      // 组大类时 查看组详情
      if (clickType === 'group' || clickType === 'groupTable') {
        // 切换展示类型
        this.groupShowType = '2'
        // if (clickType === 'groupTable') {
        //     const { pageIndex, pageSize } = this.groupQueryList
        //     this.scrollbarGroupIndex = pageIndex > 1 ? pageIndex * pageSize + index : index
        // } else {
        //     this.scrollbarGroupIndex = index ?? 0
        // }
        this.scrollbarGroupIndex = item.id ?? ''
        // 清空组详情
        this.groupDetails = ''
        this.groupTaskSelection = []
        this.groupTaskList = []
        this.groupTaskDetails = ''
        this.groupTaskHistoryList = []
        this.groupTaskMemberList = []
        this.groupTaskMemberfilter = ''
        this.groupTaskMemberfilterList = []
        this.getGroupDetailData(clickType, item.id, item)
        this.syncHashRoute({ tab: 'second', groupId: item.id, taskId: null })
      }
    },
    // 组 新增|编辑 智能体table 切换table 选中
    toggleSelection(rows) {
      if (rows) {
        rows.forEach(row => {
          this.$refs?.groupAgentTable?.toggleRowSelection(row);
        });
      } else {
        this.$refs?.groupAgentTable?.clearSelection();
      }
    },
    // 组 新增|编辑 智能体table 选中变化
    handleSelectionChange(val) {
      if (!this.isGetGroupAgent) {
        const selectedIds = new Set(val.map((i) => i.id))
        const spliceList = this.groupAgentList.filter(
          (item) => !selectedIds.has(item.id)
        )
        const pushList = this.groupAgentList.filter((item) =>
          selectedIds.has(item.id)
        )
        pushList.forEach((item) => {
          const index = this.groupForm.members.findIndex(
            (i) => i.id === item.id
          )
          if (index === -1) {
            this.groupForm.members.push(item)
          } else {
            this.groupForm.members.splice(index, 1, item)
          }
        })

        spliceList.forEach((item) => {
          const index = this.groupForm.members.findIndex(
            (i) => i.id === item.id
          )
          if (index !== -1) {
            this.groupForm.members.splice(index, 1)
          }
        })
      }

    },
    // 组 新增|编辑 智能体 成员取消选中
    groupMembersCancel(item, index) {
      this.groupForm.members.splice(index, 1);
      const findIndex = this.groupAgentList.findIndex(i => item.id === i.id)
      if (findIndex !== -1) {
        this.toggleSelection([this.groupAgentList[findIndex]])
      }
    },
    // 组列表选中变化 (批量删除)
    handleGroupSelectionChange(val) {
      this.groupSelection = val
    },
    // 组 删除
    handleGroupDelete(optype, row) {
      console.log('handleGroupDelete:', row);
      if (!row || !row.id) return
      let serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.Delete?id=${row.id}`
      if (!serviceURL) return
      // 操作确认 提示
      this.$confirm('确认删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
            });
            if (optype === 'groupTable') {
              // 重新获取数据
              this.getGroupListData('group')
            } else {
              // 查看全部组
              this.handleGroupViewAll()
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: '已取消操作'
        });
      });
    },
    // 组批量删除
    handleGroupDeleteBatch() {
      console.log('handleGroupDeleteBatch:', this.groupSelection);
      const selectedIds = (this.groupSelection || []).map(item => item.id).filter(Boolean)
      if (!selectedIds.length) {
        this.$message.warning('请先选择要删除的组')
        return
      }
      let serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.DeleteBatch'
      if (!serviceURL) return
      // 操作确认 提示
      this.$confirm('确认批量删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
            });
            // 重新获取数据
            this.getGroupListData('group')
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: '已取消操作'
        });
      });
    },


    // 任务 查看全部 列表 
    handleTaskViewAll(fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.navigateByHash({ tab: 'third' })
        return
      }
      this.clearHistoryTimer()
      this.scrollbarTaskIndex = ''
      // 清空详情数据
      this.taskDetails = ''
      this.taskSelection = []
      this.taskHistoryList = []
      this.taskMemberList = []
      this.taskMemberfilter = ''
      this.taskMemberfilterList = []
      this.gettaskListData('task')
      this.syncHashRoute({ tab: 'third', taskId: null })
    },
    // 查看 任务详情
    handleTaskView(clickType, item = {}, index = 0, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute && clickType === 'groupTask') {
        this.navigateByHash({ tab: 'second', groupId: item.chatGroupId || this.scrollbarGroupIndex || null, taskId: item.id })
        return
      }
      if (!fromHash && !this.isApplyingHashRoute && clickType === 'task') {
        this.navigateByHash({ tab: 'third', taskId: item.id })
        return
      }
      this.clearHistoryTimer()
      if (clickType === 'agentTask') {
        this.agentDetailsTaskIndex = index ?? ''
        // 清空详情数据
        this.agentDetailsTaskDetails = ''
        this.agentDetailsTaskHistoryList = []
        this.agentDetailsTaskMemberList = []
        this.agentTaskMemberfilter = ''
        this.agentTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
      }
      if (clickType === 'agentGroupTask') {
        this.agentDetailsGroupShowType = '2'
        // 清空详情数据
        this.agentDetailsGroupDetailsTaskDetails = ''
        this.agentDetailsGroupTaskHistoryList = []
        this.agentDetailsGroupTaskMemberList = []
        this.agentGroupTaskMemberfilter = ''
        this.agentGroupTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
      }
      if (clickType === 'groupTask') {
        this.groupShowType = '3'
        // 清空详情数据
        this.groupTaskDetails = ''
        this.groupTaskHistoryList = []
        this.groupTaskMemberList = []
        this.groupTaskMemberfilter = ''
        this.groupTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
        this.syncHashRoute({ tab: 'second', groupId: item.chatGroupId || this.scrollbarGroupIndex || null, taskId: item.id })
      }
      if (clickType === 'task') {
        this.scrollbarTaskIndex = index ?? ''
        // 清空详情数据
        this.taskDetails = ''
        this.taskHistoryList = []
        this.taskMemberList = []
        this.taskMemberfilter = ''
        this.taskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
        this.syncHashRoute({ tab: 'third', taskId: item.id })
      }
    },
    // 返回组详情页面
    returnGroup(clickType, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute && clickType === 'groupTask') {
        const groupId = this.groupDetails?.chatGroupDto?.id || this.scrollbarGroupIndex || null
        this.navigateByHash({ tab: 'second', groupId: groupId })
        return
      }
      this.clearHistoryTimer()
      if (clickType === 'agentGroupTask') {
        this.agentDetailsGroupShowType = '1'
        // const item = this.agentDetailsGroupList[this.agentDetailsGroupIndex]
        // this.getGroupDetailData('agentGroup', item.id,this.agentDetailsGroupDetails)

      }
      if (clickType === 'groupTask') {
        this.groupShowType = '2' // 组件详情
        this.syncHashRoute({ tab: 'second', taskId: null })
        // const item = this.groupList[this.scrollbarGroupIndex]
        // this.getGroupDetailData('groupTable', item.id,this.groupDetails)
      }
    },
    // 智能体-组 任务列表table选中变化 (批量启动和删除)
    handleAgentGroupTaskSelectionChange(val) {
      this.agentGroupTaskSelection = val
    },

    // 组 任务列表table选中变化 (批量启动和删除)
    handleGroupTaskSelectionChange(val) {
      this.groupTaskSelection = val
    },
    // 任务列表选择 
    handleTaskSelectionChange(val) {
      this.taskSelection = val
    },
    // 查看智能体参数 列表
    async viewAgentParameters(optype, item) {
      let baseList = []
      if (optype === 'task') {
        // 从任务行数据获取所在组成员列表
        if (item && item.chatGroupId) {
          await this.getTaskMemberListData('task', item.chatGroupId)
        }
        baseList = this.taskMemberList ?? []
      } else if (optype === 'taskDetail') {
        baseList = this.taskMemberList ?? []
      } else if (optype === 'agentTask') {
        baseList = this.agentDetailsTaskMemberList ?? []
      } else if (optype === 'agentGroupTaskAdmin') {
        const agentGroupId = this.agentDetailsGroupDetailsTaskDetails?.chatGroupId
          || this.agentDetailsGroupDetails?.chatGroupDto?.id
        if (!this.agentDetailsGroupTaskMemberList.length && agentGroupId) {
          await this.getTaskMemberListData('agentGroupTask', agentGroupId)
        }
        let agentDtoList = this.agentDetailsGroupTaskMemberList.length
          ? this.agentDetailsGroupTaskMemberList
          : (this.agentDetailsGroupDetails?.agentTemplateDtoList ?? [])
        let adminAgentId = this.agentDetailsGroupDetails?.chatGroupDto?.adminAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => String(a.id) === String(adminAgentId))
        baseList = findItem ? [findItem] : []
      } else if (optype === 'agentGroupTaskEnter') {
        const agentGroupId = this.agentDetailsGroupDetailsTaskDetails?.chatGroupId
          || this.agentDetailsGroupDetails?.chatGroupDto?.id
        if (!this.agentDetailsGroupTaskMemberList.length && agentGroupId) {
          await this.getTaskMemberListData('agentGroupTask', agentGroupId)
        }
        let agentDtoList = this.agentDetailsGroupTaskMemberList.length
          ? this.agentDetailsGroupTaskMemberList
          : (this.agentDetailsGroupDetails?.agentTemplateDtoList ?? [])
        let enterAgentId = this.agentDetailsGroupDetails?.chatGroupDto?.enterAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => String(a.id) === String(enterAgentId))
        baseList = findItem ? [findItem] : []
      } else if (optype === 'agentGroupTask') {
        const agentGroupId = this.agentDetailsGroupDetailsTaskDetails?.chatGroupId
          || this.agentDetailsGroupDetails?.chatGroupDto?.id
        if (!this.agentDetailsGroupTaskMemberList.length && agentGroupId) {
          await this.getTaskMemberListData('agentGroupTask', agentGroupId)
        }
        baseList = this.agentDetailsGroupTaskMemberList.length
          ? this.agentDetailsGroupTaskMemberList
          : (this.agentDetailsGroupDetails?.agentTemplateDtoList ?? [])
      } else if (optype === 'groupTaskAdmin') {
        const groupTaskChatGroupId = this.groupTaskDetails?.chatGroupId
          || this.groupDetails?.chatGroupDto?.id
        if (!this.groupTaskMemberList.length && groupTaskChatGroupId) {
          await this.getTaskMemberListData('groupTask', groupTaskChatGroupId)
        }
        let agentDtoList = this.groupTaskMemberList.length
          ? this.groupTaskMemberList
          : (this.groupDetails?.agentTemplateDtoList ?? [])
        let adminAgentId = this.groupDetails?.chatGroupDto?.adminAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => String(a.id) === String(adminAgentId))
        baseList = findItem ? [findItem] : []
      } else if (optype === 'groupTaskEnter') {
        const groupTaskChatGroupId = this.groupTaskDetails?.chatGroupId
          || this.groupDetails?.chatGroupDto?.id
        if (!this.groupTaskMemberList.length && groupTaskChatGroupId) {
          await this.getTaskMemberListData('groupTask', groupTaskChatGroupId)
        }
        let agentDtoList = this.groupTaskMemberList.length
          ? this.groupTaskMemberList
          : (this.groupDetails?.agentTemplateDtoList ?? [])
        let enterAgentId = this.groupDetails?.chatGroupDto?.enterAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => String(a.id) === String(enterAgentId))
        baseList = findItem ? [findItem] : []
      } else if (optype === 'groupTask') {
        const groupTaskChatGroupId = this.groupTaskDetails?.chatGroupId
          || this.groupDetails?.chatGroupDto?.id
        if (!this.groupTaskMemberList.length && groupTaskChatGroupId) {
          await this.getTaskMemberListData('groupTask', groupTaskChatGroupId)
        }
        baseList = this.groupTaskMemberList.length
          ? this.groupTaskMemberList
          : (this.groupDetails?.agentTemplateDtoList ?? [])
      }
      // 填充状态与历史输出后再打开弹窗
      this.agentParameterList = await this.buildAgentParameterList(baseList)
      // 先清空再开弹窗，确保 el-tabs 在 pane 渲染完成后按正确类型激活第一个 tab
      this.agentParameterTabsValue = ''
      this.visible.dialogAgentParameter = true
      this.$nextTick(() => {
        this.agentParameterTabsValue = '0'
      })
    },
    // 从参数弹窗直接复用现有 Agent 编辑表单，保留当前对话上下文
    async openAgentParameterEditor(item) {
      if (!item || item.agentKind === 'RemoteA2A' || !item.id) {
        return
      }
      await this.handleEditDrawerOpenBtn('drawerAgent', item)
    },
    // Agent 编辑保存后同步当前参数弹窗，避免关闭抽屉后仍显示旧名称、描述或参数
    async refreshAgentParameterItem(savedAgent) {
      if (!this.visible.dialogAgentParameter || !savedAgent?.id) {
        return
      }

      const index = this.agentParameterList.findIndex(item => item.id === savedAgent.id)
      if (index < 0) {
        return
      }

      const current = Object.assign({}, this.agentParameterList[index], savedAgent)
      this.$set(this.agentParameterList, index, current)
      try {
        const refreshed = await this.buildAgentParameterList([current])
        if (refreshed[0]) {
          this.$set(this.agentParameterList, index, refreshed[0])
        }
      } catch (e) {
        // 基础信息已经同步；状态接口失败时保留原有参数展示
        console.warn('refreshAgentParameterItem: refresh status failed for agent', savedAgent.id, e)
      }
    },
    // 构建智能体参数列表：为基础 DTO 列表补充 promptItemDto / aiModelDto / promptRangeDto 及历史输出
    async buildAgentParameterList(baseList) {
      const result = []
      for (const agent of baseList) {
        const enriched = Object.assign({}, agent, { outputList: [] })
        if (agent.agentKind === 'RemoteA2A') {
          result.push(enriched)
          continue
        }
        // 获取智能体运行状态（含 promptItemDto / aiModelDto / promptRangeDto）
        // 使用 serviceAM 并设置 customAlert，由拦截器静默处理错误
        try {
          const res = await serviceAM.get(
            `/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetItemStatus?id=${agent.id}`,
            { customAlert: true }
          )
          const data = res?.data ?? {}
          if (data.success) {
            const status = data?.data?.agentTemplateStatus ?? null
            if (status) {
              enriched.promptItemDto = status.promptItemDto || null
              enriched.promptRangeDto = status.promptRangeDto || null
              enriched.aiModelDto = status.aiModelDto || null
            }
          }
        } catch (e) {
          console.warn('buildAgentParameterList: GetItemStatus failed for agent', agent.id, e)
        }
        // 获取历史输出列表（PromptRange 结果）
        if (enriched.promptItemDto && enriched.promptItemDto.id) {
          try {
            const res = await serviceAM.get(
              `/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetByItemId?promptItemId=${enriched.promptItemDto.id}`,
              { customAlert: true }
            )
            const data = res?.data ?? {}
            if (data.success) {
              const promptResults = data?.data?.promptResults ?? []
              enriched.outputList = promptResults.map(oitem => {
                oitem.addTime = oitem.addTime ? formatDate(oitem.addTime) : ''
                oitem.resultStringHtml = this.renderSafeMarkdown(oitem.resultString || '')
                return oitem
              })
            }
          } catch (e) {
            console.warn('buildAgentParameterList: GetByItemId failed for promptItem', enriched.promptItemDto.id, e)
          }
        }
        result.push(enriched)
      }
      return result
    },
    // 再次执行 (即再次启动)
    handleTaskAgain(optype, item = {}) {
      let startData = item ?? {}
      const chatGroupId = this.getGroupStartChatGroupId(startData)
      if (chatGroupId) {
        startData = Object.assign({}, startData, { chatGroupId })
      }
      // this.groupStartForm.groupName = item.name
      this.handleEditDrawerOpenBtn('drawerTaskStart', startData)
    },
    // 任务删除
    handleTaskDelet(optype, row) {
      console.log('handleTaskDelet:', row);
      if (!row || !row.id) return
      let serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.Delete?id=${row.id}`
      if (!serviceURL) return
      // 操作确认 提示
      this.$confirm('确认删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
            });
            let groupDetail = {}
            if (optype === 'agentGroupTask') {
              groupDetail = this.agentDetailsGroupDetails?.chatGroupDto ?? {}
            } else if (optype === 'groupTask') {
              groupDetail = this.groupDetails?.chatGroupDto ?? {}
            } else {
              this.gettaskListData(optype)
            }
            if (groupDetail.id) {
              // 获取任务列表
              this.gettaskListData(optype, groupDetail.id)
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: '已取消操作'
        });
      });
    },
    // 组-任务批量启动(任务) agentGroupTaskBatch groupTaskBatch
    handleTaskStartBatch(opType, item) {
      let selectedRows = []
      let refreshListType = 'task'
      let refreshGroupId = 0

      if (opType === 'agentGroupTaskBatch') {
        selectedRows = this.agentGroupTaskSelection
        refreshListType = 'agentGroupTask'
        refreshGroupId = Number(this.agentDetailsGroupDetails?.chatGroupDto?.id || 0)
      } else if (opType === 'groupTaskBatch') {
        selectedRows = this.groupTaskSelection
        refreshListType = 'groupTask'
        refreshGroupId = Number(this.groupDetails?.chatGroupDto?.id || 0)
      } else if (opType === 'taskBatch') {
        selectedRows = this.taskSelection
      }

      const selectedIds = (selectedRows || []).map(task => task.id).filter(Boolean)
      if (!selectedIds.length) {
        this.$message.warning('请先选择要启动的任务')
        return
      }

      const serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.StartBatch'
      // 操作确认 提示
      this.$confirm('确认批量启动数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: res?.data?.data || '操作成功!'
            });
            if (refreshListType === 'task') {
              const refreshOptions = this.buildTaskRefreshOptions('task', {
                preferLatest: true
              }, 'drawerTaskStart')
              this.gettaskListData('task', '', 0, refreshOptions)
            } else if (refreshGroupId > 0) {
              const refreshOptions = this.buildTaskRefreshOptions(refreshListType, {
                preferLatest: true,
                focusChatGroupId: refreshGroupId
              }, 'drawerTaskStart')
              this.gettaskListData(refreshListType, refreshGroupId, 0, refreshOptions)
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: '已取消操作'
        });
      });
    },
    // 组-任务批量删除(任务) agentGroupTaskBatch groupTaskBatch
    handleTaskDeleteBatch(opType, item) {
      let selectedRows = []
      if (opType === 'agentGroupTaskBatch') {
        // item.chatGroupDto.id this.agentDetails.agentTemplateDto.id
        console.log('agentGroupTaskBatch:', this.agentGroupTaskSelection);
        selectedRows = this.agentGroupTaskSelection
      } else if (opType === 'groupTaskBatch') {
        // item.chatGroupDto.id
        console.log('groupTaskBatch:', this.groupTaskSelection);
        selectedRows = this.groupTaskSelection
      } else if (opType === 'taskBatch') {
        console.log('taskSelection:', this.taskSelection);
        selectedRows = this.taskSelection
      }
      const selectedIds = (selectedRows || []).map(task => task.id).filter(Boolean)
      if (!selectedIds.length) {
        this.$message.warning('请先选择要删除的任务')
        return
      }
      let serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.DeleteBatch'
      if (!serviceURL) return
      // 操作确认 提示
      this.$confirm('确认批量删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        // type: 'warning'
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
            });
            let groupDetail = {}, groupType = ''
            if (opType === 'agentGroupTaskBatch') {
              groupDetail = this.agentDetailsGroupDetails?.chatGroupDto ?? {}
              groupType = 'agentGroupTask' //'agentGroup'
            } else if (opType === 'groupTaskBatch') {
              groupDetail = this.groupDetails?.chatGroupDto ?? {}
              groupType = 'groupTask' //'group'
            }
            if (groupDetail.id) {
              // 获取任务列表
              this.gettaskListData(groupType, groupDetail.id)
              // this.getGroupDetailData(groupType, groupDetail.id, groupDetail)
            } else {
              this.gettaskListData('task')
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: '已取消操作'
        });
      });
    },

    handleTaskForceStop(optype, row) {
      if (!row || !row.id) return
      const serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.ForceStop?id=${row.id}`
      this.$confirm('确认强制停止该任务吗？', '操作确认', {
        dangerouslyUseHTMLString: true,
        confirmButtonText: '确定',
        cancelButtonText: '取消'
      }).then(() => {
        serviceAM.post(serviceURL).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
            })
            let groupDetail = {}
            if (optype === 'agentGroupTask') {
              groupDetail = this.agentDetailsGroupDetails?.chatGroupDto ?? {}
            } else if (optype === 'groupTask') {
              groupDetail = this.groupDetails?.chatGroupDto ?? {}
            } else {
              this.gettaskListData('task')
            }
            if (groupDetail.id) {
              this.gettaskListData(optype, groupDetail.id)
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: '已取消操作'
        })
      })
    },

    handleTaskForceStopBatch(opType, item) {
      let selectedRows = []
      if (opType === 'agentGroupTaskBatch') {
        selectedRows = this.agentGroupTaskSelection
      } else if (opType === 'groupTaskBatch') {
        selectedRows = this.groupTaskSelection
      } else if (opType === 'taskBatch') {
        selectedRows = this.taskSelection
      }
      const selectedIds = (selectedRows || []).map(task => task.id).filter(Boolean)
      if (!selectedIds.length) {
        this.$message.warning('请先选择要停止的任务')
        return
      }
      const serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.ForceStopBatch'
      this.$confirm('确认批量强制停止所选任务吗？', '操作确认', {
        dangerouslyUseHTMLString: true,
        confirmButtonText: '确定',
        cancelButtonText: '取消'
      }).then(() => {
        serviceAM.post(serviceURL, selectedIds).then(res => {
          if (res.data.success) {
            this.$message({
              type: 'success',
              message: '操作成功!'
            })
            let groupDetail = {}, groupType = ''
            if (opType === 'agentGroupTaskBatch') {
              groupDetail = this.agentDetailsGroupDetails?.chatGroupDto ?? {}
              groupType = 'agentGroupTask'
            } else if (opType === 'groupTaskBatch') {
              groupDetail = this.groupDetails?.chatGroupDto ?? {}
              groupType = 'groupTask'
            }
            if (groupDetail.id) {
              this.gettaskListData(groupType, groupDetail.id)
            } else {
              this.gettaskListData('task')
            }
          } else {
            app.$message({
              message: res.data.errorMessage || res.data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }).catch(() => {
        this.$message({
          type: 'info',
          message: '已取消操作'
        })
      })
    },
    // 查看任务描述
    viewTaskDescription(item) {
      this.describeContent = item?.promptCommand ?? ''
      this.taskDescriptionDetails = item || null
      this.visible.dialogTaskDescription = true
    },
    taskHumanInTheLoopLevelText(value) {
      return {
        0: 'L0 全自动',
        1: 'L1 风险分层',
        2: 'L2 工具审批',
        3: 'L3 Human 参与者 + 工具审批'
      }[Number(value)] || `未知（${value}）`
    },
    taskToolPermissionText(value) {
      return {
        0: '继承 HIL 等级',
        1: '自动执行',
        2: '执行前审批',
        3: '禁止使用'
      }[Number(value)] || `未知（${value}）`
    },
    taskHumanParticipantStatusText(task) {
      if (!task?.executionPolicyCaptured) {
        return '历史任务未记录'
      }
      return task.includeHumanParticipant ? '已加入本任务' : '本任务未启用'
    },
    taskDescriptionCopyText() {
      const detail = this.taskDescriptionDetails || {}
      if (!detail.executionPolicyCaptured) {
        return this.describeContent
      }

      return [
        this.describeContent,
        '',
        '--- 执行策略 ---',
        `HIL 等级：${this.taskHumanInTheLoopLevelText(detail.humanInTheLoopLevel)}`,
        `插件工具权限：${this.taskToolPermissionText(detail.pluginToolPermission)}`,
        `MCP 工具权限：${this.taskToolPermissionText(detail.mcpToolPermission)}`,
        `Human 参与者：${detail.includeHumanParticipant ? '包含' : '跳过'}`,
        `最大对话轮数：${Number(detail.chatMaxRound || 0)}`,
        `个性化参数：${detail.isPersonality ? '启用' : '关闭'}`,
        `兼容强制审批：${detail.requireHumanApproval ? '启用' : '关闭'}`
      ].join('\n')
    },
    // 任务描述复制
    taskDescriptionCopy() {
      // 复制文本
      this.copyText('4', this.taskDescriptionCopyText()).then(() => {
        this.handleElVisibleClose('dialogTaskDescription')
      })
    },
    // 任务评价
    taskEvaluation(item) {
      Object.assign(this.evaluationForm, item)
      this.visible.dialogTaskEvaluation = true
    },
    // input 数值类型处理
    handleInputNum(val, form) {
      if (form) {
        const sliderStep = 0.1
        let _val = val.replace(/[^\d]/g, '')
        //floor
        _val = Math.round(_val / sliderStep) * sliderStep
        if (form.includes('.')) {
          const formArr = form.split('.')
          // const formArrLen = formArr.length
          this.$set(this[formArr[0]], formArr[1], _val)
        } else {
          this.$set(this, form, _val)
        }
      }
    },
    // 任务成员列表筛选 
    handleTaskFilterChange(val, listType) {
      if (listType === 'agentGroupTask') {
        // 智能体 组 任务
        const chatGroupMembers = this.getTaskParticipantList(listType)
        const filterList = chatGroupMembers.filter(item => item.name.includes(val))
        this.agentGroupTaskMemberfilterList = filterList.map(item => this.getParticipantKey(item))
      } else if (listType === 'groupTask') {
        // 组 任务
        const chatGroupMembers = this.getTaskParticipantList(listType)
        const filterList = chatGroupMembers.filter(item => item.name.includes(val))
        this.groupTaskMemberfilterList = filterList.map(item => this.getParticipantKey(item))
      } else if (listType === 'agentTask') {
        // 智能体 任务
        const filterList = this.agentDetailsTaskMemberList.filter(item => item.name.includes(val))
        this.agentTaskMemberfilterList = filterList.map(item => this.getParticipantKey(item))
      } else if (listType === 'task') {
        // 任务
        const filterList = this.taskMemberList.filter(item => item.name.includes(val))
        this.taskMemberfilterList = filterList.map(item => this.getParticipantKey(item))
        console.log('handleTaskFilterChange', this.taskMemberfilterList);
      }
    },

    getTaskHistoryListForParticipantInfo(taskType) {
      const historyByType = {
        task: this.taskHistoryList,
        agentTask: this.agentDetailsTaskHistoryList,
        agentGroupTask: this.agentDetailsGroupTaskHistoryList,
        groupTask: this.groupTaskHistoryList,
      }
      return Array.isArray(historyByType[taskType]) ? historyByType[taskType] : []
    },

    getParticipantQuickInfo(taskType, participant) {
      const participantKey = this.getParticipantKey(participant)
      const relatedHistory = this.getTaskHistoryListForParticipantInfo(taskType)
        .filter(item => this.getParticipantKey(item) === participantKey)
      const usage = this.buildTaskHistoryUsageSummary(relatedHistory)
      const isHuman = participant?.isHuman === true
        || participant?.agentKind === 'Human'
        || participantKey.startsWith('human:')
      const hasReportedTokenUsage = relatedHistory.some(item => {
        return ['promptTokens', 'completionTokens', 'totalTokens']
          .some(field => item?.[field] !== null && item?.[field] !== undefined)
      })
      const isRemote = participant?.agentKind === 'RemoteA2A'
      const enabled = participant?.enable !== false
      const statusText = isRemote
        ? this.remoteParticipantAvailabilityText(participant)
        : (enabled ? '已启用' : '已停用')
      const statusType = isRemote
        ? this.remoteParticipantAvailabilityType(participant)
        : (enabled ? 'success' : 'info')
      const currentUsageText = isHuman
        ? (usage.messageCount === 0
          ? '尚无已提交输入'
          : `已输入 ${this.formatUsageCount(usage.messageCount)} 条文本`)
        : (usage.messageCount === 0
          ? '尚无已完成回复'
          : (hasReportedTokenUsage
            ? `${this.formatUsageCount(usage.messageCount)} 条回复 · ${this.formatUsageCount(usage.totalTokens)} Token`
            : `${this.formatUsageCount(usage.messageCount)} 条回复 · Token 未由远端反馈`))
      const totalTokens = Number(participant?.totalTokens || 0)

      return {
        kindText: isHuman ? 'Human 参与者' : (isRemote ? '远程 A2A' : '本地 Agent'),
        kindType: isHuman ? 'info' : (isRemote ? 'warning' : 'primary'),
        statusText,
        statusType,
        description: participant?.description || '暂无简介',
        currentUsageText,
        responseTimeText: isHuman
          ? '不适用'
          : this.formatResponseMilliseconds(usage.averageResponseMilliseconds, '暂无响应时长'),
        totalUsageText: isHuman
          ? '不产生模型 Token'
          : (totalTokens > 0
            ? `${this.formatUsageCount(totalTokens)} Token`
            : '暂无累计数据'),
        activityText: this.formatActivityTime(participant?.lastActiveTime || participant?.lastHealthCheckAt),
        healthMessage: isRemote ? (participant?.lastHealthCheckMessage || '尚未执行连接检测') : '',
        isRemote,
        canOpenEditor: !isHuman && Number.isInteger(Number(participant?.id)) && Number(participant.id) > 0,
      }
    },

    buildParticipantEditorUrl(participant) {
      const id = Number(participant?.id || 0)
      if (!Number.isInteger(id) || id <= 0 || participant?.isHuman === true || participant?.agentKind === 'Human') {
        return ''
      }

      if (participant?.agentKind === 'RemoteA2A') {
        return `/Admin/AgentsManager/Index#tab=remoteA2A&view=edit&remoteAgentId=${id}`
      }

      return `/Admin/AgentsManager/Index#tab=first&view=edit&agentId=${id}`
    },

    openParticipantAgentEditor(participant) {
      const url = this.buildParticipantEditorUrl(participant)
      if (!url) {
        return
      }

      const participantType = participant?.agentKind === 'RemoteA2A' ? 'RemoteA2A' : 'Local'
      const targetName = `NcfAgentsManager_${participantType}_${participant.id}`
      const openedWindow = typeof window.open === 'function'
        ? window.open(url, targetName)
        : null
      if (openedWindow) {
        openedWindow.focus?.()
        return
      }

      window.location?.assign?.(url)
    },

    getGroupParticipantList(groupDetail) {
      const localAgents = groupDetail?.agentTemplateDtoList ?? []
      const roleAgents = groupDetail?.roleAgentTemplateDtoList ?? []
      const remoteMembers = groupDetail?.remoteMemberDtoList ?? []
      const localParticipantMap = new Map()
      const localParticipants = localAgents.map(agent => {
        const participant = Object.assign({}, agent, {
          participantKey: agent.isHuman ? `human:${agent.id}` : `local:${agent.id}`,
          agentKind: agent.isHuman ? 'Human' : 'Local',
          roles: []
        })
        localParticipantMap.set(agent.id, participant)
        return participant
      })

      roleAgents.forEach(role => {
        const agent = role?.agentTemplateDto
        const roleName = String(role?.roleName || '').trim()
        if (!agent?.id) return

        let participant = localParticipantMap.get(agent.id)
        if (!participant) {
          participant = Object.assign({}, agent, {
            participantKey: `local:${agent.id}`,
            agentKind: 'Local',
            roles: []
          })
          localParticipantMap.set(agent.id, participant)
          localParticipants.push(participant)
        }
        if (roleName && !participant.roles.includes(roleName)) {
          participant.roles.push(roleName)
        }
      })

      const remoteAgents = remoteMembers
        .map(member => {
          const remote = member?.remoteAgentDto
          if (!remote || !remote.id) return null
          return Object.assign({}, remote, {
            participantKey: `remote:${remote.id}`,
            agentKind: 'RemoteA2A',
            avastar: null,
            enable: !!member.enable && !!remote.enable,
            connectionStatus: remote.connectionStatus
          })
        })
        .filter(Boolean)

      return localParticipants.concat(remoteAgents)
    },

    getTaskParticipantList(taskType) {
      if (taskType === 'agentGroupTask' && this.agentDetailsGroupTaskMemberList.length) {
        return this.agentDetailsGroupTaskMemberList
      }
      if (taskType === 'groupTask' && this.groupTaskMemberList.length) {
        return this.groupTaskMemberList
      }
      if (taskType === 'agentGroupTask') {
        return this.getGroupParticipantList(this.agentDetailsGroupDetails)
      }
      if (taskType === 'groupTask') {
        return this.getGroupParticipantList(this.groupDetails)
      }
      return []
    },

    getParticipantKey(participantOrHistory) {
      if (!participantOrHistory) return ''
      if (participantOrHistory.fromParticipantKey) return participantOrHistory.fromParticipantKey
      if (participantOrHistory.participantKey) return participantOrHistory.participantKey
      if (participantOrHistory.fromAgentTemplateId !== undefined && participantOrHistory.fromAgentTemplateId !== null) {
        return `local:${participantOrHistory.fromAgentTemplateId}`
      }
      return participantOrHistory.id !== undefined && participantOrHistory.id !== null
        ? `local:${participantOrHistory.id}`
        : ''
    },

    historyMatchesMemberFilter(historyItem, filterText, matchedParticipantKeys) {
      return !filterText || matchedParticipantKeys.includes(this.getParticipantKey(historyItem))
    },

    // el-scrollbar 触底滚动 到底部
    scrollbarDown(refName, istouchBottom = false, isFirst = false) {
      if (!refName) return
      const scrollbar = this.$refs[refName];
      if (!scrollbar) return
      if (istouchBottom) {
        const scrollTop = scrollbar.wrap.scrollTop; // 当前滚动的顶部
        const scrollHeight = scrollbar.wrap.scrollHeight; // 内容总高度
        const clientHeight = scrollbar.wrap.clientHeight; // 可视区域高度
        // scrollTop, scrollHeight, clientHeight
        if (scrollHeight !== clientHeight && (scrollTop + clientHeight + 30 >= scrollHeight || isFirst)) {
          // 滚动到底部
          scrollbar.wrap.scrollTop = scrollbar.wrap.scrollHeight;
        }
      } else {
        // 滚动到底部
        scrollbar.wrap.scrollTop = scrollbar.wrap.scrollHeight;
      }
    },
    // 获取发送人名称
    getTaskSenderName(taskType, historyItem) {
      const sender = this.getTaskSenderInfo(taskType, historyItem)
      if (sender && sender.name) {
        return sender.name
      }
      return historyItem?.fromParticipantName || historyItem?._streamAgentName || (historyItem?._generating ? 'Generating...' : '')
    },
    getTaskSenderInfo(taskType, participantOrHistory) {
      const participantKey = this.getParticipantKey(participantOrHistory)
      const formId = participantOrHistory?.fromAgentTemplateId ?? participantOrHistory?.id ?? participantOrHistory
      // 智能体 组 任务
      if (taskType === 'agentGroupTask') {
        const chatGroupMembers = this.getTaskParticipantList(taskType)
        const fintItem = chatGroupMembers.find(item => this.getParticipantKey(item) === participantKey)
          || chatGroupMembers.find(item => item.agentKind !== 'RemoteA2A' && String(item.id) === String(formId))
        return fintItem ?? {}
      }
      // 组 任务
      if (taskType === 'groupTask') {
        const chatGroupMembers = this.getTaskParticipantList(taskType)
        const fintItem = chatGroupMembers.find(item => this.getParticipantKey(item) === participantKey)
          || chatGroupMembers.find(item => item.agentKind !== 'RemoteA2A' && String(item.id) === String(formId))
        return fintItem ?? {}
      }
      // 智能体 任务
      if (taskType === 'agentTask') {
        const fintItem = this.agentDetailsTaskMemberList.find(item => this.getParticipantKey(item) === participantKey)
          || this.agentDetailsTaskMemberList.find(item => item.agentKind !== 'RemoteA2A' && item.id === formId)
        return fintItem ?? {}
      }

      // 任务
      if (taskType === 'task') {
        const fintItem = this.taskMemberList.find(item => this.getParticipantKey(item) === participantKey)
          || this.taskMemberList.find(item => item.agentKind !== 'RemoteA2A' && item.id === formId)
        return fintItem ?? {}
      }

      return {}
    },
    jumpPromptRange(urlType, item) {
      let url = ''
      if (urlType === 'promptRange') {
        // 靶场:rangeId   靶道:promptId（hash 路由）
        const rangeId = item?.promptRange?.id ?? ''
        const promptId = item?.id ?? ''
        if (rangeId && promptId) {
          url = `/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18#rangeId=${rangeId}&promptId=${promptId}`
        } else {
          url = `/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18`
        }
      }
      if (urlType === 'model') {
        url = `/Admin/AIKernel/Index?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69`
      }
      if (urlType === 'modelParameter') {
        // url = `/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18`
        if (item) {
          // 展示详情数据
          this.$confirm(`<div class="df">
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>Top_p:</span>
                        <span>${item.topP}</span>
                    </div>
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>Temperature:</span>
                        <span>${item.temperature}</span>
                    </div>
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>MaxToken:</span>
                        <span>${item.maxToken}</span>
                    </div>
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>Frequeny_penalty:</span>
                        <span>${item.frequencyPenalty}</span>
                    </div>
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>Presence_penalty:</span>
                        <span>${item.presencePenalty}</span>
                    </div>
                    <div class="df-wn flex-ac flex-js" style="width:50%">
                        <span>StopSequences:</span>
                        <span>${item.stopSequences}</span>
                    </div>
    </div>`, '模型参数', {
            dangerouslyUseHTMLString: true, // message 当作 HTML片段处理
            confirmButtonText: '确定',
            cancelButtonText: '取消',
            showCancelButton: false,
            // type: 'warning'
          }).then(() => { }).catch(() => { });
        }
      }
      if (!url) return
      simulationAELOperation(url)
      // openWindow(url)
    },
    // 处理靶道 和 靶场展示名称
    handlePromptShowName(showType, item) {
      let resultText = ''
      if (showType === '1') {
        // 靶道
        const itemData = item?.promptRangeDto ?? ''
        if (itemData) {
          resultText = `${itemData.alias}(${itemData.rangeName})`
        }
      } else if (showType === '2') {
        // 靶场
        const itemData = item?.promptItemDto ?? ''
        if (itemData) {
          const avg = scoreFormatter(itemData.evalAvgScore)
          const max = scoreFormatter(itemData.evalMaxScore)
          resultText = `${itemData.nickName || '未设置'} | ${itemData.fullVersion} | 平均分：${avg} | 最高分：${max} ${itemData.isDraft ? '(草稿)' : ''}`
        }
      }
      return resultText ?? ''
    },
    // 复制 task 任务描述
    async copyText(opType, item) {
      let text = ''
      if (opType === '1') {
        text = item?.message ?? ''
      } else if (opType === '2') {
        text = item?.messageHtml ?? ''
      } else if (opType === '3') {
        text = item?.promptCommand ?? ''
      } else if (opType === '4') {
        text = item ?? ''
      }

      const copied = await copyTextForEmbeddedBrowser(text)
      this.$message[copied ? 'success' : 'error'](copied ? '复制成功' : '复制失败')
      return copied
    },
    openFunctionBindingDrawer() {
      this.visible.drawerFunctionBindings = true
      this.functionBindingSearch = ''
      this.functionBindingTab = 'function'
      this.loadFunctionBindingCatalog()
    },
    async loadFunctionBindingCatalog() {
      this.functionBindingLoading = true
      try {
        const agentId = Number(this.agentForm?.id || 0)
        const response = await serviceAM.get(
          `/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetFunctionBindingCatalog?agentId=${agentId}`)
        const data = response?.data ?? {}
        if (!data.success) {
          this.$message.error(data.errorMessage || '读取 Function 与 Workflow 列表失败')
          return
        }

        const catalog = data.data || {}
        this.$set(this, 'functionBindingCatalog', {
          functions: Array.isArray(catalog.functions) ? catalog.functions : [],
          plugins: Array.isArray(catalog.plugins) ? catalog.plugins : [],
          workflows: Array.isArray(catalog.workflows) ? catalog.workflows : [],
          currentBindings: Array.isArray(catalog.currentBindings) ? catalog.currentBindings : []
        })
        if ((!this.agentForm.functionBindings || this.agentForm.functionBindings.length === 0)
          && this.functionBindingCatalog.currentBindings.length > 0) {
          this.$set(this.agentForm, 'functionBindings', this.functionBindingCatalog.currentBindings)
          this.syncLegacyFunctionCallNames()
        }
      } catch (error) {
        console.error('读取 Function 绑定目录失败:', error)
        this.$message.error(error?.message || '读取 Function 与 Workflow 列表失败')
      } finally {
        this.functionBindingLoading = false
      }
    },
    normalizeFunctionBinding(binding) {
      if (!binding) return null
      const kind = String(binding.kind || binding.Kind || 'plugin').toLowerCase()
      const key = String(binding.key || binding.Key || '').trim()
      if (!key) return null
      return {
        kind,
        key,
        name: binding.name || binding.Name || key,
        description: binding.description || binding.Description || '',
        moduleUid: binding.moduleUid || binding.ModuleUid || '',
        functionKey: binding.functionKey || binding.FunctionKey || '',
        workflowId: binding.workflowId || binding.WorkflowId || (kind === 'workflow' ? Number(key) || null : null)
      }
    },
    isFunctionBindingSelected(option) {
      const normalized = this.normalizeFunctionBinding(option)
      if (!normalized) return false
      return (this.agentForm.functionBindings || []).some(item => {
        const current = this.normalizeFunctionBinding(item)
        return current && current.kind === normalized.kind && current.key.toLowerCase() === normalized.key.toLowerCase()
      })
    },
    toggleFunctionBinding(option, selected) {
      const normalized = this.normalizeFunctionBinding(option)
      if (!normalized) return
      const current = (this.agentForm.functionBindings || [])
        .map(item => this.normalizeFunctionBinding(item))
        .filter(Boolean)
      const index = current.findIndex(item => item.kind === normalized.kind
        && item.key.toLowerCase() === normalized.key.toLowerCase())
      if (selected && index < 0) {
        current.push(normalized)
      } else if (!selected && index >= 0) {
        current.splice(index, 1)
      }
      this.$set(this.agentForm, 'functionBindings', current)
      this.syncLegacyFunctionCallNames()
    },
    removeFunctionBinding(binding) {
      this.toggleFunctionBinding(binding, false)
    },
    syncLegacyFunctionCallNames() {
      const names = (this.agentForm.functionBindings || [])
        .map(item => this.normalizeFunctionBinding(item))
        .filter(item => item?.kind === 'plugin')
        .map(item => item.key)
      this.$set(this.agentForm, 'functionCallNames', [...new Set(names)].join(','))
    },
    // 组成员头像堆叠 数量处理
    displayedAvatars(list, limit = 5) {
      if (Array.isArray(list)) {
        return list?.slice(0, limit) ?? [];
      }
      return []
    },
    // 组成员头像堆叠 数量
    exceededCount(list, limit = 5) {
      if (Array.isArray(list)) {
        return list.length > limit ? list.length - limit : 0;
      }
      return 0
    },
    // 显示新增 Function Call 输入框
    showFunctionCallInput() {
      this.functionCallInputVisible = true;
      this.$nextTick(_ => {
        this.$refs.functionCallInput.$refs.input.focus();
      });
    },

    // 处理 Function Call 输入确认
    handleFunctionCallInputConfirm() {
      const inputValue = this.functionCallInputValue;
      if (inputValue) {
        if (!this.agentForm.functionCallNames) {
          this.agentForm.functionCallNames = inputValue;
          this.functionCallTags = [inputValue];
        } else {
          const currentNames = this.agentForm.functionCallNames.split(',').filter(x => x);
          if (!currentNames.includes(inputValue)) {
            this.agentForm.functionCallNames = [...currentNames, inputValue].join(',');
            this.functionCallTags = [...currentNames, inputValue];
          }
        }
      }
      this.functionCallInputVisible = false;
      this.functionCallInputValue = '';
    },

    // 删除 Function Call 标签
    handleFunctionCallClose(tag) {
      const currentNames = this.getFunctionCallNamesList();
      const index = currentNames.indexOf(tag);
      if (index > -1) {
        currentNames.splice(index, 1);
        this.agentForm.functionCallNames = currentNames.join(',');
      }
      this.functionCallTags = currentNames;
    },

    // 获取当前 functionCallNames 的数组形式
    getFunctionCallNamesList() {
      return this.agentForm.functionCallNames
        ? this.agentForm.functionCallNames.split(',').filter(x => x)
        : [];
    },

    // 自动附加所有 XNCF 功能插件
    handleAutoAttachXncfChange(val) {
      if (val) {
        // 开启时：将所有可用插件类型合并到 functionCallNames
        const currentNames = this.getFunctionCallNamesList();
        const allNames = [...new Set([...currentNames, ...this.pluginTypes])];
        this.agentForm.functionCallNames = allNames.join(',');
      } else {
        // 关闭时：移除所有自动添加的插件类型（保留用户手动添加的）
        const currentNames = this.getFunctionCallNamesList();
        const manualNames = currentNames.filter(name => !this.pluginTypes.includes(name));
        this.agentForm.functionCallNames = manualNames.join(',');
      }
    },
    
    // 测试MCP Endpoint连接
    async testMcpEndpoint(name, endpoint) {
      // 设置加载状态
      this.$set(endpoint, 'testing', true);
      
      try {
        const response = await axios.get('/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.TestMcpConnection', {
          params: {
            endpointName: name,
            endpointUrl: endpoint.url
          }
        });
        
        // 详细日志
        console.log('MCP测试响应数据:', response);
        
        // 根据实际API返回的数据结构进行判断
        if (response.data && response.data.success) {
          // 尝试从不同位置获取工具列表
          let tools = [];
          let status = 200;
          
          // 调试完整响应
          console.log('API返回数据结构:', JSON.stringify(response.data, null, 2));
          
          // 检查可能的数据结构
          if (response.data.data) {
            console.log('data字段:', response.data.data);
            
            // 结构1: response.data.data.tools
            if (response.data.data.tools) {
              console.log('从data.tools获取工具列表');
              tools = response.data.data.tools;
              status = response.data.data.status || 200;
            } 
            // 结构2: response.data.data直接是工具列表
            else if (Array.isArray(response.data.data)) {
              console.log('data直接是工具列表');
              tools = response.data.data;
            }
          }
          
          console.log('提取的工具列表:', tools);
          
          // 确保工具列表是数组
          if (!Array.isArray(tools)) {
            console.warn('工具列表不是数组，将转换为空数组');
            tools = [];
          }
          
          // 确保每个工具对象都有必要的属性
          tools = tools.map(tool => ({
            name: tool.name || '未命名工具', 
            description: tool.description || '无描述',
            parameters: Array.isArray(tool.parameters) ? tool.parameters : []
          }));
          
          console.log('处理后的工具列表:', tools);
          
          // 初始化testResult对象
          if (!endpoint.testResult) {
            this.$set(endpoint, 'testResult', {});
          }
          
          // 设置结果属性
          this.$set(endpoint.testResult, 'success', true);
          this.$set(endpoint.testResult, 'tools', tools);
          this.$set(endpoint.testResult, 'status', status);
          
          console.log('更新后的endpoint对象:', JSON.parse(JSON.stringify(endpoint)));
          
          this.$message.success('连接测试成功');
          
          // 如果有工具列表，直接显示弹窗
          if (tools && tools.length > 0) {
            this.showMcpToolsDialog(endpoint);
          }
        } else {
          const testResult = {
            success: false,
            message: response.data.errorMessage || '未知错误'
          };
          this.$set(endpoint, 'testResult', testResult);
          this.$message.error('连接测试失败: ' + testResult.message);
        }
      } catch (error) {
        console.error('测试MCP连接出错:', error);
        const testResult = {
          success: false,
          message: error.message || '未知错误'
        };
        this.$set(endpoint, 'testResult', testResult);
        this.$message.error('连接测试出错: ' + testResult.message);
      } finally {
        // 清除加载状态
        this.$set(endpoint, 'testing', false);
      }
    },
    
    // 获取插件类型列表
    async getPluginTypes() {
      try {
        const res = await serviceAM.get('/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetPluginTypes');
        if (res?.data?.success) {
          this.pluginTypes = res.data.data || [];
        }
      } catch (error) {
        console.error('获取插件类型失败:', error);
        this.$message.error('获取插件类型失败');
      }
    },

    getKnowledgeBaseBindingInfo(knowledgeBaseId) {
      const id = Number(knowledgeBaseId || 0)
      if (!Number.isInteger(id) || id <= 0) {
        return { text: '未绑定', type: 'info' }
      }
      if (!this.knowledgeBaseOptionsLoaded) {
        return { text: '正在读取状态', type: 'info' }
      }
      const knowledgeBase = (this.knowledgeBaseOptions || []).find(item => Number(item.id) === id)
      if (!knowledgeBase) {
        return { text: '知识库不可用', type: 'danger' }
      }
      if (knowledgeBase.embeddingStatus === 'legacy') {
        return { text: '旧版向量化，待发布', type: 'warning' }
      }
      return knowledgeBase.isEmbedded
        ? { text: '可检索', type: 'success' }
        : { text: '待向量化', type: 'warning' }
    },

    getKnowledgeBaseOptionLabel(knowledgeBase) {
      if (!knowledgeBase) return ''
      if (knowledgeBase.embeddingStatus === 'legacy') {
        return `${knowledgeBase.name}（已向量化，待重新发布）`
      }
      return knowledgeBase.isEmbedded
        ? knowledgeBase.name
        : `${knowledgeBase.name}（未向量化）`
    },

    buildKnowledgeBaseUrl(knowledgeBaseId, focus = 'materials') {
      const id = Number(knowledgeBaseId || 0)
      if (!Number.isInteger(id) || id <= 0) return ''

      const url = new URL('/Admin/KnowledgeBase/Index', window.location.origin)
      url.searchParams.set('knowledgeBaseId', String(id))
      url.searchParams.set('focus', focus === 'materials' ? 'materials' : 'edit')
      return url.pathname + url.search
    },

    openKnowledgeBase(knowledgeBaseId, focus = 'materials') {
      const url = this.buildKnowledgeBaseUrl(knowledgeBaseId, focus)
      if (!url) {
        this.$message.warning('请先选择有效的知识库。')
        return
      }

      const targetName = `NcfKnowledgeBase_${knowledgeBaseId}`
      const openedWindow = window.open(url, targetName)
      if (openedWindow) {
        openedWindow.focus()
        return
      }

      this.$confirm(
        '当前环境不能打开新窗口。为保留尚未保存的 Agent 修改，请先保存；确认后将在当前页面跳转到知识库。',
        '打开知识库',
        {
          confirmButtonText: '仍要跳转',
          cancelButtonText: '取消',
          type: 'warning'
        })
        .then(() => window.location.assign(url))
        .catch(() => {})
    },

    async refreshKnowledgeBaseOptions() {
      await this.getKnowledgeBaseOptions(true)
    },

    async getKnowledgeBaseOptions(showFeedback = false) {
      try {
        const res = await serviceAM.get('/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetKnowledgeBaseOptions')
        if (res?.data?.success) {
          this.knowledgeBaseOptions = Array.isArray(res.data.data) ? res.data.data : []
          if (showFeedback) {
            this.$message.success('知识库状态已刷新')
          }
        } else if (showFeedback) {
          this.$message.error(res?.data?.errorMessage || '知识库状态刷新失败')
        }
      } catch (error) {
        console.error('获取知识库列表失败:', error)
        this.knowledgeBaseOptions = []
        if (showFeedback) {
          this.$message.error('知识库状态刷新失败')
        }
      } finally {
        this.knowledgeBaseOptionsLoaded = true
      }
    },

    // 添加插件类型到 functionCallNames
    handleAddPluginType(pluginType) {
      if (!this.agentForm.functionCallNames) {
        this.agentForm.functionCallNames = pluginType;
        this.functionCallTags = [pluginType];
      } else {
        // 将现有值分割为数组
        const currentNames = this.agentForm.functionCallNames.split(',').filter(x => x);
        if (!currentNames.includes(pluginType)) {
          // 添加新值并用逗号连接
          this.agentForm.functionCallNames = [...currentNames, pluginType].join(',');
          this.functionCallTags = [...currentNames, pluginType];
        }
      }
    },
    // McpEndpoints 相关方法
    
    // 显示添加 Endpoint 输入框
    showMcpEndpointInput() {
      this.mcpEndpointInputVisible = true;
      this.mcpEndpointNameValue = '';
      this.mcpEndpointUrlValue = '';
      this.mcpEndpointEditMode = false;
      this.mcpEndpointOriginalName = '';
      this.$nextTick(() => {
        if (this.$refs.mcpEndpointNameInput) {
          this.$refs.mcpEndpointNameInput.$refs.input.focus();
        }
      });
    },
    
    // 编辑 Endpoint
    handleMcpEndpointEdit(name, endpoint) {
      this.mcpEndpointInputVisible = true;
      this.mcpEndpointNameValue = name;
      this.mcpEndpointUrlValue = endpoint.url;
      this.mcpEndpointEditMode = true;
      this.mcpEndpointOriginalName = name;
      
      this.$nextTick(() => {
        if (this.$refs.mcpEndpointNameInput) {
          this.$refs.mcpEndpointNameInput.$refs.input.focus();
        }
      });
    },
    
    // 取消添加 Endpoint
    cancelMcpEndpointInput() {
      this.mcpEndpointInputVisible = false;
      this.mcpEndpointNameValue = '';
      this.mcpEndpointUrlValue = '';
      this.mcpEndpointEditMode = false;
      this.mcpEndpointOriginalName = '';
    },
    
    // 确认添加或更新 Endpoint
    handleMcpEndpointInputConfirm() {
      const name = this.mcpEndpointNameValue.trim();
      const url = this.mcpEndpointUrlValue.trim();
      
      if (!name || !url) {
        this.$message.warning('名称和URL不能为空');
        return;
      }
      
      let endpoints = {};
      try {
        if (this.agentForm.mcpEndpoints) {
          endpoints = JSON.parse(this.agentForm.mcpEndpoints);
        }
      } catch (e) {
        console.error('Failed to parse mcpEndpoints:', e);
        endpoints = {};
      }
      
      if (this.mcpEndpointEditMode) {
        // 编辑模式：如果名称变了，需要删除旧的再添加新的
        if (this.mcpEndpointOriginalName !== name) {
          delete endpoints[this.mcpEndpointOriginalName];
        }
      }
      
      // 添加/更新 Endpoint
      endpoints[name] = { url };
      this.agentForm.mcpEndpoints = JSON.stringify(endpoints);
      
      // 清空输入框
      this.mcpEndpointInputVisible = false;
      this.mcpEndpointNameValue = '';
      this.mcpEndpointUrlValue = '';
      this.mcpEndpointEditMode = false;
      this.mcpEndpointOriginalName = '';
    },
    
    // 删除 Endpoint
    handleMcpEndpointRemove(name) {
      let endpoints = {};
      try {
        if (this.agentForm.mcpEndpoints) {
          endpoints = JSON.parse(this.agentForm.mcpEndpoints);
        }
      } catch (e) {
        console.error('Failed to parse mcpEndpoints:', e);
        return;
      }
      
      // 删除指定 Endpoint
      if (endpoints[name]) {
        delete endpoints[name];
        this.agentForm.mcpEndpoints = Object.keys(endpoints).length > 0 
          ? JSON.stringify(endpoints) 
          : '';
      }
    },
    
    // 显示MCP工具列表对话框
    showMcpToolsDialog(endpoint) {
      console.log('调用showMcpToolsDialog函数', endpoint);
      
      // 检查endpoint对象及其属性
      if (!endpoint) {
        console.error('endpoint参数为空');
        this.$message.warning('endpoint参数为空');
        return;
      }
      
      console.log('endpoint.testResult:', endpoint.testResult);
      
      if (endpoint && endpoint.testResult && endpoint.testResult.tools) {
        // 创建一个工具列表的副本
        const tools = [...endpoint.testResult.tools];
        console.log('显示工具列表:', tools);
        console.log('工具列表数量:', tools.length);
        
        // 设置当前MCP工具列表，并显示对话框
        this.currentMcpTools = tools;
        this.visible.dialogMcpTools = true;
        
        console.log('设置currentMcpTools:', this.currentMcpTools);
        console.log('设置dialogMcpTools为可见');
        
        // 如果对话框不显示，则尝试使用备用弹窗
        setTimeout(() => {
          if (!document.querySelector('.el-dialog__wrapper[aria-label="MCP 工具列表"]')) {
            console.warn('对话框未显示，使用备用弹窗');
            this.showMcpToolsAlert(tools);
          }
        }, 500);
      } else {
        console.warn('没有可用的工具信息', endpoint);
        this.$message.warning('没有可用的工具信息');
      }
    },

    // 备用的MCP工具列表弹窗 (使用alert)
    showMcpToolsAlert(tools) {
      if (!tools || !tools.length) {
        this.$message.warning('没有可用的工具信息');
        return;
      }
      
      this.$alert(
        `<div>
          <h3>工具列表 (${tools.length}个)</h3>
          <ul style="padding-left: 20px; text-align: left;">
            ${tools.map(tool => 
              `<li style="margin-bottom: 10px;">
                <div style="font-weight: bold; color: #409EFF;">${tool.name}</div>
                <div style="margin: 5px 0; color: #606266;">${tool.description || '无描述'}</div>
                ${tool.parameters && tool.parameters.length > 0 ? 
                  `<div style="margin-top: 5px;">
                    <div style="font-weight: bold;">参数:</div>
                    <ul style="padding-left: 20px;">
                      ${tool.parameters.map(param => 
                        `<li><span style="color: #409EFF;">${param.name}</span>: ${param.description || ''}</li>`
                      ).join('')}
                    </ul>
                  </div>` : 
                  '<div>无参数</div>'
                }
              </li>`
            ).join('')}
          </ul>
        </div>`,
        'MCP工具列表',
        {
          dangerouslyUseHTMLString: true,
          closeOnClickModal: true,
          closeOnPressEscape: true,
          confirmButtonText: '关闭'
        }
      );
    },
  }
});

/**
 * 节流 防抖
 * @param {Function} func
 * @param {number} wait
 * @param {boolean} immediate
 * @return {*}
 */
function debounce(func, wait, immediate) {
  let timeout, args, context, timestamp, result
  const later = function () {
    // 据上一次触发时间间隔
    const last = +new Date() - timestamp

    // 上次被包装函数被调用时间间隔 last 小于设定时间间隔 wait
    if (last < wait && last > 0) {
      timeout = setTimeout(later, wait - last)
    } else {
      timeout = null
      // 如果设定为immediate===true，因为开始边界已经调用过了此处无需调用
      if (!immediate) {
        result = func.apply(context, args)
        if (!timeout) context = args = null
      }
    }
  }

  return function (...args) {
    context = this
    timestamp = +new Date()
    const callNow = immediate && !timeout
    // 如果延时不存在，重新设定延时
    if (!timeout) timeout = setTimeout(later, wait)
    if (callNow) {
      result = func.apply(context, args)
      context = args = null
    }

    return result
  }
}

/**
* 克隆
* @param {Object} source
* @returns {Object}
*/
function deepClone(source) {
  if (!source && typeof source !== 'object') {
    throw new Error('error arguments', 'deepClone')
  }
  const targetObj = source.constructor === Array ? [] : {}
  Object.keys(source).forEach(keys => {
    if (source[keys] && typeof source[keys] === 'object') {
      targetObj[keys] = deepClone(source[keys])
    } else {
      targetObj[keys] = source[keys]
    }
  })
  return targetObj
}

/**
* 判断值是否 数字
* @param {*} val 需要判断的变量
*/
function isNumber(val) {
  // return !isNaN(val) && (typeof val === 'number' || !isNaN(Number(val)))
  return !isNaN(val) && val !== '' && (typeof val === 'number' || !isNaN(Number()))
}

/**
* 判断值是否是 空对象
* @param {*} val 需要判断的变量
*/
function isObjEmpty(obj) {
  return Object.keys(obj).length === 0;
}

/**
 * 打开 window窗口
 * @param {Sting} url
 * @param {Sting} title
 * @param {Number} w
 * @param {Number} h
 */
function openWindow(url, title, w, h) {
  // Fixes dual-screen position                            Most browsers       Firefox
  const dualScreenLeft = window.screenLeft !== undefined ? window.screenLeft : screen.left
  const dualScreenTop = window.screenTop !== undefined ? window.screenTop : screen.top

  const width = window.innerWidth ? window.innerWidth : document.documentElement.clientWidth ? document.documentElement.clientWidth : screen.width
  const height = window.innerHeight ? window.innerHeight : document.documentElement.clientHeight ? document.documentElement.clientHeight : screen.height

  const left = ((width / 2) - (w / 2)) + dualScreenLeft
  const top = ((height / 2) - (h / 2)) + dualScreenTop
  const newWindow = window.open(url, title, 'toolbar=no, location=no, directories=no, status=no, menubar=no, scrollbars=no, resizable=yes, copyhistory=no, width=' + w + ', height=' + h + ', top=' + top + ', left=' + left)

  // WKWebView 可能不创建新窗口，此时回退为当前窗口导航。
  if (newWindow && window.focus) {
    newWindow.focus()
  } else if (!newWindow) {
    window.location.assign(url)
  }
}

/**
 * 模拟 a 标签
 * @param {string} url // 原地址
 */
function simulationAELOperation(url = '', name = '') {
  if (!url) return
  const link = document.createElement('a')
  link.style.display = 'none'
  link.href = url
  if (name) link.download = name
  // 不强制 _blank：macOS WKWebView 未实现新窗口委托，强制新窗口会导致点击无响应。
  link.click()
  link.remove()
}

async function copyTextForEmbeddedBrowser(text) {
  if (!text) return false

  if (window.isSecureContext && navigator.clipboard && navigator.clipboard.writeText) {
    try {
      await navigator.clipboard.writeText(text)
      return true
    } catch (error) {
      console.warn('Clipboard API is unavailable, using the compatibility fallback.', error)
    }
  }

  const textarea = document.createElement('textarea')
  textarea.value = text
  textarea.setAttribute('readonly', 'readonly')
  textarea.style.position = 'fixed'
  textarea.style.opacity = '0'
  document.body.appendChild(textarea)
  textarea.focus()
  textarea.select()
  textarea.setSelectionRange(0, text.length)
  try {
    return document.execCommand('copy')
  } catch (error) {
    console.error('Copy fallback failed:', error)
    return false
  } finally {
    textarea.remove()
  }
}

/**
 * 处理接口 query 参数 转换为 string
 * @param {Object} queryObj // 原地址
 */
function getInterfaceQueryStr(queryObj) {
  if (!queryObj) return ''
  // 将对象转换为 URL 参数字符串
  return Object.entries(queryObj)
    .filter(([key, value]) => {
      // 过滤掉空值
      // console.log('value', typeof value)
      if (typeof value === 'string') {
        return value !== ''
      } else if (typeof value === 'object' && value instanceof Array) {
        return value.length > 0
      } else if (typeof value === 'number') {
        return true
      } else if (typeof value === 'boolean') {
        return true
      } else {
        // if(typeof value === 'undefined')
        return false
      }
    })
    .map(
      ([key, value]) => {
        if (Array.isArray(value)) {
          let str = ""
          for (let index in value) {
            str += `${index > 0 ? '&' : ''}${encodeURIComponent(key)}=${encodeURIComponent(value[index])}`
          }
          return str
        }
        return `${encodeURIComponent(key)}=${encodeURIComponent(value)}`
      }
    )
    .join('&')
}

/**
 * 日期格式化为 yyyy-MM-dd HH:mm:ss
 * @param {date} dateString
 * @param {string} format
 * @returns {string} - 格式化后的时间
 */
function formatDate(dateString, format = 'yyyy-MM-dd HH:mm:ss') {
  if (!dateString) return ''
  const dateObject = new Date(dateString);

  const year = dateObject.getFullYear();
  const month = String(dateObject.getMonth() + 1).padStart(2, '0'); // 月份从0开始
  const day = String(dateObject.getDate()).padStart(2, '0');
  const hours = String(dateObject.getHours()).padStart(2, '0');
  const minutes = String(dateObject.getMinutes()).padStart(2, '0');
  const seconds = String(dateObject.getSeconds()).padStart(2, '0');

  // 替换格式中的标识符
  return format
    .replace('yyyy', year)
    .replace('MM', month)
    .replace('dd', day)
    .replace('HH', hours)
    .replace('mm', minutes)
    .replace('ss', seconds);
};
/**
 * 计算持续时间
 * @param {string} startTime - 开始时间字符串（ISO格式）
 * @param {string} [endTime] - 结束时间字符串（ISO格式），可选
 * @returns {string} - 持续时间字符串，根据差值级别动态显示
 */
function calculateDuration(startTime, endTime) {
  if (!startTime) return ''
  // 将开始时间和结束时间转换为 Date 对象
  const startDate = new Date(startTime);
  const endDate = endTime ? new Date(endTime) : new Date(); // 如果没有结束时间，则使用当前时间

  // 计算时间差（以毫秒为单位）
  const durationInMillis = endDate - startDate;

  // 各个时间单位的毫秒值
  const secondsInMillis = 1000;
  const minutesInMillis = secondsInMillis * 60;
  const hoursInMillis = minutesInMillis * 60;
  const daysInMillis = hoursInMillis * 24;
  const yearsInMillis = daysInMillis * 365; // 假设一年365天

  // 计算各个时间单位
  const years = Math.floor(durationInMillis / yearsInMillis);
  const days = Math.floor((durationInMillis % yearsInMillis) / daysInMillis);
  const hours = Math.floor((durationInMillis % daysInMillis) / hoursInMillis);
  const minutes = Math.floor((durationInMillis % hoursInMillis) / minutesInMillis);
  const seconds = Math.floor((durationInMillis % minutesInMillis) / secondsInMillis);

  // 动态构建输出字符串
  let durationParts = [];
  if (years > 0) durationParts.push(ncfST('{0} 年', years));
  if (days > 0) durationParts.push(ncfST('{0} 天', days));
  if (hours > 0) durationParts.push(ncfST('{0} 小时', hours));
  if (minutes > 0) durationParts.push(ncfST('{0} 分钟', minutes));
  if (seconds > 0 || durationParts.length === 0) durationParts.push(ncfST('{0} 秒', seconds));

  return durationParts.join(' ');
}

// 简单对比 数组是否相等
function arraysEqual(arr1, arr2) {
  return JSON.stringify(arr1) === JSON.stringify(arr2);
}

// prompt 分数处理
function scoreFormatter(score) {
  return score === -1 ? '--' : score.toFixed(1)
}

/**
 * 加载载 模拟json 数据
 */
// function funcMockJson() {
//     return fetch("/json/AgentsManager/data.json")
//         .then((res) => {
//             return res.json();
//         })
// }

function sanitizeTaskHtml(value) {
  const html = String(value ?? '')
  if (typeof DOMPurify !== 'undefined') {
    return DOMPurify.sanitize(html)
  }

  const template = document.createElement('template')
  template.innerHTML = html
  template.content.querySelectorAll('script,style,iframe,object,embed,applet,base,form,meta,link').forEach(node => node.remove())
  template.content.querySelectorAll('*').forEach(element => {
    Array.from(element.attributes).forEach(attribute => {
      const name = attribute.name.toLowerCase()
      if (name.startsWith('on') || name === 'style' || name === 'srcdoc') {
        element.removeAttribute(attribute.name)
        return
      }

      if (['href', 'src', 'action', 'formaction', 'xlink:href'].includes(name)) {
        try {
          const url = new URL(attribute.value, document.baseURI)
          if (url.protocol !== 'http:' && url.protocol !== 'https:' && !attribute.value.trim().startsWith('#')) {
            element.removeAttribute(attribute.name)
          }
        } catch (error) {
          element.removeAttribute(attribute.name)
        }
      }
    })
  })
  return template.innerHTML
}

// 任务右侧成员列表：名称始终完整展示，点击后查看与当前任务关联的摘要。
Vue.component('participant-quick-action', {
  props: {
    participant: {
      type: Object,
      required: true
    },
    quickInfo: {
      type: Object,
      required: true
    },
    testing: {
      type: Boolean,
      default: false
    }
  },
  methods: {
    avatarUrl() {
      return this.participant?.avastar || '/images/AgentsManager/avatar/avatar1.png'
    },
    testRemote() {
      this.$emit('test-remote')
    },
    editAgent() {
      this.$emit('edit-agent')
    }
  },
  template: `
    <el-popover placement="left-start" :width="320" trigger="click" popper-class="participant-quick-info-popover">
      <section class="participant-quick-info">
        <div class="participant-quick-info__header">
          <img :src="avatarUrl()" alt="" class="participant-quick-info__avatar">
          <div>
            <div class="participant-quick-info__name">{{ participant.name }}</div>
            <div class="participant-quick-info__tags">
              <el-tag :type="quickInfo.kindType" size="mini">{{ quickInfo.kindText }}</el-tag>
              <el-tag :type="quickInfo.statusType" size="mini">{{ quickInfo.statusText }}</el-tag>
            </div>
          </div>
        </div>
        <div class="participant-quick-info__description">{{ quickInfo.description }}</div>
        <div class="participant-quick-info__metrics">
          <div><span>当前任务用量</span><strong>{{ quickInfo.currentUsageText }}</strong></div>
          <div><span>平均响应</span><strong>{{ quickInfo.responseTimeText }}</strong></div>
          <div><span>累计 Token</span><strong>{{ quickInfo.totalUsageText }}</strong></div>
          <div><span>最近活动</span><strong>{{ quickInfo.activityText }}</strong></div>
        </div>
        <div v-if="quickInfo.isRemote" class="participant-quick-info__health" :title="quickInfo.healthMessage">
          <span>连接检测</span>
          <span>{{ quickInfo.healthMessage }}</span>
        </div>
        <div class="participant-quick-info__footer">
          <span>用量仅统计当前任务中已加载的记录</span>
          <el-button v-if="quickInfo.canOpenEditor" type="text" size="mini" icon="el-icon-top-right" title="在新窗口中编辑 Agent" @click.stop="editAgent">编辑</el-button>
          <el-button v-if="quickInfo.isRemote" type="text" size="mini" :loading="testing" @click.stop="testRemote">测试连接</el-button>
        </div>
      </section>
      <button slot="reference" type="button" class="taskmain-member-action" :title="'查看 ' + participant.name + ' 的信息'">
        <span class="taskmain-member-action__avatar">
          <img :src="avatarUrl()" alt="">
        </span>
        <span class="taskmain-member-action__content">
          <span class="taskmain-member-action__name">{{ participant.name }}</span>
          <span class="taskmain-member-action__meta">
            <el-tag :type="quickInfo.kindType" size="mini">{{ quickInfo.kindText }}</el-tag>
            <el-tag :type="quickInfo.statusType" size="mini">{{ quickInfo.statusText }}</el-tag>
          </span>
        </span>
        <i class="el-icon-arrow-right taskmain-member-action__arrow" aria-hidden="true"></i>
      </button>
    </el-popover>
  `
})

// task-html-renderer 渲染任务对话记录的内容
Vue.component('task-html-renderer', {
  props: ['content'],
  render(createElement) {
    return createElement('div', {
      class: 'taskrecord-listWrap-item-content', // 使用 CSS 类
      domProps: {
        innerHTML: sanitizeTaskHtml(this.content)
      }
    });
  }
});

// 注册一个全局自定义指令 v-el-select-loadmore
Vue.directive('el-select-loadmore', {
  bind(el, binding, vnode) {
    // 获取element-ui定义好的scroll盒子
    const SELECTWRAP_DOM = el.querySelector('.el-select-dropdown .el-select-dropdown__wrap')
    SELECTWRAP_DOM.addEventListener('scroll', function () {
      /**
      * scrollHeight 获取元素内容高度(只读)
      * scrollTop 获取或者设置元素的偏移值,常用于, 计算滚动条的位置, 当一个元素的容器没有产生垂直方向的滚动条, 那它的scrollTop的值默认为0.
      * clientHeight 读取元素的可见高度(只读)
      * 如果元素滚动到底, 下面等式返回true, 没有则返回false:
      * ele.scrollHeight - ele.scrollTop === ele.clientHeight;
      */
      const condition = this.scrollHeight - this.scrollTop <= this.clientHeight
      if (condition) {
        binding.value()
      }
    })
  }
})

// load-more-select 组件
Vue.component('load-more-select', {
  // v-el-select-loadmore="interestsLoadmore" filterable remote collapse-tags reserve-keyword :remote-method="remoteMethod" @focus="remoteMethod('',true)" @visible-change="reverseArrow"
  template: `<div :class="[direction === 'horizontal' ? 'df-wn flex-ac flex-js' : '']" style="width:100%;gap:10px;">
        <el-select ref="elSelectLoadMore" v-model="selectVal"  :disabled="disabled" :loading="interesLoading" :placeholder="placeholder" filterable :multiple="multipleChoice" clearable style="width:100%" @change="handleChange">
    <el-option v-for="(item,index) in interestsOptions" :key="item.value" :label="item.label" :value="item.value">
      <template v-if="serviceType === 'model'">
        <span>{{item.label}}</span>
        <span style="float:right;display:flex;gap:4px;">
          <el-tag size="mini" effect="plain">{{modelTypeLabel(item.configModelType)}}</el-tag>
          <el-tag size="mini" type="info" effect="plain">{{modelPlatformLabel(item.aiPlatform)}}</el-tag>
          <el-tag v-if="item.deploymentName || item.modelId" size="mini" type="warning" effect="plain">{{item.deploymentName || item.modelId}}</el-tag>
        </span>
      </template>
      <template v-else>{{item.label}}</template>
    </el-option></el-select>
    <template v-if="direction==='horizontal'">
        <i class="cursorPointer fas fa-redo" title="刷新" @click="refreshManagementList" />
    </template>
    <template v-else>
        <el-button size="mini" @click="refreshManagementList" :loading="interesLoading">刷新</el-button>
        <el-button v-if="serviceType === 'systemMessage'" type="primary" size="mini" @click="jumpPromptRange('promptRange')">管理PromptRange</el-button>
        <el-button v-if="serviceType === 'model'" type="primary" size="mini" @click="jumpPromptRange('model')">管理模型</el-button>
    </template>
    
    </div>`,
  props: {
    // eslint-disable-next-line vue/require-prop-types
    value: {
      // type: [Array, String, Number],
      required: true
    },
    placeholder: {
      type: String,
      default: ''
    },
    multipleChoice: {
      type: Boolean,
      default: false
    },
    serviceType: {
      type: String,
      default: '' // 默认使用公共 
    },
    misiptvId: {
      type: [String, Number],
      default: ''
    },
    disabled: {
      type: Boolean,
      default: false
    },
    direction: {
      type: String,
      default: 'horizontal' // 横向/竖向  horizontal/vertical
    },
    chatOnly: {
      type: Boolean,
      default: false
    }
  },
  data: function () {
    return {
      optionVisible: false,
      interestsOptions: [], //  接口返回数据
      interesLoading: false,
      currentPageSize: 0,
      listQuery: {
        pageIndex: 0,
        pageSize: 0,
        // key: '',
        filter: ''
      }
    }
  },
  computed: {
    selectVal: {
      get() {
        if (this.multipleChoice) {
          return [...this.value]
        } else {
          return this.value ?? ''
        }
      },
      set(val) {
        if (this.multipleChoice) {
          this.$emit('input', [...val])
        } else {
          this.$emit('input', val)
        }
      }
    }
  },
  watch: {
    // serviceType: {
    //     handler(val = '') {
    //         this.listQuery.key = val
    //     },
    //     immediate: true
    // }
  },
  mounted() {
    // 找到dom
    // const rulesDom = this.$refs['elSelectLoadMore'].$el.querySelector(
    //     '.el-input .el-input__suffix .el-input__suffix-inner .el-input__icon'
    // )
    // // 对dom新增class
    // rulesDom?.classList.add('el-icon-arrow-up')
    this.refreshManagementList()
  },
  methods: {
    modelTypeLabel(type) {
      return Number(type) === 2 ? 'Chat' : `类型 ${type ?? '未知'}`
    },
    modelPlatformLabel(platform) {
      const labels = { 1: 'OpenAI', 2: 'Azure OpenAI', 3: 'Hugging Face', 4: 'NeuCharAI' }
      return labels[Number(platform)] || `平台 ${platform ?? '未知'}`
    },
    jumpPromptRange(urlType) {
      let url = ''
      if (urlType === 'promptRange') {
        const selectedPromptCode = typeof this.selectVal === 'string'
          ? this.selectVal.trim()
          : ''
        if (selectedPromptCode) {
          url = `/Admin/PromptRange/Prompt?handler=Resolve&uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18&promptCode=${encodeURIComponent(selectedPromptCode)}`
        } else {
          url = `/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18`
        }
      }
      if (urlType === 'model') {
        url = `/Admin/AIKernel/Index?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69`
      }
      if (!url) return
      simulationAELOperation(url)
      // openWindow(url)
    },
    reverseArrow(flag) {
      this.optionVisible = flag
      // 找到dom
      const rulesDom = this.$refs['elSelectLoadMore'].$el.querySelector(
        '.el-input .el-input__suffix .el-input__suffix-inner .el-input__icon'
      )
      if (flag) {
        rulesDom.classList.add('is-reverse') // 对dom新增class
      } else {
        rulesDom.classList.remove('is-reverse') // 对dom新增class
      }
    },
    handleChange(e) {
      if (this.multipleChoice) {
        const filterItem = this.interestsOptions.filter((item) => {
          return e.includes(item.value)
        })
        this.$emit('change', filterItem)
      } else {
        const fintItem = this.interestsOptions.find((item) => item.value === e)
        this.$emit('change', fintItem)
      }
    },
    // 远程搜索
    remoteMethod(query, isfocus) {
      // console.log(query, 8888, this.optionVisible,isfocus)
      if (this.optionVisible && isfocus) return
      this.listQuery.filter = query ?? ''
      this.listQuery.pageIndex = 1
      this.interestsOptions = []
      this.interesLoading = true
      this.managementListOption() // 请求接口
    },
    interestsLoadmore() {
      setTimeout(() => {
        this.listQuery.pageIndex = this.listQuery.pageIndex + 1
        if (this.listQuery.pageSize > this.currentPageSize) {
          this.listQuery.pageIndex = this.listQuery.pageIndex - 1
          return
        }
        this.managementListOption()
      }, 1000)
    },
    // 刷新接口
    refreshManagementList() {
      this.listQuery.pageIndex = 1
      this.interestsOptions = []
      this.interesLoading = true
      this.managementListOption()
    },
    // 调用接口
    managementListOption() {
      // console.log('managementListOption',this.serviceType);
      this.interesLoading = true // 本地搜索 调用
      if (this.serviceType === 'agent') {
        serviceAM.get(`/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetList?${getInterfaceQueryStr(this.listQuery)}`)
          .then(res => {
            // console.log('this.serviceType === agent', res);
            const data = res?.data ?? {}
            if (data.success) {
              const agentData = data?.data?.list ?? []
              const listData = agentData.map(item => {
                return {
                  ...item,
                  label: item.name,
                  value: item.id,
                  disabled: false
                }
              })
              this.interesLoading = false
              this.currentPageSize = listData?.length ?? 0
              //this.interestsOptions = this.interestsOptions.concat(listData)
              this.interestsOptions = listData
              // [...this.interestsOptions, ...listData]
              // console.log(this.interestsOptions, 888)
            } else {
              app.$message({
                message: data.errorMessage || data.data || 'Error',
                type: 'error',
                duration: 5 * 1000
              })
            }
          })
      } else if (this.serviceType === 'model') {
        serviceAM.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetListAsync', this.listQuery).then(res => {
          // console.log('this.serviceType === model', res);
          const data = res?.data ?? {}
          if (data.success) {
            //console.log('getModelOptData:', res.data)
            const modelData = (data?.data ?? [])
              .filter(item => !this.chatOnly || Number(item.configModelType) === 2)
            const listData = modelData.map(item => {
              return {
                ...item,
                label: item.alias,
                value: item.id,
                disabled: false
              }
            })
            this.interesLoading = false
            this.currentPageSize = listData?.length ?? 0
            this.interestsOptions = this.interestsOptions.concat(listData)
            // [...this.interestsOptions, ...listData]
            // console.log(this.interestsOptions, 888)
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      } else if (this.serviceType === 'systemMessage') {
        // /api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetPromptRangeTree
        // /api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.GetPromptRangeTree
        serviceAM.get('/api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.GetPromptRangeTree', this.listQuery).then(res => {
          // console.log('this.serviceType === systemMessage', res);
          const data = res?.data ?? {}
          if (data.success) {
            //console.log('getModelOptData:', res.data)
            const promptRangeData = data?.data ?? []
            const listData = promptRangeData.map(item => {
              return {
                ...item,
                label: item.text,
                disabled: false
              }
            })
            this.interesLoading = false
            this.currentPageSize = listData?.length ?? 0
            this.interestsOptions = this.interestsOptions.concat(listData)
            this.$emit('options-loaded', this.interestsOptions)
            // [...this.interestsOptions, ...listData]
            // console.log(this.interestsOptions, 888)
          } else {
            app.$message({
              message: data.errorMessage || data.data || 'Error',
              type: 'error',
              duration: 5 * 1000
            })
          }
        })
      }

    }
  },
})

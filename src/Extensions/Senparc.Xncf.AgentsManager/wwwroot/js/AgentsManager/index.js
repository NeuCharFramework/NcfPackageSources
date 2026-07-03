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
        dialogGroupAgent: false, // 智能体 新增dialog
        drawerGroup: false, // 组 新增|编辑
        drawerGroupStart: false, // 组 启动 
        dialogAgentParameter: false, // 智能体参数 列表
        dialogTaskDescription: false, // 任务描述
        dialogTaskEvaluation: false, // 任务评价页面
        dialogMcpTools: false, // MCP工具列表对话框
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
        mcpEndpoints: '', // MCP Endpoints
      },
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
        name: '', // 名称
        members: [], // 成员列表
        description: '', // 说明
        adminAgentTemplateId: '', // 群主即agent
        enterAgentTemplateId: '' // 对接人即agent
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
        description: ''
      },
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
      functionCallInputVisible: false,
      functionCallInputValue: '',
      functionCallTags: [], // 用于编辑时临时存储标签
      pluginTypes: [], // 存储所有可用的插件类型
      agentAutoAttachXncf: false, // 是否自动附加所有 XNCF 功能插件
      // MCP Endpoints相关
      mcpEndpointInputVisible: false,
      mcpEndpointNameValue: '',
      mcpEndpointUrlValue: '',
      mcpEndpointEditMode: false,
      mcpEndpointOriginalName: '',
      currentMcpTools: [], // 当前查看的MCP工具列表
      agentListViewMode: 'panel',
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
    parseHashRoute() {
      const raw = (window.location.hash || '').replace(/^#/, '')
      const route = {
        tab: '',
        view: '',
        agentId: null,
        groupId: null,
        taskId: null
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
        if (this.agentListViewMode === 'three') {
          route.view = 'three'
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
      if (!route.tab && !route.groupId && !route.taskId && !route.agentId) {
        return
      }

      this.isApplyingHashRoute = true
      try {
        const tab = ['first', 'second', 'third'].includes(route.tab) ? route.tab : 'first'
        if (this.tabsActiveName !== tab) {
          this.tabsActiveName = tab
          this.handleTabsClick()
        }

        if (tab === 'first') {
          if (route.view === 'three') {
            this.handleAgentListViewModeChange('three', true)
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
        agents: (snapshot.agents || []).map(item => [item.id, item.chattingCount, item.score, item.enable]),
        groups: (snapshot.groups || []).map(item => [item.id, item.runningTaskCount, item.state, item.taskStatusCounts]),
        links: (snapshot.links || []).map(item => [item.groupId, item.agentId]),
        collaborations: (snapshot.collaborations || []).map(item => [item.taskId, item.groupId, item.status, item.agentIds])
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
      const agentIdSet = new Set(filteredLinks.map(item => item.agentId))

      const hasExplicitGroupConstraint = Boolean(selectedGroupId)
        || this.agentGraphShowOnlyActiveGroup
        || selectedStatuses.length > 0

      // Keep ungrouped agents visible when there is no explicit group/status constraint.
      const filteredAgents = hasExplicitGroupConstraint
        ? allAgents.filter(item => agentIdSet.has(item.id))
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
        this.navigateByHash(this.buildCurrentRoute({ tab: 'first', view: this.agentListViewMode === 'three' ? 'three' : null }))
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
      this.syncHashRoute({ tab: 'first', view: this.agentListViewMode === 'three' ? 'three' : null })
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

      const graphAgentMap = new Map(graphAgents.map(item => [item.id, item]))
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
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            // 不是仅详情时 清除轮询
            if (!detailsOn) {
              this.clearHistoryTimer()
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

              if (['task', 'agentTask'].includes(detailType)) {
                // 获取任务成员列表
                this.getTaskMemberListData(detailType, taskDetail.chatGroupId)
              }
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
              item.messageHtml = marked.parse(item.message);
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
      await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${chatGroupld}`)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const taskMemberList = data?.data?.agentTemplateDtoList ?? []
            // 任务
            if (memberType === 'task') {
              this.$set(this, 'taskMemberList', taskMemberList)
            }
            // 智能体 任务
            if (memberType === 'agentTask') {
              this.$set(this, 'agentDetailsTaskMemberList', taskMemberList)
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
    openUsageAnalytics(detailType, taskDetail) {
      if (!taskDetail || !taskDetail.id) return

      this.usageAnalyticsTaskId = taskDetail.id
      this.usageAnalyticsTaskName = taskDetail.name || `Task-${taskDetail.id}`
      this.usageAnalyticsVisible = true

      let members = []
      if (detailType === 'task') members = this.taskMemberList || []
      if (detailType === 'agentTask') members = this.agentDetailsTaskMemberList || []
      if (detailType === 'groupTask') members = (this.groupDetails && this.groupDetails.agentTemplateDtoList) || []
      if (detailType === 'agentGroupTask') {
        members = (this.agentDetailsGroupDetails && this.agentDetailsGroupDetails.agentTemplateDtoList) || []
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
      const actionText = nextArchived ? '归档' : '取消归档'
      this.taskArchiveSavingId = item.id
      try {
        const response = await serviceAM.post(
          `/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.SetArchiveStatus?id=${item.id}&isArchived=${nextArchived}`
        )
        const data = response?.data ?? {}
        if (!data.success) {
          this.$message.error(data.errorMessage || data.data || `${actionText}失败`)
          return
        }

        this.$message.success(`${actionText}成功`)
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
        this.$message.error(`${actionText}失败：${error?.message || '未知错误'}`)
      } finally {
        this.taskArchiveSavingId = 0
      }
    },
    // 保存 submitForm 数据
    async saveSubmitFormData(saveType, serviceForm = {}) {
      //debugger
      let serviceURL = ''
      // agent 新增|编辑
      if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
        // 确保 serviceForm 是正确的对象
        serviceForm = serviceForm || {};

        // 直接将 functionCallTags 数组转换为字符串并赋值
        serviceForm.functionCallNames = this.functionCallTags.length > 0 ? this.functionCallTags.join(',') : '';

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
        if (serviceForm.members) {
          const membersIds = serviceForm.members.map(item => item.id)
          serviceForm.memberAgentTemplateIds = membersIds
          serviceURL += `?${getInterfaceQueryStr({ memberAgentTemplateIds: membersIds })}`
        }
      }
      // 组启动（运行任务） ['drawerGroupStart', 'drawerTaskStart'].includes(btnType)
      if (['drawerGroupStart', 'drawerTaskStart'].includes(saveType)) {
        serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.RunGroup'
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
          this.$nextTick(() => {
            this.visible[saveType] = false
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
    buildTaskGeneratingItem(listType, chatTaskId) {
      const nowIso = new Date().toISOString()
      return {
        id: `${listType}:generating:${chatTaskId || 'unknown'}`,
        fromAgentTemplateId: 0,
        addTime: nowIso,
        message: 'Generating...',
        messageHtml: marked.parse('Generating...'),
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

      if (this.shouldShowTaskGenerating(taskStatus)) {
        this.ensureTaskGeneratingPlaceholder(listType, chatTaskId)
      }

      if (typeof EventSource === 'undefined') {
        this.pollGetTaskHistoryData(listType, this.getTaskRecordListData, chatTaskId)
        return
      }

      const streamUrl = `/api/Senparc.Xncf.AgentsManager/ChatTaskStream/Subscribe?chatTaskId=${chatTaskId}&_ts=${Date.now()}`
      const source = new EventSource(streamUrl, { withCredentials: true })
      this.historyStream[listType] = source
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

        const statusText = String(payload.text).toLowerCase()
        const finishedStates = ['finished', 'cancelled']
        if (finishedStates.includes(statusText)) {
          this.closeTaskHistoryStream(listType)
          this.clearTaskGeneratingPlaceholder(listType)
          this.pullTaskHistoryAfterStreamClosed(listType, chatTaskId)
          return
        }

        const statusCodeMap = {
          chatting: 1,
          paused: 2
        }
        if (this.shouldShowTaskGenerating(statusCodeMap[statusText])) {
          this.ensureTaskGeneratingPlaceholder(listType, chatTaskId)
        }
        rearmSilentTimer()
      }

      source.addEventListener('chunk', onChunk)
      source.addEventListener('message', onMessage)
      source.addEventListener('status', onStatus)

      source.onerror = () => {
        if (this.historyStream[listType] !== source) return
        this.clearTaskHistoryStreamSilentTimer(listType)
        this.closeTaskHistoryStream(listType)
        this.clearTaskGeneratingPlaceholder(listType)
        this.pullTaskHistoryAfterStreamClosed(listType, chatTaskId)
        this.pollGetTaskHistoryData(listType, this.getTaskRecordListData, chatTaskId)
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
          if ([3, 4].includes(taskStatus)) {
            this.closeTaskHistoryStream(listType)
            this.clearTaskGeneratingPlaceholder(listType)
            this.pullTaskHistoryAfterStreamClosed(listType, chatTaskId)
            return
          }

          if (this.shouldShowTaskGenerating(taskStatus)) {
            this.ensureTaskGeneratingPlaceholder(listType, chatTaskId)
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
      const agentInfo = this.getTaskSenderInfo(listType, payload.fromAgentTemplateId || 0) || {}
      const oldMessage = existedIndex > -1 ? (historyList[existedIndex].message || '') : ''
      const mergedMessage = `${oldMessage}${payload.text || ''}`

      const draftItem = {
        id: draftKey,
        fromAgentTemplateId: payload.fromAgentTemplateId || 0,
        addTime: payload.timestamp ? new Date(payload.timestamp).toISOString() : new Date().toISOString(),
        message: mergedMessage,
        messageHtml: marked.parse(mergedMessage || ''),
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
      const finalItem = {
        id: payload.historyId || `${draftKey || 'msg'}:${Date.now()}`,
        fromAgentTemplateId: payload.fromAgentTemplateId || 0,
        addTime: payload.timestamp ? new Date(payload.timestamp).toISOString() : new Date().toISOString(),
        message,
        messageHtml: marked.parse(message),
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

          // 确保 functionCallNames 被正确初始化
          this.functionCallTags = formData.functionCallNames ? formData.functionCallNames.split(',').filter(Boolean) : [];

          // 将数据赋值给表单
          Object.assign(this[formName], formData);

          // 打印日志以便调试
          console.log('Loaded form data:', formData);
          console.log('functionCallTags:', this.functionCallTags);

        } else if (btnType === 'drawerGroup') {
          if (item.chatGroupDto) {
            Object.assign(this[formName], {
              ...item.chatGroupDto,
              members: item.agentTemplateDtoList || item.chatGroupMembers || []
            })
          } else {
            await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${item.id}`)
              .then(res => {
                const data = res?.data ?? {}
                if (data.success) {
                  const groupDetail = data?.data ?? {}
                  Object.assign(this[formName], {
                    ...groupDetail.chatGroupDto,
                    members: groupDetail.agentTemplateDtoList || groupDetail.chatGroupMembers || []
                  })
                }
              })
          }
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
        // 打开 抽屉
        this.handleElVisibleOpenBtn(btnType)
      }
    },
    // Dailog|抽屉 打开 按钮
    handleElVisibleOpenBtn(btnType, formData) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      // console.log('通用新增按钮:', btnType);
      let visibleKey = btnType
      // 组 启动 
      if (btnType === 'drawerGroupStart') {
        // 详情: formData.chatGroupDto 列表: formData
        this.groupStartForm.groupName = formData.chatGroupDto ? formData.chatGroupDto.name : formData.name
        this.groupStartForm.name = formData.chatGroupDto ? `${formData.chatGroupDto.name}1` : `${formData.name}1`
        this.groupStartForm.chatGroupId = formData.chatGroupDto ? formData.chatGroupDto.id : formData.id
      }
      if (btnType === 'drawerTaskStart') {
        visibleKey = 'drawerGroupStart'
      }
      if (btnType === 'drawerGroup') {
        this.getAgentListData('groupAgent')
      }
      this.visible[visibleKey] = true
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
        this.$nextTick(() => {
          this.visible[btnType] = false
        })
        return
      }
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      this.$confirm('确认关闭？')
        .then(_ => {
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
            // 重置 组获取智能体query
            this.$set(this, 'groupAgentQueryList', this.$options.data().groupAgentQueryList)
            this.groupAgentList = []
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
          if (formName) {
            this.$set(this, `${formName}`, this.$options.data()[formName])
            // Object.assign(this[formName],this.$options.data()[formName])
          }
          if (refName) {
            this.$refs[refName].resetFields();
          }
          this.$nextTick(() => {
            this.visible[btnType] = false
          })
          // 清理 Function Calls 数据
          if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
            this.functionCallTags = []
            this.functionCallInputVisible = false
            this.functionCallInputValue = ''
            this.agentAutoAttachXncf = false
          }
        })
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

    handleAgentDelete(item) {
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

        this.$confirm(previewMessage, '操作确认', {
          dangerouslyUseHTMLString: true,
          confirmButtonText: '确定',
          cancelButtonText: '取消'
        }).then(() => {
          serviceAM.post(serviceURL).then(res => {
            if (res.data.success) {
              this.$message({
                type: 'success',
                message: '删除成功!'
              })
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
        this.agentGroupTaskMemberfilter = ''
        this.agentGroupTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
      }
      if (clickType === 'groupTask') {
        this.groupShowType = '3'
        // 清空详情数据
        this.groupTaskDetails = ''
        this.groupTaskHistoryList = []
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
        let agentDtoList = this.agentDetailsGroupDetails?.agentTemplateDtoList ?? []
        let adminAgentId = this.agentDetailsGroupDetails?.chatGroupDto?.adminAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => a.id === adminAgentId)
        baseList = findItem ? [findItem] : []
      } else if (optype === 'agentGroupTaskEnter') {
        let agentDtoList = this.agentDetailsGroupDetails?.agentTemplateDtoList ?? []
        let enterAgentId = this.agentDetailsGroupDetails?.chatGroupDto?.enterAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => a.id === enterAgentId)
        baseList = findItem ? [findItem] : []
      } else if (optype === 'agentGroupTask') {
        baseList = this.agentDetailsGroupDetails?.agentTemplateDtoList ?? []
      } else if (optype === 'groupTaskAdmin') {
        let agentDtoList = this.groupDetails?.agentTemplateDtoList ?? []
        let adminAgentId = this.groupDetails?.chatGroupDto?.adminAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => a.id === adminAgentId)
        baseList = findItem ? [findItem] : []
      } else if (optype === 'groupTaskEnter') {
        let agentDtoList = this.groupDetails?.agentTemplateDtoList ?? []
        let enterAgentId = this.groupDetails?.chatGroupDto?.enterAgentTemplateId ?? ''
        let findItem = agentDtoList.find(a => a.id === enterAgentId)
        baseList = findItem ? [findItem] : []
      } else if (optype === 'groupTask') {
        baseList = this.groupDetails?.agentTemplateDtoList ?? []
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
    // 构建智能体参数列表：为基础 DTO 列表补充 promptItemDto / aiModelDto / promptRangeDto 及历史输出
    async buildAgentParameterList(baseList) {
      const result = []
      for (const agent of baseList) {
        const enriched = Object.assign({}, agent, { outputList: [] })
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
                oitem.resultStringHtml = marked.parse(oitem.resultString || '')
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
      let serviceURL = ''
      if (opType === 'agentGroupTaskBatch') {
        // item.chatGroupDto.id this.agentDetails.agentTemplateDto.id
        console.log('agentGroupTaskBatch:', this.agentGroupTaskSelection);
      } else if (opType === 'groupTaskBatch') {
        // item.chatGroupDto.id
        console.log('groupTaskBatch:', this.groupTaskSelection);
      } else if (opType === 'taskBatch') {
        console.log('taskSelection:', this.taskSelection);
      }
      if (!serviceURL) return
      // 操作确认 提示
      this.$confirm('确认批量启动数据吗？', '操作确认', {
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
      this.visible.dialogTaskDescription = true
    },
    // 任务描述复制
    taskDescriptionCopy() {
      // 复制文本
      this.copyText('4', this.describeContent).then(() => {
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
        const chatGroupMembers = this.agentDetailsGroupDetails?.agentTemplateDtoList ?? []
        const filterList = chatGroupMembers.filter(item => item.name.includes(val))
        this.agentGroupTaskMemberfilterList = filterList.map(item => item.id)
      } else if (listType === 'groupTask') {
        // 组 任务
        const chatGroupMembers = this.groupDetails?.agentTemplateDtoList ?? []
        const filterList = chatGroupMembers.filter(item => item.name.includes(val))
        this.groupTaskMemberfilterList = filterList.map(item => item.id)
      } else if (listType === 'agentTask') {
        // 智能体 任务
        const filterList = this.agentDetailsTaskMemberList.filter(item => item.name.includes(val))
        this.agentTaskMemberfilterList = filterList.map(item => item.id)
      } else if (listType === 'task') {
        // 任务
        const filterList = this.taskMemberList.filter(item => item.name.includes(val))
        this.taskMemberfilterList = filterList.map(item => item.id)
        console.log('handleTaskFilterChange', this.taskMemberfilterList);
      }
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
      const sender = this.getTaskSenderInfo(taskType, historyItem?.fromAgentTemplateId)
      if (sender && sender.name) {
        return sender.name
      }
      return historyItem?._streamAgentName || (historyItem?._generating ? 'Generating...' : '')
    },
    getTaskSenderInfo(taskType, formId) {
      // 智能体 组 任务
      if (taskType === 'agentGroupTask') {
        const chatGroupMembers = this.agentDetailsGroupDetails?.agentTemplateDtoList ?? []
        const fintItem = chatGroupMembers.find(item => item.id === formId)
        return fintItem ?? {}
      }
      // 组 任务
      if (taskType === 'groupTask') {
        const chatGroupMembers = this.groupDetails?.agentTemplateDtoList ?? []
        const fintItem = chatGroupMembers.find(item => item.id === formId)
        return fintItem ?? {}
      }
      // 智能体 任务
      if (taskType === 'agentTask') {
        const fintItem = this.agentDetailsTaskMemberList.find(item => item.id === formId)
        return fintItem ?? {}
      }

      // 任务
      if (taskType === 'task') {
        const fintItem = this.taskMemberList.find(item => item.id === formId)
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
    copyText(opType, item) {
      // 把结果复制到剪切板
      return new Promise(resolve => {
        try {
          const textarea = document.createElement('textarea');
          textarea.setAttribute('readonly', 'readonly');
          if (opType === '1') {
            // task 对话 原始内容
            textarea.value = item?.message ?? ''
          } else if (opType === '2') {
            // task 对话 HTML内容
            textarea.value = item?.messageHtml ?? ''
          } else if (opType === '3') {
            // task 对话 promptCommand(任务描述)内容
            textarea.value = item?.promptCommand ?? ''
          } else if (opType === '4') {
            // task 任务描述
            textarea.value = item ?? ''
          }

          document.body.appendChild(textarea);
          textarea.select();
          textarea.setSelectionRange(0, 9999);
          if (document.execCommand('copy')) {
            document.execCommand('copy');
            this.$message.success('复制成功');
            resolve()
          } else {
            this.$message.error('复制失败');
          }
          textarea.style.display = 'none';
        } catch (err) {
          console.error('Oops, unable to copy', err);
        }
      })

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

  // Puts focus on the newWindow
  if (window.focus) {
    newWindow.focus()
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
  link.target = '_blank'
  link.click()
  link.remove()
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
  if (years > 0) durationParts.push(`${years} 年`);
  if (days > 0) durationParts.push(`${days} 天`);
  if (hours > 0) durationParts.push(`${hours} 小时`);
  if (minutes > 0) durationParts.push(`${minutes} 分钟`);
  if (seconds > 0 || durationParts.length === 0) durationParts.push(`${seconds} 秒`);

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

// task-html-renderer 渲染任务对话记录的内容
Vue.component('task-html-renderer', {
  props: ['content'],
  render(createElement) {
    return createElement('div', {
      class: 'taskrecord-listWrap-item-content', // 使用 CSS 类
      domProps: {
        innerHTML: this.content // 直接插入 HTML
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
    <el-option v-for="(item,index) in interestsOptions" :key="item.value" :label="item.label" :value="item.value"></el-option></el-select>
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
            const modelData = data?.data ?? []
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

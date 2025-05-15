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
      // 智能体参数列表
      agentParameterTabsValue: 0, // tabs选中
      agentParameterList: [],
      // 描述内容
      describeContent: '',
      functionCallInputVisible: false,
      functionCallInputValue: '',
      functionCallTags: [], // 用于编辑时临时存储标签
      pluginTypes: [], // 存储所有可用的插件类型
      // MCP Endpoints相关
      mcpEndpointInputVisible: false,
      mcpEndpointNameValue: '',
      mcpEndpointUrlValue: '',
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
  },
  watch: {},
  created() {
    // 在组件创建时获取插件类型列表
    this.getPluginTypes();
  },
  mounted() {
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

  },
  beforeDestroy() {
    this.clearHistoryTimer()
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
    async gettaskListData(listType, id, page = 0) {
      const queryList = {}
      // 任务
      if (listType === 'task') {
        this.taskQueryList.pageIndex = page ?? 1
        Object.assign(queryList, this.taskQueryList)
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
            // 任务
            if (listType === 'task') {
              this.$set(this, 'taskList', handleTaskData)
              // 默认展示第一个任务详情
              if (handleTaskData && handleTaskData.length) {
                const taskDetail = this.taskDetails ? this.taskDetails : handleTaskData[0]
                this.getTaskDetailData(listType, taskDetail.id, taskDetail)
              }
            }
            // 智能体 任务
            if (listType === 'agentTask') {
              this.$set(this, 'agentDetailsTaskList', handleTaskData)
              // 默认展示第一个任务详情
              if (handleTaskData && handleTaskData.length) {
                const taskDetail = this.agentDetailsTaskDetails ? this.agentDetailsTaskDetails : handleTaskData[0]
                this.getTaskDetailData(listType, taskDetail.id, taskDetail)
              }
            }
            // 智能体 组 任务
            if (listType === 'agentGroupTask') {
              this.$set(this, 'agentDetailsGroupTaskList', handleTaskData)
            }
            // 组 任务
            if (listType === 'groupTask') {
              this.$set(this, 'groupTaskList', handleTaskData)
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
              if (['task', 'agentTask'].includes(detailType)) {
                // 获取任务成员列表
                this.getTaskMemberListData(detailType, taskDetail.chatGroupId)
              }
              // 轮询 获取对话数据
              this.pollGetTaskHistoryData(detailType, this.getTaskRecordListData, taskDetail.id)
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
            // 任务
            if (recordType === 'task') {
              if (nextHistoryId) {
                // for (let index = 0; index < historiesData.length; index++) {
                //     const element = historiesData[index];
                //     setTimeout(() => {
                //         this.taskHistoryList.push(element)
                //     }, 1000)
                // }
                if (historiesData.length > 0) {
                  const historyList = this.taskHistoryList.concat(historiesData);
                  this.$set(this, 'taskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.taskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  this.$set(this, 'taskHistoryList', historiesData)
                }
              }
              // 滚动区域 吸附底部
              this.$nextTick(() => {
                this.scrollbarDown('taskHistoryScrollbar', true, isFirst)
              })
            }
            // 智能体 任务
            if (recordType === 'agentTask') {
              if (nextHistoryId) {
                // for (let index = 0; index < historiesData.length; index++) {
                //     const element = historiesData[index];
                //     setTimeout(() => {
                //         this.taskHistoryList.push(element)
                //     }, 1000)
                // }
                if (historiesData.length > 0) {
                  const historyList = this.agentDetailsTaskHistoryList.concat(historiesData);
                  this.$set(this, 'agentDetailsTaskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.agentDetailsTaskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  this.$set(this, 'agentDetailsTaskHistoryList', historiesData)
                }
              }
              // 滚动区域 吸附底部
              this.$nextTick(() => {
                this.scrollbarDown('agentDetailsTaskHistoryScrollbar', true, isFirst)
              })
            }
            // 智能体 组 任务
            if (recordType === 'agentGroupTask') {
              if (nextHistoryId) {
                // for (let index = 0; index < historiesData.length; index++) {
                //     const element = historiesData[index];
                //     setTimeout(() => {
                //         this.taskHistoryList.push(element)
                //     }, 1000)
                // }
                if (historiesData.length > 0) {
                  const historyList = this.agentDetailsGroupTaskHistoryList.concat(historiesData);
                  this.$set(this, 'agentDetailsGroupTaskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.agentDetailsGroupTaskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  this.$set(this, 'agentDetailsGroupTaskHistoryList', historiesData)
                }
              }
              // 滚动区域 吸附底部
              this.$nextTick(() => {
                this.scrollbarDown('agentDetailsGroupTaskHistoryScrollbar', true, isFirst)
              })
            }
            // 组 任务
            if (recordType === 'groupTask') {
              if (nextHistoryId) {
                if (historiesData.length > 0) {
                  const historyList = this.groupTaskHistoryList.concat(historiesData);
                  this.$set(this, 'groupTaskHistoryList', historyList)
                }
              } else {
                const isassignment = arraysEqual(this.groupTaskHistoryList, historiesData)
                if (!isassignment && historiesData.length > 0) {
                  this.$set(this, 'groupTaskHistoryList', historiesData)
                }
              }
              // 滚动区域 吸附底部
              this.$nextTick(() => {
                this.scrollbarDown('groupTaskHistoryScrollbar', true, isFirst)
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

            if (this.tabsActiveName === 'first') {
              // agentTemplateStatus
              if (this.agentDetails) {
                const id = this.agentDetails.agentTemplateDto ? this.agentDetails.agentTemplateDto.id : this.agentDetails.id
                if (this.agentDetailsTabsActiveName === 'first') {
                  this.getGroupListData('agentGroup', id)
                } else {
                  this.gettaskListData('agentTask', id)
                }
              }
            } else if (this.tabsActiveName === 'second') {
              this.getGroupListData('group')
            } else {
              this.gettaskListData('task')
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
          let nextHistoryId = ''
          // 任务
          if (listType === 'task') {
            let lenIndex = this.taskHistoryList.length - 1
            nextHistoryId = this.taskHistoryList[lenIndex]?.id ?? ''
          }
          // 智能体 任务
          if (listType === 'agentTask') {
            let lenIndex = this.agentDetailsTaskHistoryList.length - 1
            nextHistoryId = this.agentDetailsTaskHistoryList[lenIndex]?.id ?? ''
          }
          // 智能体 组 任务
          if (listType === 'agentGroupTask') {
            let lenIndex = this.agentDetailsGroupTaskHistoryList.length - 1
            nextHistoryId = this.agentDetailsGroupTaskHistoryList[lenIndex]?.id ?? ''
          }
          // 组 任务
          if (listType === 'groupTask') {
            let lenIndex = this.groupTaskHistoryList.length - 1
            nextHistoryId = this.groupTaskHistoryList[lenIndex]?.id ?? ''
          }
          // 执行代码块
          fun(listType, id, nextHistoryId)
          interval()
        }, 1000 * 5)
        // console.log('pollGetTaskHistoryData', this.historyTimer[listType]);
      }
      interval()
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
      this.$refs[refName].validate((valid) => {
        if (valid) {
          const submitForm = this[formName] ?? {}
          //提交数据给后端
          this.saveSubmitFormData(btnType, submitForm)
          debugger
          //只有执行分配任务启动的时候，保存后，才跳入到任务详情
          if (btnType === 'drawerGroupStart') {
            //切换到对应的tab
            this.tabsActiveName = 'third'
            //跳转到任务详情
            this.handleTaskView('task', this.groupTaskListLastNew)
          }
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
      this.clearHistoryTimer()
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
    },

    // 筛选输入变化
    handleFilterChange(value, filterType) {
      console.log('handleFilterChange', filterType, value)
      if (filterType === 'agent') {

        this.agentQueryList.filter = value
        this.getAgentListData('agent', 1)
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
    handleAgentViewAll() {
      this.clearHistoryTimer()
      this.scrollbarAgentIndex = '' // 清空索引
      this.agentDetails = '' // 清空详情数据
      this.getAgentListData('agent')
    },
    // 查看 智能体
    handleAgentView(item, index) {
      this.clearHistoryTimer()
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
    handleGroupViewAll() {
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
    },
    // 组 查看列表 详情 
    handleGroupView(clickType, item, index = 0) {
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
      let serviceURL = ''
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
      let serviceURL = ''
      console.log('handleGroupDeleteBatch:', this.groupSelection);
      if (!serviceURL) return
      // 操作确认 提示
      this.$confirm('确认批量删除数据吗？', '操作确认', {
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
    handleTaskViewAll() {
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
    },
    // 查看 任务详情
    handleTaskView(clickType, item = {}, index = 0) {
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
      }
    },
    // 返回组详情页面
    returnGroup(clickType) {
      this.clearHistoryTimer()
      if (clickType === 'agentGroupTask') {
        this.agentDetailsGroupShowType = '1'
        // const item = this.agentDetailsGroupList[this.agentDetailsGroupIndex]
        // this.getGroupDetailData('agentGroup', item.id,this.agentDetailsGroupDetails)

      }
      if (clickType === 'groupTask') {
        this.groupShowType = '2' // 组件详情
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
    viewAgentParameters(optype, item) {
      // 对接接口获取数据 this.agentParameterList
      if (optype === 'task') {
        // 获取 任务成员列表
        // this.getTaskMemberListData()
      }
      if (optype === 'taskDetail') {
        // taskMemberList
        this.agentParameterList = this.taskMemberList ?? []
      }
      if (optype === 'agentTask') {
        this.agentParameterList = this.agentDetailsTaskMemberList ?? []
      }
      if (optype === 'agentGroupTaskAdmin') {
        let agentDtoList = this.agentDetailsGroupDetails?.agentTemplateDtoList ?? []
        let adminAgentId = this.agentDetailsGroupDetails?.chatGroupDto?.adminAgentTemplateId ?? ''
        let findItem = agentDtoList.find(item => item.id === adminAgentId)
        this.agentParameterList = findItem ? [findItem] : []
      }
      if (optype === 'agentGroupTaskEnter') {
        let agentDtoList = this.agentDetailsGroupDetails?.agentTemplateDtoList ?? []
        let enterAgentId = this.agentDetailsGroupDetails?.chatGroupDto?.enterAgentTemplateId ?? ''
        let findItem = agentDtoList.find(item => item.id === enterAgentId)
        this.agentParameterList = findItem ? [findItem] : []
      }
      if (optype === 'agentGroupTask') {
        this.agentParameterList = this.agentDetailsGroupDetails?.agentTemplateDtoList ?? []
      }
      if (optype === 'groupTaskAdmin') {
        let agentDtoList = this.groupDetails?.agentTemplateDtoList ?? []
        let adminAgentId = this.groupDetails?.chatGroupDto?.adminAgentTemplateId ?? ''
        let findItem = agentDtoList.find(item => item.id === adminAgentId)
        this.agentParameterList = findItem ? [findItem] : []
      }
      if (optype === 'groupTaskEnter') {
        let agentDtoList = this.groupDetails?.agentTemplateDtoList ?? []
        let enterAgentId = this.groupDetails?.chatGroupDto?.enterAgentTemplateId ?? ''
        let findItem = agentDtoList.find(item => item.id === enterAgentId)
        this.agentParameterList = findItem ? [findItem] : []
      }
      if (optype === 'groupTask') {
        this.agentParameterList = this.groupDetails?.agentTemplateDtoList ?? []
      }
      this.visible.dialogAgentParameter = true
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
      let serviceURL = ''
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
      this.$confirm('确认批量删除数据吗？', '操作确认', {
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
        // 靶场:targetrangeId   靶道:targetlaneId
        let targetrangeId = item?.promptRange.id ?? ''
        let targetlaneId = item?.id ?? ''
        url = `/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18&targetrangeId=${targetrangeId}&targetlaneId=${targetlaneId}`
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
      const currentNames = this.agentForm.functionCallNames.split(',').filter(x => x);
      const index = currentNames.indexOf(tag);
      if (index > -1) {
        currentNames.splice(index, 1);
        this.agentForm.functionCallNames = currentNames.join(',');
      }
      this.functionCallTags = currentNames;
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
    },
    
    // 确认添加 Endpoint
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
      
      // 添加新的 Endpoint
      endpoints[name] = { url };
      this.agentForm.mcpEndpoints = JSON.stringify(endpoints);
      
      // 清空输入框
      this.mcpEndpointInputVisible = false;
      this.mcpEndpointNameValue = '';
      this.mcpEndpointUrlValue = '';
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
        url = `/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18`
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

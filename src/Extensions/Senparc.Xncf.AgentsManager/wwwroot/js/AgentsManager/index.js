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
      elSize: 'medium', // el component size defaults to empty medium, small, mini
      tabsActiveName: 'first', // first(agent) second(group) third(task)
      // visible visible
      visible: {
        drawerAgent: false, // Agent New|Edit
        dialogGroupAgent: false, // Intelligent agent adds dialog
        drawerGroup: false, // Group New|Edit
        drawerGroupStart: false, // group start 
        dialogAgentParameter: false, // Agent parameter list
        dialogTaskDescription: false, // Task description
        dialogTaskEvaluation: false, // Task evaluation page
        dialogMcpTools: false, // MCP Tool List Dialog
      },
      taskStateText: {
        0: '等待',  // Waiting stand #3376cd
        1: '聊天', // Chat Chatting loading #409EFF
        2: '停顿', // Paused loading #409EFF
        3: '完成', // Finished Finished success #67C23A
        4: '取消', // Cancel Canceled error #666 
        5: '失败', // Cancel fail error #F56C6C
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
        1: 'fas fa-spinner fa-pulse',// animation
        2: 'fas fa-play-circle',
        3: 'fas fa-check-circle', // el-icon-success
        4: 'fal fa-minus-circle fa-rotate-45', // rotate
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
      // Agent ---start
      agentQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // Filter text
        timeSort: false, // Default descending order
        proce: false, // in progress
        stop: false, // deactivate
        stand: false, // Standby
      },
      agentFCPVisible: false, // Filter condition popover show and hide
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
      fillCardNum: 0, // In order to maintain the style of the last row, the number of cards filled in
      agentListElResizeObserver: null,
      scrollbarAgentIndex: '', // Side agent index defaults to all
      agentDetails: '', // View agent details data
      // Agent details tabs
      agentDetailsTabsActiveName: 'first', // first(group) second(task)
      // Agent Details Group
      agentDetailsGroupQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // Filter text
        timeSort: false, // Default descending order
        proce: false, // in progress
        stop: false, // deactivate
        stand: false, // Standby
      },
      agentDetailsGroupList: [],
      agentDetailsGroupShowType: '1', // 1:Group details 2:Task details
      agentDetailsGroupIndex: 0, // Side group index defaults to all
      agentDetailsGroupDetails: '',
      agentDetailsGroupTaskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // Filter text
        timeSort: false, // Default descending order
        proce: false, // in progress
        stop: false, // deactivate
        stand: false, // Standby
      },
      agentGroupTaskSelection: [], // Selected task list
      agentDetailsGroupTaskList: [], // Group task list
      agentDetailsGroupTaskHistoryList: [],
      agentDetailsGroupDetailsTaskDetails: '',
      agentGroupTaskMemberfilter: '',
      agentGroupTaskMemberfilterList: [],
      // Agent Details Task
      agentDetailsTaskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // Filter text
        timeSort: false, // Default descending order
        proce: false, // in progress
        stop: false, // deactivate
        stand: false, // Standby
      },
      agentDetailsTaskIndex: 0, // Side task index defaults to all
      agentDetailsTaskList: [],
      agentDetailsTaskDetails: '',
      agentDetailsTaskHistoryList: [],
      agentDetailsTaskMemberList: [],
      agentTaskMemberfilter: '',
      agentTaskMemberfilterList: [],
      // Agent ---end
      // Group ---start
      groupQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // Filter text
        timeSort: false, // Default descending order
        proce: false, // in progress
        stop: false, // deactivate
        stand: false, // Standby
      },
      groupFCPVisible: false, // Filter condition popover show and hide
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
      groupShowType: '1', // 1: Group list 2: Group details 3: Task details
      scrollbarGroupIndex: '', // Side task index defaults to all
      groupDetails: '',
      groupTaskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // Filter text
        timeSort: false, // Default descending order
        proce: false, // in progress
        stop: false, // deactivate
        stand: false, // Standby
      },
      groupTaskSelection: [],
      groupTaskList: [],
      groupTaskListLastNew: [],
      groupTaskDetails: '',
      groupTaskHistoryList: [],
      groupTaskMemberfilter: '',
      groupTaskMemberfilterList: [],
      // Group New|Edit Agent
      groupAgentQueryList: {
        pageIndex: 0,
        pageSize: 0,
        filter: '', // Filter text
        timeSort: false, // Default descending order
        proce: false, // in progress
        stop: false, // deactivate
        stand: false, // Standby
      },
      isGetGroupAgent: false,
      groupAgentList: [], // Agent list when the group is added
      groupAgentTotal: 0,
      // Group ---end
      // Task task ---start
      taskQueryList: {
        pageIndex: 0,
        pageSize: 0,
        chatGroupId: null,
        filter: '', // Filter text
        timeSort: false, // Default descending order
        proce: false, // in progress
        stop: false, // deactivate
        stand: false, // Standby
      },
      taskFCPVisible: false, // Task module filter condition popover show and hide
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
      scrollbarTaskIndex: '', // Side task index defaults to all
      taskSelection: [],
      taskList: [],
      taskDetails: '', // View task details data
      taskHistoryList: [],
      taskMemberList: [],
      taskMemberfilter: '',
      taskMemberfilterList: [],
      // Task task ---end
      // Agent New|Edit
      agentForm: {
        id: 0, // 0 is new 
        name: '', // name
        systemMessageType: '1',
        systemMessage: '', // 
        enable: true, // Whether to enable
        description: '', // illustrate
        hookRobotType: 0, // External platform
        hookRobotParameter: '', // External parameters
        avastar: '/images/AgentsManager/avatar/avatar1.png', // avatar
        functionCallNames: '', // Function Call names, comma separated
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
        //     { required: true, message: 'Please fill in', trigger: 'blur' },
        // ],
        hookRobotType: [
          { required: true, message: '请选择', trigger: 'change' },
        ],
        // hookRobotParameter: [
        //     { required: true, message: 'Please fill in', trigger: 'blur' },
        // ],
        avastar: [
          { required: true, message: '请选择', trigger: 'change' },
        ],
        functionCallNames: [
          { required: false, message: '请输入Function Call名称', trigger: 'change' }
        ]
      },
      // Group New|Edit
      groupForm: {
        name: '', // name
        members: [], // Member list
        description: '', // illustrate
        adminAgentTemplateId: '', // The group owner is the agent
        enterAgentTemplateId: '' // The docking person is the agent
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
        //     { required: true, message: 'Please fill in', trigger: 'blur' },
        // ],
      },
      // group start
      groupStartForm: {
        groupName: '', // Group name
        chatGroupId: '', // group id
        name: '', // title
        aiModelId: '', // model id
        promptCommand: '', // Task description
        personality: true, // Whether to use personalization
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
      // task evaluation
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
      // Conversation Record Polling
      historyTimer: {},
      // Agent parameter list
      agentParameterTabsValue: 0, // tabs selected
      agentParameterList: [],
      // Description content
      describeContent: '',
      functionCallInputVisible: false,
      functionCallInputValue: '',
      functionCallTags: [], // Used to temporarily store tags while editing
      pluginTypes: [], // Stores all available plugin types
      // MCP Endpoints related
      mcpEndpointInputVisible: false,
      mcpEndpointNameValue: '',
      mcpEndpointUrlValue: '',
      mcpEndpointEditMode: false,
      mcpEndpointOriginalName: '',
      currentMcpTools: [], // List of currently viewed MCP tools
    };
  },
  computed: {
    // Count unselected plugin types
    availablePluginTypes() {
      if (!this.agentForm.functionCallNames) {
        return this.pluginTypes;
      }
      // Convert comma separated string to array for comparison
      const currentNames = this.agentForm.functionCallNames.split(',').filter(x => x);
      return this.pluginTypes.filter(type =>
        !currentNames.includes(type)
      );
    },
    // Parse McpEndpoints JSON string
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
    // Get list of plugin types when component is created
    this.getPluginTypes();
  },
  mounted() {
    this.tabsActiveName = "first";
    this.agentForm.systemMessageType = "2";
    this.getPluginTypes();
    
    // Debug dialog related variables
    console.log('Vue实例挂载完成');
    console.log('visible对象:', this.visible);
    console.log('dialogMcpTools初始状态:', this.visible.dialogMcpTools);
    
    // Test dialog box displays
    window.testDialog = () => {
      console.log('测试显示对话框');
      this.visible.dialogMcpTools = true;
      console.log('dialogMcpTools新状态:', this.visible.dialogMcpTools);
    };
    
    // Test creating fake tool list
    window.testTools = () => {
      console.log('测试创建工具列表');
      this.currentMcpTools = [
        { name: '测试工具1', description: '这是一个测试工具', parameters: [{ name: 'param1', description: '参数1' }] },
        { name: '测试工具2', description: '这是另一个测试工具', parameters: [] }
      ];
      console.log('currentMcpTools:', this.currentMcpTools);
      this.visible.dialogMcpTools = true;
    };
    
    // agent
    if (this.tabsActiveName === 'first') {
      this.getAgentListData('agent')
    }
    // Group
    if (this.tabsActiveName === 'second') {
      this.getGroupListData('group')
    }
    // Task
    if (this.tabsActiveName === 'third') {
      this.gettaskListData('task')
    }

  },
  beforeDestroy() {
    this.clearHistoryTimer()
  },
  methods: {
    //Find target string
    findDest(arg1) {
      // The string to be judged
      //const str = '2025.05.07.1-T1-A1-Draft';
      const str = arg1;

      // Regular expression: Match the structure of XXXX.XX.XX.X (X is a number)
      const regex = /^\d{4}\.\d{2}\.\d{2}\.\d+/;

      // Determine whether a string conforms to the rules
      if (regex.test(str)) {
        console.log('目标字符串');
        return true;
      } else {
        console.log('非目标字符串');
        return false;
      }
    },
    calculateDuration,
    // Calculate the number of elements that need to be filled in the agent list
    calcAgentFillNum() {
      // if (this.tabsActiveName === 'first' && this.scrollbarAgentIndex === '') {
      // }
      if (!this.agentListElResizeObserver) {
        // Calculate the number of elements that need to be filled in the agent list
        this.agentListElResizeObserver = new ResizeObserver(entries => {
          const elWidth = entries[0]?.contentRect?.width ?? 0
          const singleElWidth = 315
          const elSpac = 30
          const num = this.agentList.length
          // Single element minimum width 315
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
    // Get status text
    getStatusText(item, showType) {
      // this.taskStateText this.agentStateText
      let statusText = ''
      // agent
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
      // Group
      if (showType === '2') {
        let detailData = item.chatGroupDto || item
        statusText = detailData.enable ? '待命' : '停用'
        let resultText = ''
        if (detailData.enable) {
          resultText = this.taskStateText[item.state]
        }
        return resultText || statusText
      }
      // Task
      if (showType === '3') {
        statusText = this.taskStateText[item.status]
        return statusText
      }
      return ''
    },
    // Get status color
    getStatusColor(item, showType) {
      // this.taskStateColor this.agentStateColor
      let statusColor = ''
      // Agent list
      if (showType === '1') {
        let detailData = item.agentTemplateDto || item
        statusColor = detailData.enable ? 'standColor' : 'stopColor'
        let resultColor = ''
        if (detailData.enable) {
          resultColor = this.taskStateColor[item.status]
        }
        return resultColor || statusColor
      }
      // Group
      if (showType === '2') {
        let detailData = item.chatGroupDto || item
        statusColor = detailData.enable ? 'standColor' : 'stopColor'
        let resultColor = ''
        if (detailData.enable) {
          resultColor = this.taskStateColor[item.status]
        }
        return resultColor || statusColor
      }
      // Task 
      if (showType === '3') {
        statusColor = this.taskStateColor[item.status]
        return statusColor
      }
    },
    // Get agent data
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
      // Interface docking
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetList?${getInterfaceQueryStr(queryList)}`)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const agentData = data?.data?.list ?? []
            if (listType === 'agent') {
              this.$set(this, 'agentList', agentData)
              const agentDetail = this.agentDetails?.agentTemplateDto ?? {}
              // Get details 
              if (agentDetail.id) {
                this.getAgentDetailData(agentDetail.id, agentDetail)
              }
              // Calculate the number of elements that need to be filled in the agent list
              this.calcAgentFillNum()
            }
            if (listType === 'groupAgent') {
              this.$set(this, 'groupAgentList', agentData)
              // Make sure the selection is not cleared when updating data
              this.$nextTick(() => {
                this.isGetGroupAgent = false
              })
              // Group members table initial selection
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
    // Get agent details 
    async getAgentDetailData(id, detail = {}) {
      let taskList = []
      let groupList = []
      if (this.tabsActiveName === 'first') {
        const groupQuery = {
          pageIndex: 0,
          pageSize: 0,
          agentTemplateId: id
        }
        // Get list of groups
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
        //  Get task list
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
    // Get group data
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
      // Get agent list
      let agentAllList = []
      await serviceAM.get('/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetList')
        .then(res => {
          // debugger
          const data = res?.data ?? {}
          if (data.success) {
            agentAllList = data?.data?.list ?? []
          }
        })
      // Get task list
      let taskAllList = []
      await serviceAM.get('/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetList')
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            taskAllList = data?.data?.chatTaskList ?? []
            //Set the latest task information
            this.groupTaskListLastNew = taskAllList[0]
          }
        })
      // Get list of groups
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
              //     name: 'All groups',
              //     children: groupData
              // }]
              this.groupSelection = [] // Clear selection
              this.$set(this, 'groupList', handleGroupData)
              const groupDetail = this.groupDetails?.chatGroupDto ?? {}
              // Get details 
              if (this.groupShowType === '2' && groupDetail.id) {
                this.getGroupDetailData(listType, groupDetail.id, groupDetail)
              }
            }
            if (listType === 'agentGroup') {
              this.$set(this, 'agentDetailsGroupList', handleGroupData)
              const groupDetail = handleGroupData[this.agentDetailsGroupIndex]
              // Get details
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
    // Get group details 
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
              // Get task list
              this.gettaskListData('agentGroupTask', id)
            }
            if (['group', 'groupTable'].includes(detailType)) {
              this.$set(this, 'groupDetails', groupDetail)
              // Get task list
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
    // Get task data
    async gettaskListData(listType, id, page = 0) {
      const queryList = {}
      // Task
      if (listType === 'task') {
        this.taskQueryList.pageIndex = page ?? 1
        Object.assign(queryList, this.taskQueryList)
      }
      // agent task
      if (listType === 'agentTask') {
        this.agentDetailsTaskQueryList.pageIndex = page ?? 1
        this.agentDetailsTaskQueryList.agentTemplateId = id
        Object.assign(queryList, this.agentDetailsTaskQueryList)
      }
      // Agent group task
      if (listType === 'agentGroupTask') {
        this.agentDetailsGroupTaskQueryList.pageIndex = page ?? 1
        this.agentDetailsGroupTaskQueryList.chatGroupId = id
        Object.assign(queryList, this.agentDetailsGroupTaskQueryList)
      }
      // group task
      if (listType === 'groupTask') {
        this.groupTaskQueryList.pageIndex = page ?? 1
        this.groupTaskQueryList.chatGroupId = id
        Object.assign(queryList, this.groupTaskQueryList)
      }
      let modelList = []
      // Get model list
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
      //  Interface docking
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
            // Task
            if (listType === 'task') {
              this.$set(this, 'taskList', handleTaskData)
              // Display the first task details by default
              if (handleTaskData && handleTaskData.length) {
                const taskDetail = this.taskDetails ? this.taskDetails : handleTaskData[0]
                this.getTaskDetailData(listType, taskDetail.id, taskDetail)
              }
            }
            // agent task
            if (listType === 'agentTask') {
              this.$set(this, 'agentDetailsTaskList', handleTaskData)
              // Display the first task details by default
              if (handleTaskData && handleTaskData.length) {
                const taskDetail = this.agentDetailsTaskDetails ? this.agentDetailsTaskDetails : handleTaskData[0]
                this.getTaskDetailData(listType, taskDetail.id, taskDetail)
              }
            }
            // Agent group task
            if (listType === 'agentGroupTask') {
              this.$set(this, 'agentDetailsGroupTaskList', handleTaskData)
            }
            // group task
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
    // Get task details 
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
            // Not Details Only Clear Polling
            if (!detailsOn) {
              this.clearHistoryTimer()
            }

            let taskDetail = data?.data?.chatTaskDto ?? ''
            if (taskDetail) {
              taskDetail = Object.assign({}, detail, taskDetail)
            }
            // Agent group task
            if (detailType === 'agentGroupTask') {
              this.$set(this, 'agentDetailsGroupDetailsTaskDetails', taskDetail)
            }
            // group task
            if (detailType === 'groupTask') {
              this.$set(this, 'groupTaskDetails', taskDetail)
            }
            // agent task
            if (detailType === 'agentTask') {
              this.$set(this, 'agentDetailsTaskDetails', taskDetail)
            }
            // Task
            if (detailType === 'task') {
              this.$set(this, 'taskDetails', taskDetail)
            }

            if (!detailsOn && taskDetail) {
              if (['task', 'agentTask'].includes(detailType)) {
                // Get task member list
                this.getTaskMemberListData(detailType, taskDetail.chatGroupId)
              }
              // Polling to get conversation data
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
    // Get task history
    async getTaskRecordListData(recordType, chatTaskId, nextHistoryId, isFirst = false) {
      const queryList = {
        chatTaskId,
        nextHistoryId
      }
      //  Interface docking
      await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatGroupHistoryAppService/Xncf.AgentsManager_ChatGroupHistoryAppService.GetList?${getInterfaceQueryStr(queryList)}`, queryList)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const chatGroupHistories = data?.data?.chatGroupHistories ?? []
            const historiesData = chatGroupHistories.map(item => {
              //Use MarkDown format to display the output results
              item.messageHtml = marked.parse(item.message);
              return item
            })
            // Task
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
              // Scroll area adsorption bottom
              this.$nextTick(() => {
                this.scrollbarDown('taskHistoryScrollbar', true, isFirst)
              })
            }
            // agent task
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
              // Scroll area adsorption bottom
              this.$nextTick(() => {
                this.scrollbarDown('agentDetailsTaskHistoryScrollbar', true, isFirst)
              })
            }
            // Agent group task
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
              // Scroll area adsorption bottom
              this.$nextTick(() => {
                this.scrollbarDown('agentDetailsGroupTaskHistoryScrollbar', true, isFirst)
              })
            }
            // group task
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
              // Scroll area adsorption bottom
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
    // Get task member list
    async getTaskMemberListData(memberType, chatGroupld) {
      await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${chatGroupld}`)
        .then(res => {
          const data = res?.data ?? {}
          if (data.success) {
            const taskMemberList = data?.data?.agentTemplateDtoList ?? []
            // Task
            if (memberType === 'task') {
              this.$set(this, 'taskMemberList', taskMemberList)
            }
            // agent task
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
    // Save submitForm data
    async saveSubmitFormData(saveType, serviceForm = {}) {
      //debugger
      let serviceURL = ''
      // agent new|edit
      if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
        // Make sure serviceForm is the correct object
        serviceForm = serviceForm || {};

        // Directly convert the functionCallTags array to a string and assign it
        serviceForm.functionCallNames = this.functionCallTags.length > 0 ? this.functionCallTags.join(',') : '';

        // Print logs for debugging
        console.log('Submitting serviceForm:', serviceForm);
        console.log('functionCallTags:', this.functionCallTags);
        console.log('functionCallNames:', serviceForm.functionCallNames);

        serviceURL = '/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.SetItem'
        if (saveType === 'dialogGroupAgent') {
          this.isGetGroupAgent = true
        }
      }
      // Group New|Edit
      if (saveType === 'drawerGroup') {
        serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.SetChatGroup'
        if (serviceForm.members) {
          const membersIds = serviceForm.members.map(item => item.id)
          serviceForm.memberAgentTemplateIds = membersIds
          serviceURL += `?${getInterfaceQueryStr({ memberAgentTemplateIds: membersIds })}`
        }
      }
      // Group start (run task) ['drawerGroupStart', 'drawerTaskStart'].includes(btnType)
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
          // agent
          if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
            refName = 'agentELForm'
            formName = 'agentForm'
          }
          // Group
          if (saveType === 'drawerGroup') {
            refName = 'groupELForm'
            formName = 'groupForm'
            // Reset group acquisition agent query
            this.$set(this, 'groupAgentQueryList', this.$options.data().groupAgentQueryList)
          }
          // group start
          if (['drawerGroupStart', 'drawerTaskStart'].includes(saveType)) {
            refName = 'groupStartELForm'
            formName = 'groupStartForm'
          }
          // task evaluation
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
          // Retrieve data
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
            // Retrieve task details 
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
    // Polling to obtain task history conversation records
    pollGetTaskHistoryData(listType, fun, id) {
      if (!listType || !fun) return
      fun(listType, id, '', true)
      const interval = () => {
        if (this.historyTimer[listType]) clearTimeout(this.historyTimer[listType])
        this.historyTimer[listType] = setTimeout(() => {
          let nextHistoryId = ''
          // Task
          if (listType === 'task') {
            let lenIndex = this.taskHistoryList.length - 1
            nextHistoryId = this.taskHistoryList[lenIndex]?.id ?? ''
          }
          // agent task
          if (listType === 'agentTask') {
            let lenIndex = this.agentDetailsTaskHistoryList.length - 1
            nextHistoryId = this.agentDetailsTaskHistoryList[lenIndex]?.id ?? ''
          }
          // Agent group task
          if (listType === 'agentGroupTask') {
            let lenIndex = this.agentDetailsGroupTaskHistoryList.length - 1
            nextHistoryId = this.agentDetailsGroupTaskHistoryList[lenIndex]?.id ?? ''
          }
          // group task
          if (listType === 'groupTask') {
            let lenIndex = this.groupTaskHistoryList.length - 1
            nextHistoryId = this.groupTaskHistoryList[lenIndex]?.id ?? ''
          }
          // Execute code block
          fun(listType, id, nextHistoryId)
          interval()
        }, 1000 * 5)
        // console.log('pollGetTaskHistoryData', this.historyTimer[listType]);
      }
      interval()
    },
    // Clear polling for historical conversation records
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

    // Edit Dailog|Drawer Button 
    async handleEditDrawerOpenBtn(btnType, item) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      //console.log('handleEditDrawerOpenBtn', btnType, item);
      let formName = ''
      // agent
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        formName = 'agentForm'
      }
      // Group
      if (btnType === 'drawerGroup') {
        formName = 'groupForm'
      }
      // group start 
      if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) {
        formName = 'groupStartForm'
      }
      // Task Evaluation
      if (btnType === 'dialogTaskEvaluation') {
        formName = 'evaluationForm'
      }
      if (formName) {
        if (btnType === 'drawerAgent' && item) {
          console.log('item', item);
          // Create a new object to store form data
          const formData = item.agentTemplateDto ? { ...item.agentTemplateDto } : { ...item };
          console.log('formData', formData);

          // Make sure functionCallNames is initialized correctly
          this.functionCallTags = formData.functionCallNames ? formData.functionCallNames.split(',').filter(Boolean) : [];

          // Assign data to the form
          Object.assign(this[formName], formData);

          // Print logs for debugging
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
          // // Get all agent data
          // this.getAgentListData('groupAgent')
        } else if (btnType === 'drawerTaskStart') {
          Object.assign(this[formName], {
            ...item
            // groupName: item?.name ?? ''
          })
        } else {
          Object.assign(this[formName], item)
        }
        // echo form values
        // this.$set(this, `${formName}`, deepClone(item))
        // open drawer
        this.handleElVisibleOpenBtn(btnType)
      }
    },
    // Dailog|Drawer Open button
    handleElVisibleOpenBtn(btnType, formData) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      // console.log('Universal new button:', btnType);
      let visibleKey = btnType
      // group start 
      if (btnType === 'drawerGroupStart') {
        // Details: formData.chatGroupDto List: formData
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
    // Dailog|Drawer Close Button
    handleElVisibleClose(btnType) {
      if (btnType === 'dialogAgentParameter') {
        // Clear data
        this.agentParameterList = []
        this.$nextTick(() => {
          this.visible[btnType] = false
        })
        return
      } else if (btnType === 'dialogTaskDescription') {
        // Clear data
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
          // agent
          if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
            refName = 'agentELForm'
            formName = 'agentForm'
          }
          // Group
          if (btnType === 'drawerGroup') {
            refName = 'groupELForm'
            formName = 'groupForm'
            // Reset group acquisition agent query
            this.$set(this, 'groupAgentQueryList', this.$options.data().groupAgentQueryList)
            this.groupAgentList = []
          }
          // group start
          if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) {
            refName = 'groupStartELForm'
            formName = 'groupStartForm'
          }
          // task evaluation
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
          // Clean up Function Calls data
          if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
            this.functionCallTags = []
            this.functionCallInputVisible = false
            this.functionCallInputValue = ''
          }
        })
        .catch(_ => { });
    },
    // Dailog|Drawer Submit Button
    handleElVisibleSubmit(btnType) {
      // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
      let refName = '', formName = ''
      // agent 
      if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
        refName = 'agentELForm'
        formName = 'agentForm'
      }
      // Group
      if (btnType === 'drawerGroup') {
        refName = 'groupELForm'
        formName = 'groupForm'
      }
      // group start
      if (['drawerGroupStart', 'drawerTaskStart'].includes(btnType)) {
        refName = 'groupStartELForm'
        formName = 'groupStartForm'
      }
      // task evaluation
      if (btnType === 'dialogTaskEvaluation') {
        refName = 'evaluationELForm'
        formName = 'evaluationForm'
      }
      if (!refName) return
      this.$refs[refName].validate((valid) => {
        if (valid) {
          const submitForm = this[formName] ?? {}
          //Submit data to the backend
          this.saveSubmitFormData(btnType, submitForm)
          debugger
          //Only when the assigned task is started, and after saving, will it jump to the task details.
          if (btnType === 'drawerGroupStart') {
            //Switch to the corresponding tab
            this.tabsActiveName = 'third'
            //Jump to task details
            this.handleTaskView('task', this.groupTaskListLastNew)
          }
          // this.visible[btnType] = false
        } else {
          console.log('error submit!!');
          return false;
        }
      });
    },
    // Form single check
    handleFormValidateField(refFormEL, formName, propName, item) {
      // this[formName][propName] = item
      this.$set(this[formName], `${propName}`, item)
      this.$refs[refFormEL]?.validateField(propName, () => { })
    },

    // Identify events
    handleIdentify(e) {

      //debugger
      let bRes = this.findDest(e)
      if (bRes) {
        console.log('命中')
        //Automatically select PromptRange (no processing)

      } else {
        console.log('未命中')
        //TODO: becomes the new prompt word by default, zai

      }
      console.log('识别事件', e);
    },

    // Switch tabs page
    handleTabsClick(tab, event) {
      this.clearHistoryTimer()
      // agent
      if (this.tabsActiveName === 'first') {
        this.getAgentListData('agent')
      }
      // Group
      if (this.tabsActiveName === 'second') {
        this.getGroupListData('group')
      }
      // Task
      if (this.tabsActiveName === 'third') {
        this.gettaskListData('task')
      }
    },

    // Filter input changes
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
    // Filter event agent group task
    handleFilterCriteria(filterType, fieldType) {
      // agent
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
        // to do call interface
      }
      // Group
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
        // to do call interface
      }
      // Task
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
        // to do call interface
      }
    },



    // View all agents list 
    handleAgentViewAll() {
      this.clearHistoryTimer()
      this.scrollbarAgentIndex = '' // Clear index
      this.agentDetails = '' // Clear detailed data
      this.getAgentListData('agent')
    },
    // View Agent
    handleAgentView(item, index) {
      this.clearHistoryTimer()
      this.scrollbarAgentIndex = item.id ?? ''
      // Reset data
      this.resetAgentDetailsQuery()
      // Get agent details
      this.getAgentDetailData(item.id, item)
      // Reset group acquisition agent query
      if (this.agentDetailsTabsActiveName === 'first') {
        this.getGroupListData('agentGroup', item.id)
      }
      if (this.agentDetailsTabsActiveName === 'second') {
        this.gettaskListData('agentTask', item.id)
      }
    },
    // Reset group and task data under Agent Details
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
    // Switch the agent details tabs page
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
    // Agent state switching
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
        dangerouslyUseHTMLString: true, // message is treated as an HTML fragment
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



    // side group tree component node filter
    filterGroupTreeNode(value, data) {
      if (!value) return true;
      return data.name.indexOf(value) !== -1;
    },
    // Side group tree component node click
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
          this.groupDetails = '' // Clear group details
        }
        if (this.groupShowType === '2') {
          this.groupDetails = deepClone(data)
        }
        if (this.groupShowType === '3') {
          this.groupDetails = deepClone(data)
        }
      }
    },
    // Group View details
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
    // Group View All List 
    handleGroupViewAll() {
      this.clearHistoryTimer()
      this.groupShowType = '1'
      // Clear group details
      this.scrollbarGroupIndex = '' // Clear index
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
    // Group View List Details 
    handleGroupView(clickType, item, index = 0) {
      this.clearHistoryTimer()
      // View group details when the agent is down
      if (clickType === 'agentGroup') {
        // Switch display type
        this.agentDetailsGroupShowType = '1'
        this.agentDetailsGroupIndex = index ?? 0
        // Clear group details
        this.agentDetailsGroupDetails = ''
        this.agentGroupTaskSelection = []
        this.agentDetailsGroupTaskList = []
        this.agentDetailsGroupDetailsTaskDetails = ''
        this.agentDetailsGroupTaskHistoryList = []
        this.agentGroupTaskMemberfilter = ''
        this.agentGroupTaskMemberfilterList = []
        this.getGroupDetailData(clickType, item.id, item)
      }
      // View group details when grouping into major categories
      if (clickType === 'group' || clickType === 'groupTable') {
        // Switch display type
        this.groupShowType = '2'
        // if (clickType === 'groupTable') {
        //     const { pageIndex, pageSize } = this.groupQueryList
        //     this.scrollbarGroupIndex = pageIndex > 1 ? pageIndex * pageSize + index : index
        // } else {
        //     this.scrollbarGroupIndex = index ?? 0
        // }
        this.scrollbarGroupIndex = item.id ?? ''
        // Clear group details
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
    // Group Add | Edit Agent table Switch table Select
    toggleSelection(rows) {
      if (rows) {
        rows.forEach(row => {
          this.$refs?.groupAgentTable?.toggleRowSelection(row);
        });
      } else {
        this.$refs?.groupAgentTable?.clearSelection();
      }
    },
    // Group New|Edit Agent table Select changes
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
    // Group New|Edit Agent Members Unchecked
    groupMembersCancel(item, index) {
      this.groupForm.members.splice(index, 1);
      const findIndex = this.groupAgentList.findIndex(i => item.id === i.id)
      if (findIndex !== -1) {
        this.toggleSelection([this.groupAgentList[findIndex]])
      }
    },
    // Group list selection changes (batch deletion)
    handleGroupSelectionChange(val) {
      this.groupSelection = val
    },
    // group delete
    handleGroupDelete(optype, row) {
      console.log('handleGroupDelete:', row);
      let serviceURL = ''
      if (!serviceURL) return
      // Operation confirmation prompt
      this.$confirm('确认删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message is treated as an HTML fragment
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
              // Retrieve data
              this.getGroupListData('group')
            } else {
              // View all groups
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
    // Group batch deletion
    handleGroupDeleteBatch() {
      let serviceURL = ''
      console.log('handleGroupDeleteBatch:', this.groupSelection);
      if (!serviceURL) return
      // Operation confirmation prompt
      this.$confirm('确认批量删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message is treated as an HTML fragment
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
            // Retrieve data
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


    // Tasks View All List 
    handleTaskViewAll() {
      this.clearHistoryTimer()
      this.scrollbarTaskIndex = ''
      // Clear detailed data
      this.taskDetails = ''
      this.taskSelection = []
      this.taskHistoryList = []
      this.taskMemberList = []
      this.taskMemberfilter = ''
      this.taskMemberfilterList = []
      this.gettaskListData('task')
    },
    // View task details
    handleTaskView(clickType, item = {}, index = 0) {
      this.clearHistoryTimer()
      if (clickType === 'agentTask') {
        this.agentDetailsTaskIndex = index ?? ''
        // Clear detailed data
        this.agentDetailsTaskDetails = ''
        this.agentDetailsTaskHistoryList = []
        this.agentDetailsTaskMemberList = []
        this.agentTaskMemberfilter = ''
        this.agentTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
      }
      if (clickType === 'agentGroupTask') {
        this.agentDetailsGroupShowType = '2'
        // Clear detailed data
        this.agentDetailsGroupDetailsTaskDetails = ''
        this.agentDetailsGroupTaskHistoryList = []
        this.agentGroupTaskMemberfilter = ''
        this.agentGroupTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
      }
      if (clickType === 'groupTask') {
        this.groupShowType = '3'
        // Clear detailed data
        this.groupTaskDetails = ''
        this.groupTaskHistoryList = []
        this.groupTaskMemberfilter = ''
        this.groupTaskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
      }
      if (clickType === 'task') {
        this.scrollbarTaskIndex = index ?? ''
        // Clear detailed data
        this.taskDetails = ''
        this.taskHistoryList = []
        this.taskMemberList = []
        this.taskMemberfilter = ''
        this.taskMemberfilterList = []
        this.getTaskDetailData(clickType, item.id, item)
      }
    },
    // Return to group details page
    returnGroup(clickType) {
      this.clearHistoryTimer()
      if (clickType === 'agentGroupTask') {
        this.agentDetailsGroupShowType = '1'
        // const item = this.agentDetailsGroupList[this.agentDetailsGroupIndex]
        // this.getGroupDetailData('agentGroup', item.id,this.agentDetailsGroupDetails)

      }
      if (clickType === 'groupTask') {
        this.groupShowType = '2' // Component details
        // const item = this.groupList[this.scrollbarGroupIndex]
        // this.getGroupDetailData('groupTable', item.id,this.groupDetails)
      }
    },
    // Agent-group task list table selection change (batch startup and deletion)
    handleAgentGroupTaskSelectionChange(val) {
      this.agentGroupTaskSelection = val
    },

    // Group task list table selection change (batch startup and deletion)
    handleGroupTaskSelectionChange(val) {
      this.groupTaskSelection = val
    },
    // Task list selection 
    handleTaskSelectionChange(val) {
      this.taskSelection = val
    },
    // View the list of agent parameters
    viewAgentParameters(optype, item) {
      // The docking interface obtains data this.agentParameterList
      if (optype === 'task') {
        // Get task member list
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
    // Execute again (i.e. start again)
    handleTaskAgain(optype, item = {}) {
      let startData = item ?? {}
      // this.groupStartForm.groupName = item.name
      this.handleEditDrawerOpenBtn('drawerTaskStart', startData)
    },
    // Task deletion
    handleTaskDelet(optype, row) {
      console.log('handleTaskDelet:', row);
      let serviceURL = ''
      if (!serviceURL) return
      // Operation confirmation prompt
      this.$confirm('确认删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message is treated as an HTML fragment
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
              // Get task list
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
    // Group-task batch start (task) agentGroupTaskBatch groupTaskBatch
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
      // Operation confirmation prompt
      this.$confirm('确认批量启动数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message is treated as an HTML fragment
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
              // Get task list
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
    // Group-task batch deletion (task) agentGroupTaskBatch groupTaskBatch
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
      // Operation confirmation prompt
      this.$confirm('确认批量删除数据吗？', '操作确认', {
        dangerouslyUseHTMLString: true, // message is treated as an HTML fragment
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
              // Get task list
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
    // View task description
    viewTaskDescription(item) {
      this.describeContent = item?.promptCommand ?? ''
      this.visible.dialogTaskDescription = true
    },
    // Task description copy
    taskDescriptionCopy() {
      // copy text
      this.copyText('4', this.describeContent).then(() => {
        this.handleElVisibleClose('dialogTaskDescription')
      })
    },
    // task evaluation
    taskEvaluation(item) {
      Object.assign(this.evaluationForm, item)
      this.visible.dialogTaskEvaluation = true
    },
    // input numerical type processing
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
    // Filter task member list 
    handleTaskFilterChange(val, listType) {
      if (listType === 'agentGroupTask') {
        // Agent group task
        const chatGroupMembers = this.agentDetailsGroupDetails?.agentTemplateDtoList ?? []
        const filterList = chatGroupMembers.filter(item => item.name.includes(val))
        this.agentGroupTaskMemberfilterList = filterList.map(item => item.id)
      } else if (listType === 'groupTask') {
        // group task
        const chatGroupMembers = this.groupDetails?.agentTemplateDtoList ?? []
        const filterList = chatGroupMembers.filter(item => item.name.includes(val))
        this.groupTaskMemberfilterList = filterList.map(item => item.id)
      } else if (listType === 'agentTask') {
        // agent task
        const filterList = this.agentDetailsTaskMemberList.filter(item => item.name.includes(val))
        this.agentTaskMemberfilterList = filterList.map(item => item.id)
      } else if (listType === 'task') {
        // Task
        const filterList = this.taskMemberList.filter(item => item.name.includes(val))
        this.taskMemberfilterList = filterList.map(item => item.id)
        console.log('handleTaskFilterChange', this.taskMemberfilterList);
      }
    },

    // el-scrollbar bottom scroll to the bottom
    scrollbarDown(refName, istouchBottom = false, isFirst = false) {
      if (!refName) return
      const scrollbar = this.$refs[refName];
      if (!scrollbar) return
      if (istouchBottom) {
        const scrollTop = scrollbar.wrap.scrollTop; // The top of the current scroll
        const scrollHeight = scrollbar.wrap.scrollHeight; // total content height
        const clientHeight = scrollbar.wrap.clientHeight; // Viewing area height
        // scrollTop, scrollHeight, clientHeight
        if (scrollHeight !== clientHeight && (scrollTop + clientHeight + 30 >= scrollHeight || isFirst)) {
          // scroll to bottom
          scrollbar.wrap.scrollTop = scrollbar.wrap.scrollHeight;
        }
      } else {
        // scroll to bottom
        scrollbar.wrap.scrollTop = scrollbar.wrap.scrollHeight;
      }
    },
    // Get sender name
    getTaskSenderInfo(taskType, formId) {
      // Agent group task
      if (taskType === 'agentGroupTask') {
        const chatGroupMembers = this.agentDetailsGroupDetails?.agentTemplateDtoList ?? []
        const fintItem = chatGroupMembers.find(item => item.id === formId)
        return fintItem ?? {}
      }
      // group task
      if (taskType === 'groupTask') {
        const chatGroupMembers = this.groupDetails?.agentTemplateDtoList ?? []
        const fintItem = chatGroupMembers.find(item => item.id === formId)
        return fintItem ?? {}
      }
      // agent task
      if (taskType === 'agentTask') {
        const fintItem = this.agentDetailsTaskMemberList.find(item => item.id === formId)
        return fintItem ?? {}
      }

      // Task
      if (taskType === 'task') {
        const fintItem = this.taskMemberList.find(item => item.id === formId)
        return fintItem ?? {}
      }

      return {}
    },
    jumpPromptRange(urlType, item) {
      let url = ''
      if (urlType === 'promptRange') {
        // Target range: targetrangeId Target lane: targetlaneId
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
          // Display detailed data
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
            dangerouslyUseHTMLString: true, // message is treated as an HTML fragment
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
    // Processing Lanes and Range Display Names
    handlePromptShowName(showType, item) {
      let resultText = ''
      if (showType === '1') {
        // target lane
        const itemData = item?.promptRangeDto ?? ''
        if (itemData) {
          resultText = `${itemData.alias}(${itemData.rangeName})`
        }
      } else if (showType === '2') {
        // range
        const itemData = item?.promptItemDto ?? ''
        if (itemData) {
          const avg = scoreFormatter(itemData.evalAvgScore)
          const max = scoreFormatter(itemData.evalMaxScore)
          resultText = `${itemData.nickName || '未设置'} | ${itemData.fullVersion} | 平均分：${avg} | 最高分：${max} ${itemData.isDraft ? '(草稿)' : ''}`
        }
      }
      return resultText ?? ''
    },
    // Copy task task description
    copyText(opType, item) {
      // Copy results to clipboard
      return new Promise(resolve => {
        try {
          const textarea = document.createElement('textarea');
          textarea.setAttribute('readonly', 'readonly');
          if (opType === '1') {
            // task conversation original content
            textarea.value = item?.message ?? ''
          } else if (opType === '2') {
            // task conversation HTML content
            textarea.value = item?.messageHtml ?? ''
          } else if (opType === '3') {
            // task dialogue promptCommand (task description) content
            textarea.value = item?.promptCommand ?? ''
          } else if (opType === '4') {
            // task task description
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
    // Group member avatar stacking quantity processing
    displayedAvatars(list, limit = 5) {
      if (Array.isArray(list)) {
        return list?.slice(0, limit) ?? [];
      }
      return []
    },
    // Group member avatar stack quantity
    exceededCount(list, limit = 5) {
      if (Array.isArray(list)) {
        return list.length > limit ? list.length - limit : 0;
      }
      return 0
    },
    // Display the new Function Call input box
    showFunctionCallInput() {
      this.functionCallInputVisible = true;
      this.$nextTick(_ => {
        this.$refs.functionCallInput.$refs.input.focus();
      });
    },

    // Handling Function Call input confirmation
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

    // Delete Function Call tag
    handleFunctionCallClose(tag) {
      const currentNames = this.agentForm.functionCallNames.split(',').filter(x => x);
      const index = currentNames.indexOf(tag);
      if (index > -1) {
        currentNames.splice(index, 1);
        this.agentForm.functionCallNames = currentNames.join(',');
      }
      this.functionCallTags = currentNames;
    },
    
    // Test MCP Endpoint connection
    async testMcpEndpoint(name, endpoint) {
      // Set loading status
      this.$set(endpoint, 'testing', true);
      
      try {
        const response = await axios.get('/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.TestMcpConnection', {
          params: {
            endpointName: name,
            endpointUrl: endpoint.url
          }
        });
        
        // Detailed log
        console.log('MCP测试响应数据:', response);
        
        // Determine based on the data structure returned by the actual API
        if (response.data && response.data.success) {
          // Try getting a list of tools from a different location
          let tools = [];
          let status = 200;
          
          // Debug full response
          console.log('API返回数据结构:', JSON.stringify(response.data, null, 2));
          
          // Check possible data structures
          if (response.data.data) {
            console.log('data字段:', response.data.data);
            
            // Structure 1: response.data.data.tools
            if (response.data.data.tools) {
              console.log('从data.tools获取工具列表');
              tools = response.data.data.tools;
              status = response.data.data.status || 200;
            } 
            // Structure 2: response.data.data is directly the tool list
            else if (Array.isArray(response.data.data)) {
              console.log('data直接是工具列表');
              tools = response.data.data;
            }
          }
          
          console.log('提取的工具列表:', tools);
          
          // Make sure the tools list is an array
          if (!Array.isArray(tools)) {
            console.warn('工具列表不是数组，将转换为空数组');
            tools = [];
          }
          
          // Make sure each tool object has the necessary properties
          tools = tools.map(tool => ({
            name: tool.name || '未命名工具', 
            description: tool.description || '无描述',
            parameters: Array.isArray(tool.parameters) ? tool.parameters : []
          }));
          
          console.log('处理后的工具列表:', tools);
          
          // Initialize testResult object
          if (!endpoint.testResult) {
            this.$set(endpoint, 'testResult', {});
          }
          
          // Set result properties
          this.$set(endpoint.testResult, 'success', true);
          this.$set(endpoint.testResult, 'tools', tools);
          this.$set(endpoint.testResult, 'status', status);
          
          console.log('更新后的endpoint对象:', JSON.parse(JSON.stringify(endpoint)));
          
          this.$message.success('连接测试成功');
          
          // If there is a tool list, directly display the pop-up window
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
        // Clear loading status
        this.$set(endpoint, 'testing', false);
      }
    },
    
    // Get a list of plugin types
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

    // Add plugin type to functionCallNames
    handleAddPluginType(pluginType) {
      if (!this.agentForm.functionCallNames) {
        this.agentForm.functionCallNames = pluginType;
        this.functionCallTags = [pluginType];
      } else {
        // Split existing values ​​into array
        const currentNames = this.agentForm.functionCallNames.split(',').filter(x => x);
        if (!currentNames.includes(pluginType)) {
          // Add new value and concatenate with comma
          this.agentForm.functionCallNames = [...currentNames, pluginType].join(',');
          this.functionCallTags = [...currentNames, pluginType];
        }
      }
    },
    // McpEndpoints related methods
    
    // Display the Add Endpoint input box
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
    
    // Edit Endpoint
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
    
    // Cancel adding Endpoint
    cancelMcpEndpointInput() {
      this.mcpEndpointInputVisible = false;
      this.mcpEndpointNameValue = '';
      this.mcpEndpointUrlValue = '';
      this.mcpEndpointEditMode = false;
      this.mcpEndpointOriginalName = '';
    },
    
    // Confirm to add or update Endpoint
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
        // Edit mode: If the name changes, you need to delete the old one and add the new one.
        if (this.mcpEndpointOriginalName !== name) {
          delete endpoints[this.mcpEndpointOriginalName];
        }
      }
      
      // Add/Update Endpoint
      endpoints[name] = { url };
      this.agentForm.mcpEndpoints = JSON.stringify(endpoints);
      
      // Clear input box
      this.mcpEndpointInputVisible = false;
      this.mcpEndpointNameValue = '';
      this.mcpEndpointUrlValue = '';
      this.mcpEndpointEditMode = false;
      this.mcpEndpointOriginalName = '';
    },
    
    // Delete Endpoint
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
      
      // Delete the specified Endpoint
      if (endpoints[name]) {
        delete endpoints[name];
        this.agentForm.mcpEndpoints = Object.keys(endpoints).length > 0 
          ? JSON.stringify(endpoints) 
          : '';
      }
    },
    
    // Display the MCP Tool List dialog box
    showMcpToolsDialog(endpoint) {
      console.log('调用showMcpToolsDialog函数', endpoint);
      
      // Check the endpoint object and its properties
      if (!endpoint) {
        console.error('endpoint参数为空');
        this.$message.warning('endpoint参数为空');
        return;
      }
      
      console.log('endpoint.testResult:', endpoint.testResult);
      
      if (endpoint && endpoint.testResult && endpoint.testResult.tools) {
        // Create a copy of the tool list
        const tools = [...endpoint.testResult.tools];
        console.log('显示工具列表:', tools);
        console.log('工具列表数量:', tools.length);
        
        // Set the current MCP tool list and display the dialog box
        this.currentMcpTools = tools;
        this.visible.dialogMcpTools = true;
        
        console.log('设置currentMcpTools:', this.currentMcpTools);
        console.log('设置dialogMcpTools为可见');
        
        // If the dialog does not appear, try using an alternate popup
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

    // Alternate MCP tool list pop-up window (use alert)
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
 *Throttling and anti-shake
 * @param {Function} func
 * @param {number} wait
 * @param {boolean} immediate
 * @return {*}
 */
function debounce(func, wait, immediate) {
  let timeout, args, context, timestamp, result
  const later = function () {
    // According to the last trigger time interval
    const last = +new Date() - timestamp

    // The time interval between the last time the wrapped function was called last is less than the set time interval wait
    if (last < wait && last > 0) {
      timeout = setTimeout(later, wait - last)
    } else {
      timeout = null
      // If set to immediate===true, there is no need to call here because the starting boundary has already been called.
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
    // If the delay does not exist, reset the delay
    if (!timeout) timeout = setTimeout(later, wait)
    if (callNow) {
      result = func.apply(context, args)
      context = args = null
    }

    return result
  }
}

/**
* clone
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
* Determine whether the value is a number
* @param {*} val variable to be judged
*/
function isNumber(val) {
  // return !isNaN(val) && (typeof val === 'number' || !isNaN(Number(val)))
  return !isNaN(val) && val !== '' && (typeof val === 'number' || !isNaN(Number()))
}

/**
* Determine whether the value is an empty object
* @param {*} val variable to be judged
*/
function isObjEmpty(obj) {
  return Object.keys(obj).length === 0;
}

/**
 * Open window window
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
 * Simulate a tag
 * @param {string} url // original address
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
 * Process the interface query parameter and convert it to string
 * @param {Object} queryObj //Original address
 */
function getInterfaceQueryStr(queryObj) {
  if (!queryObj) return ''
  // Convert object to URL parameter string
  return Object.entries(queryObj)
    .filter(([key, value]) => {
      // Filter out null values
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
 * The date is formatted as yyyy-MM-dd HH:mm:ss
 * @param {date} dateString
 * @param {string} format
 * @returns {string} - formatted time
 */
function formatDate(dateString, format = 'yyyy-MM-dd HH:mm:ss') {
  if (!dateString) return ''
  const dateObject = new Date(dateString);

  const year = dateObject.getFullYear();
  const month = String(dateObject.getMonth() + 1).padStart(2, '0'); // Month starts from 0
  const day = String(dateObject.getDate()).padStart(2, '0');
  const hours = String(dateObject.getHours()).padStart(2, '0');
  const minutes = String(dateObject.getMinutes()).padStart(2, '0');
  const seconds = String(dateObject.getSeconds()).padStart(2, '0');

  // Replace identifiers in format
  return format
    .replace('yyyy', year)
    .replace('MM', month)
    .replace('dd', day)
    .replace('HH', hours)
    .replace('mm', minutes)
    .replace('ss', seconds);
};
/**
 * Calculate duration
 * @param {string} startTime - start time string (ISO format)
 * @param {string} [endTime] - end time string (ISO format), optional
 * @returns {string} - duration string, dynamically displayed according to the difference level
 */
function calculateDuration(startTime, endTime) {
  if (!startTime) return ''
  // Convert start time and end time to Date objects
  const startDate = new Date(startTime);
  const endDate = endTime ? new Date(endTime) : new Date(); // If there is no end time, the current time is used

  // Calculate time difference in milliseconds
  const durationInMillis = endDate - startDate;

  // millisecond value for each time unit
  const secondsInMillis = 1000;
  const minutesInMillis = secondsInMillis * 60;
  const hoursInMillis = minutesInMillis * 60;
  const daysInMillis = hoursInMillis * 24;
  const yearsInMillis = daysInMillis * 365; // Assume there are 365 days in a year

  // Calculate individual time units
  const years = Math.floor(durationInMillis / yearsInMillis);
  const days = Math.floor((durationInMillis % yearsInMillis) / daysInMillis);
  const hours = Math.floor((durationInMillis % daysInMillis) / hoursInMillis);
  const minutes = Math.floor((durationInMillis % hoursInMillis) / minutesInMillis);
  const seconds = Math.floor((durationInMillis % minutesInMillis) / secondsInMillis);

  // Dynamically build output string
  let durationParts = [];
  if (years > 0) durationParts.push(`${years} 年`);
  if (days > 0) durationParts.push(`${days} 天`);
  if (hours > 0) durationParts.push(`${hours} 小时`);
  if (minutes > 0) durationParts.push(`${minutes} 分钟`);
  if (seconds > 0 || durationParts.length === 0) durationParts.push(`${seconds} 秒`);

  return durationParts.join(' ');
}

// Simple comparison to see if arrays are equal
function arraysEqual(arr1, arr2) {
  return JSON.stringify(arr1) === JSON.stringify(arr2);
}

// prompt score processing
function scoreFormatter(score) {
  return score === -1 ? '--' : score.toFixed(1)
}

/**
 * Load simulated json data
 */
// function funcMockJson() {
//     return fetch("/json/AgentsManager/data.json")
//         .then((res) => {
//             return res.json();
//         })
// }

// task-html-renderer renders the content of the task conversation record
Vue.component('task-html-renderer', {
  props: ['content'],
  render(createElement) {
    return createElement('div', {
      class: 'taskrecord-listWrap-item-content', // Use CSS classes
      domProps: {
        innerHTML: this.content // Insert HTML directly
      }
    });
  }
});

// Register a global custom directive v-el-select-loadmore
Vue.directive('el-select-loadmore', {
  bind(el, binding, vnode) {
    // Get the scroll box defined by element-ui
    const SELECTWRAP_DOM = el.querySelector('.el-select-dropdown .el-select-dropdown__wrap')
    SELECTWRAP_DOM.addEventListener('scroll', function () {
      /**
      * scrollHeight gets the content height of the element (read-only)
      * scrollTop gets or sets the offset value of the element. It is often used to calculate the position of the scroll bar. When an element's container does not produce a vertical scroll bar, its scrollTop value defaults to 0.
      * clientHeight reads the visible height of the element (read-only)
      * If the element is scrolled to the end, the following equation returns true, otherwise it returns false:
      * ele.scrollHeight - ele.scrollTop === ele.clientHeight;
      */
      const condition = this.scrollHeight - this.scrollTop <= this.clientHeight
      if (condition) {
        binding.value()
      }
    })
  }
})

// load-more-select component
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
      default: '' // Use public by default 
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
      default: 'horizontal' // horizontal/vertical horizontal/vertical
    }
  },
  data: function () {
    return {
      optionVisible: false,
      interestsOptions: [], //  Interface returns data
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
    // find dom
    // const rulesDom = this.$refs['elSelectLoadMore'].$el.querySelector(
    //     '.el-input .el-input__suffix .el-input__suffix-inner .el-input__icon'
    // )
    // //Add class to dom
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
      // find dom
      const rulesDom = this.$refs['elSelectLoadMore'].$el.querySelector(
        '.el-input .el-input__suffix .el-input__suffix-inner .el-input__icon'
      )
      if (flag) {
        rulesDom.classList.add('is-reverse') // Add new class to dom
      } else {
        rulesDom.classList.remove('is-reverse') // Add new class to dom
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
    // remote search
    remoteMethod(query, isfocus) {
      // console.log(query, 8888, this.optionVisible,isfocus)
      if (this.optionVisible && isfocus) return
      this.listQuery.filter = query ?? ''
      this.listQuery.pageIndex = 1
      this.interestsOptions = []
      this.interesLoading = true
      this.managementListOption() // Request interface
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
    // Refresh interface
    refreshManagementList() {
      this.listQuery.pageIndex = 1
      this.interestsOptions = []
      this.interesLoading = true
      this.managementListOption()
    },
    // Call interface
    managementListOption() {
      // console.log('managementListOption',this.serviceType);
      this.interesLoading = true // local search call
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

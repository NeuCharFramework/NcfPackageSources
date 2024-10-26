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
            },
            taskStateText: {
                0: '等待',  // 等待 Waiting
                1: '聊天', // 聊天 Chatting
                2: '停顿', // 停顿 Paused
                3: '完成', // 完成 Finished
                4: '取消', // 取消 Cancelled
            },
            taskStateColor: {
                0: 'proceColor',
                1: 'proceColor',
                2: 'proceColor',
                3: 'proceColor',
                4: 'proceColor',
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
            agentDetailsGroupTaskList: [], // 组 任务列表
            agentDetailsGroupTaskHistoryList: [],
            agentDetailsGroupDetailsTaskDetails: '',
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
            groupTaskList: [],
            groupTaskDetails: '',
            groupTaskHistoryList: [],
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
            scrollbarTaskIndex: 0, // 侧边任务index 默认全部
            taskList: [],
            taskDetails: '', // 任务详情数据 查看
            taskHistoryList: [],
            taskMemberList: [],
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
                avastar: '/images/AgentsManager/avatar/avatar1.png' // 头像
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
            historyTimer: {},
        };
    },
    computed: {
    },
    watch: {},
    created() { },
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
        // formatDate,
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
        // 清除 获取历史对话记录 的轮询
        clearHistoryTimer() {
            for (const key in this.historyTimer) {
                if (Object.prototype.hasOwnProperty.call(this.historyTimer, key)) {
                    const element = this.historyTimer[key];
                    if (element) {
                        clearTimeout(element)
                    }
                }
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
                            this.agentList = agentData
                            const agentDetail = this.agentDetails?.agentTemplateDto ?? {}
                            // 获取详情 
                            if (agentDetail.id) {
                                this.getAgentDetailData(agentDetail.id, agentDetail)
                            }
                            // 计算 agent列表 需要填充的元素数量
                            this.calcAgentFillNum()
                        }
                        if (listType === 'groupAgent') {
                            this.groupAgentList = agentData
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
            await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetItemStatus?id=${id}`)
                .then(res => {
                    const data = res?.data ?? {}
                    if (data.success) {
                        const agentDetail = data?.data?.agentTemplateStatus ?? ''
                        if (agentDetail && agentDetail.agentTemplateDto) {
                            const agentTemplateDto = Object.assign({}, detail, agentDetail.agentTemplateDto)
                            agentDetail.agentTemplateDto = agentTemplateDto
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
            // 获取agent列表
            let agentAllList = []
            await serviceAM.get('/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetList')
                .then(res => {
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
                    }
                })
            // 获取组列表
            await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupList?${getInterfaceQueryStr(queryList)}`, queryList)
                .then(res => {
                    const data = res?.data ?? {}
                    if (data.success) {
                        const groupData = data?.data?.chatGroupDtoList ?? []
                        const handleGroupData = groupData.map(item => {
                            const adminAgentTemplateName = agentAllList.find(i => i.id === item.adminAgentTemplateId)?.name ?? ''
                            const enterAgentTemplateName = agentAllList.find(i => i.id === item.adminAgentTemplateId)?.name ?? ''
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
                            this.groupList = handleGroupData
                            const groupDetail = this.groupDetails?.chatGroupDto ?? {}
                            // 获取详情 
                            if (this.groupShowType === '2' && groupDetail.id) {
                                this.getGroupDetailData(listType, groupDetail.id, groupDetail)
                            }
                        }
                        if (listType === 'agentGroup') {
                            this.agentDetailsGroupList = handleGroupData
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
                            this.agentDetailsGroupDetails = groupDetail
                            // 获取任务列表
                            this.gettaskListData('agentGroupTask', id)
                        }
                        if (['group', 'groupTable'].includes(detailType)) {
                            this.groupDetails = groupDetail
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
                            this.taskList = handleTaskData
                            // 默认展示第一个任务详情
                            if (handleTaskData && handleTaskData.length) {
                                const taskDetail = this.taskDetails ? this.taskDetails : handleTaskData[0]
                                this.getTaskDetailData(listType, taskDetail.id, taskDetail)
                            }
                        }
                        // 智能体 任务
                        if (listType === 'agentTask') {
                            this.agentDetailsTaskList = handleTaskData
                            // 默认展示第一个任务详情
                            if (handleTaskData && handleTaskData.length) {
                                const taskDetail = this.agentDetailsTaskDetails ? this.agentDetailsTaskDetails : handleTaskData[0]
                                this.getTaskDetailData(listType, taskDetail.id, taskDetail)
                            }
                        }
                        // 智能体 组 任务
                        if (listType === 'agentGroupTask') {
                            this.agentDetailsGroupTaskList = handleTaskData
                        }
                        // 组 任务
                        if (listType === 'groupTask') {
                            this.groupTaskList = handleTaskData
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
        async getTaskDetailData(detailType, id, detail = {}) {
            await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetItem?id=${id}`)
                .then(res => {
                    const data = res?.data ?? {}
                    if (data.success) {
                        let taskDetail = data?.data?.chatTaskDto ?? ''
                        if (taskDetail) {
                            taskDetail = Object.assign({}, detail, taskDetail)
                        }
                        // 智能体 组 任务
                        if (detailType === 'agentGroupTask') {
                            this.agentDetailsGroupDetailsTaskDetails = taskDetail
                            // 轮询获取数据
                            this.pollGetTaskHistoryData(detailType, this.getTaskRecordListData, taskDetail.id)
                        }
                        // 组 任务
                        if (detailType === 'groupTask') {
                            this.groupTaskDetails = taskDetail
                            // 轮询获取数据
                            this.pollGetTaskHistoryData(detailType, this.getTaskRecordListData, taskDetail.id)
                        }
                        // 智能体 任务
                        if (detailType === 'agentTask') {
                            this.agentDetailsTaskDetails = taskDetail
                            // 获取任务成员列表
                            this.getTaskMemberListData(detailType, taskDetail.chatGroupId)
                            // 轮询获取数据
                            this.pollGetTaskHistoryData(detailType, this.getTaskRecordListData, taskDetail.id)
                        }

                        // 任务
                        if (detailType === 'task') {
                            this.taskDetails = taskDetail
                            // 获取任务成员列表
                            this.getTaskMemberListData(detailType, taskDetail.chatGroupId)
                            // 轮询获取数据
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
                            item.message = marked.parse(item.message);
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
                                    this.taskHistoryList = this.taskHistoryList.concat(historiesData);
                                }
                            } else {
                                const isassignment = arraysEqual(this.taskHistoryList, historiesData)
                                if (!isassignment && historiesData.length > 0) {
                                    this.taskHistoryList = historiesData
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
                                    this.taskHistoryList = this.taskHistoryList.concat(historiesData);
                                }
                            } else {
                                const isassignment = arraysEqual(this.agentDetailsTaskHistoryList, historiesData)
                                if (!isassignment && historiesData.length > 0) {
                                    this.agentDetailsTaskHistoryList = historiesData
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
                                    this.taskHistoryList = this.taskHistoryList.concat(historiesData);
                                }
                            } else {
                                const isassignment = arraysEqual(this.agentDetailsGroupTaskHistoryList, historiesData)
                                if (!isassignment && historiesData.length > 0) {
                                    this.agentDetailsGroupTaskHistoryList = historiesData
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
                                // for (let index = 0; index < historiesData.length; index++) {
                                //     const element = historiesData[index];
                                //     setTimeout(() => {
                                //         this.taskHistoryList.push(element)
                                //     }, 1000)
                                // }
                                if (historiesData.length > 0) {
                                    this.taskHistoryList = this.taskHistoryList.concat(historiesData);
                                }
                            } else {
                                const isassignment = arraysEqual(this.groupTaskHistoryList, historiesData)
                                if (!isassignment && historiesData.length > 0) {
                                    this.groupTaskHistoryList = historiesData
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
                        const groupDetail = data?.data ?? {}
                        // 任务
                        if (memberType === 'task') {
                            this.taskMemberList = groupDetail?.agentTemplateDtoList ?? []
                        }
                        // 智能体 任务
                        if (memberType === 'agentTask') {
                            this.agentDetailsTaskMemberList = groupDetail?.agentTemplateDtoList ?? []
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
            // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
            let serviceURL = ''
            // agent 新增|编辑
            if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
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
            // 组启动（运行任务）
            if (saveType === 'drawerGroupStart') {
                serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.RunGroup'
            }

            if (!serviceURL) return
            await serviceAM.post(serviceURL, serviceForm)
                .then(res => {
                    if (res.data.success) {

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
                        if (saveType === 'drawerGroupStart') {
                            refName = 'groupStartELForm'
                            formName = 'groupStartForm'
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
                        if (['drawerGroup', 'drawerGroupStart'].includes(saveType)) {
                            if (this.tabsActiveName === 'first') {
                                // agentTemplateStatus
                                const id = this.agentDetails?.agentTemplateStatus?.agentTemplateDto?.id ?? ''
                                this.getGroupListData('agentGroup', id)
                            } else if (this.tabsActiveName === 'second') {
                                this.getGroupListData('group')
                            }
                        } else if (['drawerAgent', 'dialogGroupAgent'].includes(saveType)) {
                            const agentMapStr = {
                                'drawerAgent': 'agent',
                                'dialogGroupAgent': 'groupAgent'
                            }
                            this.getAgentListData(agentMapStr[saveType])
                        }
                    } else {
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
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
        // 轮询获取 task 历史对话记录
        pollGetTaskHistoryData(listType, fun, id) {
            if (!listType || !fun) return
            fun(listType, id, '', true)
            const interval = () => {
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
            }
            interval()
        },


        // 编辑 Dailog|抽屉 按钮 
        async handleEditDrawerOpenBtn(btnType, item) {
            // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
            // console.log('handleEditDrawerOpenBtn', btnType, item);
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
            if (btnType === 'drawerGroupStart') {
                formName = 'groupStartForm'
            }
            if (formName) {
                if (btnType === 'drawerAgent' && item.agentTemplateDto) {
                    Object.assign(this[formName], item.agentTemplateDto)
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
            this.visible[btnType] = true
            // 组 启动
            if (btnType === 'drawerGroupStart') {
                // 详情: formData.chatGroupDto 列表: formData
                this.groupStartForm.groupName = formData.chatGroupDto ? formData.chatGroupDto.name : formData.name
                this.groupStartForm.name = formData.chatGroupDto ? `${formData.chatGroupDto.name}1` : `${formData.name}1`
                this.groupStartForm.chatGroupId = formData.chatGroupDto ? formData.chatGroupDto.id : formData.id
            }
            if (btnType === 'drawerGroup') {
                this.getAgentListData('groupAgent')
            }

        },
        // Dailog|抽屉 关闭 按钮
        handleElVisibleClose(btnType) {
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
                    if (btnType === 'drawerGroupStart') {
                        refName = 'groupStartELForm'
                        formName = 'groupStartForm'
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
            if (btnType === 'drawerGroupStart') {
                refName = 'groupStartELForm'
                formName = 'groupStartForm'
            }
            if (!refName) return
            this.$refs[refName].validate((valid) => {
                if (valid) {
                    const submitForm = this[formName] ?? {}
                    this.saveSubmitFormData(btnType, submitForm)
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
            // console.log('handleFilterChange', filterType, value)
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
            this.getAgentDetailData(item.id)
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
            this.agentDetailsGroupTaskList = []
            this.agentDetailsGroupTaskHistoryList = []
            this.agentDetailsGroupDetailsTaskDetails = ''
            this.agentDetailsTaskIndex = 0
            this.agentDetailsTaskList = []
            this.agentDetailsTaskDetails = ''
            this.agentDetailsTaskHistoryList = []
            this.agentDetailsTaskMemberList = []
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
                this.$message({
                    type: 'success',
                    message: '操作成功!'
                });
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
            this.groupTaskList = []
            this.groupTaskDetails = ''
            this.groupTaskHistoryList = []
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
                this.agentDetailsGroupTaskList = []
                this.agentDetailsGroupDetailsTaskDetails = ''
                this.agentDetailsGroupTaskHistoryList = []
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
                this.groupTaskList = []
                this.groupTaskDetails = ''
                this.groupTaskHistoryList = []

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



        // 查看 任务详情
        handleTaskView(clickType, item = {}, index = 0) {
            this.clearHistoryTimer()
            if (clickType === 'agentTask') {
                this.agentDetailsTaskIndex = index ?? ''
                // 清空详情数据
                this.agentDetailsTaskDetails = ''
                this.agentDetailsTaskHistoryList = []
                this.agentDetailsTaskMemberList = []
                this.getTaskDetailData(clickType, item.id, item)
            }
            if (clickType === 'agentGroupTask') {
                this.agentDetailsGroupShowType = '2'
                // 清空详情数据
                this.agentDetailsGroupDetailsTaskDetails = ''
                this.agentDetailsGroupTaskHistoryList = []
                this.getTaskDetailData(clickType, item.id, item)
            }
            if (clickType === 'groupTask') {
                this.groupShowType = '3'
                // 清空详情数据
                this.groupTaskDetails = ''
                this.groupTaskHistoryList = []
                this.getTaskDetailData(clickType, item.id, item)
            }
            if (clickType === 'task') {
                this.scrollbarTaskIndex = index ?? ''
                // 清空详情数据
                this.taskDetails = ''
                this.taskHistoryList = []
                this.taskMemberList = []
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


        // 跳转 PromptRange
        jumpPromptRange(urlType) {
            let url = ''
            if (urlType === 'promptRange') {
                url = '/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18'
            }
            if (urlType === 'model') {
                url = '/Admin/AIKernel/Index?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69'
            }
            if (urlType === 'modelParameter') {
                url = '/Admin/PromptRange/Prompt?uid=C6175B8E-9F79-4053-9523-F8E4AC0C3E18'
            }
            if (!url) return
            simulationAELOperation(url)
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
        // el-scrollbar 触底滚动 到底部
        scrollbarDown(refName, istouchBottom = false, isFirst = false) {
            if (!refName) return
            const scrollbar = this.$refs[refName];
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
        }
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
function formatDate(dateString, format = 'yyyy-MM-dd HH:mm') {
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

function arraysEqual(arr1, arr2) {
    return JSON.stringify(arr1) === JSON.stringify(arr2);
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
    template: `<el-select ref="elSelectLoadMore" v-model="selectVal"  :disabled="disabled" :loading="interesLoading" :placeholder="placeholder" filterable :multiple="multipleChoice" clearable style="width:100%" @change="handleChange">
    <el-option v-for="(item,index) in interestsOptions" :key="item.value" :label="item.label" :value="item.value"></el-option></el-select>`,
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
        this.managementListOption()
    },
    methods: {
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
        // 调用接口
        managementListOption() {
            // console.log('managementListOption',this.serviceType);
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

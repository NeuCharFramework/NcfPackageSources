var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com',
            elSize: 'medium', // el 组件尺寸大小 默认为空  medium、small、mini
            tabsActiveName: 'first', // first(智能体) second(组) third(任务)
            // 显隐 visible
            visible: {
                agent: false, // 智能体 新增|编辑
                groupAgent: false, // 智能体 新增dialog
                group: false, // 组 新增|编辑
                groupStart: false, // 组 启动 
            },
            // 智能体 通用
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
            // 智能体
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
            agentDetailsGroupTreeData: [],
            agentDetailsGroupList: [],
            agentDetailsGroupShowType: '1', // 1:组列表 2:组详情 3:任务详情
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
            // 组
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
            groupAgentList: [], // 组新增时的智能体列表
            groupAgentTotal: 0,

            // 任务 task
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
                avatar: '/images/AgentsManager/avatar/avatar1.png' // 头像
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
                avatar: [
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
        if (this.tabsActiveName === 'first') {
            this.getAgentListData('agent')
        }
        if (this.tabsActiveName === 'second') {
            this.getGroupListData('group')
        }
        if (this.tabsActiveName === 'third') {
            this.gettaskListData('task')
        }
    },
    beforeDestroy() {
        // 清除 获取历史对话记录 的轮询
        for (const key in this.historyTimer) {
            if (Object.prototype.hasOwnProperty.call(this.historyTimer, key)) {
                const element = this.historyTimer[key];
                if (element) {
                    clearTimeout(element)
                }
            }
        }
        // if (this.historyTimer) clearTimeout(this.historyTimer)
    },
    methods: {
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
                        }
                        if (listType === 'groupAgent') {
                            this.groupAgentList = agentData
                            // this.groupAgentTotal = agentData.length
                            // const filterList = this.groupAgentList.filter(i => {
                            //     if (this.groupForm.members) {
                            //         return this.groupForm.members.findIndex(item => item.id === i.id) !== -1
                            //     }
                            //     return false
                            // })
                            // this.toggleSelection(filterList)
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
        // 获取 智能体详情 
        async getAgentDetailData(id) {
            await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetItemStatus?id=${id}`)
                .then(res => {
                    const data = res?.data ?? {}
                    if (data.success) {
                        this.agentDetails = data?.data?.agentTemplateStatus ?? ''
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
                this.agentDetailsTaskQueryList.agentTemplateId = id
                Object.assign(queryList, this.agentDetailsGroupQueryList)
            }
            // 接口对接
            await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupList?${getInterfaceQueryStr(queryList)}`, queryList)
                .then(res => {
                    const data = res?.data ?? {}
                    if (data.success) {
                        const groupData = data?.data?.chatGroupDtoList ?? []
                        if (listType === 'group') {
                            this.groupTreeData = [{
                                id: '0',
                                name: '全部组',
                                children: groupData
                            }]
                            this.groupList = groupData
                            if (this.groupShowType === '2') {
                                // 获取详情
                                this.getGroupDetailData(listType, this.groupDetails.id)
                            }
                        }
                        if (listType === 'agentGroup') {
                            this.agentDetailsGroupTreeData = [{
                                id: '0',
                                name: '全部组',
                                children: groupData
                            }]
                            this.agentDetailsGroupList = groupData
                            // 获取详情
                            this.getGroupDetailData(listType, groupData[this.agentDetailsGroupIndex].id)
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
        async getGroupDetailData(detailType, id) {
            await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${id}`)
                .then(res => {
                    const data = res?.data ?? {}
                    if (data.success) {
                        const groupDetail = data?.data?.chatGroupDto ?? ''
                        if (['agentGroup', 'agentGroupTable'].includes(detailType)) {
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
            //  接口对接
            await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetList?${getInterfaceQueryStr(queryList)}`, queryList)
                .then(res => {
                    const data = res?.data ?? {}
                    if (data.success) {
                        const taskData = data?.data?.chatTaskList ?? []
                        this.taskDetails = ''
                        // 任务
                        if (listType === 'task') {
                            this.taskList = taskData
                            if (taskData && taskData.length) {
                                this.getTaskDetailData(listType, taskData[0].id)
                            }
                        }
                        // 智能体 任务
                        if (listType === 'agentTask') {
                            this.agentDetailsTaskList = taskData
                            if (taskData && taskData.length) {
                                this.getTaskDetailData(listType, taskData[0].id)
                            }
                        }
                        // 智能体 组 任务
                        if (listType === 'agentGroupTask') {
                            this.agentDetailsGroupTaskList = taskData
                        }
                        // 组 任务
                        if (listType === 'groupTask') {
                            this.groupTaskList = taskData
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
        async getTaskDetailData(detailType, id) {
            await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetItem?id=${id}`)
                .then(res => {
                    const data = res?.data ?? {}
                    if (data.success) {
                        const taskDetail = data?.data?.chatTaskDto ?? ''
                        // 智能体 组 任务
                        if (detailType === 'agentGroupTask') {
                            this.agentDetailsGroupDetails = taskDetail
                            this.pollGetTaskHistoryData(detailType, this.getTaskRecordListData, taskDetail.id)
                        }
                        // 智能体 任务
                        if (detailType === 'agentTask') {
                            this.agentDetailsTaskDetails = taskDetail
                            this.pollGetTaskHistoryData(detailType, this.getTaskRecordListData, taskDetail.id)
                            this.getTaskMemberListData(detailType, taskDetail.id)
                        }
                        // 组 任务
                        if (detailType === 'groupTask') {
                            this.groupDetails = taskDetail
                            this.pollGetTaskHistoryData(detailType, this.getTaskRecordListData, taskDetail.id)
                        }
                        // 任务
                        if (detailType === 'task') {
                            this.taskDetails = taskDetail
                            this.pollGetTaskHistoryData(detailType, this.getTaskRecordListData, taskDetail.id)
                            this.getTaskMemberListData(detailType, taskDetail.id)
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
        async getTaskRecordListData(recordType, chatTaskId, nextHistoryId) {
            const queryList = {
                chatTaskId,
                nextHistoryId
            }
            //  接口对接
            await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatGroupHistoryAppService/Xncf.AgentsManager_ChatGroupHistoryAppService.GetList?${getInterfaceQueryStr(queryList)}`, queryList)
                .then(res => {
                    const data = res?.data ?? {}
                    if (data.success) {
                        const taskData = data?.data?.chatTaskList ?? []
                        // 任务
                        if (recordType === 'task') {
                            if (nextHistoryId) {
                                for (let index = 0; index < taskData.length; index++) {
                                    const element = taskData[index];
                                    setTimeout(() => {
                                        this.taskHistoryList.push(element)
                                    }, 1000)
                                }
                            } else {
                                this.taskHistoryList = taskData
                            }

                        }
                        // 智能体 任务
                        if (recordType === 'agentTask') {
                            if (nextHistoryId) {
                                for (let index = 0; index < taskData.length; index++) {
                                    const element = taskData[index];
                                    setTimeout(() => {
                                        this.agentDetailsTaskHistoryList.push(element)
                                    }, 1000)
                                }
                            } else {
                                this.agentDetailsTaskHistoryList = taskData
                            }

                        }
                        // 智能体 组 任务
                        if (recordType === 'agentGroupTask') {
                            if (nextHistoryId) {
                                for (let index = 0; index < taskData.length; index++) {
                                    const element = taskData[index];
                                    setTimeout(() => {
                                        this.agentDetailsGroupTaskHistoryList.push(element)
                                    }, 1000)
                                }
                            } else {
                                this.agentDetailsGroupTaskHistoryList = taskData
                            }

                        }
                        // 组 任务
                        if (recordType === 'groupTask') {
                            if (nextHistoryId) {
                                for (let index = 0; index < taskData.length; index++) {
                                    const element = taskData[index];
                                    setTimeout(() => {
                                        this.groupTaskHistoryList.push(element)
                                    }, 1000)
                                }
                            } else {
                                this.groupTaskHistoryList = taskData
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
        // 获取 任务 成员列表
        async getTaskMemberListData(memberType, chatGroupld) {
            await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${chatGroupld}`)
                .then(res => {
                    const data = res?.data ?? {}
                    if (data.success) {
                        const groupDetail = data?.data?.chatGroupDto ?? ''
                        // 任务
                        if (memberType === 'task') {
                            this.taskMemberList = groupDetail?.chatGroupMembers ?? []
                        }
                        // 智能体 任务
                        if (memberType === 'agentTask') {
                            this.agentDetailsTaskMemberList = groupDetail?.chatGroupMembers ?? []
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
        async saveSubmitFormData(btnType, serviceForm = {}) {
            let serviceURL = ''
            // agent 新增|编辑
            if (btnType === 'agent' || btnType === 'groupAgent') {
                serviceURL = `/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.SetItem`
            }
            // 组 新增|编辑
            if (btnType === 'group') {
                serviceURL = `/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.SetChatGroup`
                if (serviceForm.members) {
                    const membersIds = serviceForm.members.map(item => item.id)
                    serviceForm.memberAgentTemplateIds = membersIds
                    serviceURL += `?${getInterfaceQueryStr({ memberAgentTemplateIds: membersIds })}`
                }
            }
            // 组启动（运行任务）
            if (btnType === 'groupStart') {
                serviceURL = '/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.RunGroup'
            }

            if (!serviceURL) return
            await serviceAM.post(serviceURL, serviceForm)
                .then(res => {
                    if (res.data.success) {
                        // 关闭弹框
                        this.visible[btnType] = false
                        // 重新获取数据
                        if (['group', 'groupStart'].includes(btnType)) {
                            if (this.tabsActiveName === 'first') {
                                this.getGroupListData('agentGroup')
                            } else if (this.tabsActiveName === 'second') {
                                this.getGroupListData('group')
                            }

                        } else if (['agent', 'groupAgent'].includes(btnType)) {
                            this.getAgentListData(btnType)
                        }
                    } else {
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
                            type: 'error',
                            duration: 5 * 1000
                        })
                    }
                })
        },
        // 轮询获取 task 历史对话记录
        pollGetTaskHistoryData(listType, fun, id) {
            if (!listType || !fun) return
            const interval = () => {
                this.historyTimer[listType] = setTimeout(() => {
                    // 执行代码块
                    fun(listType, id)
                    interval()
                }, 1000)
                //     this.historyTimer = setTimeout(() => {
                //         // 执行代码块
                //         fun()
                //         interval()
                //     }, 1000)
            }
            interval()
        },
        // 切换 tabs 页面
        handleTabsClick(tab, event) {
            if (this.tabsActiveName === 'first') {
                this.getAgentListData('agent')
            }
            if (this.tabsActiveName === 'second') {
                this.getGroupListData('group')
            }
            if (this.tabsActiveName === 'third') {
                this.gettaskListData('task')
            }
        },
        // 切换 智能体详情 tabs 页面
        handleAgentDetailsTabsClick(tab, event) {
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
        // 编辑 Dailog|抽屉 打开 按钮 agent groupAgent group groupStart
        handleEditDrawerOpenBtn(btnType, item) {
            let formName = ''
            // 智能体
            if (btnType === 'agent' || btnType === 'groupAgent') {
                formName = 'agentForm'
            }
            // 组
            if (btnType === 'group') {
                formName = 'groupForm'
                // 加载  智能体数据
                this.getAgentListData('groupAgent')
            }
            // 组 启动
            if (btnType === 'groupStart') {
                formName = 'groupStartForm'
            }
            if (formName) {
                if (btnType === 'agent' && item.agentTemplateDto) {
                    Object.assign(this[formName], deepClone(item.agentTemplateDto))
                } else {
                    Object.assign(this[formName], deepClone(item))
                }
                // 回显 表单值

                // this.$set(this, `${formName}`, deepClone(item))
                // 打开 抽屉
                this.handleElVisibleOpenBtn(btnType)
            }
        },
        // Dailog|抽屉 打开 按钮 agent groupAgent group groupStart
        handleElVisibleOpenBtn(btnType, formData) {
            // console.log('通用新增按钮:', btnType);
            // 组 启动
            if (btnType === 'groupStart') {
                this.groupStartForm.groupName = formData?.name ?? ''
                this.groupStartForm.chatGroupId = formData?.id ?? ''
            }
            if (btnType === 'group') {
                this.getAgentListData('groupAgent')
            }
            this.visible[btnType] = true
        },
        // Dailog|抽屉 关闭 按钮 agent groupAgent group groupStart
        handleElVisibleClose(btnType) {
            this.$confirm('确认关闭？')
                .then(_ => {
                    let refName = '', formName = ''
                    // 智能体
                    if (btnType === 'agent' || btnType === 'groupAgent') {
                        refName = 'agentELForm'
                        formName = 'agentForm'
                    }
                    // 组
                    if (btnType === 'group') {
                        refName = 'groupELForm'
                        formName = 'groupForm'
                        // 重置 组获取智能体query
                        this.$set(this, 'groupAgentQueryList', this.$options.data().groupAgentQueryList)
                    }
                    // 组 启动
                    if (btnType === 'groupStart') {
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
                        this.visible[btnType] = false
                    })
                })
                .catch(_ => { });
        },
        // Dailog|抽屉 提交 按钮 agent groupAgent group groupStart
        handleElVisibleSubmit(btnType) {
            let refName = '', formName = ''
            // 智能体 
            if (['agent', 'groupAgent'].includes(btnType)) {
                refName = 'agentELForm'
                formName = 'agentForm'
            }
            // 组
            if (btnType === 'group') {
                refName = 'groupELForm'
                formName = 'groupForm'
            }
            // 组 启动
            if (btnType === 'groupStart') {
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
            this.$refs[refFormEL]?.validateField('avatar', () => { })
        },
        // 筛选输入变化
        handleFilterChange(value, filterType) {
            // console.log('handleFilterChange', filterType, value)
            if (filterType === 'agent') {
                this.agentQueryList.filter = value
                this.getAgentListData('agent', 1)
            }
            if (filterType === 'agentGroup') {
                this.agentDetailsGroupQueryList.filter = value
                this.getGroupListData('agentGroup', 1)
            }
            if (filterType === 'agentTask') {
                this.agentDetailsTaskQueryList.filter = value
                this.gettaskListData('agentTask', 1)
            }
            if (filterType === 'group') {
                this.groupQueryList.filter = value
                this.getGroupListData('group', 1)
            }
            if (filterType === 'groupAgent') {
                this.groupAgentQueryList.filter = value
                this.getAgentListData('groupAgent', 1)
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
        },
        // 查看 智能体
        handleAgentView(item, index) {
            this.scrollbarAgentIndex = index ?? ''
            // 清空 定时器 agentGroupTask agentTask
            const timerList = ['agentGroupTask', 'agentTask']
            for (let index = 0; index < timerList.length; index++) {
                const element = timerList[index];
                if (this.historyTimer[element]) {
                    clearTimeout(this.historyTimer[element])
                }
            }
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
            this.agentDetailsGroupTreeData = []
            this.agentDetailsGroupList = []
            this.agentDetailsGroupTaskList = []
            this.agentDetailsGroupTaskHistoryList = []
            this.agentDetailsGroupShowType = '2'
            this.agentDetailsGroupIndex = 0
            this.agentDetailsGroupDetails = ''
            this.agentDetailsTaskIndex = 0
            this.agentDetailsTaskList = []
            this.agentDetailsTaskDetails = ''
            this.agentDetailsTaskHistoryList = []
            this.agentDetailsTaskMemberList = []
            this.$set(this, 'agentDetailsGroupTaskQueryList', this.$options.data().agentDetailsGroupTaskQueryList)
            this.$set(this, 'agentDetailsGroupQueryList', this.$options.data().agentDetailsGroupQueryList)
            this.$set(this, 'agentDetailsTaskQueryList', this.$options.data().agentDetailsTaskQueryList)
        },
        // 智能体 状态 切换
        setAgentState(stateType, item) {
            if (!stateType || !item) return
            let messageText = ''
            if (stateType === 'stop') {
                messageText = `<div>是否确认将${item.name}智能体停用？</div><div>此智能体将不会参与后续新任务</div>`
            }
            if (stateType === 'delete') {
                messageText = `<div>是否确认从${item.name}组退出？</div><div>移除出后将无法看到之前参与的任务记录</div>`
            }
            this.$confirm(messageText, '操作确认', {
                dangerouslyUseHTMLString: true, // message当作HTML片段处理
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                // type: 'warning'
            }).then(() => {
                // to do 调用接口
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
                    this.agentDetailsGroupDetails = '' // 清空组详情
                }
                if (this.agentDetailsGroupShowType === '2') {
                    this.agentDetailsGroupDetails = deepClone(data)
                }
                if (this.agentDetailsGroupShowType === '3') {
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
                this.agentDetailsGroupShowType = '2'
                this.agentDetailsGroupDetails = deepClone(row)
            }
            if (clickType === 'group') {
                this.groupShowType = '2'
                this.groupDetails = deepClone(row)
            }
        },
        // 组 查看全部 列表 
        handleGroupViewAll() {
            this.groupShowType = '1'
            this.scrollbarGroupIndex = '' // 清空索引
            this.groupDetails = '' // 清空详情数据
        },
        // 组 查看列表 详情 
        handleGroupView(clickType, item, index = 0) {
            // 清空 定时器 agentGroupTask groupTask
            const timerList = ['agentGroupTask', 'groupTask']
            for (let index = 0; index < timerList.length; index++) {
                const element = timerList[index];
                if (this.historyTimer[element]) {
                    clearTimeout(this.historyTimer[element])
                }
            }
            if (clickType === 'agentGroup' || clickType === 'agentGroupTable') {
                this.agentDetailsGroupShowType = '2'
                if (clickType === 'agentGroupTable') {
                    const { pageIndex, pageSize } = this.agentDetailsGroupQueryList
                    this.agentDetailsGroupIndex = pageIndex > 1 ? pageIndex * pageSize + index : index
                } else {
                    this.agentDetailsGroupIndex = index ?? 0
                }
                this.getGroupDetailData(clickType, item.id)
            }
            if (clickType === 'agentGroupTask') {
                this.agentDetailsGroupShowType = '3'
                this.getTaskDetailData(clickType, item.id)
            }
            if (clickType === 'group' || clickType === 'groupTable') {
                this.groupShowType = '2'
                if (clickType === 'groupTable') {
                    const { pageIndex, pageSize } = this.groupQueryList
                    this.scrollbarGroupIndex = pageIndex > 1 ? pageIndex * pageSize + index : index
                } else {
                    this.scrollbarGroupIndex = index ?? 0
                }
                this.scrollbarGroupIndex = index ?? ''
                this.getGroupDetailData(clickType, item.id)
            }
            if (clickType === 'groupTask') {
                this.groupShowType = '3'
                this.getTaskDetailData(clickType, item.id)
            }
        },
        // 返回组详情页面
        returnGroup(clickType) {
            // 清空 定时器 agentGroupTask agentTask
            const timerList = ['agentGroupTask', 'groupTask']
            for (let index = 0; index < timerList.length; index++) {
                const element = timerList[index];
                if (this.historyTimer[element]) {
                    clearTimeout(this.historyTimer[element])
                }
            }
            if (clickType === 'agentGroupTask') {
                this.agentDetailsGroupShowType = '2'
                const item = this.agentDetailsGroupList[this.agentDetailsGroupIndex]
                this.getGroupDetailData(clickType, item.id)

            }
            if (clickType === 'groupTask') {
                this.groupShowType = '2'
                const item = this.groupList[this.scrollbarGroupIndex]
                this.getGroupDetailData(clickType, item.id)
            }
        },
        // 查看 任务详情
        handleTaskView(clickType, item, index = 0) {
            // 清空 定时器 agentGroupTask groupTask
            const timerList = ['agentTask', 'task']
            for (let index = 0; index < timerList.length; index++) {
                const element = timerList[index];
                if (this.historyTimer[element]) {
                    clearTimeout(this.historyTimer[element])
                }
            }
            if (clickType === 'agentTask') {
                this.agentDetailsTaskIndex = index ?? ''
                this.getTaskDetailData(clickType, item.id)
            }
            if (clickType === 'task') {
                this.scrollbarTaskIndex = index ?? ''
                this.getTaskDetailData(clickType, item.id)
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
            if (val && val.length) {
                this.groupAgentList.forEach(i => {
                    const vFindIndex = val.findIndex(item => item.id === i.id)
                    if (vFindIndex === -1) {
                        const findIndex = this.groupForm.members.findIndex(item => item.id === i.id)
                        if (findIndex !== -1) {
                            this.groupForm.members.splice(findIndex, 1);
                        }
                    } else {
                        const findIndex = this.groupForm.members.findIndex(item => item.id === i.id)
                        if (findIndex === -1) {
                            this.groupForm.members.push(i)
                        }
                    }
                })
            } else {
                this.groupAgentList.forEach(i => {
                    const findIndex = this.groupForm.members.findIndex(item => item.id === i.id)
                    if (findIndex !== -1) {
                        this.groupForm.members.splice(findIndex, 1);
                    }
                })
            }
        },
        // 组 新增|编辑 智能体 成员取消选中
        groupMembersCancel(item, index) {
            const findIndex = this.groupAgentList.findIndex(i => item.id === i.id)
            if (findIndex !== -1) {
                this.toggleSelection([this.groupAgentList[findIndex]])
            }
            this.groupForm.members.splice(index, 1);
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
        // 任务 滚动 向下
        taskScrollbarDown() {
            if (this.$refs.taskHistoryScrollbar) {
                this.$refs.taskHistoryScrollbar.handleScroll = () => {
                    const wrap = this.$refs.taskHistoryScrollbar.wrap;
                    this.$refs.taskHistoryScrollbar.moveY = (wrap.scrollTop * 100) / wrap.clientHeight;
                    this.$refs.taskHistoryScrollbar.moveX = (wrap.scrollLeft * 100) / wrap.clientWidth;
                    let poor = wrap.scrollHeight - wrap.clientHeight;
                    if (
                        poor == parseInt(wrap.scrollTop) ||
                        poor == Math.ceil(wrap.scrollTop) ||
                        poor == Math.floor(wrap.scrollTop)
                    ) {
                        console.log("已经触底了");
                    }
                };
            }
        },
        // 获取发送人名称
        getTaskSenderName(taskType, formId) {
            // 智能体 组 任务
            if (taskType === 'agentGroupTask') {
                const chatGroupMembers = this.agentDetailsGroupDetails?.chatGroupMembers ?? []
                const fintItem = chatGroupMembers.find(item => item.id === formId)
                return fintItem ?? {}
            }
            // 组 任务
            if (taskType === 'groupTask') {
                const chatGroupMembers = this.groupDetails?.chatGroupMembers ?? []
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
 * 加载载 模拟json 数据
 */
// function funcMockJson() {
//     return fetch("/json/AgentsManager/data.json")
//         .then((res) => {
//             return res.json();
//         })
// }


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
    template: `<el-select ref="elSelectLoadMore" v-model="selectVal"  :disabled="disabled" :loading="interesLoading" :placeholder="placeholder" :multiple="multipleChoice" clearable style="width:100%" @change="handleChange">
    <el-option v-for="(item,index) in interestsOptions" :key="item.value" :label="item.label" :value="item.value"></el-option></el-select>`,
    props: {
        // eslint-disable-next-line vue/require-prop-types
        value: {
            // type: [Array, String, Number],
            required: true
        },
        selectKey: {
            type: String,
            // required: true,
            default: ''
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
                pageIndex: 1,
                pageSize: 100,
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
        // selectKey: {
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
        console.log('s*******');

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
                            this.interestsOptions = this.interestsOptions.concat(deepClone(listData))
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
                        this.interestsOptions = this.interestsOptions.concat(deepClone(listData))
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
                        this.interestsOptions = this.interestsOptions.concat(deepClone(listData))
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

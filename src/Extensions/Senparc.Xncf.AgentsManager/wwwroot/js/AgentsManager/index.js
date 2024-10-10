/**
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
* This is just a simple version of deep copy
* Has a lot of edge cases bug
* If you want to use a perfect deep copy, use lodash's _.cloneDeep
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
var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com',
            elSize: 'medium', // el 组件尺寸大小 默认为空  medium、small、mini
            tabsActiveName: 'second', // first(智能体) second(组) third(任务)
            // 显隐 visible
            visible: {
                agent: false, // 智能体 新增|编辑
                agentDialog: false, // 智能体 新增dialog
                group: true, // 组 新增|编辑
                groupStart: false, // 组 启动 
            },
            // 智能体
            agentQueryList: {
                page: 1,
                size: 100,
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
            // 新增智能体
            agentForm: {
                name: '',
                systemMessageType: '1',
                systemMessageId: '',
                systemMessageInput: '',
                description: '',
                platform: '',
                parameter: '',
                avatar: ''
            },
            agentFormRules: {
                name: [
                    { required: true, message: '请填写', trigger: 'blur' },
                ],
                systemMessageId: [
                    { required: true, message: '请选择', trigger: 'change' },
                ],
                systemMessageInput: [
                    { required: true, message: '请填写', trigger: 'blur' },
                ],
                description: [
                    { required: true, message: '请填写', trigger: 'blur' },
                ],
                platform: [
                    { required: true, message: '请选择', trigger: 'change' },
                ],
                parameter: [
                    { required: true, message: '请填写', trigger: 'blur' },
                ],
                avatar: [
                    { required: true, message: '请选择', trigger: 'change' },
                ]
            },
            // 组
            groupQueryList: {
                page: 1,
                size: 100,
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
            groupDetails: '',
            groupForm: {
                name: '',
                members: [],
                groupLeader: '',
                dockingPerson: '',
                description: '',
            },
            groupFormRules: {
                name: [
                    { required: true, message: '请填写', trigger: 'blur' },
                ],
                members: [
                    { required: true, message: '请填写', trigger: 'change' },
                ],
                groupLeader: [
                    { required: true, message: '请填写', trigger: 'change' },
                ],
                dockingPerson: [
                    { required: true, message: '请选择', trigger: 'change' },
                ],
                // description: [
                //     { required: true, message: '请填写', trigger: 'blur' },
                // ],
            },
            groupAgentQueryList: {
                page: 1,
                size: 10,
                filter: '', // 筛选文本
                timeSort: false, // 默认降序
                proce: false, // 进行中
                stop: false, // 停用
                stand: false, // 待命
            },
            groupAgentList: [], // 组新增时的智能体列表
            groupAgentTotal: 0,
            groupStartForm: {
                groupName: '',
                name: '',
                modelName: '',
                personality: false,
                description: ''
            },
            groupStartFormRules: {
                groupName: [
                    { required: true, message: '请填写', trigger: 'blur' },
                ],
                name: [
                    { required: true, message: '请填写', trigger: 'blur' },
                ],
                modelName: [
                    { required: true, message: '请选择', trigger: 'change' },
                ],
                description: [
                    { required: true, message: '请填写', trigger: 'blur' },
                ],
            },
            // 任务 task
            taskList: [],
        };
    },
    computed: {
    },
    watch: {},
    created() {
    },
    mounted() {
        if (this.tabsActiveName === 'first') {
            this.getAgentListData()
        }
        if (this.tabsActiveName === 'second') {
            this.getGroupListData()
        }
        if (this.tabsActiveName === 'third') {

        }
    },
    beforeDestroy() {

    },
    methods: {
        // 切换 tabs 页面
        handleTabsClick(tab, event) {
            if (this.tabsActiveName === 'first') {
                this.getAgentListData()
            }
            if (this.tabsActiveName === 'second') {
                this.getGroupListData()
            }
            if (this.tabsActiveName === 'third') {

            }
        },
        // 编辑 智能体
        handleEditDrawerOpenBtn(btnType, item) {
            let formName = ''
            // 智能体
            if (btnType === 'agent' || btnType === 'agentDialog') {
                formName = 'agentForm'
            }
            // 组
            if (btnType === 'group') {
                formName = 'groupForm'
                this.getGroupAgentListData()
            }
            // 组 启动
            if (btnType === 'groupStart') {
                formName = 'groupStartForm'
            }
            if (formName) {
                // 回显 表单值
                this.$set(this, `${formName}`, deepClone(item))
                // 打开 抽屉
                this.handleElVisibleOpenBtn(btnType)
            }
        },
        // Dailog|抽屉 打开 按钮 agent group
        handleElVisibleOpenBtn(btnType, formData) {
            // console.log('通用新增按钮:', btnType);
            // 组 启动
            if (btnType === 'groupStart') {
                this.groupStartForm.groupName = formData?.name ?? ''
            }
            if (btnType === 'group') {
                this.getGroupAgentListData()
            }
            this.visible[btnType] = true
        },
        // Dailog|抽屉 关闭 agent group
        handleElVisibleClose(btnType) {
            this.$confirm('确认关闭？')
                .then(_ => {
                    let refName = '', formName = ''
                    // 智能体
                    if (btnType === 'agent' || btnType === 'agentDialog') {
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
        // Dailog|抽屉 提交 agent group
        handleElVisibleSubmit(btnType) {
            let refName = '', formName = ''
            if (btnType === 'agent' || btnType === 'agentDialog') {
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
                    alert('submit!');
                    const submitForm = this[formName] ?? {}
                    if (btnType === 'agent' || btnType === 'agentDialog') {
                        // submitForm
                        // to do 调用接口
                    }
                    if (btnType === 'group') {
                        // submitForm
                        // to do 调用接口
                    }
                    if (btnType === 'groupStart') {
                        // submitForm
                        // to do 调用接口
                    }
                    this.visible[btnType] = false
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
        // 处理  筛选条件事件 agent group
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
        },
        // 获取 智能体 数据
        getAgentListData() {
            // 模拟数据
            const _agentList = []
            for (let index = 0; index < 15; index++) {
                _agentList.push({
                    id: index + 1,
                    systemMessageType: '1',
                    systemMessageId: '',
                    systemMessageInput: '',
                    name: '盛派设计师',
                    description: '资深设计师，精通平面设计和交互设计，能够根据项目需求进行全面的用户需求分析，确保设计方案不仅美观，同时也符合用户体验和功能需求。',
                    numberParticipants: '49',
                    score: '9.7',
                    state: 1, // 1\2\3
                    platform: '',
                    parameter: '',
                    avatar: '',
                })
            }
            this.agentList = _agentList
        },
        // 查看全部智能体 列表 
        handleAgentViewAll() {
            this.scrollbarAgentIndex = '' // 清空索引
            this.agentDetails = '' // 清空详情数据
        },
        // 查看 智能体
        handleAgentView(item, index) {
            this.scrollbarAgentIndex = index ?? ''
            this.agentDetails = item
            // to do 获取详情 this.agentDetails
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
        // 获取 组 数据
        getGroupListData() {
            // 模拟数据
            const _groupList = []
            for (let index = 0; index < 15; index++) {
                _groupList.push({
                    id: index + 1,
                    name: `运营部门${index + 1}`,
                    numberTasks: '6',
                    lastRunningTime: '2024-10-9 17:11:20',
                    groupLeader: '张三',
                    dockingPerson: '项目经理',
                    description: '',
                    state: 1, // 1\2\3
                    children: [{
                        id: `${index + 1}.1`,
                        name: '用户满意度调查',
                        startTime: '2024-10-9 17:11:20',
                        duration: '1分钟',
                        modelName: '测试模型1',
                        description: '这是一个测试任务',
                        state: 1 // 1\2\3
                    }, {
                        id: `${index + 1}.2`,
                        name: '用户抱怨反馈调查',
                        startTime: '2024-10-9 17:11:20',
                        duration: '1分钟',
                        modelName: '测试模型1',
                        description: '这是一个测试任务',
                        state: 1 // 1\2\3
                    }]
                })
            }
            this.groupTreeData = [{
                id: '0',
                name: '全部组',
                children: _groupList
            }]
            this.groupList = _groupList
        },
        // 侧边 组tree 组件 节点 筛选
        filterGroupTreeNode(value, data) {
            if (!value) return true;
            return data.name.indexOf(value) !== -1;
        },
        // 侧边 组tree 组件 节点 点击
        handleGroupTreeNodeClick(node, data) {
            // console.log('handleGroupTreeNodeClick', node, data)
            this.groupShowType = node.level.toString()
            if (this.groupShowType === '1') {
                this.groupDetails = '' // 清空组详情
                this.taskDetails = '' // 清空任务详情
            }
            if (this.groupShowType === '2') {
                this.groupDetails = deepClone(data)
            }
            if (this.groupShowType === '3') {
                this.taskDetails = deepClone(data)
            }
        },
        // 组 查看详情
        handleGroupView(row) {
            // console.log('handleGroupView', row)
            this.groupShowType = '2'
            this.groupDetails = deepClone(row)
        },
        // 组的智能体table 切换table 选中
        toggleSelection(rows) {
            if (rows) {
                rows.forEach(row => {
                    this.$refs?.groupAgentTable?.toggleRowSelection(row);
                });
            } else {
                this.$refs?.groupAgentTable?.clearSelection();
            }
        },
        // 组的智能体table 选中变化
        handleSelectionChange(val) {
            this.groupForm.members = val;
        },
        // 组的成员取消选中
        groupMembersCancel(item) {
            console.log('groupMembersCancel',item)
        },
        // 获取组 新增时的智能体
        getGroupAgentListData(page) {
            this.groupAgentQueryList.page = page ?? 1
            // to do 获取接口
            // 模拟数据
            const _groupAgentList = []
            for (let index = 0; index < 15; index++) {
                _groupAgentList.push({
                    id: index + 1,
                    systemMessageType: '1',
                    systemMessageId: '',
                    systemMessageInput: '',
                    name: '盛派设计师',
                    description: '资深设计师，精通平面设计和交互设计，能够根据项目需求进行全面的用户需求分析，确保设计方案不仅美观，同时也符合用户体验和功能需求。',
                    numberParticipants: '49',
                    score: '9.7',
                    state: 1, // 1\2\3
                    platform: '',
                    parameter: '',
                    avatar: '',
                })
            }
            this.groupAgentList = _groupAgentList
            this.groupAgentTotal = _groupAgentList.length
        },
        // 获取任务列表
        getTaskListData() {
            // 模拟数据
            const _taskList = []
            for (let index = 0; index < 15; index++) {
                _taskList.push({
                    id: index + 1,
                    name: '大运营部门',
                    startTime: '2024-10-9 17:11:20',
                    duration: '1分钟',
                    modelName: '测试模型1',
                    description: '这是一个测试任务',
                    state: 1 // 1\2\3
                })
            }
            this.taskList = _taskList
        }
    }
});

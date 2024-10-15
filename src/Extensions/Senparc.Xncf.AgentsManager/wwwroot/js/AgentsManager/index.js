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

/**
 *Created by PanJiaChen on 16/11/29.
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

function FuncMockJson() {
    return fetch("/json/AgentsManager/data.json")
        .then((res) => {
            return res.json();
        })
    // .then((data) => {
    //     // console.log('Func', data)
    // });
}

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
// 
Vue.component('load-more-select', {
    template: `<el-select ref="elSelectLoadMore" v-model="selectVal" v-el-select-loadmore="interestsLoadmore" :disabled="disabled" :loading="interesLoading" :placeholder="placeholder" :multiple="multipleChoice" filterable remote collapse-tags reserve-keyword :remote-method="remoteMethod" clearable style="width:100%" @focus="remoteMethod('',true)" @change="handleChange" @visible-change="reverseArrow">
    <el-option v-for="(item,index) in interestsOptions" :key="index" :label="item.key" :value="item.value"></el-option></el-select>`,
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
                page: 1,
                size: 100,
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
        const rulesDom = this.$refs['elSelectLoadMore'].$el.querySelector(
            '.el-input .el-input__suffix .el-input__suffix-inner .el-input__icon'
        )
        // 对dom新增class
        rulesDom?.classList.add('el-icon-arrow-up')
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
            this.listQuery.page = 1
            this.interestsOptions = []
            this.interesLoading = true
            this.managementListOption() // 请求接口
        },
        interestsLoadmore() {
            setTimeout(() => {
                this.listQuery.page = this.listQuery.page + 1
                if (this.listQuery.size > this.currentPageSize) {
                    this.listQuery.page = this.listQuery.page - 1
                    return
                }
                this.managementListOption()
            }, 1000)
        },
        // 调用接口
        managementListOption() {
            let serviceURL = ''
            if (this.serviceType === 'agent') {
                serviceURL = ''
            } else if (this.serviceType === 'model') {
                serviceURL = ''
            } else if (this.serviceType === 'modelC') {
                serviceURL = ''
            }
            if (!serviceURL) return
            // if (serviceForm.id) {
            //     serviceURL += `?id=${serviceForm.id}`
            // }
            // 获取列表接口
            serviceAM.post('', this.listQuery).then(res => {
                const data = res?.data ?? {}
                if (data.errorMessage) {
                    this.$message.error(data.errorMessage)
                }
                const {
                    items = []
                    // total = 0
                } = data
                this.interesLoading = false
                this.currentPageSize = items?.length ?? 0
                const listData = this.interestsOptions.concat(items)
                this.interestsOptions = listData // [...this.interestsOptions, ...items]
                // console.log(this.interestsOptions, 888)
            }).catch((err) => {
                console.log('err', err)
            })
                .finally(() => { })
        }
    },
})

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
                agentDialog: false, // 智能体 新增dialog
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
            // 智能体详情
            agentDetailsTabsActiveName: 'first', // first(组) second(任务)
            // 智能体详情 组
            agentDetailsGroupQueryList: {
                page: 1,
                size: 100,
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
            // 智能体详情 任务
            agentDetailsTaskQueryList: {
                page: 1,
                size: 100,
                filter: '', // 筛选文本
                timeSort: false, // 默认降序
                proce: false, // 进行中
                stop: false, // 停用
                stand: false, // 待命
            },
            agentDetailsTaskIndex: 0, // 侧边任务index 默认全部
            agentDetailsTaskList: [],
            agentDetailsTaskDetails: '',
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
            scrollbarGroupIndex: '', // 侧边任务index 默认全部
            groupDetails: '',
            // 组 新增|编辑 智能体
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

            // 任务 task
            taskQueryList: {
                page: 1,
                size: 100,
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
            // 智能体 新增|编辑
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
            // 组 新增|编辑
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
            // 组 启动
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
            }
        };
    },
    computed: {
    },
    watch: {},
    created() { },
    mounted() {
        if (this.tabsActiveName === 'first') {
            this.getAgentListData(1, 'agent')
        }
        if (this.tabsActiveName === 'second') {
            this.getGroupListData(1, 'group')
        }
        if (this.tabsActiveName === 'third') {
            this.gettaskListData(1, 'task')
        }
    },
    beforeDestroy() { },
    methods: {
        // 跳转 prompt
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
        // 切换 智能体详情 tabs 页面
        handleAgentDetailsTabsClick(tab, event) {
            if (this.agentDetailsTabsActiveName === 'first') {
                this.getGroupListData(1, 'agentGroup')
            }
            if (this.agentDetailsTabsActiveName === 'second') {
                this.gettaskListData(1, 'agentTask')
            }
        },
        // 切换 tabs 页面
        handleTabsClick(tab, event) {
            if (this.tabsActiveName === 'first') {
                this.getAgentListData(1, 'agent')
            }
            if (this.tabsActiveName === 'second') {
                this.getGroupListData(1, 'group')
            }
            if (this.tabsActiveName === 'third') {
                this.gettaskListData(1, 'task')
            }
        },
        // 编辑 Dailog|抽屉 打开 按钮 agent agentDialog group groupStart
        handleEditDrawerOpenBtn(btnType, item) {
            let formName = ''
            // 智能体
            if (btnType === 'agent' || btnType === 'agentDialog') {
                formName = 'agentForm'
            }
            // 组
            if (btnType === 'group') {
                formName = 'groupForm'
                this.getAgentListData(1, 'groupAgent')
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
        // Dailog|抽屉 打开 按钮 agent agentDialog group groupStart
        handleElVisibleOpenBtn(btnType, formData) {
            // console.log('通用新增按钮:', btnType);
            // 组 启动
            if (btnType === 'groupStart') {
                this.groupStartForm.groupName = formData?.name ?? ''
                // this.groupStartForm.groupId =  formData?.id ?? ''
            }
            if (btnType === 'group') {
                this.getAgentListData(1, 'groupAgent')
            }
            this.visible[btnType] = true
        },
        // Dailog|抽屉 关闭 按钮 agent agentDialog group groupStart
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
        // Dailog|抽屉 提交 按钮 agent agentDialog group groupStart
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
            console.log('handleFilterChange', filterType, value)
            if (filterType === 'agent') {
                this.agentQueryList.filter = value
                this.getAgentListData(1, 'agent')
            }
            if (filterType === 'agentGroup') {
                this.agentDetailsGroupQueryList.filter = value
                this.getGroupListData(1, 'agentGroup')
            }
            if (filterType === 'agentTask') {
                this.agentDetailsTaskQueryList.filter = value
                this.gettaskListData(1, 'agentTask')
            }
            if (filterType === 'group') {
                this.groupQueryList.filter = value
                this.getGroupListData(1, 'group')
            }
            if (filterType === 'groupAgent') {
                this.groupAgentQueryList.filter = value
                this.getAgentListData(1, 'groupAgent')
            }
            if (filterType === 'task') {
                this.taskQueryList.filter = value
                this.gettaskListData(1, 'task')
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
            this.agentDetails = item
            this.agentDetailsTabsActiveName = 'first'
            // 重置 数据
            this.resetAgentDetailsQuery()
            // 重置 组获取智能体query
            if (this.agentDetailsTabsActiveName === 'first') {

                this.getGroupListData(1, 'agentGroup')
            }
            if (this.agentDetailsTabsActiveName === 'second') {

                this.gettaskListData(1, 'agentTask')
            }
        },
        // 重置 智能体详情下的组和任务数据
        resetAgentDetailsQuery() {
            this.agentDetailsGroupTreeData = []
            this.agentDetailsGroupList = []
            this.agentDetailsGroupShowType = '2'
            this.agentDetailsGroupIndex = 0
            this.agentDetailsGroupDetails = ''
            this.agentDetailsTaskIndex = 0
            this.agentDetailsTaskList = []
            this.agentDetailsTaskDetails = ''
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
            console.log('handleGroupView', clickType, item, index);

            if (clickType === 'agentGroup' || clickType === 'agentGroupTable') {
                this.agentDetailsGroupShowType = '2'
                if (clickType === 'agentGroupTable') {
                    const { page, size } = this.agentDetailsGroupQueryList
                    this.agentDetailsGroupIndex = page > 1 ? page * size + index : index
                } else {
                    this.agentDetailsGroupIndex = index ?? 0
                }
                this.agentDetailsGroupDetails = deepClone(item)
            }
            if (clickType === 'agentGroupTask') {
                this.agentDetailsGroupShowType = '3'
                this.agentDetailsGroupDetails = deepClone(item)
            }
            if (clickType === 'group' || clickType === 'groupTable') {
                this.groupShowType = '2'
                if (clickType === 'groupTable') {
                    const { page, size } = this.groupQueryList
                    this.scrollbarGroupIndex = page > 1 ? page * size + index : index
                } else {
                    this.scrollbarGroupIndex = index ?? 0
                }
                this.scrollbarGroupIndex = index ?? ''
                this.groupDetails = deepClone(item)
            }
            if (clickType === 'groupTask') {
                this.groupShowType = '3'
                this.groupDetails = deepClone(item)
            }
        },

        // 查看 任务详情
        handleTaskView(clickType, item, index = 0) {
            if (clickType === 'agentTask') {
                this.agentDetailsTaskIndex = index ?? ''
                this.agentDetailsTaskDetails = item
            }
            if (clickType === 'task') {
                this.scrollbarTaskIndex = index ?? ''
                this.taskDetails = item
            }
            // to do 获取详情 this.agentDetails
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
            // console.log('handleSelectionChange',val);
            // this.groupAgentList val
            // const members = deepClone(this.groupForm.members)
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
        // 返回组详情页面
        returnGroup(clickType) {
            if (clickType === 'agentGroupTask') {
                this.agentDetailsGroupShowType = '2'
                const item = this.agentDetailsGroupList[this.agentDetailsGroupIndex]
                // this.agentDetailsGroupIndex = index ?? 0
                this.agentDetailsGroupDetails = deepClone(item)
            }
            if (clickType === 'groupTask') {
                this.groupShowType = '2'
                const item = this.groupList[this.scrollbarGroupIndex]
                // this.scrollbarGroupIndex = index ?? ''
                this.groupDetails = deepClone(item)
            }
        },
        // 获取 智能体 数据
        async getAgentListData(page, queryType) {
            // console.log('getAgentListData',page,queryType);
            const queryList = {}
            if (queryType === 'agent') {
                this.agentQueryList.page = page ?? 1
                Object.assign(queryList, this.agentQueryList)
            }
            if (queryType === 'groupAgent') {
                this.groupAgentQueryList.page = page ?? 1
                Object.assign(queryList, this.groupAgentQueryList)
            }
            // 模拟数据
            // console.log('FuncMockJson()',);
            FuncMockJson().then((data) => {
                // console.log('Func', data)
                const { agents = [] } = data
                if (queryType === 'agent') {
                    this.agentList = agents
                }
                if (queryType === 'groupAgent') {
                    this.groupAgentList = agents
                    this.groupAgentTotal = agents.length
                    const filterList = this.groupAgentList.filter(i => {
                        if (this.groupForm.members) {
                            return this.groupForm.members.findIndex(item => item.id === i.id) !== -1
                        }
                        return false
                    })
                    this.toggleSelection(filterList)
                }
            })

            // to do 接口对接
            // return await serviceAM.post('', queryList)
            //     .then(res => {
            //         if (res.data.success) {
            //             const agentData = res?.data?.data ?? []
            //             if (queryType === 'agent') {
            //                 this.agentList = agentData
            //             }
            //             if (queryType === 'groupAgent') {
            //                 this.groupAgentList = agentData
            //                 this.groupAgentTotal = agentData.length
            //             }
            //         } else {
            //             app.$message({
            //                 message: res.data.errorMessage || res.data.data || 'Error',
            //                 type: 'error',
            //                 duration: 5 * 1000
            //             })
            //         }
            //     })
        },
        // 获取 组 数据
        async getGroupListData(page, queryType) {
            // console.log('getGroupListData',page,queryType);
            const queryList = {}
            if (queryType === 'group') {
                this.groupQueryList.page = page ?? 1
                Object.assign(queryList, this.groupQueryList)
            }
            if (queryType === 'agentGroup') {
                this.agentDetailsGroupQueryList.page = page ?? 1
                Object.assign(queryList, this.agentDetailsGroupQueryList)
            }
            // 模拟数据
            FuncMockJson().then((data) => {
                // console.log('Func', data)
                const { group, task = [] } = data
                const _groupList = group.map(item => {
                    return {
                        ...item,
                        children: task
                    }
                })
                if (queryType === 'group') {
                    this.groupTreeData = [{
                        id: '0',
                        name: '全部组',
                        children: _groupList
                    }]
                    this.groupList = _groupList
                }
                if (queryType === 'agentGroup') {
                    this.agentDetailsGroupTreeData = [{
                        id: '0',
                        name: '全部组',
                        children: _groupList
                    }]
                    this.agentDetailsGroupList = _groupList
                    const groupIndex = this.agentDetailsGroupIndex ?? 0
                    this.handleGroupView(queryType, _groupList[groupIndex], groupIndex)
                }
            })
            // to do 接口对接
            // return await serviceAM.post('', queryList)
            // .then(res => {
            //     if (res.data.success) {
            //         const groupData = res?.data?.data ?? []
            //         if (queryType === 'group') {
            //             this.groupTreeData = [{
            //                 id: '0',
            //                 name: '全部组',
            //                 children: groupData
            //             }]
            //             this.groupList = groupData
            //         }
            //         if (queryType === 'agentGroup') {
            //             this.agentDetailsGroupTreeData = [{
            //                 id: '0',
            //                 name: '全部组',
            //                 children: groupData
            //             }]
            //             this.agentDetailsGroupList = groupData
            //         }
            //     } else {
            //         app.$message({
            //             message: res.data.errorMessage || res.data.data || 'Error',
            //             type: 'error',
            //             duration: 5 * 1000
            //         })
            //     }
            // })
        },
        // 获取 任务 数据
        async gettaskListData(page, queryType) {
            const queryList = {}
            if (queryType === 'task') {
                this.taskQueryList.page = page ?? 1
                Object.assign(queryList, this.taskQueryList)
            }
            if (queryType === 'agentTask') {
                this.agentDetailsTaskQueryList.page = page ?? 1
                Object.assign(queryList, this.agentDetailsTaskQueryList)
            }
            // 模拟数据
            // console.log('FuncMockJson()',);
            FuncMockJson().then((data) => {
                // console.log('Func', data)
                const { task = [] } = data
                if (queryType === 'task') {
                    this.taskList = task
                    const taskIndex = this.scrollbarTaskIndex ?? 0
                    this.handleTaskView(queryType, task[taskIndex], taskIndex)
                    // this.taskDetails = task.length ? task[0] : ''
                }
                if (queryType === 'agentTask') {
                    this.agentDetailsTaskList = task
                    const taskIndex = this.agentDetailsTaskIndex ?? 0
                    this.handleTaskView(queryType, task[taskIndex], taskIndex)
                    // this.agentDetailsTaskDetails = task.length ? task[0] : ''
                }
            })


            // to do 接口对接
            // return await serviceAM.post('', queryList)
            // .then(res => {
            //     if (res.data.success) {
            //         const taskData = res?.data?.data ?? []
            //         if (queryType === 'task') {
            //             this.taskList = taskData
            //             this.taskDetails = taskData.length ? taskData[0] : ''
            //         }
            //         if (queryType === 'agentTask') {
            //             this.agentDetailsTaskList = taskData
            //             this.agentDetailsTaskDetails = taskData.length ? taskData[0] : ''
            //         }
            //     } else {
            //         app.$message({
            //             message: res.data.errorMessage || res.data.data || 'Error',
            //             type: 'error',
            //             duration: 5 * 1000
            //         })
            //     }
            // })
        },
        // 保存 submitForm 数据
        async saveSubmitFormData(btnType, serviceForm = {}) {
            let serviceURL = ''
            if (btnType === 'agent' || btnType === 'agentDialog') {
                serviceURL = ''
            }
            if (btnType === 'group') {
                serviceURL = ''
            }
            if (btnType === 'groupStart') {
                serviceURL = ''
            }
            if (!serviceURL) return
            if (serviceForm.id) {
                serviceURL += `?id=${serviceForm.id}`
            }
            return await serviceAM.post(serviceURL, serviceForm)
                .then(res => {
                    if (res.data.success) {
                        this.visible[btnType] = false
                        if (btnType === 'agent') {
                            this.getAgentListData(1, 'agent')
                        }
                        if (btnType === 'agentDialog') {
                            this.getAgentListData(1, 'groupAgent')
                        }
                        if (btnType === 'group' || btnType === 'groupStart') {
                            this.getGroupListData(1, 'group')
                        }
                    } else {
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
                            type: 'error',
                            duration: 5 * 1000
                        })
                    }
                })
        }
    }
});

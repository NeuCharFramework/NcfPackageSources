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
            tabsActiveName: 'first', // first(智能体) second(组) third(任务)
            // 抽屉 显隐
            drawerVisible: {
                agent: false, // 智能体 新增|编辑
                group: false, // 组 新增
            },
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
            // 新增智能体
            agentForm: {
                name: '',
                systemMessageType: '1',
                systemMessageId: '',
                systemMessageInput: '',
                introduction: '',
                platform: '',
                parameter:'',
                avatar:''
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
                introduction: [
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
        };
    },
    computed: {
    },
    watch: {},
    created() {
    },
    mounted() {
        // 获取数据
        this.getAgentListData()
    },
    beforeDestroy() {

    },
    methods: {
        // 切换 tabs 页面
        handleTabsClick(tab, event) {
            console.log('切换菜单:', tab, event);
        },
        // 抽屉 打开 按钮 'agent' 'group'
        handleDrawerOpenBtn(btnType, id) {
            console.log('通用新增按钮:', btnType, id);
            // id 有值为编辑
            // if (id) {}
            this.drawerVisible[btnType] = true
        },
        // 抽屉 关闭 
        handleDrawerClose(btnType) {
            this.$confirm('确认关闭？')
                .then(_ => {
                    let formName = ''
                    if (btnType === 'agent') {
                        formName = 'agentELForm'
                    }
                    if (formName) {
                        this.$refs[formName].resetFields();
                    }
                    // this.$nextTick(() => {})
                    this.drawerVisible[btnType] = false
                })
                .catch(_ => { });
        },
        // 抽屉 提交
        handleDrawerSubmit(btnType) {
            let formName = ''
            if (btnType === 'agent') {
                formName = 'agentELForm'
            }
            if (!formName) {
                return
            }
            this.$refs[formName].validate((valid) => {
                if (valid) {
                    alert('submit!');
                    if (btnType === 'agent') {
                        // this.agentForm
                        // to do 调用接口
                    }
                    this.drawerVisible[btnType] = false
                } else {
                    console.log('error submit!!');
                    return false;
                }
            });
        },
        // 处理  筛选条件事件
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
            // 任务
        },
        // 获取 智能体 列
        getAgentListData() {
            // 模拟数据
            const _agentList = []
            for (let index = 0; index < 15; index++) {
                _agentList.push({
                    avatar: '',
                    name: '盛派设计师',
                    introduction: '资深设计师，精通平面设计和交互设计，能够根据项目需求进行全面的用户需求分析，确保设计方案不仅美观，同时也符合用户体验和功能需求。',
                    numberParticipants: '49',
                    score: '9.7',
                    state: 1 // 1\2\3
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
    }
});

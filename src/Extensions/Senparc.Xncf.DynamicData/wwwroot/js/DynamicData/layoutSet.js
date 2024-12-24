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
            layoutName: '', // 布局名称
            isEditLayoutName: false,// 布局名称是否 编辑
            // 布局菜单
            loadingAddMenu: false, // 新增菜单loading

            layoutMenuActive: 0, // 默认选中的菜单
            layoutComponentActive: '', // 默认选中的组件
            layoutComponentActiveType: '', // 组件 选中类型 table normalColumn customColumn columnEditBtn form formItem formSubmitBtn
            columnActiveIndex: '', // 列选中 index

            dragStarIndex: '', // 拖拽 开始 index
            dragEnterIndex: '', // 拖拽 结束 index

            layoutMenuList: [
                {
                    name: '菜单1',
                    isEditName: false,
                    layoutList: []
                }
            ],
            // 布局组件区域列表
            layoutComponentsList: [],

            // 侧边菜单 切换
            tabsOverallActiveName: 'first', //tabs 类别 添加:first 设置:second
            setUpActiveName: 'first', // 设置 tabs 类别 属性:first 样式:second
            // 数据表 的 列管理
            columnForm: {
                dataSheet: '',
                allChecked: false,
                // 是否选择checked 字段:field 类型:dataType 名称:name
                columnData: []
            },
            // 添加列表单
            addColumnVisible: false,
            addColumnForm: {},
            // 数据设置
            dataSetForm: {
                filterCriteriaLen: 0, // 筛选条件个数
                filterCriteria: [], // 筛选条件field symbol condition 
                sortingRules: [],// 排序规则
                allFilterChecked: true,
                filterFieldLen: 0,// 筛选字段 数量
                filterFieldList: [],// 筛选字段
                queryScope: '', // 字段查询范围
            },
            // 表格样式设置
            tableStyleForm: {
                stripe: false, // 是否 斑马纹
                border: false, // 是否 边框
                showHeader: true, // 是否显示表头
                height: '', // 表格高度 固定高度
                size: 'medium', // Table 的尺寸 medium / small / mini
                columnData: [] // 列数据
            },
            // 分页设置
            paginationForm: {
                enable: false, // 是否启用分页
                small: false, // 是否使用小型分页样式
                background: false, // 是否为分页按钮添加背景色
                hideOnSinglePage: false, // 只有一页时是否隐藏
                pagerCount: 7, // 设置最大页码按钮数 默认7
                pageSizes: [10, 20, 30], // 每页显示个数
                position: 'bottom-end', // 位置
                allCheckedLayout: false,
                layout: [{
                    name: 'total',
                    checked: false,
                }, {
                    name: 'sizes',
                    checked: false,
                }, {
                    name: 'prev',
                    checked: true,
                }, {
                    name: 'pager',
                    checked: true,
                }, {
                    name: 'next',
                    checked: true,
                }, {
                    name: 'jumper',
                    checked: false,
                }]
            },
            paginationFormRules: [],
            pageSizesOpt: [10, 20, 30, 40, 50, 100],
            positionClassOpt: {
                'top': 'flex-jc',
                'top-start': 'flex-js',
                'top-end': 'flex-je',
                'bottom': 'flex-jc',
                'bottom-start': 'flex-js',
                'bottom-end': 'flex-je'
            },
            pageSizesPositionOpt: ['top', 'top-start', 'top-end', 'bottom', 'bottom-start', 'bottom-end'],
            // 自定义列按钮 
            customColumnBtnForm: {
                name: '',
                property: '',
                type: '',
                disable: false,
                enterID: false,
                customIDName: false,
                btnEvent: '',
                componentType: '',
                executionMethod: '',
                interactive: '',
                page: '',
                popupForm: {
                    popupTitleText: '是否确认删除此条数据',
                    popupCancelText: '取消',
                    popupConfirmText: '确认',
                    popupPromptContent: '删除成功',
                    popupPromptOften: '1600', // ms
                    popupClosePage: '关闭当前页面'
                }
            },
            // 表格样式设置
            formStyleForm: {
                size: 'medium', // Table 的尺寸 medium / small / mini
                labelPosition: 'left', // left right top
                labelWidth: '100', // lable 狂顶
                // lableFontSize: '16',
                lableColor: '#333',
            },
            // input switch select time-select time-picker date-picker checkbox radio
            // 表单 组件
            formItemOpt: [
                {
                    label: '输入框',
                    value: 'input',
                    disabled: false,
                    children: [
                        {
                            label: '文本输入框',
                            value: 'text',
                            disabled: false,
                        },
                        {
                            label: '文本域输入框',
                            value: 'textarea',
                            disabled: false,
                        },
                        {
                            label: '数字输入框',
                            value: 'number',
                            disabled: false,
                        }
                    ]
                },
                {
                    label: '开关',
                    value: 'switch',
                    disabled: false,
                },
                {
                    label: '选择器',
                    value: 'select',
                    disabled: false,
                },
                {
                    label: '任意时间点选择器',
                    value: 'timePicker',
                    disabled: false,
                },
                // {
                //     label: '固定时间点选择器',
                //     value: 'timeSelect',
                //     disabled: false,
                // },
                {
                    label: '日期选择器',
                    value: 'date',
                    disabled: false,
                    children: [
                        // datetimerange/daterange/monthrange
                        {
                            label: '年份选择器',
                            value: 'year',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: '年份选择器-多选',
                            value: 'years',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: '月份选择器',
                            value: 'month',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: '月份选择器-多选',
                            value: 'months',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: '日期选择器',
                            value: 'date',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: '日期选择器-多选',
                            value: 'dates',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: '周选择器',
                            value: 'week',
                            format: 'yyyy 第 WW 周',
                            disabled: false,
                        },
                        {
                            label: '日期时间选择器',
                            value: 'datetime',
                            format: '',
                            disabled: false,
                        }

                    ]
                },
                {
                    label: '复选框',
                    value: 'checkbox',
                    disabled: false,
                },
                {
                    label: '单选框',
                    value: 'radio',
                    disabled: false,
                }
            ],
            // 表单 组件 类型
            formItemTypeOptChildren: [],
            // // 自定义列设置
            // customSetForm: {
            //     columnAttribute: '', //自定义列属性
            //     columnTitle: '',  //自定义列标题
            //     btnList: [], //按钮列表
            //     displayColumn: false,//显示列
            //     fixedColumn: true,//固定列
            // },
            // // 列属性设置
            // ColumnAttributeSetForm: {
            //     columnTitle: '',//列标题
            //     boundField: '',//绑定字段
            //     displayContent: '',//展示内容
            //     dataFormat: '',//数据格式
            //     colWidth: '',//列宽
            //     alignment: '',//对齐方式
            //     sortEnable: false,//允许排序
            //     exportEnable: false,//允许导出
            //     editEnable: false,//允许编辑
            //     deleteEnable: false,//允许删除
            // },
            // // 按钮设置
            formSubmitBtnForm: {
                buttonName: '',//按钮名称
                buttonAttribute: '',//按钮属性
                buttonType: '',//按钮类型
                disableId: false,//禁用
                bringinId: false,//带入id
                customIdname: false//自定义id名称
            },
            //事件设置
            eventSettingForm: {
                buttonEvent: '',//按钮事件
                moduleName: '',//组件名称
                mannerExecution: '',//执行方法
                selectInteraction: '',//选择交互
                selectPage: '',//选择页面
                calldataMethod: '',//选择数据源
            },
            // //表单属性设置
            // sheetSettingForm: {
            //     boundField: '',//绑定字段
            //     selectComponent: '',//选择组件
            //     fieldName: '',//字段名称
            //     tipText: '',//提示文字
            //     defaultValue: '',//默认值
            //     formType: '1',//类型
            //     mustFillin: '',//必填
            //     notReuse: '',//不允许重复输入
            //     finiteWord: '',//限制字数
            //     maxWord: '',//最大字数
            //     miniWord: '',//最小字数
            //     finiteFormat: '',//限定输入格式
            //     size: '1/4',//表单尺寸
            //     displayUsage: '',//显示方式
            //     dateType: '',//日期类型
            //     timeType: '',//时间类型
            //     dateFormat: '',//日期格式
            //     readOnly: '',//只读
            //     conCeal: '',//隐藏
            //     optionList: [], //选项列表
            //     customData: '',//自定义数据集
            // },
        };
    },
    computed: {
    },
    watch: {
        // table | form 的数据表和列 配置
        columnForm: {
            handler: function (val, oldVal) {
                // 选择组件 后变化后修改对应的
                if (isNumber(this.layoutComponentActive) && ['table', 'form'].includes(this.layoutComponentActiveType)) {
                    // 当前组件
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // 处理数据
                    const { dataSheet = '', columnData = [] } = val ?? {}
                    const _columnData = []
                    const tableMockItem = {}
                    const tableMockData = []
                    columnData.forEach(item => {
                        if (item.checked) {
                            _columnData.push({
                                columnkey: this.layoutComponentActive,
                                ...item
                            })
                            tableMockItem[item.field] = '模拟数据'
                        }
                    })

                    const newLayoutComponentItem = {
                        ...layoutComponentItem,
                        dataSheet,
                        columnData: _columnData,
                        columnConfig: {
                            ...val
                        }
                    }
                    // table 组件 增加模拟数据
                    if (this.layoutComponentActiveType === 'table') {
                        if (!isObjEmpty(tableMockItem)) {
                            tableMockData.push(tableMockItem)
                        }
                        newLayoutComponentItem.tableMockData = tableMockData
                        const tableStyleColumn = _columnData.map(item => {
                            if (item) {
                                item.checked = false
                                item.fixed = 'left'
                            }
                            return item
                        })
                        this.$set(this.tableStyleForm, 'columnData', tableStyleColumn)
                    }

                    this.setMeunOrComponentData(newLayoutComponentItem)
                }
            },
            // immediate: true,
            deep: true
        },
        dataSetForm: {
            handler: function (val, oldVal) {
                // 选择组件 后变化后修改对应的
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'table') {
                    // 当前组件
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // 处理数据
                    const newLayoutComponentItem = {
                        ...layoutComponentItem,
                        filterConfig: {
                            ...val
                        }
                    }
                    this.setMeunOrComponentData(newLayoutComponentItem)
                }
            },
            // immediate: true,
            deep: true
        },
        tableStyleForm: {
            handler: function (val, oldVal) {
                // 选择组件 后变化后修改对应的
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'table') {
                    // 当前组件
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // 处理数据
                    const { columnData = [] } = val ?? {}
                    const tableColumnFixed = {}
                    columnData.forEach(item => {
                        if (item.checked) {
                            tableColumnFixed[item.field] = item.fixed
                        }
                    })
                    const newLayoutComponentItem = {
                        ...layoutComponentItem,
                        tableColumnFixed,
                        tableConfig: {
                            ...val
                        }
                    }
                    this.setMeunOrComponentData(newLayoutComponentItem)
                }
            },
            // immediate: true,
            deep: true
        },
        paginationForm: {
            handler: function (val, oldVal) {
                // 选择组件 后变化后修改对应的
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'table') {
                    // 当前组件
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // 处理数据
                    const { layout = [] } = val ?? {}
                    const layoutStrArr = []
                    layout.forEach(item => {
                        if (item.checked) {
                            layoutStrArr.push(item.name)
                        }
                    })
                    const newLayoutComponentItem = {
                        ...layoutComponentItem,
                        pagConfig: {
                            ...val,
                            layoutStr: layoutStrArr.join(','),
                        }
                    }
                    this.setMeunOrComponentData(newLayoutComponentItem)
                }
            },
            // immediate: true,
            deep: true
        },
        // customColumnBtnForm:{},
        // table列|form列 选择侧边 配置
        addColumnForm: {
            handler: function (val, oldVal) {
                // 选择组件 和table列 后变化后修改对应的
                if (isNumber(this.layoutComponentActive) && isNumber(this.columnActiveIndex)) {
                    // 当前组件
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // 
                    const tableColumnItem = layoutComponentItem.columnData[this.columnActiveIndex]

                    // 处理数据
                    const newLayoutComponentItem = {
                        ...layoutComponentItem
                    }

                    newLayoutComponentItem.columnData[this.columnActiveIndex] = {
                        ...tableColumnItem,
                        ...val
                    }

                    // table 组件 )
                    // if (this.layoutComponentActiveType === 'table' ) {
                    // }
                    // if (this.layoutComponentActiveType === 'form') {
                    // }

                    this.setMeunOrComponentData(newLayoutComponentItem)
                }
            },
            // immediate: true,
            deep: true
        },
        // formStyleForm 表单样式
        formStyleForm: {
            handler: function (val, oldVal) {
                // 选择组件 后变化后修改对应的
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'form') {
                    // 当前组件
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // 处理数据
                    const newLayoutComponentItem = {
                        ...layoutComponentItem,
                        formConfig: {
                            ...val
                        }
                    }
                    this.setMeunOrComponentData(newLayoutComponentItem)
                }
            },
            // immediate: true,
            deep: true
        },
        formSubmitBtnForm: {
            handler: function (val, oldVal) {
                // 选择组件 后变化后修改对应的
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'form') {
                    // 当前组件
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // 处理数据
                    const newLayoutComponentItem = {
                        ...layoutComponentItem,
                        submitBtnConfig: {
                            ...val
                        }
                    }
                    this.setMeunOrComponentData(newLayoutComponentItem)
                }
            },
            // immediate: true,
            deep: true
        },
        eventSettingForm: {
            handler: function (val, oldVal) {
                // 选择组件 后变化后修改对应的
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'form') {
                    // 当前组件
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // 处理数据
                    const newLayoutComponentItem = {
                        ...layoutComponentItem,
                        eventSetConfig: {
                            ...val
                        }
                    }
                    this.setMeunOrComponentData(newLayoutComponentItem)
                }
            },
            // immediate: true,
            deep: true
        },
    },
    created() {
    },
    mounted() {
        // 添加页面 点击事件监听
        // document.addEventListener('click',()=>{});
    },
    beforeDestroy() {
        // 移除页面 点击事件监听
        // document.removeEventListener('click',()=>{})
    },
    methods: {
        // 编辑区域 组件占位显隐判断
        componentOccupantShow(item) {
            if (!item.componentType) return true
            if (item.componentType === 'table') {
                return (!item.columnData || !item.columnData.length) && (!item.pagConfig || !item.pagConfig.enable)
            }
            if (item.componentType === 'form') {
                return !item.columnData || !item.columnData.length
            }
            if (item.componentType === 'btn') {
                return false
            }
        },
        // 侧边 添加组件按钮 禁用判断
        addAssembBtnDisabled(addType) {
            if (isNumber(this.layoutComponentActive)) {
                const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                if (layoutComponentItem && layoutComponentItem.componentType) {
                    return addType !== layoutComponentItem.componentType
                }
                return false
            }
            return true
        },
        // 设置菜单和组件列表的值
        setMeunOrComponentData(newComponent) {
            // 修改对应组件
            this.$set(this.layoutComponentsList, this.layoutComponentActive, deepClone(newComponent))
            // 当前菜单
            const menuItem = this.layoutMenuList[this.layoutMenuActive]
            menuItem.layoutList = deepClone(this.layoutComponentsList)
            // 修改对应菜单
            this.$set(this.layoutMenuList, this.layoutMenuActive, deepClone(menuItem))
        },
        // 处理 Subsection 数据更新
        handleSubsectionUpdate(dataName, upData) {
            // 目前 column选中时 subsection 不切换
            if (this.columnActiveIndex !== '') {
                return
            }
            if (!dataName) {
                return
            }
            try {
                if (dataName.includes('.')) {
                    const dataNameArr = dataName.split('.')
                    this.$set(this[dataNameArr[0]], `${dataNameArr[1]}`, upData)
                } else {
                    this.$set(this, `${dataName}`, upData)
                    // this[dataName] = upData
                }
            } catch (error) {
                console.log('handleSubsectionUpdate:', error);

            }
        },
        // 拖拽修改排序 开始
        handleDragstart(index) {
            this.dragStarIndex = index
        },
        // 拖拽修改排序 结束
        handleDragenter(e, index, dragType) {
            e.preventDefault();
            this.dragEnterIndex = index
            if (dragType === 'layoutComponent') {
                this.layoutComponentActive = index
            }
            debounce(() => {
                if (this.dragStarIndex !== index) {
                    if (dragType === 'layoutComponent') {
                        const source = this.layoutComponentsList[this.dragStarIndex]
                        if (source.componentType && source.componentType === 'table') {
                            source?.columnData?.forEach(item => {
                                item.columnkey = index
                            })
                        }
                        // 切换位置
                        this.layoutComponentsList.splice(this.dragStarIndex, 1)
                        this.layoutComponentsList.splice(index, 0, source)
                    }
                    if (dragType === 'column') {
                        const source = this.columnForm.columnData[this.dragStarIndex]
                        // 切换位置
                        this.columnForm.columnData.splice(this.dragStarIndex, 1)
                        this.columnForm.columnData.splice(index, 0, source)
                    }
                    if (dragType === 'customColumnBtn') {
                        const source = this.addColumnForm.btnList[this.dragStarIndex]
                        // 切换位置
                        this.addColumnForm.btnList.splice(this.dragStarIndex, 1)
                        this.addColumnForm.btnList.splice(index, 0, source)
                    }
                    if (dragType === 'sortingRules') {
                        const source = this.dataSetForm.sortingRules[this.dragStarIndex]
                        this.dataSetForm.sortingRules.splice(this.dragStarIndex, 1)
                        this.dataSetForm.sortingRules.splice(index, 0, source)
                    }
                    // 排序变化后目标对象的索引变成源对象的索引
                    this.dragStarIndex = index;
                }
            }, 100)()
        },
        // 拖拽修改排序 移动
        handleDragover(e) {
            e.preventDefault();
        },
        // 处理 选中组件事件
        handleSelectComponent(item, index = '') {
            // if (this.layoutComponentActive === index) return
            this.layoutComponentActive = index
            this.layoutComponentActiveType = item.componentType ?? ''
            this.columnActiveIndex = '' // 列选中 index
            // 重置 中间|右侧 配置
            this.resetItemsConfig()
            this.resetAddColumnForm()
            // 数据回显
            if (item.componentType === 'table') {
                const { columnConfig, filterConfig, tableConfig, pagConfig } = item
                if (columnConfig) {
                    this.columnForm = deepClone(columnConfig)  // 列管理
                }
                if (filterConfig) {
                    this.dataSetForm = deepClone(filterConfig) // 数据设置
                }
                if (tableConfig) {
                    this.tableStyleForm = deepClone(tableConfig) // 表格样式设置
                }
                if (pagConfig) {
                    this.paginationForm = deepClone(pagConfig)  // 分页设置
                }
            }
            if (item.componentType === 'form') {
                const { columnConfig, formConfig, submitBtnConfig, eventSetConfig } = item
                if (columnConfig) {
                    this.columnForm = deepClone(columnConfig)  // 列管理
                }
                if (formConfig) {
                    this.formStyleForm = deepClone(formConfig)  // 样式
                }
                if (submitBtnConfig) {
                    this.formSubmitBtnForm = deepClone(submitBtnConfig)  // 提交按钮
                }
                if (eventSetConfig) {
                    this.eventSettingForm = deepClone(eventSetConfig)  // 提交按钮事件
                }
            }
            if (item.componentType === 'btn') { }
            this.tabsOverallActiveName = 'first' // tabs 类别 添加:first 设置:second
            this.setUpActiveName = 'first' // 属性:first 样式:second
        },
        // 添加 组件
        handleAddComponent(addType = '') {
            let addComponentObj = {
                name: '',
                componentType: addType
            }
            const addTypeList = ['table', 'form', 'btn']
            if (addTypeList.includes(addType)) {
                if (this.addAssembBtnDisabled()) return
                // 重置 中间|右侧 配置
                this.resetItemsConfig()
                // tabs 类别切换
                this.tabsOverallActiveName = 'second'  // 添加:first 设置:second
                this.setUpActiveName = 'first' // 属性:first 样式:second
                // 添加 组件 数据类型
                const currentItem = this.layoutComponentsList[this.layoutComponentActive]
                currentItem.componentType = addType

                this.$set(this.layoutComponentsList, this.layoutComponentActive, currentItem)
                this.layoutComponentActiveType = addType
                this.resetAddColumnForm()
            } else if (isNumber(addType) && Number(addType) < this.layoutComponentsList.length) {
                this.layoutComponentsList.splice(Number(addType), 0, addComponentObj)
            } else {
                this.layoutComponentsList.push(addComponentObj)
            }
            // 当前菜单
            const menuItem = this.layoutMenuList[this.layoutMenuActive]
            menuItem.layoutList = deepClone(this.layoutComponentsList)
            // 修改对应菜单 数据
            this.$set(this.layoutMenuList, this.layoutMenuActive, deepClone(menuItem))
        },
        // 重置各项 配置
        resetItemsConfig() {
            this.dragStarIndex = '' // 组件区域 拖拽 开始 index
            this.dragEnterIndex = '' // 组件区域 拖拽 结束 index

            // 重置 table this.$options.data.call(this)
            this.columnForm = this.$options.data().columnForm // 列管理
            this.dataSetForm = this.$options.data().dataSetForm // 数据设置
            this.tableStyleForm = this.$options.data().tableStyleForm // 表格样式设置
            this.paginationForm = this.$options.data().paginationForm // 分页设置
            this.addColumnForm = this.$options.data().addColumnForm // 添加列
            this.customColumnBtnForm = this.$options.data().customColumnBtnForm // 自定义列添加按钮

            // 重置 form
            this.formStyleForm = this.$options.data().formStyleForm
            this.formItemTypeOptChildren = []
        },

        // 代码查看
        handleCodeView() { },
        // 预览
        handlePreview() { },
        // 发布
        handleRelease() { },

        // 布局名称 编辑
        handleEditLayoutName() {
            this.isEditLayoutName = true
            // 布局名称 输入框 聚焦
            this.$nextTick(() => {
                this.$refs.inputLayoutName.focus()
            })
        },
        // 布局名称 失去焦点
        handleBlurLayoutName() {
            this.isEditLayoutName = false
        },

        // 新增菜单
        handleAddMenu() {
            if (this.loadingAddMenu) return
            this.loadingAddMenu = true
            this.layoutMenuList.push({
                name: `菜单${this.layoutMenuList.length + 1}`,
                isEditName: false,
                layoutList: []
            })
            this.loadingAddMenu = false
        },
        // 切换菜单
        handleSwitchMenu(index) {
            this.layoutMenuActive = index // 选中的菜单
            this.layoutComponentActive = '' // 选中的组件
            this.layoutComponentActiveType = '' // 组件 选中类型
            this.columnActiveIndex = '' // 列选中 index
            // 布局组件区域列表
            this.layoutComponentsList = this.layoutMenuList[index]?.layoutList ?? []
            // 切换 tabs 类别  
            this.tabsOverallActiveName = 'first' // 添加:first 设置:second
            this.setUpActiveName = 'first' // 属性:first 样式:second
            // 重置 中间|右侧 配置
            this.resetItemsConfig()
        },
        // 修改菜单名称
        handleEditMenuName(item, index) {
            item.isEditName = true
            // 菜单名称 输入框 聚焦
            this.$nextTick(() => {
                // console.log('ref', this.$refs)
                this.$refs.inputMenuName[0].focus()
            })
        },
        // 修改菜单名称 失去焦点
        handleBlurMenuName(item) {
            item.isEditName = false
        },



        // table 组件 renderHeader
        renderHeader(createElement, { column, $index }) {
            return createElement(
                'div', {
                'class': ['thead-cell'],
                on: {
                    mousedown: ($event) => { this.handleTableHeaderMouseDown($event, column, $index) }
                }
            }, [
                // 添加 <a> 用于显示表头 label
                createElement('a', column.label),
                // 添加一个空标签用于显示拖动动画
                // createElement('span', {
                //     'class': ['virtual']
                // })
            ])
        },
        // table 组件 列 鼠标按下
        handleTableHeaderMouseDown(e, column, columnIndex) {
            e.preventDefault();
            this.resetItemsConfig()
            this.layoutComponentActive = column.columnKey
            // column.columnKey
            const currentComponentItem = this.layoutComponentsList[column.columnKey]
            // 数据 回显
            const selectColumnForm = currentComponentItem.columnConfig.columnData[columnIndex]

            this.$set(this, 'addColumnForm', deepClone(selectColumnForm))
            // tabs 切换
            this.columnActiveIndex = columnIndex
            this.layoutComponentActiveType = selectColumnForm.property
            this.tabsOverallActiveName = 'second'  // 添加:first 设置:second
            this.handleSubsectionUpdate('setUpActiveName', 'first')
        },
        // table 组件 选中 heade列样式
        headerCellClassName({ row, column, columnIndex }) {
            if (this.layoutComponentActive === column.columnKey) {
                return this.columnActiveIndex === columnIndex ? 'column_active_th' : ''
            }
            return ''
        },
        // table 组件 选中 heade列样式
        cellClassName({ row, column, columnIndex }) {
            if (this.layoutComponentActive === column.columnKey) {
                return this.columnActiveIndex === columnIndex ? 'column_active_td' : ''
            }
            return ''
        },

        // 表单 item 列 鼠标按下
        handleFormItemMouseDown(index, fIndex) {
            // 重置 配置
            this.resetItemsConfig()
            this.layoutComponentActive = index
            this.columnActiveIndex = fIndex // 选中列
            this.layoutComponentActiveType = 'formItem' // 选中组件类型

            const currentComponentItem = this.layoutComponentsList[index]
            // 数据 回显
            const selectColumnForm = currentComponentItem.columnData[fIndex]

            this.$set(this, 'addColumnForm', deepClone(selectColumnForm))
            // tabs 切换
            this.tabsOverallActiveName = 'second'  // 添加:first 设置:second
            this.handleSubsectionUpdate('setUpActiveName', 'first')
        },

        // 切换 tabs
        handleTabsLeave(activeName, oldActiveName) {
            // 没有选中的菜单则禁用 
            // this.layoutComponentActiveType  ['customColumn','normalColumn']
            if (activeName === 'second' && !isNumber(this.layoutComponentActive)) {
                return false
            }
            return true
        },
        // 右侧Tabs 整体组件 添加 设置
        handleTabsClickOverall(tab, event) {
            // console.log(tab, event);
        },

        // 确认添加 列
        onConfirmAddColumn() {
            const copyAddColumnForm = deepClone(this.addColumnForm)
            // 自定义列 增加按钮列表
            if (copyAddColumnForm.property === 'customColumn') {
                copyAddColumnForm.btnList = []
            }
            this.columnForm.columnData.push(copyAddColumnForm)
            this.addColumnVisible = false
            this.resetAddColumnForm()
        },
        // 重置添加列
        resetAddColumnForm() {
            // 选择组件类型为 table 时
            if (this.layoutComponentActiveType === 'table') {
                this.addColumnForm = {
                    property: '', // 列属性
                    name: '', // 列标题
                    field: '', // 绑定字段
                    displayContent: '', // 展示内容
                    dataType: '', // 数据格式
                    columnWidth: '', // 列宽
                    alignment: '', // 对齐方式
                    isSort: false, // 允许排序
                    isExport: false, // 允许导出
                    isEdit: false, // 允许编辑
                    isDelete: false, // 允许删除
                }
            }
            // 选择组件类型为 form 时 设置基础信息
            if (this.layoutComponentActiveType === 'form') {
                this.addColumnForm = {
                    field: '', // 绑定字段
                    formItemType: '', // 表单 组件类别 默认是input switch select time date checkbox radio
                    type: '', // 组件类型
                    name: '', // 列标题
                    displayContent: '', // 展示内容
                    columnWidth: 100, // 列宽
                    optionDataType: '',
                    dataSheet: '',
                    optionData: [],
                    value: '', // 值 可设置默认值
                }
            }
        },
        // 添加列 表单 单个组件类型切换处理
        handleFormItemTypeChange(val) {
            this.formItemTypeOptChildren = this.formItemOpt.find(item => {
                return item.value === val
            })?.children ?? []
            // 'checkbox' 'radio' 'select'

            if (['checkbox', 'radio', 'select'].includes(val)) {
                Object.assign(this.addColumnForm, {
                    optionData: []
                });
            } else if (this.addColumnForm.optionData) {
                delete this.addColumnForm.optionData
            }

            // timeSelect pickerOptions
        },
        // form 选择
        // 列 table 全选
        handleColumnAllCheckedChange(val) {
            this.columnForm.columnData.forEach(item => {
                item.checked = val
            })
            this.dataSetForm.filterFieldList = val ? deepClone(this.columnForm.columnData) : []
            this.dataSetForm.filterFieldLen = val ? this.dataSetForm.filterFieldList.length : 0
        },
        // 列 table 选择
        handleColumnCheckedChange(val) {
            const isAllChecked = this.columnForm.columnData.every(item => {
                return item.checked
            })
            this.columnForm.allChecked = isAllChecked
            const filterFieldList = this.columnForm.columnData.filter(item => item.checked)
            this.dataSetForm.filterFieldList = deepClone(filterFieldList)
            this.dataSetForm.filterFieldLen = filterFieldList.length
        },

        // 添加 筛选条件
        handleAddFilterCondition() {
            this.dataSetForm.filterCriteria.push({
                id: this.dataSetForm.filterCriteriaLen + 1,
                field: '',
                symbol: '',
                condition: ''
            })
            this.dataSetForm.filterCriteriaLen += 1
        },
        // 删除 筛选条件
        handleDeleteFilterCondition(index) {
            this.dataSetForm.filterCriteria.splice(index, 1);
            this.dataSetForm.filterCriteriaLen -= 1
        },
        // 添加 排序规则
        handleAddSortingRules() {
            this.dataSetForm.sortingRules.push({
                field: '',
                sort: '升序'
            })
        },
        // 删除 排序规则
        handleDeleteSortingRules(index) {
            this.dataSetForm.sortingRules.splice(index, 1);
        },

        // 筛选器 table 全选
        handleFilterAllCheckedChange(val) {
            this.dataSetForm.filterFieldList.forEach(item => {
                item.checked = val
            })
            this.dataSetForm.filterFieldLen = val ? this.dataSetForm.filterFieldList.length : 0
        },
        // 筛选器 table 选择
        handleFilterCheckedChange(val) {
            let checkedLen = 0
            const isAllChecked = this.dataSetForm.filterFieldList.every(item => {
                if (item.checked) {
                    checkedLen += 1
                }
                return item.checked
            })
            this.dataSetForm.allFilterChecked = isAllChecked
            this.dataSetForm.filterFieldLen = checkedLen
        },
        // 分页 全选
        handlePaginationAllCheckedChange(val) {
            this.paginationForm.layout.forEach(item => {
                item.checked = val
            })
        },
        // 分页 选择
        handlePaginationCheckedChange(val) {
            const isAllChecked = this.paginationForm.layout.every(item => {
                return item.checked
            })
            this.paginationForm.allCheckedLayout = isAllChecked
        },

        // table 自定义列 添加按钮
        handleAddCustomColumnBtn() {
            this.addColumnForm.btnList.push({
                name: '',
                property: '',
                type: '',
                disable: false,
                enterID: false,
                customIDName: false,
                btnEvent: '',
                componentType: '',
                executionMethod: '',
                interactive: '',
                page: '',
                popupForm: {
                    popupTitleText: '是否确认删除此条数据',
                    popupCancelText: '取消',
                    popupConfirmText: '确认',
                    popupPromptContent: '删除成功',
                    popupPromptOften: '1600', // ms
                    popupClosePage: '关闭当前页面'
                }
            })
        },
        // table 自定义列 删除按钮
        handleDeleteCustomColumnBtn(index) {
            this.addColumnForm.btnList.splice(index, 1);
        },

        // table 自定义列 按钮（选中） 侧边属性编辑
        customColumnBtnSelect(item) {
            this.layoutComponentActiveType = 'columnEditBtn'
            this.customColumnBtnForm = {
                ...item
            }
        },

        // form 列 添加 optionData
        handleAddOptionData() {
            this.addColumnForm?.optionData?.push({
                label: "",
                value: ""
            })
        },
        // form 列 删除 optionData
        handleDeleteOptionData(index) {
            this.addColumnForm?.optionData?.splice(index, 1);
        },
        // form 提交 按钮
        handleSubmitBtnEdit(item) {
            this.layoutComponentActiveType = 'formSubmitBtn'
            if (item.submitBtnConfig) {
                this.formSubmitBtnForm = {
                    ...item.submitBtnConfig
                }
            }
            if (item.eventSetConfig) {
                this.eventSettingForm = {
                    ...item.eventSetConfig
                }
            }
        },
        // //切换宽度占比
        // handleSwitchSubformstyle(field, value) {
        //     console.log(111, field, value)
        //     this.$set(this.sheetSettingForm, field, value);
        //     console.log(this.sheetSettingForm.size)
        // },
        // // 添加 按钮列表
        // handleAddBtnList() {
        //     this.customSetForm.btnList.push({
        //         btntext: '',

        //     })
        // },
        // // 删除 按钮列表
        // handleDeleteBtnList(index) {
        //     this.customSetForm.btnList.splice(index, 1);
        // },
    }
});

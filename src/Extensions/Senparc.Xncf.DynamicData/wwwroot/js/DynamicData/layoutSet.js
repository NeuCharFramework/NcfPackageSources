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
            layoutMenuList: [
                {
                    name: '菜单1',
                    isEditName: false,
                    layoutList: []
                }
            ],
            // 布局组件区域列表
            layoutComponentsList: [],
            tableColumnActive: '', // table 列选中
            // 侧边菜单 切换
            tabsOverallActiveName: 'first', //tabs 类别 添加:first 设置:second
            setUpActiveName: 'first', // 设置 tabs 类别 属性:first 样式:second
            // 列管理
            columnForm: {
                dataSheet: '',
                allChecked: false,
                // 是否选择checked 字段:field 类型:dataType 名称:name
                columnData: []
            },
            columnFormRules: [],
            columnDragStarIndex: '', // 拖拽 开始 index
            columnDragEnterIndex: '', // 拖拽 结束 index
            // 添加列表单
            addColumnVisible: false,
            addColumnForm: {
                property: '',
                name: '',
                field: '',
                displayContent: '',
                dataType: '',
                columnWidth: '',
                alignment: '',
                isSort: false,
                isExport: false,
                isEdit: false,
                isDelete: false,
            },
            addColumnFormRules: [],
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
            dataSetFormRules: [],
            sortingRulesDragStarIndex: '', // 排序规则拖拽 开始 index
            sortingRulesDragEnterIndex: '',// 排序规则拖拽 结束 index
            // 表格样式设置
            tableStyleForm: {
                stripe: false, // 是否 斑马纹
                border: false, // 是否 边框
                showHeader: true, // 是否显示表头
                height: '', // 表格高度 固定高度
                size: 'medium', // Table 的尺寸 medium / small / mini
                columnData: [] // 列数据
            },
            tableStyleFormRules: [],
            // 分页设置
            paginationForm: {
                enable: false, // 是否启用分页
                small: false, // 是否使用小型分页样式
                background: false, // 是否为分页按钮添加背景色
                hideOnSinglePage: false, // 只有一页时是否隐藏
                pagerCount: '', // 设置最大页码按钮数 默认7
                pageSizes: [], // 每页显示个数
                position: '', // 位置
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
            pageSizesPositionOpt: ['top', 'top-start', 'top-end', 'bottom', 'bottom-start', 'bottom-end'],
        };
    },
    computed: {
    },
    watch: {
        columnForm: {
            handler: function (val, oldVal) {
                // 选择组件 后变化后修改对应的
                if (isNumber(this.layoutComponentActive)) {
                    // 当前组件
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // 处理数据
                    const { dataSheet = '', columnData = [] } = val ?? {}
                    const tableColumn = []
                    const tableMockData = []
                    const tableMockItem = {}
                    columnData.forEach(item => {
                        if (item.checked) {
                            tableColumn.push({
                                columnkey: this.layoutComponentActive,
                                ...item
                            })
                            tableMockItem[item.field] = '模拟数据'
                        }
                    })
                    if (!isObjEmpty(tableMockItem)) {
                        tableMockData.push(tableMockItem)
                    }

                    const newLayoutComponentItem = {
                        ...layoutComponentItem,
                        dataSheet,
                        tableMockData,
                        tableColumn,
                        columnConfig: {
                            ...val
                        }
                    }

                    const tableStyleColumn = tableColumn.map(item=>{
                        if (item){
                            item.checked = false
                            item.fixed = 'left'
                        }
                        return item
                    })

                    this.$set(this.tableStyleForm,'columnData',tableStyleColumn)
                    this.setMeunOrComponentData(newLayoutComponentItem)
                }
            },
            // immediate: true,
            deep: true
        },
        dataSetForm: {
            handler: function (val, oldVal) {
                // 选择组件 后变化后修改对应的
                if (isNumber(this.layoutComponentActive)) {
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
                if (isNumber(this.layoutComponentActive)) {
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
                if (isNumber(this.layoutComponentActive)) {
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
    },
    created() {
    },
    mounted() {

    },
    beforeDestroy() {

    },
    methods: {
        // 添加组件按钮 禁用
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
            // 当前选中的菜单
            this.layoutMenuActive = index
            // 当前选中的组件
            this.layoutComponentActive = ''
            // 布局组件区域列表
            this.layoutComponentsList = this.layoutMenuList[index]?.layoutList ?? []
            // 重置 中间|右侧 配置
            this.resetItemsConfig()
        },
        // 修改菜单名称
        handleEditMenuName(item, index) {
            item.isEditName = true
            //this.$set(this.layoutMenuList, index, {
            //    ...item,
            //    isEditName:true
            //})
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

        // 重置各项 配置
        resetItemsConfig() {
            this.$options.data.call(this).columnForm // 列管理
            this.columnDragStarIndex = '' // 拖拽 开始 index
            this.columnDragEnterIndex = '' // 拖拽 结束 index
            this.$options.data.call(this).dataSetForm // 数据设置
            this.sortingRulesDragStarIndex = '' // 排序规则拖拽 开始 index
            this.sortingRulesDragEnterIndex = '' // 排序规则拖拽 结束 index
            this.$options.data.call(this).tableStyleForm // 表格样式设置
            this.$options.data.call(this).paginationForm // 分页设置
            this.$options.data.call(this).addColumnForm // 添加列
            
        },
        // 处理 选中组件事件
        handleSelectComponent(item, index = '') {
            this.tableColumnActive = ''
            if(this.layoutComponentActive === index) return
            // 重置 中间|右侧 配置
            this.resetItemsConfig()
            this.layoutComponentActive = index
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
            if (item.componentType === 'form') { }
            if (item.componentType === 'btn') { }
            this.tabsOverallActiveName = 'first' // tabs 类别 添加:first 设置:second
        },
        // table 组件 renderHeader
        renderHeader(createElement, { column, $index }) {
            return createElement(
                'div', {
                'class': ['thead-cell'],
                on: {
                    mousedown: ($event) => { this.handleMouseDown($event, column, $index) }
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
        handleMouseDown(e, column, columnIndex) {
            e.preventDefault();
            // column.columnKey
            const currentComponentItem = this.layoutComponentsList[column.columnKey]
            // 数据 回显
            const addColumnForm = currentComponentItem.columnConfig.columnData[columnIndex]

            this.$set(this,'addColumnForm',deepClone(addColumnForm))
            // tabs 切换
            this.tableColumnActive = columnIndex
            this.tabsOverallActiveName = 'second'  // 添加:first 设置:second
            this.handleSwitchSubsection('setUpActiveName','first')
        },
        // table 组件 选中 heade列样式
        headerCellClassName({ row, column, columnIndex }) {
            if (this.layoutComponentActive === column.columnKey) {
                return this.tableColumnActive === columnIndex ? 'column_active_th' : ''
            }
            return ''
        },
        // table 组件 选中 heade列样式
        cellClassName({ row, column, columnIndex }) {
            if (this.layoutComponentActive === column.columnKey) {
                return this.tableColumnActive === columnIndex ? 'column_active_td' : ''
            }
            return ''
        },

        // 切换 tabs
        handleTabsLeave(activeName, oldActiveName) {
            // 没有选中的菜单则禁用
            if (activeName === 'second' && !isNumber(this.layoutComponentActive)) {
                return false
            }
            return true
        },
        // 右侧Tabs 整体组件 添加 设置
        handleTabsClickOverall(tab, event) {
            // console.log(tab, event);
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

        // 切换设置类别
        handleSwitchSubsection(dataName, upData) {
            if (!dataName || this.tableColumnActive !== '') {
                return
            }

            if (dataName.includes('.')) {
                const dataNameArr = dataName.split('.')
                this.$set(this[dataNameArr[0]], `${dataNameArr[1]}`, upData)
            } else {
                this[dataName] = upData
            }
        },
        // 添加列
        handleAddColumn() {
            this.columnForm.columnData.push({
                checked: false,
                iscustom: true,
                field: `customColumn`,
                name: '自定义列',
                dataType: ''
            })
        },
        // 确认添加 列
        onConfirmAddColumn() {
            const copyAddColumnForm = JSON.parse(JSON.stringify(this.addColumnForm))
            this.columnForm.columnData.push(copyAddColumnForm)
            this.addColumnForm = {
                property: '',
                name: '',
                field: '',
                displayContent: '',
                dataType: '',
                columnWidth: '',
                alignment: '',
                isSort: false,
                isExport: false,
                isEdit: false,
                isDelete: false,
            }
            this.addColumnVisible = false
        },
        // 列 table 全选
        handleColumnAllCheckedChange(val) {
            this.columnForm.columnData.forEach(item => {
                item.checked = val
            })
            this.dataSetForm.filterFieldList = val ? JSON.parse(JSON.stringify(this.columnForm.columnData)) : []
            this.dataSetForm.filterFieldLen = val ? this.dataSetForm.filterFieldList.length : 0
        },
        // 列 table 选择
        handleColumnCheckedChange(val) {
            const isAllChecked = this.columnForm.columnData.every(item => {
                return item.checked
            })
            this.columnForm.allChecked = isAllChecked
            const filterFieldList = this.columnForm.columnData.filter(item => item.checked)
            this.dataSetForm.filterFieldList = JSON.parse(JSON.stringify(filterFieldList))
            this.dataSetForm.filterFieldLen = filterFieldList.length
        },
        // 列拖拽 开始
        columnDragstart(index) {
            //console.log('start index ===>>> ', index)
            this.columnDragStarIndex = index
        },
        // 列拖拽 结束
        columnDragenter(e, index) {
            e.preventDefault();
            //console.log('end index ===>>> ', index)
            this.columnDragEnterIndex = index
            debounce(() => {
                if (this.columnDragStarIndex !== index) {
                    const source = this.columnForm.columnData[this.columnDragStarIndex]
                    this.columnForm.columnData.splice(this.columnDragStarIndex, 1)
                    this.columnForm.columnData.splice(index, 0, source)
                    // 排序变化后目标对象的索引变成源对象的索引
                    this.columnDragStarIndex = index;
                }
                //console.log('结果 columnData', this.columnForm.columnData)
            }, 100)()
        },
        // 列拖拽 移动
        columnDragover(e, index) {
            e.preventDefault();
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
        // 列拖拽 开始
        sortingRulesDragstart(index) {
            //console.log('start index ===>>> ', index)
            this.sortingRulesDragStarIndex = index
        },
        // 列拖拽 结束
        sortingRulesDragenter(e, index) {
            e.preventDefault();
            //console.log('end index ===>>> ', index)
            this.sortingRulesDragEnterIndex = index
            debounce(() => {
                if (this.sortingRulesDragStarIndex !== index) {
                    const source = this.dataSetForm.sortingRules[this.sortingRulesDragStarIndex]
                    this.dataSetForm.sortingRules.splice(this.sortingRulesDragStarIndex, 1)
                    this.dataSetForm.sortingRules.splice(index, 0, source)
                    // 排序变化后目标对象的索引变成源对象的索引
                    this.sortingRulesDragStarIndex = index;
                }
                //console.log('结果 columnData', this.dataSetForm.sortingRules)
            }, 100)()
        },
        // 列拖拽 移动
        sortingRulesDragover(e, index) {
            e.preventDefault();
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
    }
});
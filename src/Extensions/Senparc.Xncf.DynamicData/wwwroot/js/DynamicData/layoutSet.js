/**
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
var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com',
            elSize: 'medium', // el component size defaults to empty medium, small, mini
            layoutName: '', // layout name
            isEditLayoutName: false,// Whether the layout name is Edit
            // layout menu
            loadingAddMenu: false, // Added menu loading

            layoutMenuActive: 0, // Default selected menu
            layoutComponentActive: '', // Components selected by default
            layoutComponentActiveType: '', // Component selected type table normalColumn customColumn columnEditBtn form formItem formSubmitBtn
            columnActiveIndex: '', // Column selected index

            dragStarIndex: '', // Drag and drop to start index
            dragEnterIndex: '', // Drag to end index

            layoutMenuList: [
                {
                    name: '菜单1',
                    isEditName: false,
                    layoutList: []
                }
            ],
            // Layout component area list
            layoutComponentsList: [],

            // Side menu toggle
            tabsOverallActiveName: 'first', //tabs category add:first set:second
            setUpActiveName: 'first', // Set tabs category attribute: first style: second
            // Data table column management
            columnForm: {
                dataSheet: '',
                allChecked: false,
                // Whether to select checked field: field type: dataType name: name
                columnData: []
            },
            // Add list
            addColumnVisible: false,
            addColumnForm: {},
            // Data settings
            dataSetForm: {
                filterCriteriaLen: 0, // Number of filter conditions
                filterCriteria: [], // filter conditionfield symbol condition 
                sortingRules: [],// Sorting rules
                allFilterChecked: true,
                filterFieldLen: 0,// Filter field quantity
                filterFieldList: [],// Filter fields
                queryScope: '', // Field query range
            },
            // Table style settings
            tableStyleForm: {
                stripe: false, // Whether zebra print
                border: false, // Whether border
                showHeader: true, // Whether to display header
                height: '', // Table height fixed height
                size: 'medium', // Table size medium / small / mini
                columnData: [] // column data
            },
            // Pagination settings
            paginationForm: {
                enable: false, // Whether to enable paging
                small: false, // Whether to use small pagination style
                background: false, // Whether to add a background color to the paging button
                hideOnSinglePage: false, // Whether to hide when there is only one page
                pagerCount: 7, // Set the maximum number of page number buttons, default 7
                pageSizes: [10, 20, 30], // Display number per page
                position: 'bottom-end', // Location
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
            // Custom column buttons 
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
            // Table style settings
            formStyleForm: {
                size: 'medium', // Table size medium / small / mini
                labelPosition: 'left', // left right top
                labelWidth: '100', // lable Crazy support
                // lableFontSize: '16',
                lableColor: '#333',
            },
            // input switch select time-select time-picker date-picker checkbox radio
            // form component
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
                //     label: 'Fixed time point selector',
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
            // form component type
            formItemTypeOptChildren: [],
            // // Custom column settings
            // customSetForm: {
            //     columnAttribute: '', //custom column attributes
            //     columnTitle: '', //custom column title
            //     btnList: [], //Button list
            //     displayColumn: false,//display column
            //     fixedColumn: true,//Fixed column
            // },
            // //Column attribute settings
            // ColumnAttributeSetForm: {
            //     columnTitle: '',//column title
            //     boundField: '',//Binding field
            //     displayContent: '',//display content
            //     dataFormat: '',//data format
            //     colWidth: '',//column width
            //     alignment: '',//alignment
            //     sortEnable: false,//Allow sorting
            //     exportEnable: false,//Allow export
            //     editEnable: false,//Allow editing
            //     deleteEnable: false,//Allow deletion
            // },
            // // Button settings
            formSubmitBtnForm: {
                buttonName: '',//Button name
                buttonAttribute: '',//Button properties
                buttonType: '',//button type
                disableId: false,//Disable
                bringinId: false,//Bring in id
                customIdname: false//Custom id name
            },
            //Event settings
            eventSettingForm: {
                buttonEvent: '',//Button event
                moduleName: '',//Component name
                mannerExecution: '',//Execution method
                selectInteraction: '',//Select interaction
                selectPage: '',//Select page
                calldataMethod: '',//Select data source
            },
            // //Form attribute settings
            // sheetSettingForm: {
            //     boundField: '',//Binding field
            //     selectComponent: '',//Select component
            //     fieldName: '',//Field name
            //     tipText: '',//prompt text
            //     defaultValue: '',//Default value
            //     formType: '1',//type
            //     mustFillin: '',//required
            //     notReuse: '',//Do not allow repeated input
            //     finiteWord: '',//Limit the number of words
            //     maxWord: '',//Maximum number of words
            //     miniWord: '',//minimum number of words
            //     finiteFormat: '',//limited input format
            //     size: '1/4',//form size
            //     displayUsage: '',//Display mode
            //     dateType: '',//Date type
            //     timeType: '',//time type
            //     dateFormat: '',//Date format
            //     readOnly: '',//read only
            //     conCeal: '',//Hide
            //     optionList: [], //option list
            //     customData: '',//custom data set
            // },
        };
    },
    computed: {
    },
    watch: {
        // table | form data table and column configuration
        columnForm: {
            handler: function (val, oldVal) {
                // Select the component and modify the corresponding
                if (isNumber(this.layoutComponentActive) && ['table', 'form'].includes(this.layoutComponentActiveType)) {
                    // current component
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // Process data
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
                    // table component adds simulation data
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
                // Select the component and modify the corresponding
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'table') {
                    // current component
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // Process data
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
                // Select the component and modify the corresponding
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'table') {
                    // current component
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // Process data
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
                // Select the component and modify the corresponding
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'table') {
                    // current component
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // Process data
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
        // table column|form column select side configuration
        addColumnForm: {
            handler: function (val, oldVal) {
                // Select the component and table column and modify the corresponding ones after changing them.
                if (isNumber(this.layoutComponentActive) && isNumber(this.columnActiveIndex)) {
                    // current component
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // 
                    const tableColumnItem = layoutComponentItem.columnData[this.columnActiveIndex]

                    // Process data
                    const newLayoutComponentItem = {
                        ...layoutComponentItem
                    }

                    newLayoutComponentItem.columnData[this.columnActiveIndex] = {
                        ...tableColumnItem,
                        ...val
                    }

                    // table component)
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
        // formStyleForm form style
        formStyleForm: {
            handler: function (val, oldVal) {
                // Select the component and modify the corresponding
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'form') {
                    // current component
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // Process data
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
                // Select the component and modify the corresponding
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'form') {
                    // current component
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // Process data
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
                // Select the component and modify the corresponding
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'form') {
                    // current component
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // Process data
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
        // Add page click event monitoring
        // document.addEventListener('click',()=>{});
    },
    beforeDestroy() {
        // Remove page click event listener
        // document.removeEventListener('click',()=>{})
    },
    methods: {
        // Edit area component occupancy display and concealment judgment
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
        // Side Add component button Disable judgment
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
        // Set values ​​for menus and component lists
        setMeunOrComponentData(newComponent) {
            // Modify the corresponding component
            this.$set(this.layoutComponentsList, this.layoutComponentActive, deepClone(newComponent))
            // Current menu
            const menuItem = this.layoutMenuList[this.layoutMenuActive]
            menuItem.layoutList = deepClone(this.layoutComponentsList)
            // Modify the corresponding menu
            this.$set(this.layoutMenuList, this.layoutMenuActive, deepClone(menuItem))
        },
        // Handling Subsection data updates
        handleSubsectionUpdate(dataName, upData) {
            // Currently, subsection does not switch when column is selected.
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
        // Drag and drop to modify sorting start
        handleDragstart(index) {
            this.dragStarIndex = index
        },
        // Drag and drop to modify sorting End
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
                        // Switch location
                        this.layoutComponentsList.splice(this.dragStarIndex, 1)
                        this.layoutComponentsList.splice(index, 0, source)
                    }
                    if (dragType === 'column') {
                        const source = this.columnForm.columnData[this.dragStarIndex]
                        // Switch location
                        this.columnForm.columnData.splice(this.dragStarIndex, 1)
                        this.columnForm.columnData.splice(index, 0, source)
                    }
                    if (dragType === 'customColumnBtn') {
                        const source = this.addColumnForm.btnList[this.dragStarIndex]
                        // Switch location
                        this.addColumnForm.btnList.splice(this.dragStarIndex, 1)
                        this.addColumnForm.btnList.splice(index, 0, source)
                    }
                    if (dragType === 'sortingRules') {
                        const source = this.dataSetForm.sortingRules[this.dragStarIndex]
                        this.dataSetForm.sortingRules.splice(this.dragStarIndex, 1)
                        this.dataSetForm.sortingRules.splice(index, 0, source)
                    }
                    // After the sorting change, the index of the target object becomes the index of the source object.
                    this.dragStarIndex = index;
                }
            }, 100)()
        },
        // Drag and drop to modify sorting and move
        handleDragover(e) {
            e.preventDefault();
        },
        // Handle selected component events
        handleSelectComponent(item, index = '') {
            // if (this.layoutComponentActive === index) return
            this.layoutComponentActive = index
            this.layoutComponentActiveType = item.componentType ?? ''
            this.columnActiveIndex = '' // Column selected index
            // Reset center|right configuration
            this.resetItemsConfig()
            this.resetAddColumnForm()
            // Data echo
            if (item.componentType === 'table') {
                const { columnConfig, filterConfig, tableConfig, pagConfig } = item
                if (columnConfig) {
                    this.columnForm = deepClone(columnConfig)  // column management
                }
                if (filterConfig) {
                    this.dataSetForm = deepClone(filterConfig) // Data settings
                }
                if (tableConfig) {
                    this.tableStyleForm = deepClone(tableConfig) // Table style settings
                }
                if (pagConfig) {
                    this.paginationForm = deepClone(pagConfig)  // Pagination settings
                }
            }
            if (item.componentType === 'form') {
                const { columnConfig, formConfig, submitBtnConfig, eventSetConfig } = item
                if (columnConfig) {
                    this.columnForm = deepClone(columnConfig)  // column management
                }
                if (formConfig) {
                    this.formStyleForm = deepClone(formConfig)  // style
                }
                if (submitBtnConfig) {
                    this.formSubmitBtnForm = deepClone(submitBtnConfig)  // submit button
                }
                if (eventSetConfig) {
                    this.eventSettingForm = deepClone(eventSetConfig)  // submit button event
                }
            }
            if (item.componentType === 'btn') { }
            this.tabsOverallActiveName = 'first' // tabs category add:first set:second
            this.setUpActiveName = 'first' // Attribute: first Style: second
        },
        // Add component
        handleAddComponent(addType = '') {
            let addComponentObj = {
                name: '',
                componentType: addType
            }
            const addTypeList = ['table', 'form', 'btn']
            if (addTypeList.includes(addType)) {
                if (this.addAssembBtnDisabled()) return
                // Reset center|right configuration
                this.resetItemsConfig()
                // tabs category switching
                this.tabsOverallActiveName = 'second'  // Add:first Set:second
                this.setUpActiveName = 'first' // Attribute: first Style: second
                // Add component data type
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
            // Current menu
            const menuItem = this.layoutMenuList[this.layoutMenuActive]
            menuItem.layoutList = deepClone(this.layoutComponentsList)
            // Modify corresponding menu data
            this.$set(this.layoutMenuList, this.layoutMenuActive, deepClone(menuItem))
        },
        // Reset various configurations
        resetItemsConfig() {
            this.dragStarIndex = '' // Component area drag start index
            this.dragEnterIndex = '' // Component area drag end index

            // Reset table this.$options.data.call(this)
            this.columnForm = this.$options.data().columnForm // column management
            this.dataSetForm = this.$options.data().dataSetForm // Data settings
            this.tableStyleForm = this.$options.data().tableStyleForm // Table style settings
            this.paginationForm = this.$options.data().paginationForm // Pagination settings
            this.addColumnForm = this.$options.data().addColumnForm // Add column
            this.customColumnBtnForm = this.$options.data().customColumnBtnForm // Custom column add button

            // reset form
            this.formStyleForm = this.$options.data().formStyleForm
            this.formItemTypeOptChildren = []
        },

        // code view
        handleCodeView() { },
        // Preview
        handlePreview() { },
        // release
        handleRelease() { },

        // Layout name edit
        handleEditLayoutName() {
            this.isEditLayoutName = true
            // Layout name input box focus
            this.$nextTick(() => {
                this.$refs.inputLayoutName.focus()
            })
        },
        // Layout name loses focus
        handleBlurLayoutName() {
            this.isEditLayoutName = false
        },

        // New menu
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
        // Toggle menu
        handleSwitchMenu(index) {
            this.layoutMenuActive = index // selected menu
            this.layoutComponentActive = '' // selected component
            this.layoutComponentActiveType = '' // Component selected type
            this.columnActiveIndex = '' // Column selected index
            // Layout component area list
            this.layoutComponentsList = this.layoutMenuList[index]?.layoutList ?? []
            // Switch tabs category  
            this.tabsOverallActiveName = 'first' // Add:first Set:second
            this.setUpActiveName = 'first' // Attribute: first Style: second
            // Reset center|right configuration
            this.resetItemsConfig()
        },
        // Modify menu name
        handleEditMenuName(item, index) {
            item.isEditName = true
            // Menu name input box focus
            this.$nextTick(() => {
                // console.log('ref', this.$refs)
                this.$refs.inputMenuName[0].focus()
            })
        },
        // Modify menu name and lose focus
        handleBlurMenuName(item) {
            item.isEditName = false
        },



        // table component renderHeader
        renderHeader(createElement, { column, $index }) {
            return createElement(
                'div', {
                'class': ['thead-cell'],
                on: {
                    mousedown: ($event) => { this.handleTableHeaderMouseDown($event, column, $index) }
                }
            }, [
                // Add <a> to display header label
                createElement('a', column.label),
                // Add an empty label to display the drag animation
                // createElement('span', {
                //     'class': ['virtual']
                // })
            ])
        },
        // table component column mouse click
        handleTableHeaderMouseDown(e, column, columnIndex) {
            e.preventDefault();
            this.resetItemsConfig()
            this.layoutComponentActive = column.columnKey
            // column.columnKey
            const currentComponentItem = this.layoutComponentsList[column.columnKey]
            // Data echo
            const selectColumnForm = currentComponentItem.columnConfig.columnData[columnIndex]

            this.$set(this, 'addColumnForm', deepClone(selectColumnForm))
            // tabs switch
            this.columnActiveIndex = columnIndex
            this.layoutComponentActiveType = selectColumnForm.property
            this.tabsOverallActiveName = 'second'  // Add:first Set:second
            this.handleSubsectionUpdate('setUpActiveName', 'first')
        },
        // table component selects header column style
        headerCellClassName({ row, column, columnIndex }) {
            if (this.layoutComponentActive === column.columnKey) {
                return this.columnActiveIndex === columnIndex ? 'column_active_th' : ''
            }
            return ''
        },
        // table component selects header column style
        cellClassName({ row, column, columnIndex }) {
            if (this.layoutComponentActive === column.columnKey) {
                return this.columnActiveIndex === columnIndex ? 'column_active_td' : ''
            }
            return ''
        },

        // Form item column mouse click
        handleFormItemMouseDown(index, fIndex) {
            // reset configuration
            this.resetItemsConfig()
            this.layoutComponentActive = index
            this.columnActiveIndex = fIndex // Select column
            this.layoutComponentActiveType = 'formItem' // Select component type

            const currentComponentItem = this.layoutComponentsList[index]
            // Data echo
            const selectColumnForm = currentComponentItem.columnData[fIndex]

            this.$set(this, 'addColumnForm', deepClone(selectColumnForm))
            // tabs switch
            this.tabsOverallActiveName = 'second'  // Add:first Set:second
            this.handleSubsectionUpdate('setUpActiveName', 'first')
        },

        // switch tabs
        handleTabsLeave(activeName, oldActiveName) {
            // Unselected menus are disabled 
            // this.layoutComponentActiveType  ['customColumn','normalColumn']
            if (activeName === 'second' && !isNumber(this.layoutComponentActive)) {
                return false
            }
            return true
        },
        // Tabs overall component on the right Add settings
        handleTabsClickOverall(tab, event) {
            // console.log(tab, event);
        },

        // Confirm to add column
        onConfirmAddColumn() {
            const copyAddColumnForm = deepClone(this.addColumnForm)
            // Custom column Add button list
            if (copyAddColumnForm.property === 'customColumn') {
                copyAddColumnForm.btnList = []
            }
            this.columnForm.columnData.push(copyAddColumnForm)
            this.addColumnVisible = false
            this.resetAddColumnForm()
        },
        // Reset added columns
        resetAddColumnForm() {
            // When the component type is selected as table
            if (this.layoutComponentActiveType === 'table') {
                this.addColumnForm = {
                    property: '', // Column properties
                    name: '', // Column header
                    field: '', // Bind field
                    displayContent: '', // Show content
                    dataType: '', // Data format
                    columnWidth: '', // column width
                    alignment: '', // Alignment
                    isSort: false, // Allow sorting
                    isExport: false, // Allow export
                    isEdit: false, // Allow editing
                    isDelete: false, // Allow deletion
                }
            }
            // When selecting the component type as form, set basic information
            if (this.layoutComponentActiveType === 'form') {
                this.addColumnForm = {
                    field: '', // Bind field
                    formItemType: '', // Form component category default is input switch select time date checkbox radio
                    type: '', // Component type
                    name: '', // Column header
                    displayContent: '', // Show content
                    columnWidth: 100, // column width
                    optionDataType: '',
                    dataSheet: '',
                    optionData: [],
                    value: '', // Value can set default value
                }
            }
        },
        // Add list form Single component type switching processing
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
        // form selection
        // Column table select all
        handleColumnAllCheckedChange(val) {
            this.columnForm.columnData.forEach(item => {
                item.checked = val
            })
            this.dataSetForm.filterFieldList = val ? deepClone(this.columnForm.columnData) : []
            this.dataSetForm.filterFieldLen = val ? this.dataSetForm.filterFieldList.length : 0
        },
        // Column table selection
        handleColumnCheckedChange(val) {
            const isAllChecked = this.columnForm.columnData.every(item => {
                return item.checked
            })
            this.columnForm.allChecked = isAllChecked
            const filterFieldList = this.columnForm.columnData.filter(item => item.checked)
            this.dataSetForm.filterFieldList = deepClone(filterFieldList)
            this.dataSetForm.filterFieldLen = filterFieldList.length
        },

        // Add filter
        handleAddFilterCondition() {
            this.dataSetForm.filterCriteria.push({
                id: this.dataSetForm.filterCriteriaLen + 1,
                field: '',
                symbol: '',
                condition: ''
            })
            this.dataSetForm.filterCriteriaLen += 1
        },
        // Delete filter
        handleDeleteFilterCondition(index) {
            this.dataSetForm.filterCriteria.splice(index, 1);
            this.dataSetForm.filterCriteriaLen -= 1
        },
        // Add sorting rules
        handleAddSortingRules() {
            this.dataSetForm.sortingRules.push({
                field: '',
                sort: '升序'
            })
        },
        // Delete sorting rule
        handleDeleteSortingRules(index) {
            this.dataSetForm.sortingRules.splice(index, 1);
        },

        // filter table select all
        handleFilterAllCheckedChange(val) {
            this.dataSetForm.filterFieldList.forEach(item => {
                item.checked = val
            })
            this.dataSetForm.filterFieldLen = val ? this.dataSetForm.filterFieldList.length : 0
        },
        // filter table selection
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
        // Pagination Select all
        handlePaginationAllCheckedChange(val) {
            this.paginationForm.layout.forEach(item => {
                item.checked = val
            })
        },
        // Pagination Select
        handlePaginationCheckedChange(val) {
            const isAllChecked = this.paginationForm.layout.every(item => {
                return item.checked
            })
            this.paginationForm.allCheckedLayout = isAllChecked
        },

        // table custom column add button
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
        // table custom column delete button
        handleDeleteCustomColumnBtn(index) {
            this.addColumnForm.btnList.splice(index, 1);
        },

        // table custom column button (selected) side attribute editing
        customColumnBtnSelect(item) {
            this.layoutComponentActiveType = 'columnEditBtn'
            this.customColumnBtnForm = {
                ...item
            }
        },

        // form column add optionData
        handleAddOptionData() {
            this.addColumnForm?.optionData?.push({
                label: "",
                value: ""
            })
        },
        // form column delete optionData
        handleDeleteOptionData(index) {
            this.addColumnForm?.optionData?.splice(index, 1);
        },
        // form submit button
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
        // //Switch width ratio
        // handleSwitchSubformstyle(field, value) {
        //     console.log(111, field, value)
        //     this.$set(this.sheetSettingForm, field, value);
        //     console.log(this.sheetSettingForm.size)
        // },
        // //Add button list
        // handleAddBtnList() {
        //     this.customSetForm.btnList.push({
        //         btntext: '',

        //     })
        // },
        // //Delete button list
        // handleDeleteBtnList(index) {
        //     this.customSetForm.btnList.splice(index, 1);
        // },
    }
});

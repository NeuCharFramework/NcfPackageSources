/**

 * @param {Function} func

 * @param {number} wait

 * @param {boolean} immediate

 * @return {*}

 */
function debounce(func, wait, immediate) {
    let timeout, args, context, timestamp, result
    const later = function () {
        //Legacy comment translated to English.
        const last = +new Date() - timestamp

        //Legacy comment translated to English.
        if (last < wait && last > 0) {
            timeout = setTimeout(later, wait - last)
        } else {
            timeout = null
            //Legacy comment translated to English.
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
        //Legacy comment translated to English.
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

 * Legacy comment translated to English.

 * English documentation comment.

 */
function isNumber(val) {
    // return !isNaN(val) && (typeof val === 'number' || !isNaN(Number(val)))
    return !isNaN(val) && val !== '' && (typeof val === 'number' || !isNaN(Number()))
}

/**

 * Legacy comment translated to English.

 * English documentation comment.

 */
function isObjEmpty(obj) {
    return Object.keys(obj).length === 0;
}
var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com',
            elSize: 'medium', //Legacy comment translated to English.
            layoutName: '', //Legacy comment translated to English.
            isEditLayoutName: false,//Legacy comment translated to English.
            //Legacy comment translated to English.
            loadingAddMenu: false, //Legacy comment translated to English.

            layoutMenuActive: 0, //Legacy comment translated to English.
            layoutComponentActive: '', //Legacy comment translated to English.
            layoutComponentActiveType: '', //Legacy comment translated to English.
            columnActiveIndex: '', //Legacy comment translated to English.

            dragStarIndex: '', //Legacy comment translated to English.
            dragEnterIndex: '', //Legacy comment translated to English.

            layoutMenuList: [
                {
                    name: ncfST('Frontend.276'),
                    isEditName: false,
                    layoutList: []
                }
            ],
            //Legacy comment translated to English.
            layoutComponentsList: [],

            //Legacy comment translated to English.
            tabsOverallActiveName: 'first', //Legacy comment translated to English.
            setUpActiveName: 'first', //Legacy comment translated to English.
            //Legacy comment translated to English.
            columnForm: {
                dataSheet: '',
                allChecked: false,
                //Legacy comment translated to English.
                columnData: []
            },
            //Legacy comment translated to English.
            addColumnVisible: false,
            addColumnForm: {},
            // datasettings
            dataSetForm: {
                filterCriteriaLen: 0, //Legacy comment translated to English.
                filterCriteria: [], //Legacy comment translated to English.
                sortingRules: [],//Legacy comment translated to English.
                allFilterChecked: true,
                filterFieldLen: 0,//Legacy comment translated to English.
                filterFieldList: [],//Legacy comment translated to English.
                queryScope: '', //Legacy comment translated to English.
            },
            //Legacy comment translated to English.
            tableStyleForm: {
                stripe: false, //Legacy comment translated to English.
                border: false, //Legacy comment translated to English.
                showHeader: true, //Legacy comment translated to English.
                height: '', //Legacy comment translated to English.
                size: 'medium', //Legacy comment translated to English.
                columnData: [] //Legacy comment translated to English.
            },
            //Legacy comment translated to English.
            paginationForm: {
                enable: false, //Legacy comment translated to English.
                small: false, //Legacy comment translated to English.
                background: false, //Legacy comment translated to English.
                hideOnSinglePage: false, //Legacy comment translated to English.
                pagerCount: 7, //Legacy comment translated to English.
                pageSizes: [10, 20, 30], //Legacy comment translated to English.
                position: 'bottom-end', //Legacy comment translated to English.
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
            //Legacy comment translated to English.
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
                    popupTitleText: ncfST('Frontend.278'),
                    popupCancelText: ncfST('Frontend.034'),
                    popupConfirmText: ncfST('Frontend.152'),
                    popupPromptContent: ncfST('Frontend.279'),
                    popupPromptOften: '1600', // ms
                    popupClosePage: ncfST('Frontend.280')
                }
            },
            //Legacy comment translated to English.
            formStyleForm: {
                size: 'medium', //Legacy comment translated to English.
                labelPosition: 'left', // left right top
                labelWidth: '100', //Legacy comment translated to English.
                // lableFontSize: '16',
                lableColor: '#333',
            },
            // input switch select time-select time-picker date-picker checkbox radio
            //Legacy comment translated to English.
            formItemOpt: [
                {
                    label: ncfST('Frontend.281'),
                    value: 'input',
                    disabled: false,
                    children: [
                        {
                            label: ncfST('Frontend.282'),
                            value: 'text',
                            disabled: false,
                        },
                        {
                            label: ncfST('Frontend.283'),
                            value: 'textarea',
                            disabled: false,
                        },
                        {
                            label: ncfST('Frontend.284'),
                            value: 'number',
                            disabled: false,
                        }
                    ]
                },
                {
                    label: ncfST('Frontend.285'),
                    value: 'switch',
                    disabled: false,
                },
                {
                    label: ncfST('Frontend.286'),
                    value: 'select',
                    disabled: false,
                },
                {
                    label: ncfST('Frontend.287'),
                    value: 'timePicker',
                    disabled: false,
                },
                // {
                //Legacy comment translated to English.
                //     value: 'timeSelect',
                //     disabled: false,
                // },
                {
                    label: ncfST('Frontend.288'),
                    value: 'date',
                    disabled: false,
                    children: [
                        // datetimerange/daterange/monthrange
                        {
                            label: ncfST('Frontend.289'),
                            value: 'year',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: ncfST('Frontend.290'),
                            value: 'years',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: ncfST('Frontend.291'),
                            value: 'month',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: ncfST('Frontend.292'),
                            value: 'months',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: ncfST('Frontend.288'),
                            value: 'date',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: ncfST('Frontend.293'),
                            value: 'dates',
                            format: '',
                            disabled: false,
                        },
                        {
                            label: ncfST('Frontend.294'),
                            value: 'week',
                            format: ncfST('Frontend.295'),
                            disabled: false,
                        },
                        {
                            label: ncfST('Frontend.296'),
                            value: 'datetime',
                            format: '',
                            disabled: false,
                        }

                    ]
                },
                {
                    label: ncfST('Frontend.297'),
                    value: 'checkbox',
                    disabled: false,
                },
                {
                    label: ncfST('Frontend.298'),
                    value: 'radio',
                    disabled: false,
                }
            ],
            //Legacy comment translated to English.
            formItemTypeOptChildren: [],
            //Legacy comment translated to English.
            // customSetForm: {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            // },
            //Legacy comment translated to English.
            // ColumnAttributeSetForm: {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            // },
            //Legacy comment translated to English.
            formSubmitBtnForm: {
                buttonName: '',//Legacy comment translated to English.
                buttonAttribute: '',//Legacy comment translated to English.
                buttonType: '',//Legacy comment translated to English.
                disableId: false,//Legacy comment translated to English.
                bringinId: false,//Legacy comment translated to English.
                customIdname: false//customidname
            },
            //Legacy comment translated to English.
            eventSettingForm: {
                buttonEvent: '',//Legacy comment translated to English.
                moduleName: '',//Legacy comment translated to English.
                mannerExecution: '',//executemethod
                selectInteraction: '',//Legacy comment translated to English.
                selectPage: '',//Legacy comment translated to English.
                calldataMethod: '',//Legacy comment translated to English.
            },
            //Legacy comment translated to English.
            // sheetSettingForm: {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //     formType: '1',//type
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //     conCeal: '',//hide
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            // },
        };
    },
    computed: {
    },
    watch: {
        //Legacy comment translated to English.
        columnForm: {
            handler: function (val, oldVal) {
                // update the corresponding value after the selected component changes
                if (isNumber(this.layoutComponentActive) && ['table', 'form'].includes(this.layoutComponentActiveType)) {
                    //Legacy comment translated to English.
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // handlingdata
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
                            tableMockItem[item.field] = ncfST('Frontend.299')
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
                    //Legacy comment translated to English.
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
                // update the corresponding value after the selected component changes
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'table') {
                    //Legacy comment translated to English.
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // handlingdata
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
                // update the corresponding value after the selected component changes
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'table') {
                    //Legacy comment translated to English.
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // handlingdata
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
                // update the corresponding value after the selected component changes
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'table') {
                    //Legacy comment translated to English.
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // handlingdata
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
        //Legacy comment translated to English.
        addColumnForm: {
            handler: function (val, oldVal) {
                //Legacy comment translated to English.
                if (isNumber(this.layoutComponentActive) && isNumber(this.columnActiveIndex)) {
                    //Legacy comment translated to English.
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // 
                    const tableColumnItem = layoutComponentItem.columnData[this.columnActiveIndex]

                    // handlingdata
                    const newLayoutComponentItem = {
                        ...layoutComponentItem
                    }

                    newLayoutComponentItem.columnData[this.columnActiveIndex] = {
                        ...tableColumnItem,
                        ...val
                    }

                    //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        formStyleForm: {
            handler: function (val, oldVal) {
                // update the corresponding value after the selected component changes
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'form') {
                    //Legacy comment translated to English.
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // handlingdata
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
                // update the corresponding value after the selected component changes
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'form') {
                    //Legacy comment translated to English.
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // handlingdata
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
                // update the corresponding value after the selected component changes
                if (isNumber(this.layoutComponentActive) && this.layoutComponentActiveType === 'form') {
                    //Legacy comment translated to English.
                    const layoutComponentItem = this.layoutComponentsList[this.layoutComponentActive]
                    // handlingdata
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
        //Legacy comment translated to English.
        // document.addEventListener('click',()=>{});
    },
    beforeDestroy() {
        //Legacy comment translated to English.
        // document.removeEventListener('click',()=>{})
    },
    methods: {
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        setMeunOrComponentData(newComponent) {
            //Legacy comment translated to English.
            this.$set(this.layoutComponentsList, this.layoutComponentActive, deepClone(newComponent))
            //Legacy comment translated to English.
            const menuItem = this.layoutMenuList[this.layoutMenuActive]
            menuItem.layoutList = deepClone(this.layoutComponentsList)
            //Legacy comment translated to English.
            this.$set(this.layoutMenuList, this.layoutMenuActive, deepClone(menuItem))
        },
        // handling Subsection dataupdate
        handleSubsectionUpdate(dataName, upData) {
            //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        handleDragstart(index) {
            this.dragStarIndex = index
        },
        //Legacy comment translated to English.
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
                        //Legacy comment translated to English.
                        this.layoutComponentsList.splice(this.dragStarIndex, 1)
                        this.layoutComponentsList.splice(index, 0, source)
                    }
                    if (dragType === 'column') {
                        const source = this.columnForm.columnData[this.dragStarIndex]
                        //Legacy comment translated to English.
                        this.columnForm.columnData.splice(this.dragStarIndex, 1)
                        this.columnForm.columnData.splice(index, 0, source)
                    }
                    if (dragType === 'customColumnBtn') {
                        const source = this.addColumnForm.btnList[this.dragStarIndex]
                        //Legacy comment translated to English.
                        this.addColumnForm.btnList.splice(this.dragStarIndex, 1)
                        this.addColumnForm.btnList.splice(index, 0, source)
                    }
                    if (dragType === 'sortingRules') {
                        const source = this.dataSetForm.sortingRules[this.dragStarIndex]
                        this.dataSetForm.sortingRules.splice(this.dragStarIndex, 1)
                        this.dataSetForm.sortingRules.splice(index, 0, source)
                    }
                    //Legacy comment translated to English.
                    this.dragStarIndex = index;
                }
            }, 100)()
        },
        //Legacy comment translated to English.
        handleDragover(e) {
            e.preventDefault();
        },
        //Legacy comment translated to English.
        handleSelectComponent(item, index = '') {
            // if (this.layoutComponentActive === index) return
            this.layoutComponentActive = index
            this.layoutComponentActiveType = item.componentType ?? ''
            this.columnActiveIndex = '' //Legacy comment translated to English.
            //Legacy comment translated to English.
            this.resetItemsConfig()
            this.resetAddColumnForm()
            //Legacy comment translated to English.
            if (item.componentType === 'table') {
                const { columnConfig, filterConfig, tableConfig, pagConfig } = item
                if (columnConfig) {
                    this.columnForm = deepClone(columnConfig)  //Legacy comment translated to English.
                }
                if (filterConfig) {
                    this.dataSetForm = deepClone(filterConfig) // datasettings
                }
                if (tableConfig) {
                    this.tableStyleForm = deepClone(tableConfig) //Legacy comment translated to English.
                }
                if (pagConfig) {
                    this.paginationForm = deepClone(pagConfig)  //Legacy comment translated to English.
                }
            }
            if (item.componentType === 'form') {
                const { columnConfig, formConfig, submitBtnConfig, eventSetConfig } = item
                if (columnConfig) {
                    this.columnForm = deepClone(columnConfig)  //Legacy comment translated to English.
                }
                if (formConfig) {
                    this.formStyleForm = deepClone(formConfig)  //Legacy comment translated to English.
                }
                if (submitBtnConfig) {
                    this.formSubmitBtnForm = deepClone(submitBtnConfig)  //Legacy comment translated to English.
                }
                if (eventSetConfig) {
                    this.eventSettingForm = deepClone(eventSetConfig)  //Legacy comment translated to English.
                }
            }
            if (item.componentType === 'btn') { }
            this.tabsOverallActiveName = 'first' //Legacy comment translated to English.
            this.setUpActiveName = 'first' //Legacy comment translated to English.
        },
        //Legacy comment translated to English.
        handleAddComponent(addType = '') {
            let addComponentObj = {
                name: '',
                componentType: addType
            }
            const addTypeList = ['table', 'form', 'btn']
            if (addTypeList.includes(addType)) {
                if (this.addAssembBtnDisabled()) return
                //Legacy comment translated to English.
                this.resetItemsConfig()
                //Legacy comment translated to English.
                this.tabsOverallActiveName = 'second'  //Legacy comment translated to English.
                this.setUpActiveName = 'first' //Legacy comment translated to English.
                //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            const menuItem = this.layoutMenuList[this.layoutMenuActive]
            menuItem.layoutList = deepClone(this.layoutComponentsList)
            //Legacy comment translated to English.
            this.$set(this.layoutMenuList, this.layoutMenuActive, deepClone(menuItem))
        },
        //Legacy comment translated to English.
        resetItemsConfig() {
            this.dragStarIndex = '' //Legacy comment translated to English.
            this.dragEnterIndex = '' //Legacy comment translated to English.

            //Legacy comment translated to English.
            this.columnForm = this.$options.data().columnForm //Legacy comment translated to English.
            this.dataSetForm = this.$options.data().dataSetForm // datasettings
            this.tableStyleForm = this.$options.data().tableStyleForm //Legacy comment translated to English.
            this.paginationForm = this.$options.data().paginationForm //Legacy comment translated to English.
            this.addColumnForm = this.$options.data().addColumnForm //Legacy comment translated to English.
            this.customColumnBtnForm = this.$options.data().customColumnBtnForm //Legacy comment translated to English.

            //Legacy comment translated to English.
            this.formStyleForm = this.$options.data().formStyleForm
            this.formItemTypeOptChildren = []
        },

        //Legacy comment translated to English.
        handleCodeView() { },
        //Legacy comment translated to English.
        handlePreview() { },
        //Legacy comment translated to English.
        handleRelease() { },

        //Legacy comment translated to English.
        handleEditLayoutName() {
            this.isEditLayoutName = true
            //Legacy comment translated to English.
            this.$nextTick(() => {
                this.$refs.inputLayoutName.focus()
            })
        },
        //Legacy comment translated to English.
        handleBlurLayoutName() {
            this.isEditLayoutName = false
        },

        //Legacy comment translated to English.
        handleAddMenu() {
            if (this.loadingAddMenu) return
            this.loadingAddMenu = true
            this.layoutMenuList.push({
                name: ncfST('Frontend.277', this.layoutMenuList.length + 1),
                isEditName: false,
                layoutList: []
            })
            this.loadingAddMenu = false
        },
        //Legacy comment translated to English.
        handleSwitchMenu(index) {
            this.layoutMenuActive = index //Legacy comment translated to English.
            this.layoutComponentActive = '' //Legacy comment translated to English.
            this.layoutComponentActiveType = '' //Legacy comment translated to English.
            this.columnActiveIndex = '' //Legacy comment translated to English.
            //Legacy comment translated to English.
            this.layoutComponentsList = this.layoutMenuList[index]?.layoutList ?? []
            //Legacy comment translated to English.
            this.tabsOverallActiveName = 'first' //Legacy comment translated to English.
            this.setUpActiveName = 'first' //Legacy comment translated to English.
            //Legacy comment translated to English.
            this.resetItemsConfig()
        },
        //Legacy comment translated to English.
        handleEditMenuName(item, index) {
            item.isEditName = true
            //Legacy comment translated to English.
            this.$nextTick(() => {
                // console.log('ref', this.$refs)
                this.$refs.inputMenuName[0].focus()
            })
        },
        //Legacy comment translated to English.
        handleBlurMenuName(item) {
            item.isEditName = false
        },



        //Legacy comment translated to English.
        renderHeader(createElement, { column, $index }) {
            return createElement(
                'div', {
                'class': ['thead-cell'],
                on: {
                    mousedown: ($event) => { this.handleTableHeaderMouseDown($event, column, $index) }
                }
            }, [
                //Legacy comment translated to English.
                createElement('a', column.label),
                //Legacy comment translated to English.
                // createElement('span', {
                //     'class': ['virtual']
                // })
            ])
        },
        //Legacy comment translated to English.
        handleTableHeaderMouseDown(e, column, columnIndex) {
            e.preventDefault();
            this.resetItemsConfig()
            this.layoutComponentActive = column.columnKey
            // column.columnKey
            const currentComponentItem = this.layoutComponentsList[column.columnKey]
            //Legacy comment translated to English.
            const selectColumnForm = currentComponentItem.columnConfig.columnData[columnIndex]

            this.$set(this, 'addColumnForm', deepClone(selectColumnForm))
            //Legacy comment translated to English.
            this.columnActiveIndex = columnIndex
            this.layoutComponentActiveType = selectColumnForm.property
            this.tabsOverallActiveName = 'second'  //Legacy comment translated to English.
            this.handleSubsectionUpdate('setUpActiveName', 'first')
        },
        //Legacy comment translated to English.
        headerCellClassName({ row, column, columnIndex }) {
            if (this.layoutComponentActive === column.columnKey) {
                return this.columnActiveIndex === columnIndex ? 'column_active_th' : ''
            }
            return ''
        },
        //Legacy comment translated to English.
        cellClassName({ row, column, columnIndex }) {
            if (this.layoutComponentActive === column.columnKey) {
                return this.columnActiveIndex === columnIndex ? 'column_active_td' : ''
            }
            return ''
        },

        //Legacy comment translated to English.
        handleFormItemMouseDown(index, fIndex) {
            //Legacy comment translated to English.
            this.resetItemsConfig()
            this.layoutComponentActive = index
            this.columnActiveIndex = fIndex //Legacy comment translated to English.
            this.layoutComponentActiveType = 'formItem' //Legacy comment translated to English.

            const currentComponentItem = this.layoutComponentsList[index]
            //Legacy comment translated to English.
            const selectColumnForm = currentComponentItem.columnData[fIndex]

            this.$set(this, 'addColumnForm', deepClone(selectColumnForm))
            //Legacy comment translated to English.
            this.tabsOverallActiveName = 'second'  //Legacy comment translated to English.
            this.handleSubsectionUpdate('setUpActiveName', 'first')
        },

        //Legacy comment translated to English.
        handleTabsLeave(activeName, oldActiveName) {
            //Legacy comment translated to English.
            // this.layoutComponentActiveType  ['customColumn','normalColumn']
            if (activeName === 'second' && !isNumber(this.layoutComponentActive)) {
                return false
            }
            return true
        },
        //Legacy comment translated to English.
        handleTabsClickOverall(tab, event) {
            // console.log(tab, event);
        },

        //Legacy comment translated to English.
        onConfirmAddColumn() {
            const copyAddColumnForm = deepClone(this.addColumnForm)
            //Legacy comment translated to English.
            if (copyAddColumnForm.property === 'customColumn') {
                copyAddColumnForm.btnList = []
            }
            this.columnForm.columnData.push(copyAddColumnForm)
            this.addColumnVisible = false
            this.resetAddColumnForm()
        },
        //Legacy comment translated to English.
        resetAddColumnForm() {
            //Legacy comment translated to English.
            if (this.layoutComponentActiveType === 'table') {
                this.addColumnForm = {
                    property: '', //Legacy comment translated to English.
                    name: '', //Legacy comment translated to English.
                    field: '', //Legacy comment translated to English.
                    displayContent: '', //Legacy comment translated to English.
                    dataType: '', //Legacy comment translated to English.
                    columnWidth: '', //Legacy comment translated to English.
                    alignment: '', //Legacy comment translated to English.
                    isSort: false, //Legacy comment translated to English.
                    isExport: false, //Legacy comment translated to English.
                    isEdit: false, //Legacy comment translated to English.
                    isDelete: false, //Legacy comment translated to English.
                }
            }
            //Legacy comment translated to English.
            if (this.layoutComponentActiveType === 'form') {
                this.addColumnForm = {
                    field: '', //Legacy comment translated to English.
                    formItemType: '', //Legacy comment translated to English.
                    type: '', //Legacy comment translated to English.
                    name: '', //Legacy comment translated to English.
                    displayContent: '', //Legacy comment translated to English.
                    columnWidth: 100, //Legacy comment translated to English.
                    optionDataType: '',
                    dataSheet: '',
                    optionData: [],
                    value: '', //Legacy comment translated to English.
                }
            }
        },
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        handleColumnAllCheckedChange(val) {
            this.columnForm.columnData.forEach(item => {
                item.checked = val
            })
            this.dataSetForm.filterFieldList = val ? deepClone(this.columnForm.columnData) : []
            this.dataSetForm.filterFieldLen = val ? this.dataSetForm.filterFieldList.length : 0
        },
        //Legacy comment translated to English.
        handleColumnCheckedChange(val) {
            const isAllChecked = this.columnForm.columnData.every(item => {
                return item.checked
            })
            this.columnForm.allChecked = isAllChecked
            const filterFieldList = this.columnForm.columnData.filter(item => item.checked)
            this.dataSetForm.filterFieldList = deepClone(filterFieldList)
            this.dataSetForm.filterFieldLen = filterFieldList.length
        },

        //Legacy comment translated to English.
        handleAddFilterCondition() {
            this.dataSetForm.filterCriteria.push({
                id: this.dataSetForm.filterCriteriaLen + 1,
                field: '',
                symbol: '',
                condition: ''
            })
            this.dataSetForm.filterCriteriaLen += 1
        },
        //Legacy comment translated to English.
        handleDeleteFilterCondition(index) {
            this.dataSetForm.filterCriteria.splice(index, 1);
            this.dataSetForm.filterCriteriaLen -= 1
        },
        //Legacy comment translated to English.
        handleAddSortingRules() {
            this.dataSetForm.sortingRules.push({
                field: '',
                sort: ncfST('Frontend.031')
            })
        },
        //Legacy comment translated to English.
        handleDeleteSortingRules(index) {
            this.dataSetForm.sortingRules.splice(index, 1);
        },

        //Legacy comment translated to English.
        handleFilterAllCheckedChange(val) {
            this.dataSetForm.filterFieldList.forEach(item => {
                item.checked = val
            })
            this.dataSetForm.filterFieldLen = val ? this.dataSetForm.filterFieldList.length : 0
        },
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        handlePaginationAllCheckedChange(val) {
            this.paginationForm.layout.forEach(item => {
                item.checked = val
            })
        },
        //Legacy comment translated to English.
        handlePaginationCheckedChange(val) {
            const isAllChecked = this.paginationForm.layout.every(item => {
                return item.checked
            })
            this.paginationForm.allCheckedLayout = isAllChecked
        },

        //Legacy comment translated to English.
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
                    popupTitleText: ncfST('Frontend.278'),
                    popupCancelText: ncfST('Frontend.034'),
                    popupConfirmText: ncfST('Frontend.152'),
                    popupPromptContent: ncfST('Frontend.279'),
                    popupPromptOften: '1600', // ms
                    popupClosePage: ncfST('Frontend.280')
                }
            })
        },
        //Legacy comment translated to English.
        handleDeleteCustomColumnBtn(index) {
            this.addColumnForm.btnList.splice(index, 1);
        },

        //Legacy comment translated to English.
        customColumnBtnSelect(item) {
            this.layoutComponentActiveType = 'columnEditBtn'
            this.customColumnBtnForm = {
                ...item
            }
        },

        //Legacy comment translated to English.
        handleAddOptionData() {
            this.addColumnForm?.optionData?.push({
                label: "",
                value: ""
            })
        },
        //Legacy comment translated to English.
        handleDeleteOptionData(index) {
            this.addColumnForm?.optionData?.splice(index, 1);
        },
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        // handleSwitchSubformstyle(field, value) {
        //     console.log(111, field, value)
        //     this.$set(this.sheetSettingForm, field, value);
        //     console.log(this.sheetSettingForm.size)
        // },
        //Legacy comment translated to English.
        // handleAddBtnList() {
        //     this.customSetForm.btnList.push({
        //         btntext: '',

        //     })
        // },
        //Legacy comment translated to English.
        // handleDeleteBtnList(index) {
        //     this.customSetForm.btnList.splice(index, 1);
        // },
    }
});

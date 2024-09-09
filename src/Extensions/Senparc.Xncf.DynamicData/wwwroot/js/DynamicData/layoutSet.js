var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com',
            elSize: 'medium', // el 组件尺寸大小 默认为空  medium、small、mini
            layoutName: '', // 布局名称
            isEditLayoutName: false,// 布局名称是否 编辑
            // 布局菜单列表
            layoutMenuList: [
                {
                    name: '菜单1',
                    isEditName: false,
                    layoutList: []
                }],
            loadingAddMenu: false, // 新增菜单loading
            layoutComponentsList: [],// 布局 组件 列表
            tabsOverallActiveName: 'first', //tabs 组件整体 操作
            setUpActiveName: 'first', // 设置 类别
            // 列管理
            columnForm: {
                dataSheet: '',
                allChecked: false,
                // 是否选择checked 字段:field 类型:dataType 名称:name
                columnData: []
            },
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
                isDelete:false,
            },
            addColumnFormRules:[],
            columnFormRules: [],
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
            pageSizesOpt: [10, 20, 30, 40, 50, 100],
            pageSizesPositionOpt: ['top', 'top-start', 'top-end', 'bottom', 'bottom-start', 'bottom-end'],
            paginationFormRules: [],
        };
    },
    computed: {
    },
    watch: {
        //'isExtend': {
        //    handler: function (val, oldVal) {
        //    },
        //    immediate: true,
        //    //deep:true
        //}
    },
    created() {
    },
    mounted() {

    },
    beforeDestroy() {

    },
    methods: {
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
        handleSwitchMenu() {
            //this.layoutComponentsList = []
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
        // 添加组件区域
        // handleAddComponentArea() {},
        // 右侧Tabs 整体组件 添加 设置
        handleTabsClickOverall(tab, event) {
            console.log(tab, event);
        },
        // 添加表格 组件
        handleAddComponent(addType) {
            // layoutComponentsList
            let addComponentObj = {
                name: ''
            }
            if (addType === 'table') {

            }
            if (addType === 'form') {

            }
            if (addType === 'btn') {

            }
            this.layoutComponentsList.push(addComponentObj)
        },
        // 切换设置类别
        handleSwitchSubsection(dataName, upData) {
            if (!dataName) {
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
    }
});

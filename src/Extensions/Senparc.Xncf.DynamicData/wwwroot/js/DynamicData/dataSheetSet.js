var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com',
            elSize: 'medium', // el 组件尺寸大小 默认为空  medium、small、mini\
            // 数据表 列表 list
            dataSheetList: [], 
            dataSheetTotal: 0,
            queryDataSheet: {
                page: 1,
                size: 10
            },
            // 数据表 选中的item Id
            sheetSelectId: 0,
            // 创建数据表的 表单
            sheetFormEidt:false,
            sheetForm: {
                name: '',
                description: ''
            },
            formRules: {
                name: [
                    { required: true, message: '请输入名称', trigger: 'blur' },
                ],
                description: [
                    { required: true, message: '请输入描述', trigger: 'blur' }
                ],
            },
            // 表字段 列表 list
            tableFieldList: [],
            tableFieldTotal: 0,
            queryTableField: {
                page: 1,
                size: 10
            },
        };
    },
    computed: {
        // 表相关按钮 disabled
        sheetBD() {
            return !this.sheetSelectId
        },
        // 布局相关按钮 disabled
        layoutBD() {
            return !this.sheetSelectId || !this.tableFieldTotal
        },
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
        this.getDataSheetListData()
    },
    mounted() {

    },
    beforeDestroy() {

    },
    methods: {
        // 获取 数据表 列表
        getDataSheetListData() {
            // 模拟 数据
            const simulationData = []
            for (let i = 0; i < 10; i++) {
                simulationData.push({
                    id: i + 1,
                    name: '设备管理证书（spot_checkasd）',
                    description: '绑定数据表1，绑定数据表2，绑定数据表3绑定数据表1，绑定数据表2，绑定数据表3绑定数据表1，绑定数据表2，绑定数据表3',
                    operationVisible: false
                })
            }
            this.dataSheetList = simulationData
            this.dataSheetTotal = simulationData.length
            // todo 调用接口
        },
        // 查看表详情
        viewTableDetails(item) {
            if (!item) return
            this.sheetSelectId = item.id
            this.sheetForm = {
                name: item.name,
                description: item.description
            }
            this.sheetFormEidt = true
            // todo 调用接口
        },
        // 新增数据表
        addDataTable() {
            this.sheetSelectId = 0
            this.sheetForm = {
                name: '',
                description: ''
            }
            this.sheetFormEidt = false
            // 重置表单
            //this.resetSheetForm()
        },
        // 新增字段
        newFieldsAdd() {
            // todo 调用接口
        },
        // 创建布局
        createLayout() {
            // todo 跳转
        },
        // 导入
        importDataSheet() {
            // todo 调用接口
        },
        // 导出
        exportDataSheet() {
            // todo 调用接口
        },
        // 数据表 基础信息编辑
        onEidtSheet() {
            this.sheetFormEidt = false
        },
        // 确认创建数据表
        onSubmitSheet() {
            // 校验 sheetForm 表单
            //this.$refs.sheetElForm.validate((valid) => {
            //    if (valid) {
            //        // todo 调用接口
            //    } else {
            //        console.log('error submit!!');
            //        return false;
            //    }
            //});
            const { name, description } = this.sheetForm
            let messageText = ''
            if (!name && !description) {
                messageText = '请输入名称和描述'
            } else if (!name) {
                this.$message.error('请输入名称');
            } else if (!description) {
                messageText = '请输入描述'
            }
            if (messageText) {
                this.$message.error(messageText);
                return
            }
            // 模拟调用接口
            const simulationId = this.dataSheetTotal + 1
            this.dataSheetList.push({
                id: simulationId,
                name,
                description,
                operationVisible: false
            })
            this.dataSheetTotal += 1
            this.sheetSelectId = simulationId
            this.sheetFormEidt = true
            // todo 调用接口 创建 this.sheetSelectId
            // todo 调用接口 重新获取 dataSheetList
            // todo 调用接口 重新获取 tableFieldList: [], tableFieldTotal: 0,
        },
        // 重置 sheetForm 表单
        //resetSheetForm() {
        //    this.$refs.sheetElForm.resetFields();
        //}
    }
});

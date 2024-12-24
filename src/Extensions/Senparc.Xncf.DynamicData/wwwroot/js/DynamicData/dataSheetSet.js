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
            // 创建 表字段 
            newFieldDrawer: false,
            newFieldForm: {
                name:'',
                identificationId: '',
                dataType: '',
                format: '',
                minLen: '',
                maxLen: '',
                requir: false,
                primaryKey: false,
                event: '',
                color: '#000000',
                description: ''
            },
            newFieldFormRules: {
                name: [
                    { required: true, message: '请填写名称', trigger: 'blur' },
                ],
                identificationId: [
                    { required: true, message: '请填写标识Id', trigger: 'blur' },
                ],
                dataType: [
                    { required: true, message: '请选择数据类型', trigger: 'change' },
                ],
                format: [
                    { required: true, message: '请选择格式', trigger: 'change' },
                ],
                requir: [
                    { required: true, message: '请选择是否必填', trigger: 'change' },
                ],
                primaryKey: [
                    { required: true, message: '请选择是否唯一', trigger: 'change' },
                ],
                //event: [
                //    { required: true, message: '请选择事件', trigger: 'change' },
                //],
                //color: [
                //    { required: true, message: '请选择颜色', trigger: 'change' },
                //],
                //description: [
                //    { required: true, message: '请填写描述', trigger: 'blur' }
                //]
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
        // 获取 数据表字段 列表
        getTableFieldListData() {
            // 模拟 数据
            const simulationData = []
            for (let i = 0; i < 5; i++) {
                simulationData.push({
                    id: i + 1,
                    name: '测试名称1',
                    identificationId: '4654645646',
                    dataType: '类型文本',
                    format: '格式文本',
                    minLen: '1',
                    maxLen: '30',
                    requir: false,
                    primaryKey: false,
                    event: '事件文本',
                    color: '#000',
                    description: '描述文本'
                })
            }
            this.tableFieldList = simulationData
            this.tableFieldTotal = simulationData.length
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
            // 调用接口 获取字段列表信息
            this.getTableFieldListData()
        },
        // 新增数据表
        addDataTable() {
            this.sheetSelectId = 0
            this.sheetForm = {
                name: '',
                description: ''
            }
            this.sheetFormEidt = false
            // 清空 字段数据
            this.tableFieldList = []
            this.tableFieldTotal = 0
            // 重置表单
            //this.resetSheetForm()
        },
        // 数据表 基础信息编辑
        onEidtSheet() {
            this.sheetFormEidt = false
        },
        // 取消编辑数据表
        onCancelEidtSheet() {
            const sheetFindItem = this.dataSheetList.find(item => item.id === this.sheetSelectId)
            if (sheetFindItem) {
                this.sheetForm = {
                    ...sheetFindItem
                }
            }
            this.sheetFormEidt = true
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
            if (this.sheetSelectId) {
                const sheetFindIndex = this.dataSheetList.findIndex(item => item.id === this.sheetSelectId)
                if (sheetFindIndex !== -1) {
                    const newItem = {
                        ...this.dataSheetList[sheetFindIndex],
                        ...this.sheetForm
                    }
                    this.$set(this.dataSheetList, sheetFindIndex, newItem)
                }
            } else {
                const simulationId = this.dataSheetTotal + 1
                this.dataSheetList.push({
                    id: simulationId,
                    name,
                    description,
                    operationVisible: false
                })
                this.dataSheetTotal += 1
                this.sheetSelectId = simulationId
            }
            
            this.sheetFormEidt = true
            // todo 调用接口 创建 this.sheetSelectId
            // todo 调用接口 重新获取 dataSheetList
            // todo 调用接口 重新获取 tableFieldList: [], tableFieldTotal: 0,
        },
        // 重置 sheetForm 表单
        //resetSheetForm() {
        //    this.$refs.sheetElForm.resetFields();
        //}
        // 关闭 字段表单抽屉
        handleNewFieldDrawerClose(done) {
            this.$confirm('确认关闭？')
                .then(_ => {
                    this.resetNewField(false)
                    this.$nextTick(() => {
                        done();
                    })
                })
                .catch(_ => { });
        },
        // 新增字段
        newFieldsAdd() {
            this.newFieldDrawer = true
            // todo 调用接口
        },
        // 编辑字段
        newFieldsEdit(row) {
            this.newFieldForm = {
                ...row
            }
            this.newFieldDrawer = true
        },
        // 删除字段
        newFieldsDelete(row) {
            this.$confirm('此操作将永久删除该文件, 是否继续?', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(() => {
                this.$message({
                    type: 'success',
                    message: '删除成功!'
                });
                // todo 调用删除接口
                // 调用接口 获取字段列表信息
                // this.getTableFieldListData()
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: '已取消删除'
                });
            });
        },
        // 标识ID 自动生成 
        autoGenerate() {

        },
        // 提交 新增字段表单抽屉
        submitNewField() {
            // 校验 sheetForm 表单
            this.$refs.newFieldElForm.validate((valid) => {
                if (valid) {
                    // 模拟调用接口
                    if (this.newFieldForm.id) {
                        const fieldFindIndex = this.tableFieldList.findIndex(item => item.id === this.newFieldForm.id)
                        if (fieldFindIndex !== -1) {
                            this.$set(this.tableFieldList, fieldFindIndex, { ...this.newFieldForm })
                        }
                    } else {
                        const simulationId = this.tableFieldTotal + 1
                        this.tableFieldList.push({
                            id: simulationId,
                            ...this.newFieldForm
                        })
                        this.tableFieldTotal += 1
                    }
                   
                    // 调用接口 获取字段列表信息
                    // this.getTableFieldListData()
                    // 清空表单 关闭抽屉
                    this.resetNewField()
                } else {
                    console.log('error submit!!');
                    return false;
                }
            });
        },
        // 重置 新增字段表单抽屉
        resetNewField(isClose = true) {
            Object.assign(this.newFieldForm, this.$options.data().newFieldForm)
            this.$refs.newFieldElForm.resetFields();
            if (isClose) {
                this.newFieldDrawer = false
            }
             
        },
        // 创建布局
        createLayout() {
            // todo 跳转 ?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69
            window.open('/Admin/DynamicData/LayoutSet')
        },
        // 导入
        importDataSheet() {
            // todo 调用接口
        },
        // 导出
        exportDataSheet() {
            // todo 调用接口
        }
    }
});

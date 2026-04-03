var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com',
            elSize: 'medium', // el component size defaults to empty medium, small, mini\
            // data table list list
            dataSheetList: [], 
            dataSheetTotal: 0,
            queryDataSheet: {
                page: 1,
                size: 10
            },
            // Data table selected item ID
            sheetSelectId: 0,
            // Create data table form
            sheetFormEidt:false,
            sheetForm: {
                name: '',
                description: ''
            },
            // Create table fields 
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
                //    { required: true, message: 'Please select an event', trigger: 'change' },
                //],
                //color: [
                //    { required: true, message: 'Please select a color', trigger: 'change' },
                //],
                //description: [
                //    { required: true, message: 'Please fill in the description', trigger: 'blur' }
                //]
            },
            // table field list list
            tableFieldList: [],
            tableFieldTotal: 0,
            queryTableField: {
                page: 1,
                size: 10
            },
        };
    },
    computed: {
        // Table related buttons disabled
        sheetBD() {
            return !this.sheetSelectId
        },
        // Layout related buttons disabled
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
        // Get data table list
        getDataSheetListData() {
            // simulated data
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
            // todo calling interface
        },
        // Get the list of data table fields
        getTableFieldListData() {
            // simulated data
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
            // todo calling interface
        },
        // View table details
        viewTableDetails(item) {
            if (!item) return
            this.sheetSelectId = item.id
            this.sheetForm = {
                name: item.name,
                description: item.description
            }
            this.sheetFormEidt = true
            // Call the interface to obtain field list information
            this.getTableFieldListData()
        },
        // Add data table
        addDataTable() {
            this.sheetSelectId = 0
            this.sheetForm = {
                name: '',
                description: ''
            }
            this.sheetFormEidt = false
            // Clear field data
            this.tableFieldList = []
            this.tableFieldTotal = 0
            // Reset form
            //this.resetSheetForm()
        },
        // Data table basic information editing
        onEidtSheet() {
            this.sheetFormEidt = false
        },
        // Cancel editing data table
        onCancelEidtSheet() {
            const sheetFindItem = this.dataSheetList.find(item => item.id === this.sheetSelectId)
            if (sheetFindItem) {
                this.sheetForm = {
                    ...sheetFindItem
                }
            }
            this.sheetFormEidt = true
        },
        // Confirm creation of data table
        onSubmitSheet() {
            // Validate sheetForm form
            //this.$refs.sheetElForm.validate((valid) => {
            //    if (valid) {
            //        // todo calling interface
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
            // Simulation call interface
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
            // todo calls the interface to create this.sheetSelectId
            // todo calls the interface to re-obtain dataSheetList
            // todo calls the interface to re-obtain tableFieldList: [], tableFieldTotal: 0,
        },
        // Reset sheetForm form
        //resetSheetForm() {
        //    this.$refs.sheetElForm.resetFields();
        //}
        // Close Field Form Drawer
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
        // Add new field
        newFieldsAdd() {
            this.newFieldDrawer = true
            // todo calling interface
        },
        // Edit field
        newFieldsEdit(row) {
            this.newFieldForm = {
                ...row
            }
            this.newFieldDrawer = true
        },
        // Delete field
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
                // todo calls the delete interface
                // Call the interface to obtain field list information
                // this.getTableFieldListData()
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: '已取消删除'
                });
            });
        },
        // Identification ID automatically generated 
        autoGenerate() {

        },
        // Submit New Field Form Drawer
        submitNewField() {
            // Validate sheetForm form
            this.$refs.newFieldElForm.validate((valid) => {
                if (valid) {
                    // Simulation call interface
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
                   
                    // Call the interface to obtain field list information
                    // this.getTableFieldListData()
                    // Clear form Close drawer
                    this.resetNewField()
                } else {
                    console.log('error submit!!');
                    return false;
                }
            });
        },
        // Reset the new field form drawer
        resetNewField(isClose = true) {
            Object.assign(this.newFieldForm, this.$options.data().newFieldForm)
            this.$refs.newFieldElForm.resetFields();
            if (isClose) {
                this.newFieldDrawer = false
            }
             
        },
        // Create layout
        createLayout() {
            // todo jump ?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69
            window.open('/Admin/DynamicData/LayoutSet')
        },
        // import
        importDataSheet() {
            // todo calling interface
        },
        // Export
        exportDataSheet() {
            // todo calling interface
        }
    }
});

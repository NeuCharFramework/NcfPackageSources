var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com',
            elSize: 'medium', //Legacy comment translated to English.
            //Legacy comment translated to English.
            dataSheetList: [], 
            dataSheetTotal: 0,
            queryDataSheet: {
                page: 1,
                size: 10
            },
            //Legacy comment translated to English.
            sheetSelectId: 0,
            //Legacy comment translated to English.
            sheetFormEidt:false,
            sheetForm: {
                name: '',
                description: ''
            },
            //Legacy comment translated to English.
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
                    { required: true, message: ncfST('Frontend.187'), trigger: 'blur' },
                ],
                identificationId: [
                    { required: true, message: ncfST('Frontend.188'), trigger: 'blur' },
                ],
                dataType: [
                    { required: true, message: ncfST('Frontend.216'), trigger: 'change' },
                ],
                format: [
                    { required: true, message: ncfST('Frontend.218'), trigger: 'change' },
                ],
                requir: [
                    { required: true, message: ncfST('Frontend.264'), trigger: 'change' },
                ],
                primaryKey: [
                    { required: true, message: ncfST('Frontend.265'), trigger: 'change' },
                ],
                //event: [
                //Legacy comment translated to English.
                //],
                //color: [
                //Legacy comment translated to English.
                //],
                //description: [
                //    { required: true, message: 'please enter a valuedescription', trigger: 'blur' }
                //]
            },
            //Legacy comment translated to English.
            tableFieldList: [],
            tableFieldTotal: 0,
            queryTableField: {
                page: 1,
                size: 10
            },
        };
    },
    computed: {
        //Legacy comment translated to English.
        sheetBD() {
            return !this.sheetSelectId
        },
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        getDataSheetListData() {
            //Legacy comment translated to English.
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
            // todo invokeinterface
        },
        //Legacy comment translated to English.
        getTableFieldListData() {
            //Legacy comment translated to English.
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
            // todo invokeinterface
        },
        //Legacy comment translated to English.
        viewTableDetails(item) {
            if (!item) return
            this.sheetSelectId = item.id
            this.sheetForm = {
                name: item.name,
                description: item.description
            }
            this.sheetFormEidt = true
            //Legacy comment translated to English.
            this.getTableFieldListData()
        },
        // adddatatable
        addDataTable() {
            this.sheetSelectId = 0
            this.sheetForm = {
                name: '',
                description: ''
            }
            this.sheetFormEidt = false
            //Legacy comment translated to English.
            this.tableFieldList = []
            this.tableFieldTotal = 0
            //Legacy comment translated to English.
            //this.resetSheetForm()
        },
        // datatable baseinformationedit
        onEidtSheet() {
            this.sheetFormEidt = false
        },
        //Legacy comment translated to English.
        onCancelEidtSheet() {
            const sheetFindItem = this.dataSheetList.find(item => item.id === this.sheetSelectId)
            if (sheetFindItem) {
                this.sheetForm = {
                    ...sheetFindItem
                }
            }
            this.sheetFormEidt = true
        },
        //Legacy comment translated to English.
        onSubmitSheet() {
            //Legacy comment translated to English.
            //this.$refs.sheetElForm.validate((valid) => {
            //    if (valid) {
            //        // todo invokeinterface
            //    } else {
            //        console.log('error submit!!');
            //        return false;
            //    }
            //});
            const { name, description } = this.sheetForm
            let messageText = ''
            if (!name && !description) {
                messageText = ncfST('Frontend.270')
            } else if (!name) {
                this.$message.error(ncfST('Frontend.195'));
            } else if (!description) {
                messageText = ncfST('Frontend.199')
            }
            if (messageText) {
                this.$message.error(messageText);
                return
            }
            //Legacy comment translated to English.
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
            // todo invokeinterface create this.sheetSelectId
            //Legacy comment translated to English.
            //Legacy comment translated to English.
        },
        //Legacy comment translated to English.
        //resetSheetForm() {
        //    this.$refs.sheetElForm.resetFields();
        //}
        //Legacy comment translated to English.
        handleNewFieldDrawerClose(done) {
            this.$confirm(ncfST('Frontend.271'))
                .then(_ => {
                    this.resetNewField(false)
                    this.$nextTick(() => {
                        done();
                    })
                })
                .catch(_ => { });
        },
        //Legacy comment translated to English.
        newFieldsAdd() {
            this.newFieldDrawer = true
            // todo invokeinterface
        },
        //Legacy comment translated to English.
        newFieldsEdit(row) {
            this.newFieldForm = {
                ...row
            }
            this.newFieldDrawer = true
        },
        //Legacy comment translated to English.
        newFieldsDelete(row) {
            this.$confirm(ncfST('Frontend.272'), ncfST('Frontend.273'), {
                confirmButtonText: ncfST('Frontend.152'),
                cancelButtonText: ncfST('Frontend.034'),
                type: 'warning'
            }).then(() => {
                this.$message({
                    type: 'success',
                    message: ncfST('Frontend.274')
                });
                // todo invokedeleteinterface
                //Legacy comment translated to English.
                // this.getTableFieldListData()
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: ncfST('Frontend.275')
                });
            });
        },
        //Legacy comment translated to English.
        autoGenerate() {

        },
        //Legacy comment translated to English.
        submitNewField() {
            //Legacy comment translated to English.
            this.$refs.newFieldElForm.validate((valid) => {
                if (valid) {
                    //Legacy comment translated to English.
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
                   
                    //Legacy comment translated to English.
                    // this.getTableFieldListData()
                    //Legacy comment translated to English.
                    this.resetNewField()
                } else {
                    console.log('error submit!!');
                    return false;
                }
            });
        },
        //Legacy comment translated to English.
        resetNewField(isClose = true) {
            Object.assign(this.newFieldForm, this.$options.data().newFieldForm)
            this.$refs.newFieldElForm.resetFields();
            if (isClose) {
                this.newFieldDrawer = false
            }
             
        },
        //Legacy comment translated to English.
        createLayout() {
            //Legacy comment translated to English.
            window.location.assign('/Admin/DynamicData/LayoutSet')
        },
        //Legacy comment translated to English.
        importDataSheet() {
            // todo invokeinterface
        },
        //Legacy comment translated to English.
        exportDataSheet() {
            // todo invokeinterface
        }
    }
});

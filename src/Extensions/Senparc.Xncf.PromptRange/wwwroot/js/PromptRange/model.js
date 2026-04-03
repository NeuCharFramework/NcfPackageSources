var app = new Vue({
    el: "#app",
    data() {
        return {
            // Query list parameters
            queryList: {
                page: 1,
                size: 10,
                modelName: ''
            },
            pageSizes: [10, 20, 30, 50],
            tableTotal: 0,
            tableData: [], // Model list
            multipleSelection: {}, // selected model
            dialogFormVisible: false,
            dialogFormTitle: '新增模型',
            formLabelWidth: '',
            editModelName: true,
            modifyId: "",
            modifyFlag: true,
            modifyName: "",
            newModelForm: {
                modelType: '',
                modelName: '',
                modelAPI: '',
                modelAPIkey: ''
            },
            options: [{
                value: 0,
                label: 'OpenAI'
            }, {
                value: 1,
                label: 'AzureOpenAI'
            }, {
                value: 2,
                label: 'NeuCharOpenAI'
            }, {
                value: 3,
                label: 'HugginFace'
            }],
            rules: {
                modelType: [
                    { required: true, message: 'Please select a model type', trigger: 'change' }
                ],
                modelName: [
                    { required: true, message: 'Please enter a model name', trigger: 'blur' }
                ],
                modelAPI: [
                    { required: true, message: 'Please enter a model API', trigger: 'blur' }
                ],
                modelAPIkey: [
                    { required: true, message: 'Please enter a model API key', trigger: 'blur' }
                ]
            }
        };
    },
    watch: {
        //'isExtend': {
        //    handler: function (val, oldVal) {
        //    },
        //    immediate: true,
        //    //deep:true
        //}
    },
    created: function () {
        // Get table list data
        this.getList();
    },
    methods: {
        closeDialog() {
            this.$refs.newDialogModelForm.resetFields();
        },
        submitForm() {
            this.$refs.newDialogModelForm.validate((valid) => {
                if (valid) {
                    if (this.editModelName) {
                        this.addModel()
                    } else {
                        this.modifyModelName(this.modifyId, this.modifyName, this.modifyFlag)
                    }
                    this.dialogFormVisible = false

                } else {
                    console.log('error submit!!');
                    return false;
                }
            });
        },
        // New model btn
        async createBtnFrom() {
            this.dialogFormTitle = '新增模型'
            this.editModelName = true
            this.dialogFormVisible = true
        },
        async addModel() {
            const request = {
                name: this.newModelForm.modelName,
                endpoint: this.newModelForm.modelAPI,
                apiKey: this.newModelForm.modelAPIkey,
            }
            const res = await service.post('/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.Add', request)
            if (res.data.success) {
                this.getList()
                alert('success!');
            }
        },
        // Edit model btn
        editBtnFrom(row) {
            this.dialogFormTitle = '编辑模型'
            this.newModelForm = {
                ...row
            }
            this.editModelName = false
            this.dialogFormVisible = true
            this.modifyId = row.id
            this.modifyFlag = row.show
            this.modifyName = row.name
        },
        async modifyModelName(id, name, flag) {
            const updatedData = {
                id,
                name,
                show: flag

            };
            const res = await service.put('/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.Modify', updatedData)
            if (res.data.success) {
                this.getList()
            } else {
                alert("error")
            }
        },
        // Delete model 
        deleteHandle(row) {
            console.log("row.id", row.id)
            const idsToDelete = []
            idsToDelete.push(row.id)
            this.deletModel(idsToDelete)
        },
        // delete
        async deletModel(idsToDelete) {
            const res = await service.request({
                method: 'delete',
                url: '/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.BatchDelete',
                data: idsToDelete // Send a list of IDs as request body data
            });
            console.log(res)
            if (res.data.success) {
                this.getList()
            } else {
                alert("error")
            }
        },
        // btn batch delete
        btnBatchdelete() {
            console.log('批量删除', this.multipleSelection)
            // Loop this.multipleSelection
            // this.$refs.multipleTable.toggleRowSelection(row);
            const idsToDelete = []
            this.multipleSelection[1].forEach(item => {
                idsToDelete.push(item.id)
            });
            this.deletModel(idsToDelete)
        },
        // async gets table list data
        async getList() {
            const res = await service.get(`/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.GetLlmModelList?pageIndex=${this.queryList.page}&pageSize=${this.queryList.size}&key=${this.queryList.modelName}`);
            this.tableData = res.data.data.list;
            this.tableTotal = res.data.data.totalCount;
        },

        // table custom row number
        indexMethod(index) {
            let { page, size } = this.queryList
            return (page - 1) * size + index + 1;
            //return  index + 1;
        },
        // table selected column
        handleSelectionChange(val) {
            let { page } = this.queryList
            this.multipleSelection[page] = val;
            // Record the number of selected pages according to the page number
            console.log('tbale 选择', this.multipleSelection)
        },
        // Pagination page size
        handleSizeChange(val) {
            this.queryList.size = val
            this.getList()
        },
        // Pagination Page number
        handleCurrentChange(val) {
            this.queryList.page = val
            this.getList()
        },
        // Click to search
        clickSearch() {
            this.getList()
        },
        changeStatue(row, flag) {
            this.modifyModelName(row.id, row.name, flag)
        }
    }
});
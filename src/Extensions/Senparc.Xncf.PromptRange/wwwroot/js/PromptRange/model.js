var app = new Vue({
    el: "#app",
    data() {
        return {
            // List query parameters.
            queryList: {
                page: 1,
                size: 10,
                modelName: ''
            },
            pageSizes: [10, 20, 30, 50],
            tableTotal: 0,
            tableData: [], // Model list.
            multipleSelection: {}, // Selected models.
            dialogFormVisible: false,
            dialogFormTitle: ncfT('PromptRange.Model.NewTitle'),
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
                    { required: true, message: ncfT('PromptRange.Model.SelectType'), trigger: 'change' }
                ],
                modelName: [
                    { required: true, message: ncfT('PromptRange.Model.EnterName'), trigger: 'blur' }
                ],
                modelAPI: [
                    { required: true, message: ncfT('PromptRange.Model.EnterApi'), trigger: 'blur' }
                ],
                modelAPIkey: [
                    { required: true, message: ncfT('PromptRange.Model.EnterApiKey'), trigger: 'blur' }
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
        //Legacy comment translated to English.
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
        // Create-model button.
        async createBtnFrom() {
            this.dialogFormTitle = ncfT('PromptRange.Model.NewTitle')
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
                this.$message.success(ncfT('PromptRange.Model.AddSuccess'));
            } else {
                this.$message.error(res.data.errorMessage || res.data.message || ncfT('PromptRange.Model.AddFailed'));
            }
        },
        // Edit-model button.
        editBtnFrom(row) {
            this.dialogFormTitle = ncfT('PromptRange.Model.EditTitle')
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
                this.$message.error(res.data.errorMessage || res.data.message || ncfT('PromptRange.Model.ModifyFailed'))
            }
        },
        // Delete a model.
        deleteHandle(row) {
            console.log("row.id", row.id)
            const idsToDelete = []
            idsToDelete.push(row.id)
            this.deletModel(idsToDelete)
        },
        // Delete models.
        async deletModel(idsToDelete) {
            const res = await service.request({
                method: 'delete',
                url: '/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.BatchDelete',
                data: idsToDelete // Send the ID list as the request body.
            });
            console.log(res)
            if (res.data.success) {
                this.getList()
            } else {
                this.$message.error(res.data.errorMessage || res.data.message || ncfT('PromptRange.Model.DeleteFailed'))
            }
        },
        // Batch-delete button.
        btnBatchdelete() {
            console.log('批量删除', this.multipleSelection)
            // Iterate through the selected rows.
            // this.$refs.multipleTable.toggleRowSelection(row);
            const idsToDelete = []
            this.multipleSelection[1].forEach(item => {
                idsToDelete.push(item.id)
            });
            this.deletModel(idsToDelete)
        },
        // Load table data asynchronously.
        async getList() {
            const res = await service.get(`/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.GetLlmModelList?pageIndex=${this.queryList.page}&pageSize=${this.queryList.size}&key=${this.queryList.modelName}`);
            this.tableData = res.data.data.list;
            this.tableTotal = res.data.data.totalCount;
        },

        // Custom table row number.
        indexMethod(index) {
            let { page, size } = this.queryList
            return (page - 1) * size + index + 1;
            //return  index + 1;
        },
        // Selected table rows.
        handleSelectionChange(val) {
            let { page } = this.queryList
            this.multipleSelection[page] = val;
            // Keep the selection for each page.
            console.log('Table selection', this.multipleSelection)
        },
        // Pagination page size.
        handleSizeChange(val) {
            this.queryList.size = val
            this.getList()
        },
        // Pagination page number.
        handleCurrentChange(val) {
            this.queryList.page = val
            this.getList()
        },
        // Search button.
        clickSearch() {
            this.getList()
        },
        changeStatue(row, flag) {
            this.modifyModelName(row.id, row.name, flag)
        }
    }
});

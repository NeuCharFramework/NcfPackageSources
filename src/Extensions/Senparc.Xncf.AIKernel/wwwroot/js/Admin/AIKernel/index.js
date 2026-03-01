var app = new Vue({
    el: "#app",
    data() {
        return {
            keyword: '',
            page: {
                page: 1,
                size: 10
            },
            tableLoading: true,
            tableData: [],
            addFormDialogVisible: false,
            neuCharFormDialogVisible: false, // 新增的对话框可见性  
            addForm: {
                alias: "",
                "modelId": "",
                "deploymentName": "",
                "endpoint": "",
                "aiPlatform": '4',
                "configModelType": '1',
                "organizationId": "",
                "apiKey": "",
                "apiVersion": "",
                "note": "",
                "maxToken": 0,
            },
            neuCharForm: { // 新增的表单数据  
                developerId: "",
                apiKey: ""
            },
            editFormDialogVisible: false,
            editForm: {
                alias: "",
                "modelId": "",
                "deploymentName": "",
                "endpoint": "",
                "aiPlatform": '4',
                "configModelType": '1',
                "organizationId": "",
                "apiKey": "",
                "apiVersion": "",
                "note": "",
                "maxToken": 0,
                "show": true
            },
            total: 0,
            addRules: {
                alias: [
                    { required: true, message: '请输入别名', trigger: 'change' }
                ],
                aiPlatform: [
                    { required: true, message: '请选择平台类型', trigger: 'change' }
                ],
                configModelType: [
                    { required: true, message: '请选择模型类型', trigger: 'change' }
                ],
                modelId: [
                    { required: true, message: '请输入模型名称', trigger: 'blur' }
                ],
                deploymentName: [
                    { required: true, message: '请输入模型部署名称', trigger: 'blur' }
                ],
                apiVersion: [
                    { required: true, message: '请输入API Version', trigger: 'blur' }
                ],
                apiKey: [
                    { required: true, message: '请输入API key', trigger: 'blur' }
                ],
                endpoint: [
                    { required: true, message: '请输入End Point', trigger: 'blur' }
                ],
                organizationId: [
                    { required: true, message: '请输入Organization Id', trigger: 'blur' }
                ]
            },
            neuCharRules: { // 新增的验证规则  
                developerId: [
                    { required: true, message: '请输入 DeveloperId', trigger: 'blur' }
                ],
                apiKey: [
                    { required: true, message: '请输入 ApiKey', trigger: 'blur' }
                ]
            },
            editRules: {
                alias: [
                    { required: true, message: '请输入别名', trigger: 'change' }
                ],
                aiPlatform: [
                    { required: true, message: '请选择平台类型', trigger: 'change' }
                ],
                configModelType: [
                    { required: true, message: '请选择模型类型', trigger: 'change' }
                ],
                modelId: [
                    { required: true, message: '请输入模型名称', trigger: 'blur' }
                ],
                deploymentName: [
                    { required: true, message: '请输入模型部署名称', trigger: 'blur' }
                ],
                apiVersion: [
                    { required: true, message: '请输入API Version', trigger: 'blur' }
                ],
                endpoint: [
                    { required: true, message: '请输入End Point', trigger: 'blur' }
                ]
            }
        }
    },
    watch: {
        'addForm.aiPlatform'(val) {
            if (val === '512') this.addForm.endpoint = this.addForm.endpoint || 'https://api.deepseek.com';
        },
        'editForm.aiPlatform'(val) {
            if (val === '512') this.editForm.endpoint = this.editForm.endpoint || 'https://api.deepseek.com';
        }
    },
    mounted() {
        //wait page load  
        setTimeout(async () => {
            await this.init();
        }, 100)
    },
    methods: {
        async init() {
            await this.getDataList();
        },
        handleSearch() {
            this.page.page = 1;
            this.getDataList();
        },
        resetCondition() {
            this.keyword = '';
            this.page.page = 1;
            this.getDataList();
        },
        handlePageSizeChange(val) {
            this.page.size = val;
            this.page.page = 1;
            this.getDataList();
        },
        handlePageChange(val) {
            this.page.page = val;
            this.getDataList();
        },
        async handleSizeChange(val) {
            this.page.size = val;
            await this.getDataList();
        },
        async handleCurrentChange(val) {
            this.page.page = val;
            await this.getDataList();
        },
        async getDataList() {
            this.tableLoading = true;
            var alias = this.keyword ? (String(this.keyword).trim() || null) : null;
            if (alias && alias.indexOf('%') === -1) alias = '%' + alias + '%';
            var payload = { page: this.page.page, size: this.page.size };
            if (alias) payload.alias = alias;
            try {
                var res = await service.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetPagedListAsync', payload);
                var data = res && res.data && res.data.data;
                this.tableData = (data && Array.isArray(data.data)) ? data.data : [];
                this.total = (data && typeof data.total === 'number') ? data.total : 0;
            } catch (e) {
                this.tableData = [];
                this.total = 0;
            }
            this.tableLoading = false;
        },
        addModel() {
            this.addFormDialogVisible = true;
        },
        addNeuCharModel() {
            this.neuCharFormDialogVisible = true; // 显示对话框  
        },
        copyInfo(key) {
            if (key == null || key === '') {
                this.$message.warning('无 Api Key 可复制');
                return;
            }
            var input = document.createElement('input');
            input.setAttribute('readonly', 'readonly');
            input.setAttribute('value', key);
            document.body.appendChild(input);
            input.select();
            input.setSelectionRange(0, 9999);
            try {
                if (document.execCommand('copy')) {
                    this.$message.success('已复制【******' + key.slice(-4) + '】！');
                }
            } finally {
                document.body.removeChild(input);
            }
        },
        async addModelSubmit() {
            this.$refs.addForm.validate(async (valid) => {
                if (valid) {
                    this.addForm.aiPlatform = parseInt(this.addForm.aiPlatform)
                    this.addForm.configModelType = parseInt(this.addForm.configModelType)
                    this.addForm.maxToken = parseInt(this.addForm.maxToken)
                    await service.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.CreateAsync', {
                        ...this.addForm
                    }
                    ).then(res => {
                        this.$message({
                            type: res.data.success ? 'success' : 'error',
                            message: res.data.success ? '添加成功!' : '添加失败'
                        });
                        if (res.data.success) {
                            this.getDataList()
                            this.clearAddForm()
                            this.addFormDialogVisible = false;
                        }
                    })
                } else {
                    return false;
                }
            });
        },
        async addNeuCharModelSubmit() {
            this.$refs.neuCharForm.validate(async (valid) => {
                if (valid) {
                    await service.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.UpdateNeuCharModels', {
                        developerId: this.neuCharForm.developerId,
                        apiKey: this.neuCharForm.apiKey
                    }).then(res => {
                        if (res.data.success) {
                            this.$message({
                                type: 'success',
                                message: res.data.data // display success message from res.data.data  
                            });
                            this.getDataList()
                            this.clearNeuCharForm()
                            this.neuCharFormDialogVisible = false;
                        } else {
                            this.$message({
                                type: 'error',
                                message: res.data.errorMessage
                            });
                        }
                    })
                } else {
                    return false;
                }
            });
        },
        clearAddForm() {
            this.addForm = {
                "alias": "",
                "modelId": "",
                "deploymentName": "",
                "endpoint": "",
                "aiPlatform": '4',
                "configModelType": '1',
                "organizationId": "",
                "apiKey": "",
                "apiVersion": "",
                "note": "",
                "maxToken": 0,
            }
        },
        clearNeuCharForm() { // 新增的清理表单方法  
            this.neuCharForm = {
                developerId: "",
                apiKey: ""
            }
        },
        clearEditForm() {
            this.editForm = {
                "alias": "",
                "modelId": "",
                "deploymentName": "",
                "endpoint": "",
                "aiPlatform": '4',
                "configModelType": '1',
                "organizationId": "",
                "apiKey": "",
                "apiVersion": "",
                "note": "",
                "maxToken": 0,
                "show": true
            }
        },
        async editModelSubmit() {
            this.$refs.editForm.validate(async (valid) => {
                if (valid) {
                    this.editForm.aiPlatform = parseInt(this.editForm.aiPlatform)
                    this.editForm.configModelType = parseInt(this.editForm.configModelType)
                    this.editForm.maxToken = parseInt(this.editForm.maxToken)
                    // clear empty value  
                    for (const key in this.editForm) {
                        if (this.editForm.hasOwnProperty(key)) {
                            const element = this.editForm[key];
                            if (element === null || element === undefined) {
                                delete this.editForm[key]
                            }
                        }
                    }

                    await service.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.EditAsync', {
                        ...this.editForm
                    }).then(res => {
                        this.$message({
                            type: res.data.success ? 'success' : 'error',
                            message: res.data.success ? '编辑成功!' : '编辑失败'
                        });
                        if (res.data.success) {
                            this.clearEditForm()
                            this.getDataList()
                            this.editFormDialogVisible = false;
                        }
                    })
                } else {
                    return false;
                }
            });
        },
        dateformatter(date) {
            return new Date(date).toLocaleString()
        },
        editModel(row) {
            this.editFormDialogVisible = true;
            this.editForm = {
                ...row,
                aiPlatform: row.aiPlatform.toString(),
                configModelType: row.configModelType.toString()
            };
        },
        deleteModel(row) {
            this.$confirm(`此操作将永久删除【${row.alias}】模型, 是否继续?`, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(async () => {
                await service.delete('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.DeleteAsync', {
                    params: {
                        id: row.id
                    }
                }).then(async res => {
                    this.$message({
                        type: res.data.success ? 'success' : 'error',
                        message: res.data.success ? '删除成功!' : '删除失败'
                    });
                    await this.getDataList().then(() => {
                        if (this.tableData.length === 0 && this.page.page > 1) {
                            this.page.page--;
                            this.getDataList();
                        }
                    })
                })
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: '已取消删除'
                });
            });
        },
    },
});  

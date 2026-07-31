var app = new Vue({
    el: "#app",
    data() {
        return {
            page: {
                page: 1,
                size: 10
            },
            tableLoading: true,
            tableData: [],
            neuCharAiModelList: [
                "text-davinci-003",
                "gpt-4",
                "text-embedding-ada-002",
                "gpt-35-turbo",
                "gpt-35-turbo-instruct",
                "dall-e-3",
                "DeepSeek-R1"
            ],
            deepSeekModelList: [
                "deepseek-chat",
                "deepseek-coder"
            ],
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
                    { required: true, message: ncfT('AIKernel.Vector.AliasRequired'), trigger: 'change' }
                ],
                aiPlatform: [
                    { required: true, message: ncfT('AIKernel.Model.PlatformRequired'), trigger: 'change' }
                ],
                configModelType: [
                    { required: true, message: ncfT('AIKernel.Model.TypeRequired'), trigger: 'change' }
                ],
                modelId: [
                    { required: true, message: ncfT('AIKernel.Vector.ModelNameRequired'), trigger: 'blur' }
                ],
                deploymentName: [
                    { required: true, message: ncfT('AIKernel.Model.DeploymentRequired'), trigger: 'blur' }
                ],
                apiVersion: [
                    { required: true, message: ncfT('AIKernel.Model.ApiVersionRequired'), trigger: 'blur' }
                ],
                apiKey: [
                    { required: true, message: ncfT('AIKernel.Model.ApiKeyRequired'), trigger: 'blur' }
                ],
                endpoint: [
                    { required: true, message: ncfT('AIKernel.Model.EndpointRequired'), trigger: 'blur' }
                ],
                organizationId: [
                    { required: true, message: ncfT('AIKernel.Model.OrganizationRequired'), trigger: 'blur' }
                ]
            },
            neuCharRules: { // 新增的验证规则  
                developerId: [
                    { required: true, message: ncfT('AIKernel.Model.DeveloperIdRequired'), trigger: 'blur' }
                ],
                apiKey: [
                    { required: true, message: ncfT('AIKernel.Model.ApiKeyRequired'), trigger: 'blur' }
                ]
            },
            editRules: {
                alias: [
                    { required: true, message: ncfT('AIKernel.Vector.AliasRequired'), trigger: 'change' }
                ],
                aiPlatform: [
                    { required: true, message: ncfT('AIKernel.Model.PlatformRequired'), trigger: 'change' }
                ],
                configModelType: [
                    { required: true, message: ncfT('AIKernel.Model.TypeRequired'), trigger: 'change' }
                ],
                modelId: [
                    { required: true, message: ncfT('AIKernel.Vector.ModelNameRequired'), trigger: 'blur' }
                ],
                deploymentName: [
                    { required: true, message: ncfT('AIKernel.Model.DeploymentRequired'), trigger: 'blur' }
                ],
                apiVersion: [
                    { required: true, message: ncfT('AIKernel.Model.ApiVersionRequired'), trigger: 'blur' }
                ],
                endpoint: [
                    { required: true, message: ncfT('AIKernel.Model.EndpointRequired'), trigger: 'blur' }
                ]
            }
        }
    },
    mounted() {
        //wait page load  
        setTimeout(async () => {
            await this.init();
        }, 100)
    },
    methods: {
        getSelectableModelList(defaultModelList, currentModelId) {
            const modelList = [...defaultModelList];
            if (currentModelId && !modelList.includes(currentModelId)) {
                modelList.unshift(currentModelId);
            }
            return modelList;
        },
        async init() {
            await this.getDataList();
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
            this.tableLoading = true
            await service.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetPagedListAsync', {
                "page": this.page.page,
                "size": this.page.size,
            })
                .then(res => {
                    console.log(res)
                    this.tableData = res.data.data.data;
                    this.total = res.data.data.total;
                    this.tableLoading = false
                })
        },
        addModel() {
            this.addFormDialogVisible = true;
        },
        addNeuCharModel() {
            this.neuCharFormDialogVisible = true; // 显示对话框  
        },
        async copyInfo(key) {
            let copied = false
            try {
                if (window.isSecureContext && navigator.clipboard && navigator.clipboard.writeText) {
                    await navigator.clipboard.writeText(key)
                    copied = true
                }
            } catch (error) {
                console.warn('Clipboard API is unavailable, using the compatibility fallback.', error)
            }

            if (!copied) {
                const input = document.createElement('textarea')
                input.setAttribute('readonly', 'readonly')
                input.value = key
                input.style.position = 'fixed'
                input.style.opacity = '0'
                document.body.appendChild(input)
                input.select()
                input.setSelectionRange(0, key.length)
                try {
                    copied = document.execCommand('copy')
                } finally {
                    input.remove()
                }
            }

            if (copied) {
                this.$message.success(ncfT('AIKernel.Vector.CopySuccess', key.slice(-4)))
            } else {
                this.$message.error(ncfT('AIKernel.Vector.CopyFailed'))
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
                            message: res.data.success ? ncfT('AIKernel.Vector.AddSuccess') : ncfT('AIKernel.Vector.AddFailed')
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
                            message: res.data.success ? ncfT('AIKernel.Vector.EditSuccess') : ncfT('AIKernel.Vector.EditFailed')
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
            this.$confirm(ncfT('AIKernel.Vector.DeleteConfirm', row.alias), ncfT('AIKernel.Vector.DeleteConfirmTitle'), {
                confirmButtonText: ncfT('AIKernel.Vector.Confirm'),
                cancelButtonText: ncfT('AIKernel.Vector.Cancel'),
                type: 'warning'
            }).then(async () => {
                await service.delete('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.DeleteAsync', {
                    params: {
                        id: row.id
                    }
                }).then(async res => {
                    this.$message({
                        type: res.data.success ? 'success' : 'error',
                        message: res.data.success ? ncfT('AIKernel.Vector.DeleteSuccess') : ncfT('AIKernel.Vector.DeleteFailed')
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
                    message: ncfT('AIKernel.Vector.CancelDelete')
                });
            });
        },
    },
});  

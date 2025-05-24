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
            addFormDialogVisible: false,
            addForm: {
                red: 128,
                green: 128,
                blue: 128
            },
            editFormDialogVisible: false,
            editForm: {
                id: 0,
                red: 128,
                green: 128,
                blue: 128
            },
            total: 0,
            addRules: {
                red: [
                    { required: true, message: '请设置红色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '红色值范围为0-255', trigger: 'change' }
                ],
                green: [
                    { required: true, message: '请设置绿色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '绿色值范围为0-255', trigger: 'change' }
                ],
                blue: [
                    { required: true, message: '请设置蓝色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '蓝色值范围为0-255', trigger: 'change' }
                ]
            },
            editRules: {
                red: [
                    { required: true, message: '请设置红色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '红色值范围为0-255', trigger: 'change' }
                ],
                green: [
                    { required: true, message: '请设置绿色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '绿色值范围为0-255', trigger: 'change' }
                ],
                blue: [
                    { required: true, message: '请设置蓝色值', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: '蓝色值范围为0-255', trigger: 'change' }
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
            await service.get('/Admin/Template_XncfName/DatabaseSampleIndex?handler=ColorList', {
                params: {
                    pageIndex: this.page.page,
                    pageSize: this.page.size,
                    orderField: "Id desc",
                    keyword: ""
                }
            })
                .then(res => {
                    console.log('API Response:', res)
                    if (res.data && res.data.list) {
                        this.tableData = res.data.list;
                        this.total = res.data.totalCount;
                    } else {
                        console.warn('No data found in response:', res.data);
                        this.tableData = [];
                        this.total = 0;
                    }
                    this.tableLoading = false
                })
                .catch(error => {
                    console.error('获取数据失败:', error);
                    this.tableLoading = false;
                    this.$message.error('获取数据失败: ' + (error.message || error));
                });
        },
        addColor() {
            this.addFormDialogVisible = true;
        },
        refreshList() {
            this.getDataList();
        },
        async addColorSubmit() {
            this.$refs.addForm.validate(async (valid) => {
                if (valid) {
                    await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=CreateColor', {
                        red: this.addForm.red,
                        green: this.addForm.green,
                        blue: this.addForm.blue
                    })
                        .then(res => {
                            this.$message({
                                type: res.data.success ? 'success' : 'error',
                                message: res.data.message
                            });
                            if (res.data.success) {
                                this.getDataList()
                                this.clearAddForm()
                                this.addFormDialogVisible = false;
                            }
                        })
                        .catch(error => {
                            console.error('创建失败:', error);
                            this.$message.error('创建失败');
                        });
                } else {
                    return false;
                }
            });
        },
        clearAddForm() {
            this.addForm = {
                red: 128,
                green: 128,
                blue: 128
            };
            if (this.$refs.addForm) {
                this.$refs.addForm.resetFields();
            }
        },
        clearEditForm() {
            this.editForm = {
                id: 0,
                red: 128,
                green: 128,
                blue: 128
            };
            if (this.$refs.editForm) {
                this.$refs.editForm.resetFields();
            }
        },
        async editColorSubmit() {
            this.$refs.editForm.validate(async (valid) => {
                if (valid) {
                    await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=UpdateColor', {
                        id: this.editForm.id,
                        red: this.editForm.red,
                        green: this.editForm.green,
                        blue: this.editForm.blue
                    })
                        .then(res => {
                            this.$message({
                                type: res.data.success ? 'success' : 'error',
                                message: res.data.message
                            });
                            if (res.data.success) {
                                this.getDataList()
                                this.clearEditForm()
                                this.editFormDialogVisible = false;
                            }
                        })
                        .catch(error => {
                            console.error('更新失败:', error);
                            this.$message.error('更新失败');
                        });
                } else {
                    return false;
                }
            });
        },
        dateformatter(date) {
            return dayjs(date).format('YYYY-MM-DD HH:mm:ss');
        },
        editColor(row) {
            this.editForm = {
                id: row.id,
                red: row.red,
                green: row.green,
                blue: row.blue
            };
            this.editFormDialogVisible = true;
        },
        deleteColor(row) {
            this.$confirm('此操作将永久删除该颜色, 是否继续?', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(async () => {
                await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=DeleteColor', {
                    id: row.id
                })
                    .then(res => {
                        this.$message({
                            type: res.data.success ? 'success' : 'error',
                            message: res.data.message
                        });
                        if (res.data.success) {
                            this.getDataList();
                        }
                    })
                    .catch(error => {
                        console.error('删除失败:', error);
                        this.$message.error('删除失败');
                    });
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: '已取消删除'
                });
            });
        },
        async randomizeColor(row) {
            await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=RandomizeColor', {
                id: row.id
            })
                .then(res => {
                    this.$message({
                        type: res.data.success ? 'success' : 'error',
                        message: res.data.message
                    });
                    if (res.data.success) {
                        this.getDataList();
                    }
                })
                .catch(error => {
                    console.error('随机化失败:', error);
                    this.$message.error('随机化失败');
                });
        },
        randomizeForm() {
            this.addForm.red = Math.floor(Math.random() * 256);
            this.addForm.green = Math.floor(Math.random() * 256);
            this.addForm.blue = Math.floor(Math.random() * 256);
        },
        randomizeEditForm() {
            this.editForm.red = Math.floor(Math.random() * 256);
            this.editForm.green = Math.floor(Math.random() * 256);
            this.editForm.blue = Math.floor(Math.random() * 256);
        }
    }
}); 
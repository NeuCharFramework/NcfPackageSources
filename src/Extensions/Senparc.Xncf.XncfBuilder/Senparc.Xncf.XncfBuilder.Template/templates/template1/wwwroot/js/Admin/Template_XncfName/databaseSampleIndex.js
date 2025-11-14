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
            showDebug: false,
            addFormDialogVisible: false,
            addForm: {
                red: 128,
                green: 128,
                blue: 128,
                additionNote: ''
            },
            editFormDialogVisible: false,
            editForm: {
                id: 0,
                red: 128,
                green: 128,
                blue: 128,
                additionNote: ''
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
                    console.log('=== API Response Debug ===');
                    console.log('Complete Response:', res);
                    console.log('Response Data:', res.data);
                    console.log('Response Data Type:', typeof res.data);
                    console.log('Has res.data.data?:', res.data && res.data.data);
                    console.log('Has res.data.data.list?:', res.data && res.data.data && res.data.data.list);
                    console.log('res.data.data.list value:', res.data && res.data.data ? res.data.data.list : 'nested data not found');
                    console.log('==================');
                    
                    // 尝试多种可能的数据结构
                    let dataList = null;
                    let totalCount = 0;
                    let dataSource = '';
                    
                    if (res.data && res.data.data && res.data.data.data && res.data.data.data.list) {
                        // NCF框架标准格式 + 新的API格式: {data: {data: {success, message, data: {list, totalCount}}}}
                        dataList = res.data.data.data.list;
                        totalCount = res.data.data.data.totalCount || 0;
                        dataSource = 'NCF标准格式: res.data.data.data.list';
                        console.log('✅ 使用NCF标准格式: res.data.data.data.list');
                        console.log('✅ List数据:', dataList);
                        console.log('✅ TotalCount:', totalCount);
                    } else if (res.data && res.data.data && res.data.data.list) {
                        // 简单格式: {data: {list, totalCount}}
                        dataList = res.data.data.list;
                        totalCount = res.data.data.totalCount || 0;
                        dataSource = '简单格式: res.data.data.list';
                        console.log('✅ 使用简单格式: res.data.data.list');
                    } else if (res.data && Array.isArray(res.data)) {
                        // 如果data直接是数组
                        dataList = res.data;
                        totalCount = res.data.length;
                        dataSource = '数组格式: res.data (array)';
                        console.log('✅ 使用数组格式: res.data (array)');
                    } else if (res && res.list) {
                        // 如果list在顶层
                        dataList = res.list;
                        totalCount = res.totalCount || 0;
                        dataSource = '顶层格式: res.list';
                        console.log('✅ 使用顶层格式: res.list');
                    } else {
                        console.error('❌ 无法识别的数据格式:', res);
                        console.log('🔍 尝试的路径:');
                        console.log('- res.data.data.list:', res.data && res.data.data ? res.data.data.list : 'not found');
                        console.log('- res.data.list:', res.data ? res.data.list : 'not found');
                        console.log('- res.data (array):', res.data && Array.isArray(res.data) ? 'is array' : 'not array');
                        console.log('- res.list:', res.list ? res.list : 'not found');
                        dataList = [];
                        totalCount = 0;
                        dataSource = '无法识别格式';
                    }
                    
                    console.log('🎯 Final dataList:', dataList);
                    console.log('🎯 Final totalCount:', totalCount);
                    console.log('🎯 Data source:', dataSource);
                    
                    // 数据赋值前的状态
                    console.log('📋 赋值前 tableData:', this.tableData);
                    console.log('📋 赋值前 total:', this.total);
                    
                    this.tableData = dataList || [];
                    this.total = totalCount;
                    
                    // 数据赋值后的状态
                    console.log('📋 赋值后 tableData:', this.tableData);
                    console.log('📋 赋值后 tableData.length:', this.tableData.length);
                    console.log('📋 赋值后 total:', this.total);
                    
                    // 强制Vue更新
                    this.$forceUpdate();
                    console.log('🔄 Vue已强制更新');
                    
                    // 延迟检查数据是否正确绑定
                    setTimeout(() => {
                        console.log('⏰ 延迟检查 tableData:', this.tableData);
                        console.log('⏰ 延迟检查 tableData.length:', this.tableData ? this.tableData.length : 'null');
                    }, 100);
                    
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
                    console.log('📤 发送创建请求:', {
                        red: this.addForm.red,
                        green: this.addForm.green,
                        blue: this.addForm.blue,
                        additionNote: this.addForm.additionNote
                    });
                    
                    await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=CreateColor', {
                        red: this.addForm.red,
                        green: this.addForm.green,
                        blue: this.addForm.blue,
                        additionNote: this.addForm.additionNote
                    }, {
                        headers: {
                            'Content-Type': 'application/json'
                        }
                    })
                        .then(res => {
                            console.log('📥 创建响应:', res);
                            // 兼容NCF框架的嵌套响应格式
                            const responseData = res.data.data || res.data;
                            this.$message({
                                type: responseData.success ? 'success' : 'error',
                                message: responseData.message || '操作完成'
                            });
                            if (responseData.success) {
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
                blue: 128,
                additionNote: ''
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
                blue: 128,
                additionNote: ''
            };
            if (this.$refs.editForm) {
                this.$refs.editForm.resetFields();
            }
        },
        async editColorSubmit() {
            this.$refs.editForm.validate(async (valid) => {
                if (valid) {
                    console.log('📤 发送更新请求:', {
                        id: this.editForm.id,
                        red: this.editForm.red,
                        green: this.editForm.green,
                        blue: this.editForm.blue,
                        additionNote: this.editForm.additionNote
                    });
                    
                    await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=UpdateColor', {
                        id: this.editForm.id,
                        red: this.editForm.red,
                        green: this.editForm.green,
                        blue: this.editForm.blue,
                        additionNote: this.editForm.additionNote
                    }, {
                        headers: {
                            'Content-Type': 'application/json'
                        }
                    })
                        .then(res => {
                            console.log('📥 更新响应:', res);
                            // 兼容NCF框架的嵌套响应格式
                            const responseData = res.data.data || res.data;
                            this.$message({
                                type: responseData.success ? 'success' : 'error',
                                message: responseData.message || '操作完成'
                            });
                            if (responseData.success) {
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
            if (!date) return '';
            
            try {
                // 使用原生JavaScript格式化日期
                const d = new Date(date);
                
                // 检查日期是否有效
                if (isNaN(d.getTime())) {
                    return date; // 如果无法解析，返回原始值
                }
                
                // 格式化为 YYYY-MM-DD HH:mm:ss
                const year = d.getFullYear();
                const month = String(d.getMonth() + 1).padStart(2, '0');
                const day = String(d.getDate()).padStart(2, '0');
                const hours = String(d.getHours()).padStart(2, '0');
                const minutes = String(d.getMinutes()).padStart(2, '0');
                const seconds = String(d.getSeconds()).padStart(2, '0');
                
                return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
            } catch (error) {
                console.warn('日期格式化错误:', error, '原始值:', date);
                return date; // 如果格式化失败，返回原始值
            }
        },
        editColor(row) {
            this.editForm = {
                id: row.id,
                red: row.red,
                green: row.green,
                blue: row.blue,
                additionNote: row.additionNote || ''
            };
            this.editFormDialogVisible = true;
        },
        deleteColor(row) {
            this.$confirm('此操作将永久删除该颜色, 是否继续?', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(async () => {
                console.log('📤 发送删除请求:', { id: row.id });
                
                await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=DeleteColor', {
                    id: row.id
                }, {
                    headers: {
                        'Content-Type': 'application/json'
                    }
                })
                    .then(res => {
                        console.log('📥 删除响应:', res);
                        // 兼容NCF框架的嵌套响应格式
                        const responseData = res.data.data || res.data;
                        this.$message({
                            type: responseData.success ? 'success' : 'error',
                            message: responseData.message || '操作完成'
                        });
                        if (responseData.success) {
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
            console.log('📤 发送随机化请求:', { id: row.id });
            
            await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=RandomizeColor', {
                id: row.id
            }, {
                headers: {
                    'Content-Type': 'application/json'
                }
            })
                .then(res => {
                    console.log('📥 随机化响应:', res);
                    // 兼容NCF框架的嵌套响应格式
                    const responseData = res.data.data || res.data;
                    this.$message({
                        type: responseData.success ? 'success' : 'error',
                        message: responseData.message || '操作完成'
                    });
                    if (responseData.success) {
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
        },
        debugInfo() {
            this.showDebug = !this.showDebug;
            console.log('=== Vue Component Debug Info ===');
            console.log('Current tableData:', this.tableData);
            console.log('tableData length:', this.tableData ? this.tableData.length : 'null/undefined');
            console.log('Total:', this.total);
            console.log('Page:', this.page);
            console.log('Table Loading:', this.tableLoading);
            console.log('Show Debug:', this.showDebug);
            console.log('Vue instance $el:', this.$el);
            console.log('================================');
            
            // 测试Vue响应性
            if (this.tableData && this.tableData.length === 0) {
                console.log('测试：添加假数据');
                this.tableData = [
                    {id: 999, red: 255, green: 0, blue: 0, addTime: new Date().toISOString(), lastUpdateTime: new Date().toISOString(), remark: 'test'}
                ];
                this.total = 1;
                setTimeout(() => {
                    console.log('2秒后清除假数据');
                    this.tableData = [];
                    this.total = 0;
                }, 2000);
            }
        }
    }
}); 
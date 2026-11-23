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
                    { required: true, message: ncfST('Frontend.Template_XncfName.SetTheRedValue'), trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: ncfST('Frontend.Template_XncfName.RedValueMustBeBetweenZeroAnd255'), trigger: 'change' }
                ],
                green: [
                    { required: true, message: ncfST('Frontend.Template_XncfName.SetTheGreenValue'), trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: ncfST('Frontend.Template_XncfName.GreenValueMustBeBetweenZeroAnd255'), trigger: 'change' }
                ],
                blue: [
                    { required: true, message: ncfST('Frontend.Template_XncfName.SetTheBlueValue'), trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: ncfST('Frontend.Template_XncfName.BlueValueMustBeBetweenZeroAnd255'), trigger: 'change' }
                ]
            },
            editRules: {
                red: [
                    { required: true, message: ncfST('Frontend.Template_XncfName.SetTheRedValue'), trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: ncfST('Frontend.Template_XncfName.RedValueMustBeBetweenZeroAnd255'), trigger: 'change' }
                ],
                green: [
                    { required: true, message: ncfST('Frontend.Template_XncfName.SetTheGreenValue'), trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: ncfST('Frontend.Template_XncfName.GreenValueMustBeBetweenZeroAnd255'), trigger: 'change' }
                ],
                blue: [
                    { required: true, message: ncfST('Frontend.Template_XncfName.SetTheBlueValue'), trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: ncfST('Frontend.Template_XncfName.BlueValueMustBeBetweenZeroAnd255'), trigger: 'change' }
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

                    //Legacy comment translated to English.
                    let dataList = null;
                    let totalCount = 0;
                    let dataSource = '';

                    if (res.data && res.data.data && res.data.data.data && res.data.data.data.list) {
                        //Legacy comment translated to English.
                        dataList = res.data.data.data.list;
                        totalCount = res.data.data.data.totalCount || 0;
                        dataSource = 'NCF standard format: res.data.data.data.list';
                        console.log('✅ UsingNCF standard format: res.data.data.data.list');
                        console.log('✅ List data:', dataList);
                        console.log('✅ TotalCount:', totalCount);
                    } else if (res.data && res.data.data && res.data.data.list) {
                        //Legacy comment translated to English.
                        dataList = res.data.data.list;
                        totalCount = res.data.data.totalCount || 0;
                        dataSource = 'simple format: res.data.data.list';
                        console.log('✅ Usingsimple format: res.data.data.list');
                    } else if (res.data && Array.isArray(res.data)) {
                        //Legacy comment translated to English.
                        dataList = res.data;
                        totalCount = res.data.length;
                        dataSource = 'array format: res.data (array)';
                        console.log('✅ Usingarray format: res.data (array)');
                    } else if (res && res.list) {
                        //Legacy comment translated to English.
                        dataList = res.list;
                        totalCount = res.totalCount || 0;
                        dataSource = 'top-level format: res.list';
                        console.log('✅ Usingtop-level format: res.list');
                    } else {
                        console.error('❌ unrecognized data format:', res);
                        console.log('🔍 Tried paths:');
                        console.log('- res.data.data.list:', res.data && res.data.data ? res.data.data.list : 'not found');
                        console.log('- res.data.list:', res.data ? res.data.list : 'not found');
                        console.log('- res.data (array):', res.data && Array.isArray(res.data) ? 'is array' : 'not array');
                        console.log('- res.list:', res.list ? res.list : 'not found');
                        dataList = [];
                        totalCount = 0;
                        dataSource = 'unrecognized format';
                    }

                    console.log('🎯 Final dataList:', dataList);
                    console.log('🎯 Final totalCount:', totalCount);
                    console.log('🎯 Data source:', dataSource);

                    //Legacy comment translated to English.
                    console.log('📋 before assignment tableData:', this.tableData);
                    console.log('📋 before assignment total:', this.total);

                    this.tableData = dataList || [];
                    this.total = totalCount;

                    //Legacy comment translated to English.
                    console.log('📋 after assignment tableData:', this.tableData);
                    console.log('📋 after assignment tableData.length:', this.tableData.length);
                    console.log('📋 after assignment total:', this.total);

                    //Legacy comment translated to English.
                    this.$forceUpdate();
                    console.log('🔄 Vueforce-updated by Vue');

                    //Legacy comment translated to English.
                    setTimeout(() => {
                        console.log('⏰ delayed check tableData:', this.tableData);
                        console.log('⏰ delayed check tableData.length:', this.tableData ? this.tableData.length : 'null');
                    }, 100);

                    this.tableLoading = false
                })
                .catch(error => {
                    console.error('Failed to load data:', error);
                    this.tableLoading = false;
                    this.$message.error(ncfST('Frontend.Template_XncfName.FailedToLoadDataValue', error.message || error));
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
                    console.log('📤 Sending create request:', {
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
                            console.log('📥 Create response:', res);
                            // support the nested response format used by the NCF framework
                            const responseData = res.data.data || res.data;
                            this.$message({
                                type: responseData.success ? 'success' : 'error',
                                message: responseData.message || ncfST('Frontend.Template_XncfName.OperationCompleted')
                            });
                            if (responseData.success) {
                                this.getDataList()
                                this.clearAddForm()
                                this.addFormDialogVisible = false;
                            }
                        })
                        .catch(error => {
                            console.error('Create failed:', error);
                            this.$message.error(ncfST('Frontend.Template_XncfName.CreationFailed'));
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
                    console.log('📤 Sending update request:', {
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
                            console.log('📥 Update response:', res);
                            // support the nested response format used by the NCF framework
                            const responseData = res.data.data || res.data;
                            this.$message({
                                type: responseData.success ? 'success' : 'error',
                                message: responseData.message || ncfST('Frontend.Template_XncfName.OperationCompleted')
                            });
                            if (responseData.success) {
                                this.getDataList()
                                this.clearEditForm()
                                this.editFormDialogVisible = false;
                            }
                        })
                        .catch(error => {
                            console.error('Update failed:', error);
                            this.$message.error(ncfST('Frontend.Template_XncfName.UpdateFailed'));
                        });
                } else {
                    return false;
                }
            });
        },
        dateformatter(date) {
            if (!date) return '';

            try {
                //Legacy comment translated to English.
                const d = new Date(date);

                //Legacy comment translated to English.
                if (isNaN(d.getTime())) {
                    return date; //Legacy comment translated to English.
                }

                //Legacy comment translated to English.
                const year = d.getFullYear();
                const month = String(d.getMonth() + 1).padStart(2, '0');
                const day = String(d.getDate()).padStart(2, '0');
                const hours = String(d.getHours()).padStart(2, '0');
                const minutes = String(d.getMinutes()).padStart(2, '0');
                const seconds = String(d.getSeconds()).padStart(2, '0');

                return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
            } catch (error) {
                console.warn('Date formatting failed:', error, 'Original value:', date);
                return date; //Legacy comment translated to English.
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
            this.$confirm(ncfST('Frontend.Template_XncfName.ThisWillPermanentlyDeleteThisColorContinue'), ncfST('Frontend.Template_XncfName.Notice'), {
                confirmButtonText: ncfST('Frontend.Template_XncfName.Confirm'),
                cancelButtonText: ncfST('Frontend.Template_XncfName.CancelAlternate'),
                type: 'warning'
            }).then(async () => {
                console.log('📤 Sending delete request:', { id: row.id });

                await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=DeleteColor', {
                    id: row.id
                }, {
                    headers: {
                        'Content-Type': 'application/json'
                    }
                })
                    .then(res => {
                        console.log('📥 Delete response:', res);
                        // support the nested response format used by the NCF framework
                        const responseData = res.data.data || res.data;
                        this.$message({
                            type: responseData.success ? 'success' : 'error',
                            message: responseData.message || ncfST('Frontend.Template_XncfName.OperationCompleted')
                        });
                        if (responseData.success) {
                            this.getDataList();
                        }
                    })
                    .catch(error => {
                        console.error('Delete failed:', error);
                        this.$message.error(ncfST('Frontend.Template_XncfName.DeleteFailed'));
                    });
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: ncfST('Frontend.Template_XncfName.DeletionCancelled')
                });
            });
        },
        async randomizeColor(row) {
            console.log('📤 Sending randomization request:', { id: row.id });

            await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=RandomizeColor', {
                id: row.id
            }, {
                headers: {
                    'Content-Type': 'application/json'
                }
            })
                .then(res => {
                    console.log('📥 Randomization response:', res);
                    // support the nested response format used by the NCF framework
                    const responseData = res.data.data || res.data;
                    this.$message({
                        type: responseData.success ? 'success' : 'error',
                        message: responseData.message || ncfST('Frontend.Template_XncfName.OperationCompleted')
                    });
                    if (responseData.success) {
                        this.getDataList();
                    }
                })
                .catch(error => {
                    console.error('Randomization failed:', error);
                    this.$message.error(ncfST('Frontend.Template_XncfName.RandomizationFailed'));
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

            //Legacy comment translated to English.
            if (this.tableData && this.tableData.length === 0) {
                console.log('Test: adding mock data');
                this.tableData = [
                    {id: 999, red: 255, green: 0, blue: 0, addTime: new Date().toISOString(), lastUpdateTime: new Date().toISOString(), remark: 'test'}
                ];
                this.total = 1;
                setTimeout(() => {
                    console.log('2Clearing mock data after 2 seconds');
                    this.tableData = [];
                    this.total = 0;
                }, 2000);
            }
        }
    }
});

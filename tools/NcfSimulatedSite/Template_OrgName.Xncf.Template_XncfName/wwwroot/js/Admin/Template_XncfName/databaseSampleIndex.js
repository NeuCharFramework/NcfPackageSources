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
                    { required: true, message: 'Please set the red value', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: 'Red value range is 0-255', trigger: 'change' }
                ],
                green: [
                    { required: true, message: 'Please set the green value', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: 'Green value range is 0-255', trigger: 'change' }
                ],
                blue: [
                    { required: true, message: 'Please set the blue value', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: 'Blue value range is 0-255', trigger: 'change' }
                ]
            },
            editRules: {
                red: [
                    { required: true, message: 'Please set the red value', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: 'Red value range is 0-255', trigger: 'change' }
                ],
                green: [
                    { required: true, message: 'Please set the green value', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: 'Green value range is 0-255', trigger: 'change' }
                ],
                blue: [
                    { required: true, message: 'Please set the blue value', trigger: 'change' },
                    { type: 'number', min: 0, max: 255, message: 'Blue value range is 0-255', trigger: 'change' }
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

                    // Try multiple possible data structures
                    let dataList = null;
                    let totalCount = 0;
                    let dataSource = '';

                    if (res.data && res.data.data && res.data.data.data && res.data.data.data.list) {
                        // NCF framework standard format + new API format: {data: {data: {success, message, data: {list, totalCount}}}}
                        dataList = res.data.data.data.list;
                        totalCount = res.data.data.data.totalCount || 0;
                        dataSource = 'NCF standard format: res.data.data.data.list';
                        console.log('Using NCF standard format: res.data.data.data.list');
                        console.log('List data:', dataList);
                        console.log('TotalCount:', totalCount);
                    } else if (res.data && res.data.data && res.data.data.list) {
                        // Simple format: {data: {list, totalCount}}
                        dataList = res.data.data.list;
                        totalCount = res.data.data.totalCount || 0;
                        dataSource = 'Simple format: res.data.data.list';
                        console.log('Using simple format: res.data.data.list');
                    } else if (res.data && Array.isArray(res.data)) {
                        // If data is directly an array
                        dataList = res.data;
                        totalCount = res.data.length;
                        dataSource = 'Array format: res.data (array)';
                        console.log('Using array format: res.data (array)');
                    } else if (res && res.list) {
                        // If list is at top level
                        dataList = res.list;
                        totalCount = res.totalCount || 0;
                        dataSource = 'Top-level format: res.list';
                        console.log('Using top-level format: res.list');
                    } else {
                        console.error('Unrecognized data format:', res);
                        console.log('Tried paths:');
                        console.log('- res.data.data.list:', res.data && res.data.data ? res.data.data.list : 'not found');
                        console.log('- res.data.list:', res.data ? res.data.list : 'not found');
                        console.log('- res.data (array):', res.data && Array.isArray(res.data) ? 'is array' : 'not array');
                        console.log('- res.list:', res.list ? res.list : 'not found');
                        dataList = [];
                        totalCount = 0;
                        dataSource = 'Unrecognized format';
                    }

                    console.log('Final dataList:', dataList);
                    console.log('Final totalCount:', totalCount);
                    console.log('Data source:', dataSource);

                    // State before assignment
                    console.log('Before assignment tableData:', this.tableData);
                    console.log('Before assignment total:', this.total);

                    this.tableData = dataList || [];
                    this.total = totalCount;

                    // State after assignment
                    console.log('After assignment tableData:', this.tableData);
                    console.log('After assignment tableData.length:', this.tableData.length);
                    console.log('After assignment total:', this.total);

                    // Force Vue update
                    this.$forceUpdate();
                    console.log('Vue force updated');

                    // Delayed check if data is correctly bound
                    setTimeout(() => {
                        console.log('Delayed check tableData:', this.tableData);
                        console.log('Delayed check tableData.length:', this.tableData ? this.tableData.length : 'null');
                    }, 100);

                    this.tableLoading = false
                })
                .catch(error => {
                    console.error('Failed to get data:', error);
                    this.tableLoading = false;
                    this.$message.error('Failed to get data: ' + (error.message || error));
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
                    console.log('Sending create request:', {
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
                            console.log('Create response:', res);
                            // Compatible with NCF framework nested response format
                            const responseData = res.data.data || res.data;
                            this.$message({
                                type: responseData.success ? 'success' : 'error',
                                message: responseData.message || 'Operation completed'
                            });
                            if (responseData.success) {
                                this.getDataList()
                                this.clearAddForm()
                                this.addFormDialogVisible = false;
                            }
                        })
                        .catch(error => {
                            console.error('Create failed:', error);
                            this.$message.error('Create failed');
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
                    console.log('Sending update request:', {
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
                            console.log('Update response:', res);
                            // Compatible with NCF framework nested response format
                            const responseData = res.data.data || res.data;
                            this.$message({
                                type: responseData.success ? 'success' : 'error',
                                message: responseData.message || 'Operation completed'
                            });
                            if (responseData.success) {
                                this.getDataList()
                                this.clearEditForm()
                                this.editFormDialogVisible = false;
                            }
                        })
                        .catch(error => {
                            console.error('Update failed:', error);
                            this.$message.error('Update failed');
                        });
                } else {
                    return false;
                }
            });
        },
        dateformatter(date) {
            if (!date) return '';

            try {
                // Format date using native JavaScript
                const d = new Date(date);

                // Check if date is valid
                if (isNaN(d.getTime())) {
                    return date; // If cannot parse, return original value
                }

                // Format as YYYY-MM-DD HH:mm:ss
                const year = d.getFullYear();
                const month = String(d.getMonth() + 1).padStart(2, '0');
                const day = String(d.getDate()).padStart(2, '0');
                const hours = String(d.getHours()).padStart(2, '0');
                const minutes = String(d.getMinutes()).padStart(2, '0');
                const seconds = String(d.getSeconds()).padStart(2, '0');

                return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
            } catch (error) {
                console.warn('Date formatting error:', error, 'Original value:', date);
                return date; // If formatting fails, return original value
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
            this.$confirm('This will permanently delete this color. Continue?', 'Notice', {
                confirmButtonText: 'OK',
                cancelButtonText: 'Cancel',
                type: 'warning'
            }).then(async () => {
                console.log('Sending delete request:', { id: row.id });

                await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=DeleteColor', {
                    id: row.id
                }, {
                    headers: {
                        'Content-Type': 'application/json'
                    }
                })
                    .then(res => {
                        console.log('Delete response:', res);
                        // Compatible with NCF framework nested response format
                        const responseData = res.data.data || res.data;
                        this.$message({
                            type: responseData.success ? 'success' : 'error',
                            message: responseData.message || 'Operation completed'
                        });
                        if (responseData.success) {
                            this.getDataList();
                        }
                    })
                    .catch(error => {
                        console.error('Delete failed:', error);
                        this.$message.error('Delete failed');
                    });
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: 'Deletion cancelled'
                });
            });
        },
        async randomizeColor(row) {
            console.log('Sending randomize request:', { id: row.id });

            await service.post('/Admin/Template_XncfName/DatabaseSampleIndex?handler=RandomizeColor', {
                id: row.id
            }, {
                headers: {
                    'Content-Type': 'application/json'
                }
            })
                .then(res => {
                    console.log('Randomize response:', res);
                    // Compatible with NCF framework nested response format
                    const responseData = res.data.data || res.data;
                    this.$message({
                        type: responseData.success ? 'success' : 'error',
                        message: responseData.message || 'Operation completed'
                    });
                    if (responseData.success) {
                        this.getDataList();
                    }
                })
                .catch(error => {
                    console.error('Randomize failed:', error);
                    this.$message.error('Randomize failed');
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

            // Test Vue reactivity
            if (this.tableData && this.tableData.length === 0) {
                console.log('Test: Adding fake data');
                this.tableData = [
                    {id: 999, red: 255, green: 0, blue: 0, addTime: new Date().toISOString(), lastUpdateTime: new Date().toISOString(), remark: 'test'}
                ];
                this.total = 1;
                setTimeout(() => {
                    console.log('Clearing fake data after 2 seconds');
                    this.tableData = [];
                    this.total = 0;
                }, 2000);
            }
        }
    }
});

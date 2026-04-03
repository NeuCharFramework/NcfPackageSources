var app = new Vue({
    el: "#app",
    data() {
        return {
            //Paging parameters
            paginationQuery: {
                total: 0
            },
            //Paging interface parameter passing
            listQuery: {
                pageIndex: 1,
                pageSize: 10,
                adminUserInfoName: ''
            },
            tableData: [],
            requestTenantInfo: {},
            tenantRule: '',
            enableMultiTenant: true,
            dialog: {
                title: '新增租户信息',
                visible: false,
                data: {
                    id: 0,
                    name: '',
                    tenantKey: '',
                    adminRemark: '',
                    enable: true
                },
                rules: {
                    name: [
                        { required: true, message: "租户名称为必填项", trigger: "blur" }
                    ],
                    tenantKey: [
                        { required: true, message: "租户匹配规则为必填项", trigger: "blur" }
                    ]
                },
                updateLoading: false,
                updateLoadingSet: false, // Confirm loading button
                nameError: '',
                tenantKeyError: ''
            },
            initializeDialog: {
                visible: false,
                loading: false,
                data: {
                    tenantId: 0,
                    systemName: '',
                    adminAccount: ''
                },
                rules: {
                    systemName: [
                        { required: true, message: "系统名称为必填项", trigger: "blur" }
                    ],
                    adminAccount: [
                        { required: true, message: "默认管理员账号为必填项", trigger: "blur" }
                    ]
                }
            },
            resultDialog: {
                visible: false,
                data: {
                    tenantInfo: {
                        id: 0,
                        name: '',
                        tenantKey: ''
                    },
                    adminAccount: {
                        username: '',
                        password: ''
                    }
                }
            }
        };
    },
    created: function () {
        this.getList();
        this.getRequestTenantInfo();
    },
    computed: {

    },
    watch: {
        'dialog.visible': function (val, old) {
            // Close the dialog and clear it
            if (!val) {
                this.dialog.data = {
                    id: 0,
                    name: '',
                    tenantKey: '',
                    adminRemark: '',
                    enable: true
                };
                this.dialog.nameError = '';
                this.dialog.tenantKeyError = '';
                this.dialog.updateLoading = false;
                this.$refs['dataForm'].resetFields();
            }
        }
    },
    methods: {
        // Get data
        getList() {
            let { pageIndex, pageSize } = this.listQuery;
            service.get(`/Admin/TenantInfo/index?handler=List&pageIndex=${pageIndex}&pageSize=${pageSize}`).then(res => {
                if (res.data.success) {
                    this.tableData = res.data.data.list;
                    this.paginationQuery.total = res.data.data.totalCount;
                }
            });
        },
        // New
        handleAdd() {
            this.dialog.title = '新增租户信息';
            this.dialog.visible = true;
            this.dialog.data = {
                id: 0,
                name: '',
                tenantKey: '',
                adminRemark: '',
                enable: true
            };
        },
        // edit
        handleEdit(index, row) {
            this.dialog.visible = true;
            if (row) {
                // edit
                let { id, name, tenantKey, adminRemark, enable } = row;
                this.dialog.data = {
                    id, name, tenantKey, adminRemark, enable
                };
                this.dialog.title = '编辑租户信息';
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // New
                this.dialog.title = '新增租户信息';
            }
        },
        // Update new editor
        updateData() {
            this.$refs['dataForm'].validate(valid => {
                // form validation
                if (valid) {
                    this.dialog.updateLoading = true;
                    let data = {
                        Id: this.dialog.data.id,
                        Name: this.dialog.data.name,
                        TenantKey: this.dialog.data.tenantKey,
                        AdminRemark: this.dialog.data.adminRemark,
                        Enable: this.dialog.data.enable
                    };
                    service.post("/Admin/TenantInfo/Index?handler=Save", data).then(res => {
                        if (res.data.success) {
                            this.getList();
                            this.$notify({
                                title: "Success",
                                message: res.data.msg,
                                type: "success",
                                duration: 2000
                            });
                            this.dialog.visible = false;
                            this.dialog.updateLoading = false;
                        }
                    }).catch(error => {
                        this.dialog.updateLoading = false;
                    });
                }
            });
        },
        // delete
        handleDelete(index, row) {
            let ids = [row.id];
            service.post("/Admin/TenantInfo/Index?handler=Delete", ids).then(res => {
                if (res.data.success) {
                    this.getList();
                    this.$notify({
                        title: "Success",
                        message: "删除成功",
                        type: "success",
                        duration: 2000
                    });
                }
            });
        },
        getRequestTenantInfo() {
            service.get("/Admin/TenantInfo/Index?handler=RequestTenantInfo").then(res => {
                if (res.data.success) {
                    this.requestTenantInfo = res.data.data.requestTenantInfo;
                    this.tenantRule = res.data.data.tenantRule;
                    this.enableMultiTenant = res.data.data.enableMultiTenant;
                }
            });
        },
        // initialization
        handleInitialize(row) {
            this.initializeDialog.visible = true;
            this.initializeDialog.data = {
                tenantId: row.id,
                systemName: '',
                adminAccount: ''
            };
        },

        // Submit initialization
        submitInitialize() {
            this.$refs['initializeForm'].validate(valid => {
                if (valid) {
                    this.initializeDialog.loading = true;
                    service.post("/Admin/TenantInfo/Index?handler=Initialize", this.initializeDialog.data).then(res => {
                        if (res.data.success) {
                            this.initializeDialog.visible = false;
                            // Show result popup
                            this.resultDialog.data = res.data.data;
                            this.resultDialog.visible = true;
                        } else {
                            this.$notify({
                                title: "Error",
                                message: res.data.msg || "初始化失败",
                                type: "error",
                                duration: 2000
                            });
                        }
                    }).finally(() => {
                        this.initializeDialog.loading = false;
                    });
                }
            });
        }
    }

});

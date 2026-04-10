var ncfI18n = window.ncfI18n || {};

var app = new Vue({
    el: "#app",
    data() {
        return {
            // Pagination parameters
            paginationQuery: {
                total: 0
            },
            // Pagination API parameters
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
                title: ncfI18n.addTenantInfo || 'Add Tenant Info',
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
                        { required: true, message: ncfI18n.tenantNameRequired || "Tenant name is required", trigger: "blur" }
                    ],
                    tenantKey: [
                        { required: true, message: ncfI18n.tenantKeyRequired || "Tenant matching rule is required", trigger: "blur" }
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
                        { required: true, message: ncfI18n.systemNameRequired || "System name is required", trigger: "blur" }
                    ],
                    adminAccount: [
                        { required: true, message: ncfI18n.defaultAdminRequired || "Default admin account is required", trigger: "blur" }
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
            // Clear on dialog close
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
        // Add
        handleAdd() {
            this.dialog.title = ncfI18n.addTenantInfo || 'Add Tenant Info';
            this.dialog.visible = true;
            this.dialog.data = {
                id: 0,
                name: '',
                tenantKey: '',
                adminRemark: '',
                enable: true
            };
        },
        // Edit
        handleEdit(index, row) {
            this.dialog.visible = true;
            if (row) {
                // Edit
                let { id, name, tenantKey, adminRemark, enable } = row;
                this.dialog.data = {
                    id, name, tenantKey, adminRemark, enable
                };
                this.dialog.title = ncfI18n.editTenantInfo || 'Edit Tenant Info';
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // Add
                this.dialog.title = ncfI18n.addTenantInfo || 'Add Tenant Info';
            }
        },
        // Update add/edit
        updateData() {
            this.$refs['dataForm'].validate(valid => {
                // Form validation
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
        // Delete
        handleDelete(index, row) {
            let ids = [row.id];
            service.post("/Admin/TenantInfo/Index?handler=Delete", ids).then(res => {
                if (res.data.success) {
                    this.getList();
                    this.$notify({
                        title: "Success",
                        message: ncfI18n.deleteSuccess || "Deleted successfully",
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
        // Initialize
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
                            // Show result dialog
                            this.resultDialog.data = res.data.data;
                            this.resultDialog.visible = true;
                        } else {
                            this.$notify({
                                title: "Error",
                                message: res.data.msg || (ncfI18n.initializationFailed || "Initialization failed"),
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

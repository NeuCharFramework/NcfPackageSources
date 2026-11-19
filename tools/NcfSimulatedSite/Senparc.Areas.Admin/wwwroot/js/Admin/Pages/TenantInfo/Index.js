var app = new Vue({
    el: "#app",
    data() {
        return {
            //pagination parameters
            paginationQuery: {
                total: 0
            },
            //pagination API parameters
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
                title: ncfT('Tenant.CreateTitle'),
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
                        { required: true, message: ncfT('Tenant.NameRequired'), trigger: "blur" }
                    ],
                    tenantKey: [
                        { required: true, message: ncfT('Tenant.KeyRequired'), trigger: "blur" }
                    ]
                },
                updateLoading: false,
                updateLoadingSet: false, //Legacy comment translated to English.
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
                        { required: true, message: ncfT('Tenant.SystemNameRequired'), trigger: "blur" }
                    ],
                    adminAccount: [
                        { required: true, message: ncfT('Tenant.AdminAccountRequired'), trigger: "blur" }
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
            // disabledialog，clear
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
        // getdata
        getList() {
            let { pageIndex, pageSize } = this.listQuery;
            service.get(`/Admin/TenantInfo/index?handler=List&pageIndex=${pageIndex}&pageSize=${pageSize}`).then(res => {
                if (res.data.success) {
                    this.tableData = res.data.data.list;
                    this.paginationQuery.total = res.data.data.totalCount;
                    if (typeof res.data.data.enableMultiTenant === "boolean") {
                        this.enableMultiTenant = res.data.data.enableMultiTenant;
                    }
                }
            });
        },
        // add
        handleAdd() {
            this.dialog.title = ncfT('Tenant.CreateTitle');
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
                this.dialog.title = ncfT('Tenant.EditTitle');
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // add
                this.dialog.title = ncfT('Tenant.CreateTitle');
            }
        },
        // updateaddedit
        updateData() {
            this.$refs['dataForm'].validate(valid => {
                // single-table validation
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
                                title: ncfT('Admin.Common.Success'),
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
                        message: ncfT('Tenant.DeleteSuccess'),
                        type: "success",
                        duration: 2000
                    });
                } else {
                    this.$notify({
                        title: ncfT('Admin.Common.Error'),
                        message: res.data.msg || ncfT('Tenant.DeleteNotFound'),
                        type: "error",
                        duration: 3000
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
        // initialize
        handleInitialize(row) {
            this.initializeDialog.visible = true;
            this.initializeDialog.data = {
                tenantId: row.id,
                systemName: '',
                adminAccount: ''
            };
        },

        //Legacy comment translated to English.
        submitInitialize() {
            this.$refs['initializeForm'].validate(valid => {
                if (valid) {
                    this.initializeDialog.loading = true;
                    service.post("/Admin/TenantInfo/Index?handler=Initialize", this.initializeDialog.data).then(res => {
                        if (res.data.success) {
                            this.initializeDialog.visible = false;
                            //Legacy comment translated to English.
                            this.resultDialog.data = res.data.data;
                            this.resultDialog.visible = true;
                        } else {
                            this.$notify({
                                title: ncfT('Admin.Common.Error'),
                                message: res.data.msg || ncfT('Tenant.InitializeFailed'),
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

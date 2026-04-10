var ncfI18n = window.ncfI18n || {};

var app = new Vue({
    el: "#app",
    data() {
        return {
            // Pagination parameters
            paginationQuery: {
                total: 5
            },
            // Pagination API parameters
            listQuery: {
                pageIndex: 1,
                pageSize: 20,
                adminUserInfoName: ''
            },
            tableData: [],
            dialog: {
                title: ncfI18n.addAdmin || 'Add Admin',
                visible: false,
                data: {
                    id: 0,
                    userName: '',
                    password: '',
                    password2: '',
                    realName: '',
                    phone: '',
                    note: ''
                },
                rules: {
                    userName: [
                        { required: true, message: ncfI18n.usernameRequired || "Username is required", trigger: "blur" }
                    ]
                },
                updateLoading: false,
                visibleSet: false, // Set role dialog
                updateLoadingSet: false, // Confirm loading button
                dialogSetData: [],
                dialogSetSelected: [], // Checkboxes
                setId: '',
                setTitle: '',
                passwordError:'',
                password2Error: '',
                isVerTrue:false // Whether password validation passed
            }
        };
    },
    created: function () {
        this.getList();
    },
    computed: {
        // Password needs validation
        isVerPass() {
            if (this.dialog.title === (ncfI18n.addAdmin || 'Add Admin')) {
                return true;
            } else if (this.dialog.title === (ncfI18n.editAdmin || 'Edit Admin') && this.dialog.data.password.length > 0 || this.dialog.title === (ncfI18n.editAdmin || 'Edit Admin') && this.dialog.data.password2.length > 0) {
                return true;
            } else {
                return false;
            }
        }
    },
    watch: {
        'dialog.data.password': function myfunction(val) {
            this.checkPass();
        },
        'dialog.data.password2': function myfunction(val) {
            this.checkPass();
        },
        'dialog.visible': function (val, old) {
            // Clear on dialog close
            if (!val) {
                this.dialog.data = {
                    id: 0,
                    userName: '',
                    password: '',
                    password2: '',
                    realName: '',
                    phone: '',
                    note: ''
                };
                this.dialog.password2Error = '';
                this.dialog.passwordError = '';
                this.dialog.updateLoading = false;
                this.$refs['dataForm'].resetFields();
            }
        }
    },
    methods: {
        // Validate password again
        checkPass() {
            if (this.isVerPass) {
                if (this.dialog.data.password === '') {
                    if (this.dialog.visible) {

                    this.dialog.passwordError = ncfI18n.pleaseEnterPassword || 'Please enter a password';
                    this.dialog.isVerTrue = false;
                    return false;
                    }
                } else {
                    this.dialog.passwordError = '';
                    if (this.dialog.data.password2 === '') {
                        this.dialog.password2Error = ncfI18n.pleaseReenterPassword || 'Please re-enter the password';
                        this.dialog.isVerTrue = false;
                        return false;
                    } else if (this.dialog.data.password !== this.dialog.data.password2) {
                        this.dialog.password2Error = ncfI18n.passwordMismatch || 'Passwords do not match';
                        this.dialog.isVerTrue = false;
                        return false;
                    } else {
                        this.dialog.password2Error = '';
                        this.dialog.passwordError = '';
                        this.dialog.isVerTrue = true;
                        return true;
                    }
                }
            }
        },
        // Get data
        getList() {
            let { adminUserInfoName, pageIndex, pageSize } = this.listQuery;
            service.get(`/Admin/AdminUserInfo/index?handler=List&adminUserInfoName=${adminUserInfoName}&pageIndex=${pageIndex}&pageSize=${pageSize}`).then(res => {
                this.tableData = res.data.data.list;
                this.paginationQuery.total = res.data.data.totalCount;
            });
        },
        // Edit
        handleEdit(index, row) {
            this.dialog.visible = true;
            if (row) {
                // Edit
                let { userName, password, realName, phone, note, id } = row;
                this.dialog.data = {
                    userName, realName, phone, note, id, password: '', password2: ''
                };
                this.dialog.title = ncfI18n.editAdmin || 'Edit Admin';
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // Add
                this.dialog.title = ncfI18n.addAdmin || 'Add Admin';
            }
        },
        // Update add/edit
        updateData() {
            this.$refs['dataForm'].validate(valid => {
                // Form validation
                if (valid) {
                    // Needs validation
                    if (this.isVerPass) {
                        // Validation failed
                        if (!this.dialog.isVerTrue) {
                            console.log('Validation failed');
                            return false;
                        }
                    }
                    this.dialog.updateLoading = true;
                    let data = {
                        Id: this.dialog.data.id,
                        UserName: this.dialog.data.userName,
                        Password: this.dialog.data.password,
                        Note: this.dialog.data.note,
                        RealName: this.dialog.data.realName,
                        Phone: this.dialog.data.phone
                    };
                    service.post("/Admin/AdminUserInfo/Edit?handler=Save", data).then(res => {
                        if (res.data.success) {
                            this.getList();
                            this.$notify({
                                title: "Success",
                                message: ncfI18n.operationSuccess || "Operation successful",
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
        // Update set role
        updateDataSet() {
            this.dialog.updateLoadingSet = true;
            const data = { RoleIds: this.dialog.dialogSetSelected, AccountId: this.setId };
            service.post("/Admin/AdminUserInfo/AuthorizationPage", data).then(res => {
                if (res.data.success) {
                    this.getList();
                    this.$notify({
                        title: "Success",
                        message: ncfI18n.operationSuccess || "Operation successful",
                        type: "success",
                        duration: 2000
                    });
                    this.dialog.visibleSet = false;
                    this.dialog.updateLoadingSet = false;
                }
            });
        },
        // Set role
        async    handleSet(index, row) {
            this.dialog.dialogSetSelected = [];
            this.dialog.visibleSet = true;
            this.setId = row.id;
            this.dialog.setTitle = row.userName;
            // All roles
            const a = await service.get("/Admin/Role/edit?Handler=SelectItems");
            if (a.data.success) {
                this.dialog.dialogSetData = a.data.data;
            }
            // Existing roles
            const b = await service.get(`/Admin/AdminUserInfo/AuthorizationPage?Handler=Detail&accountId=${this.setId}`);
            if (b.data.success) {
                b.data.data.map(res => {
                    this.dialog.dialogSetSelected.push(res.roleId);
                });
            }
        },
        // Delete
        handleDelete(index, row) {
            let ids = [row.id];
            service.post("/Admin/AdminUserInfo/Index?handler=Delete", ids).then(res => {
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
        }
    }

});

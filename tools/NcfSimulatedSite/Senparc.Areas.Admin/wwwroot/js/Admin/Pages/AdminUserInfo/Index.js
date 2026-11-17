var app = new Vue({
    el: "#app",
    data() {
        //const validatePass = (rule, value, callback) => {
        //    if (value === '') {
        //        callback();
        //    } else {
        //            this.$refs.dataForm.validateField('password2');
        //        callback();
        //    }
        //};
        //const validatePass2 = (rule, value, callback) => {
        //    if (value === '') {
        //Legacy comment translated to English.
        //    } else if (value !== this.dialog.data.password) {
        //Legacy comment translated to English.
        //    } else {
        //        callback();
        //    }
        //};
        return {
            //pagination parameters
            paginationQuery: {
                total: 5
            },
            //pagination API parameters
            listQuery: {
                pageIndex: 1,
                pageSize: 20,
                adminUserInfoName: ''
            },
            tableData: [],
            dialog: {
                title: ncfT('AdminUserInfo.AddTitle'),
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
                        { required: true, message: ncfT('Admin.Login.Error.UsernameRequired'), trigger: "blur" }
                    ]
                    //password: [{ required: true, validator: validatePass, trigger: "blur" }],
                    //password2: [{ required: true, validator: validatePass2, trigger: "blur" }]
                },
                updateLoading: false,
                visibleSet: false, // settingsroledialog
                updateLoadingSet: false, //Legacy comment translated to English.
                dialogSetData: [],
                dialogSetSelected: [], //Legacy comment translated to English.
                setId: '',
                setTitle: '',
                passwordError:'',
                password2Error: '',
                isVerTrue:false //Legacy comment translated to English.
            }
        };
    },
    created: function () {
        this.getList();
    },
    computed: {
        //Legacy comment translated to English.
        isVerPass() {
            if (this.dialog.title === ncfT('AdminUserInfo.AddTitle')) {
                return true;
            } else if (this.dialog.title === ncfT('AdminUserInfo.EditTitle') && this.dialog.data.password.length > 0 || this.dialog.title === ncfT('AdminUserInfo.EditTitle') && this.dialog.data.password2.length > 0) {
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
            // disabledialog，clear
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
        //Legacy comment translated to English.
        checkPass() {
            if (this.isVerPass) {
                if (this.dialog.data.password === '') {
                    if (this.dialog.visible) {

                    this.dialog.passwordError = ncfT('AdminUserInfo.PasswordRequired');
                    this.dialog.isVerTrue = false;
                    return false;
                    }
                } else {
                    this.dialog.passwordError = '';
                    if (this.dialog.data.password2 === '') {
                        this.dialog.password2Error = ncfT('AdminUserInfo.PasswordConfirmRequired');
                        this.dialog.isVerTrue = false;
                        return false;
                    } else if (this.dialog.data.password !== this.dialog.data.password2) {
                        this.dialog.password2Error = ncfT('AdminUserInfo.PasswordMismatch');
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
        // getdata
        getList() {
            let { adminUserInfoName, pageIndex, pageSize } = this.listQuery;
            service.get(`/Admin/AdminUserInfo/index?handler=List&adminUserInfoName=${adminUserInfoName}&pageIndex=${pageIndex}&pageSize=${pageSize}`).then(res => {
                this.tableData = res.data.data.list;
                this.paginationQuery.total = res.data.data.totalCount;
            });
        },
        // edit
        handleEdit(index, row) {
            this.dialog.visible = true;
            if (row) {
                // edit
                let { userName, password, realName, phone, note, id } = row;
                this.dialog.data = {
                    userName, realName, phone, note, id, password: '', password2: ''
                };
                this.dialog.title = ncfT('AdminUserInfo.EditTitle');
                this.dialog = Object.assign({}, this.dialog);
            } else {
                // add
                this.dialog.title = ncfT('AdminUserInfo.AddTitle');
            }
        },
        // updateaddedit
        updateData() {
            this.$refs['dataForm'].validate(valid => {
                // single-table validation
                if (valid) {
                    //Legacy comment translated to English.
                    if (this.isVerPass) {
                        //Legacy comment translated to English.
                        if (!this.dialog.isVerTrue) {
                            console.log('不通过');
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
                                title: ncfT('AdminUserInfo.Success'),
                                message: ncfT('AdminUserInfo.Success'),
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
        // updatesettingsrole
        updateDataSet() {
            this.dialog.updateLoadingSet = true;
            const data = { RoleIds: this.dialog.dialogSetSelected, AccountId: this.setId };
            service.post("/Admin/AdminUserInfo/AuthorizationPage", data).then(res => {
                if (res.data.success) {
                    this.getList();
                    this.$notify({
                        title: ncfT('AdminUserInfo.Success'),
                        message: ncfT('AdminUserInfo.Success'),
                        type: "success",
                        duration: 2000
                    });
                    this.dialog.visibleSet = false;
                    this.dialog.updateLoadingSet = false;
                }
            });
        },
        // settingsrole
        async    handleSet(index, row) {
            this.dialog.dialogSetSelected = [];
            this.dialog.visibleSet = true;
            this.setId = row.id;
            this.dialog.setTitle = row.userName;
            // allrole
            const a = await service.get("/Admin/Role/edit?Handler=SelectItems");
            if (a.data.success) {
                this.dialog.dialogSetData = a.data.data;
            }
            //Legacy comment translated to English.
            const b = await service.get(`/Admin/AdminUserInfo/AuthorizationPage?Handler=Detail&accountId=${this.setId}`);
            if (b.data.success) {
                b.data.data.map(res => {
                    this.dialog.dialogSetSelected.push(res.roleId);
                });
            }
        },
        // delete
        handleDelete(index, row) {
            let ids = [row.id];
            service.post("/Admin/AdminUserInfo/Index?handler=Delete", ids).then(res => {
                if (res.data.success) {
                    this.getList();
                    this.$notify({
                        title: ncfT('AdminUserInfo.Success'),
                        message: ncfT('AdminChat.DeleteSessionSuccess'),
                        type: "success",
                        duration: 2000
                    });
                }
            });
        }
    }

});

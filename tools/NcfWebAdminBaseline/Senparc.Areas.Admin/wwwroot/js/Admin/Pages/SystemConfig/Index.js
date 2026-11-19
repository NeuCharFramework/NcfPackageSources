var app = new Vue({
    el: "#app",
    data() {
        return {
            //pagination parameters
            paginationQuery: {
                total: 5
            },
            //Legacy comment translated to English.
            listQuery: {
                pageIndex: 1,
                pageSize: 20,
            },
            tableData: [],
            tenantData: {},
            dialog: {
                title: ncfT('SystemConfig.Title'),
                visible: false,
                data: {
                    id: 0,
                    systemName: '',
                    footerContent: '',
                },
                rules: {
                    systemName: [
                        { required: true, message: ncfT('SystemConfig.NameRequired'), trigger: "blur" }
                    ],
                    footerContent: [
                        { required: true, message: 'Footer 内容不能为空', trigger: "blur" },
                        { max: 2000, message: 'Footer 内容不能超过 2000 个字符', trigger: "blur" }
                    ]
                },
                updateLoading: false
            }
        };
    },
    created: function () {
        this.getList();
    },
    computed: {
    },
    watch: {
        'dialog.visible': function (val, old) {
            // disabledialog，clear
            if (!val) {
                this.dialog.data = {
                    id: 0,
                    systemName: '',
                    footerContent: ''
                };
                this.dialog.updateLoading = false;
                if (this.$refs['dataForm']) {
                    this.$refs['dataForm'].resetFields();
                }
            }
        }
    },
    methods: {
        // getdata
        getList() {
            let { pageIndex, pageSize } = this.listQuery;
            service.get(`/Admin/SystemConfig?handler=List&pageIndex=${pageIndex}&pageSize=${pageSize}`).then(res => {
                this.tableData = res.data.data.list;
                this.paginationQuery.total = res.data.data.totalCount;
            });
        },
        // edit
        handleEdit(index, row) {
            this.dialog.visible = true;
            if (row) {
                // edit
                let { systemName, footerContent, id } = row;
                this.dialog.data = {
                    systemName, footerContent, id
                };
                this.dialog = Object.assign({}, this.dialog);
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
                        SystemName: this.dialog.data.systemName,
                        FooterContent: this.dialog.data.footerContent,
                    };
                    service.post("/Admin/SystemConfig?handler=Edit", data).then(res => {
                        if (res.data.success) {
                            this.getList();
                            this.$notify({
                                title: ncfT('AdminUserInfo.Success'),
                                message: ncfT('SystemConfig.Updated'),
                                type: "success",
                                duration: 2000
                            });
                            this.dialog.visible = false;
                            this.dialog.updateLoading = false;
                        } else {
                            this.$notify({
                                title: ncfT('Admin.Common.Error'),
                                message: ncfT('SystemConfig.UpdateFailed') + res.data.msg,
                                type: "error",
                                duration: 2000
                            });
                        }
                    }).catch(error => {
                        this.dialog.updateLoading = false;
                    });
                }
            });
        },
    }
});

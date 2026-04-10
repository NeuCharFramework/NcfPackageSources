var ncfI18n = window.ncfI18n || {};

var app = new Vue({
    el: "#app",
    data() {
        return {
            // Pagination parameters
            paginationQuery: {
                total: 5
            },
            // Pagination API parameters (only one record)
            listQuery: {
                pageIndex: 1,
                pageSize: 20,
            },
            tableData: [],
            tenantData: {},
            dialog: {
                title: ncfI18n.editSystemInfo || 'Edit System Info',
                visible: false,
                data: {
                    id: 0,
                    systemName: '',
                },
                rules: {
                    systemName: [
                        { required: true, message: ncfI18n.systemNameRequired || "System name is required", trigger: "blur" }
                    ]
                }
            },
            updateLoading: false,
            updateLoadingSet: false, // Confirm loading button
        };
    },
    created: function () {
        this.getList();
    },
    computed: {
    },
    watch: {
        'dialog.visible': function (val, old) {
            // Clear on dialog close
            if (!val) {
                this.dialog.data = {
                    id: 0,
                    systemName: ''
                };
                this.dialog.updateLoading = false;
                this.$refs['dataForm'].resetFields();
            }
        }
    },
    methods: {
        // Get data
        getList() {
            let { pageIndex, pageSize } = this.listQuery;
            service.get(`/Admin/SystemConfig/index?handler=List&&pageIndex=${pageIndex}&pageSize=${pageSize}`).then(res => {
                this.tableData = res.data.data.list;
                this.paginationQuery.total = res.data.data.totalCount;
            });
        },
        // Edit
        handleEdit(index, row) {
            this.dialog.visible = true;
            if (row) {
                // Edit
                let { systemName, id } = row;
                this.dialog.data = {
                    systemName, id
                };
                this.dialog = Object.assign({}, this.dialog);
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
                        SystemName: this.dialog.data.systemName,
                    };
                    service.post("/Admin/SystemConfig/Edit?handler=Save", data).then(res => {
                        if (res.data.success) {
                            this.getList();
                            this.$notify({
                                title: "Success",
                                message: ncfI18n.updateSuccess || "Updated successfully!",
                                type: "success",
                                duration: 2000
                            });
                            this.dialog.visible = false;
                            this.dialog.updateLoading = false;
                        } else {
                            this.$notify({
                                title: "Failed",
                                message: (ncfI18n.updateFailed || "Update failed: ") + res.data.msg,
                                type: "success",
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

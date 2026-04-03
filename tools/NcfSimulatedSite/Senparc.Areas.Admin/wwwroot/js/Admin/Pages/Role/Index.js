var app = new Vue({
    el: "#app",
    data() {
        return {
            //Paging parameters
            paginationQuery: {
                total: 5
            },
            //Paging interface parameter passing
            listQuery: {
                pageIndex: 1,
                pageSize: 20,
                roleName: '',
                orderField: ''
            },
            tableData: [],
            dialog: {
                title: '新增角色',
                visible: false,
                data: {
                    roleName: '', roleCode: '', adminRemark: '', remark: '', addTime: '', id: '', enabled: false
                },
                rules: {
                    roleName: [
                        { required: true, message: "角色名称为必填项", trigger: "blur" }
                    ],
                    roleCode: [{ required: true, message: "角色代码为必填项", trigger: "blur" }]
                },
                updateLoading: false
            },
            // tree structure fields
            defaultProps: {
                children: 'children',
                label: 'menuName'
            },
            // Authorize
            au: {
                title: '',
                visible: false,
                updateLoading: false,
                temp: {}
            },
            allMenu: [],// All permissions
            currMenu: [],// Current permissions
            defaultExpandedKeys: [], // Expand by default
            defaultCheckedKeys: [], // Selected by default
            parentArr: [] // set of parent nodes
        };
    },
    created: function () {
        this.getList();
    },
    watch: {
        'dialog.visible': function (val, old) {
            // Close the dialog and clear it
            if (!val) {
                this.dialog.data = {
                    roleName: '', roleCode: '', adminRemark: '', remark: '', addTime: '', id: ''
                };
                this.dialog.updateLoading = false;
            }
        },
        'au.visible': function (val, old) {
            // Close the dialog and clear it
            if (!val) {
                this.currMenu = [];
                this.defaultCheckedKeys = [];
                this.defaultExpandedKeys = [];
                this.au.updateLoading = false;
            }
        }
    },
    methods: {
        // Permissions
        async handleRole(index, row) {
            // open dialog
            this.au = {
                title: row.roleName,
                visible: true,
                temp: row
            };
            // Currently has permission
            const c = await service.get(`/Admin/Role/Permission?handler=RolePermission&roleId=${row.id}`);
            this.currMenu = c.data.data;
            let defaultCheckedKeys = [];
            this.currMenu.map(res => {
                defaultCheckedKeys.push(res.permissionId);
            });

            const a = await service.get('/Admin/Menu/Edit?handler=menu');
            const b = a.data.data;
            let allMenu = [];
            // The set of parent nodes is used to find the difference set when the default and condition are parent nodes, and solve the problem of no half-selection in element tree.
            let parentMenuNodes = [];
            this.ddd(b, null, allMenu, parentMenuNodes);
            this.allMenu = allMenu
            // All permissions formatted data (used for rendering tree)
            const e = [];
            parentMenuNodes.map((res) => {
                defaultCheckedKeys.map((ele) => {
                    if (res !== ele && parentMenuNodes.indexOf(ele) < 0 && e.indexOf(ele) < 0) {
                        e.push(ele);
                    }
                });
            });
            this.defaultCheckedKeys = e;
        },
        ddd(source, parentId, dest, nodes) {
            var array = source.filter(_ => _.parentId === parentId);
            for (let i in array) {
                let ele = array[i];
                ele.children = [];
                dest.push(ele);
                this.ddd(source, ele.id, ele.children, nodes);
                if (ele.children.length > 0) {
                    nodes.push(ele.id);
                }
            }
        },
        // Update authorization
        async  auUpdateData() {
            this.au.updateLoading = true;
            const checkNodes = this.$refs.tree.getCheckedNodes(false, true);
            let array = [];
            checkNodes.map((ele) => {
                array.push({
                    PermissionId: ele.id,
                    roleId: this.au.temp.id,
                    isMenu: ele.isMenu,
                    roleCode: ele.resourceCode
                });
            });
            const respnseData = await service.post('/Admin/Role/Permission', array);
            if (respnseData.data.success) {
                this.getList();
                this.$notify({
                    title: "Success",
                    message: "授权成功",
                    type: "success",
                    duration: 800,
                    onClose: function () {
                        app.au.visible = false;
                        app.au.updateLoading = false;
                        window.location.reload();
                    }
                });
            }
        },
        // Initialize data acquisition
        getList() {
            let { pageIndex, pageSize, roleName, orderField } = this.listQuery;
            service.get(`/Admin/Role/index?handler=List&pageIndex=${pageIndex}&pageSize=${pageSize}&roleName=${roleName}&orderField=${orderField}`).then(res => {
                this.tableData = res.data.data.list;
                this.paginationQuery.total = res.data.data.totalCount;
            });
        },
        // edit
        handleEdit(index, row) {
            this.dialog.visible = true;
            if (row) {
                // edit
                let { roleName, roleCode, adminRemark, remark, addTime, id, enabled } = row;
                this.dialog.data = {
                    roleName, roleCode, adminRemark, remark, addTime, id, enabled
                };
                this.dialog.title = '编辑角色';
            } else {
                // New
                this.dialog.title = '新增角色';
            }
        },
        // Update, add, edit
        updateData() {
            this.dialog.updateLoading = true;
            this.$refs['dataForm'].validate(valid => {
                // form validation
                if (valid) {
                    let data = {
                        Id: this.dialog.data.id,
                        RoleName: this.dialog.data.roleName,
                        RoleCode: this.dialog.data.roleCode,
                        AdminRemark: this.dialog.data.adminRemark,
                        Remark: this.dialog.data.remark,
                        Enabled: this.dialog.data.enabled
                    };
                    service.post("/Admin/Role/Edit?handler=Save", data).then(res => {
                        if (res.data.success) {
                            this.getList();
                            this.$notify({
                                title: "Success",
                                message: "成功",
                                type: "success",
                                duration: 2000
                            });
                            this.dialog.visible = false;
                            this.dialog.updateLoading = false;
                        }
                    });
                }
            });


        },
        // delete
        handleDelete(index, row) {
            let ids = [row.id];
            service.post("/Admin/Role/Index?handler=Delete", ids).then(res => {
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
        }
    }

});
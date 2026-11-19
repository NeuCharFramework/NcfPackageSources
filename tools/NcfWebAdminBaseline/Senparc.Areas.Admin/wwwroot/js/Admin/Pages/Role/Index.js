var app = new Vue({
    el: "#app",
    data() {
        return {
            //pagination parameters
            paginationQuery: {
                total: 5
            },
            //pagination API parameters
            listQuery: {
                pageIndex: 1,
                pageSize: 20,
                roleName: '',
                orderField: ''
            },
            tableData: [],
            dialog: {
                title: ncfT('Role.AddTitle'),
                visible: false,
                data: {
                    roleName: '', roleCode: '', adminRemark: '', remark: '', addTime: '', id: '', enabled: false
                },
                rules: {
                    roleName: [
                        { required: true, message: ncfT('Role.NameRequired'), trigger: "blur" }
                    ],
                    roleCode: [{ required: true, message: ncfT('Role.CodeRequired'), trigger: "blur" }]
                },
                updateLoading: false
            },
            //Legacy comment translated to English.
            defaultProps: {
                children: 'children',
                label: 'menuName'
            },
            //Legacy comment translated to English.
            au: {
                title: '',
                visible: false,
                updateLoading: false,
                temp: {}
            },
            allMenu: [],//Legacy comment translated to English.
            currMenu: [],//Legacy comment translated to English.
            defaultExpandedKeys: [], //Legacy comment translated to English.
            defaultCheckedKeys: [], //Legacy comment translated to English.
            parentArr: [] //Legacy comment translated to English.
        };
    },
    created: function () {
        this.getList();
    },
    watch: {
        'dialog.visible': function (val, old) {
            // disabledialog，clear
            if (!val) {
                this.dialog.data = {
                    roleName: '', roleCode: '', adminRemark: '', remark: '', addTime: '', id: ''
                };
                this.dialog.updateLoading = false;
            }
        },
        'au.visible': function (val, old) {
            // disabledialog，clear
            if (!val) {
                this.currMenu = [];
                this.defaultCheckedKeys = [];
                this.defaultExpandedKeys = [];
                this.au.updateLoading = false;
            }
        }
    },
    methods: {
        //Legacy comment translated to English.
        async handleRole(index, row) {
            //Legacy comment translated to English.
            this.au = {
                title: row.roleName,
                visible: true,
                temp: row
            };
            //Legacy comment translated to English.
            const c = await service.get(`/Admin/Role/Permission?handler=RolePermission&roleId=${row.id}`);
            this.currMenu = c.data.data;
            let defaultCheckedKeys = [];
            this.currMenu.map(res => {
                defaultCheckedKeys.push(res.permissionId);
            });

            const a = await service.get('/Admin/Menu/Edit?handler=menu');
            const b = a.data.data;
            let allMenu = [];
            //Legacy comment translated to English.
            let parentMenuNodes = [];
            this.ddd(b, null, allMenu, parentMenuNodes);
            this.allMenu = allMenu
            //Legacy comment translated to English.
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
        //Legacy comment translated to English.
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
                    title: ncfT('AdminUserInfo.Success'),
                    message: ncfT('Role.AuthorizationSuccess'),
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
        // initializegetdata
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
                this.dialog.title = ncfT('Role.EditTitle');
            } else {
                // add
                this.dialog.title = ncfT('Role.AddTitle');
            }
        },
        // updateadd、edit
        updateData() {
            this.dialog.updateLoading = true;
            this.$refs['dataForm'].validate(valid => {
                // single-table validation
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
                                title: ncfT('AdminUserInfo.Success'),
                                message: ncfT('AdminUserInfo.Success'),
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

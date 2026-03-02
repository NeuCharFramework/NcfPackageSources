var app = new Vue({
    el: "#app",
    data() {
        return {
            loading: false,
            saving: false,
            isEditing: false,
            form: {
                id: 0,
                systemName: '',
                hideModuleManager: false
            },
            rules: {
                systemName: [
                    { required: true, message: "系统名称为必填项", trigger: "blur" }
                ]
            }
        };
    },
    created: function () {
        this.getConfig();
    },
    methods: {
        // 获取系统配置（仅一条）
        getConfig() {
            this.loading = true;
            service.get(`/Admin/SystemConfig/index?handler=List&pageIndex=1&pageSize=1`).then(res => {
                const list = res.data.data.list || [];
                if (list.length > 0) {
                    const row = list[0];
                    this.form = {
                        id: row.id,
                        systemName: row.systemName,
                        hideModuleManager: row.hideModuleManager
                    };
                }
            }).finally(() => {
                this.loading = false;
            });
        },
        // 开始编辑
        startEdit() {
            this.isEditing = true;
        },
        // 取消编辑，恢复数据
        cancelEdit() {
            this.isEditing = false;
            if (this.$refs.configForm) {
                this.$refs.configForm.clearValidate();
            }
            this.getConfig();
        },
        // 保存配置
        saveConfig() {
            this.$refs.configForm.validate(valid => {
                if (!valid) {
                    return;
                }
                this.saving = true;
                const data = {
                    Id: this.form.id,
                    SystemName: this.form.systemName
                };
                service.post("/Admin/SystemConfig/Edit?handler=Save", data).then(res => {
                    if (res.data.success) {
                        this.$notify({
                            title: "Success",
                            message: "更新成功！",
                            type: "success",
                            duration: 2000
                        });
                        this.isEditing = false;
                        this.getConfig();
                    } else {
                        this.$notify({
                            title: "Failed",
                            message: "更新失败：" + (res.data.msg || ''),
                            type: "error",
                            duration: 2000
                        });
                    }
                }).finally(() => {
                    this.saving = false;
                });
            });
        }
    }
});

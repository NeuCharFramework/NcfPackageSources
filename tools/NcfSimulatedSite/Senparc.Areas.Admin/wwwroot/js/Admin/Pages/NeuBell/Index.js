/*----------------------------------------------------------------
    文件名：Index.js
    文件功能描述：NeuBell WebHook（WebAPI）设置页：
    通知端点 CRUD、启用开关、测试发送
----------------------------------------------------------------*/
// 统一响应解包
window.NeuBellWebHookUi = {
    unwrap(response) {
        const body = response && response.data;
        if (body && body.success && 'data' in body) {
            return body.data;
        }
        return null;
    }
};

new Vue({
    el: '#app',
    data() {
        return {
            loading: false,
            items: [],
            providers: [],
            keyword: '',
            formDialogVisible: false,
            formSaving: false,
            form: {
                id: 0, name: '', webHookUrl: '', providerFilter: '', secret: '',
                notifyOnAdd: true, notifyOnRemove: true, isEnabled: true, showSecret: false
            }
        };
    },
    computed: {
        filteredItems() {
            const keyword = (this.keyword || '').trim().toLowerCase();
            return this.items.filter(item => !keyword
                || [item.name, item.webHookUrl, item.providerFilter]
                    .some(value => String(value || '').toLowerCase().includes(keyword)));
        }
    },
    created() {
        this.load();
    },
    methods: {
        async load() {
            this.loading = true;
            try {
                const itemsResponse = await service.get('/Admin/NeuBell/Index?handler=List');
                const providersResponse = await service.get('/Admin/NeuBell/Index?handler=Providers');
                const items = NeuBellWebHookUi.unwrap(itemsResponse);
                this.items = (Array.isArray(items) ? items : []).map(item => ({
                    ...item,
                    testing: false
                }));
                const providers = NeuBellWebHookUi.unwrap(providersResponse);
                this.providers = Array.isArray(providers) ? providers : [];
            } finally {
                this.loading = false;
            }
        },
        openCreate() {
            this.form = {
                id: 0, name: '', webHookUrl: '', providerFilter: '', secret: '',
                notifyOnAdd: true, notifyOnRemove: true, isEnabled: true, showSecret: false
            };
            this.formDialogVisible = true;
        },
        openEdit(row) {
            this.form = {
                id: row.id,
                name: row.name || '',
                webHookUrl: row.webHookUrl || '',
                providerFilter: row.providerFilter || '',
                secret: '',
                notifyOnAdd: !!row.notifyOnAdd,
                notifyOnRemove: !!row.notifyOnRemove,
                isEnabled: !!row.isEnabled,
                showSecret: false
            };
            this.formDialogVisible = true;
        },
        async saveRow() {
            if (!this.form.name || !String(this.form.name).trim()) {
                this.$message.warning('名称不能为空');
                return;
            }
            if (!this.form.webHookUrl || !String(this.form.webHookUrl).trim()) {
                this.$message.warning('WebHook 地址不能为空');
                return;
            }
            const url = String(this.form.webHookUrl).trim();
            if (!/^https?:\/\//i.test(url)) {
                this.$message.warning('WebHook 地址必须是合法的 http/https 绝对地址');
                return;
            }
            if (!this.form.notifyOnAdd && !this.form.notifyOnRemove) {
                this.$message.warning('请至少开启“新增时通知”或“移除时通知”');
                return;
            }
            this.formSaving = true;
            try {
                const response = await service.post('/Admin/NeuBell/Index?handler=Save', this.form, { customAlert: true });
                const body = response && response.data;
                if (body && body.success) {
                    this.$message.success('已保存');
                    this.formDialogVisible = false;
                    await this.load();
                } else {
                    this.$message.error((body && body.msg) || '保存失败');
                }
            } finally {
                this.formSaving = false;
            }
        },
        async toggleEnabled(row) {
            const response = await service.post('/Admin/NeuBell/Index?handler=Save', {
                id: row.id,
                name: row.name,
                webHookUrl: row.webHookUrl,
                providerFilter: row.providerFilter,
                secret: '',
                notifyOnAdd: row.notifyOnAdd,
                notifyOnRemove: row.notifyOnRemove,
                isEnabled: row.isEnabled
            }, { customAlert: true });
            const body = response && response.data;
            if (!body || !body.success) {
                row.isEnabled = !row.isEnabled;
            }
        },
        async sendTest(row) {
            row.testing = true;
            try {
                const response = await service.post('/Admin/NeuBell/Index?handler=Test', { id: row.id }, { customAlert: true });
                const body = response && response.data;
                const data = body && body.data;
                if (body && body.success && data && data.success) {
                    this.$message.success('测试成功：' + (data.message || ''));
                } else {
                    this.$message.error('测试失败：' + ((data && data.message) || (body && body.msg) || '未知错误'));
                }
            } finally {
                row.testing = false;
            }
        },
        async removeRow(row) {
            try {
                await this.$confirm('确定删除 WebHook“' + (row.name || row.id) + '”？删除后将不再向该地址发送通知。', '删除 WebHook', { type: 'warning' });
            } catch (error) {
                return;
            }
            const response = await service.post('/Admin/NeuBell/Index?handler=Delete', { id: row.id });
            if (response && response.data && response.data.success) {
                this.$message.success('已删除');
                await this.load();
            }
        }
    }
});


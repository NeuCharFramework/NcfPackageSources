/* MCP Endpoint 可视化管理：列表 / 新增 / 编辑 / 启用切换 / 连接测试（含可展开的 Function 信息） */
(function () {
    var apiBase = '/api/Senparc.Xncf.MCP/MCPEndpointAppService/Xncf.MCP_MCPEndpointAppService';

    function request(url, data, method) {
        return axios({
            method: method || 'post',
            url: url,
            data: data || {},
            headers: { 'x-requested-with': 'XMLHttpRequest' }
        }).then(function (response) {
            var body = response.data;
            if (!body || typeof body !== 'object' || typeof body.success !== 'boolean') {
                throw new Error('服务器响应格式不正确，请确认登录状态并刷新页面');
            }
            if (!body.success) {
                throw new Error(body.errorMessage || (typeof body.data === 'string' && body.data) || '请求失败');
            }
            return response;
        });
    }

    function errorMessage(error, fallback) {
        var response = error && error.response;
        var body = response && response.data;
        var detail = body && (body.errorMessage || body.message || body.title);
        if (detail) return fallback + '：' + detail;
        if (response && response.status === 404) {
            return fallback + '：接口不存在（HTTP 404），请确认站点已加载新版 MCP 模块';
        }
        if (response && (response.status === 401 || response.status === 403)) {
            return fallback + '：登录已失效或无访问权限，请重新登录';
        }
        if (response) return fallback + '（HTTP ' + response.status + '）';
        return fallback + (error && error.message ? '：' + error.message : '');
    }

    function endpointPayload(form, authConfig) {
        return {
            id: form.id,
            name: (form.name || '').trim(),
            endpoint: (form.endpoint || '').trim(),
            endpointType: form.endpointType,
            protocolVersion: form.protocolVersion,
            description: form.description,
            enabled: form.enabled,
            authConfig: authConfig,
            extraConfig: form.extraConfig
        };
    }

    function connectionResult(body) {
        var result = body.data;
        if (!result || typeof result.success !== 'boolean' || !Array.isArray(result.tools)) {
            throw new Error('服务器返回的连接测试结果格式不正确');
        }
        return result;
    }

    function formatDateTime(value) {
        if (!value) return '-';
        var d = new Date(value);
        if (isNaN(d.getTime())) return value;
        function pad(n) { return n < 10 ? '0' + n : '' + n; }
        return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate())
            + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
    }

    function formatSchemaJson(json) {
        if (!json) return '';
        try {
            return JSON.stringify(JSON.parse(json), null, 2);
        } catch (e) {
            return json;
        }
    }

    new Vue({
        el: '#app',
        data: function () {
            return {
                loading: false,
                tableData: [],
                testLoadingId: 0,
                filterText: '',
                onlyEnabled: false,

                // 新增/编辑对话框
                dialogVisible: false,
                dialogTitle: '新增 MCP Endpoint',
                saving: false,
                form: {
                    id: 0,
                    name: '',
                    endpoint: '',
                    endpointType: 'sse',
                    protocolVersion: '',
                    description: '',
                    enabled: true,
                    bearerToken: ''
                },
                rules: {
                    name: [{ required: true, message: '请填写端点名称', trigger: 'blur' }],
                    endpoint: [{ required: true, message: '请填写端点地址', trigger: 'blur' }]
                },
                endpointTypeOptions: ['sse', 'http', 'stdio', 'websocket'],

                // 保存前连接测试
                preTestLoading: false,
                preTestResult: null,

                // 测试结果对话框
                testDialogVisible: false,
                testResult: null,
                testResultEndpointName: '',
                expandedTools: [],

                // 工具 schema 原始 JSON 查看
                schemaDialogVisible: false,
                schemaTitle: '',
                schemaText: ''
            };
        },
        computed: {
            filteredTableData: function () {
                var list = this.tableData;
                if (this.onlyEnabled) {
                    list = list.filter(z => z.enabled);
                }
                if (this.filterText) {
                    var key = this.filterText.toLowerCase();
                    list = list.filter(z =>
                        (z.name && z.name.toLowerCase().indexOf(key) >= 0)
                        || (z.endpoint && z.endpoint.toLowerCase().indexOf(key) >= 0)
                        || (z.description && z.description.toLowerCase().indexOf(key) >= 0));
                }
                return list;
            },
            testResultTools: function () {
                if (!this.testResult || !this.testResult.tools) return [];
                return this.testResult.tools;
            }
        },
        created: function () {
            this.getList();
        },
        methods: {
            formatDateTime: formatDateTime,
            formatSchemaJson: formatSchemaJson,
            async getList() {
                this.loading = true;
                try {
                    var res = await request(apiBase + '.GetAllEndpoints');
                    var body = res.data || {};
                    if (!Array.isArray(body.data)) {
                        throw new Error('服务器返回的端点列表格式不正确');
                    }
                    this.tableData = body.data.map(z => Object.assign({}, z, {
                        bearerToken: this.extractBearerToken(z.authConfig)
                    }));
                } catch (error) {
                    console.error(error);
                    this.$message.error(errorMessage(error, '读取 MCP Endpoint 列表失败'));
                } finally {
                    this.loading = false;
                }
            },
            extractBearerToken(authConfigJson) {
                if (!authConfigJson) return '';
                try {
                    var obj = JSON.parse(authConfigJson);
                    return obj.token || obj.accessToken || obj.bearerToken || obj.apiKey || '';
                } catch (e) {
                    return authConfigJson;
                }
            },
            handleAdd() {
                this.dialogTitle = '新增 MCP Endpoint';
                this.form = {
                    id: 0, name: '', endpoint: '', endpointType: 'sse',
                    protocolVersion: '', description: '', enabled: true, bearerToken: ''
                };
                this.preTestResult = null;
                this.dialogVisible = true;
                this.$nextTick(() => {
                    if (this.$refs.mcpForm) this.$refs.mcpForm.clearValidate();
                });
            },
            handleEdit(row) {
                this.dialogTitle = '编辑 MCP Endpoint';
                this.form = Object.assign({}, row);
                if (!this.form.id) this.form.id = 0;
                this.form.bearerToken = this.extractBearerToken(row.authConfig);
                this.preTestResult = null;
                this.dialogVisible = true;
                this.$nextTick(() => {
                    if (this.$refs.mcpForm) this.$refs.mcpForm.clearValidate();
                });
            },
            buildAuthConfig() {
                var token = (this.form.bearerToken || '').trim();
                if (this.form.authConfig && token === this.extractBearerToken(this.form.authConfig)) {
                    return this.form.authConfig;
                }
                if (!token) return '';
                return JSON.stringify({ token: token });
            },
            async handleTestBeforeSave() {
                this.preTestLoading = true;
                this.preTestResult = null;
                try {
                    var res = await request(apiBase + '.TestConnection', {
                        name: this.form.name,
                        endpoint: this.form.endpoint,
                        endpointType: this.form.endpointType,
                        authConfig: this.buildAuthConfig()
                    });
                    var body = res.data || {};
                    this.preTestResult = connectionResult(body);
                    if (this.preTestResult.success) {
                        this.$message.success(this.preTestResult.statusMessage || '连接成功');
                    } else {
                        this.$message.error(this.preTestResult.statusMessage || '连接失败');
                    }
                } catch (error) {
                    console.error(error);
                    this.$message.error(errorMessage(error, '连接测试失败'));
                } finally {
                    this.preTestLoading = false;
                }
            },
            handleSubmit() {
                if (this.saving) return;
                return this.$refs.mcpForm.validate(async (valid) => {
                    if (!valid || this.saving) return;
                    this.saving = true;
                    try {
                        var payload = endpointPayload(this.form, this.buildAuthConfig());
                        var res = await request(apiBase + '.SaveEndpoint', payload);
                        var body = res.data || {};
                        this.$message.success(body.data || '保存成功');
                        this.dialogVisible = false;
                        await this.getList();
                    } catch (error) {
                        console.error(error);
                        this.$message.error(errorMessage(error, '保存失败'));
                    } finally {
                        this.saving = false;
                    }
                });
            },
            async handleToggleEnabled(row) {
                if (row.saving) return;
                this.$set(row, 'saving', true);
                try {
                    var payload = endpointPayload(row, row.authConfig || '');
                    await request(apiBase + '.SaveEndpoint', payload);
                } catch (error) {
                    row.enabled = !row.enabled;
                    console.error(error);
                    this.$message.error(errorMessage(error, '更新失败'));
                } finally {
                    this.$set(row, 'saving', false);
                }
            },
            async handleDelete(row) {
                try {
                    await this.$confirm('确认删除 MCP Endpoint「' + row.name + '」？', '提示', { type: 'warning' });
                } catch (e) {
                    return;
                }
                try {
                    await request(apiBase + '.DeleteEndpoint', { id: row.id });
                    this.$message.success('删除成功');
                    await this.getList();
                } catch (error) {
                    console.error(error);
                    this.$message.error(errorMessage(error, '删除失败'));
                }
            },
            async handleTest(row) {
                this.testLoadingId = row.id;
                try {
                    var res = await request(apiBase + '.TestEndpoint', { id: row.id });
                    var body = res.data || {};
                    var result = connectionResult(body);
                    this.testResult = result;
                    this.testResultEndpointName = row.name;
                    this.expandedTools = [];
                    if (result.success) {
                        this.$message.success(result.statusMessage || '连接成功');
                    } else {
                        this.$message.error(result.statusMessage || '连接失败');
                    }
                    this.testDialogVisible = true;
                    await this.getList();
                } catch (error) {
                    console.error(error);
                    this.$message.error(errorMessage(error, '连接测试失败'));
                } finally {
                    this.testLoadingId = 0;
                }
            },
            handleToolExpandChange(row, expanded) {
                var isExpanded = Array.isArray(expanded)
                    ? expanded.some(tool => tool.name === row.name)
                    : expanded;
                if (isExpanded && this.expandedTools.indexOf(row.name) < 0) {
                    this.expandedTools.push(row.name);
                } else if (!isExpanded) {
                    this.expandedTools = this.expandedTools.filter(z => z !== row.name);
                }
            },
            showSchema(tool) {
                this.schemaTitle = tool.name || '输入 Schema';
                this.schemaText = formatSchemaJson(tool.inputSchemaJson) || '（无 JSON Schema 信息）';
                this.schemaDialogVisible = true;
            }
        }
    });
})();

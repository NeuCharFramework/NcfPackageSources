/* Legacy comment translated to English. */
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
                throw new Error(ncfST('Frontend.Mcp.TheServerResponseFormatIsInvalidConfirmThatYouAreSignedIn'));
            }
            if (!body.success) {
                throw new Error(body.errorMessage || (typeof body.data === 'string' && body.data) || ncfST('Frontend.Mcp.RequestFailed'));
            }
            return response;
        });
    }

    function errorMessage(error, fallback) {
        var response = error && error.response;
        var body = response && response.data;
        var detail = body && (body.errorMessage || body.message || body.title);
        if (detail) return fallback + ': ' + detail;
        if (response && response.status === 404) {
                return fallback + ncfST('Frontend.Mcp.TheInterfaceWasNotFoundHTTP404ConfirmThatTheSiteHas');
        }
        if (response && (response.status === 401 || response.status === 403)) {
                return fallback + ncfST('Frontend.Mcp.YourSignInHasExpiredOrYouDoNotHavePermissionSign');
        }
        if (response) return fallback + '（HTTP ' + response.status + '）';
        return fallback + (error && error.message ? ': ' + error.message : '');
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
                throw new Error(ncfST('Frontend.Mcp.TheServerReturnedAnInvalidConnectionTestResultFormat'));
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
                publishedLoading: false,
                publishedServers: [],
                publishedFilterText: '',
                testLoadingId: 0,
                filterText: '',
                onlyEnabled: false,

                //Legacy comment translated to English.
                dialogVisible: false,
                    dialogTitle: ncfST('Frontend.Mcp.AddMCPEndpoint'),
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
                    name: [{ required: true, message: ncfST('Frontend.Mcp.EndpointNameCannotBeEmpty'), trigger: 'blur' }],
                    endpoint: [{ required: true, message: ncfST('Frontend.Mcp.EndpointAddressCannotBeEmpty'), trigger: 'blur' }]
                },
                endpointTypeOptions: ['sse', 'http', 'stdio', 'websocket'],

                //Legacy comment translated to English.
                preTestLoading: false,
                preTestResult: null,

                //Legacy comment translated to English.
                testDialogVisible: false,
                testResult: null,
                testResultEndpointName: '',
                expandedTools: [],

                //Legacy comment translated to English.
                schemaDialogVisible: false,
                schemaTitle: '',
                schemaText: ''
            };
        },
        computed: {
            filteredPublishedServers: function () {
                var key = this.publishedFilterText.toLowerCase().trim();
                if (!key) return this.publishedServers;
                return this.publishedServers.filter(server =>
                    (server.serverName || '').toLowerCase().indexOf(key) >= 0
                    || (server.xncfName || '').toLowerCase().indexOf(key) >= 0
                    || (server.route || '').toLowerCase().indexOf(key) >= 0
                    || server.endpoints.some(endpoint => endpoint.endpoint.toLowerCase().indexOf(key) >= 0));
            },
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
            this.getPublishedServers();
        },
        methods: {
            formatDateTime: formatDateTime,
            formatSchemaJson: formatSchemaJson,
            async getPublishedServers() {
                this.publishedLoading = true;
                try {
                    var res = await request(apiBase + '.GetPublishedServers');
                    var servers = res.data.data;
                    if (!Array.isArray(servers) || servers.some(server =>
                        !server || typeof server.serverName !== 'string'
                        || typeof server.xncfName !== 'string' || typeof server.route !== 'string'
                        || !Array.isArray(server.endpoints) || server.endpoints.some(endpoint =>
                            !endpoint || typeof endpoint.endpointType !== 'string' || typeof endpoint.endpoint !== 'string'))) {
                        throw new Error(ncfST('Frontend.Mcp.TheServerReturnedAnInvalidListOfMCPServicesPublishedByThis'));
                    }
                    this.publishedServers = servers;
                } catch (error) {
                    console.error(error);
                    this.$message.error(errorMessage(error, ncfST('Frontend.Mcp.FailedToReadMCPServicesPublishedByThisSite')));
                } finally {
                    this.publishedLoading = false;
                }
            },
            async copyPublishedEndpoint(endpoint) {
                if (!navigator.clipboard || !navigator.clipboard.writeText) {
                    this.$message.error(ncfST('Frontend.Mcp.ThisBrowserCannotCopyToTheClipboardSelectAndCopyTheEndpoint'));
                    return;
                }
                try {
                    await navigator.clipboard.writeText(endpoint);
                    this.$message.success(ncfST('Frontend.Mcp.EndpointAddressCopied'));
                } catch (error) {
                    console.error(error);
                    this.$message.error(errorMessage(error, ncfST('Frontend.Mcp.CopyFailedSelectAndCopyTheEndpointAddressManually')));
                }
            },
            async getList() {
                this.loading = true;
                try {
                    var res = await request(apiBase + '.GetAllEndpoints');
                    var body = res.data || {};
                    if (!Array.isArray(body.data)) {
                        throw new Error(ncfST('Frontend.Mcp.TheServerReturnedAnInvalidEndpointListFormat'));
                    }
                    this.tableData = body.data.map(z => Object.assign({}, z, {
                        bearerToken: this.extractBearerToken(z.authConfig)
                    }));
                } catch (error) {
                    console.error(error);
                    this.$message.error(errorMessage(error, ncfST('Frontend.Mcp.FailedToReadTheMCPEndpointList')));
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
                this.dialogTitle = ncfST('Frontend.Mcp.AddMCPEndpoint');
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
                this.dialogTitle = ncfST('Frontend.Mcp.EditMCPEndpoint');
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
                        this.$message.success(this.preTestResult.statusMessage || ncfST('Frontend.Mcp.EndpointConnectionTestSucceeded'));
                    } else {
                        this.$message.error(this.preTestResult.statusMessage || ncfST('Frontend.Mcp.EndpointConnectionTestFailed'));
                    }
                } catch (error) {
                    console.error(error);
                    this.$message.error(errorMessage(error, ncfST('Frontend.Mcp.EndpointConnectionTestFailed')));
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
                        this.$message.success(body.data || ncfST('Frontend.Mcp.SavedSuccessfully'));
                        this.dialogVisible = false;
                        await this.getList();
                    } catch (error) {
                        console.error(error);
                    this.$message.error(errorMessage(error, ncfST('Frontend.Mcp.SaveFailed')));
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
                    this.$message.error(errorMessage(error, ncfST('Frontend.Mcp.UpdateFailed')));
                } finally {
                    this.$set(row, 'saving', false);
                }
            },
            async handleDelete(row) {
                try {
                    await this.$confirm(ncfST('Frontend.Mcp.DeleteMCPEndpointValue', row.name), ncfST('Frontend.Mcp.Notice'), { type: 'warning' });
                } catch (e) {
                    return;
                }
                try {
                    await request(apiBase + '.DeleteEndpoint', { id: row.id });
                    this.$message.success(ncfST('Frontend.Mcp.DeletedSuccessfully'));
                    await this.getList();
                } catch (error) {
                    console.error(error);
                    this.$message.error(errorMessage(error, ncfST('Frontend.Mcp.DeleteFailed')));
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
                        this.$message.success(result.statusMessage || ncfST('Frontend.Mcp.EndpointConnectionTestSucceeded'));
                    } else {
                        this.$message.error(result.statusMessage || ncfST('Frontend.Mcp.EndpointConnectionTestFailed'));
                    }
                    this.testDialogVisible = true;
                    await this.getList();
                } catch (error) {
                    console.error(error);
                    this.$message.error(errorMessage(error, ncfST('Frontend.Mcp.EndpointConnectionTestFailed')));
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
                this.schemaTitle = tool.name || ncfST('Frontend.Mcp.InputSchema');
                this.schemaText = formatSchemaJson(tool.inputSchemaJson) || ncfST('Frontend.Mcp.NoJSONSchemaInformation');
                this.schemaDialogVisible = true;
            }
        }
    });
})();

(function () {
    const pageUrl = window.location.pathname || '/Admin/FileManager/Index';
    const maxFilesPerUpload = 20;
    const maxTotalUploadBytes = 100 * 1024 * 1024;
    const maxFileSizeBytes = 50 * 1024 * 1024;

    function unwrap(response) {
        return response && response.data && response.data.data !== undefined ? response.data.data : response.data;
    }

    function errorMessage(error) {
        const data = error && error.response && error.response.data;
        const message = typeof data === 'string' ? data : (data && (data.errorMessage || data.message || data.title));
        if (message) return message;
        if (error && error.response && error.response.status === 400) {
            return '请求被服务器拒绝，请刷新页面后重试。';
        }
        return (error && error.message) || '请求失败';
    }

    function getRequestVerificationToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!tokenInput || !tokenInput.value) {
            throw new Error('页面防伪令牌缺失，请刷新页面后重试。');
        }
        return tokenInput.value;
    }

    function post(url, data, config) {
        const requestConfig = config || {};
        requestConfig.headers = Object.assign({}, requestConfig.headers, {
            RequestVerificationToken: getRequestVerificationToken(),
            'x-requested-with': 'XMLHttpRequest'
        });
        return axios.post(url, data, requestConfig);
    }

    new Vue({
        el: '#app',
        data: function () {
            return {
                tableData: [],
                tableLoading: false,
                page: { page: 1, size: 10 },
                total: 0,
                resourceScope: 100,
                currentFolderId: null,
                folderPath: [],
                folderTree: [],
                folderTreeKey: 0,
                folderTreeProps: {
                    label: 'name',
                    children: 'children',
                    isLeaf: function (data) { return data && data.hasChildren === false; }
                },
                uploadDialog: { visible: false, fileList: [], uploading: false, progress: 0, mode: 'files', folderRootName: '' },
                folderDialog: { visible: false, loading: false, editing: false, form: { id: null, name: '', description: '' } },
                noteDialog: { visible: false, loading: false, row: null, note: '' },
                fileTagDialog: { visible: false, loading: false, adding: false, draft: '', row: null, tags: [] },
                guideDialogVisible: false,
                treeFilter: '',
                fileSearchKeyword: '',
                orgName: '山西米立信息技术有限公司',
                rootFolderName: '企业文档',
                activeNavKey: 'enterprise',
                dashboard: {
                    capacityRange: '7d',
                    capacityDates: [],
                    statsCutoff: '',
                    enterpriseUsed: '0B',
                    totalCount: 0,
                    totalSizeBytes: 0,
                    capacityTrend: [],
                    orgUsages: [
                        { name: '山西米立信息技术有限公司', size: '0B' },
                        { name: '企业文档根目录', size: '0B' },
                        { name: '知识库资料', size: '0B' }
                    ],
                    sizeTotalLabel: '0B',
                    countTotalLabel: '0',
                    sizeSlices: [
                        { name: '文档', value: 0, color: '#95de64' },
                        { name: '图片', value: 0, color: '#597ef7' },
                        { name: '视频', value: 0, color: '#ffd666' },
                        { name: '音频', value: 0, color: '#ff9c6e' },
                        { name: '其他', value: 0, color: '#91d5ff' }
                    ],
                    countSlices: [
                        { name: '文档', value: 0, color: '#95de64' },
                        { name: '图片', value: 0, color: '#597ef7' },
                        { name: '视频', value: 0, color: '#ffd666' },
                        { name: '音频', value: 0, color: '#ff9c6e' },
                        { name: '其他', value: 0, color: '#91d5ff' }
                    ]
                },
                _dashboardCharts: null,
                tagManager: {
                    keyword: '',
                    category: '',
                    status: '',
                    selectedIds: [],
                    categoryOptions: [],
                    list: [],
                    displayList: [],
                    tags: [],
                    treeExpanded: true,
                    treeExpandKey: 0
                },
                tagManagerStorageKey: 'ncf.fileManager.tagManager',
                tagAdminOptions: ['管理员', '当前用户'],
                tagCategoryDialog: {
                    visible: false,
                    loading: false,
                    editing: false,
                    form: { id: '', name: '', description: '', parentId: '', admin: '' }
                },
                tagCreateDialog: {
                    visible: false,
                    loading: false,
                    editing: false,
                    form: { id: '', name: '', description: '', categoryId: '' }
                },
                tagCategoryRules: {
                    name: [
                        { required: true, message: '请输入分类名称', trigger: 'blur' },
                        { max: 50, message: '分类名称最多50个字符', trigger: 'blur' }
                    ]
                },
                tagCreateRules: {
                    name: [
                        { required: true, message: '请输入标签名称', trigger: 'blur' },
                        { max: 30, message: '标签名称最多30个字符', trigger: 'blur' }
                    ],
                    categoryId: [
                        { required: true, message: '请选择所属分类', trigger: 'change' }
                    ]
                },
                recycleBin: {
                    list: [],
                    displayList: [],
                    selectedIds: [],
                    page: 1,
                    size: 20,
                    total: 0,
                    emptying: false
                },
                recycleEmptyDialog: { visible: false },
                recycleStorageKey: 'ncf.fileManager.recycleBin',
                favoriteList: {
                    keyword: '',
                    list: [],
                    displayList: [],
                    page: 1,
                    size: 20,
                    total: 0
                },
                favoriteStorageKey: 'ncf.fileManager.favorites',
                quickAccessStorageKey: 'ncf.fileManager.quickAccess',
                homePage: {
                    keyword: '',
                    list: [],
                    displayList: [],
                    quickAccess: []
                },
                uploadSettingsStorageKey: 'ncf.fileManager.uploadSettings',
                uploadSettings: {
                    allowedTypesText: '',
                    maxSizeMb: 50,
                    blockedTypesText: ''
                },
                navItems: [
                    { key: 'home', label: '首页', icon: 'el-icon-s-home' },
                    { key: 'favorite', label: '收藏', icon: 'el-icon-star-off' },
                    { key: 'enterprise', label: '企业文档', icon: 'el-icon-folder', scope: 100 },
                    { key: 'recycle', label: '回收站', icon: 'el-icon-delete' },
                    { key: 'divider', label: '', divider: true },
                    { key: 'dashboard', label: '数据面板', icon: 'el-icon-data-line' },
                    { key: 'tags', label: '标签管理', icon: 'el-icon-price-tag' },
                    { key: 'settings', label: '设置', icon: 'el-icon-setting' }
                ]
            };
        },
        watch: {
            treeFilter: function (value) {
                if (this.$refs.folderTree) this.$refs.folderTree.filter(value);
            },
            activeNavKey: function (value) {
                if (value === 'dashboard') {
                    this.$nextTick(function () {
                        this.loadDashboardStats();
                    }.bind(this));
                }
            }
        },
        computed: {
            isSiteAsset: function () { return this.resourceScope === 200; },
            resourceScopeName: function () { return this.isSiteAsset ? '站点静态资源' : '知识库资料'; },
            canGoParent: function () { return this.currentFolderId != null; },
            displayTableData: function () {
                const keyword = (this.fileSearchKeyword || '').trim().toLowerCase();
                if (!keyword) return this.tableData;
                return this.tableData.filter(function (row) {
                    return (row.fileName || '').toLowerCase().indexOf(keyword) !== -1
                        || (row.description || '').toLowerCase().indexOf(keyword) !== -1;
                });
            },
            scopeHint: function () {
                return this.isSiteAsset
                    ? '站点静态资源默认私有；公开后会生成带 SHA-256 指纹的 /assets/ URL。为防止同源脚本注入，不接受 HTML、SVG、JavaScript 或压缩包。'
                    : '知识库源文件只能是可安全提取的文本和 Office Open XML 文件。它们不会生成公开 URL，也不能在此处发布。';
            },
            uploadExtensionHint: function () {
                if (this.uploadSettings && this.uploadSettings.allowedTypesText) {
                    const list = this.parseExtensionList(this.uploadSettings.allowedTypesText);
                    if (list.length) {
                        return '支持 ' + list.map(function (ext) { return ext.replace(/^\./, '').toUpperCase(); }).join('、') + '。';
                    }
                }
                return this.isSiteAsset
                    ? '支持 JPG、PNG、GIF、WebP、AVIF、ICO、音视频和字体格式。'
                    : '支持文本、JSON/XML/YAML、代码、DOCX、XLSX、PPTX。';
            },
            uploadTargetText: function () {
                const currentFolder = this.folderPath.length ? this.folderPath[this.folderPath.length - 1].name : this.rootFolderName;
                return this.resourceScopeName + ' / ' + currentFolder;
            },
            uploadFolderSummary: function () {
                if (this.uploadDialog.mode !== 'folder' || !this.uploadDialog.fileList.length) return '';
                return '已选择文件夹“' + this.uploadDialog.folderRootName + '”，共 ' + this.uploadDialog.fileList.length + ' 个文件；上传后会在左侧文件树中还原原始目录结构。';
            },
            tagCategorySelectOptions: function () {
                const list = (this.tagManager && this.tagManager.list) || [];
                return list.map(function (row) {
                    return { label: row.name, value: row.id };
                });
            }
        },
        created: function () {
            this.restoreRouteState();
            this.syncActiveNavFromScope();
            this.initDashboardDefaults();
            this.loadFavoritesFromStorage();
            this.loadRecycleFromStorage();
            this.loadQuickAccessFromStorage();
            this.loadUploadSettingsFromStorage();
            this.loadTagManagerFromStorage();
            this.enterFolder(this.currentFolderId);
        },
        mounted: function () {
            window.addEventListener('resize', this.resizeDashboardCharts);
        },
        beforeDestroy: function () {
            window.removeEventListener('resize', this.resizeDashboardCharts);
            this.disposeDashboardCharts();
        },
        methods: {
            syncActiveNavFromScope: function () {
                this.activeNavKey = 'enterprise';
            },
            onNavClick: function (item) {
                if (!item || item.divider) return;
                if (item.key === 'settings') {
                    this.activeNavKey = 'settings';
                    this.loadUploadSettingsFromStorage();
                    return;
                }
                if (item.key === 'dashboard') {
                    this.activeNavKey = 'dashboard';
                    this.$nextTick(function () {
                        this.loadDashboardStats();
                    }.bind(this));
                    return;
                }
                if (item.key === 'tags') {
                    this.activeNavKey = 'tags';
                    this.loadTagManagerFromStorage();
                    return;
                }
                if (item.key === 'recycle') {
                    this.activeNavKey = 'recycle';
                    this.$set(this.recycleBin, 'emptying', false);
                    this.loadRecycleFromStorage();
                    return;
                }
                if (item.key === 'favorite') {
                    this.activeNavKey = 'favorite';
                    this.searchFavorites();
                    return;
                }
                if (item.key === 'home') {
                    this.activeNavKey = 'home';
                    this.searchHomeRecent();
                    return;
                }
                if (item.scope === 100 || item.scope === 200) {
                    if (this.resourceScope === item.scope) {
                        this.activeNavKey = item.key;
                        return;
                    }
                    this.resourceScope = item.scope;
                    this.activeNavKey = item.key;
                    this.changeResourceScope();
                    return;
                }
                if (item.key === 'enterprise') {
                    this.activeNavKey = 'enterprise';
                    return;
                }
                this.activeNavKey = item.key;
                this.$message.info('「' + item.label + '」功能即将开放');
            },
            initDashboardDefaults: function () {
                const end = new Date();
                const start = new Date();
                start.setDate(end.getDate() - 6);
                const fmt = function (d) {
                    const m = String(d.getMonth() + 1).padStart(2, '0');
                    const day = String(d.getDate()).padStart(2, '0');
                    return d.getFullYear() + '-' + m + '-' + day;
                };
                const pad = function (n) { return String(n).padStart(2, '0'); };
                this.dashboard.capacityDates = [fmt(start), fmt(end)];
                this.dashboard.statsCutoff = fmt(end) + ' ' + pad(end.getHours()) + ':' + pad(end.getMinutes()) + ':' + pad(end.getSeconds());
                if (!this.dashboard.orgUsages.some(function (x) { return x.name === '山西米立信息技术有限公司'; })) {
                    this.dashboard.orgUsages.unshift({ name: '山西米立信息技术有限公司', size: this.dashboard.enterpriseUsed });
                }
            },
            getOrCreateChart: function (el) {
                if (!el || typeof echarts === 'undefined') return null;
                const existing = echarts.getInstanceByDom(el);
                return existing || echarts.init(el);
            },
            buildCapacityDates: function () {
                const dates = this.dashboard.capacityDates;
                if (Array.isArray(dates) && dates.length === 2 && dates[0] && dates[1]) {
                    const list = [];
                    const cursor = new Date(dates[0]);
                    const end = new Date(dates[1]);
                    while (cursor <= end && list.length < 366) {
                        const m = String(cursor.getMonth() + 1).padStart(2, '0');
                        const d = String(cursor.getDate()).padStart(2, '0');
                        list.push(m + '-' + d);
                        cursor.setDate(cursor.getDate() + 1);
                    }
                    if (list.length) return list;
                }
                const days = this.dashboard.capacityRange === '365d' ? 365 : (this.dashboard.capacityRange === '30d' ? 30 : 7);
                const result = [];
                const now = new Date();
                for (let i = days - 1; i >= 0; i--) {
                    const d = new Date(now);
                    d.setDate(now.getDate() - i);
                    result.push(String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0'));
                }
                return result;
            },
            onDashboardRangeChange: function () {
                const end = new Date();
                const start = new Date();
                const days = this.dashboard.capacityRange === '365d' ? 365 : (this.dashboard.capacityRange === '30d' ? 30 : 7);
                start.setDate(end.getDate() - (days - 1));
                const fmt = function (d) {
                    return d.getFullYear() + '-' + String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0');
                };
                this.dashboard.capacityDates = [fmt(start), fmt(end)];
                this.loadDashboardStats();
            },
            loadDashboardStats: async function () {
                if (!this.dashboard) return;
                try {
                    let url = pageUrl + '?handler=DashboardStats';
                    const dates = this.dashboard.capacityDates;
                    if (Array.isArray(dates) && dates[0] && dates[1]) {
                        url += '&startDate=' + encodeURIComponent(dates[0]) + '&endDate=' + encodeURIComponent(dates[1]);
                    }
                    const result = unwrap(await axios.get(url)) || {};
                    if (result.statsCutoff) this.dashboard.statsCutoff = result.statsCutoff;
                    if (result.enterpriseUsed) this.dashboard.enterpriseUsed = result.enterpriseUsed;
                    if (result.totalCount != null) this.dashboard.totalCount = result.totalCount;
                    if (result.totalSizeBytes != null) this.dashboard.totalSizeBytes = result.totalSizeBytes;
                    if (Array.isArray(result.orgUsages)) this.dashboard.orgUsages = result.orgUsages;
                    if (result.sizeTotalLabel) this.dashboard.sizeTotalLabel = result.sizeTotalLabel;
                    if (result.countTotalLabel != null) this.dashboard.countTotalLabel = String(result.countTotalLabel);
                    if (Array.isArray(result.sizeSlices)) this.dashboard.sizeSlices = result.sizeSlices;
                    if (Array.isArray(result.countSlices)) this.dashboard.countSlices = result.countSlices;
                    if (Array.isArray(result.capacityTrend)) this.dashboard.capacityTrend = result.capacityTrend;
                } catch (error) {
                    this.dashboard.capacityTrend = this.buildCapacityDates().map(function (date) {
                        return { date: date, fileCount: 0, totalSizeBytes: 0 };
                    });
                }
                this.$nextTick(this.refreshDashboardCharts);
            },
            refreshDashboardCharts: function () {
                if (this.activeNavKey !== 'dashboard') return;
                if (typeof echarts === 'undefined') return;

                const trend = Array.isArray(this.dashboard.capacityTrend) && this.dashboard.capacityTrend.length
                    ? this.dashboard.capacityTrend
                    : this.buildCapacityDates().map(function (date) {
                        return { date: date, fileCount: 0, totalSizeBytes: 0 };
                    });
                const labels = trend.map(function (x) { return x.date; });
                const fileCounts = trend.map(function (x) { return Number(x.fileCount || 0); });
                const sizeMb = trend.map(function (x) {
                    return Number(((Number(x.totalSizeBytes || 0)) / (1024 * 1024)).toFixed(2));
                });

                const capacityChart = this.getOrCreateChart(this.$refs.capacityChart);
                if (capacityChart) {
                    capacityChart.setOption({
                        color: ['#409eff', '#67c23a'],
                        tooltip: {
                            trigger: 'axis',
                            formatter: function (params) {
                                if (!params || !params.length) return '';
                                var lines = [params[0].axisValue];
                                params.forEach(function (p) {
                                    var unit = p.seriesName === '文件数量' ? ' 个' : ' MB';
                                    lines.push(p.marker + p.seriesName + '：' + p.data + unit);
                                });
                                return lines.join('<br/>');
                            }
                        },
                        legend: { bottom: 0, data: ['文件数量', '总大小'] },
                        grid: { left: 48, right: 56, top: 28, bottom: 48 },
                        xAxis: { type: 'category', boundaryGap: false, data: labels },
                        yAxis: [
                            {
                                type: 'value',
                                name: '数量',
                                minInterval: 1,
                                axisLabel: { formatter: '{value}' },
                                splitLine: { lineStyle: { type: 'dashed' } }
                            },
                            {
                                type: 'value',
                                name: '大小',
                                axisLabel: { formatter: '{value}MB' },
                                splitLine: { show: false }
                            }
                        ],
                        series: [
                            {
                                name: '文件数量',
                                type: 'line',
                                smooth: true,
                                yAxisIndex: 0,
                                data: fileCounts,
                                areaStyle: { opacity: 0.08 }
                            },
                            {
                                name: '总大小',
                                type: 'line',
                                smooth: true,
                                yAxisIndex: 1,
                                data: sizeMb
                            }
                        ]
                    }, true);
                }

                const sizeSlices = (this.dashboard.sizeSlices || []).filter(function (x) { return Number(x.value) > 0; });
                const sizeChart = this.getOrCreateChart(this.$refs.sizeChart);
                if (sizeChart) {
                    sizeChart.setOption({
                        tooltip: { trigger: 'item', formatter: '{b}: {c} MB ({d}%)' },
                        legend: { bottom: 0, data: (this.dashboard.sizeSlices || []).map(function (x) { return x.name; }) },
                        series: [{
                            type: 'pie',
                            radius: ['48%', '68%'],
                            center: ['50%', '46%'],
                            label: { formatter: '{b}\\n{d}%' },
                            data: (sizeSlices.length ? sizeSlices : this.dashboard.sizeSlices).map(function (x) {
                                return { name: x.name, value: Number(x.value || 0), itemStyle: { color: x.color } };
                            }),
                            emphasis: { scale: false }
                        }],
                        graphic: [{
                            type: 'text',
                            left: 'center',
                            top: '42%',
                            style: { text: this.dashboard.sizeTotalLabel || '0B', textAlign: 'center', fill: '#303133', fontSize: 22, fontWeight: 600 }
                        }]
                    }, true);
                }

                const countSlices = (this.dashboard.countSlices || []).filter(function (x) { return Number(x.value) > 0; });
                const countChart = this.getOrCreateChart(this.$refs.countChart);
                if (countChart) {
                    countChart.setOption({
                        tooltip: { trigger: 'item', formatter: '{b}: {c} 个 ({d}%)' },
                        legend: { bottom: 0, data: (this.dashboard.countSlices || []).map(function (x) { return x.name; }) },
                        series: [{
                            type: 'pie',
                            radius: ['48%', '68%'],
                            center: ['50%', '46%'],
                            label: { formatter: '{b}\\n{c}个' },
                            data: (countSlices.length ? countSlices : this.dashboard.countSlices).map(function (x) {
                                return { name: x.name, value: Number(x.value || 0), itemStyle: { color: x.color } };
                            }),
                            emphasis: { scale: false }
                        }],
                        graphic: [{
                            type: 'text',
                            left: 'center',
                            top: '42%',
                            style: { text: String(this.dashboard.countTotalLabel || '0'), textAlign: 'center', fill: '#303133', fontSize: 28, fontWeight: 600 }
                        }]
                    }, true);
                }

                this._dashboardCharts = [capacityChart, sizeChart, countChart].filter(Boolean);
                this.resizeDashboardCharts();
            },
            resizeDashboardCharts: function () {
                if (!this._dashboardCharts) return;
                this._dashboardCharts.forEach(function (chart) {
                    if (chart) chart.resize();
                });
            },
            disposeDashboardCharts: function () {
                if (!this._dashboardCharts) return;
                this._dashboardCharts.forEach(function (chart) {
                    if (chart) chart.dispose();
                });
                this._dashboardCharts = null;
            },
            searchTags: function () {
                if (!this.tagManager) return;
                this.tagManager.displayList = this.buildTagTreeDisplay();
            },
            matchTagStatusFilter: function (statusValue, filter) {
                const enabled = statusValue === '启用';
                if (filter === 'enabled') return enabled;
                if (filter === 'disabled') return !enabled;
                return true;
            },
            isTagRowEnabled: function (row) {
                return !!(row && row.status === '启用');
            },
            isTagCategoryRow: function (row) {
                return !!(row && row.nodeType !== 'tag');
            },
            buildTagTreeDisplay: function () {
                if (!this.tagManager) return [];
                const keyword = (this.tagManager.keyword || '').trim().toLowerCase();
                const categoryFilter = this.tagManager.category || '';
                const statusFilter = this.tagManager.status || '';
                const self = this;
                const categoryMap = {};
                const categories = (this.tagManager.list || []).map(function (row) {
                    const cat = Object.assign({}, row, { nodeType: 'category', children: [] });
                    categoryMap[cat.id] = cat;
                    return cat;
                });

                categories.forEach(function (cat) {
                    const hasParent = !!(cat.parentId && categoryMap[cat.parentId]);
                    cat.type = hasParent ? '二级分类' : '一级分类';
                    cat.level = hasParent ? 2 : 1;
                    if (hasParent) {
                        categoryMap[cat.parentId].children.push(cat);
                        cat.__linked = true;
                    }
                });

                (this.tagManager.tags || []).forEach(function (row) {
                    const tag = Object.assign({}, row, {
                        nodeType: 'tag',
                        type: '标签',
                        tagCount: '-',
                        admin: '-',
                        children: []
                    });
                    const parent = tag.categoryId && categoryMap[tag.categoryId];
                    if (parent) parent.children.push(tag);
                });

                categories.forEach(function (cat) {
                    cat.tagCount = (cat.children || []).filter(function (c) { return c.nodeType === 'tag'; }).length;
                });

                function prune(node) {
                    const children = (node.children || []).map(prune).filter(function (item) { return !!item; });
                    const nameOk = !keyword || String(node.name || '').toLowerCase().indexOf(keyword) !== -1;
                    const statusOk = self.matchTagStatusFilter(node.status, statusFilter);
                    if (nameOk && statusOk) {
                        return Object.assign({}, node, { children: children });
                    }
                    if (children.length) {
                        return Object.assign({}, node, { children: children });
                    }
                    return null;
                }

                let roots = categories.filter(function (cat) { return !cat.__linked; });
                if (categoryFilter && categoryMap[categoryFilter]) {
                    roots = [categoryMap[categoryFilter]];
                }
                return roots.map(prune).filter(function (item) { return !!item; });
            },
            resetTagFilters: function () {
                if (!this.tagManager) return;
                this.tagManager.keyword = '';
                this.tagManager.category = '';
                this.tagManager.status = '';
                this.searchTags();
            },
            onTagSelectionChange: function (rows) {
                this.tagManager.selectedIds = (rows || []).map(function (row) { return row.id; });
            },
            formatTagDateTime: function (date) {
                const d = date instanceof Date ? date : new Date(date || Date.now());
                const pad = function (n) { return n < 10 ? '0' + n : '' + n; };
                return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate())
                    + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
            },
            syncTagCategoryOptions: function () {
                if (!this.tagManager) return;
                this.tagManager.categoryOptions = (this.tagManager.list || []).map(function (row) {
                    return { label: row.name, value: row.id };
                });
            },
            loadTagManagerFromStorage: function () {
                if (!this.tagManager) {
                    this.tagManager = {
                        keyword: '', category: '', status: '', selectedIds: [],
                        categoryOptions: [], list: [], displayList: [], tags: [],
                        treeExpanded: true, treeExpandKey: 0
                    };
                }
                if (this.tagManager.treeExpanded == null) this.tagManager.treeExpanded = true;
                if (this.tagManager.treeExpandKey == null) this.tagManager.treeExpandKey = 0;
                try {
                    const raw = localStorage.getItem(this.tagManagerStorageKey || 'ncf.fileManager.tagManager');
                    const saved = raw ? JSON.parse(raw) : null;
                    this.tagManager.list = saved && Array.isArray(saved.categories) ? saved.categories : [];
                    this.tagManager.tags = saved && Array.isArray(saved.tags) ? saved.tags : [];
                } catch (e) {
                    this.tagManager.list = [];
                    this.tagManager.tags = [];
                }
                this.syncTagCategoryOptions();
                this.searchTags();
            },
            saveTagManagerToStorage: function () {
                try {
                    localStorage.setItem(
                        this.tagManagerStorageKey || 'ncf.fileManager.tagManager',
                        JSON.stringify({
                            categories: (this.tagManager && this.tagManager.list) || [],
                            tags: (this.tagManager && this.tagManager.tags) || []
                        })
                    );
                } catch (e) { /* ignore */ }
            },
            createTag: function (presetCategoryId) {
                this.loadTagManagerFromStorage();
                if (!(this.tagManager.list || []).length) {
                    this.$message.warning('请先新增分类后再创建标签');
                    return;
                }
                this.tagCreateDialog = {
                    visible: true,
                    loading: false,
                    editing: false,
                    form: { id: '', name: '', description: '', categoryId: presetCategoryId || '' }
                };
                const self = this;
                this.$nextTick(function () {
                    if (self.$refs.tagCreateForm) self.$refs.tagCreateForm.clearValidate();
                });
            },
            createTagCategory: function (presetParentId) {
                this.loadTagManagerFromStorage();
                this.tagCategoryDialog = {
                    visible: true,
                    loading: false,
                    editing: false,
                    form: { id: '', name: '', description: '', parentId: presetParentId || '', admin: '' }
                };
                const self = this;
                this.$nextTick(function () {
                    if (self.$refs.tagCategoryForm) self.$refs.tagCategoryForm.clearValidate();
                });
            },
            createTagUnder: function (row) {
                if (!this.isTagCategoryRow(row)) return;
                this.createTag(row.id);
            },
            createTagCategoryUnder: function (row) {
                if (!this.isTagCategoryRow(row)) return;
                this.createTagCategory(row.id);
            },
            editTagRow: function (row) {
                if (!row) return;
                if (row.nodeType === 'tag') {
                    this.tagCreateDialog = {
                        visible: true,
                        loading: false,
                        editing: true,
                        form: {
                            id: row.id,
                            name: row.name || '',
                            description: row.description || '',
                            categoryId: row.categoryId || ''
                        }
                    };
                    const self = this;
                    this.$nextTick(function () {
                        if (self.$refs.tagCreateForm) self.$refs.tagCreateForm.clearValidate();
                    });
                    return;
                }
                this.tagCategoryDialog = {
                    visible: true,
                    loading: false,
                    editing: true,
                    form: {
                        id: row.id,
                        name: row.name || '',
                        description: row.description || '',
                        parentId: row.parentId || '',
                        admin: row.admin && row.admin !== '-' ? row.admin : ''
                    }
                };
                const self = this;
                this.$nextTick(function () {
                    if (self.$refs.tagCategoryForm) self.$refs.tagCategoryForm.clearValidate();
                });
            },
            deleteTagRow: function (row) {
                if (!row) return;
                const self = this;
                const tip = row.nodeType === 'tag'
                    ? '确定删除标签「' + row.name + '」吗？'
                    : '确定删除分类「' + row.name + '」及其下属标签吗？';
                this.$confirm(tip, '提示', { type: 'warning' }).then(function () {
                    if (row.nodeType === 'tag') {
                        self.tagManager.tags = (self.tagManager.tags || []).filter(function (item) { return item.id !== row.id; });
                        const category = (self.tagManager.list || []).find(function (item) { return item.id === row.categoryId; });
                        if (category) {
                            category.tagCount = Math.max(0, (category.tagCount || 0) - 1);
                            category.updatedAt = self.formatTagDateTime(new Date());
                        }
                    } else {
                        const removeIds = {};
                        removeIds[row.id] = true;
                        (self.tagManager.list || []).forEach(function (cat) {
                            if (cat.parentId === row.id) removeIds[cat.id] = true;
                        });
                        self.tagManager.list = (self.tagManager.list || []).filter(function (cat) { return !removeIds[cat.id]; });
                        self.tagManager.tags = (self.tagManager.tags || []).filter(function (tag) { return !removeIds[tag.categoryId]; });
                    }
                    self.syncTagCategoryOptions();
                    self.saveTagManagerToStorage();
                    self.searchTags();
                    self.$message.success('已删除');
                }).catch(function () { /* cancel */ });
            },
            toggleTagRowStatus: function (row) {
                if (!row) return;
                const enabled = this.isTagRowEnabled(row);
                const nextStatus = enabled ? '已停用' : '启用';
                const now = this.formatTagDateTime(new Date());
                if (row.nodeType === 'tag') {
                    const tag = (this.tagManager.tags || []).find(function (item) { return item.id === row.id; });
                    if (!tag) return;
                    tag.status = nextStatus;
                    tag.updatedAt = now;
                } else {
                    const cat = (this.tagManager.list || []).find(function (item) { return item.id === row.id; });
                    if (!cat) return;
                    cat.status = nextStatus;
                    cat.updatedAt = now;
                }
                this.saveTagManagerToStorage();
                this.searchTags();
                this.$message.success(enabled ? '已停用' : '已启用');
            },
            setSelectedTagStatus: function (enabled) {
                const ids = (this.tagManager && this.tagManager.selectedIds) || [];
                if (!ids.length) {
                    this.$message.warning('请先选择数据');
                    return;
                }
                const idMap = {};
                ids.forEach(function (id) { idMap[id] = true; });
                const nextStatus = enabled ? '启用' : '已停用';
                const now = this.formatTagDateTime(new Date());
                (this.tagManager.list || []).forEach(function (cat) {
                    if (idMap[cat.id]) {
                        cat.status = nextStatus;
                        cat.updatedAt = now;
                    }
                });
                (this.tagManager.tags || []).forEach(function (tag) {
                    if (idMap[tag.id]) {
                        tag.status = nextStatus;
                        tag.updatedAt = now;
                    }
                });
                this.saveTagManagerToStorage();
                this.searchTags();
                this.$message.success(enabled ? '已启用所选项目' : '已停用所选项目');
            },
            onTagCategoryDialogClosed: function () {
                if (!this.tagCategoryDialog) return;
                this.tagCategoryDialog.loading = false;
                this.tagCategoryDialog.editing = false;
                this.tagCategoryDialog.form = { id: '', name: '', description: '', parentId: '', admin: '' };
                if (this.$refs.tagCategoryForm) this.$refs.tagCategoryForm.clearValidate();
            },
            onTagCreateDialogClosed: function () {
                if (!this.tagCreateDialog) return;
                this.tagCreateDialog.loading = false;
                this.tagCreateDialog.editing = false;
                this.tagCreateDialog.form = { id: '', name: '', description: '', categoryId: '' };
                if (this.$refs.tagCreateForm) this.$refs.tagCreateForm.clearValidate();
            },
            submitTagCategory: function () {
                const formRef = this.$refs.tagCategoryForm;
                if (!formRef) return;
                const self = this;
                formRef.validate(function (valid) {
                    if (!valid) return;
                    const form = self.tagCategoryDialog.form || {};
                    const name = (form.name || '').trim();
                    const editing = !!self.tagCategoryDialog.editing;
                    const currentId = form.id || '';
                    if (!name) {
                        self.$message.warning('请输入分类名称');
                        return;
                    }
                    if (form.parentId && form.parentId === currentId) {
                        self.$message.warning('所属分类不能选择自己');
                        return;
                    }
                    const exists = (self.tagManager.list || []).some(function (row) {
                        if (editing && row.id === currentId) return false;
                        return String(row.name || '').toLowerCase() === name.toLowerCase();
                    });
                    if (exists) {
                        self.$message.warning('分类名称已存在');
                        return;
                    }
                    self.tagCategoryDialog.loading = true;
                    try {
                        const now = self.formatTagDateTime(new Date());
                        if (editing) {
                            const target = (self.tagManager.list || []).find(function (row) { return row.id === currentId; });
                            if (!target) {
                                self.$message.error('分类不存在');
                                return;
                            }
                            target.name = name;
                            target.description = (form.description || '').trim();
                            target.parentId = form.parentId || '';
                            target.admin = form.admin || '-';
                            target.updatedAt = now;
                            target.updatedBy = '当前用户';
                            self.$message.success('分类已更新');
                        } else {
                            self.tagManager.list.unshift({
                                id: 'cat-' + Date.now(),
                                name: name,
                                description: (form.description || '').trim(),
                                parentId: form.parentId || '',
                                type: '分类',
                                status: '启用',
                                tagCount: 0,
                                updatedAt: now,
                                updatedBy: '当前用户',
                                admin: form.admin || '-'
                            });
                            self.$message.success('分类已创建');
                        }
                        self.syncTagCategoryOptions();
                        self.saveTagManagerToStorage();
                        self.searchTags();
                        self.tagCategoryDialog.visible = false;
                    } finally {
                        self.tagCategoryDialog.loading = false;
                    }
                });
            },
            submitTagCreate: function () {
                const formRef = this.$refs.tagCreateForm;
                if (!formRef) return;
                const self = this;
                formRef.validate(function (valid) {
                    if (!valid) return;
                    const form = self.tagCreateDialog.form || {};
                    const name = (form.name || '').trim();
                    const categoryId = form.categoryId;
                    const editing = !!self.tagCreateDialog.editing;
                    const currentId = form.id || '';
                    if (!name) {
                        self.$message.warning('请输入标签名称');
                        return;
                    }
                    if (!categoryId) {
                        self.$message.warning('请选择所属分类');
                        return;
                    }
                    const category = (self.tagManager.list || []).find(function (row) { return row.id === categoryId; });
                    if (!category) {
                        self.$message.warning('所属分类不存在，请重新选择');
                        return;
                    }
                    const dup = (self.tagManager.tags || []).some(function (tag) {
                        if (editing && tag.id === currentId) return false;
                        return tag.categoryId === categoryId
                            && String(tag.name || '').toLowerCase() === name.toLowerCase();
                    });
                    if (dup) {
                        self.$message.warning('该分类下已存在同名标签');
                        return;
                    }
                    self.tagCreateDialog.loading = true;
                    try {
                        if (!Array.isArray(self.tagManager.tags)) self.tagManager.tags = [];
                        const now = self.formatTagDateTime(new Date());
                        if (editing) {
                            const target = self.tagManager.tags.find(function (tag) { return tag.id === currentId; });
                            if (!target) {
                                self.$message.error('标签不存在');
                                return;
                            }
                            const oldCategoryId = target.categoryId;
                            target.name = name;
                            target.description = (form.description || '').trim();
                            target.categoryId = categoryId;
                            target.categoryName = category.name;
                            target.updatedAt = now;
                            target.updatedBy = '当前用户';
                            if (oldCategoryId !== categoryId) {
                                const oldCat = (self.tagManager.list || []).find(function (row) { return row.id === oldCategoryId; });
                                if (oldCat) oldCat.tagCount = Math.max(0, (oldCat.tagCount || 0) - 1);
                                category.tagCount = (category.tagCount || 0) + 1;
                            }
                            category.updatedAt = now;
                            self.$message.success('标签已更新');
                        } else {
                            self.tagManager.tags.unshift({
                                id: 'tag-' + Date.now(),
                                name: name,
                                description: (form.description || '').trim(),
                                categoryId: categoryId,
                                categoryName: category.name,
                                status: '启用',
                                updatedAt: now,
                                updatedBy: '当前用户'
                            });
                            category.tagCount = (category.tagCount || 0) + 1;
                            category.updatedAt = now;
                            category.updatedBy = '当前用户';
                            self.$message.success('标签已创建');
                        }
                        self.saveTagManagerToStorage();
                        self.searchTags();
                        self.tagCreateDialog.visible = false;
                    } finally {
                        self.tagCreateDialog.loading = false;
                    }
                });
            },
            expandTagRows: function () {
                if (!this.tagManager) return;
                this.tagManager.treeExpanded = !this.tagManager.treeExpanded;
                this.tagManager.treeExpandKey = (this.tagManager.treeExpandKey || 0) + 1;
            },
            enableSelectedTags: function () { this.setSelectedTagStatus(true); },
            disableSelectedTags: function () { this.setSelectedTagStatus(false); },
            importTags: function () { this.$message.info('导入功能即将开放'); },
            exportTags: function () { this.$message.info('导出功能即将开放'); },
            openTagRowMenu: function (row) {
                if (!row) return;
                // 与行扩展一致：打开编辑
                this.editTagRow(row);
            },
            loadRecycleBin: function () {
                if (!this.recycleBin) return;
                this.recycleBin.displayList = this.recycleBin.list || [];
                this.recycleBin.total = this.recycleBin.displayList.length;
            },
            onRecycleSelectionChange: function (rows) {
                this.recycleBin.selectedIds = (rows || []).map(function (row) { return row.id; });
            },
            handleRecyclePageChange: function (page) {
                this.recycleBin.page = page;
                this.loadRecycleBin();
            },
            handleRecycleSizeChange: function (size) {
                this.recycleBin.size = size;
                this.recycleBin.page = 1;
                this.loadRecycleBin();
            },
            loadRecycleFromStorage: function () {
                if (!this.recycleBin) {
                    this.recycleBin = { list: [], displayList: [], selectedIds: [], page: 1, size: 20, total: 0 };
                }
                try {
                    const raw = localStorage.getItem(this.recycleStorageKey || 'ncf.fileManager.recycleBin');
                    const list = raw ? JSON.parse(raw) : [];
                    this.recycleBin.list = Array.isArray(list) ? list : [];
                } catch (e) {
                    this.recycleBin.list = [];
                }
                this.loadRecycleBin();
            },
            saveRecycleToStorage: function () {
                try {
                    localStorage.setItem(
                        this.recycleStorageKey || 'ncf.fileManager.recycleBin',
                        JSON.stringify(this.recycleBin.list || [])
                    );
                } catch (e) { /* ignore */ }
            },
            getRecycledIdSet: function () {
                const set = {};
                (this.recycleBin && this.recycleBin.list || []).forEach(function (item) {
                    if (item && item.id != null) set[item.id] = true;
                });
                return set;
            },
            filterOutRecycledFiles: function (rows) {
                const recycled = this.getRecycledIdSet();
                return (rows || []).filter(function (row) { return !recycled[row.id]; });
            },
            moveFileToRecycleBin: function (row) {
                if (!row || row.id == null) return;
                if (!this.recycleBin) {
                    this.recycleBin = { list: [], displayList: [], selectedIds: [], page: 1, size: 20, total: 0 };
                }
                const list = this.recycleBin.list || [];
                if (list.some(function (item) { return item.id === row.id; })) return;
                list.unshift({
                    id: row.id,
                    fileName: row.fileName,
                    fileSize: row.fileSize,
                    uploadTime: row.uploadTime,
                    updatedBy: typeof this.updatedByLabel === 'function' ? this.updatedByLabel(row) : '-',
                    folderPathText: typeof this.buildFavoritePathText === 'function' ? this.buildFavoritePathText() : '',
                    deletedAt: new Date().toISOString(),
                    resourceScope: this.resourceScope
                });
                this.recycleBin.list = list;
                this.saveRecycleToStorage();
                this.loadRecycleBin();

                // 从收藏中移除
                if (this.favoriteList && this.favoriteList.list) {
                    this.favoriteList.list = this.favoriteList.list.filter(function (item) { return item.id !== row.id; });
                    this.saveFavoritesToStorage();
                    this.searchFavorites();
                }

                // 从快速访问移除
                if (this.homePage && this.homePage.quickAccess) {
                    this.homePage.quickAccess = this.homePage.quickAccess.filter(function (item) { return item.id !== row.id; });
                    this.saveQuickAccessToStorage();
                }

                // 从当前列表移除
                this.tableData = (this.tableData || []).filter(function (item) { return item.id !== row.id; });
                if (this.total > 0) this.total -= 1;
            },
            emptyRecycleBin: function () {
                var list = (this.recycleBin && (this.recycleBin.list || this.recycleBin.displayList)) || [];
                if (!list.length) {
                    this.$message.info('回收站暂无文档');
                    return;
                }
                if (!this.recycleEmptyDialog) {
                    this.$set(this, 'recycleEmptyDialog', { visible: false });
                }
                this.$set(this.recycleBin, 'emptying', false);
                this.recycleEmptyDialog.visible = true;
            },
            cancelEmptyRecycleBin: function () {
                if (this.recycleBin && this.recycleBin.emptying) return;
                if (this.recycleEmptyDialog) this.recycleEmptyDialog.visible = false;
            },
            confirmEmptyRecycleBin: async function () {
                if (!this.recycleBin || (this.recycleBin && this.recycleBin.emptying)) return;

                var items = (this.recycleBin.list && this.recycleBin.list.length
                    ? this.recycleBin.list
                    : (this.recycleBin.displayList || [])).slice();
                if (!items.length) {
                    if (this.recycleEmptyDialog) this.recycleEmptyDialog.visible = false;
                    this.$message.info('回收站暂无文档');
                    return;
                }

                this.$set(this.recycleBin, 'emptying', true);
                try {
                    var ids = items.map(function (item) { return item.id; }).filter(function (id) { return id != null; });
                    var result = null;
                    try {
                        result = unwrap(await post(pageUrl + '?handler=EmptyRecycle', { ids: ids }));
                    } catch (batchError) {
                        // 后端未重编时回退为逐个 Delete
                        var remainingFallback = [];
                        var successFallback = 0;
                        var lastError = '';
                        for (var i = 0; i < items.length; i++) {
                            try {
                                await post(pageUrl + '?handler=Delete&id=' + encodeURIComponent(items[i].id));
                                successFallback += 1;
                            } catch (error) {
                                remainingFallback.push(items[i]);
                                lastError = errorMessage(error);
                            }
                        }
                        result = {
                            deletedCount: successFallback,
                            failedIds: remainingFallback.map(function (x) { return x.id; }),
                            message: remainingFallback.length === 0
                                ? '一键清空成功'
                                : ('已删除 ' + successFallback + ' 个，失败 ' + remainingFallback.length + ' 个' + (lastError ? '：' + lastError : ''))
                        };
                    }

                    var failedIds = (result && result.failedIds) || [];
                    var failedSet = {};
                    failedIds.forEach(function (id) { failedSet[id] = true; });

                    var remaining = items.filter(function (item) { return failedSet[item.id]; });
                    this.recycleBin.list = remaining;
                    this.recycleBin.displayList = remaining.slice();
                    this.recycleBin.total = remaining.length;
                    this.recycleBin.selectedIds = [];
                    this.saveRecycleToStorage();
                    this.loadRecycleBin();

                    if (this.recycleEmptyDialog) this.recycleEmptyDialog.visible = false;

                    if (remaining.length === 0) {
                        this.$message.success((result && result.message) || '一键清空成功');
                    } else if (remaining.length < items.length) {
                        this.$message.warning((result && result.message) || ('部分文件删除失败，仍有 ' + remaining.length + ' 个留在回收站'));
                    } else {
                        this.$message.error((result && result.message) || '一键清空失败，文件未能彻底删除');
                    }
                } catch (error) {
                    this.$message.error('一键清空失败：' + errorMessage(error));
                } finally {
                    this.$set(this.recycleBin, 'emptying', false);
                }
            },
            searchFavorites: function () {
                if (!this.favoriteList) return;
                const keyword = (this.favoriteList.keyword || '').trim().toLowerCase();
                const filtered = (this.favoriteList.list || []).filter(function (row) {
                    if (!keyword) return true;
                    return String(row.fileName || '').toLowerCase().indexOf(keyword) !== -1
                        || String(row.folderPathText || '').toLowerCase().indexOf(keyword) !== -1;
                });
                this.favoriteList.displayList = filtered;
                this.favoriteList.total = filtered.length;
            },
            handleFavoritePageChange: function (page) {
                this.favoriteList.page = page;
                this.searchFavorites();
            },
            handleFavoriteSizeChange: function (size) {
                this.favoriteList.size = size;
                this.favoriteList.page = 1;
                this.searchFavorites();
            },
            loadFavoritesFromStorage: function () {
                if (!this.favoriteList) {
                    this.favoriteList = { keyword: '', list: [], displayList: [], page: 1, size: 20, total: 0 };
                }
                try {
                    const raw = localStorage.getItem(this.favoriteStorageKey || 'ncf.fileManager.favorites');
                    const list = raw ? JSON.parse(raw) : [];
                    this.favoriteList.list = Array.isArray(list) ? list : [];
                } catch (e) {
                    this.favoriteList.list = [];
                }
                this.searchFavorites();
                this.syncFavoriteFlagsOnTable();
            },
            saveFavoritesToStorage: function () {
                try {
                    localStorage.setItem(
                        this.favoriteStorageKey || 'ncf.fileManager.favorites',
                        JSON.stringify(this.favoriteList.list || [])
                    );
                } catch (e) { /* ignore quota */ }
            },
            buildFavoritePathText: function () {
                const segments = [this.rootFolderName || '企业文档', this.orgName || ''].concat(
                    (this.folderPath || []).map(function (item) { return item.name; })
                ).filter(function (name) { return !!name; });
                return segments.join(' > ');
            },
            isFileFavorited: function (row) {
                if (!row || row.id == null || !this.favoriteList) return false;
                return (this.favoriteList.list || []).some(function (item) { return item.id === row.id; });
            },
            syncFavoriteFlagsOnTable: function () {
                const self = this;
                (this.tableData || []).forEach(function (row) {
                    self.$set(row, 'isFavorite', self.isFileFavorited(row));
                });
            },
            toggleFavorite: function (row) {
                if (!row || row.id == null) return;
                if (!this.favoriteList) {
                    this.favoriteList = { keyword: '', list: [], displayList: [], page: 1, size: 20, total: 0 };
                }
                const list = this.favoriteList.list || [];
                const index = list.findIndex(function (item) { return item.id === row.id; });
                if (index >= 0) {
                    list.splice(index, 1);
                    this.$set(row, 'isFavorite', false);
                    this.$message.success('已取消收藏');
                } else {
                    list.unshift({
                        id: row.id,
                        fileName: row.fileName,
                        fileSize: row.fileSize,
                        uploadTime: row.uploadTime,
                        updatedAt: row.uploadTime || row.updatedAt,
                        updatedBy: this.updatedByLabel(row),
                        folderPathText: this.buildFavoritePathText(),
                        resourceScope: this.resourceScope,
                        isFavorite: true
                    });
                    this.$set(row, 'isFavorite', true);
                    this.$message.success('已加入收藏');
                }
                this.favoriteList.list = list;
                this.saveFavoritesToStorage();
                this.searchFavorites();
                this.syncFavoriteFlagsOnTable();
            },
            searchHomeRecent: function () {
                if (!this.homePage) return;
                const keyword = (this.homePage.keyword || '').trim().toLowerCase();
                this.homePage.displayList = (this.homePage.list || []).filter(function (row) {
                    if (!keyword) return true;
                    return String(row.fileName || '').toLowerCase().indexOf(keyword) !== -1;
                });
            },
            restoreRouteState: function () {
                const query = new URLSearchParams(window.location.search);
                const scope = Number(query.get('scope'));
                const folderId = Number(query.get('folderId'));
                if (scope === 100 || scope === 200) this.resourceScope = scope;
                this.currentFolderId = Number.isInteger(folderId) && folderId > 0 ? folderId : null;
            },
            syncRouteState: function () {
                const url = new URL(window.location.href);
                url.searchParams.set('scope', String(this.resourceScope));
                if (this.currentFolderId == null) url.searchParams.delete('folderId');
                else url.searchParams.set('folderId', String(this.currentFolderId));
                window.history.replaceState({}, '', url.toString());
            },
            dateFormatter: function (date) { return date ? new Date(date).toLocaleString() : ''; },
            formatFileSize: function (size) {
                const units = ['B', 'KB', 'MB', 'GB'];
                let index = 0;
                let value = Number(size || 0);
                while (value >= 1024 && index < units.length - 1) { value /= 1024; index++; }
                return value.toFixed(index === 0 ? 0 : 2) + ' ' + units[index];
            },
            fileTypeLabel: function (fileType) {
                return ({ 0: '文本', 1: 'Word', 2: 'PowerPoint', 3: 'Excel', 4: '代码', 999: '其他' })[fileType] || '其他';
            },
            updatedByLabel: function (row) { return '-'; },
            filterFolderNode: function (value, data) {
                if (!value) return true;
                return (data.name || '').toLowerCase().indexOf(value.toLowerCase()) !== -1;
            },
            handleCreateCommand: function (command) {
                if (command === 'folder') this.showCreateFolderDialog();
            },
            handleUploadCommand: function (command) {
                if (command === 'files') {
                    this.showUploadDialog();
                    return;
                }
                if (command === 'folder') {
                    this.showUploadDialog();
                    this.$nextTick(function () { this.chooseUploadFolder(); }.bind(this));
                }
            },
            handleUploadPickerCommand: function (command) {
                if (command === 'files') {
                    this.triggerUploadFilePicker();
                    return;
                }
                if (command === 'folder') this.chooseUploadFolder();
            },
            triggerUploadFilePicker: function () {
                const upload = this.$refs.upload;
                if (!upload) return;
                if (upload.$refs && upload.$refs['upload-inner'] && typeof upload.$refs['upload-inner'].handleClick === 'function') {
                    upload.$refs['upload-inner'].handleClick();
                    return;
                }
                const input = upload.$el && upload.$el.querySelector('input[type="file"]');
                if (input) input.click();
            },
            clearUploadList: function () {
                this.uploadDialog.fileList = [];
                this.uploadDialog.mode = 'files';
                this.uploadDialog.folderRootName = '';
                this.uploadDialog.progress = 0;
                if (this.$refs.upload) this.$refs.upload.clearFiles();
            },
            getUploadDialogTitle: function () {
                const segments = [this.rootFolderName || '根目录'].concat((this.folderPath || []).map(function (item) { return item.name; }));
                return '上传文件到 【/' + segments.join('/') + '】 --支持断点续传';
            },
            handleRowCommand: function (command, row) {
                if (command === 'download') return this.downloadFile(row);
                if (command === 'togglePublish') return this.setPublication(row, row.accessLevel !== 100);
                if (command === 'copyUrl') return this.copyPublicUrl(row);
                if (command === 'editNote') return this.showNoteDialog(row);
                if (command === 'toggleQuickAccess') return this.toggleQuickAccess(row);
                if (command === 'manageTags') return this.showFileTagDialog(row);
                if (command === 'delete') return this.deleteFile(row);
            },
            isInQuickAccess: function (row) {
                if (!row || row.id == null || !this.homePage) return false;
                return (this.homePage.quickAccess || []).some(function (item) { return item.id === row.id; });
            },
            loadQuickAccessFromStorage: function () {
                if (!this.homePage) {
                    this.homePage = { keyword: '', list: [], displayList: [], quickAccess: [] };
                }
                try {
                    const raw = localStorage.getItem(this.quickAccessStorageKey || 'ncf.fileManager.quickAccess');
                    const list = raw ? JSON.parse(raw) : [];
                    this.$set(this.homePage, 'quickAccess', Array.isArray(list) ? list : []);
                } catch (e) {
                    this.$set(this.homePage, 'quickAccess', []);
                }
            },
            saveQuickAccessToStorage: function () {
                try {
                    localStorage.setItem(
                        this.quickAccessStorageKey || 'ncf.fileManager.quickAccess',
                        JSON.stringify((this.homePage && this.homePage.quickAccess) || [])
                    );
                } catch (e) { /* ignore */ }
            },
            toggleQuickAccess: function (row) {
                if (!row || row.id == null) return;
                if (!this.homePage) {
                    this.$set(this, 'homePage', { keyword: '', list: [], displayList: [], quickAccess: [] });
                }
                if (!Array.isArray(this.homePage.quickAccess)) {
                    this.$set(this.homePage, 'quickAccess', []);
                }
                const list = this.homePage.quickAccess;
                const index = list.findIndex(function (item) { return item.id === row.id; });
                if (index >= 0) {
                    list.splice(index, 1);
                    this.$message.success('已从快速访问移除');
                } else {
                    list.unshift({
                        id: row.id,
                        fileName: row.fileName,
                        fileSize: row.fileSize,
                        uploadTime: row.uploadTime,
                        folderPathText: typeof this.buildFavoritePathText === 'function' ? this.buildFavoritePathText() : '',
                        resourceScope: this.resourceScope
                    });
                    this.$message.success('已添加至快速访问');
                }
                this.saveQuickAccessToStorage();
            },
            handleQuickAccessCommand: function (command, item) {
                if (command === 'remove') this.toggleQuickAccess(item);
            },
            getDefaultUploadSettings: function () {
                return {
                    allowedTypesText: [
                        '.txt', '.log', '.md', '.markdown', '.csv', '.tsv', '.json', '.xml',
                        '.yaml', '.yml', '.html', '.htm', '.css', '.js', '.ts', '.cs', '.sql',
                        '.docx', '.xlsx', '.pptx',
                        '.jpg', '.jpeg', '.png', '.gif', '.webp', '.avif', '.ico',
                        '.mp3', '.wav', '.ogg', '.mp4', '.webm',
                        '.woff', '.woff2', '.ttf', '.otf', '.pdf'
                    ].join('\n'),
                    maxSizeMb: 50,
                    blockedTypesText: [
                        '.exe', '.bat', '.cmd', '.com', '.msi', '.scr', '.ps1', '.vbs',
                        '.dll', '.sys', '.apk', '.ipa'
                    ].join('\n')
                };
            },
            parseExtensionList: function (text) {
                const result = [];
                const seen = {};
                String(text || '').split(/[\n,;，；\s]+/).forEach(function (part) {
                    var ext = String(part || '').trim().toLowerCase();
                    if (!ext) return;
                    if (ext.charAt(0) !== '.') ext = '.' + ext;
                    if (seen[ext]) return;
                    seen[ext] = true;
                    result.push(ext);
                });
                return result;
            },
            loadUploadSettingsFromStorage: function () {
                const defaults = this.getDefaultUploadSettings();
                try {
                    const raw = localStorage.getItem(this.uploadSettingsStorageKey || 'ncf.fileManager.uploadSettings');
                    const saved = raw ? JSON.parse(raw) : null;
                    this.uploadSettings = {
                        allowedTypesText: saved && saved.allowedTypesText != null ? saved.allowedTypesText : defaults.allowedTypesText,
                        maxSizeMb: saved && saved.maxSizeMb != null ? Number(saved.maxSizeMb) || defaults.maxSizeMb : defaults.maxSizeMb,
                        blockedTypesText: saved && saved.blockedTypesText != null ? saved.blockedTypesText : defaults.blockedTypesText
                    };
                } catch (e) {
                    this.uploadSettings = defaults;
                }
            },
            saveUploadSettings: function () {
                if (!this.uploadSettings) this.uploadSettings = this.getDefaultUploadSettings();
                let maxSizeMb = Number(this.uploadSettings.maxSizeMb);
                if (!maxSizeMb || maxSizeMb < 1) maxSizeMb = 1;
                if (maxSizeMb > 512) maxSizeMb = 512;
                this.uploadSettings.maxSizeMb = maxSizeMb;
                try {
                    localStorage.setItem(
                        this.uploadSettingsStorageKey || 'ncf.fileManager.uploadSettings',
                        JSON.stringify({
                            allowedTypesText: this.uploadSettings.allowedTypesText || '',
                            maxSizeMb: maxSizeMb,
                            blockedTypesText: this.uploadSettings.blockedTypesText || ''
                        })
                    );
                    this.$message.success('上传设置已保存');
                } catch (e) {
                    this.$message.error('保存失败，请检查浏览器本地存储是否可用');
                }
            },
            resetUploadSettings: function () {
                this.uploadSettings = this.getDefaultUploadSettings();
                try {
                    localStorage.setItem(
                        this.uploadSettingsStorageKey || 'ncf.fileManager.uploadSettings',
                        JSON.stringify({
                            allowedTypesText: this.uploadSettings.allowedTypesText || '',
                            maxSizeMb: this.uploadSettings.maxSizeMb,
                            blockedTypesText: this.uploadSettings.blockedTypesText || ''
                        })
                    );
                    this.$message.success('已恢复默认上传设置');
                } catch (e) {
                    this.$message.error('恢复默认失败');
                }
            },
            getMaxUploadSizeBytes: function () {
                const mb = (this.uploadSettings && Number(this.uploadSettings.maxSizeMb)) || 50;
                return Math.max(1, mb) * 1024 * 1024;
            },
            validateUploadFile: function (fileName, fileSize) {
                const name = fileName || '';
                const extMatch = /\.[^.\\/]+$/.exec(name);
                const ext = extMatch ? extMatch[0].toLowerCase() : '';
                const blocked = this.parseExtensionList((this.uploadSettings && this.uploadSettings.blockedTypesText) || '');
                if (ext && blocked.indexOf(ext) !== -1) {
                    return '文件“' + name + '”类型 ' + ext + ' 已被过滤，无法上传。';
                }
                const allowed = this.parseExtensionList((this.uploadSettings && this.uploadSettings.allowedTypesText) || '');
                if (allowed.length && (!ext || allowed.indexOf(ext) === -1)) {
                    return '文件“' + name + '”类型不在允许上传列表中。';
                }
                const maxBytes = this.getMaxUploadSizeBytes();
                if (fileSize > maxBytes) {
                    return '文件“' + name + '”超过 ' + Math.round(maxBytes / 1024 / 1024) + ' MB，无法上传。';
                }
                return '';
            },
            showFileTagDialog: function (row) {
                const existing = Array.isArray(row.tags) ? row.tags.slice() : [];
                this.fileTagDialog = {
                    visible: true,
                    loading: false,
                    adding: false,
                    draft: '',
                    row: row,
                    tags: existing
                };
            },
            startAddFileTag: function () {
                this.fileTagDialog.adding = true;
                this.fileTagDialog.draft = '';
                const self = this;
                this.$nextTick(function () {
                    const input = self.$refs.fileTagInput;
                    if (input && typeof input.focus === 'function') input.focus();
                });
            },
            confirmAddFileTag: function () {
                if (!this.fileTagDialog.adding) return;
                const name = (this.fileTagDialog.draft || '').trim();
                this.fileTagDialog.adding = false;
                this.fileTagDialog.draft = '';
                if (!name) return;
                if (this.fileTagDialog.tags.indexOf(name) !== -1) {
                    this.$message.warning('标签已存在');
                    return;
                }
                this.fileTagDialog.tags.push(name);
            },
            removeFileTag: function (index) {
                this.fileTagDialog.tags.splice(index, 1);
            },
            onFileTagDialogClosed: function () {
                this.fileTagDialog.adding = false;
                this.fileTagDialog.draft = '';
                this.fileTagDialog.row = null;
                this.fileTagDialog.tags = [];
            },
            submitFileTags: function () {
                const row = this.fileTagDialog.row;
                if (!row) return;
                this.fileTagDialog.loading = true;
                try {
                    if (this.fileTagDialog.adding) this.confirmAddFileTag();
                    this.$set(row, 'tags', this.fileTagDialog.tags.slice());
                    this.fileTagDialog.visible = false;
                    this.$message.success('标签已更新');
                } finally {
                    this.fileTagDialog.loading = false;
                }
            },
            enterParentFolder: function () {
                if (this.folderPath.length > 1) {
                    this.enterFolder(this.folderPath[this.folderPath.length - 2].id);
                    return;
                }
                this.enterFolder(null);
            },
            showNoteDialog: function (row) {
                this.noteDialog = { visible: true, loading: false, row: row, note: row.description || '' };
            },
            submitNote: async function () {
                const row = this.noteDialog.row;
                if (!row) return;
                this.noteDialog.loading = true;
                try {
                    row.description = this.noteDialog.note || '';
                    await this.handleNoteChange(row);
                    this.noteDialog.visible = false;
                } finally {
                    this.noteDialog.loading = false;
                }
            },
            getList: async function () {
                this.tableLoading = true;
                try {
                    let url = pageUrl + '?handler=List&page=' + this.page.page + '&pageSize=' + this.page.size + '&resourceScope=' + this.resourceScope;
                    if (this.currentFolderId != null) url += '&folderId=' + encodeURIComponent(this.currentFolderId);
                    const result = unwrap(await axios.get(url));
                    // PagedList used to serialize as a bare array (TotalCount lost).
                    // Prefer explicit { items, totalCount }; still accept legacy shapes.
                    if (Array.isArray(result)) {
                        this.tableData = this.filterOutRecycledFiles(result);
                        this.total = result.length;
                    } else {
                        const items = (result && (result.items || result.data)) || [];
                        this.tableData = this.filterOutRecycledFiles(items);
                        this.total = (result && (result.totalCount != null ? result.totalCount : result.total)) || 0;
                    }
                    this.syncFavoriteFlagsOnTable();
                } catch (error) {
                    this.$message.error('获取文件列表失败：' + errorMessage(error));
                } finally {
                    this.tableLoading = false;
                }
            },
            loadFolderChildren: async function (node, resolve) {
                try {
                    const parentId = node.level === 0 ? null : node.data.id;
                    let url = pageUrl + '?handler=Folders&resourceScope=' + this.resourceScope;
                    if (parentId != null) url += '&parentId=' + encodeURIComponent(parentId);
                    const list = unwrap(await axios.get(url)) || [];
                    resolve(list.map(function (folder) {
                        return { id: folder.id, name: folder.name, description: folder.description || '', parentId: folder.parentId, hasChildren: true };
                    }));
                } catch (error) {
                    this.$message.error('获取文件夹失败：' + errorMessage(error));
                    resolve([]);
                }
            },
            changeResourceScope: async function () {
                this.currentFolderId = null;
                this.folderPath = [];
                this.page.page = 1;
                this.treeFilter = '';
                this.fileSearchKeyword = '';
                this.syncActiveNavFromScope();
                this.reloadFolderTree();
                await this.enterFolder(null);
            },
            onFolderNodeClick: function (data) { this.enterFolder(data.id); },
            enterFolder: async function (id) {
                this.currentFolderId = id == null ? null : Number(id);
                this.page.page = 1;
                if (this.currentFolderId != null) {
                    try {
                        await this.loadFolderPath();
                    } catch (error) {
                        this.currentFolderId = null;
                        this.folderPath = [];
                        this.$message.warning('目标文件夹不可用，已回到根目录。');
                    }
                } else {
                    this.folderPath = [];
                }
                await this.getList();
                this.syncRouteState();
            },
            loadFolderPath: async function () {
                if (this.currentFolderId == null) {
                    this.folderPath = [];
                    return;
                }
                const url = pageUrl + '?handler=FolderPath&folderId=' + encodeURIComponent(this.currentFolderId) + '&resourceScope=' + this.resourceScope;
                const folders = unwrap(await axios.get(url));
                if (!Array.isArray(folders) || folders.length === 0) throw new Error('Folder path not found');
                this.folderPath = folders.map(function (folder) { return { id: folder.id, name: folder.name }; });
            },
            reloadFolderTree: function () {
                this.folderTree = [];
                this.folderTreeKey += 1;
            },
            handleCurrentChange: function (value) { this.page.page = value; this.getList(); },
            handleSizeChange: function (value) { this.page.size = value; this.page.page = 1; this.getList(); },
            handleNoteChange: async function (row) {
                try {
                    await post(pageUrl + '?handler=EditNote', { id: row.id, note: row.description || '' });
                    this.$message.success('备注已更新');
                } catch (error) {
                    this.$message.error('更新备注失败：' + errorMessage(error));
                }
            },
            downloadFile: function (row) { window.location.assign(pageUrl + '?handler=Download&id=' + encodeURIComponent(row.id)); },
            deleteFile: async function (row) {
                try {
                    const shortName = String(row.fileName || '').replace(/\.[^.]+$/, '') || row.fileName;
                    await this.$confirm(
                        '此操作将文件放至回收站，是否继续？',
                        '是否删除: ' + shortName,
                        { type: 'warning', confirmButtonText: '确定', cancelButtonText: '取消' }
                    );
                    this.moveFileToRecycleBin(row);
                    this.$message.success('已移至回收站');
                } catch (error) {
                    if (error !== 'cancel') this.$message.error('删除失败：' + errorMessage(error));
                }
            },
            setPublication: async function (row, publish) {
                try {
                    await post(pageUrl + '?handler=SetSiteAssetPublication', { id: row.id, publish: publish });
                    this.$message.success(publish ? '资源已公开' : '资源已设为私有');
                    await this.getList();
                } catch (error) {
                    this.$message.error('更新发布状态失败：' + errorMessage(error));
                }
            },
            copyPublicUrl: async function (row) {
                const url = window.location.origin + row.publicUrl;
                try {
                    await navigator.clipboard.writeText(url);
                    this.$message.success('公开 URL 已复制');
                } catch (_) {
                    this.$prompt('请复制以下公开 URL', '公开 URL', { inputValue: url, inputType: 'textarea' });
                }
            },
            resetUploadDialog: function (visible) {
                this.uploadDialog = { visible: visible, fileList: [], uploading: false, progress: 0, mode: 'files', folderRootName: '' };
                if (this.$refs.upload) this.$refs.upload.clearFiles();
            },
            showUploadDialog: function () { this.resetUploadDialog(true); },
            handleFileChange: function (file, fileList) {
                if (file.description === undefined) this.$set(file, 'description', '');
                this.uploadDialog.fileList = fileList;
                if (this.uploadDialog.mode !== 'folder') {
                    this.uploadDialog.mode = 'files';
                    this.uploadDialog.folderRootName = '';
                }
            },
            cancelUpload: function () {
                if (this.uploadDialog.uploading) return;
                this.resetUploadDialog(false);
            },
            chooseUploadFolder: function () {
                const input = this.$refs.folderUploadInput;
                if (!input || !('webkitdirectory' in input)) {
                    this.$message.warning('当前浏览器不支持选择文件夹，请使用最新版 Chrome、Edge 或 Safari。');
                    return;
                }
                input.click();
            },
            handleFolderSelection: function (event) {
                const rawFiles = Array.prototype.slice.call((event.target && event.target.files) || []);
                if (!rawFiles.length) return;

                const paths = rawFiles.map(function (file) { return file.webkitRelativePath || ''; });
                const rootNames = Array.from(new Set(paths.map(function (path) { return path.split('/')[0]; }).filter(Boolean)));
                if (paths.some(function (path) { return !path; }) || rootNames.length !== 1) {
                    this.$message.error('浏览器未提供完整的文件夹相对路径，请重新选择一个文件夹。');
                    return;
                }

                if (this.$refs.upload) this.$refs.upload.clearFiles();
                this.uploadDialog.fileList = rawFiles.map(function (rawFile, index) {
                    return {
                        uid: 'folder-' + Date.now() + '-' + index,
                        name: rawFile.name,
                        size: rawFile.size,
                        status: 'ready',
                        raw: rawFile,
                        relativePath: rawFile.webkitRelativePath,
                        description: ''
                    };
                });
                this.uploadDialog.mode = 'folder';
                this.uploadDialog.folderRootName = rootNames[0];
                event.target.value = '';
            },
            beforeUpload: function () { return false; },
            createUploadBatches: function (fileList) {
                const self = this;
                const maxBytes = typeof this.getMaxUploadSizeBytes === 'function'
                    ? this.getMaxUploadSizeBytes()
                    : maxFileSizeBytes;
                const maxMb = Math.round(maxBytes / 1024 / 1024);
                const batches = [];
                let batch = [];
                let batchBytes = 0;
                fileList.forEach(function (file) {
                    const rawFile = file.raw || file;
                    if (!rawFile || !rawFile.size) throw new Error('存在无法读取的文件，请重新选择。');
                    if (typeof self.validateUploadFile === 'function') {
                        const reason = self.validateUploadFile(file.name || rawFile.name, rawFile.size);
                        if (reason) throw new Error(reason);
                    } else if (rawFile.size > maxBytes) {
                        throw new Error('文件“' + file.name + '”超过 ' + maxMb + ' MB，无法上传。');
                    }
                    if (batch.length && (batch.length >= maxFilesPerUpload || batchBytes + rawFile.size > maxTotalUploadBytes)) {
                        batches.push(batch);
                        batch = [];
                        batchBytes = 0;
                    }
                    batch.push(file);
                    batchBytes += rawFile.size;
                });
                if (batch.length) batches.push(batch);
                return batches;
            },
            submitUpload: async function () {
                if (!this.uploadDialog.fileList.length) { this.$message.warning('请选择要上传的文件'); return; }
                this.uploadDialog.uploading = true;
                this.uploadDialog.progress = 0;
                let uploadedCount = 0;
                try {
                    const files = this.uploadDialog.fileList.slice();
                    const batches = this.createUploadBatches(files);
                    for (let batchIndex = 0; batchIndex < batches.length; batchIndex++) {
                        const formData = new FormData();
                        batches[batchIndex].forEach(function (file) {
                            const rawFile = file.raw || file;
                            formData.append('files', rawFile);
                            formData.append('descriptions', file.description || '');
                            formData.append('relativePaths', file.relativePath || rawFile.webkitRelativePath || '');
                        });
                        formData.append('resourceScope', this.resourceScope);
                        if (this.currentFolderId != null) formData.append('folderId', this.currentFolderId);
                        // 同时放入表单和请求头，兼容 Razor Pages 的 multipart 防伪验证。
                        formData.append('__RequestVerificationToken', getRequestVerificationToken());
                        await post(pageUrl + '?handler=Upload', formData);
                        uploadedCount += batches[batchIndex].length;
                        this.uploadDialog.progress = Math.round(uploadedCount * 100 / files.length);
                    }
                    this.$message.success(this.uploadDialog.mode === 'folder'
                        ? '文件夹上传完成，已保留原始目录结构。'
                        : '上传成功；站点静态资源仍需显式公开后才可被站点引用。');
                    this.uploadDialog.visible = false;
                    this.reloadFolderTree();
                    await this.getList();
                } catch (error) {
                    const partialMessage = uploadedCount > 0 ? '，已完成 ' + uploadedCount + ' 个文件' : '';
                    this.$message.error('上传失败' + partialMessage + '：' + errorMessage(error));
                } finally {
                    this.uploadDialog.uploading = false;
                }
            },
            showCreateFolderDialog: function () {
                this.folderDialog = { visible: true, loading: false, editing: false, form: { id: null, name: '', description: '' } };
            },
            showEditFolderDialog: function (folder) {
                this.folderDialog = { visible: true, loading: false, editing: true, form: { id: folder.id, name: folder.name, description: folder.description || '' } };
            },
            submitFolder: async function () {
                const form = this.folderDialog.form;
                if (!form.name || !form.name.trim()) { this.$message.warning('请输入文件夹名称'); return; }
                this.folderDialog.loading = true;
                try {
                    if (this.folderDialog.editing) {
                        await post(pageUrl + '?handler=UpdateFolder', { id: form.id, name: form.name, description: form.description || '' });
                    } else {
                        await post(pageUrl + '?handler=CreateFolder', {
                            name: form.name,
                            description: form.description || '',
                            parentId: this.currentFolderId,
                            resourceScope: this.resourceScope
                        });
                    }
                    this.$message.success(this.folderDialog.editing ? '文件夹已更新' : '文件夹已创建');
                    this.folderDialog.visible = false;
                    this.reloadFolderTree();
                } catch (error) {
                    this.$message.error('保存文件夹失败：' + errorMessage(error));
                } finally {
                    this.folderDialog.loading = false;
                }
            },
            deleteFolder: async function (folder) {
                try {
                    await this.$confirm('仅空文件夹可以删除。确认删除“' + folder.name + '”吗？', '确认删除', { type: 'warning' });
                    await post(pageUrl + '?handler=DeleteFolder&id=' + encodeURIComponent(folder.id));
                    if (this.currentFolderId === folder.id) await this.enterFolder(null);
                    this.reloadFolderTree();
                    this.$message.success('文件夹已删除');
                } catch (error) {
                    if (error !== 'cancel') this.$message.error('删除文件夹失败：' + errorMessage(error));
                }
            }
        }
    });
})();

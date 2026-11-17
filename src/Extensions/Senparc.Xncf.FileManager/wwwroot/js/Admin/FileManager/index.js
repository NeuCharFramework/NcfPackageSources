(function () {
    const pageUrl = window.location.pathname || '/Admin/FileManager/Index';
    const maxFilesPerUpload = 20;
    const maxTotalUploadBytes = 100 * 1024 * 1024;
    const maxFileSizeBytes = 50 * 1024 * 1024;

    function fileManagerText(key, fallback) {
        const args = Array.prototype.slice.call(arguments, 2);
        const value = typeof window.ncfST === 'function' ? window.ncfST(key) : fallback;
        return value.replace(/\{(\d+)\}/g, function (token, index) {
            return typeof args[index] === 'undefined' ? token : args[index];
        });
    }

    function resolveSystemName() {
        const name = (window.__NCF_SYSTEM_NAME__ || '').toString().trim();
        return name || 'NCF';
    }

    function unwrap(response) {
        return response && response.data && response.data.data !== undefined ? response.data.data : response.data;
    }

    function errorMessage(error) {
        const data = error && error.response && error.response.data;
        const message = typeof data === 'string' ? data : (data && (data.errorMessage || data.message || data.title));
        if (message) return message;
        if (error && error.response && error.response.status === 400) {
            return fileManagerText('Script.RequestRejected', 'The server rejected the request. Refresh the page and try again.');
        }
        return (error && error.message) || fileManagerText('Script.RequestFailed', 'Request failed.');
    }

    function getRequestVerificationToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!tokenInput || !tokenInput.value) {
            throw new Error(fileManagerText('Script.CsrfMissing', 'The page anti-forgery token is missing. Refresh the page and try again.'));
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
                fileTagDialog: {
                    visible: false,
                    loading: false,
                    row: null,
                    selectedIds: []
                },
                fileTagStorageKey: 'ncf.fileManager.fileTags',
                fileTagMap: {},
                filePreviewDialog: {
                    visible: false,
                    loading: false,
                    fileName: '',
                    mode: '',
                    url: '',
                    content: '',
                    html: '',
                    slides: [],
                    error: '',
                    row: null
                },
                guideDialogVisible: false,
                treeFilter: '',
                fileSearchKeyword: '',
                orgName: resolveSystemName(),
                rootFolderName: fileManagerText('Frontend.072', 'Enterprise documents'),
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
                        { name: resolveSystemName(), size: '0B' },
                        { name: fileManagerText('Frontend.072', 'Enterprise documents') + ' root', size: '0B' },
                        { name: fileManagerText('Frontend.149', 'Knowledge-base materials'), size: '0B' }
                    ],
                    sizeTotalLabel: '0B',
                    countTotalLabel: '0',
                    sizeSlices: [
                        { name: fileManagerText('Frontend.206', 'Documents'), value: 0, color: '#95de64' },
                        { name: fileManagerText('Frontend.207', 'Images'), value: 0, color: '#597ef7' },
                        { name: fileManagerText('Frontend.208', 'Videos'), value: 0, color: '#ffd666' },
                        { name: fileManagerText('Frontend.209', 'Audio'), value: 0, color: '#ff9c6e' },
                        { name: fileManagerText('Frontend.210', 'Other'), value: 0, color: '#91d5ff' }
                    ],
                    countSlices: [
                        { name: fileManagerText('Frontend.206', 'Documents'), value: 0, color: '#95de64' },
                        { name: fileManagerText('Frontend.207', 'Images'), value: 0, color: '#597ef7' },
                        { name: fileManagerText('Frontend.208', 'Videos'), value: 0, color: '#ffd666' },
                        { name: fileManagerText('Frontend.209', 'Audio'), value: 0, color: '#ff9c6e' },
                        { name: fileManagerText('Frontend.210', 'Other'), value: 0, color: '#91d5ff' }
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
                tagAdminOptions: [fileManagerText('Frontend.153', 'Administrator'), fileManagerText('Frontend.226', 'Current user')],
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
                        { required: true, message: fileManagerText('Frontend.161', 'Enter a category name, up to 50 characters'), trigger: 'blur' },
                        { max: 50, message: fileManagerText('Frontend.161', 'Enter a category name, up to 50 characters'), trigger: 'blur' }
                    ]
                },
                tagCreateRules: {
                    name: [
                        { required: true, message: fileManagerText('Frontend.163', 'Enter a tag name, up to 30 characters'), trigger: 'blur' },
                        { max: 30, message: fileManagerText('Frontend.163', 'Enter a tag name, up to 30 characters'), trigger: 'blur' }
                    ],
                    categoryId: [
                        { required: true, message: fileManagerText('Frontend.171', 'Select category'), trigger: 'change' }
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
                    { key: 'home', label: fileManagerText('Frontend.029', 'Home'), icon: 'el-icon-s-home' },
                    { key: 'favorite', label: fileManagerText('Frontend.119', 'Favorite'), icon: 'el-icon-star-off' },
                    { key: 'enterprise', label: fileManagerText('Frontend.072', 'Enterprise documents'), icon: 'el-icon-folder', scope: 100 },
                    { key: 'fine-tuning', label: fileManagerText('Frontend.106', 'Fine-tuning artifacts'), icon: 'el-icon-s-data', scope: 300 },
                    { key: 'recycle', label: fileManagerText('Frontend.096', 'Recycle bin'), icon: 'el-icon-delete' },
                    { key: 'divider', label: '', divider: true },
                    { key: 'dashboard', label: fileManagerText('Frontend.120', 'Data dashboard'), icon: 'el-icon-data-line' },
                    { key: 'tags', label: fileManagerText('Frontend.143', 'Tag Management'), icon: 'el-icon-price-tag' },
                    { key: 'settings', label: fileManagerText('Frontend.157', 'Settings'), icon: 'el-icon-setting' }
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
            isFineTuning: function () { return this.resourceScope === 300; },
            resourceScopeName: function () {
                if (this.isFineTuning) return fileManagerText('Frontend.106', 'Fine-tuning artifacts');
                return this.isSiteAsset
                    ? fileManagerText('Frontend.152', 'Site resources')
                    : fileManagerText('Frontend.149', 'Knowledge-base materials');
            },
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
                if (this.isFineTuning) {
                    return fileManagerText('Frontend.193', 'Fine-tuning artifacts are always private.');
                }
                return this.isSiteAsset
                    ? fileManagerText('Frontend.194', 'Site static resources are private by default.')
                    : fileManagerText('Frontend.195', 'Knowledge-base source files must be safely extractable text or Office Open XML files.');
            },
            uploadExtensionHint: function () {
                if (this.uploadSettings && this.uploadSettings.allowedTypesText) {
                    const list = this.parseExtensionList(this.uploadSettings.allowedTypesText);
                    if (list.length) {
                        return fileManagerText('Frontend.196', 'Supports {0}.', list.map(function (ext) { return ext.replace(/^\./, '').toUpperCase(); }).join(', '));
                    }
                }
                if (this.isFineTuning) {
                    return fileManagerText('Frontend.197', 'Supports JSONL, JSON, ZIP and other private artifacts generated by training.');
                }
                return this.isSiteAsset
                    ? fileManagerText('Frontend.198', 'Supports JPG, PNG, GIF, WebP, AVIF, ICO, audio/video and font formats.')
                    : fileManagerText('Frontend.199', 'Supports text, JSON/XML/YAML, code, DOCX, XLSX and PPTX.');
            },
            uploadTargetText: function () {
                const currentFolder = this.folderPath.length ? this.folderPath[this.folderPath.length - 1].name : this.rootFolderName;
                return this.resourceScopeName + ' / ' + currentFolder;
            },
            uploadFolderSummary: function () {
                if (this.uploadDialog.mode !== 'folder' || !this.uploadDialog.fileList.length) return '';
                return fileManagerText('Frontend.219', 'Selected folder “{0}” with {1} files; the original directory structure will be restored in the left file tree after upload.', this.uploadDialog.folderRootName, this.uploadDialog.fileList.length);
            },
            tagCategorySelectOptions: function () {
                const list = (this.tagManager && this.tagManager.list) || [];
                return list.map(function (row) {
                    return { label: row.name, value: row.id };
                });
            },
            fileTagCatalogOptions: function () {
                return (this.tagManager && this.tagManager.tags) || [];
            },
            fileTagCatalogGroups: function () {
                const categories = (this.tagManager && this.tagManager.list) || [];
                const tags = (this.tagManager && this.tagManager.tags) || [];
                const groups = categories.map(function (cat) {
                    return {
                        id: cat.id,
                        name: cat.name,
                        tags: tags.filter(function (tag) { return tag.categoryId === cat.id; })
                    };
                }).filter(function (group) { return group.tags.length; });
                const known = {};
                categories.forEach(function (cat) { known[cat.id] = true; });
                const orphanTags = tags.filter(function (tag) { return !tag.categoryId || !known[tag.categoryId]; });
                if (orphanTags.length) {
                    groups.push({ id: '__other__', name: fileManagerText('Frontend.220', 'Uncategorized'), tags: orphanTags });
                }
                return groups;
            },
            selectedFileTagItems: function () {
                const ids = (this.fileTagDialog && this.fileTagDialog.selectedIds) || [];
                const catalog = this.fileTagCatalogOptions || [];
                const map = {};
                catalog.forEach(function (tag) { map[tag.id] = tag; });
                const saved = (this.fileTagDialog && this.fileTagDialog.row && this.fileTagDialog.row.tagItems) || [];
                saved.forEach(function (tag) {
                    if (tag && tag.id && !map[tag.id]) map[tag.id] = tag;
                });
                return ids.map(function (id) {
                    return map[id] || { id: id, name: id };
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
            this.loadFileTagMapFromStorage();
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
                if (item.scope === 100 || item.scope === 200 || item.scope === 300) {
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
                this.$message.info(fileManagerText('Frontend.221', 'The “{0}” feature is coming soon.', item.label));
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
                const systemName = resolveSystemName();
                this.orgName = systemName;
                if (!this.dashboard.orgUsages.some(function (x) { return x.name === systemName; })) {
                    this.dashboard.orgUsages.unshift({ name: systemName, size: this.dashboard.enterpriseUsed });
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
                    if (result.systemName) {
                        this.orgName = result.systemName;
                        window.__NCF_SYSTEM_NAME__ = result.systemName;
                    }
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
                const fileCountLabel = fileManagerText('Frontend.121', 'File count');
                const fileSizeLabel = fileManagerText('Frontend.123', 'File size');

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
                                    var unit = p.seriesName === fileCountLabel ? '' : ' MB';
                                    lines.push(p.marker + p.seriesName + '：' + p.data + unit);
                                });
                                return lines.join('<br/>');
                            }
                        },
                        legend: { bottom: 0, data: [fileCountLabel, fileSizeLabel] },
                        grid: { left: 48, right: 56, top: 28, bottom: 48 },
                        xAxis: { type: 'category', boundaryGap: false, data: labels },
                        yAxis: [
                            {
                                type: 'value',
                                name: fileCountLabel,
                                minInterval: 1,
                                axisLabel: { formatter: '{value}' },
                                splitLine: { lineStyle: { type: 'dashed' } }
                            },
                            {
                                type: 'value',
                                name: fileSizeLabel,
                                axisLabel: { formatter: '{value}MB' },
                                splitLine: { show: false }
                            }
                        ],
                        series: [
                            {
                                name: fileCountLabel,
                                type: 'line',
                                smooth: true,
                                yAxisIndex: 0,
                                data: fileCounts,
                                areaStyle: { opacity: 0.08 }
                            },
                            {
                                name: fileSizeLabel,
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
                        tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
                        legend: { bottom: 0, data: (this.dashboard.countSlices || []).map(function (x) { return x.name; }) },
                        series: [{
                            type: 'pie',
                            radius: ['48%', '68%'],
                            center: ['50%', '46%'],
                            label: { formatter: '{b}\\n{c}' },
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
                    cat.type = hasParent ? fileManagerText('Frontend.222', 'Subcategory') : fileManagerText('Frontend.223', 'Category');
                    cat.level = hasParent ? 2 : 1;
                    if (hasParent) {
                        categoryMap[cat.parentId].children.push(cat);
                        cat.__linked = true;
                    }
                });

                (this.tagManager.tags || []).forEach(function (row) {
                    const tag = Object.assign({}, row, {
                        nodeType: 'tag',
                        type: fileManagerText('Frontend.224', 'Tag'),
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
                    this.$message.warning(fileManagerText('Frontend.128', 'Add a category first.'));
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
                    ? fileManagerText('Frontend.257', 'Delete tag “{0}”?', row.name)
                    : fileManagerText('Frontend.258', 'Delete category “{0}” and its child tags?', row.name);
                this.$confirm(tip, fileManagerText('Frontend.259', 'Notice'), { type: 'warning' }).then(function () {
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
                    self.$message.success(fileManagerText('Frontend.039', 'Deleted successfully'));
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
                this.$message.success(enabled
                    ? fileManagerText('Frontend.076', 'Disable')
                    : fileManagerText('Frontend.095', 'Enable'));
            },
            setSelectedTagStatus: function (enabled) {
                const ids = (this.tagManager && this.tagManager.selectedIds) || [];
                if (!ids.length) {
                    this.$message.warning(fileManagerText('Frontend.169', 'Select at least one item'));
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
                this.$message.success(enabled
                    ? fileManagerText('Frontend.095', 'Enable')
                    : fileManagerText('Frontend.076', 'Disable'));
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
                        self.$message.warning(fileManagerText('Frontend.161', 'Enter a category name, up to 50 characters'));
                        return;
                    }
                    if (form.parentId && form.parentId === currentId) {
                        self.$message.warning(fileManagerText('Frontend.260', 'A category cannot select itself'));
                        return;
                    }
                    const exists = (self.tagManager.list || []).some(function (row) {
                        if (editing && row.id === currentId) return false;
                        return String(row.name || '').toLowerCase() === name.toLowerCase();
                    });
                    if (exists) {
                        self.$message.warning(fileManagerText('Frontend.082', 'The category name already exists'));
                        return;
                    }
                    self.tagCategoryDialog.loading = true;
                    try {
                        const now = self.formatTagDateTime(new Date());
                        if (editing) {
                            const target = (self.tagManager.list || []).find(function (row) { return row.id === currentId; });
                            if (!target) {
                                self.$message.error(fileManagerText('Frontend.261', 'Category does not exist'));
                                return;
                            }
                            target.name = name;
                            target.description = (form.description || '').trim();
                            target.parentId = form.parentId || '';
                            target.admin = form.admin || '-';
                            target.updatedAt = now;
                            target.updatedBy = '当前用户';
                            self.$message.success(fileManagerText('Frontend.033', 'Updated successfully'));
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
                            self.$message.success(fileManagerText('Frontend.041', 'Created successfully'));
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
                        self.$message.warning(fileManagerText('Frontend.163', 'Enter a tag name, up to 30 characters'));
                        return;
                    }
                    if (!categoryId) {
                        self.$message.warning(fileManagerText('Frontend.171', 'Select a category'));
                        return;
                    }
                    const category = (self.tagManager.list || []).find(function (row) { return row.id === categoryId; });
                    if (!category) {
                        self.$message.warning(fileManagerText('Frontend.262', 'The selected category does not exist. Select another.'));
                        return;
                    }
                    const dup = (self.tagManager.tags || []).some(function (tag) {
                        if (editing && tag.id === currentId) return false;
                        return tag.categoryId === categoryId
                            && String(tag.name || '').toLowerCase() === name.toLowerCase();
                    });
                    if (dup) {
                        self.$message.warning(fileManagerText('Frontend.140', 'A tag with this name already exists in the category'));
                        return;
                    }
                    self.tagCreateDialog.loading = true;
                    try {
                        if (!Array.isArray(self.tagManager.tags)) self.tagManager.tags = [];
                        const now = self.formatTagDateTime(new Date());
                        if (editing) {
                            const target = self.tagManager.tags.find(function (tag) { return tag.id === currentId; });
                            if (!target) {
                                self.$message.error(fileManagerText('Frontend.263', 'Tag does not exist'));
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
                            self.$message.success(fileManagerText('Frontend.033', 'Updated successfully'));
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
                            self.$message.success(fileManagerText('Frontend.041', 'Created successfully'));
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
            importTags: function () { this.$message.info(fileManagerText('Frontend.100', 'Import')); },
            exportTags: function () { this.$message.info(fileManagerText('Frontend.101', 'Export')); },
            openTagRowMenu: function (row) {
                if (!row) return;
                //Legacy comment translated to English.
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

                //Legacy comment translated to English.
                if (this.favoriteList && this.favoriteList.list) {
                    this.favoriteList.list = this.favoriteList.list.filter(function (item) { return item.id !== row.id; });
                    this.saveFavoritesToStorage();
                    this.searchFavorites();
                }

                //Legacy comment translated to English.
                if (this.homePage && this.homePage.quickAccess) {
                    this.homePage.quickAccess = this.homePage.quickAccess.filter(function (item) { return item.id !== row.id; });
                    this.saveQuickAccessToStorage();
                }

                //Legacy comment translated to English.
                this.tableData = (this.tableData || []).filter(function (item) { return item.id !== row.id; });
                if (this.total > 0) this.total -= 1;
            },
            emptyRecycleBin: function () {
                var list = (this.recycleBin && (this.recycleBin.list || this.recycleBin.displayList)) || [];
                if (!list.length) {
                    this.$message.info(fileManagerText('Frontend.133', 'No documents'));
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
                    this.$message.info(fileManagerText('Frontend.133', 'No documents'));
                    return;
                }

                this.$set(this.recycleBin, 'emptying', true);
                try {
                    var ids = items.map(function (item) { return item.id; }).filter(function (id) { return id != null; });
                    var result = null;
                    try {
                        result = unwrap(await post(pageUrl + '?handler=EmptyRecycle', { ids: ids }));
                    } catch (batchError) {
                        //Legacy comment translated to English.
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
                                ? fileManagerText('Frontend.229', 'Recycle bin emptied successfully')
                                : fileManagerText('Frontend.230', 'Deleted {0}; failed {1}: {2}', successFallback, remainingFallback.length, lastError || '')
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
                        this.$message.success((result && result.message) || fileManagerText('Frontend.229', 'Recycle bin emptied successfully'));
                    } else if (remaining.length < items.length) {
                        this.$message.warning((result && result.message) || fileManagerText('Frontend.231', 'Some files could not be deleted; {0} remain in the recycle bin', remaining.length));
                    } else {
                        this.$message.error((result && result.message) || fileManagerText('Frontend.232', 'Could not empty the recycle bin completely'));
                    }
                } catch (error) {
                    this.$message.error(fileManagerText('Frontend.233', 'Failed to empty the recycle bin: {0}', errorMessage(error)));
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
                const segments = [this.rootFolderName || fileManagerText('Frontend.072', 'Enterprise documents'), this.orgName || ''].concat(
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
                this.$message.success(fileManagerText('Frontend.090', 'Remove favorite'));
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
                this.$message.success(fileManagerText('Frontend.119', 'Favorite'));
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
                return ({
                    0: fileManagerText('Frontend.020', 'Text'),
                    1: 'Word',
                    2: 'PowerPoint',
                    3: 'Excel',
                    4: fileManagerText('Frontend.005', 'Code'),
                    999: fileManagerText('Frontend.006', 'Other')
                })[fileType] || fileManagerText('Frontend.006', 'Other');
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
                const segments = [this.rootFolderName || fileManagerText('Frontend.023', 'Root folder')].concat((this.folderPath || []).map(function (item) { return item.name; }));
                return fileManagerText('Frontend.234', 'Upload files to [/{0}] -- resumable upload supported', segments.join('/'));
            },
            handleRowCommand: function (command, row) {
                if (command === 'download') return this.downloadFile(row);
                if (command === 'preview') return this.previewFile(row);
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
                    this.$message.success(fileManagerText('Frontend.071', 'Remove from Quick Access'));
                } else {
                    list.unshift({
                        id: row.id,
                        fileName: row.fileName,
                        fileSize: row.fileSize,
                        uploadTime: row.uploadTime,
                        folderPathText: typeof this.buildFavoritePathText === 'function' ? this.buildFavoritePathText() : '',
                        resourceScope: this.resourceScope
                    });
                    this.$message.success(fileManagerText('Frontend.147', 'Use Quick Access'));
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
                    this.$message.success(fileManagerText('Frontend.075', 'Save settings'));
                } catch (e) {
                    this.$message.error(fileManagerText('Frontend.035', 'Save failed'));
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
                    this.$message.success(fileManagerText('Frontend.108', 'Restore defaults'));
                } catch (e) {
                    this.$message.error(fileManagerText('Frontend.040', 'Delete failed'));
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
                    return fileManagerText('Frontend.216', 'File “{0}” with type {1} has been filtered and cannot be uploaded.', name, ext);
                }
                const allowed = this.parseExtensionList((this.uploadSettings && this.uploadSettings.allowedTypesText) || '');
                if (allowed.length && (!ext || allowed.indexOf(ext) === -1)) {
                    return fileManagerText('Frontend.217', 'File “{0}” has a type that is not in the allowed upload list.', name);
                }
                const maxBytes = this.getMaxUploadSizeBytes();
                if (fileSize > maxBytes) {
                    return fileManagerText('Frontend.218', 'File “{0}” exceeds {1} MB and cannot be uploaded.', name, Math.round(maxBytes / 1024 / 1024));
                }
                return '';
            },
            showFileTagDialog: function (row) {
                if (typeof this.loadTagManagerFromStorage === 'function') this.loadTagManagerFromStorage();
                if (typeof this.loadFileTagMapFromStorage === 'function') this.loadFileTagMapFromStorage();
                const catalog = (this.tagManager && this.tagManager.tags) || [];
                if (!catalog.length) {
                    this.$message.warning(fileManagerText('Frontend.130', 'Create tags in Tag Management first.'));
                }
                const existingItems = this.getFileTagItems(row);
                const selectedIds = existingItems.map(function (item) { return item.id; }).filter(Boolean);
                this.fileTagDialog = {
                    visible: true,
                    loading: false,
                    row: row,
                    selectedIds: selectedIds
                };
            },
            getFileTagItems: function (row) {
                if (!row) return [];
                if (Array.isArray(row.tagItems) && row.tagItems.length) {
                    return row.tagItems.map(function (item) {
                        if (typeof item === 'string') return { id: item, name: item };
                        return { id: item.id, name: item.name || item.id };
                    });
                }
                const fromMap = this.fileTagMap && row.id != null ? this.fileTagMap[String(row.id)] : null;
                if (Array.isArray(fromMap) && fromMap.length) {
                    return fromMap.map(function (item) {
                        if (typeof item === 'string') return { id: item, name: item };
                        return { id: item.id, name: item.name || item.id };
                    });
                }
                if (Array.isArray(row.tags) && row.tags.length) {
                    return row.tags.map(function (item) {
                        if (typeof item === 'string') return { id: item, name: item };
                        return { id: item.id, name: item.name || item.id };
                    });
                }
                return [];
            },
            getFileTagLabels: function (row) {
                return this.getFileTagItems(row).map(function (item) { return item.name; }).filter(Boolean);
            },
            removeSelectedFileTag: function (tagId) {
                if (!this.fileTagDialog || !Array.isArray(this.fileTagDialog.selectedIds)) return;
                this.fileTagDialog.selectedIds = this.fileTagDialog.selectedIds.filter(function (id) { return id !== tagId; });
            },
            loadFileTagMapFromStorage: function () {
                try {
                    const raw = localStorage.getItem(this.fileTagStorageKey || 'ncf.fileManager.fileTags');
                    const map = raw ? JSON.parse(raw) : {};
                    this.fileTagMap = map && typeof map === 'object' && !Array.isArray(map) ? map : {};
                } catch (e) {
                    this.fileTagMap = {};
                }
            },
            saveFileTagMapToStorage: function () {
                try {
                    localStorage.setItem(
                        this.fileTagStorageKey || 'ncf.fileManager.fileTags',
                        JSON.stringify(this.fileTagMap || {})
                    );
                } catch (e) { /* ignore */ }
            },
            applyFileTagsToTable: function () {
                const self = this;
                if (!this.fileTagMap) this.loadFileTagMapFromStorage();
                (this.tableData || []).forEach(function (row) {
                    const mapped = self.fileTagMap && row.id != null ? self.fileTagMap[String(row.id)] : null;
                    const finalItems = Array.isArray(mapped)
                        ? mapped.map(function (item) {
                            if (typeof item === 'string') return { id: item, name: item };
                            return { id: item.id, name: item.name || item.id };
                        })
                        : [];
                    self.$set(row, 'tagItems', finalItems);
                    self.$set(row, 'tags', finalItems.map(function (item) { return item.name; }));
                    self.$set(row, 'tagIds', finalItems.map(function (item) { return item.id; }));
                });
            },
            onFileTagDialogClosed: function () {
                if (!this.fileTagDialog) return;
                this.fileTagDialog.loading = false;
                this.fileTagDialog.row = null;
                this.fileTagDialog.selectedIds = [];
            },
            submitFileTags: function () {
                const row = this.fileTagDialog && this.fileTagDialog.row;
                if (!row || row.id == null) return;
                this.fileTagDialog.loading = true;
                try {
                    const selectedIds = (this.fileTagDialog.selectedIds || []).slice();
                    const catalogMap = {};
                    ((this.tagManager && this.tagManager.tags) || []).forEach(function (tag) {
                        catalogMap[tag.id] = tag;
                    });
                    const items = selectedIds.map(function (id) {
                        const hit = catalogMap[id];
                        return hit
                            ? { id: hit.id, name: hit.name, categoryId: hit.categoryId || '' }
                            : { id: id, name: String(id) };
                    });
                    if (!this.fileTagMap) this.fileTagMap = {};
                    this.$set(this.fileTagMap, String(row.id), items);
                    this.saveFileTagMapToStorage();
                    this.$set(row, 'tagItems', items);
                    this.$set(row, 'tags', items.map(function (item) { return item.name; }));
                    this.$set(row, 'tagIds', items.map(function (item) { return item.id; }));
                    this.fileTagDialog.visible = false;
                    this.$message.success(items.length ? fileManagerText('Frontend.227', 'File tags updated') : fileManagerText('Frontend.228', 'File tags cleared'));
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
                    this.applyFileTagsToTable();
                } catch (error) {
                    this.$message.error(fileManagerText('Frontend.030', 'Failed to load the list') + ': ' + errorMessage(error));
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
                    this.$message.error(fileManagerText('Frontend.030', 'Failed to load the list') + ': ' + errorMessage(error));
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
                        this.$message.warning(fileManagerText('Frontend.215', 'The target folder is unavailable. Returned to the root folder.'));
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
                    this.$message.success(fileManagerText('Frontend.033', 'Updated successfully'));
                } catch (error) {
                    this.$message.error(fileManagerText('Frontend.034', 'Update failed') + ': ' + errorMessage(error));
                }
            },
            downloadFile: function (row) { window.location.assign(pageUrl + '?handler=Download&id=' + encodeURIComponent(row.id)); },
            buildPreviewUrl: function (id) {
                return pageUrl + '?handler=Preview&id=' + encodeURIComponent(id);
            },
            getPreviewKind: function (fileName) {
                const ext = String(fileName || '').split('.').pop().toLowerCase();
                const imageExt = { png: 1, jpg: 1, jpeg: 1, gif: 1, webp: 1, avif: 1, bmp: 1, svg: 1, ico: 1 };
                const textExt = { txt: 1, log: 1, csv: 1, tsv: 1, json: 1, xml: 1, yaml: 1, yml: 1, sql: 1, cs: 1, js: 1, ts: 1, css: 1, html: 1, htm: 1 };
                if (ext === 'pdf') return 'pdf';
                if (imageExt[ext]) return 'image';
                if (ext === 'md' || ext === 'markdown') return 'markdown';
                if (textExt[ext]) return 'text';
                if (ext === 'docx') return 'docx';
                if (ext === 'xlsx') return 'xlsx';
                if (ext === 'pptx') return 'pptx';
                if (ext === 'doc' || ext === 'xls' || ext === 'ppt') return 'legacy-office';
                return 'unsupported';
            },
            fetchPreviewArrayBuffer: async function (id) {
                const response = await axios.get(this.buildPreviewUrl(id), { responseType: 'arraybuffer' });
                return response.data;
            },
            fetchPreviewTextContent: async function (id) {
                const response = await axios.get(pageUrl + '?handler=PreviewContent&id=' + encodeURIComponent(id));
                const payload = unwrap(response);
                return (payload && payload.content) || '';
            },
            renderMarkdownHtml: function (text) {
                if (typeof marked !== 'undefined' && marked.parse) {
                    return marked.parse(text || '');
                }
                return (text || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/\n/g, '<br>');
            },
            splitSlideText: function (text) {
                return String(text || '').split(/\n\s*\n+/).map(function (part) { return part.trim(); }).filter(Boolean);
            },
            onFileRowDblClick: function (row) {
                if (!row || row.id == null) return;
                this.previewFile(row);
            },
            onFilePreviewClosed: function () {
                this.filePreviewDialog = {
                    visible: false,
                    loading: false,
                    fileName: '',
                    mode: '',
                    url: '',
                    content: '',
                    html: '',
                    slides: [],
                    error: '',
                    row: null
                };
            },
            previewFile: async function (row) {
                if (!row || row.id == null) {
                    this.$message.warning(fileManagerText('Frontend.131', 'No previewable content'));
                    return;
                }
                const fileName = row.fileName || row.name || fileManagerText('Frontend.235', 'Unnamed file');
                const kind = this.getPreviewKind(fileName);
                const previewUrl = this.buildPreviewUrl(row.id);
                this.filePreviewDialog = {
                    visible: true,
                    loading: true,
                    fileName: fileName,
                    mode: kind,
                    url: '',
                    content: '',
                    html: '',
                    slides: [],
                    error: '',
                    row: row
                };
                try {
                    if (kind === 'pdf' || kind === 'image') {
                        this.filePreviewDialog.url = previewUrl;
                        this.filePreviewDialog.mode = kind;
                        return;
                    }
                    if (kind === 'legacy-office') {
                        this.filePreviewDialog.mode = 'error';
                        this.filePreviewDialog.error = fileManagerText('Frontend.200', 'Legacy Office formats cannot be previewed online.');
                        return;
                    }
                    if (kind === 'unsupported') {
                        this.filePreviewDialog.mode = 'error';
                        this.filePreviewDialog.error = fileManagerText('Frontend.201', 'This file type cannot be previewed online.');
                        return;
                    }
                    if (kind === 'docx' && typeof mammoth !== 'undefined') {
                        const arrayBuffer = await this.fetchPreviewArrayBuffer(row.id);
                        const result = await mammoth.convertToHtml({ arrayBuffer: arrayBuffer });
                        this.filePreviewDialog.mode = 'html';
                        this.filePreviewDialog.html = result.value || '<p>' + fileManagerText('Frontend.202', 'The document is empty.') + '</p>';
                        return;
                    }
                    if (kind === 'xlsx' && typeof XLSX !== 'undefined') {
                        const arrayBuffer = await this.fetchPreviewArrayBuffer(row.id);
                        const workbook = XLSX.read(arrayBuffer, { type: 'array' });
                        const sheetName = workbook.SheetNames && workbook.SheetNames[0];
                        if (!sheetName) {
                            this.filePreviewDialog.mode = 'error';
                            this.filePreviewDialog.error = fileManagerText('Frontend.203', 'The Excel file has no previewable worksheets.');
                            return;
                        }
                        this.filePreviewDialog.mode = 'excel';
                        this.filePreviewDialog.html = XLSX.utils.sheet_to_html(workbook.Sheets[sheetName]);
                        return;
                    }
                    if (kind === 'markdown') {
                        const arrayBuffer = await this.fetchPreviewArrayBuffer(row.id);
                        const text = new TextDecoder('utf-8').decode(arrayBuffer);
                        this.filePreviewDialog.mode = 'markdown';
                        this.filePreviewDialog.content = text;
                        this.filePreviewDialog.html = this.renderMarkdownHtml(text);
                        return;
                    }
                    if (kind === 'text') {
                        const arrayBuffer = await this.fetchPreviewArrayBuffer(row.id);
                        this.filePreviewDialog.mode = 'text';
                        this.filePreviewDialog.content = new TextDecoder('utf-8').decode(arrayBuffer);
                        return;
                    }
                    if (kind === 'pptx') {
                        const text = await this.fetchPreviewTextContent(row.id);
                        const slides = this.splitSlideText(text);
                        this.filePreviewDialog.mode = 'slides';
                        this.filePreviewDialog.slides = slides.length ? slides : [text];
                        return;
                    }
                    if (kind === 'docx' || kind === 'xlsx') {
                        const text = await this.fetchPreviewTextContent(row.id);
                        this.filePreviewDialog.mode = 'text';
                        this.filePreviewDialog.content = text;
                        return;
                    }
                    this.filePreviewDialog.mode = 'error';
                    this.filePreviewDialog.error = fileManagerText('Frontend.204', 'This file type does not support online preview.');
                } catch (error) {
                    this.filePreviewDialog.mode = 'error';
                    this.filePreviewDialog.error = fileManagerText('Frontend.205', 'Preview failed: {0}', errorMessage(error));
                } finally {
                    this.filePreviewDialog.loading = false;
                }
            },
            deleteFile: async function (row) {
                try {
                    const shortName = String(row.fileName || '').replace(/\.[^.]+$/, '') || row.fileName;
                    await this.$confirm(
                        fileManagerText('Frontend.236', 'This will move the file to the recycle bin. Continue?'),
                        fileManagerText('Frontend.237', 'Delete: {0}', shortName),
                        { type: 'warning', confirmButtonText: '确定', cancelButtonText: '取消' }
                    );
                    this.moveFileToRecycleBin(row);
                    this.$message.success(fileManagerText('Frontend.238', 'Moved to the recycle bin'));
                } catch (error) {
                    if (error !== 'cancel') this.$message.error(fileManagerText('Frontend.239', 'Delete failed: {0}', errorMessage(error)));
                }
            },
            setPublication: async function (row, publish) {
                try {
                    await post(pageUrl + '?handler=SetSiteAssetPublication', { id: row.id, publish: publish });
                    this.$message.success(publish ? fileManagerText('Frontend.240', 'Resource published') : fileManagerText('Frontend.241', 'Resource made private'));
                    await this.getList();
                } catch (error) {
                    this.$message.error(fileManagerText('Frontend.242', 'Failed to update publication status: {0}', errorMessage(error)));
                }
            },
            copyPublicUrl: async function (row) {
                const url = window.location.origin + row.publicUrl;
                try {
                    await navigator.clipboard.writeText(url);
                    this.$message.success(fileManagerText('Frontend.243', 'Public URL copied'));
                } catch (_) {
                    this.$prompt(fileManagerText('Frontend.244', 'Copy the following public URL'), fileManagerText('Frontend.245', 'Public URL'), { inputValue: url, inputType: 'textarea' });
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
                    this.$message.warning(fileManagerText('Frontend.246', 'This browser does not support folder selection. Use the latest Chrome, Edge or Safari.'));
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
                    this.$message.error(fileManagerText('Frontend.247', 'The browser did not provide a complete relative folder path. Select a folder again.'));
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
                    if (!rawFile || !rawFile.size) throw new Error(fileManagerText('Frontend.248', 'Some files could not be read. Select again.'));
                    if (typeof self.validateUploadFile === 'function') {
                        const reason = self.validateUploadFile(file.name || rawFile.name, rawFile.size);
                        if (reason) throw new Error(reason);
                    } else if (rawFile.size > maxBytes) {
                        throw new Error(fileManagerText('Frontend.218', 'File “{0}” exceeds {1} MB and cannot be uploaded.', file.name, maxMb));
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
                if (!this.uploadDialog.fileList.length) { this.$message.warning(fileManagerText('Frontend.211', 'Select files to upload.')); return; }
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
                        //Legacy comment translated to English.
                        formData.append('__RequestVerificationToken', getRequestVerificationToken());
                        await post(pageUrl + '?handler=Upload', formData);
                        uploadedCount += batches[batchIndex].length;
                        this.uploadDialog.progress = Math.round(uploadedCount * 100 / files.length);
                    }
                    this.$message.success(this.uploadDialog.mode === 'folder'
                        ? fileManagerText('Frontend.212', 'Folder upload completed; the original directory structure was preserved.')
                        : fileManagerText('Frontend.213', 'Upload succeeded; site static resources must still be explicitly published before the site can reference them.'));
                    this.uploadDialog.visible = false;
                    this.reloadFolderTree();
                    await this.getList();
                } catch (error) {
                    const partialMessage = uploadedCount > 0 ? '，已完成 ' + uploadedCount + ' 个文件' : '';
                    this.$message.error(fileManagerText('Frontend.214', 'Upload failed: {0}', partialMessage ? partialMessage + ': ' + errorMessage(error) : errorMessage(error)));
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
                if (!form.name || !form.name.trim()) { this.$message.warning(fileManagerText('Frontend.249', 'Enter a folder name')); return; }
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
                    this.$message.success(this.folderDialog.editing ? fileManagerText('Frontend.250', 'Folder updated') : fileManagerText('Frontend.251', 'Folder created'));
                    this.folderDialog.visible = false;
                    this.reloadFolderTree();
                } catch (error) {
                    this.$message.error(fileManagerText('Frontend.252', 'Failed to save the folder: {0}', errorMessage(error)));
                } finally {
                    this.folderDialog.loading = false;
                }
            },
            deleteFolder: async function (folder) {
                try {
                    await this.$confirm(fileManagerText('Frontend.253', 'Only empty folders can be deleted. Delete “{0}”?', folder.name), fileManagerText('Frontend.254', 'Confirm deletion'), { type: 'warning' });
                    await post(pageUrl + '?handler=DeleteFolder&id=' + encodeURIComponent(folder.id));
                    if (this.currentFolderId === folder.id) await this.enterFolder(null);
                    this.reloadFolderTree();
                    this.$message.success(fileManagerText('Frontend.255', 'Folder deleted'));
                } catch (error) {
                    if (error !== 'cancel') this.$message.error(fileManagerText('Frontend.256', 'Failed to delete the folder: {0}', errorMessage(error)));
                }
            }
        }
    });
})();

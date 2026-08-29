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
                    enterpriseUsed: '6.28GB',
                    orgUsages: [
                        { name: '山西米立信息技术有限公司', size: '6.28GB' },
                        { name: '企业文档根目录', size: '0.00KB' },
                        { name: '知识库资料', size: '0.00KB' }
                    ],
                    sizeTotalLabel: '7G',
                    countTotalLabel: '90',
                    sizeSlices: [
                        { name: '其他', value: 4.55, percent: '70.00%', color: '#91d5ff' },
                        { name: '文档', value: 1.22, percent: '18.77%', color: '#95de64' },
                        { name: '视频', value: 0.54, percent: '8.31%', color: '#ffd666' },
                        { name: '图片', value: 0.19, percent: '2.92%', color: '#597ef7' },
                        { name: '音频', value: 0, percent: '0.00%', color: '#ff9c6e' }
                    ],
                    countSlices: [
                        { name: '其他', value: 48, percent: '53.33%', color: '#91d5ff' },
                        { name: '图片', value: 23, percent: '25.56%', color: '#597ef7' },
                        { name: '文档', value: 13, percent: '14.44%', color: '#95de64' },
                        { name: '视频', value: 6, percent: '6.67%', color: '#ffd666' },
                        { name: '音频', value: 0, percent: '0.00%', color: '#ff9c6e' }
                    ]
                },
                _dashboardCharts: null,
                tagManager: {
                    keyword: '',
                    category: '',
                    status: '',
                    selectedIds: [],
                    categoryOptions: [
                        { label: '业务分类', value: 'biz' },
                        { label: '系统分类', value: 'system' }
                    ],
                    list: [],
                    displayList: []
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
                homePage: {
                    keyword: '',
                    list: [],
                    displayList: []
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
                    this.$nextTick(this.refreshDashboardCharts);
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
            }
        },
        created: function () {
            this.restoreRouteState();
            this.syncActiveNavFromScope();
            this.initDashboardDefaults();
            this.loadFavoritesFromStorage();
            this.loadRecycleFromStorage();
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
                    this.guideDialogVisible = true;
                    return;
                }
                if (item.key === 'dashboard') {
                    this.activeNavKey = 'dashboard';
                    this.$nextTick(this.refreshDashboardCharts);
                    return;
                }
                if (item.key === 'tags') {
                    this.activeNavKey = 'tags';
                    this.searchTags();
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
                    while (cursor <= end && list.length < 31) {
                        const m = String(cursor.getMonth() + 1).padStart(2, '0');
                        const d = String(cursor.getDate()).padStart(2, '0');
                        list.push(m + '-' + d);
                        cursor.setDate(cursor.getDate() + 1);
                    }
                    if (list.length) return list;
                }
                const days = this.dashboard.capacityRange === '365d' ? 12 : (this.dashboard.capacityRange === '30d' ? 30 : 7);
                const result = [];
                const now = new Date();
                for (let i = days - 1; i >= 0; i--) {
                    const d = new Date(now);
                    d.setDate(now.getDate() - i);
                    result.push(String(d.getMonth() + 1).padStart(2, '0') + '-' + String(d.getDate()).padStart(2, '0'));
                }
                return result;
            },
            refreshDashboardCharts: function () {
                if (this.activeNavKey !== 'dashboard') return;
                if (typeof echarts === 'undefined') return;
                const labels = this.buildCapacityDates();
                const total = labels.map(function (_, i) { return Number((6.2 + (i % 5) * 0.05).toFixed(2)); });
                const enterprise = labels.map(function () { return Number((0.08 + Math.random() * 0.05).toFixed(2)); });
                const group = labels.map(function () { return Number((0.05 + Math.random() * 0.04).toFixed(2)); });
                const personal = labels.map(function () { return Number((0.03 + Math.random() * 0.03).toFixed(2)); });
                const team = labels.map(function () { return Number((0.02 + Math.random() * 0.02).toFixed(2)); });

                const capacityChart = this.getOrCreateChart(this.$refs.capacityChart);
                if (capacityChart) {
                    capacityChart.setOption({
                        color: ['#409eff', '#95de64', '#69c0ff', '#ff9c6e', '#ffc069'],
                        tooltip: { trigger: 'axis' },
                        legend: { bottom: 0, data: ['总容量', '企业文档', '群文档', '个人文档', '团队文档'] },
                        grid: { left: 40, right: 20, top: 24, bottom: 48 },
                        xAxis: { type: 'category', boundaryGap: false, data: labels },
                        yAxis: { type: 'value', axisLabel: { formatter: '{value}G' }, splitLine: { lineStyle: { type: 'dashed' } } },
                        series: [
                            { name: '总容量', type: 'line', smooth: true, data: total },
                            { name: '企业文档', type: 'line', smooth: true, data: enterprise },
                            { name: '群文档', type: 'line', smooth: true, data: group },
                            { name: '个人文档', type: 'line', smooth: true, data: personal },
                            { name: '团队文档', type: 'line', smooth: true, data: team }
                        ]
                    }, true);
                }

                const sizeChart = this.getOrCreateChart(this.$refs.sizeChart);
                if (sizeChart) {
                    sizeChart.setOption({
                        tooltip: { trigger: 'item', formatter: '{b}: {c}G ({d}%)' },
                        legend: { bottom: 0, data: this.dashboard.sizeSlices.map(function (x) { return x.name; }) },
                        series: [{
                            type: 'pie',
                            radius: ['48%', '68%'],
                            center: ['50%', '46%'],
                            label: { formatter: '{b}\n{c}G ({d}%)' },
                            data: this.dashboard.sizeSlices.map(function (x) {
                                return { name: x.name, value: x.value, itemStyle: { color: x.color } };
                            }),
                            emphasis: { scale: false }
                        }],
                        graphic: [{
                            type: 'text',
                            left: 'center',
                            top: '42%',
                            style: { text: this.dashboard.sizeTotalLabel, textAlign: 'center', fill: '#303133', fontSize: 28, fontWeight: 600 }
                        }]
                    }, true);
                }

                const countChart = this.getOrCreateChart(this.$refs.countChart);
                if (countChart) {
                    countChart.setOption({
                        tooltip: { trigger: 'item', formatter: '{b}: {c}个文件 ({d}%)' },
                        legend: { bottom: 0, data: this.dashboard.countSlices.map(function (x) { return x.name; }) },
                        series: [{
                            type: 'pie',
                            radius: ['48%', '68%'],
                            center: ['50%', '46%'],
                            label: { formatter: '{b}\n{c}个文件 ({d}%)' },
                            data: this.dashboard.countSlices.map(function (x) {
                                return { name: x.name, value: x.value, itemStyle: { color: x.color } };
                            }),
                            emphasis: { scale: false }
                        }],
                        graphic: [{
                            type: 'text',
                            left: 'center',
                            top: '42%',
                            style: { text: this.dashboard.countTotalLabel, textAlign: 'center', fill: '#303133', fontSize: 28, fontWeight: 600 }
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
                const keyword = (this.tagManager.keyword || '').trim().toLowerCase();
                const category = this.tagManager.category || '';
                const status = this.tagManager.status || '';
                this.tagManager.displayList = (this.tagManager.list || []).filter(function (row) {
                    if (keyword && String(row.name || '').toLowerCase().indexOf(keyword) === -1) return false;
                    if (category && row.category !== category) return false;
                    if (status === 'enabled' && row.status !== '启用') return false;
                    if (status === 'disabled' && row.status !== '停用') return false;
                    return true;
                });
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
            createTag: function () { this.$message.info('新增标签功能即将开放'); },
            createTagCategory: function () { this.$message.info('新增分类功能即将开放'); },
            expandTagRows: function () { this.$message.info('展开功能即将开放'); },
            enableSelectedTags: function () { this.$message.info('启用功能即将开放'); },
            disableSelectedTags: function () { this.$message.info('停用功能即将开放'); },
            importTags: function () { this.$message.info('导入功能即将开放'); },
            exportTags: function () { this.$message.info('导出功能即将开放'); },
            openTagRowMenu: function () { this.$message.info('更多操作即将开放'); },
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
                if (command === 'manageTags') return this.showFileTagDialog(row);
                if (command === 'delete') return this.deleteFile(row);
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
                const batches = [];
                let batch = [];
                let batchBytes = 0;
                fileList.forEach(function (file) {
                    const rawFile = file.raw || file;
                    if (!rawFile || !rawFile.size) throw new Error('存在无法读取的文件，请重新选择。');
                    if (rawFile.size > maxFileSizeBytes) throw new Error('文件“' + file.name + '”超过 50 MB，无法上传。');
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

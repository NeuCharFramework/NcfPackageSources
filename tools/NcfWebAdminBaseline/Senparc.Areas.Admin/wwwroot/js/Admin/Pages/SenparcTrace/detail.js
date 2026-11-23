var app = new Vue({
    el: '#app',
    data: {
        loading: false,
        detailLoading: false,
        dateTitle: '',
        tableData: [],
        availableThreads: [],
        exceptionCount: 0,
        fileSize: 0,
        detailVisible: false,
        copied: false,
        detail: {},
        searchData: {
            pageIndex: 1,
            pageSize: 20,
            total: 0
        },
        filterConditions: {
            keyword: '',
            timeRange: [],
            threadId: '',
            traceType: '',
            exceptionStatus: ''
        },
        debounceTimer: null,
        traceTypes: [
            { value: 0, label: ncfT('Trace.TypeNormal') },
            { value: 2, label: ncfT('Trace.TypeAPI') },
            { value: 4, label: ncfT('Trace.TypePostRequest') },
            { value: 6, label: ncfT('Trace.TypeGetRequest') },
            { value: 8, label: ncfT('Trace.TypeException') }
        ],
        pickerOptions: {
            shortcuts: [{
                text: ncfT('Trace.Today'),
                onClick(picker) {
                    const end = new Date();
                    const start = new Date();
                    start.setHours(0, 0, 0, 0);
                    picker.$emit('pick', [start, end]);
                }
            }, {
                text: ncfT('Trace.LastHour'),
                onClick(picker) {
                    const end = new Date();
                    picker.$emit('pick', [new Date(end.getTime() - 3600 * 1000), end]);
                }
            }]
        }
    },
    created() {
        this.dateTitle = resizeUrl().date || '';
        this.fetchData();
    },
    methods: {
        scheduleFetch() {
            clearTimeout(this.debounceTimer);
            this.debounceTimer = setTimeout(() => {
                this.searchData.pageIndex = 1;
                this.fetchData();
            }, 350);
        },
        fetchData() {
            this.loading = true;
            const params = {
                handler: 'Query',
                date: this.dateTitle,
                pageIndex: this.searchData.pageIndex,
                pageSize: this.searchData.pageSize
            };
            if (this.filterConditions.keyword) params.keyword = this.filterConditions.keyword;
            if (this.filterConditions.threadId !== '') params.threadId = this.filterConditions.threadId;
            if (this.filterConditions.traceType !== '') params.traceType = this.filterConditions.traceType;
            if (this.filterConditions.exceptionStatus !== '') params.isException = this.filterConditions.exceptionStatus;
            if (this.filterConditions.timeRange && this.filterConditions.timeRange.length === 2) {
                params.startTime = this.filterConditions.timeRange[0].toISOString();
                params.endTime = this.filterConditions.timeRange[1].toISOString();
            }

            service.get('/Admin/SenparcTrace/DateLog', { params })
                .then(res => {
                    const data = res.data.data;
                    this.tableData = data.items;
                    this.searchData.total = data.total;
                    this.exceptionCount = data.exceptionCount;
                    this.fileSize = data.fileSize;
                    this.availableThreads = data.threads;
                })
                .catch(() => this.$message.error(ncfT('Trace.LoadFailed')))
                .finally(() => {
                    this.loading = false;
                });
        },
        resetFilters() {
            this.filterConditions = {
                keyword: '',
                timeRange: [],
                threadId: '',
                traceType: '',
                exceptionStatus: ''
            };
            this.searchData.pageIndex = 1;
            this.fetchData();
        },
        openDetail(row) {
            this.detailVisible = true;
            this.detailLoading = true;
            this.copied = false;
            service.get('/Admin/SenparcTrace/DateLog', {
                params: {
                    handler: 'Entry',
                    date: this.dateTitle,
                    line: row.line
                }
            }).then(res => {
                this.detail = res.data.data;
            }).catch(() => {
                this.detailVisible = false;
                this.$message.error(ncfT('Trace.LoadFailed'));
            }).finally(() => {
                this.detailLoading = false;
            });
        },
        traceTypeText(value) {
            const match = this.traceTypes.find(item => item.value === Number(value));
            return match ? match.label : value;
        },
        typeTag(value) {
            if (Number(value) === 8) return 'danger';
            if (Number(value) === 4 || Number(value) === 6) return 'warning';
            if (Number(value) === 2) return 'primary';
            return 'info';
        },
        formatBytes(bytes) {
            if (!Number.isFinite(bytes) || bytes <= 0) return '0 B';
            const units = ['B', 'KB', 'MB', 'GB'];
            const exponent = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
            const value = bytes / Math.pow(1024, exponent);
            return `${value.toFixed(exponent === 0 ? 0 : 1)} ${units[exponent]}`;
        },
        copyDetail() {
            const detail = this.detail || {};
            navigator.clipboard.writeText(detail.totalResult || '')
                .then(() => {
                    this.copied = true;
                    this.$message.success(ncfT('Trace.Copied'));
                })
                .catch(() => this.$message.error(ncfT('Trace.CopyFailed')));
        }
    }
});

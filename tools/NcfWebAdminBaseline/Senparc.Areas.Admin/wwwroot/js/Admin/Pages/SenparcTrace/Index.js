var app = new Vue({
    el: '#app',
    data: {
        loading: false,
        searchData: {
            pageIndex: 1,
            pageSize: 20,
            total: 0
        },
        tableData: []
    },
    created() {
        this.fetchData();
    },
    methods: {
        fetchData() {
            this.loading = true;
            const params = {
                handler: 'List',
                pageIndex: this.searchData.pageIndex,
                pageSize: this.searchData.pageSize
            };
            service.get('/Admin/SenparcTrace/Index', { params })
                .then(res => {
                    const data = res.data.data;
                    this.searchData.total = data.total;
                    this.tableData = data.items.map((item, index) => ({
                        ...item,
                        no: (data.pageIndex - 1) * data.pageSize + index + 1
                    }));
                })
                .catch(() => this.$message.error(ncfT('Trace.LoadFailed')))
                .finally(() => {
                    this.loading = false;
                });
        },
        formatBytes(bytes) {
            if (!Number.isFinite(bytes) || bytes <= 0) return '0 B';
            const units = ['B', 'KB', 'MB', 'GB'];
            const exponent = Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
            const value = bytes / Math.pow(1024, exponent);
            return `${value.toFixed(exponent === 0 ? 0 : 1)} ${units[exponent]}`;
        },
        formatTime(value) {
            return value ? new Date(value).toLocaleString() : '-';
        }
    }
});

new Vue({
    el: '.admin-site',
    data() {
        return {
            task: {},
            stats: {
                totalUrls: 0,
                successUrls: 0,
                failedUrls: 0
            },
            domains: [],
            activeTab: '',
            urlList: [],
            loading: false,
            currentPage: 1,
            pageSize: 10,
            total: 0
        }
    },
    mounted() {
        const id = this.getTaskId();
        this.loadTask(id);
        this.loadStats(id);
    },
    methods: {
        getTaskId() {
            const params = new URLSearchParams(window.location.search);
            return params.get('id');
        },
        async loadTask(id) {
            const res = await service.get(`/Admin/SenMapic/Task/Detail?handler=Task&id=${id}`);
            this.task = res.data;
        },
        async loadStats(id) {
            const res = await service.get(`/Admin/SenMapic/Task/Detail?handler=Stats&id=${id}`);
            this.stats = res.data;
            this.domains = res.data.domains;
            if (this.domains.length > 0) {
                this.activeTab = this.domains[0];
                this.loadUrlList();
            }
        },
        async loadUrlList() {
            this.loading = true;
            try {
                const id = this.getTaskId();
                const res = await service.get(`/Admin/SenMapic/Task/Detail?handler=UrlList&id=${id}&domain=${this.activeTab}&page=${this.currentPage}&pageSize=${this.pageSize}`);
                this.urlList = res.data.items;
                this.total = res.data.total;
            } finally {
                this.loading = false;
            }
        },
        handleTabClick() {
            this.currentPage = 1;
            this.loadUrlList();
        },
        handleSizeChange(val) {
            this.pageSize = val;
            this.loadUrlList();
        },
        handleCurrentChange(val) {
            this.currentPage = val;
            this.loadUrlList();
        },
        getStatusType(status) {
            const map = {
                '-1': 'danger',
                '0': 'info',
                '1': 'warning',
                '2': 'success'
            };
            return map[status] || 'info';
        },
        getStatusText(status) {
            const map = {
                '-1': '出错',
                '0': '等待开始',
                '1': '进行中',
                '2': '已完成'
            };
            return map[status] || '未知';
        },
        getHttpStatusType(code) {
            if (code >= 200 && code < 300) return 'success';
            if (code >= 300 && code < 400) return 'warning';
            if (code >= 400) return 'danger';
            return 'info';
        }
    }
}); 
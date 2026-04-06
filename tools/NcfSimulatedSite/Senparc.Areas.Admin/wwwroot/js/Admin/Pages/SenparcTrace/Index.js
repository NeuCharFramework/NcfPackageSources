var app = new Vue({
    el: '#app',
    data: {
        searchData: {
            pageIndex: 1,
            pageSize: 10,
            total: 0,
            keyword: ''
        },
        tableData: []
    },
    mounted() {
    },
    created() {
        this.fetchData();
    },
    methods: {
        fetchData: function () {
            const { pageIndex, pageSize, keyword } = this.searchData;
            service.get(`/Admin/SenparcTrace/Index?handler=List&pageIndex=${pageIndex}&pageSize=${pageSize}&keyword=${encodeURIComponent(keyword || '')}`).then(res => {
                var responseData = res.data.data;
                const actualData = [];
                var startIndex = (pageIndex - 1) * pageSize;
                (responseData.list || []).forEach(ele => {
                    startIndex++;
                    actualData.push({ no: startIndex, text: ele });
                });
                this.tableData = actualData;
                this.searchData.total = responseData.count;
                console.info(actualData);
            });
        },
        handleSearch() {
            this.searchData.pageIndex = 1;
            this.fetchData();
        },
        resetCondition() {
            this.searchData.keyword = '';
            this.searchData.pageIndex = 1;
            this.searchData.pageSize = 10;
            this.fetchData();
        }
    }
});
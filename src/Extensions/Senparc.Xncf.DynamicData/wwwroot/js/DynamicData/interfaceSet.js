var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com', // Test address
            elSize: 'medium', // el component size defaults to empty medium, small, mini
            // layout data list
            layoutList: [],
            listTotal:0,
            queryList: {
                page: 1,
                size:10
            }
        };
    },
    computed: {
    },
    watch: {
        //'isExtend': {
        //    handler: function (val, oldVal) {
        //    },
        //    immediate: true,
        //    //deep:true
        //}
    },
    created() {
        this.getListData()
    },
    mounted() {
        
    },
    beforeDestroy() {
        
    },
    methods: {
        // Get layoutList data
        getListData() {
            // simulated data
            const simulationData = []
            for (let i = 0; i < 10; i++) {
                simulationData.push({
                    id: i + 1,
                    thumbnail: '',
                    name: '这是布局名称',
                    description: '绑定数据表1，绑定数据表2，绑定数据表3绑定数据表1，绑定数据表2，绑定数据表3绑定数据表1，绑定数据表2，绑定数据表3',
                    state: i % 2 ? 1 : 2,
                    operationVisible: false
                })
            }
            this.layoutList = simulationData
            this.listTotal = simulationData.length
            // todo calling interface
        },
        // Get status style name
        getStateClass(val) {
            let valFormatt = val ? val.toString() : val
            let className = ''
            switch (valFormatt) {
                case '1':
                    className = 'stateColor-green'
                    break
                case '2':
                    className = 'stateColor-grey'
                    break
            }
            return className
        },
        // Get status text
        getStateText(val) {
            let valFormatt = val ? val.toString() : val
            let text = ''
            switch (valFormatt) {
                case '1':
                    text = '已启用'
                    break
                case '2':
                    text = '未启用'
                    break
            }
            return text
        },
        // Create layout 
        createLayout() {
            // todo jump ?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69
            window.open('/Admin/DynamicData/LayoutSet')
        },
        // Copy layout
        copyLayout(item) {
            if (!item) return
            item.operationVisible = false
            // todo calling interface
            // <strong>This is a <i>HTML</i> fragment</strong>
            this.$prompt('布局名称', '复制布局', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                inputPattern: /^(?!\s*$).+/,
                inputErrorMessage: '布局名称不正确'
            }).then(({ value }) => {
                this.$message({
                    type: 'success',
                    message: '你的布局名称是: ' + value
                });
            }).catch(() => {
                //this.$message({
                //    type: 'info',
                //    message: 'Cancel input'
                //});
            });
        },
        // View layout
        viewLayout(item) {
            if (!item) return
            item.operationVisible = false
            // todo calling interface
            // todo jump ?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69
            window.open('/Admin/DynamicData/LayoutSet')
        },
        // Delete layout
        deleteLayout(item) {
            if (!item) return
            item.operationVisible = false
            // todo calling interface
        }
    }
});

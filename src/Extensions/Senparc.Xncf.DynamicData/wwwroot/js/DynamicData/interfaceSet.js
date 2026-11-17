var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com', //Legacy comment translated to English.
            elSize: 'medium', //Legacy comment translated to English.
            //Legacy comment translated to English.
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
        // get layoutList data
        getListData() {
            //Legacy comment translated to English.
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
            // todo invokeinterface
        },
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        createLayout() {
            //Legacy comment translated to English.
            window.location.assign('/Admin/DynamicData/LayoutSet')
        },
        //Legacy comment translated to English.
        copyLayout(item) {
            if (!item) return
            item.operationVisible = false
            // todo invokeinterface
            //English documentation comment.
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
                //Legacy comment translated to English.
                //});
            });
        },
        //Legacy comment translated to English.
        viewLayout(item) {
            if (!item) return
            item.operationVisible = false
            // todo invokeinterface
            //Legacy comment translated to English.
            window.location.assign('/Admin/DynamicData/LayoutSet')
        },
        //Legacy comment translated to English.
        deleteLayout(item) {
            if (!item) return
            item.operationVisible = false
            // todo invokeinterface
        }
    }
});

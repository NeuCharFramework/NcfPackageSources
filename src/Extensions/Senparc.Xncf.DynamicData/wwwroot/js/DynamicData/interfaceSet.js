var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com', // 测试 地址
            elSize: 'medium', // el 组件尺寸大小 默认为空  medium、small、mini
            // 布局 数据 列表
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
        // 获取 layoutList 数据
        getListData() {
            // 模拟 数据
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
            // todo 调用接口
        },
        // 获取 状态样式名称
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
        // 获取 状态文本
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
        // 创建布局 
        createLayout() {
            // todo 跳转
        },
        // 复制布局
        copyLayout(item) {
            if (!item) return
            item.operationVisible = false
            // todo 调用接口
        },
        // 查看布局
        viewLayout(item) {
            if (!item) return
            item.operationVisible = false
            // todo 调用接口
        },
        // 删除布局
        deleteLayout(item) {
            if (!item) return
            item.operationVisible = false
            // todo 调用接口
        }
    }
});

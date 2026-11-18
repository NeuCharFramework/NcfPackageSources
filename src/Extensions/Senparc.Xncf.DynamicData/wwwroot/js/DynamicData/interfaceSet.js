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
                    name: ncfST('Frontend.300'),
                    description: ncfST('Frontend.267'),
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
                        text = ncfST('Frontend.301')
                    break
                case '2':
                        text = ncfST('Frontend.302')
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
            this.$prompt(ncfST('Frontend.303'), ncfST('Frontend.044'), {
                confirmButtonText: ncfST('Frontend.152'),
                cancelButtonText: ncfST('Frontend.034'),
                inputPattern: /^(?!\s*$).+/,
                inputErrorMessage: ncfST('Frontend.304')
            }).then(({ value }) => {
                this.$message({
                    type: 'success',
                    message: ncfST('Frontend.305') + ' ' + value
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

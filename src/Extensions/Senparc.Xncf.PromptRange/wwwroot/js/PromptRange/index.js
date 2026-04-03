var app = new Vue({
    el: "#app",
    data() {
        return {
            // Query list parameters
            queryList: {
                page: 1,
                size: 10,
                modelName: ''
            },
            pageSizes: [10, 20, 30, 50],
            tableTotal: 0,
            tableData: [], // Model list
            multipleSelection: {}, // selected model
            dialogFormVisible: false,
            dialogFormTitle: '新增模型',
            formLabelWidth: '',
            dateRange: [],
            usageData: [
                { date: '2024-08-01', usage: 100 },
                { date: '2024-08-02', usage: 120 },
                { date: '2024-08-03', usage: 90 },
                { date: '2024-08-04', usage: 110 },
                { date: '2024-08-05', usage: 150 },
                { date: '2024-08-06', usage: 80 },
                { date: '2024-08-07', usage: 130 },
            ],
            newModelForm: {
                modelName: '',
                modelAPI: '',
                modelAPIkey: ''
            },
            topPrompts: [
                { text: "生成风景图片", usageCount: 120 },
                { text: "编写故事", usageCount: 110 },
                { text: "自动生成摘要", usageCount: 90 },
            ],
            topModels: [
                { name: "GPT-4", usageCount: 150 },
                { name: "DALL-E", usageCount: 140 },
                { name: "Stable Diffusion", usageCount: 130 },
            ],
            rules: {
                modelName: [
                    { required: true, message: '请输入模型名称', trigger: 'blur' }
                ],
                modelAPI: [
                    { required: true, message: '请输入模型API', trigger: 'blur' }
                ],
                modelAPIkey: [
                    { required: true, message: '请输入API key', trigger: 'blur' }
                ]
            }
        };
    },
    mounted() {
        var chartDom = document.getElementById('main');
        var myChart = echarts.init(chartDom);
        
        var option;

        option = {
            tooltip: {
                trigger: 'axis'
            },
            legend: {
                show: true
            },
            xAxis: {
                type: 'category',
                data: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']
            },
            yAxis: {
                type: 'value'
            },
            series: [
                {
                    name: '使用次数',
                    data: [150, 230, 224, 218, 135, 147, 260],
                    type: 'line'
                }
            ]
        };
        myChart.setOption(option);
        this.initChart();
        return {
            dateRange: [],
            usageData: [
                { date: '2024-08-01', usage: 100 },
                { date: '2024-08-02', usage: 120 },
                { date: '2024-08-03', usage: 90 },
                { date: '2024-08-04', usage: 110 },
                { date: '2024-08-05', usage: 150 },
                { date: '2024-08-06', usage: 80 },
                { date: '2024-08-07', usage: 130 },
            ],
            topPrompts: [
                { text: "生成风景图片", usageCount: 120 },
                { text: "编写故事", usageCount: 110 },
                { text: "自动生成摘要", usageCount: 90 },
            ],
                topModels: [
                    { name: "GPT-4", usageCount: 150 },
                    { name: "DALL-E", usageCount: 140 },
                    { name: "Stable Diffusion", usageCount: 130 },
                ],
        };
        
        
        
    },
    watch: {
        //'isExtend': {
        //    handler: function (val, oldVal) {
        //    },
        //    immediate: true,
        //    //deep:true
        //}
    },
    created: function () {
        // Get table list data
        this.getList();
    },
    methods: {
        initChart() {
            this.chart = echarts.init(this.$refs.chart);
            this.updateChart();
        },
        updateChart() {
            var filteredData = this.filterDataByDateRange(this.dateRange);
            var dates = filteredData.map(item => item.date);
            var usageCounts = filteredData.map(item => item.usage);
            this.chart.setOption({
                tooltip: {
                    trigger: 'axis',
                },
                xAxis: {
                    type: 'category',
                    data: dates,
                },
                yAxis: {
                    type: 'value',
                },
                series: [
                    {
                        data: usageCounts,
                        type: 'line',
                        smooth: true,
                        areaStyle: {},
                    },
                ],
                color: ['#409EFF'],
            });
        },
        filterDataByDateRange(dateRange) {
            if (dateRange.length === 0) {
                return this.usageData;
            }
            const [start, end] = dateRange;
            return this.usageData.filter(item => {
                return new Date(item.date) >= new Date(start) && new Date(item.date) <= new Date(end);
            });
        },//echarts form
        closeDialog() {
            this.$refs.newDialogModelForm.resetFields();
        },
        submitForm() {
            this.$refs.newDialogModelForm.validate((valid) => {
                if (valid) {
                    alert('submit!');
                    this.dialogFormVisible = false
                } else {
                    console.log('error submit!!');
                    return false;
                }
            });
        },
        // New model btn
        createBtnFrom() {
            this.dialogFormTitle = '新增模型'
            this.dialogFormVisible = true
        },
        // Edit model btn
        editBtnFrom(row) {
            this.dialogFormTitle = '编辑模型'
            this.newModelForm = {
                ...row
            }
            this.dialogFormVisible = true
        },
        // Delete model 
        deleteHandle(row) {
            console.log('删除', row)
        },
        // btn batch delete
        btnBatchdelete() {
            console.log('批量删除', this.multipleSelection)
            // Loop this.multipleSelection
            // this.$refs.multipleTable.toggleRowSelection(row);
        },
        // async gets table list data
        getList() {
            this.tableData = [{
                id: 1,
                modelName: 'Prompt名称1',
                developer: '王小虎',
                isItPublic: false,
                date: '2016-05-03'
            }, {
                id: 2,
                modelName: 'Prompt名称2',
                developer: '王小虎',
                isItPublic: true,
                date: '2016-05-04'
            }, {
                id: 3,
                modelName: 'Prompt名称3',
                developer: '王小虎',
                isItPublic: false,
                date: '2016-05-05'
            }]
            this.tableTotal = 3
            // to do docking interface queryList
            //const _tableData = await service.get(`/Admin/PromptRange/Index?handler=Mofules`);
            //this.tableData = tableData.data.data.result;
        },

        // table custom row number
        indexMethod(index) {
            let { page, size } = this.queryList
            return (page - 1) * size + index + 1;
            //return  index + 1;
        },
        // table selected column
        handleSelectionChange(val) {
            let { page } = this.queryList
            this.multipleSelection[page] = val;
            // Record the number of selected pages according to the page number
            console.log('tbale 选择', this.multipleSelection)
        },
        // Pagination page size
        handleSizeChange(val) {
            this.queryList.size = val
            this.getList()
        },
        // Pagination Page number
        handleCurrentChange(val) {
            this.queryList.page = val
            this.getList()
        }
    }
});
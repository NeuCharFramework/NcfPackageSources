var app = new Vue({
    el: "#app",
    data() {
        return {
            // 查询列表 参数
            queryList: {
                page: 1,
                size: 10,
                modelName: ''
            },
            pageSizes: [10, 20, 30, 50],
            tableTotal: 0,
            tableData: [], // 模型列表
            multipleSelection: {}, // 选中的模型
            dialogFormVisible: false,
            dialogFormTitle: '新增模型',
            formLabelWidth: '',
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
        return {
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
        // 获取table列表数据
        this.getList();
    },
    methods: {
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
        // 新增模型 btn
        createBtnFrom() {
            this.dialogFormTitle = '新增模型'
            this.dialogFormVisible = true
        },
        // 编辑模型 btn
        editBtnFrom(row) {
            this.dialogFormTitle = '编辑模型'
            this.newModelForm = {
                ...row
            }
            this.dialogFormVisible = true
        },
        // 删除模型 
        deleteHandle(row) {
            console.log('删除', row)
        },
        // btn 批量删除
        btnBatchdelete() {
            console.log('批量删除', this.multipleSelection)
            // 循环 this.multipleSelection
            // this.$refs.multipleTable.toggleRowSelection(row);
        },
        // async  获取table列表数据
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
            // to do 对接接口 queryList
            //const _tableData = await service.get(`/Admin/PromptRange/Index?handler=Mofules`);
            //this.tableData = tableData.data.data.result;
        },

        // table 自定义行号
        indexMethod(index) {
            let { page, size } = this.queryList
            return (page - 1) * size + index + 1;
            //return  index + 1;
        },
        // table 选中列
        handleSelectionChange(val) {
            let { page } = this.queryList
            this.multipleSelection[page] = val;
            // 按照 页码 记录对应页选择的数量
            console.log('tbale 选择', this.multipleSelection)
        },
        // 分页 页大小
        handleSizeChange(val) {
            this.queryList.size = val
            this.getList()
        },
        // 分页 页码
        handleCurrentChange(val) {
            this.queryList.page = val
            this.getList()
        }
    }
});
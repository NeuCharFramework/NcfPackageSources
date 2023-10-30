var app = new Vue({
    el: "#app",
    data() {
        return {
            // 配置 输入 ---start
            promptid: '',// 选择靶场
            modelid: '',// 选择模型
            content: '',// prompt 输入内容
            remarks: '', // prompt 输入的备注
            // 参数设置 视图配置列表
            parameterViewList: [
                {
                    tips: '',
                    formField: 'topP',
                    label: 'Top_p',
                    value: '',
                    isSlider: true,
                    sliderMin: 0,
                    sliderMax: 1,
                    sliderStep:0.1
                },
                {
                    tips: '',
                    formField: 'temperature',
                    label: 'Temperature',
                    value: '',
                    isSlider: true,
                    sliderMin: 0,
                    sliderMax: 1,
                    sliderStep: 0.1
                },
                {
                    tips: '',
                    formField: 'maxToken',
                    label: 'MaxToken',
                    value: '',
                    isSlider: false,
                    sliderMin: 0,
                    sliderMax: 'Infinity',
                    sliderStep: 1
                },
                {
                    tips: '',
                    formField: 'frequencyPenalty',
                    label: 'Frequeny_penalty',
                    value: '',
                    isSlider: true,
                    sliderMin: -2,
                    sliderMax: 2,
                    sliderStep: 0.1
                },
                {
                    tips: '',
                    formField:'presencePenalty',
                    label: 'Presence_penalty',
                    value: '',
                    isSlider: true,
                    sliderMin: 0,
                    sliderMax: 1,
                    sliderStep: 0.1
                }
            ],
            // 配置 输入 ---end
            // 输出 ---start
            outputActive: '', // 输出列表选中查看|评分
            outputList: [
                {
                    id:1,
                    time: '2023-10-25 19:55:55',
                    costTime: '4s',
                    version: '1-4',
                    answer: '输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容',
                    scoreType: '1', // 1 ai、2手动 
                    isScore: false, // 是否已经评分
                    isScoreView:false, // 是否显示评分视图
                    scoreVal: '',
                    alResultList: [
                        {
                            id: 1,
                            label: 'Expected Answer 1',
                            value: ''
                        }, {
                            id: 2,
                            label: 'Expected Answer 2',
                            value: ''
                        }, {
                            id: 3,
                            label: 'Expected Answer 3',
                            value: ''
                        }
                    ],

                }
            ],
            // 输出 ---end
            tabsActive: '1',
            // prompt 面板
            modelSelectVal: '',
            parameter: {
                promptGroupId: 0,
                topP: 0,
                temperature: 0,
                maxToken: 0,
                frequencyPenalty: 0,
                presencePenalty: 0,
                modelid: '',
                content: ''
            },
            inputPromptVal: '',
            outPutVal: '',
            recordInfoTabsActive: '1',
            modelOpt: [],
            versionData: [],
            historyData: [],
            // 管理面板
            scoringType: '', // 1 2
            alScoreList: [],
            manualScorVal: '',
            promptSelectVal: '',
            promptDetail: {},
            promptSelectValObj: {
                ask: '123',
                answer: '123'
            },
            manageQueryList: {
                page: 1,
                size: 10,
                modelName: ''
            },
            pageSizes: [10, 20, 30, 50],
            manageTableTotal: 0,
            manageTableData: [],
            promptOpt: [],
            multipleSelection: {}
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
        this.getModelOptData()
        this.getScoringTrendData()
        //this.getHistoryData()
        //this.getVersionData()
    },
    methods: {
        // 获取模型列表
        async getModelOptData() {
            let res = await service.get('/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.GetIdAndName')
            /*console.log('getModelOptData:', res)*/
            if (res.data.success) {
                //console.log('getModelOptData:', res.data)
                let _optList = res.data.data || []
                this.modelOpt = _optList.map(item => {
                    return {
                        id: item.id,
                        label: item.name,
                        value: item.id,
                        disabled: false
                    }
                })
            } else {
                alert('error');
            }
        },
        // 打靶
        async testHandel() {
            //promptid: ''// 选择靶场
            //modelid: ''// 选择模型
            //content: ''// prompt 输入内容
            //remarks: '' // prompt 输入的备注
            //// 参数设置 视图配置列表
            //parameterViewList: [] 
            //tips: ''
            //    formField: 'topP'
            //        label: 'Top_p'
            //            value: ''
            //                isSlider: true
            //                    sliderMin: 0
            //                        sliderMax: 1
            //                            sliderStep: 0.1
            // 处理上述参数
            let res = await service.post('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Add', this.parameter)
            //console.log('testHandel res ', res.data)

            if (res.data.success) {
                //console.log('testHandel res data:', res.data.data)
                //let { resultString = '', addTime = '', costTime = '' } = res.data.data || {}
                //this.outPutVal = resultString
                // 新增到历史剧记录中
                //this.historyData.push({
                //    id: this.historyData.length + 1,
                //    time: new Date(addTime).toLocaleString(),
                //    costTime,
                //    ask: content || '',
                //    answer: resultString
                //})
            } else {
                alert('error!');
            }
        },
        // 输出 选中切换
        outputSelectSwitch(index) {
            if (this.outputActive !== '' && this.outputActive !== index) {
                this.outputList[this.outputActive].isScoreView = false
            }
            this.outputActive = index
        },
        // 显示评分视图
        showRatingView(index) {
            //event.stopPropagation()
            this.outputList[index].isScoreView = true
        },
        // 输出 切换ai评分
        alBtnScoring(index) {
            this.outputList[index].scoreType = '1'
        },
        // 输出 ai评分 增加结果行
        addAlScoring(index) {
            let _len = this.outputList[index].alResultList.length
            this.outputList[index].alResultList.push({
                id: _len + 1,
                label: `Expected Answer ${_len + 1}`,
                value: ''
            })
        },
        // 输出 切换手动评分
        manualBtnScoring(index) {
            this.outputList[index].scoreType = '2'
        },
        // 保存评分
        async saveManualScore() {
            //console.log('manualScorVal', this.promptSelectVal, this.manualScorVal)

            let res = await service.post('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Scoring', {
                promptItemId: this.promptSelectVal,
                humanScore: this.manualScorVal
            })
            //console.log('saveManualScore res ', res.data)
            if (res.data.success) {
                //console.log('testHandel res data:', res.data.data)
                //let { resultString = '', addTime = '' } = res.data.data || {}
                //this.outPutVal = resultString
                //// 新增到历史剧记录中
                //this.manageTableData.push({
                //    ...this.promptDetail,
                //    score: this.manualScorVal
                //})
            } else {
                alert('error!');
            }
        },
        // 获取评分趋势
        getScoringTrendData() {
            let scoreChart = document.getElementById('scoreChart');
            let chartOption = {
                xAxis: {
                    name: '平均分',
                    type: 'category',
                    data: ['1-4', '1-4-2', '1-4-3', '1-4-4', '1-4-5', '1-4-6', '1-4-7', '1-4-8', '1-4-9', '1-4-10', '1-4-11', '1-4-12']
                },
                yAxis: {
                    name:'版本',
                    type: 'value',
                    axisLabel: {
                        formatter: '{value} 元'
                    }
                },
                series: [{
                    name: '分数趋势图',
                    type: 'line',
                    data: [1, 8, 6, 7, 0, 0, 3, 7, 4, 9, 1, 8]
                }]
            };

            let chartInstance = echarts.init(scoreChart);
            chartInstance.setOption(chartOption);
        },





        closeDialog() {
            this.$refs.newDialogModelForm.resetFields();
        },
        // 
        async getPromptetail(item) {
            //if (item) {

            //}
            let res = await service.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Get?id=${item}`,)
            console.log('getPromptetail:', res)
            if (res.data.success) {
                //console.log('getModelOptData:', res.data)
                this.promptDetail  = res.data.data
            } else {
                alert('error');
            }
        },
        // prompt管理 批量导出
        btnBatchExport() {
            // to do 对接接口
        },
        // prompt管理 批量删除
        btnBatchDelete() {
            // to do 对接接口
        },
        // 删除 prompt btn
        async btnDeleteHandle(row) {
            
            const res = await service.request({
                method: 'delete',
                url: '/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Del',
                data: idsToDelete // 将 ID 列表作为请求体数据发送
            });
            if (res.data.success) {
                this.getList()
            } else {
                alert("error")
            }
        },
        // 详情 prompt btn
        btnDetailHandle() {

        },
        // 编辑 prompt btn
        btnEditHandle(row) {
            // this.dialogFormTitle = '编辑模型'
            // this.dialogFormVisible = true
        },
        // 面板  prompt | prompt管理 tabs 切换
        tabsChange(indexStr = '1') {
            if (indexStr === '1') {
                this.getModelOptData();
                this.getHistoryData()
                this.getVersionData()
            }
            if (indexStr === '2') {
                this.getPromptOptData()
                this.getManageData()
            }
            this.tabsActive = indexStr
        },
        // 历史记录 | 版本信息 tabs 切换
        recordInfoTabsChange(indexStr = '1') {
            if (indexStr === '1') {
                this.getHistoryData()
            }
            if (indexStr === '2') {
                this.getVersionData()
            }
            this.recordInfoTabsActive = indexStr

        },
 // 获取 
        getManageData() {
            //this.manageTableData = []
            this.manageTableTotal = 0
            // to do 对接接口 queryList
            //const _tableData = await service.get('/Admin/XncfModule/Index?handler=Mofules');
            //this.tableData = tableData.data.data.result;
        },
        // 获取 Prompt 列表
        async getPromptOptData() {
            // to do  对接接口
            
            let res = await service.get('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.GetIdAndName')
            /*console.log('getModelOptData:', res)*/
            if (res.data.success) {
                //console.log('getModelOptData:', res.data)
                let _optList = res.data.data || []
                this.promptOpt = _optList.map(item => {
                    return {
                        id: item.id,
                        label: item.name,
                        value: item.id,
                        disabled: false
                    }
                })
            } else {
                alert('error');
            }
        },
       // 获取历史记录
        getHistoryData() {
            // to do  对接接口
            this.historyData = []
        },
        // 获取版本信息
        async getVersionData() {
            let res = await service.get('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.GetVersionInfoList')
            //console.log('getVersionData:', res)
            if (res.data.success) {
                //console.log('getVersionData:', res.data)
                this.versionData = res.data.data || []
            } else {
                alert('error!');
            }
        },
        // table 自定义行号
        indexMethod(index) {
            let { page, size } = this.manageQueryList
            return (page - 1) * size + index + 1;
            //return  index + 1;
        },
        // table 选中列
        handleSelectionChange(val) {
            let { page } = this.manageQueryList
            this.multipleSelection[page] = val;
            // 按照 页码 记录对应页选择的数量
            //console.log('tbale 选择', this.multipleSelection)
        },
        // 分页 页大小
        handleSizeChange(val) {
            this.manageQueryList.size = val
            this.getManageData()
        },
        // 分页 页码
        handleCurrentChange(val) {
            this.manageQueryList.page = val
            this.getManageData()
        },
    }
});
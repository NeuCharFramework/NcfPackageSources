var app = new Vue({
    el: "#app",
    data() {
        return {
            // 配置 输入 ---start
            promptOpt: [], // prompt列表
            modelOpt: [], // 模型列表
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
                    value: 0,
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
            outputAverage: '',// 输出列表的平均分
            outputActive: '', // 输出列表选中查看|评分
            outputList: [],  // 输出列表
            chartData:[], // 图表数据
            chartInstance:null, // 图表实例
            // 输出 ---end
            // 版本记录 ---start
            versionDrawer: false,// 抽屉
            versionSearchVal: '', // 版本搜索
            versionTreeProps: {
                children: 'children',
                label: 'label'
            },
            versionTreeData:[],
            // 版本记录 ---end
            replaceFormVisible: false,
            modelFormVisible: false,
            modelFormSubmitLoading:false,
            replaceForm: {
                prefix: '',
                suffix:'',
                variableList:[]
            },
            modelForm: {
                modelType: '',
                modelName: '',
                modelAPI: '',
                modelAPIkey: ''
            },
            modelTypeOpt: [{
                value: 0,
                label: 'OpenAI'
            }, {
                value: 1,
                label: 'AzureOpenAI'
            }, {
                value: 2,
                label: 'NeuCharOpenAI'
            }, {
                value: 3,
                label: 'HugginFace'
            }],
            rules: {
                prefix: [
                    { required: true, message: '请输入前缀', trigger: 'blur' }
                ],
                suffix: [
                    { required: true, message: '请输入后缀', trigger: 'blur' }
                ],
                variableName: [
                    { required: true, message: '请输入变量名', trigger: 'blur' }
                ],
                variableValue: [
                    { required: true, message: '请输入变量值', trigger: 'blur' }
                ],
                modelType: [
                    { required: true, message: '请选择模型类型', trigger: 'change' }
                ],
                modelName: [
                    { required: true, message: '请输入模型名称', trigger: 'blur' }
                ],
                modelAPI: [
                    { required: true, message: '请输入模型API', trigger: 'blur' }
                ],
                modelAPIkey: [
                    { required: true, message: '请输入API key', trigger: 'blur' }
                ]
            },
            versionData: [],
            promptDetail: {}
        };
    },
    watch: {
        //'isExtend': {
        //    handler: function (val, oldVal) {
        //    },
        //    immediate: true,
        //    //deep:true
        //}
        versionSearchVal(val) {
            this.$refs.versionTree.filter(val);
        }
    },
    mounted() {
        this.getPromptOptData()
        this.getModelOptData()
        // 图表自适应
        let self = this;
        const viewElem = document.body;
        const resizeObserver = new ResizeObserver(() => {
            // 加个if约束，当Echarts存在时再执行resize()，否则图表不存在时运行到这会报错。
            if (self.chartInstance) {
                self.chartInstance.resize();
            }

        });
        resizeObserver.observe(viewElem);
        //this.getHistoryData()
        //this.getVersionData()
    },
    methods: {
        // 配置 获取prompt 下拉列表数据
        async getPromptOptData() {
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
        // 配置 获取模型 下拉列表数据
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
        //// 配置 新增靶场
        //addPromptBtn() { },
        //// 配置 新增模型
        //addModelBtn() { },
        // 配置 重置参数
        resetConfigurineParam() {
            //console.log('配置参数 重置:', this.parameterViewList)
        },
        // 配置 输入Prompt 重置 
        resetInputPrompt() {
            //console.log('输入Prompt 重置:', this.content)
        },
        // 配置 打靶
        async testHandel() {
            let _postData = {
                promptid: this.promptid,// 选择靶场
                modelid: this.modelid,// 选择模型
                content: this.content,// prompt 输入内容
                remarks: this.remarks, // prompt 输入的备注
            }
            this.parameterViewList.forEach(item => {
                _postData[item.formField] = item.value
            })
            // 模拟操作成功
            // 获取输出列表和平均分
            this.getOutputList()
            // 获取分数趋势图表数据
            this.getScoringTrendData()
            //let res = await service.post('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Add', _postData)
            //// console.log('testHandel res ', res.data)

            //if (res.data.success) {
            //    // 获取输出列表和平均分
            //    this.getOutputList()
            //    // 获取分数趋势图表数据
            //    this.getScoringTrendData()
            //    //console.log('testHandel res data:', res.data.data)
            //    //let { resultString = '', addTime = '', costTime = '' } = res.data.data || {}
            //    //this.outPutVal = resultString
            //    // 新增到历史剧记录中
            //    //this.historyData.push({
            //    //    id: this.historyData.length + 1,
            //    //    time: new Date(addTime).toLocaleString(),
            //    //    costTime,
            //    //    ask: content || '',
            //    //    answer: resultString
            //    //})
            //}
        },

        // 输出 分数趋势图初始化
        chartInitialization(){
            let scoreChart = document.getElementById('promptPage_scoreChart');
            let chartOption = {
                tooltip: {
                    trigger: "axis",
                    formatter: '{b}: {c}分 ',
                },
                grid: {
                    top: '35',
                    right: '35',
                    bottom: '0',
                    left: '15',
                    containLabel: true
                },
                xAxis: {
                    name: "版本", // 
                    nameGap: 5,  // 表现为上下位置
                    nameTextStyle: {
                        verticalAlign: "bottom",//标题位置
                        padding: [0, 0, 40, 0],//控制X轴标题位置
                        color: "#999",
                        fontSize: 12,
                    },
                    type: 'category',
                    boundaryGap: false,
                    data: [],
                    axisTick: {
                        show: true,
                        alignWithLabel: true,
                        lineStyle: {
                            color: '#999',
                        }

                    },
                    axisLabel: {
                        //interval: 0,
                        textStyle: {
                            fontSize: 12,
                            // fontWeight: '400',
                            color: '#999'
                        }
                    },
                    axisLine: {
                        lineStyle: {
                            color: '#999',
                        }
                    },
                    splitLine: {
                        show: false,
                        lineStyle: {
                            color: '#EBEEF5'
                        }
                    }
                },
                yAxis: {
                    name: '平均分',// 平均分
                    nameGap: 15,  // 表现为上下位置
                    nameTextStyle: {
                        verticalAlign: 'left',//标题位置
                        padding: [0, 0, 0, -40],//控制y轴标题位置
                        color: "#999",
                        fontSize: 12
                    },
                    type: 'value',
                    //axisPointer: {
                    //    show: true,
                    //},
                    axisTick: {
                        show: false,
                        alignWithLabel: true,
                    },
                    axisLabel: {
                        /*formatter: '{value}',*/
                        textStyle: {
                            fontSize: 12,
                            // fontWeight: '400',
                            color: '#999'
                        }
                    },
                    axisLine: {
                        show: false,
                        lineStyle: {
                            color: '#EBEEF5',
                        }
                    },
                    splitLine: {
                        lineStyle: {
                            color: '#EBEEF5'
                        }
                    }
                },
                series: [{
                    name: '分数趋势图',
                    type: 'line',
                    data: [],
                    itemStyle: {
                        //emphasis: {
                        //    color: "rgb(7,162,148)",
                        //    borderColor: "rgba(7,162,148,0.6)",
                        //    borderWidth: 20
                        //},
                        normal: {
                            color: "#0083ff",
                            //borderColor: "rgb(6,65,95)",
                            //lineStyle: {
                            //    color: "rgb(101,184,196)"
                            //}
                        }
                    },
                    areaStyle: {
                        normal: {
                            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{ offset: 0, color: "#0083ff" }, { offset: 1, color: "#fff" }], false)
                        }
                    }
                }]
            };
            let chartInstance = echarts.init(scoreChart);
            chartInstance.setOption(chartOption);
            this.chartInstance = chartInstance
        },
        // 输出 获取评分趋势 图表数据
        getScoringTrendData() {
            // to do 接口对接 async await
            this.chartData = {
                xData: ['1-4', '1-4-2', '1-4-3', '1-4-4', '1-4-5', '1-4-6', '1-4-7', '1-4-8', '1-4-9', '1-4-10', '1-4-11', '1-4-12'],
                vData: [1, 8, 6, 7, 0, 0, 3, 7, 4, 9, 1, 8]
            }
            // 初始化图表 接口调用成功
            this.chartInitialization()
            let _setOption = {
                xAxis: {
                    data: this.chartData.xData || []
                },
                series: [{
                    data: this.chartData.vData || []
                }]
            }
            this.chartInstance.setOption(_setOption);
        },
        // 获取输出列表
        getOutputList() {
            this.outputList = [{
                id: 1,
                time: '2023-10-25 19:55:55',
                costTime: '4s',
                version: '1-4',
                answer: '输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容输入内容',
                scoreType: '1', // 1 ai、2手动 
                isScore: false, // 是否已经评分
                isScoreView: false, // 是否显示评分视图
                scoreVal: '',
                alResultList: [
                    {
                        id: 1,
                        label: '预期结果1',
                        value: ''
                    }, {
                        id: 2,
                        label: '预期结果2',
                        value: ''
                    }, {
                        id: 3,
                        label: '预期结果3',
                        value: ''
                    }
                ]
            }]
            this.outputAverage = '8'
            // to do 接口对接 async await
        },
        // 输出 选中切换
        outputSelectSwitch(index) {
            if (this.outputActive !== '' && this.outputActive !== index) {
                this.outputList[this.outputActive].isScoreView = false
            }
            this.outputActive = index
        },
        // 输出 显示评分视图
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
                label: `预期结果${_len + 1}`,
                value: ''
            })
        },
        // 输出 切换手动评分
        manualBtnScoring(index) {
            this.outputList[index].scoreType = '2'
        },
        // 输出 保存评分
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


        // 版本记录 获取版本记录 树形数据
        getVersionRecordData() {
            // 模拟数据
            this.versionTreeData = [{
                id: 1,
                label: '一级 1',
                isPublic: false,
                children: [{
                    id: 4,
                    label: '二级 1-1',
                    isPublic: false,
                    children: [{
                        id: 9,
                        label: '三级 1-1-1',
                        isPublic: false
                    }, {
                        id: 10,
                        label: '三级 1-1-2',
                        isPublic: false
                    }]
                }]
            }, {
                id: 2,
                label: '一级 2',
                isPublic: false,
                children: [{
                    id: 5,
                    label: '二级 2-1',
                    isPublic: false
                }, {
                    id: 6,
                    label: '二级 2-2',
                    isPublic: false
                }]
            }, {
                id: 3,
                label: '一级 3',
                isPublic: false,
                children: [{
                    id: 7,
                    label: '二级 3-1',
                    isPublic: false
                }, {
                    id: 8,
                    label: '二级 3-2',
                    isPublic: false
                }]
                }]
            // to do 接口对接 async await
        },
        // 版本记录 查看
        seeVersionRecord() {
            this.versionDrawer = true
            // 重新获取数据
            this.getVersionRecordData()
        },
        // 版本记录 树形控件 过滤节点
        versionTreeFilterNode(value, data) {
            if (!value) return true;
            return data.label.indexOf(value) !== -1;
        },
        // 版本记录 抽屉关闭
        versionDrawerClose() {
            this.versionSearchVal = ''
        },
        // 版本记录 是否公开
        versionRecordIsPublic(itemData) {
            // console.log('版本记录 是否公开:', itemData)
            // to do 接口对接 async await
        },
        // 版本记录 编辑
        versionRecordEdit(itemData) {
            //console.log('版本记录 编辑:', itemData)
            // to do 接口对接 async await
            // 获取输出列表和平均分
            //this.getOutputList()
            // 获取分数趋势图表数据
            //this.getScoringTrendData()
        },
        // 版本记录 生成代码
        versionRecordGenerateCode(itemData) {
            //console.log('版本记录 生成代码:', itemData)
            // to do 接口对接 async await
        },
        // 版本记录 查看备注
        versionRecordViewNotes(itemData) {
            //console.log('版本记录 查看备注:', itemData)
            // to do 接口对接 async await
        },
        // 版本记录 删除
        versionRecordDelete(itemData) {
            //console.log('版本记录 删除:', itemData)
            // to do 接口对接 async await
        },



        // 替换 dialog 关闭
        replaceFormCloseDialog() {
            //replaceForm: {
            //    prefix: '',
            //        suffix: '',
            //            variableList: []
            //}
            this.replaceForm = {
                prefix: '',
                suffix: '',
                variableList: []
            }
            this.$refs.replaceForm.resetFields();
            
        },
        // 替换 dialog 添加变量行btn
        addVariableBtn() {
            this.replaceForm.variableList.push({
                name: '',
                value:''
            })
        },
        // 替换 dialog 删除变量行btn
        deleteVariableBtn(index) {
            this.replaceForm.variableList.splice(index,1)
        },
        // 替换 dialog 提交
        replaceFormSubmit() {
            //console.log('替换 dialog 提交:', this.replaceForm)
            // to do 接口对接 async await
        },

        // 新增模型 dialog 关闭
        modelFormCloseDialog() {
            this.modelForm = {
                modelType: '',
                modelName: '',
                modelAPI: '',
                modelAPIkey: ''
            }
            this.$refs.modelForm.resetFields();
        },
        // 新增模型 dialog 提交
        modelFormSubmitBtn() {
            this.$refs.modelForm.validate(async (valid) => {
                if (valid) {
                    this.modelFormSubmitLoading = true
                    const request = {
                        type: this.modelForm.modelType,
                        name: this.modelForm.modelName,
                        endpoint: this.modelForm.modelAPI,
                        apiKey: this.modelForm.modelAPIkey,
                    }
                    const res = await service.post('/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.Add', request)
                    if (res.data.success) {
                        this.modelFormSubmitLoading = false
                        // 重新获取模型列表
                        this.getModelOptData()
                        // 关闭dialog
                        this.modelFormVisible = false
                    } else {
                        this.modelFormSubmitLoading = false
                    }
                } else {
                    return false;
                }
            });
        },



        
        // 获取 prompt 详情
        async getPromptetail(item) {
            let res = await service.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Get?id=${item}`,)
            /*console.log('getPromptetail:', res)*/
            if (res.data.success) {
                //console.log('getModelOptData:', res.data)
                this.promptDetail  = res.data.data
            } else {
                alert('error');
            }
        },
        // 删除 prompt 
        async btnDeleteHandle(row) {
            const res = await service.request({
                method: 'delete',
                url: '/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Del',
                data: idsToDelete // 将 ID 列表作为请求体数据发送
            });
            if (res.data.success) {
                //重新获取 prompt列表
            } else {
                alert("error")
            }
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
        }
    }
});
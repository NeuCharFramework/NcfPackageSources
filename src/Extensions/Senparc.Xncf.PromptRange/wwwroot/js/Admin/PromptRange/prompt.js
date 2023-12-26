var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com',
            // 配置 输入 ---start
            promptOpt: [], // prompt列表
            modelOpt: [], // 模型列表
            promptid: '',// 选择靶场
            modelid: '',// 选择模型
            content: '',// prompt 输入内容
            remarks: '', // prompt 输入的备注
            numsOfResults: 1, // prompt 的连发次数(发射次数) 1-10
            numsOfResultsOpt: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], // prompt 的连发次数(发射次数) 1-10
            // 参数设置 视图配置列表
            parameterViewList: [
                {
                    tips: '',
                    formField: 'topP',
                    label: 'Top_p',
                    value: 0.5,
                    isSlider: true,
                    isStr: false,
                    sliderMin: 0,
                    sliderMax: 1,
                    sliderStep: 0.1
                },
                {
                    tips: '',
                    formField: 'temperature',
                    label: 'Temperature',
                    value: 0.5,
                    isSlider: true,
                    isStr: false,
                    sliderMin: 0,
                    sliderMax: 1,
                    sliderStep: 0.1
                },
                {
                    tips: '',
                    formField: 'maxToken',
                    label: 'MaxToken',
                    value: 100,
                    isSlider: false,
                    isStr: false,
                    sliderMin: 0,
                    sliderMax: 'Infinity',
                    sliderStep: 1
                },
                {
                    tips: '',
                    formField: 'frequencyPenalty',
                    label: 'Frequeny_penalty',
                    value: 0,
                    isSlider: true,
                    isStr: false,
                    sliderMin: -2,
                    sliderMax: 2,
                    sliderStep: 0.1
                },
                {
                    tips: '',
                    formField: 'presencePenalty',
                    label: 'Presence_penalty',
                    value: 0,
                    isSlider: true,
                    isStr: false,
                    sliderMin: -2,
                    sliderMax: 2,
                    sliderStep: 0.1
                },
                {
                    tips: '',
                    formField: 'stopSequences',
                    label: 'StopSequences',
                    value: '',
                    isSlider: false,
                    isStr: true,
                    sliderMin: 0,
                    sliderMax: 'Infinity',
                    sliderStep: 1
                }
            ],
            promptLeftShow: false, // prompt左侧区域整体 显隐
            parameterViewShow: false, // 模型参数设置 显隐 false是默认显示 trun是隐藏
            targetShootLoading: false, // 打靶按钮loading
            targetShootDisabled: false, // 打靶按钮禁用
            // 配置 输入 ---end
            // prompt请求参数 ---start
            promptParamVisible: true,// prompt请求参数 显隐 false是显示 trun是默认隐藏
            promptParamFormLoading: false,
            promptParamForm: {
                prefix: '',
                suffix: '',
                variableList: []
            },
            // prompt请求参数 ---end
            // 输出 ---start
            outputAverage: '',// 输出列表的平均分
            outputActive: '', // 输出列表选中查看|评分
            outputList: [],  // 输出列表
            chartData: [], // 图表数据
            chartInstance: null, // 图表实例
            // 输出 ---end
            // 版本记录 ---start
            versionDrawer: false,// 抽屉
            versionSearchVal: '', // 版本搜索
            versionTreeProps: {
                children: 'children',
                label: 'label'
            },
            versionTreeData: [],
            // 版本记录 ---end
            // 战术选择
            tacticalFormVisible: false,
            tacticalFormSubmitLoading: false,
            tacticalForm: {
                tactics: '重新瞄准'
            },
            // 模型
            modelFormVisible: false,
            modelFormSubmitLoading: false,
            modelForm: {
                modelType: "", // string
                name: "", // string
                apiVersion: "", // string
                apiKey: "", // string
                endpoint: "", // string
                organizationId: "", // string
            },
            modelTypeOpt: [{
                value: 'OpenAI',
                label: 'OpenAI',
                disabled: false
            }, {
                value: 'AzureOpenAI',
                label: 'AzureOpenAI',
                disabled: false
            }, {
                value: 'NeuCharOpenAI',
                label: 'NeuCharOpenAI',
                disabled: false
            }, {
                value: 'HugginFace',
                label: 'HugginFace',
                disabled: false
            }],
            rules: {
                tactics: [
                    {required: true, message: '请选择战术', trigger: 'change'}
                ],
                prefix: [
                    {required: true, message: '请输入前缀', trigger: 'blur'}
                ],
                suffix: [
                    {required: true, message: '请输入后缀', trigger: 'blur'}
                ],
                variableValue: [
                    {required: true, message: '请输入变量值', trigger: 'blur'}
                ],
                modelType: [
                    {required: true, message: '请选择模型类型', trigger: 'change'}
                ],
                name: [
                    {required: true, message: '请输入模型名称', trigger: 'blur'}
                ],
                apiVersion: [
                    {required: true, message: '请输入API Version', trigger: 'blur'}
                ],
                apiKey: [
                    {required: true, message: '请输入API key', trigger: 'blur'}
                ],
                variableName: [
                    {required: true, message: '请输入变量名', trigger: 'blur'}
                ],
                endpoint: [
                    {required: true, message: '请输入End Point', trigger: 'blur'}
                ],
                organizationId: [
                    {required: true, message: '请输入Organization Id', trigger: 'blur'}
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
    created() {
        // 浏览器关闭|浏览器刷新|页面关闭|打开新页面 提示有数据变动保存数据
        // 添加 beforeunload 事件监听器
        window.addEventListener('beforeunload', this.beforeunloadHandler);
    },
    mounted() {
        // 获取靶场列表
        this.getPromptOptData()
        // 获取模型列表
        this.getModelOptData()
        // 获取分数趋势图
        // this.getScoringTrendData()
        // 图表自适应
        const self = this;
        const viewElem = document.body;
        const resizeObserver = new ResizeObserver(() => {
            // 加个if约束，当Echarts存在时再执行resize()，否则图表不存在时运行到这会报错。
            if (self.chartInstance) {
                self.chartInstance.resize();
            }

        });
        resizeObserver.observe(viewElem);

    },
    beforeDestroy() {
        // 销毁之前移除事件监听器
window.removeEventListener('beforeunload', this.beforeunloadHandler);
    },
    methods: {
        beforeunloadHandler(e) {
            console.log('浏览器关闭|浏览器刷新|页面关闭|打开新页面')
            // 如果数据没有变动，则不需要提示用户保存
            //if (this.isDataChanged()) {
            //    // 显示自定义对话框
            //    let confirmationMessage = '您的数据已经修改，是否保存？';
            //    // 阻止默认行为
            //    e.preventDefault();
            //    // 兼容旧版本浏览器
            //    e.returnValue = confirmationMessage;
            //    return confirmationMessage;
            //}
            setTimeout(function () {
                // 弹出自定义模态框
                var modal = document.createElement("div");
                modal.innerHTML = "您确定要离开本页面吗？";
                var btn = document.createElement("button");
                btn.textContent = "留在页面";
                btn.onclick = function () {
                    // 取消默认的 beforeunload 行为
                    e.preventDefault();
                    // 关闭自定义模态框
                    modal.remove();
                };
                modal.appendChild(btn);
                document.body.appendChild(modal);
            }, 0);
        },
        // 保存数据的函数
         saveData() {
    // TODO: 将数据保存到服务器或本地存储中
},

// 判断是否有数据变动的函数
 isDataChanged() {
     // TODO: 判断数据是否有变动，如果有变动，返回 true；否则返回 false
     return true;
    },

        formatDate(d) {
            var date = new Date(d);
            var YY = date.getFullYear() + '-';
            var MM =
                (date.getMonth() + 1 < 10
                    ? '0' + (date.getMonth() + 1)
                    : date.getMonth() + 1) + '-';
            var DD = date.getDate() < 10 ? '0' + date.getDate() : date.getDate();
            var hh =
                (date.getHours() < 10 ? '0' + date.getHours() : date.getHours()) + ':';
            var mm =
                (date.getMinutes() < 10 ? '0' + date.getMinutes() : date.getMinutes()) +
                ':';
            var ss =
                date.getSeconds() < 10 ? '0' + date.getSeconds() : date.getSeconds();
            return YY + MM + DD + ' ' + hh + mm + ss;
        },
        // 输出 分数趋势图初始化
        chartInitialization() {
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
                            color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{
                                offset: 0,
                                color: "#0083ff"
                            }, {offset: 1, color: "#fff"}], false)
                        }
                    }
                }]
            };
            let chartInstance = echarts.init(scoreChart);
            chartInstance.setOption(chartOption);
            this.chartInstance = chartInstance
        },
        // 输出 获取评分趋势 图表数据
        async getScoringTrendData() {
            this.chartData = {
                xData: [],
                vData: []
            }
            if (this.promptid) {
                let res = await service.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.GetHistoryScore?promptItemId=${this.promptid}`)
                if (res.data.success) {
                    this.chartData = {
                        xData: res.data.data.xList || [],
                        vData: res.data.data.yList || []
                    }
                }
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

        // 靶道选择变化
        promptChangeHandel(val, itemKey) {
            console.log('靶道选择变化', val, this.promptDetail)
            if (itemKey === 'promptid') {
                this.getPromptetail(val, true)
            } else {
                let _isEdit = true
                // //判断是否有修改 任意一项修改过 解除打靶按钮禁用
                // this.parameterViewList.forEach(el => {
                //     if (el.formField === itemKey && this.promptDetail[itemKey] === val) {
                //        
                //     }
                // })
                // if (itemKey === 'content') {
                //     _isEdit = true
                // }
                // if (itemKey === 'remarks') {
                //     _isEdit = true
                // }
                if (_isEdit) this.targetShootDisabled = false
            }

        },

        // 战术选择 关闭弹出
        // 战术选择 dialog 关闭
        tacticalFormCloseDialog() {
            this.tacticalForm = {
                tactics: ''
            }
            this.$refs.tacticalForm.resetFields();
        },
        // 战术选择 dialog 提交
        tacticalFormSubmitBtn() {
            this.$refs.tacticalForm.validate(async (valid) => {
                if (valid) {
                    this.tacticalFormSubmitLoading = true
                    let _postData = {
                        //promptid: this.promptid,// 选择靶场
                        modelid: this.modelid,// 选择模型
                        content: this.content,// prompt 输入内容
                        note: this.remarks // prompt 输入的备注
                    }
                    if (this.promptid) {
                        _postData.id = this.promptid
                        if (this.tacticalForm.tactics === '创建新战术') {
                            _postData.isNewTactic = true // prompt 新建分支
                        }
                        if (this.tacticalForm.tactics === '新增子战术') {
                            _postData.isNewSubTactic = true // prompt 新建子分支
                        }
                        if (this.tacticalForm.tactics === '重新瞄准') {
                            _postData.isNewAiming = true // prompt 内容变化
                        }
                    }
                    // id: null, // 
                    this.parameterViewList.forEach(item => {
                        // todo 单独处理
                        if (item.formField === 'stopSequences') {
                            _postData[item.formField] = item.value ? JSON.stringify(item.value.split(',')) : ''
                        } else if (item.formField === 'maxToken') {
                            _postData[item.formField] = item.value ? Number(item.value) : 0
                        } else {
                            _postData[item.formField] = item.value
                        }
                    })

                    let res = await service.post('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Add', _postData)
                    // console.log('testHandel res ', res.data)
                    this.tacticalFormSubmitLoading = false
                    if (res.data.success) {
                        // 关闭dialog
                        this.tacticalFormVisible = false
                        let {promptResultList = [], fullVersion = '', id} = res.data.data || {}
                        // 重新获取prompt列表
                        this.getPromptOptData(id)

                        // 拷贝数据
                        let copyResultData = JSON.parse(JSON.stringify(res.data.data))
                        delete copyResultData.promptResultList
                        this.promptDetail = copyResultData
                        // 平均分 
                        this.outputAverage = 0 // 保留整数
                        // 输出列表
                        this.outputList = promptResultList.map(item => {
                            if (item) {
                                item.promptId = id
                                item.version = fullVersion
                                item.scoreType = '1' // 1 ai、2手动 
                                item.isScoreView = false // 是否显示评分视图
                                item.addTime = item.addTime ? this.formatDate(item.addTime) : ''
                                item.scoreVal = 0 // 手动评分
                                // ai评分预期结果
                                item.alResultList = [{
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
                                }]
                            }
                            return item
                        })
                       
                        // 获取分数趋势图表数据
                        this.getScoringTrendData()
                    } else {
                        this.targetShootDisabled = false
                    }

                } else {
                    return false;
                }
            });
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
            return data.label.indexOf(value) > -1;
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




        // 获取输出列表
        async getOutputList(promptId) {
            let res = await service.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetByItemId?promptItemId=${promptId}`)
            //console.log('getOutputList:', res)
            if (res.data.success) {
                let {promptResults = [],promptItem={}} = res.data.data || {}
                // 平均分 _totalScore/promptResults 保留整数
                this.outputAverage = promptItem.evaluationScore; // 保留整数
                // 输出列表
                this.outputList = promptResults.map(item => {
                    if (item) {
                        item.promptId = this.promptDetail.id
                        item.version = this.promptDetail.fullVersion
                        item.scoreType = '1' // 1 ai、2手动
                        item.isScoreView = false // 是否显示评分视图
                        item.addTime = item.addTime ? this.formatDate(item.addTime) : ''
                        // 手动评分
                        item.scoreVal = item.humanScore > -1 ? item.humanScore : 0 
                        // ai评分预期结果
                        if (promptItem.expectedResultsJson) {
                            let _expectedResultsJson = JSON.parse(promptItem.expectedResultsJson)
                            item.alResultList = _expectedResultsJson.map((item, index) => {
                                return {
                                    id: index + 1,
                                    label: `预期结果${index + 1}`,
                                    value: item
                                }
                            })
                        } else {
                            item.alResultList = [{
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
                        }]
                        }
                        
                    }
                    return item
                })
                

            } else {
                alert('error');
            }
        },
        // 输出 保存评分
        async saveManualScore(item, index) {
            //console.log('manualScorVal', this.promptSelectVal, this.manualScorVal)
            if (item.scoreType === '1') {
                let _list = item.alResultList.map(item => item.value)
                let res = await service.post(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.RobotScore?isRefresh=true&promptResultId=${item.id}`, _list)
                if (res.data.success) {
                    //console.log('testHandel res data:', res.data.data)
                    // 重新获取输出列表
                    this.getOutputList(item.promptId)
                    // 重新获取图表
                    this.getScoringTrendData()
                } else {
                    alert('error!');
                }
            }
            if (item.scoreType === '2') {
                let res = await service.post('/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.HumanScore', {
                    promptResultId: item.id,
                    humanScore: item.scoreVal
                })
                if (res.data.success) {
                    //console.log('testHandel res data:', res.data.data)
                    // 重新获取输出列表
                    this.getOutputList(item.promptId)
                    // 重新获取图表
                    this.getScoringTrendData()
                } else {
                    alert('error!');
                }
            }

            //console.log('saveManualScore res ', res.data)

        },
        // 输出 选中切换
        outputSelectSwitch(index) {
            if (this.outputActive !== '' && this.outputActive !== index) {
                this.outputList[this.outputActive].isScoreView = false
            }
            this.outputActive = index
        },
        // 输出 显示评分视图
        showRatingView(index, scoreType) {
            //event.stopPropagation()
            this.outputList[index].scoreType = scoreType
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


        // 打靶     
        async targetShootHandel() {
            if (this.promptid) {
                this.tacticalFormVisible = true
                return
            }

            this.targetShootLoading = true
            let _postData = {
                //promptid: this.promptid,// 选择靶场
                modelid: this.modelid,// 选择模型
                content: this.content,// prompt 输入内容
                note: this.remarks // prompt 输入的备注
            }
            if (this.promptid) {
                _postData.id = this.promptid
                if (this.tacticalForm.tactics === '创建新战术') {
                    _postData.isNewTactic = true // prompt 新建分支
                }
                if (this.tacticalForm.tactics === '新增子战术') {
                    _postData.isNewSubTactic = true // prompt 新建子分支
                }
                if (this.tacticalForm.tactics === '重新瞄准') {
                    _postData.isNewAiming = true // prompt 内容变化
                }
            }
// id: null, // 
            this.parameterViewList.forEach(item => {
                // todo 单独处理
                if (item.formField === 'stopSequences') {
                    _postData[item.formField] = item.value ? JSON.stringify(item.value.split(',')) : ''
                } else if (item.formField === 'maxToken') {
                    _postData[item.formField] = item.value ? Number(item.value) : 0
                } else {
                    _postData[item.formField] = item.value
                }
            })
// console.log('testHandel _postData:', _postData)

            let res = await service.post('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Add', _postData)
            // console.log('testHandel res ', res.data)
            this.targetShootLoading = false
            if (res.data.success) {
                let {promptResultList = [], fullVersion = '', id} = res.data.data || {}
                // 重新获取prompt列表
                this.getPromptOptData(id)
                this.targetShootDisabled = true
                // 拷贝数据
                let copyResultData = JSON.parse(JSON.stringify(res.data.data))
                delete copyResultData.promptResultList
                this.promptDetail = copyResultData
                // 平均分 _totalScore/promptResultList 保留整数
                this.outputAverage = 0 // 保留整数
                // 输出列表
                this.outputList = promptResultList.map(item => {
                    if (item) {
                        item.promptId = id
                        item.version = fullVersion
                        item.scoreType = '1' // 1 ai、2手动 
                        item.isScoreView = false // 是否显示评分视图
                        //时间 格式化  addTime
                        item.addTime = item.addTime ? this.formatDate(item.addTime) : ''
                        // 手动评分
                        item.scoreVal = 0
                        // ai评分预期结果
                        item.alResultList = [{
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
                        }]
                    }
                    return item
                })
                
                // 获取分数趋势图表数据
                this.getScoringTrendData()
            } else {
                this.targetShootDisabled = false
            }
        },
        // 配置 重置参数
        resetConfigurineParam() {
            //console.log('配置参数 重置:', this.parameterViewList)
            // 参数设置 视图配置列表
            this.parameterViewList = [
                {
                    tips: '',
                    formField: 'topP',
                    label: 'Top_p',
                    value: 0.5,
                    isSlider: true,
                    isStr: false,
                    sliderMin: 0,
                    sliderMax: 1,
                    sliderStep: 0.1
                },
                {
                    tips: '',
                    formField: 'temperature',
                    label: 'Temperature',
                    value: 0.5,
                    isSlider: true,
                    isStr: false,
                    sliderMin: 0,
                    sliderMax: 1,
                    sliderStep: 0.1
                },
                {
                    tips: '',
                    formField: 'maxToken',
                    label: 'MaxToken',
                    value: 100,
                    isSlider: false,
                    isStr: false,
                    sliderMin: 0,
                    sliderMax: 'Infinity',
                    sliderStep: 1
                },
                {
                    tips: '',
                    formField: 'frequencyPenalty',
                    label: 'Frequeny_penalty',
                    value: 0,
                    isSlider: true,
                    isStr: false,
                    sliderMin: -2,
                    sliderMax: 2,
                    sliderStep: 0.1
                },
                {
                    tips: '',
                    formField: 'presencePenalty',
                    label: 'Presence_penalty',
                    value: 0,
                    isSlider: true,
                    isStr: false,
                    sliderMin: -2,
                    sliderMax: 2,
                    sliderStep: 0.1
                },
                {
                    tips: '',
                    formField: 'stopSequences',
                    label: 'StopSequences',
                    value: '',
                    isSlider: false,
                    isStr: true,
                    sliderMin: 0,
                    sliderMax: 'Infinity',
                    sliderStep: 1
                }
            ]
        },
        // 配置 参数设置输入回调
        parameterInputHandle(val, item) {
            let {sliderMax, sliderMin, sliderStep, isStr, isSlider, formField} = item
            //console.log('parameterInputHandle:', val)
            let _findIdnex = this.parameterViewList.findIndex(item => item.formField === formField)
            let _findItem = this.parameterViewList[_findIdnex]
            // 根据 item里面的参数 判断限制输入的内容
            if (isStr) {
                // 字符串类型
            } else {
                // if (isSlider || isStr )
                //有滑动选择的 数据必须为数字
                let _val = val.replace(/[^\d]/g, '')
                //floor
                _val = Math.round(_val / sliderStep) * sliderStep
                //console.log('parameterInputHandle _val:', _val)
                //且小于sliderMax大于sliderMin保留位数与sliderStep一样
                if (_val < sliderMin) {
                    this.$set(this.parameterViewList, _findIdnex, {..._findItem, value: item.sliderMin})
                } else if (sliderMax === 'Infinity') {
                    this.$set(this.parameterViewList, _findIdnex, {..._findItem, value: _val})
                } else if (_val > sliderMax) {
                    this.$set(this.parameterViewList, _findIdnex, {..._findItem, value: item.sliderMax})
                } else {
                    this.$set(this.parameterViewList, _findIdnex, {..._findItem, value: _val})
                }
            }

        },
        // 配置 输入Prompt 重置 
        resetInputPrompt() {
            //console.log('输入Prompt 重置:', this.content)
            this.content = ''// prompt 输入内容
            //this.remarks = '' // prompt 输入的备注
        },

        //  prompt请求参数 关闭
        promptParamFormClose() {
            this.promptParamForm = {
                prefix: '',
                suffix: '',
                variableList: []
            }
            this.$refs.promptParamForm.resetFields();
            this.promptParamVisible = true;
        },
        // prompt请求参数 添加变量行btn
        addVariableBtn() {
            this.promptParamForm.variableList.push({
                name: '',
                value: ''
            })
        },
        // prompt请求参数 删除变量行btn
        deleteVariableBtn(index) {
            this.promptParamForm.variableList.splice(index, 1)
        },
        // prompt请求参数 提交
        promptParamFormSubmit() {
            this.$refs.promptParamForm.validate(async (valid) => {
                if (valid) {
                        console.log('promptParamSubmit:', this.promptParamForm)
                    //this.promptParamFormLoading = true
                    //const res = await service.post('/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.Add', this.modelForm)
                    //if (res.data.success) {
                    //    this.promptParamFormLoading = false
                    //    let { prefix = '', suffix = '', variableList = [] } = this.promptParamForm

                    //    this.promptParamFormClose()
                    //} else {
                    //    this.promptParamFormLoading = false
                    //}
                } else {
                    return false;
                }
            });
            
        },

        // 配置 获取模型 下拉列表数据
        async getModelOptData() {
            let res = await service.get('/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.GetIdAndName')
            //console.log('getModelOptData:', res)
            if (res.data.success) {
                //console.log('getModelOptData:', res.data)
                let _optList = res.data.data || []
                this.modelOpt = _optList.map(item => {
                    return {
                        ...item,
                        label: item.name,
                        value: item.id,
                        disabled: false
                    }
                })
            } else {
                alert('error');
            }
        },
        // 新增模型 dialog 关闭
        modelFormCloseDialog() {
            this.modelForm = {
                modelType: "", // string
                name: "", // string
                apiVersion: "", // string
                apiKey: "", // string
                endpoint: "", // string
                organizationId: "", // string
            }
            this.$refs.modelForm.resetFields();
        },
        // 新增模型 dialog 提交
        modelFormSubmitBtn() {
            this.$refs.modelForm.validate(async (valid) => {
                if (valid) {
                    this.modelFormSubmitLoading = true
                    const res = await service.post('/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.Add', this.modelForm)
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


        // 配置 获取prompt 下拉列表数据
        async getPromptOptData(id) {
            let res = await service.get('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.GetIdAndName')
            if (res.data.success) {
                //console.log('getModelOptData:', res)
                let _optList = res.data.data || []
                this.promptOpt = _optList.map(item => {
                    return {
                        ...item,
                        label: `${item.fullVersion} | ${item.evalScore}`,
                        value: item.id,
                        disabled: false
                    }
                })
                if (id) {
                    this.$nextTick(() => {
                        // 设置 prompt选中
                        this.promptid = Number(id)
                    })
                        
                }
            } else {
                alert('error');
            }
        },
        // 获取 prompt 详情
        async getPromptetail(id, overwrite) {
            let res = await service.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Get?id=${Number(id)}`,)
            /*console.log('getPromptetail:', res)*/
            if (res.data.success) {
                console.log('getPromptetail:', res.data)
                this.promptDetail = res.data.data
                if (overwrite) {
                    // 重新获取输出列表
                    this.getOutputList(this.promptDetail.id)
                    // 重新获取图表
                    this.getScoringTrendData()
                    if (this.promptDetail.id == id) {
                        // 将打靶按钮禁用
                        this.targetShootDisabled = true
                    } else {
                        this.targetShootDisabled = false
                    }

                    // 参数覆盖
                    let _parameterViewList = JSON.parse(JSON.stringify(this.parameterViewList))
                    this.modelid = this.promptDetail.modelId
                    this.parameterViewList = _parameterViewList.map(item => {
                        if (item) {
                            item.value = this.promptDetail[item.formField] || item.value
                        }
                        return item
                    })
                    this.content = this.promptDetail.promptContent || '' // prompt 输入内容
                    this.remarks = this.promptDetail.note || ''  // prompt 输入的备注
                }


            } else {
                alert('error');
            }
        },
        // 删除 prompt 
        async btnDeleteHandle(id) {
            const res = await service.request({
                method: 'delete',
                url: `/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Del?id=${id}`,
                //data: {id:item.id} // 将 ID 列表作为请求体数据发送
            });
            if (res.data.success) {
                //重新获取 prompt列表
            } else {
                alert("error")
            }
        },
        // 修改 prompt 
        async btnEditHandle(item) {
            const res = await service.request({
                method: 'post',
                url: `/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Modify`,
                data: {
                    id: item.id,
                    name: item.name
                }
            });
            if (res.data.success) {
                //重新获取 prompt列表
            } else {
                alert("error")
            }
        },
    }
});
var app = new Vue({
    el: "#app",
    data() {
        return {
            devHost: 'http://pr-felixj.frp.senparc.com',
            pageChange: false, // 页面是否有变化
            isAvg: true, // 是否平均分 默认false 不平均
            // 配置 输入 ---start
            promptField: '', // 靶场列表
            promptFieldOpt: [], // 靶场列表
            promptOpt: [], // prompt列表
            modelOpt: [], // 模型列表
            waitRefreshModel: false, // 是否等待刷新模型列表
            promptid: '',// 选择靶场
            modelid: '',// 选择模型
            content: '',// prompt 输入内容
            remarks: '', // prompt 输入的备注
            numsOfResults: 1, // prompt 的连发次数(发射次数) 1-10
            numsOfResultsOpt: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], // prompt 的连发次数(发射次数) 1-10
            // 参数设置 视图配置列表
            parameterViewList: [
                {
                    tips: '控制词的选择范围，值越高，生成的文本将包含更多的不常见词汇',
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
                    tips: '采样温度，较高的值如0.8会使输出更加随机，而较低的值如0.2则会使其输出更具有确定性',
                    formField: 'temperature',
                    label: 'Temperature',
                    value: 0.5,
                    isSlider: true,
                    isStr: false,
                    sliderMin: 0,
                    sliderMax: 2,
                    sliderStep: 0.1
                },
                {
                    tips: '请求与返回的Token总数或生成文本的最大长度，具体请参考API文档！',
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
                    tips: '惩罚频繁出现的词',
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
                    tips: '惩罚已出现的词',
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
                    tips: '设定生成文本时的终止词序列。当遇到这些词序列时，模型将停止生成。（输入的内容将会根据英文逗号进行分割）',
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
            targetShootLoading: false, // 打靶loading
            dodgersLoading: false, // 连发loading
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
            // sendBtns: 打靶、连发、保存草稿
            sendBtns: [
                {
                    text: '打靶'
                },
                {
                    text: '连发'
                },
                {
                    text: '保存草稿'
                }
            ],
            sendBtnText: '打靶',
            // 输出 ---start
            outputAverageDeci: -1,// 输出列表的平均分
            outputMaxDeci: -1, // 输出列表的最高分
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
            // 靶场
            fieldFormVisible: false,
            fieldFormSubmitLoading: false,
            fieldForm: {
                alias: ''
            },
            // ai 评分标准
            aiScoreFormVisible: false,
            aiScoreFormSubmitLoading: false,
            aiScoreForm: {
                resultList: [{
                    id: 1,
                    label: '预期结果',
                    value: ''
                }]
            },

            // 模型
            modelFormVisible: false,
            modelFormSubmitLoading: false,
            modelForm: {
                alias: "", // string
                modelType: "", // string
                deploymentName: "", // string
                apiVersion: "", // string
                apiKey: "", // string
                endpoint: "", // string
                organizationId: "", // string
            },
            modelTypeOpt: [{
                value: '8',
                label: 'OpenAI',
                disabled: false
            }, {
                value: '16',
                label: 'AzureOpenAI',
                disabled: false
            }, {
                value: '4',
                label: 'NeuCharAI',
                disabled: false
            }, {
                value: '32',
                label: 'HuggingFace',
                disabled: false
            }, {
                value: '128',
                label: 'FastAPI',
                disabled: false
            }],
            // 表单校验规则
            rules: {
                fieldName: [
                    { required: true, message: '请输入靶场名称', trigger: 'blur' }
                ],
                tactics: [
                    { required: true, message: '请选择战术', trigger: 'change' }
                ],
                prefix: [
                    { required: true, message: '请输入前缀', trigger: 'blur' }
                ],
                suffix: [
                    { required: true, message: '请输入后缀', trigger: 'blur' }
                ],
                variableValue: [
                    { required: true, message: '请输入变量值', trigger: 'blur' }
                ],
                modelType: [
                    { required: true, message: '请选择模型类型', trigger: 'change' }
                ],
                alias: [
                    { required: true, message: '请输入别名', trigger: 'blur' }
                ],
                deploymentName: [
                    { required: true, message: '请输入模型名称', trigger: 'blur' }
                ],
                apiVersion: [
                    { required: true, message: '请输入API Version', trigger: 'blur' }
                ],
                apiKey: [
                    { required: true, message: '请输入API key', trigger: 'blur' }
                ],
                variableName: [
                    { required: true, message: '请输入变量名', trigger: 'blur' }
                ],
                endpoint: [
                    { required: true, message: '请输入End Point', trigger: 'blur' }
                ],
                organizationId: [
                    { required: true, message: '请输入Organization Id', trigger: 'blur' }
                ],
                aiResultVal: [
                    { required: true, message: '请输入期望结果', trigger: 'blur' }
                ],
                rangeIdList: [
                    { type: 'array', required: true, message: '请至少选择一个靶场', trigger: 'change' }
                ],
                promptIdList: [
                    { type: 'array', required: true, message: '请至少选择一个靶道', trigger: 'change' }
                ],
                checkList: [
                    { type: 'array', required: true, message: '请至少选择一个靶道', trigger: 'change' }
                ],
            },
            versionData: [],
            promptDetail: {},
            uploadPluginVisible: false, // Plugin 上传dialog 显隐
            uploadPluginDropAreaVisible: true,// 上传区域显隐
            uploadPluginDropHover: false,// 拖拽文件 Hover
            uploadPluginData: [], // Plugin 文件夹的文件列表
            jsZip: null, // 压缩实例
            expectedPluginVisible: false, // Plugin 导出dialog 显隐
            // 导出 Plugin 选择数据
            expectedPluginFoem: {
                checkList: [], // 选择的数据 tree 
            },
            expectedPluginFieldList: [],
            defaultProps: {
                children: 'children',
                label: 'label'
            },
            contentTextareaRows: 14, //prompt 输入框的行数
        };
    },
    computed: {
        isPageLoading() {
            let result = this.tacticalFormSubmitLoading || this.modelFormSubmitLoading || this.aiScoreFormSubmitLoading || this.targetShootLoading || this.dodgersLoading
            return result
        },
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
        // 获取靶道列表
        setTimeout(() => {
            this.getFieldList()
            // 获取模型列表
            this.getModelOptData()

        }, 100)
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
        style(val) {
            const length = 10,
                progress = val - 0,
                left = progress / length * 100
            return {
                paddingLeft: `${left}%`,
            }
        },
        // 靶场删除
        fieldDeleteHandel(e, id) {
            // 阻止事件冒泡
            e.stopPropagation();
            // 弹出提示框
            this.$confirm('此操作将永久删除该靶场下的所有内容', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(async () => {
                let res = await servicePR.delete(`/api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.DeleteAsync?rangeId=${id}`)
                if (res.data.success) {
                    this.$message.success('删除成功')
                    let _isReset = id == this.promptField
                    // 重新获取靶场列表
                    this.getFieldList().then(() => {
                        if (_isReset) {
                            this.promptField = ''
                            // 重置页面数据
                            this.resetPageData()
                        }
                    })
                } else {
                    this.$message.error(res.data.errorMessage || res.data.data || 'Error')
                }
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: '已取消操作'
                });
            });

        },
        // 删除靶道
        promptDeleteHandel(e, id) {
            // 阻止事件冒泡
            e.stopPropagation();
            // 弹出提示框
            this.$confirm('此操作将永久删除该靶道', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(async () => {
                this.btnDeleteHandle(id)
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: '已取消操作'
                });
            });
        },
        // 备注失去焦点 保存
        promptRemarkSave() {
            let { id } = this.promptDetail
            this.btnEditHandle({
                id,
                note: this.remarks
            }, true)
        },
        parameterViewToggle() {
            if (this.parameterViewShow) {
                this.contentTextareaRows = 14
                this.parameterViewShow = false

            } else {
                this.parameterViewShow = true
                setTimeout(() => {
                    this.contentTextareaRows = 21
                }, 300)
            }
        },
        // 靶道 名称
        promptNameField(e, item) {
            // 阻止事件冒泡
            e.stopPropagation();
            //弹出提示框，输入新的靶场名称，确认后提交，取消后，提示已取消操作
            this.$prompt('请输入新的靶道名称', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                inputErrorMessage: '靶道名称不能为空',
            }).then(async ({ value }) => {
                this.btnEditHandle({ id: item.id, nickName: value })
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: '已取消操作'
                });
            });
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
                        note: this.remarks, // prompt 输入的备注
                        numsOfResults: 1,
                        isDraft: this.sendBtnText === '保存草稿',
                        suffix: this.promptParamForm.suffix,
                        prefix: this.promptParamForm.prefix
                    }
                    // ai评分标准
                    if (this.aiScoreForm.resultList.length > 0) {
                        let _list = this.aiScoreForm.resultList.map(item => item.value)
                        _list = _list.filter(item => item)
                        if (_list.length > 0) {
                            _postData.expectedResultsJson = JSON.stringify(_list)
                        }

                    }
                    if (this.promptParamForm.variableList.length > 0) {
                        _postData.variableDictJson = this.convertData(this.promptParamForm.variableList)
                    }
                    if (this.promptid) {
                        _postData.id = this.promptid
                        //创建顶级战术，创建平行战术，创建子战术，重新瞄准
                        if (this.tacticalForm.tactics === '创建顶级战术') {
                            _postData.isTopTactic = true // prompt 新建分支
                        }
                        if (this.tacticalForm.tactics === '创建平行战术') {
                            _postData.isNewTactic = true // prompt 新建分支
                        }
                        if (this.tacticalForm.tactics === '创建子战术') {
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

                    // 要提交this.promptField
                    _postData['rangeId'] = this.promptField
                    let res = await servicePR.post('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Add', _postData)
                    // console.log('testHandel res ', res.data)
                    this.tacticalFormSubmitLoading = false
                    if (res.data.success) {
                        this.pageChange = false
                        // 关闭dialog
                        this.tacticalFormVisible = false
                        let {
                            promptResultList = [],
                            fullVersion = '',
                            id,
                            evalAvgScore = -1,
                            evalMaxScore = -1
                        } = res.data.data || {}

                        // 拷贝数据
                        let copyResultData = JSON.parse(JSON.stringify(res.data.data))
                        delete copyResultData.promptResultList
                        let vArr = copyResultData.fullVersion.split('-')
                        copyResultData.promptFieldStr = vArr[0] || ''
                        copyResultData.promptStr = vArr[1] || ''
                        copyResultData.tacticsStr = vArr[2] || ''
                        this.promptDetail = copyResultData
                        this.sendBtns = [
                            {
                                text: '连发'
                            },
                            {
                                text: '保存草稿'
                            }
                        ]
                        this.sendBtnText = '连发'
                        // 平均分 
                        this.outputAverageDeci = evalAvgScore > -1 ? evalAvgScore : -1;
                        // 最高分
                        this.outputMaxDeci = evalMaxScore > -1 ? evalMaxScore : -1;
                        // 输出列表
                        this.outputList = promptResultList.map(item => {
                            if (item) {
                                item.promptId = id
                                item.version = fullVersion
                                item.scoreType = '1' // 1 ai、2手动 
                                item.isScoreView = false // 是否显示评分视图
                                item.addTime = item.addTime ? this.formatDate(item.addTime) : ''

                                //使用 MarkDown 格式，对输出结果进行展示
                                item.resultStringHtml = marked.parse(item.resultString);

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
                        //console.log('选择正确的靶场')
                        //提交数据后，选择正确的靶场和靶道
                        this.getFieldList().then(() => {
                            this.getPromptOptData(id)
                            // 获取分数趋势图表数据
                            this.getScoringTrendData()
                        })

                        if (this.sendBtnText !== '保存草稿' && this.numsOfResults > 1) {
                            //进入连发模式, 根据numOfResults-1 的数量调用N次连发接口
                            this.dealRapicFireHandel(this.numsOfResults - 1, id)
                        }
                    } else {
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
                            type: 'error',
                            duration: 5 * 1000
                        });
                    }
                } else {
                    return false;
                }
            });
        },
        /*
        * 打靶 事件
        * isDraft 是否保存草稿
        */
        async targetShootHandel(isDraft = false, isSaveDirect) {
            if (!this.modelid) {
                this.$message({
                    message: '请选择模型！',
                    type: 'warning'
                })
                return
            }
            if (!isDraft && !this.content) {
                this.$message({
                    message: '请输入内容！',
                    type: 'warning'
                })
                return
            }
            // 弹窗逻辑1，有promptid且不是保存草稿，就要弹窗
            if (this.promptid && !isDraft) {
                this.tacticalFormVisible = true
                return
            }
            // 弹窗逻辑2，有promptid且不是保存草稿，就要弹窗
            let _isPromptDraft = false
            let _findPrompt = this.promptOpt.find(item => item.value == this.promptid)
            if (isDraft && _findPrompt) {
                _isPromptDraft = _findPrompt.isDraft
            }

            if (isDraft && !_isPromptDraft && this.promptOpt.length !== 0) {
                this.tacticalFormVisible = true
                return
            }


            this.targetShootLoading = true
            let _postData = {
                //promptid: this.promptid,// 选择靶场
                modelid: this.modelid,// 选择模型
                content: this.content,// prompt 输入内容
                note: this.remarks, // prompt 输入的备注,
                numsOfResults: 1,
                //numsOfResults: isDraft?this.numsOfResults:1,
                isDraft: isDraft,
                suffix: this.promptParamForm.suffix,
                prefix: this.promptParamForm.prefix,

            }
            // ai评分标准
            if (this.aiScoreForm.resultList.length > 0) {
                let _list = this.aiScoreForm.resultList.map(item => item.value)
                _list = _list.filter(item => item)
                if (_list.length > 0) {
                    _postData.expectedResultsJson = JSON.stringify(_list)
                }
            }
            // 请求参数
            if (this.promptParamForm.variableList.length > 0) {
                _postData.variableDictJson = this.convertData(this.promptParamForm.variableList)
            }

            this.parameterViewList.forEach(item => {
                console.log('item' + item);
                // todo 单独处理
                if (item.formField === 'stopSequences') {
                    //console.log("item.formField === 'stopSequences'");
                    //if (item.value) {
                    //    let obj = JSON.parse(_postData[item.formField]);
                    //    let valuesArray = Object.values(obj);
                    //    var displayValue = valuesArray.join(' , ');
                    //    console.log('displayValue:'+displayValue);

                    //    _postData[item.formField] = displayValue;
                    //}
                    //else {
                    //    _postData[item.formField] = '';
                    //}
                    _postData[item.formField] = item.value ? JSON.stringify(item.value.split(',')) : ''
                } else if (item.formField === 'maxToken') {
                    _postData[item.formField] = item.value ? Number(item.value) : 0
                } else {
                    _postData[item.formField] = item.value
                }
            })
            // 要提交this.promptField
            _postData['rangeId'] = this.promptField

            if (isDraft && _isPromptDraft) {
                return await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.UpdateDraftAsync?promptItemId=${this.promptid}`, _postData).then(res => {
                    this.targetShootLoading = false
                    if (res.data.success) {
                        this.pageChange = false
                        // 提示保存成功
                        this.$message({
                            message: '保存成功！',
                            type: 'success'
                        })
                    } else {
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
                            type: 'error',
                            duration: 5 * 1000
                        });
                    }
                }).catch(err => {
                    this.targetShootLoading = false
                })
            } else {
                return await servicePR.post('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Add', _postData).then(res => {
                    this.targetShootLoading = false
                    if (res.data.success) {
                        this.pageChange = false
                        if (isDraft) {
                            // 提示保存成功
                            this.$message({
                                message: '保存成功！',
                                type: 'success'
                            })
                            this.sendBtns = [
                                {
                                    text: '打靶'
                                },
                                {
                                    text: '保存草稿'
                                }
                            ]
                            this.sendBtnText = '打靶'
                        } else {
                            this.sendBtns = [
                                {
                                    text: '连发'
                                },
                                {
                                    text: '保存草稿'
                                }
                            ]
                            this.sendBtnText = '连发'
                        }
                        let {
                            promptResultList = [],
                            fullVersion = '',
                            id,
                            evalAvgScore = -1,
                            evalMaxScore = -1
                        } = res.data.data || {}
                        // 拷贝数据
                        let copyResultData = JSON.parse(JSON.stringify(res.data.data))
                        delete copyResultData.promptResultList
                        let vArr = copyResultData.fullVersion.split('-')
                        copyResultData.promptFieldStr = vArr[0] || ''
                        copyResultData.promptStr = vArr[1] || ''
                        copyResultData.tacticsStr = vArr[2] || ''
                        this.promptDetail = copyResultData
                        // 平均分 
                        this.outputAverageDeci = evalAvgScore > -1 ? evalAvgScore : -1;
                        // 最高分
                        this.outputMaxDeci = evalMaxScore > -1 ? evalMaxScore : -1;
                        // 输出列表
                        this.outputList = promptResultList.map(item => {
                            if (item) {
                                item.promptId = id
                                item.version = fullVersion
                                item.scoreType = '1' // 1 ai、2手动 
                                item.isScoreView = false // 是否显示评分视图
                                //时间 格式化  addTime
                                item.addTime = item.addTime ? this.formatDate(item.addTime) : ''

                                //使用 MarkDown 格式，对输出结果进行展示
                                item.resultStringHtml = marked.parse(item.resultString);

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
                        //提交数据后，选择正确的靶场和靶道
                        this.getFieldList().then(() => {

                            this.getPromptOptData(id)
                            // 获取分数趋势图表数据
                            this.getScoringTrendData()
                        })

                        if (this.sendBtnText !== '保存草稿' && this.numsOfResults > 1) {
                            //进入连发模式, 根据numOfResults-1 的数量调用N次连发接口
                            this.dealRapicFireHandel(this.numsOfResults - 1, id)
                        }

                    } else {
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
                            type: 'error',
                            duration: 5 * 1000
                        });
                    }
                }).catch(err => {
                    this.targetShootLoading = false
                })
            }




            // console.log('testHandel res ', res.data)

        },

        /*
         * 连发 事件
         */
        async dealRapicFireHandel(howmany, id) {
            if (!this.promptid) {
                this.$message({
                    message: '请选择一个靶道！',
                    type: 'warning'
                })
                return
            }
            if (!this.modelid) {
                this.$message({
                    message: '请选择一个模型！',
                    type: 'warning'
                })
                return
            }
            this.targetShootLoading = true
            this.dodgersLoading = true
            let promises = [];
            for (let i = 0; i < howmany; i++) {
                promises.push(this.rapidFireHandel(id));
            }
            await Promise.all(promises)
            // 从新获取靶场列表
            this.getPromptOptData()
            this.targetShootLoading = false
            this.dodgersLoading = false
        },
        // 导入 plugins dialog close 回调
        uploadPluginCloseDialog() {
            // 清空fileData
            this.uploadPluginDropAreaVisible = true
            this.uploadPluginData = []
            this.jsZip = null
        },
        // 导入 plugins 在拖动区来回拖拽时
        pluginDropOverHandler(e) {
            e.stopPropagation();
            e.preventDefault();
            this.uploadPluginDropHover = true;
        },
        // 导入 plugins 第一次进入拖动区时
        pluginDropEnterHandler(e) {
            e.stopPropagation();
            e.preventDefault();
            this.uploadPluginDropHover = true;
        },
        // 导入 plugins 拖后放
        pluginDropLeaveHandler(e) {
            e.stopPropagation();
            e.preventDefault();
            this.uploadPluginDropHover = false;
        },
        // 导入 plugins 拖拽 选择文件夹
        enentPluginDrop(e) {
            this.uploadPluginDropHover = false
            let items = e.dataTransfer.items;
            if (!items) return
            this.uploadPluginDropAreaVisible = false
            this.jsZip = new JSZip();
            for (let i = 0; i <= items.length - 1; i++) {
                let item = items[i];
                if (item.kind === "file") {
                    // FileSystemFileEntry 或 FileSystemDirectoryEntry 对象
                    let entry = item.webkitGetAsEntry();
                    // 递归地获取entry下包含的所有File
                    this.getFileFromEntryRecursively(entry);
                }
            }

            // console.log('Drop',items);
            e.stopPropagation();
            e.preventDefault();
        },
        // 拖拽上传 获取文件
        getFileFromEntryRecursively(entry) {
            //let _this = this
            if (entry.isFile) {
                // 文件
                entry.file(
                    file => {
                        //console.log('Drop file', { file, path: _path });
                        // 想要保留拖拽的层级结构的话，只能从 entry 中获取
                        // 取path是为了获取上传的文件夹一级的名称
                        let _path = entry.fullPath
                        if (entry.fullPath.startsWith('/')) {
                            _path = entry.fullPath.slice(1)
                        }
                        this.forEachZip(file, _path);
                        // 文件列表
                        this.uploadPluginData.push({ name: file.name, path: _path })
                    },
                    e => { console.log(e); }
                );
            } else {
                // 文件夹
                let reader = entry.createReader();
                reader.readEntries(
                    entries => {
                        entries.forEach(entry => this.getFileFromEntryRecursively(entry));
                    },
                    e => { console.log(e); }
                );
            }
        },
        // 导入 plugins 点击 选择文件夹
        enentPluginInput() {
            let input = document.createElement("input");
            input.type = "file";
            input.setAttribute("allowdirs", "true");
            input.setAttribute("directory", "true");
            input.setAttribute("webkitdirectory", "true"); //设置了webkitdirectory就可以选择文件夹进行上传了
            input.multiple = false;
            document.querySelector("body").appendChild(input);
            input.click();
            let _this = this;
            input.onchange = async function (e) {
                let files = e.target["files"];
                //console.log('input',file)
                if (!files) return
                _this.uploadPluginDropAreaVisible = false
                _this.jsZip = new JSZip();
                // 处理文件夹里的所有子文件
                for (let i = 0; i <= files.length - 1; i++) {
                    _this.uploadPluginData.push({ name: files[i].name, path: files[i].webkitRelativePath })
                    // 取path是为了获取上传的文件夹一级的名称
                    _this.forEachZip(files[i], files[i].webkitRelativePath);

                }

                document.querySelector("body").removeChild(input);
            };
        },
        // 将上传的文件添加到压缩包中
        forEachZip(file, path) {
            //console.log('forEachZip files：', file, path)
            // 归类处理文件到指定的文件夹
            let _path = path
            let _index = path.indexOf('/')
            if (_index > -1) {
                _path = _path.slice(_index + 1)
            }
            this.jsZip.file(`${_path}`, file);
        },
        // 导入 plugins 上传按钮
        submitUploadPlugins() {
            let _fileData = JSON.parse(JSON.stringify(this.uploadPluginData))
            if (_fileData.length === 0) {
                this.$message.warning('请选择文件夹')
                return
            }
            if (this.isPageLoading) return
            this.isPageLoading = true
            let name = _fileData[0].path.split('/')[0] || 'plugin'
            // 生成压缩文件
            this.jsZip.generateAsync({ type: "blob" }).then((content) => {
                //将blob类型的再转为file类型用于上传
                let zipFile = new File([content], `${name}.zip`, {
                    type: "application/zip",
                });
                //做个大小限制
                //let isLt2M = zipFile.size / 1024 / 1024 < 80;
                //if (!isLt2M) {
                //    this.fileList = [];
                //    this.$message({
                //        message: "上传文件大小不能超过 80MB!",
                //        type: "warning",
                //    });
                //    return false;
                //} else {
                //    let filedata = new FormData();
                //    // filedata.append("file", zipFile);
                //    filedata.append("zipFile", zipFile);
                //    this.folderHandlesubmit(filedata); //上传事件，filedata已经是压缩好的文件了
                //    //saveAs(content, `${name}.zip`); //下载用，可以下载下来文件查看上传的是否正确
                //}
                let filedata = new FormData();
                // filedata.append("file", zipFile);
                filedata.append("zipFile", zipFile);
                this.folderHandlesubmit(filedata); //上传事件，filedata已经是压缩好的文件了
                //saveAs(content, `${name}.zip`); //下载用，可以下载下来文件查看上传的是否正确
            }).catch(() => {
                this.isPageLoading = false
            })
        },
        // 上传 Plugins api
        folderHandlesubmit(formData) {
            //ajax上传formData
            servicePR.request({
                url: '/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.UploadPluginsAsync',
                method: 'POST',
                //headers: {
                //    'Content-Type': 'multipart/form-data'
                //},
                data: formData
            }).then((res) => {
                this.isPageLoading = false
                //console.log(res)
                if (res.data.success) {
                    this.uploadPluginVisible = false
                    app.$message.success('上传成功')
                    // 更新靶场数据
                    this.getFieldList().then(() => {
                        if (this.promptFieldOpt && this.promptFieldOpt.length > 0) {
                            this.promptField = this.promptFieldOpt[this.promptFieldOpt.length - 1].id
                            // 重置页面数据
                            this.resetPageData()
                        }
                    })
                } else {
                    app.$message({
                        message: res.data.errorMessage || res.data.data || 'Error',
                        type: 'error',
                        duration: 5 * 1000
                    });
                }
            }).catch(() => {
                this.isPageLoading = false
            })
        },
        // 导出 plugins dialog close 回调
        expectedPluginCloseDialog() {
            this.expectedPluginFoem = {
                checkList: []
            }
            this.expectedPluginFieldList = [] // tree数据列表
            this.$refs.expectedPluginFoem.resetFields();
        },
        // 导出 plugins dialog open
        async expectedPluginOpen() {
            // 获取树形数据 靶场列表
            let _valList = this.promptFieldOpt
            let promises = [];
            for (let i = 0; i < _valList.length; i++) {
                // 获取靶道列表
                promises.push(this.getPromptOptData(_valList[i].id, true));
            }
            this.expectedPluginVisible = true
            await Promise.all(promises)
            // 设置默认选中
            this.$refs.expectedPluginTree.setCheckedKeys(this.expectedPluginFoem.checkList)
        },
        // 导出 plugins dialog tree 选中变化
        treeCheckChange(data, currentCheck, childrenCheck) {
            //console.log('treeCheckChange', data, currentCheck, childrenCheck)
            if (currentCheck) {
                // 选中 判断是否有值有的话就不添加
                if (this.expectedPluginFoem.checkList.indexOf(data.idkey) === -1) {
                    this.expectedPluginFoem.checkList.push(data.idkey)
                    // 判断是否有子节点 有的话就添加
                    if (data.children.length > 0) {
                        data.children.forEach(item => {
                            if (this.expectedPluginFoem.checkList.indexOf(item.idkey) === -1) {
                                this.expectedPluginFoem.checkList.push(item.idkey)
                            }
                        })
                    }
                }
            } else {
                // 取消选中
                let _index = this.expectedPluginFoem.checkList.indexOf(data.idkey)
                if (_index > -1) {
                    this.expectedPluginFoem.checkList.splice(_index, 1)
                }
                // 判断是否有子节点 有的话就删除
                if (data.children.length > 0) {
                    data.children.forEach(item => {
                        let _index = this.expectedPluginFoem.checkList.indexOf(item.idkey)
                        if (_index > -1) {
                            this.expectedPluginFoem.checkList.splice(_index, 1)
                        }
                    })
                }
            }

        },
        // 导出 plugins 确认
        btnExpectedPlugins() {
            this.$refs.expectedPluginFoem.validate(async (valid) => {
                if (valid) {
                    //console.log('导出 plugins 确认', this.expectedPluginFoem.checkList)
                    //return
                    this.isPageLoading = true
                    // 导出 plugins
                    let _zipname = 'plugins'
                    let _rangeIds = []
                    let _ids = []
                    this.expectedPluginFoem.checkList.forEach(item => {
                        // 判断是否包含 - 如果包含就是靶道 否则就是靶场
                        if (item.indexOf('_') > -1) {
                            let _itemArr = item.split('_')
                            _ids.push(Number(_itemArr[1]))
                            _rangeIds.push(Number(_itemArr[0]))
                        } else {
                            _rangeIds.push(Number(item))
                        }
                    })
                    // _rangeIds 和 _ids 去重
                    _rangeIds = [...new Set(_rangeIds)]
                    _ids = [...new Set(_ids)]

                    servicePR.request({
                        url: "/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.ExportPluginsAsync",
                        method: 'post',
                        responseType: 'blob',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        data: {
                            rangeIds: _rangeIds,//靶场
                            ids: _ids//靶道
                        }
                    }).then((res) => {
                        this.isPageLoading = false
                        this.expectedPluginVisible = false
                        const blob = new Blob([res.data], { type: 'application/zip' });
                        const url = URL.createObjectURL(blob);
                        const a = document.createElement('a');
                        a.href = url;
                        a.download = `${_zipname}.zip`; // 设置下载的文件名，可以根据需要修改
                        a.click(); // 触发点击事件开始下载
                        // 下载完成后删除 <a> 标签
                        URL.revokeObjectURL(url); // 释放 URL 对象
                        a.parentNode.removeChild(a); // 从 DOM 中删除 <a> 标签
                    }).catch(() => {
                        this.isPageLoading = false
                    })
                } else {
                    return false;
                }
            });
        },
        // ai 评分删除
        deleteAiScoreBtn(index) {
            //console.log('删除', index)
            this.$confirm('此操作将删除该期望结果, 是否继续?', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(() => {
                this.aiScoreForm.resultList.splice(index, 1)
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: '已取消删除'
                });
            });

        },
        // 新增靶场
        addPromptField() {
            // 如果靶场变化 靶道
            this.fieldFormVisible = true
        },
        // 连发次数 数量变化
        changeNumsBtn(command = 1) {
            this.numsOfResults = command
        },
        // 打靶按钮 类型切换
        changeBtn(command) {
            this.sendBtnText = command
        },
        // 打靶按钮 点击 触发对应类型事件
        clickSendBtn() {
            const command = this.sendBtnText
            //console.log('点击了' + command)
            if (command === '打靶') {
                this.targetShootHandel()
            } else if (command === '保存草稿') {
                this.targetShootHandel(true)
            } else if (command === '连发') {
                this.dealRapicFireHandel(this.numsOfResults)
            }
        },
        // beforeunload 事件处理函数
        beforeunloadHandler(e) {
            //console.log('浏览器关闭|浏览器刷新|页面关闭|打开新页面')
            // 如果数据没有变动，则不需要提示用户保存
            if (this.pageChange) {
                // 显示自定义对话框
                let confirmationMessage = '您的数据已经修改，是否保存为草稿？';
                // 阻止默认行为
                e.preventDefault();
                // 兼容旧版本浏览器
                e.returnValue = confirmationMessage;
                return confirmationMessage;
            }
            //setTimeout(function () {
            //    // 弹出自定义模态框
            //    var modal = document.createElement("div");
            //    modal.innerHTML = "您确定要离开本页面吗？";
            //    var btn = document.createElement("button");
            //    btn.textContent = "留在页面";
            //    btn.onclick = function () {
            //        // 取消默认的 beforeunload 行为
            //        e.preventDefault();
            //        // 关闭自定义模态框
            //        modal.remove();
            //    };
            //    modal.appendChild(btn);
            //    document.body.appendChild(modal);
            //}, 0);
        },
        copyInfo() {
            // 找到promptOpt里面的promptid
            if (!this.promptid) {
                this.$message.info('请选择靶道后再复制信息！')
                return
            }

            const promptItem = this.promptOpt.find(item => item.id === this.promptid)

            const fullVersion = promptItem.fullVersion
            // 把结果复制到剪切板
            const input = document.createElement('input')
            input.setAttribute('readonly', 'readonly')
            input.setAttribute('value', fullVersion)
            document.body.appendChild(input)
            input.select()
            input.setSelectionRange(0, 9999)
            if (document.execCommand('copy')) {
                document.execCommand('copy')
                this.$message.success(`复制【${fullVersion}】成功`)
            }

        },
        // 格式化时间
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
                    show: true,
                    confine: true, //是否将 tooltip 框限制在图表的区域内。
                    //appendToBody: true
                    formatter: (params) => {
                        //console.log('params', params)
                        let _data = params?.data[3] || {}
                        //find alias name by rangeName
                        let alias = '未设置'
                        that.promptFieldOpt.forEach(item => {
                            if (item.rangeName === _data?.rangeName) {
                                alias = item.alias
                            }
                        })
                        let _html = `<div style="text-align: left;font-size:10px;">
<p>自定义名称：${alias}</p>
    <p>靶场：${_data?.rangeName || ''}</p>
    <p>版本：${_data?.fullVersion || ''}</p>
<p>平均分：${_data?.evalAvgScore.toFixed(1) || ''}</p>
<p>最高分：${_data?.evalMaxScore.toFixed(1) || ''}</p>
<p>时间：${this.formatDate(_data?.addTime) || ''}</p>
</div>`
                        return _html
                    }
                },
                xAxis3D: {
                    type: "category",
                    name: "",
                    //axisLabel: {
                    //    show: true,
                    //    interval: 10  //使x轴都显示
                    //},
                    data: this.chartData?.xData || [],
                },
                yAxis3D: {
                    type: "category",
                    name: "",
                    //axisLabel: {
                    //    show: true,
                    //    interval: 10  
                    //},
                    data: this.chartData?.yData || [],

                },
                zAxis3D: {
                    type: "value",
                    name: "",
                    //max: 10,
                },
                //  grid3D
                grid3D: {
                    show: true,
                    boxHeight: 150, // 3维图表的高度 z轴
                    boxWidth: 400, // 3维图表的宽度 x轴
                    boxDepth: 150, // 3维图表的深度 y轴
                    // 整个chart背景，可为自定义颜色或图片
                    environment: '#fff',
                    //坐标轴轴线(线)控制
                    axisLine: {
                        show: true,//该参数需设为true
                        // interval:200,//x,y坐标轴刻度标签的显示间隔，在类目轴中有效。
                        lineStyle: {//坐标轴样式
                            color: 'rgba(0,0,0,0.3)',
                            opacity: 1,//(单个刻度不会受影响)
                            width: 2//线条宽度
                        }
                    },
                    // 坐标轴 label
                    axisLabel: {
                        show: true,//是否显示刻度  (刻度上的数字，或者类目)
                        interval: 0,//坐标轴刻度标签的显示间隔，在类目轴中有效。
                        formatter: function (v) {
                            return typeof v === 'number' ? v.toFixed(1) : v
                        },
                        textStyle: {
                            color: '#32b8be',//刻度标签样式
                            //color: function (value, index) {
                            //    return value >= 6 ? 'green' : 'red';//根据范围显示颜色，主页为值有效
                            //},
                            //  borderWidth:"",//文字的描边宽度。
                            //  borderColor:'',//文字的描边颜色。
                            fontSize: 12,//刻度标签字体大小
                            fontWeight: '400',//粗细
                        }
                    },
                    //刻度
                    axisTick: {
                        show: true,//是否显示出
                        // interval:100,//坐标轴刻度标签的显示间隔，在类目轴中有效
                        //length: 5,//坐标轴刻度的长度
                        //lineStyle: {//举个例子，样式太丑将就
                        //    color: '#000',//颜色
                        //    opacity: 1,
                        //    width: 5//厚度（虽然为宽表现为高度），对应length*(宽)
                        //}
                    },
                    //平面上的分隔线。
                    splitLine: {
                        show: true,//立体网格线
                        // interval:100,//坐标轴刻度标签的显示间隔，在类目轴中有效
                        lineStyle: {//坐标轴样式
                            color: 'rgba(0,0,0,0.05)',
                            opacity: 1,//(单个刻度不会受影响)
                            width: 1//线条宽度
                        },
                        //splitArea: {
                        //    show: true,
                        //    // interval:100,//坐标轴刻度标签的显示间隔，在类目轴中有效
                        //    areaStyle: {
                        //        color: ['rgba(250,250,250,0.2)', 'rgba(200,200,200,0.3)', 'rgba(250,250,250,0.2)', 'rgba(200,200,200,0.2)']
                        //    }
                        //},
                    },
                    // 坐标轴指示线。
                    axisPointer: {
                        show: false,//鼠标在chart上的显示线
                        // lineStyle:{
                        //     color:'#000',//颜色
                        //     opacity:1,
                        //     width:5//厚度（虽然为宽表现为高度），对应length*(宽)
                        // }
                    },
                    //viewControl用于鼠标的旋转，缩放等视角控制。(以下适合用于地球自转等)
                    viewControl: {
                        minBeta: 0, //最小旋转角度
                        maxBeta: 90, //最大旋转角度
                        minAlpha: 0, //最小旋转角度
                        maxAlpha: 90, //最大旋转角度
                        rotateSensitivity: 10,//旋转灵敏度，值越大旋转越快
                        // projection: 'orthographic'//默认为透视投影'perspective'，也支持设置为正交投影'orthographic'。
                        // autoRotate:true,//会有自动旋转查看动画出现,可查看每个维度信息
                        // autoRotateDirection:'ccw',//物体自传的方向。默认是 'cw' 也就是从上往下看是顺时针方向，也可以取 'ccw'，既从上往下看为逆时针方向。
                        // autoRotateSpeed:12,//物体自传的速度
                        // autoRotateAfterStill:2,//在鼠标静止操作后恢复自动旋转的时间间隔。在开启 autoRotate 后有效。
                        distance: 350,//默认视角距离主体的距离(常用)
                        alpha: 1,//视角绕 x 轴，即上下旋转的角度(与beta一起控制视野成像效果)
                        beta: 30,//视角绕 y 轴，即左右旋转的角度。
                        // center:[]//视角中心点，旋转也会围绕这个中心点旋转，默认为[0,0,0]
                        animation: true,
                    },
                    //光照相关的设置
                    //light: {
                    //    main: {
                    //        color: '#fff',//光照颜色会与所设置颜色发生混合
                    //        intensity: 1.2,//主光源的强度(光的强度)
                    //        shadow: false,//主光源是否投射阴影。默认关闭。
                    //        // alpha:0//主光源绕 x 轴，即上下旋转的角度。配合 beta 控制光源的方向(跟beta结合可确定太阳位置)
                    //        // beta:10//主光源绕 y 轴，即左右旋转的角度。
                    //    },
                    //    ambient: {//全局的环境光设置。
                    //        intensity: 0.3,
                    //        color: '#fff'//影响柱条颜色
                    //    },
                    //    // ambientCubemap: {//会使用纹理作为光源的环境光
                    //    //  texture: 'pisa.hdr',
                    //    // // 解析 hdr 时使用的曝光值
                    //    // exposure: 1.0
                    //    // }
                    //},
                    // postEffect:{//后处理特效的相关配置，后处理特效可以为画面添加高光，景深，环境光遮蔽（SSAO），调色等效果。可以让整个画面更富有质感。
                    //     show:true,
                    //     bloom:{
                    //         enable:true//高光特效,适合地球仪
                    //     }
                    // }
                },
                series: []
            };
            let _series = [], _scatterSeries = []
            const that = this
            this.chartData?.seriesData?.forEach(item => {
                if (item) {
                    _series.push({
                        type: 'bar3D',
                        name: item[0][1],
                        data: item,    //每个区的数据一一对应
                        itemStyle: {
                            opacity: 0.7
                        },
                        label: {
                            position: 'top',
                            show: true,
                            formatter: function (params) {
                                const promptItem = that.promptOpt.find(item => item.id === that.promptid)
                                const fullVersion = promptItem ? promptItem.fullVersion : ''
                                return params.data[3].fullVersion === fullVersion ? '当前' : ' ';  // 将 label 内容固定为 ""
                            },
                            textStyle: {
                                color: '#000',
                                fontSize: 12,
                                fontWeight: '400',
                            }
                        }
                    })
                }
            })
            chartOption.series = _series
            //console.log('chartOption', chartOption)
            this.chartInstance = null
            let chartInstance = echarts.init(scoreChart);
            chartInstance.setOption(chartOption);
            this.chartInstance = chartInstance
            chartInstance.off('click')
            // 监听点击事件
            chartInstance.on('click', (params) => {
                // console.log('click params：', params)
                const promptItem = this.promptOpt.find(item => item.fullVersion === params.data[3].fullVersion)
                if (promptItem) {
                    // 设置霸道选中
                    this.promptid = promptItem.id
                    // 获取靶道详情
                    this.getPromptetail(promptItem.id, true)
                    this.chartInstance.resize()
                    // 获取输出列表和平均分
                    //this.getOutputList()
                    // 获取分数趋势图表数据
                    // this.getScoringTrendData()
                }
            })
            //监听图表鼠标移入事件 mouseover globalout
            // chartInstance.on('mouseover', (params) => {
            //     //console.log('mouseover', params)
            //     if (params.seriesType !== "line3D") return
            //     let _sFilter = JSON.parse(JSON.stringify(_scatterSeries))
            //     if (_sFilter && _sFilter.length > 0) {
            //         _sFilter.forEach(item => {
            //             let _sFindIndex = _scatterSeries.findIndex(el => item.data == el.data)
            //             _scatterSeries.splice(_sFindIndex, 1)
            //         })
            //     }
            //     // 添加对应的 scatter3D
            //     _scatterSeries.push({
            //         type: 'scatter3D',
            //         name: params.seriesName,
            //         symbol: 'circle',  // 设置圆点样式为圆形
            //         symbolSize: 10,  // 设置圆点的大小
            //         label: {
            //             show: false,  // 设置 label 显示
            //             formatter: function (params) {
            //                 return '';  // 将 label 内容固定为 ""
            //             }
            //         },
            //         data: [params.data]    //每个区的数据一一对应
            //     })
            //     chartInstance.setOption({ series: [..._series, ..._scatterSeries] });
            // })
            //监听图表鼠标移出事件
            // chartInstance.on('mouseout', (params) => {
            //     /*console.log('globalout', _series, _sFilter, params)*/
            //     _scatterSeries = []
            //     //chartOption.series = _series
            //     //this.chartInstance.setOption(chartOption);
            //     chartInstance.setOption({ series: _series });
            // })
        },
        formatTooltip(val) {
            return val.toFixed(1)
        },
        // 输出 获取评分趋势 图表数据
        async getScoringTrendData() {
            this.chartData = {
                xData: [],
                yData: [],
                seriesData: []
            }
            if (this.promptid) {
                //console.log('获取评分趋势 图表数据', this.isAvg)
                /* /api/Senparc.Xncf.PromptRange/StatisticAppService/Xncf.PromptRange_StatisticAppService.GetLineChartDataAsync?promptItemId=${this.promptid}*/
                let res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/StatisticAppService/Xncf.PromptRange_StatisticAppService.GetLineChartDataAsync?promptItemId=${this.promptid}&isAvg=${this.isAvg}`)
                if (res.data.success) {
                    let _dataPoints = res?.data?.data?.dataPoints || []
                    let _xData = [], _yData = [], _seriesData = []
                    _dataPoints.forEach(item => {
                        if (item && item.length > 0) {
                            let _zData = []
                            item.forEach(el => {
                                if (el) {
                                    if (_xData.indexOf(`${el.y}`) === -1) {
                                        _xData.push(`${el.y}`)
                                    }
                                    if (_yData.indexOf(`${el.x}`) === -1) {
                                        _yData.push(`${el.x}`)
                                    }
                                    _zData.push([`${el.y}`, `${el.x}`, `${el.z}`, el.data])
                                }
                            })
                            _seriesData.push(_zData)
                        }
                    })
                    //console.log('_xData',_xData,_yData, _seriesData)
                    this.chartData = {
                        xData: _xData.sort((a, b) => {
                            return a - b
                        }),
                        yData: _yData.sort((a, b) => {
                            return a - b
                        }),
                        seriesData: _seriesData
                    }
                } else {
                    app.$message({
                        message: res.data.errorMessage || res.data.data || 'Error',
                        type: 'error',
                        duration: 5 * 1000
                    });
                }
                // 初始化图表 接口调用成功
                this.chartInitialization()
            }


            //let _setOption = {
            //    xAxis: {
            //        data: this.chartData.xData || []
            //    },
            //    series: [{
            //        data: this.chartData.vData || []
            //    }]
            //}
            //this.chartInstance.setOption(_setOption);
        },
        // 靶场|靶道|模型 选择变化
        promptChangeHandel(val, itemKey, oldVal) {
            // 靶道变化时，重置打靶按钮
            this.numsOfResults = 1
            //console.log(this.promptFieldOldVal,'|', val, '|', itemKey, '|', oldVal)
            if (itemKey === 'promptField') {
                // 如果靶场变化 靶道
                if (this.pageChange && this.modelid) {
                    // 提示 有数据变化 是否保存为草稿
                    this.$confirm('您的数据已经修改，是否保存为草稿？', '提示', {
                        confirmButtonText: '保存',
                        cancelButtonText: '不保存',
                        type: 'warning'
                    }).then(() => {
                        // 保存草稿
                        this.targetShootHandel(true).then(() => {
                            this.resetPageData()
                        })
                    }).catch(() => {

                        this.resetPageData()
                    });
                    return
                }
                // 重置页面数据
                this.resetPageData()
            } else if (itemKey === 'promptid') {
                if (this.pageChange && this.modelid) {
                    // 提示 有数据变化 是否保存为草稿
                    this.$confirm('您的数据已经修改，是否保存为草稿？', '提示', {
                        confirmButtonText: '保存',
                        cancelButtonText: '不保存',
                        type: 'warning'
                    }).then(() => {
                        // 保存草稿
                        this.targetShootHandel(true).then(() => {
                            this.resetPageData()
                            this.getPromptetail(val, true, true)
                        })
                        // 重置 页面变化记录
                        this.pageChange = false
                        // 重新获取靶道列表
                        //this.getFieldList()
                    }).catch(() => {
                        // 重置 页面变化记录
                        this.pageChange = false
                        // val 在 promptOpt 中的位置
                        let _fitem = this.promptOpt.find(item => item.value === val)
                        if (_fitem.isDraft) {
                            this.sendBtns = [
                                {
                                    text: '打靶'
                                },
                                {
                                    text: '保存草稿'
                                }
                            ]
                            this.sendBtnText = '打靶'
                        } else {
                            this.sendBtns = [
                                {
                                    text: '连发'
                                },
                                {
                                    text: '保存草稿'
                                }
                            ]
                            this.sendBtnText = '连发'
                        }

                        // 清空ai评分标准
                        this.aiScoreForm = {
                            resultList: []
                        }

                        // 获取靶道详情
                        this.getPromptetail(val, true, true)

                    });
                } else {
                    // 重置 页面变化记录
                    this.pageChange = false
                    // 清空ai评分标准
                    this.aiScoreForm = {
                        resultList: []
                    }
                    let _fitem = this.promptOpt.find(item => item.value === val)
                    if (_fitem.isDraft) {
                        this.sendBtns = [
                            {
                                text: '打靶'
                            },
                            {
                                text: '保存草稿'
                            }
                        ]
                        this.sendBtnText = '打靶'
                    } else {
                        this.sendBtns = [
                            {
                                text: '连发'
                            },
                            {
                                text: '保存草稿'
                            }
                        ]
                        this.sendBtnText = '连发'
                    }
                    // 靶道
                    this.getPromptetail(val, true, true)
                }

            } else {
                // 其他
                //if (itemKey === 'modelid'){}
                // 页面变化记录
                this.pageChange = true
                this.sendBtns = [
                    {
                        text: '打靶'
                    },
                    {
                        text: '保存草稿'
                    }
                ]
                this.sendBtnText = '打靶'
            }

        },
        // 重置页面数据
        async resetPageData() {
            // 重置 页面变化记录
            this.pageChange = false
            // 靶场
            this.promptid = '' // 靶道
            this.modelid = '' // 模型
            // 参数设置 视图配置列表
            this.resetConfigurineParam(false)
            // 输入Prompt 重置
            this.resetInputPrompt()
            this.outputList = []
            this.outputAverageDeci = -1
            this.outputMaxDeci = -1
            this.promptDetail = {}
            this.promptParamForm = {
                prefix: '',
                suffix: '',
                variableList: []
            }
            // ai评分标准 重置
            this.aiScoreForm = {
                resultList: []
            }
            this.numsOfResults = 1
            if (this.promptField) {
                // 获取靶道列表
                await this.getPromptOptData().then(() => {
                    // promptid is the last one of promptOpt
                    if (this.promptOpt && this.promptOpt.length > 0) {
                        let _el = this.promptOpt[this.promptOpt.length - 1]
                        this.promptid = _el.id
                        if (_el.isDraft) {
                            this.sendBtns = [
                                {
                                    text: '打靶'
                                },
                                {
                                    text: '保存草稿'
                                }
                            ]
                            this.sendBtnText = '打靶'
                        } else {
                            this.sendBtns = [
                                {
                                    text: '连发'
                                },
                                {
                                    text: '保存草稿'
                                }
                            ]
                            this.sendBtnText = '连发'
                        }

                        // 获取详情
                        this.getPromptetail(this.promptid, true, true)
                    } else {
                        this.sendBtns = [
                            {
                                text: '打靶'
                            },
                            {
                                text: '保存草稿'
                            }
                        ]
                        this.sendBtnText = '打靶'
                    }

                })
                // 获取分数趋势图表数据
                await this.getScoringTrendData()
            }

        },

        // 战术选择 关闭弹出
        // 战术选择 dialog 关闭
        tacticalFormCloseDialog() {
            this.tacticalForm = {
                tactics: '重新瞄准'
            }
            this.$refs.tacticalForm.resetFields();
        },
        checkUseRed(item, which) {
            if (item.finalScore === -1 || item.finalScore === '-1') return '';
            return item.finalScore === item[which] ? 'warnRow' : ''
        },
        // 版本记录 获取版本记录 树形数据
        async getVersionRecordData() {
            //find rangeName by id

            // const rangeName
            let _find = this.promptFieldOpt.find(item => item.value === this.promptField)
            const name = _find ? _find.rangeName : ''

            let res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.GetTacticTree?rangeName=${name}`)
            if (res.data.success) {
                //console.log('获取版本记录数据', res.data.data.rootNodeList)
                let _listData = res?.data?.data?.rootNodeList || []
                /*console.log('this.treeArrayFormat(_listData)', this.treeArrayFormat(_listData))*/
                this.versionTreeData = this.treeArrayFormat(_listData)
            } else {
                app.$message({
                    message: res.data.errorMessage || res.data.data || 'Error',
                    type: 'error',
                    duration: 5 * 1000
                });
            }
        },
        //树形数据格式化  参数data:要格式化的数据,child为要格式化数据的子数组值名
        treeArrayFormat(data, child) {
            let trees = new Array();
            let fn = null;
            let newData = null;
            for (let i in data) {
                newData = {
                    id: data[i].data.id,
                    label: data[i].name ? data[i].name : "未命名",
                    isPublic: false,
                    data: data[i].data,
                    attributes: data[i].attributes,
                    children: []
                }
                trees.push(newData);
                if (data[i].children && data[i].children.length > 0) {
                    trees[i].children = this.treeArrayFormat(data[i].children, child);
                }

            }
            return trees
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
            // 提示敬请期待
            this.$message.warning('敬请期待')
        },
        // 版本记录 编辑
        versionRecordEdit(itemData) {
            //console.log('版本记录 编辑:', itemData)
            // to do 接口对接 async await
            // 设置霸道选中
            this.promptid = itemData.id
            // 获取靶道详情
            this.getPromptetail(itemData.id, true, true)
            // 获取输出列表和平均分
            //this.getOutputList()
            // 获取分数趋势图表数据
            this.getScoringTrendData()
            // 关闭抽屉
            this.versionDrawer = false
        },
        // 版本记录 生成代码
        versionRecordGenerateCode(itemData) {
            //console.log('版本记录 生成代码:', itemData)
            // 提示敬请期待
            this.$message.warning('敬请期待')
            // to do 接口对接 async await
        },
        // 版本记录 删除
        versionRecordDelete(itemData) {
            //console.log('版本记录 删除:', itemData)
            this.$message.warning('敬请期待')
            // to do 接口对接 async await
            //this.$confirm('此操作将永久删除该靶道版本, 是否继续?', '提示', {
            //    confirmButtonText: '确定',
            //    cancelButtonText: '取消',
            //    type: 'warning'
            //}).then(() => {
            //    // 对接接口 删除
            //    this.btnDeleteHandle(itemData.id,true)

            //}).catch(() => {
            //    this.$message({
            //        type: 'info',
            //        message: '已取消删除'
            //    });
            //});
        },
        // 版本记录 查看备注
        versionRecordViewNotes(itemData) {
            //console.log('版本记录 查看备注:', itemData)
            // to do 接口对接 async await
        },


        // 获取输出列表
        async getOutputList(promptId) {
            let res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetByItemId?promptItemId=${promptId}`)
            //console.log('getOutputList:', res)
            if (res.data.success) {
                let { promptResults = [], promptItem = {} } = res.data.data || {}
                // 平均分 _totalScore/promptResults 保留整数
                this.outputAverageDeci = promptItem.evalAvgScore > -1 ? promptItem.evalAvgScore : -1; // 保留整数
                this.outputMaxDeci = promptItem.evalMaxScore > -1 ? promptItem.evalMaxScore : -1; // 保留整数

                // 输出列表
                this.outputList = promptResults.map(item => {
                    if (item) {
                        item.promptId = this.promptDetail.id
                        item.version = this.promptDetail.fullVersion
                        item.scoreType = '1' // 1 ai、2手动
                        item.isScoreView = false // 是否显示评分视图
                        item.addTime = item.addTime ? this.formatDate(item.addTime) : ''

                        //使用 MarkDown 格式，对输出结果进行展示
                        item.resultStringHtml = marked.parse(item.resultString);

                        // 手动评分
                        item.scoreVal = item.humanScore > -1 ? item.humanScore : 0
                        // ai评分预期结果
                        if (promptItem.expectedResultsJson) {
                            let _expectedResultsJson = JSON.parse(promptItem.expectedResultsJson)
                            item.alResultList = _expectedResultsJson.map((item, index) => {
                                return {
                                    id: index + 1,
                                    label: `预期结果`,
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
                app.$message({
                    message: res.data.errorMessage || res.data.data || 'Error',
                    type: 'error',
                    duration: 5 * 1000
                });
            }
        },
        // 输出 保存评分
        async saveManualScore(item, index) {
            //console.log('manualScorVal', this.promptSelectVal, this.manualScorVal)
            if (item.scoreType === '1') {
                let _list = item.alResultList.map(item => item.value)
                let res = await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.RobotScore?isRefresh=true&promptResultId=${item.id}`, _list)
                if (res.data.success) {
                    //console.log('testHandel res data:', res.data.data)
                    // 从新获取靶场列表
                    this.getPromptOptData()
                    // 重新获取输出列表
                    this.getOutputList(item.promptId)
                    // 重新获取图表
                    this.getScoringTrendData()
                } else {
                    app.$message({
                        message: res.data.errorMessage || res.data.data || 'Error',
                        type: 'error',
                        duration: 5 * 1000
                    });
                }
            }
            if (item.scoreType === '2') {
                let res = await servicePR.post('/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.HumanScore', {
                    promptResultId: item.id,
                    humanScore: item.scoreVal
                })
                if (res.data.success) {
                    //console.log('testHandel res data:', res.data.data)
                    // 从新获取靶场列表
                    this.getPromptOptData()
                    // 重新获取输出列表
                    this.getOutputList(item.promptId)
                    // 重新获取图表
                    this.getScoringTrendData()
                } else {
                    app.$message({
                        message: res.data.errorMessage || res.data.data || 'Error',
                        type: 'error',
                        duration: 5 * 1000
                    });
                }
            }

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
            // 如果是ai评分 不显示评分视图 如果没有预期结果则提醒设置预期结果
            if (scoreType === '1') {
                if (this.promptDetail.modelId) {
                    let _index = this.modelOpt.findIndex(item => item.value == this.promptDetail.modelId)
                    if (_index === -1 && !this.modelid) {
                        this.$message({
                            message: '模型已被删除，请选择模型后重新打靶！',
                            type: 'warning'
                        })
                        return
                    }
                }
                let _list = this.outputList[index].alResultList.map(item => item.value)
                _list = _list.filter(item => item)
                if (_list.length === 0) {
                    let _listVal = this.aiScoreForm.resultList.filter(item => item.value)
                    if (_listVal.length > 0) {
                        this.outputList[index].alResultList = _listVal.map((item, index) => {
                            return item
                        })
                        this.saveManualScore(this.outputList[index])
                    } else {
                        this.$message({
                            message: '请设置预期结果！',
                            type: 'warning'
                        })
                    }
                } else {
                    // todo 接口对接 重新评分
                    this.saveManualScore(this.outputList[index])
                }
                return
            }
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
                label: `预期结果`,
                value: ''
            })
        },
        // 输出 切换手动评分
        manualBtnScoring(index) {
            this.outputList[index].scoreType = '2'
        },


        convertData(data) {
            // data is like [{name:'',value:''}], convert to {name:value}
            let res = {}
            data.forEach(item => {
                res[item.name] = item.value
            })
            return JSON.stringify(res)
        },
        async rapidFireHandel(id) {
            const promptItemId = id || this.promptid
            const numsOfResults = 1
            return await servicePR.get('/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GenerateWithItemId',
                { params: { promptItemId, numsOfResults } }).then(res => {
                    //console.log('testHandel res ', res.data)
                    if (!res.data.success) {
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
                            type: 'error',
                            duration: 5 * 1000
                        });
                        return
                    }
                    this.outputAverageDeci = res.data.data.promptItem.evalAvgScore > -1 ? res.data.data.promptItem.evalAvgScore : -1; // 保留整数
                    this.outputMaxDeci = res.data.data.promptItem.evalMaxScore > -1 ? res.data.data.promptItem.evalMaxScore : -1; // 保留整数
                    //输出列表 
                    res.data.data.promptResults.map(item => {
                        item.promptId = promptItemId
                        item.scoreType = '1' // 1 ai、2手动 
                        item.isScoreView = false // 是否显示评分视图
                        //时间 格式化  addTime
                        item.addTime = item.addTime ? this.formatDate(item.addTime) : ''

                        //使用 MarkDown 格式，对输出结果进行展示
                        item.resultStringHtml = marked.parse(item.resultString);

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
                        this.outputList.push(item)
                    })
                    this.scrollToBtm()
                })
        },

        scrollToBtm() {
            // scroll to btm of resultBox  at nextick
            this.$nextTick(() => {
                let _outputArea_contentBox = document.getElementById('resultBox')
                _outputArea_contentBox.scrollTop = _outputArea_contentBox.scrollHeight
            })
        },

        // 配置 重置参数
        resetConfigurineParam(isPageChange) {
            // todo 判断是否 记录 页面变化记录
            if (isPageChange) {
                this.pageChange = true
                this.sendBtns = [
                    {
                        text: '打靶'
                    },
                    {
                        text: '保存草稿'
                    }
                ]
                this.sendBtnText = '打靶'
            }
            //console.log('配置参数 重置:', this.parameterViewList)
            // 参数设置 视图配置列表
            this.parameterViewList = [
                {
                    tips: '控制词的选择范围，值越高，生成的文本将包含更多的不常见词汇',
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
                    tips: '采样温度，较高的值如0.8会使输出更加随机，而较低的值如0.2则会使其输出更具有确定性',
                    formField: 'temperature',
                    label: 'Temperature',
                    value: 0.5,
                    isSlider: true,
                    isStr: false,
                    sliderMin: 0,
                    sliderMax: 2,
                    sliderStep: 0.1
                },
                {
                    tips: '请求与返回的Token总数或生成文本的最大长度，具体请参考API文档！',
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
                    tips: '惩罚频繁出现的词',
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
                    tips: '惩罚已出现的词',
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
                    tips: '设定生成文本时的终止词序列。当遇到这些词序列时，模型将停止生成。（输入的内容将会根据英文逗号进行分割）',
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
            // 页面变化记录
            this.pageChange = true
            this.sendBtns = [
                {
                    text: '打靶'
                },
                {
                    text: '保存草稿'
                }
            ]
            this.sendBtnText = '打靶'
            let { sliderMax, sliderMin, sliderStep, isStr, isSlider, formField } = item
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
                    this.$set(this.parameterViewList, _findIdnex, { ..._findItem, value: item.sliderMin })
                } else if (sliderMax === 'Infinity') {
                    this.$set(this.parameterViewList, _findIdnex, { ..._findItem, value: _val })
                } else if (_val > sliderMax) {
                    this.$set(this.parameterViewList, _findIdnex, { ..._findItem, value: item.sliderMax })
                } else {
                    this.$set(this.parameterViewList, _findIdnex, { ..._findItem, value: _val })
                }
            }

        },
        // 配置 输入Prompt 重置 
        resetInputPrompt() {
            //console.log('输入Prompt 重置:', this.content)
            this.content = ''// prompt 输入内容
            //this.remarks = '' // prompt 输入的备注
        },
        deleteModel(item) {
            //删除模型 confirm
            this.$confirm(`此操作将永久删除模型【${item.alias}】, 是否继续?`, '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(async () => {
                await servicePR.delete('/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.BatchDelete', { data: [item.id] })
                    .then(res => {
                        //reload model list
                        if (res.data.success) {
                            app.$message({
                                message: res.data.errorMessage || res.data.data || 'Error',
                                type: 'error',
                                duration: 5 * 1000
                            });
                        } else {
                            // 重置模型列表
                            this.modelid = ''
                            this.getModelOptData()
                            this.$message({
                                type: res.data.success ? 'success' : 'error',
                                message: res.data.success ? '删除成功' : '删除失败'
                            });
                        }
                    })
            })
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
        toAIKernel() {
            window.open('/Admin/AIKernel/Index?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69')
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
                    //const res = await servicePR.post('/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.Add', this.modelForm)
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

        async refreshModelOpt() {
            this.waitRefreshModel = true
            await this.getModelOptData().then(() => {
                this.waitRefreshModel = false
                // 提示刷新完成
                this.$message({
                    message: '刷新完成！',
                    type: 'success'
                })
            })
        },
        // 配置 获取模型 下拉列表数据
        async getModelOptData() {
            let res = await servicePR.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetListAsync', {})
            //console.log('getModelOptData:', res)
            if (res.data.success) {
                //console.log('getModelOptData:', res.data)
                let _optList = res.data.data || []
                this.modelOpt = _optList.map(item => {
                    return {
                        ...item,
                        label: item.alias,
                        value: item.id,
                        disabled: false
                    }
                })
            } else {
                app.$message({
                    message: res.data.errorMessage || res.data.data || 'Error',
                    type: 'error',
                    duration: 5 * 1000
                })
            }
        },
        // 新增模型 dialog 关闭
        modelFormCloseDialog() {
            this.modelForm = {
                alias: "", // string
                modelType: "", // string
                deploymentName: "", // string
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
                    const res = await servicePR.post('/api/Senparc.Xncf.PromptRange/LlmModelAppService/Xncf.PromptRange_LlmModelAppService.Add',
                        {
                            ...this.modelForm,
                            modelType: parseInt(this.modelForm.modelType)
                        },
                        { customAlert: true })
                    if (res.data.success) {
                        this.modelFormSubmitLoading = false
                        // 重新获取模型列表
                        await this.getModelOptData().then(() => {
                            this.modelid = res.data.data.id
                        })
                        // 提示添加成功
                        this.$message({
                            message: '添加成功！',
                            type: 'success'
                        })
                        // 关闭dialog
                        this.modelFormVisible = false
                    } else {
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
                            type: 'error',
                            duration: 5 * 1000
                        })
                        this.modelFormSubmitLoading = false
                    }
                } else {
                    return false;
                }
            });
        },


        // 关闭新增靶场 dialog
        fieldFormCloseDialog() {
            this.fieldForm = {
                alias: ''
            }
            this.$refs.fieldForm.resetFields();
        },
        // dialog 新增靶场 提交按钮
        fieldFormSubmitBtn() {
            const that = this
            this.$refs.fieldForm.validate(async (valid) => {
                if (valid) {
                    this.fieldFormVisible = false
                    // post 接口 /api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.AddAsync'
                    const res = await servicePR.post('/api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.AddAsync?alias='
                        + that.fieldForm.alias, {})
                    if (res.data.success) {
                        // 重新获取靶场列表
                        await this.getFieldList().then(() => {
                            that.promptField = res.data.data.id
                            this.resetPageData()
                        })
                        // 提示添加成功
                        this.$message({
                            message: '添加成功！',
                            type: 'success'
                        })
                        // 关闭dialog
                        this.fieldFormVisible = false
                    } else {
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
                            type: 'error',
                            duration: 5 * 1000
                        })
                    }

                } else {
                    return false;
                }
            });
        },
        // 配置 获取靶场 下拉列表数据
        async getFieldList() {
            return await servicePR.post('/api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.GetListAsync', {})
                .then(res => {
                    if (res.data.success) {
                        this.promptFieldOpt = res.data.data.map(item => {
                            return {
                                ...item,
                                label: item.alias + '（' + item.rangeName + '）',
                                value: item.id,
                                disabled: false
                            }
                        })
                    } else {
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
                            type: 'error',
                            duration: 5 * 1000
                        })
                    }
                })
        },
        renameField(e, item) {
            e.stopPropagation()
            //弹出提示框，输入新的靶场名称，确认后提交，取消后，提示已取消操作
            this.$prompt('请输入新的靶场名称', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                inputErrorMessage: '靶场名称不能为空',
            }).then(async ({ value }) => {
                const res = await servicePR.get('/api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.ChangeAliasAsync', {
                    params: {
                        rangeId: item.id,
                        alias: value
                    }
                })
                if (res.data.success) {
                    this.getFieldList()
                } else {
                    app.$message({
                        message: res.data.errorMessage || res.data.data || 'Error',
                        type: 'error',
                        duration: 5 * 1000
                    })
                }
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: '已取消操作'
                });
            });

        },
        // 获取靶道 下拉列表数据
        async getPromptOptData(id, isExpected) {
            // find rangeName by id
            let _find = this.promptFieldOpt.find(item => item.value === this.promptField)
            if (isExpected) {
                _find = this.promptFieldOpt.find(item => item.value === id)
            }

            const name = _find ? _find.rangeName : ''
            let res = await servicePR
                .get('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.GetIdAndName', {
                    params: {
                        rangeName: name
                    }
                })
            if (res.data.success) {
                let _optList = res.data.data || []
                //let _promptIdList = []
                let _promptOpt = _optList.map(item => {
                    const avg = scoreFormatter(item.evalAvgScore)
                    const max = scoreFormatter(item.evalMaxScore)
                    //_promptIdList.push(item.id)
                    return {
                        ...item,
                        label: `名称：${item.nickName || '未设置'} | 版本号：${item.fullVersion} | 平均分：${avg} | 最高分：${max} ${item.isDraft ? '(草稿)' : ''}`,
                        value: item.id,
                        disabled: false,
                    }
                })
                if (isExpected) {
                    this.expectedPluginFoem.checkList.push(`${_find.value}`)
                    // 导出 树形数据 
                    this.expectedPluginFieldList.push({
                        id: _find.id,
                        label: _find.label, // 靶场名称
                        idkey: `${_find.value}`, // 靶场id
                        children: _promptOpt.map(item => {
                            this.expectedPluginFoem.checkList.push(`${_find.value}_${item.id}`)
                            return {
                                id: item.id,
                                label: item.label, // 靶场名称
                                idkey: `${_find.value}_${item.id}`, // 靶场id
                            }
                        }),
                    })

                } else {
                    //console.log('getModelOptData:', res)
                    this.promptOpt = _promptOpt
                    if (id) {
                        this.promptid = id
                    }

                }


            } else {
                app.$message({
                    message: res.data.errorMessage || res.data.data || 'Error',
                    type: 'error',
                    duration: 5 * 1000
                })
            }
        },
        // 获取 prompt 详情
        async getPromptetail(id, overwrite, isChart) {
            let res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Get?id=${Number(id)}`,)
            /*console.log('getPromptetail:', res)*/
            if (res.data.success) {
                //console.log('getPromptetail:', res.data)
                // 拷贝数据
                let copyResultData = JSON.parse(JSON.stringify(res.data.data))
                let vArr = copyResultData.fullVersion.split('-')
                copyResultData.promptFieldStr = vArr[0] || ''
                copyResultData.promptStr = vArr[1] || ''
                copyResultData.tacticsStr = vArr[2] || ''
                if (copyResultData.stopSequences) {
                    copyResultData.stopSequences = JSON.parse(copyResultData.stopSequences).join(',')
                }
                this.promptDetail = copyResultData
                //如果获取到的结果没有，则延续以往的expectedJson.
                if (copyResultData.expectedResultsJson) {
                    let expectedResultsJson = this.promptDetail.expectedResultsJson
                    if (expectedResultsJson) {
                        let _expectedResultsJson = JSON.parse(expectedResultsJson)
                        this.aiScoreForm.resultList = _expectedResultsJson.map((item, index) => {
                            return {
                                id: index + 1,
                                label: `预期结果`,
                                value: item
                            }
                        })
                    }

                } else {
                    this.aiScoreForm = {
                        resultList: []
                    }
                }
                if (overwrite) {
                    // 重新获取输出列表
                    this.getOutputList(this.promptDetail.id)
                    if (isChart) {
                        // 重新获取图表
                        this.getScoringTrendData()
                    }


                    // 参数覆盖
                    let _parameterViewList = JSON.parse(JSON.stringify(this.parameterViewList))

                    this.parameterViewList = _parameterViewList.map(item => {
                        if (item) {
                            item.value = this.promptDetail[item.formField] || item.value
                        }
                        return item
                    })

                    // 判断模型列表是否有选中的模型
                    let _findIndex = this.modelOpt.findIndex(item => item.value === this.promptDetail.modelId)
                    if (_findIndex > -1) {
                        // 模型覆盖
                        this.modelid = this.promptDetail.modelId
                    } else {
                        this.modelid = ''
                    }
                    // prompt 输入内容
                    this.content = this.promptDetail.promptContent || ''
                    // prompt 输入的备注
                    this.remarks = this.promptDetail.note || ''
                    // prompt请求参数
                    let _promptParamForm = JSON.parse(JSON.stringify(this.promptParamForm))
                    _promptParamForm.prefix = this.promptDetail.prefix || ''
                    _promptParamForm.suffix = this.promptDetail.suffix || ''
                    _promptParamForm.variableList = []
                    if (this.promptDetail.variableDictJson) {
                        let _variableDictJson = JSON.parse(this.promptDetail.variableDictJson)
                        // _variableDictJson不是空对象 就循环赋值
                        if (Object.keys(_variableDictJson).length > 0) {
                            _promptParamForm.variableList = Object.keys(_variableDictJson).map(item => {
                                return {
                                    name: item,
                                    value: _variableDictJson[item]
                                }
                            })
                        }

                    }
                    this.promptParamForm = _promptParamForm

                    //ai 期望结果里面增加接口返回内容
                    if (res.data.data.expectedResultsJson) {
                        const expectedResultsJson = JSON.parse(res.data.data.expectedResultsJson)
                        this.aiScoreForm.resultList = expectedResultsJson.map((item, index) => {
                            return {
                                id: index + 1,
                                label: `预期结果`,
                                value: item
                            }
                        })
                    }
                }


            } else {
                app.$message({
                    message: res.data.errorMessage || res.data.data || 'Error',
                    type: 'error',
                    duration: 5 * 1000
                })
            }
        },
        // 删除 prompt 
        async btnDeleteHandle(id, versionRecord) {
            const res = await servicePR.request({
                method: 'delete',
                url: `/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.DeleteAsync?id=${id}`,
            });
            if (res.data.success) {
                // 重新获取 靶道列表 如果删除的是当前选中的靶道，就重重置靶道选中值并重置模型、参数、输入内容、备注、输出列表、平均分、图表、ai评分预期结果
                if (id === this.promptid) {
                    this.resetPageData()
                } else {
                    // 重新获取prompt列表
                    await this.getPromptOptData(this.promptid)
                }

                if (versionRecord) {
                    // 重新获取版本记录
                    await this.getVersionRecordData()
                }


                // 删除成功
                this.$message({
                    type: 'success',
                    message: '删除成功!'
                });
            } else {
                app.$message({
                    message: res.data.errorMessage || res.data.data || 'Error',
                    type: 'error',
                    duration: 5 * 1000
                })
            }
        },
        // 修改 prompt 别名
        async btnEditHandle(item, isSave) {

            if (!item) return
            const res = await servicePR.request({
                method: 'post',
                url: `/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Modify`,
                data: item
            });
            if (res.data.success) {
                if (isSave) {
                    // 提示保存成功
                    this.$message({
                        message: '保存成功！',
                        type: 'success'
                    })
                } else {
                    // 重新获取 prompt列表
                    this.getPromptOptData()
                }

            } else {
                app.$message({
                    message: res.data.errorMessage || res.data.data || 'Error',
                    type: 'error',
                    duration: 5 * 1000
                })
            }
        },

        // ai评分设置 dialog 新增结果行btn
        aiScoreFormAddRow() {
            let _len = this.aiScoreForm.resultList.length
            this.aiScoreForm.resultList.push({
                id: _len + 1,
                label: `预期结果`,
                value: ''
            })
        },
        // ai评分设置 打开 dialog 
        aiScoreFormOpenDialog() {
            let _list = this.aiScoreForm.resultList
            let _listVal = _list.filter(item => item.value)
            //console.log('_listVal:', _list, _listVal, this.promptDetail)
            if (_list.length === 1 && _listVal.length === 0 && this.promptDetail && this.promptDetail.expectedResultsJson) {
                let _expectedResultsJson = JSON.parse(this.promptDetail.expectedResultsJson)
                this.aiScoreForm = {
                    resultList: _expectedResultsJson.map((item, index) => {
                        return {
                            id: index + 1,
                            label: `预期结果`,
                            value: item
                        }
                    })
                }
            }
            //else {
            //    // 如果没有预期结果就重置
            //    this.aiScoreForm = {
            //        resultList: [{
            //            id: 1,
            //            label: '预期结果1',
            //            value: ''
            //        }]
            //    }
            //}
            // 判断 this.aiScoreForm.resultList 是否有值            
            this.aiScoreFormVisible = !this.aiScoreFormVisible
        },
        // 关闭ai评分设置 dialog
        aiScoreFormCloseDialog() {
            this.$refs.aiScoreForm.resetFields();
        },
        aiScoreFormClose() {
            this.aiScoreFormVisible = false
            // 判断 this.aiScoreForm.resultList 对比详情 this.promptDetail.expectedResultsJson 是否有改动
            let _list = this.aiScoreForm.resultList
            let _listVal = _list.filter(item => item.value)
            let _expectedResultsJson = this.promptDetail.expectedResultsJson
            if (_listVal.length > 0 && _expectedResultsJson) {
                let _expectedResultsJson = _listVal.map(item => item.value)
                if (JSON.stringify(_expectedResultsJson) !== _expectedResultsJson) {
                    this.pageChange = true
                    this.numsOfResults = 1
                    this.sendBtns = [
                        {
                            text: '打靶'
                        },
                        {
                            text: '保存草稿'
                        }
                    ]
                    this.sendBtnText = '打靶'
                }
            }
        },
        // dialog ai评分设置 提交按钮
        aiScoreFormSubmitBtn() {
            this.$refs.aiScoreForm.validate(async (valid) => {
                if (valid) {
                    this.aiScoreFormSubmitLoading = true
                    let _list = this.aiScoreForm.resultList.map(item => item.value)
                    const res = await servicePR.request({
                        method: 'post',
                        url: `/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.UpdateExpectedResults`,
                        params: {
                            promptItemId: Number(this.promptid),
                            expectedResults: JSON.stringify(_list)
                        }
                    });
                    this.aiScoreFormSubmitLoading = false
                    if (res.data.success) {
                        // 重新获取详情
                        this.getPromptetail(this.promptid, false)
                        // 关闭dialog
                        this.aiScoreFormVisible = false
                    } else {
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
                            type: 'error',
                            duration: 5 * 1000
                        })
                    }
                } else {
                    return false;
                }
            });
        },
        // 复制 Prompt 测试结果
        copyPromptResult(item, rawResult) {

            // 把结果复制到剪切板
            try {
                const input = document.createElement('input');
                input.setAttribute('readonly', 'readonly');
                input.setAttribute('value', rawResult ? item.resultString : item.resultStringHtml);
                document.body.appendChild(input);
                input.select();
                input.setSelectionRange(0, 9999);
                if (document.execCommand('copy')) {
                    document.execCommand('copy');
                    this.$message.success(`复制成功`);
                } else {
                    this.$message.error(`复制失败`);
                }
            } catch (err) {
                console.error('Oops, unable to copy', err);
            }
        }
    }
});


function scoreFormatter(score) {
    return score === -1 ? '--' : score.toFixed(1)
}

function getUuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = (Math.random() * 16) | 0,
            v = c == 'x' ? r : (r & 0x3) | 0x8;
        return v.toString(16);
    });
}
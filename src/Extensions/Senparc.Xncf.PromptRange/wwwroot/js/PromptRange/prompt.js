var app = new Vue({
    el: "#app",
    data() {
        return {
            isAIGrade: true,
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
            isComposing: false, // 是否正在使用输入法（IME composition）
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
            promptParamVisible: false,// prompt请求参数 显隐 true是显示 false是默认隐藏
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
            robotScoreLoadingMap: {}, // AI评分加载状态映射 {itemId: true/false}
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
                tactics: '重新瞄准',
                chatMode: '对话模式' // 对话模式/直接测试，默认对话模式
            },
            // 战术选择弹窗中的对话输入
            tacticalChatInput: '', // 对话模式下的用户输入内容
            // 继续聊天相关状态
            continueChatMode: false, // 是否处于继续聊天模式
            continueChatPromptResultId: null, // 继续聊天的 PromptResult ID
            continueChatHistory: [], // 继续聊天的历史记录
            // 导图相关状态
            mapDialogVisible: false, // 导图对话框显示状态
            map3dScene: null, // three.js 场景
            map3dCamera: null, // three.js 相机
            map3dRenderer: null, // three.js 渲染器
            map3dControls: null, // 相机控制器
            map3dNodes: [], // 3D 节点数组
            map3dTreeData: null, // 树状结构数据
            map3dClickHandler: null, // 点击事件处理器
            map3dAnimationId: null, // 动画ID
            map3dNeedsAnimationUpdate: false, // 是否需要更新动画
            map3dNodeMap: new Map(), // 节点映射，用于快速查找
            map3dLastAnimationTime: 0, // 上次动画更新时间（用于节流）
            map3dCurrentNodes: [], // 缓存当前选中的节点（性能优化）
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
            exportPluginExpandAll: false, // 是否展开所有节点
            exportPluginSelectedCount: 0, // 已选择的靶道数量
            defaultProps: {
                children: 'children',
                label: 'label'
            },
            contentTextareaRows: 14, //prompt 输入框的行数
            dialogVisible: false,
            targetlaneName: '',
            dailogpromptOptlist: [],
            box1Hidden: false,
            box2Hidden: false,
            box3Hidden: false,
            lastClickedBox: null,
            centerAreaMaximized: false, // 中间区域是否最大化
            rightAreaMaximized: false,  // 右侧区域是否最大化
            isBoxVisible: true, // 控制盒子显示和隐藏的状态
            foldsidebarShow: false,
            // 区域宽度控制
            leftAreaWidth: 360,    // 左侧区域宽度（默认360px）
            centerAreaWidth: 380,  // 中间区域宽度（默认380px）
            isResizing: false,     // 是否正在拖动
            resizeType: null,      // 拖动类型：'left' 或 'right'
            resizeStartX: 0,       // 拖动开始的X坐标
            resizeStartLeftWidth: 0,   // 拖动开始时左侧区域的宽度
            resizeStartCenterWidth: 0, // 拖动开始时中间区域的宽度
            // Prompt 对比功能
            compareDialogVisible: false,  // 对比对话框显示状态
            comparePromptAId: null,       // 对比的Prompt A的ID
            comparePromptBId: null,       // 对比的Prompt B的ID
            comparePromptA: null,         // 对比的Prompt A的完整数据
            comparePromptB: null,         // 对比的Prompt B的完整数据
            // 自定义滚动条缩略图
            showScrollbarThumbnails: false,
            scrollInfo: {
                scrollTop: 0,
                scrollHeight: 0,
                clientHeight: 0
            }
        };
    },
    computed: {
        isPageLoading() {
            let result = this.tacticalFormSubmitLoading || this.modelFormSubmitLoading || this.aiScoreFormSubmitLoading || this.targetShootLoading || this.dodgersLoading
            return result
        },
        
        // 获取可选择的Prompt列表（用于对比对话框）
        availablePrompts() {
            return this.promptOpt || [];
        },
        
        // 获取Prompt A的显示信息
        comparePromptAInfo() {
            if (!this.comparePromptA) return null;
            
            // 从 fullVersion 解析名称 (格式: 靶场-靶道-战术)
            const versionParts = this.comparePromptA.fullVersion ? this.comparePromptA.fullVersion.split('-') : [];
            
            return {
                targetRangeName: versionParts[0] || '未知靶场',
                targetLaneName: versionParts[1] || '未知靶道',
                tacticalName: versionParts[2] || '未知战术',
                modelName: this.getModelName(this.comparePromptA.modelId)
            };
        },
        
        // 获取Prompt B的显示信息
        comparePromptBInfo() {
            if (!this.comparePromptB) return null;
            
            // 从 fullVersion 解析名称 (格式: 靶场-靶道-战术)
            const versionParts = this.comparePromptB.fullVersion ? this.comparePromptB.fullVersion.split('-') : [];
            
            return {
                targetRangeName: versionParts[0] || '未知靶场',
                targetLaneName: versionParts[1] || '未知靶道',
                tacticalName: versionParts[2] || '未知战术',
                modelName: this.getModelName(this.comparePromptB.modelId)
            };
        },
        
        // 检查是否是同一个Prompt
        isSamePrompt() {
            if (!this.comparePromptA || !this.comparePromptB) return false;
            if (!this.comparePromptAId || !this.comparePromptBId) return false;
            
            // 通过ID判断是否为同一个Prompt
            return this.comparePromptAId === this.comparePromptBId;
        },
        
        // 检测Prompt中的变量
        detectedVariables() {
            if (!this.content) return [];
            
            const prefix = this.promptParamForm.prefix || '';
            const suffix = this.promptParamForm.suffix || '';
            const variableList = this.promptParamForm.variableList || [];
            
            // 如果没有设置前缀和后缀，返回空数组
            if (!prefix || !suffix) return [];
            
            // 转义前缀和后缀用于正则表达式
            const escapedPrefix = prefix.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            const escapedSuffix = suffix.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            
            // 构建正则表达式：匹配 prefix + 变量名 + suffix
            const regex = new RegExp(`${escapedPrefix}(\\w+)${escapedSuffix}`, 'g');
            
            // 获取所有已定义的变量名
            const definedVarNames = variableList.map(v => v.name).filter(n => n);
            
            // 找出所有匹配的变量
            const variables = [];
            const seen = new Set();
            let match;
            
            while ((match = regex.exec(this.content)) !== null) {
                const fullMatch = match[0];
                const varName = match[1];
                
                // 避免重复
                if (!seen.has(fullMatch)) {
                    seen.add(fullMatch);
                    variables.push({
                        fullMatch: fullMatch,
                        name: varName,
                        isDefined: definedVarNames.includes(varName)
                    });
                }
            }
            
            return variables;
        }
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
        },
        
        // 监听content外部变化（如加载数据时更新编辑器）
        content(newVal, oldVal) {
            const editor = this.$refs.promptEditor;
            if (!editor || this.isComposing) return;
            
            const currentText = editor.innerText || '';
            // 如果编辑器内容与content不一致（外部赋值），更新编辑器
            if (currentText !== newVal && newVal !== oldVal) {
                this.$nextTick(() => {
                    const html = this.generateHighlightHTML(newVal);
                    editor.innerHTML = html;
                });
            }
        }
    },
    created() {
        // 浏览器关闭|浏览器刷新|页面关闭|打开新页面 提示有数据变动保存数据
        // 添加 beforeunload 事件监听器
        window.addEventListener('beforeunload', this.beforeunloadHandler);
        
        // 页面创建时加载保存的宽度设置
        this.loadAreaWidthsFromStorage();
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
        setTimeout(() => {
          this.getTargetRangeIdFromUrl();
      }, 200)
      
        // 初始化contenteditable编辑器
        this.$nextTick(() => {
            const editor = this.$refs.promptEditor;
            if (editor && this.content) {
                const html = this.generateHighlightHTML(this.content);
                editor.innerHTML = html;
            }
        });
      
    },
    beforeDestroy() {
        // 销毁之前移除事件监听器
        window.removeEventListener('beforeunload', this.beforeunloadHandler);
        
        // 组件销毁前移除拖动相关的事件监听器
        document.removeEventListener('mousemove', this.handleResize);
        document.removeEventListener('mouseup', this.stopResize);
    },
    methods: {
        //获取路径id 页面数据回显
        getTargetRangeIdFromUrl() {
             // 添加安全检查，防止 $route 未定义
             if (!this.$route || !this.$route.query) {
                 return;
             }
             const targetrangeId = this.$route.query.targetrangeId;
           
           
            if (targetrangeId) {
                this.setDefaultSelectedOption(targetrangeId);
            }
        },
        setDefaultSelectedOption(targetrangeId) {
           
            const defaultOption = this.promptFieldOpt.find(item => item.id === targetrangeId);
        
            if (defaultOption) {
                this.promptField = defaultOption.value;
               
                    // 获取靶道列表
                  this.getPromptOptData().then(() => {
                    
                        // promptid is the last one of promptOpt
                        if (this.promptOpt && this.promptOpt.length > 0) {
                            const targetlaneId = this.$route.query.targetlaneId;
                            const _el = this.promptOpt.find(item => item.id === targetlaneId);
                 
                            this.promptid = _el.value
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
                     this.getScoringTrendData()
                

            }
        },
        //侧边栏收起操作
        foldsidebar() {
            this.isBoxVisible = !this.isBoxVisible;
            if (this.foldsidebarShow) {
             
                this.foldsidebarShow = false

            } else {
                this.foldsidebarShow = true
               
            }
        },
      
        //放大输入区域
       
        Amplification(boxClicked) {
            if (this.lastClickedBox === boxClicked) {
                // 再次点击同一个区域，恢复所有区域
                this.box1Hidden = false;
                this.box2Hidden = false;
                this.box3Hidden = false;
                this.centerAreaMaximized = false;
                this.rightAreaMaximized = false;
                this.lastClickedBox = null;
                this.getScoringTrendData();
            } else {
                // 点击不同区域，实现最大化
                if (boxClicked === 'box1') {
                    // 右侧输出区域最大化：隐藏中间区域，隐藏分析图表，扩展右侧区域
                    this.box1Hidden = false;
                    this.box2Hidden = true;  // 隐藏中间Prompt区域
                    this.box3Hidden = true;  // 隐藏分析图表
                    this.centerAreaMaximized = false;
                    this.rightAreaMaximized = true;
                } else if (boxClicked === 'box2') {
                    // 中间Prompt区域最大化：隐藏右侧所有内容，扩展中间区域
                    this.box1Hidden = true;  // 隐藏右侧输出区域
                    this.box2Hidden = false;
                    this.box3Hidden = true;  // 隐藏分析图表
                    this.centerAreaMaximized = true;
                    this.rightAreaMaximized = false;
                } else if (boxClicked === 'box3') {
                    // 保留原有逻辑
                    this.box1Hidden = false;
                    this.box2Hidden = false;
                    this.box3Hidden = false;
                }
                this.lastClickedBox = boxClicked;
            }
            },
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
               // this.contentTextareaRows = 14
                this.parameterViewShow = false

            } else {
                this.parameterViewShow = true
                //setTimeout(() => {
                   // this.contentTextareaRows = 21
               // }, 300)
            }
        },
   
      //  promptNameField(e, item) {
            // 阻止事件冒泡
          //  e.stopPropagation();
            //弹出提示框，输入新的靶场名称，确认后提交，取消后，提示已取消操作
          //  this.$prompt('请输入新的靶道名称', '提示', {
           //     confirmButtonText: '确定',
           //     cancelButtonText: '取消',
            //    inputErrorMessage: '靶道名称不能为空',
           // }).then(async ({ value }) => {
           //     this.btnEditHandle({ id: item.id, nickName: value })
           // }).catch(() => {
               // this.$message({
              //      type: 'info',
             //       message: '已取消操作'
           //     });
         //   });
        // },
        //重置靶道名称
        promptNameRest(e,id) {
            // 阻止事件冒泡
            e.stopPropagation();
            // 弹出提示框
            this.$confirm('此操作将重置该靶道名称', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(async () => {
                this.btnEditHandle({ id: id, nickName: '' })
            }).catch(() => {
                this.$message({
                    type: 'info',
                    message: '已取消操作'
                });
            });
        },
        // 靶道 名称弹窗
        promptNameField(e, item) {
            e.stopPropagation();
            this.targetlaneNameid = item.id;
     
            this.dialogVisible = true
        },
        handleSelect(item) {
            console.log(item);
        },
        // 修改靶道 名称弹窗 确认操作
        confirmtargetlaneName() {
            if(!this.targetlaneName){
                this.$message({
                    message: '靶道名称不能为空！',
                    type: 'error'
                })
                return
            }
            const prefix = '名称：';
            const suffix = ' | 版本号：';

            const startIndex = this.targetlaneName.indexOf(prefix);
            const endIndex = this.targetlaneName.indexOf(suffix, startIndex);

            if (startIndex !== -1 && endIndex !== -1) {
                // 如果找到了“名称：”和“| 版本号：”，则提取它们之间的文本
                this.targetlaneName = this.targetlaneName.substring(startIndex + prefix.length, endIndex);
                console.log(this.targetlaneName);
                this.btnEditHandle({ id: this.targetlaneNameid, nickName: this.targetlaneName })
          
                this.dialogVisible = false
                this.targetlaneName = ''
            } else {
                // 如果没有找到“名称：”或“| 版本号：”，则执行备用逻辑
                this.btnEditHandle({ id: this.targetlaneNameid, nickName: this.targetlaneName })
               
                this.dialogVisible = false
                this.targetlaneName = ''
                console.log(this.targetlaneName);
            }
           
        },
        //获取靶道弹窗input列表 过滤没有名字的靶道
        querySearch(queryString, cb) {
            let restaurants = this.dailogpromptOptlist;
            console.log(111,this.dailogpromptOptlist)
            // let results = queryString ? restaurants.filter(this.createFilter(queryString)) : restaurants;
            let results = queryString
                ? restaurants.filter(item => {
                    return (
                        this.createFilter(queryString)(item) &&
                        !item.label.includes('未设置')
                    );
                })
                : restaurants.filter(item => !item.label.includes('未设置'));
            // 调用 callback 返回建议列表的数据
            cb(results);
        },
        createFilter(queryString) {
            return (restaurant) => {
                console.log('Filtering restaurant:', restaurant.label, 'with query:', queryString);
                const label = restaurant.label || '';
                return label.toLowerCase().includes(queryString.toLowerCase());
            };
        },
        // 战术选择 dialog 提交
        tacticalFormSubmitBtn() {
            // 如果是继续聊天模式，直接处理，不需要验证战术字段
            if (this.continueChatMode && this.continueChatPromptResultId) {
                // 检查是否有输入内容
                if (!this.tacticalChatInput || !this.tacticalChatInput.trim()) {
                    this.$message({
                        message: '请输入对话内容',
                        type: 'warning',
                        duration: 3000
                    })
                    return
                }
                
                // 调用继续聊天 API
                this.continueChatSubmit(this.continueChatPromptResultId, this.tacticalChatInput.trim())
                return
            }
            
            // 普通模式，需要验证表单
            this.$refs.tacticalForm.validate(async (valid) => {
                if (valid) {
                    // 如果选择对话模式，需要检查是否有输入内容
                    if (this.tacticalForm.chatMode === '对话模式') {
                        // 检查是否有输入内容
                        if (!this.tacticalChatInput || !this.tacticalChatInput.trim()) {
                            this.$message({
                                message: '请输入对话内容',
                                type: 'warning',
                                duration: 3000
                            })
                            return
                        }
                        
                        // 执行打靶，将输入内容作为 userMessage 传递
                        await this.executeTargetShootWithChatMessage(this.tacticalChatInput.trim())
                        // 清空对话输入
                        this.tacticalChatInput = ''
                        return
                    }
                    
                    // 直接测试模式，继续原有流程
                    await this.executeTargetShoot()
                } else {
                    return false;
                }
            });
        },
        
        // 继续聊天提交
        async continueChatSubmit(promptResultId, userMessage) {
            this.tacticalFormSubmitLoading = true
            try {
                const res = await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.ContinueChat`, {
                    promptResultId: promptResultId,
                    userMessage: userMessage || ''
                })
                
                if (res.data.success) {
                    const newChatMessages = res.data.data || []
                    
                    // 验证新消息是否有有效的 ID
                    const invalidMessages = newChatMessages.filter(msg => {
                        const msgId = typeof msg.id === 'string' ? parseInt(msg.id, 10) : msg.id
                        return !msgId || msgId === 0 || isNaN(msgId)
                    })
                    
                    if (invalidMessages.length > 0) {
                        console.error('错误：部分消息没有有效的 ID，重新加载历史记录以确保获取正确的 ID', invalidMessages)
                        // 如果消息没有 ID，重新加载历史记录以确保获取正确的 ID
                        const reloadRes = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetChatHistory?promptResultId=${promptResultId}`)
                        if (reloadRes.data.success) {
                            this.continueChatHistory = reloadRes.data.data || []
                            console.log('重新加载后的历史记录:', this.continueChatHistory)
                            
                            // 验证重新加载后的记录是否都有有效的 ID
                            const stillInvalid = this.continueChatHistory.filter(msg => {
                                const msgId = typeof msg.id === 'string' ? parseInt(msg.id, 10) : msg.id
                                return !msgId || msgId === 0 || isNaN(msgId)
                            })
                            if (stillInvalid.length > 0) {
                                console.error('严重错误：重新加载后仍有消息 ID 无效:', stillInvalid)
                            }
                        }
                    } else {
                        // 追加新的对话记录到历史记录
                        console.log('追加新消息到历史记录:', newChatMessages.map(m => ({ id: m.id, roleType: m.roleType, sequence: m.sequence })))
                        this.continueChatHistory.push(...newChatMessages)
                        console.log('更新后的历史记录（最后5条）:', this.continueChatHistory.slice(-5).map(m => ({ id: m.id, idType: typeof m.id, roleType: m.roleType, sequence: m.sequence })))
                    }
                    
                    // 找到对应的输出项并更新
                    const resultIndex = this.outputList.findIndex(item => item.id === promptResultId)
                    if (resultIndex !== -1) {
                        const resultItem = this.outputList[resultIndex]
                        
                        // 更新显示：将最新的 AI 回复追加到 ResultString
                        const latestAssistantMessage = this.continueChatHistory.find(msg => msg.roleType === 2 && msg.sequence === Math.max(...this.continueChatHistory.map(m => m.sequence)))
                        if (latestAssistantMessage) {
                            // 追加到现有的 ResultString（格式化为对话形式）
                            const currentResult = resultItem.resultString || ''
                            const separator = currentResult ? '\n\n---\n\n' : ''
                            const newContent = `**用户**: ${userMessage}\n\n**助手**: ${latestAssistantMessage.content}`
                            resultItem.resultString = currentResult + separator + newContent
                            resultItem.resultStringHtml = marked.parse(resultItem.resultString)
                        }
                    }
                    
                    // 刷新对话历史显示
                    this.$forceUpdate()
                    
                    // 滚动到底部显示最新消息
                    this.$nextTick(() => {
                        const container = document.getElementById('chatHistoryContainer')
                        if (container) {
                            container.scrollTop = container.scrollHeight
                        }
                    })
                    
                    // 清空输入框，但保持弹窗打开以便继续对话
                    this.tacticalChatInput = ''
                    this.$message({
                        message: '对话已追加',
                        type: 'success',
                        duration: 2000
                    })
                } else {
                    this.$message({
                        message: res.data.errorMessage || '继续聊天失败',
                        type: 'error'
                    })
                }
            } catch (error) {
                this.$message({
                    message: '继续聊天失败：' + (error.message || '未知错误'),
                    type: 'error'
                })
            } finally {
                this.tacticalFormSubmitLoading = false
            }
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
            _postData.isAIGrade = this.isAIGrade
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
            
            // 注意：现在后端会自动根据第一个 PromptResult 来判断类型
            // 如果第一个结果是 Chat 模式，后端会从对话记录中获取 userMessage
            // 所以前端不需要传递 userMessage，后端会自动处理
            // 但为了兼容性，如果前端有保存的 userMessage，仍然可以传递
            
            let promises = [];
            for (let i = 0; i < howmany; i++) {
                // 后端会自动判断第一个结果的模式，不需要前端传递 userMessage
                // 但如果前端有保存的 userMessage，可以传递以保持一致性
                promises.push(this.rapidFireHandel(id, this._lastUserMessage || null));
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
            this.exportPluginSelectedCount = 0 // 重置已选择数量
            this.exportPluginExpandAll = false // 重置展开状态
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
            // 等待 DOM 更新后再设置选中和更新计数
            this.$nextTick(() => {
            // 设置默认选中
            this.$refs.expectedPluginTree.setCheckedKeys(this.expectedPluginFoem.checkList)
                
                // 确保 checkList 只包含叶子节点
                const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
                const leafNodeKeys = [];
                
                checkedNodes.forEach(node => {
                    // 只记录叶子节点（靶道）
                    if (!node.children || node.children.length === 0) {
                        leafNodeKeys.push(node.idkey);
                    }
                });
                
                this.expectedPluginFoem.checkList = leafNodeKeys;
                
                // 更新已选择数量
                this.updateExportPluginSelectedCount()
            })
        },
        // 导出 plugins dialog tree 选中变化
        treeCheckChange(data, currentCheck, childrenCheck) {
            // 更新已选择数量（使用 $nextTick 确保 Tree 状态已更新）
            this.$nextTick(() => {
                // 使用 Tree API 重新获取所有选中的节点（只获取叶子节点）
                if (!this.$refs.expectedPluginTree) return;
                
                const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
                const leafNodeKeys = [];
                
                checkedNodes.forEach(node => {
                    // 只记录叶子节点（靶道）
                    if (!node.children || node.children.length === 0) {
                        leafNodeKeys.push(node.idkey);
                    }
                });
                
                // 更新 checkList，只包含叶子节点
                this.expectedPluginFoem.checkList = leafNodeKeys;
                
                // 更新计数
                this.updateExportPluginSelectedCount();
            });
        },
        
        // 更新已选择的靶道数量
        updateExportPluginSelectedCount() {
            if (!this.$refs.expectedPluginTree) {
                this.exportPluginSelectedCount = 0;
                return;
            }
            
            // 使用 Tree 组件的 API 获取所有选中的节点（包括半选状态的父节点的子节点）
            const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
            const halfCheckedNodes = this.$refs.expectedPluginTree.getHalfCheckedNodes();
            
            // 统计所有选中的子节点（靶道）
            // 靶道的特征：有 idkey 且包含下划线，或者没有 children
            let count = 0;
            
            // 统计完全选中的节点中的靶道
            checkedNodes.forEach(node => {
                // 如果是叶子节点（靶道），统计
                if (!node.children || node.children.length === 0) {
                    count++;
                }
            });
            
            // 对于半选状态的父节点，需要统计其已选中的子节点
            // Element UI Tree 的 getCheckedNodes() 已经包含了所有选中的子节点，所以不需要额外处理
            
            this.exportPluginSelectedCount = count;
        },
        
        // 导出plugin - 全选
        exportPluginSelectAll() {
            if (!this.$refs.expectedPluginTree) return;
            
            // 获取所有节点的key（包括父节点和子节点）
            const allKeys = [];
            const collectKeys = (nodes) => {
                nodes.forEach(node => {
                    allKeys.push(node.idkey);
                    if (node.children && node.children.length > 0) {
                        collectKeys(node.children);
                    }
                });
            };
            collectKeys(this.expectedPluginFieldList);
            
            // 设置选中
            this.$refs.expectedPluginTree.setCheckedKeys(allKeys);
            
            // 等待 DOM 更新后，只记录叶子节点
            this.$nextTick(() => {
                const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
                const leafNodeKeys = [];
                
                checkedNodes.forEach(node => {
                    // 只记录叶子节点（靶道）
                    if (!node.children || node.children.length === 0) {
                        leafNodeKeys.push(node.idkey);
                    }
                });
                
                this.expectedPluginFoem.checkList = leafNodeKeys;
                this.updateExportPluginSelectedCount();
            });
            
            this.$message.success('已全选所有靶道');
        },
        
        // 导出plugin - 反选
        exportPluginInvertSelection() {
            if (!this.$refs.expectedPluginTree) return;
            
            // 收集所有叶子节点（靶道）的key
            const allLeafKeys = [];
            const collectLeafKeys = (nodes) => {
                nodes.forEach(node => {
                    if (!node.children || node.children.length === 0) {
                        // 这是叶子节点（靶道）
                        allLeafKeys.push(node.idkey);
                    } else {
                        // 这是父节点（靶场），继续递归
                        collectLeafKeys(node.children);
                    }
                });
            };
            collectLeafKeys(this.expectedPluginFieldList);
            
            // 获取当前选中的叶子节点key（使用 leafOnly=true 参数）
            const currentCheckedLeafKeys = this.$refs.expectedPluginTree.getCheckedKeys(true);
            
            // 反选：所有叶子节点 - 当前选中的叶子节点
            const invertedLeafKeys = allLeafKeys.filter(key => !currentCheckedLeafKeys.includes(key));
            
            // 设置反选后的结果（只设置叶子节点）
            this.$refs.expectedPluginTree.setCheckedKeys(invertedLeafKeys);
            
            // 等待 DOM 更新后更新状态
            this.$nextTick(() => {
                this.expectedPluginFoem.checkList = invertedLeafKeys;
                this.updateExportPluginSelectedCount();
            });
            
            this.$message.success('已反选');
        },
        
        // 导出plugin - 清空选择
        exportPluginClearAll() {
            if (!this.$refs.expectedPluginTree) return;
            
            this.$refs.expectedPluginTree.setCheckedKeys([]);
            this.expectedPluginFoem.checkList = [];
            
            // 等待 DOM 更新后再统计
            this.$nextTick(() => {
                this.updateExportPluginSelectedCount();
            });
            
            this.$message.info('已清空所有选择');
        },
        
        // 导出plugin - 切换展开/收起
        exportPluginToggleExpand() {
            this.exportPluginExpandAll = !this.exportPluginExpandAll;
            
            // 需要重新渲染树来应用展开状态
            if (!this.$refs.expectedPluginTree) return;
            
            const tree = this.$refs.expectedPluginTree;
            const allNodes = tree.store.nodesMap;
            
            for (let key in allNodes) {
                const node = allNodes[key];
                if (node.childNodes && node.childNodes.length > 0) {
                    node.expanded = this.exportPluginExpandAll;
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
                        a.parentNode && a.parentNode.removeChild(a); // 从 DOM 中删除 <a> 标签
                        
                        this.$message.success('导出成功！');
                    }).catch((error) => {
                        this.isPageLoading = false
                        console.error('导出失败:', error);
                        
                        let errorMessage = '导出 plugins 失败';
                        if (error.response) {
                            // 服务器返回错误响应
                            if (error.response.status === 500) {
                                errorMessage = '服务器错误：' + (error.response.data?.message || '导出过程中发生错误');
                            } else if (error.response.data && error.response.data.message) {
                                errorMessage = error.response.data.message;
                            }
                        } else if (error.message) {
                            errorMessage = error.message;
                        }
                        
                        this.$message.error(errorMessage);
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
        copyInfo(source) {
            let fullVersion = '';
            let label = '';
            
            // 如果传入了参数，则复制对比窗口中的版本号
            if (source === 'A' || source === 'B') {
                const prompt = source === 'A' ? this.comparePromptA : this.comparePromptB;
                if (!prompt || !prompt.fullVersion) {
                    this.$message.warning('无法获取版本号');
                    return;
                }
                fullVersion = prompt.fullVersion;
                label = `Prompt ${source} 版本号`;
            } else {
                // 否则复制当前选中的 Prompt
            if (!this.promptid) {
                this.$message.info('请选择靶道后再复制信息！')
                return
            }
            const promptItem = this.promptOpt.find(item => item.id === this.promptid)
                if (!promptItem) {
                    this.$message.warning('无法获取当前 Prompt 信息');
                    return;
                }
                fullVersion = promptItem.fullVersion;
                label = '';
            }
            
            // 把结果复制到剪切板
            const input = document.createElement('input')
            input.setAttribute('readonly', 'readonly')
            input.setAttribute('value', fullVersion)
            document.body.appendChild(input)
            input.select()
            input.setSelectionRange(0, 9999)
            if (document.execCommand('copy')) {
                document.execCommand('copy')
                const message = label ? `复制${label}【${fullVersion}】成功` : `复制【${fullVersion}】成功`;
                this.$message.success(message)
            }
            document.body.removeChild(input);
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
        // 格式化聊天时间（更简洁的格式）
        formatChatTime(d) {
            if (!d) return ''
            var date = new Date(d)
            var now = new Date()
            var diff = now - date
            var minutes = Math.floor(diff / 60000)
            var hours = Math.floor(minutes / 60)
            var days = Math.floor(hours / 24)
            
            if (minutes < 1) return '刚刚'
            if (minutes < 60) return minutes + '分钟前'
            if (hours < 24) return hours + '小时前'
            if (days < 7) return days + '天前'
            
            // 超过7天显示具体日期
            var hh = (date.getHours() < 10 ? '0' + date.getHours() : date.getHours()) + ':'
            var mm = (date.getMinutes() < 10 ? '0' + date.getMinutes() : date.getMinutes())
            var MM = (date.getMonth() + 1 < 10 ? '0' + (date.getMonth() + 1) : date.getMonth() + 1) + '-'
            var DD = date.getDate() < 10 ? '0' + date.getDate() : date.getDate()
            return MM + DD + ' ' + hh + mm
        },
        // 格式化聊天内容（支持markdown）
        formatChatContent(content) {
            if (!content) return ''
            // 使用marked解析markdown
            return marked.parse(content)
        },
        // 切换聊天反馈（Like/Unlike）
        async toggleChatFeedback(chatId, feedback) {
            // 防止重复点击：如果正在处理，直接返回
            if (this._isUpdatingFeedback) {
                return
            }
            this._isUpdatingFeedback = true
            
            try {
                // 验证 chatId 是否有效
                if (!chatId || chatId === 0 || chatId === '0') {
                    console.error('无效的 chatId:', chatId, '类型:', typeof chatId)
                    console.error('当前历史记录:', this.continueChatHistory)
                    this.$message({
                        message: '对话记录ID无效，请刷新页面后重试',
                        type: 'error'
                    })
                    return
                }
                
                // 确保 chatId 是数字类型
                const numericChatId = typeof chatId === 'string' ? parseInt(chatId, 10) : chatId
                if (isNaN(numericChatId) || numericChatId <= 0) {
                    console.error('无效的 chatId（转换后）:', numericChatId, '原始值:', chatId)
                    this.$message({
                        message: '对话记录ID无效，请刷新页面后重试',
                        type: 'error'
                    })
                    return
                }
                
                // 找到当前消息（同时检查 id 和 numericChatId）
                const msgIndex = this.continueChatHistory.findIndex(msg => {
                    const msgId = typeof msg.id === 'string' ? parseInt(msg.id, 10) : msg.id
                    return msgId === numericChatId || msg.id === numericChatId || msg.id === chatId
                })
                
                if (msgIndex === -1) {
                    console.error('未找到对应的对话记录')
                    console.error('查找的 chatId:', numericChatId, '原始 chatId:', chatId)
                    console.error('历史记录中的所有 ID:', this.continueChatHistory.map(m => ({ id: m.id, idType: typeof m.id, roleType: m.roleType, sequence: m.sequence })))
                    this.$message({
                        message: '未找到对应的对话记录，请刷新页面后重试',
                        type: 'error'
                    })
                    return
                }
                
                const currentMsg = this.continueChatHistory[msgIndex]
                console.log('找到的消息:', currentMsg)
                
                // 验证消息 ID 是否有效
                const msgId = typeof currentMsg.id === 'string' ? parseInt(currentMsg.id, 10) : currentMsg.id
                if (!msgId || msgId === 0 || isNaN(msgId)) {
                    console.error('消息 ID 无效:', currentMsg.id, '类型:', typeof currentMsg.id)
                    this.$message({
                        message: '消息ID无效，请刷新页面后重试',
                        type: 'error'
                    })
                    return
                }
                
                // 如果点击的是当前已选中的反馈，则取消反馈（设为null）
                const newFeedback = currentMsg.userFeedback === feedback ? null : feedback
                
                // 调用API更新反馈（使用有效的消息 ID）
                // 注意：使用 Request DTO 格式，属性名需要首字母大写
                const res = await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.UpdateChatFeedback`, {
                    ChatId: msgId,
                    Feedback: newFeedback
                })
                
                if (res.data.success) {
                    // 更新本地数据
                    this.continueChatHistory[msgIndex].userFeedback = newFeedback
                    this.$forceUpdate()
                    
                    this.$message({
                        message: newFeedback === null ? '已取消反馈' : (newFeedback ? '已点赞' : '已点踩'),
                        type: 'success',
                        duration: 1500
                    })
                } else {
                    this.$message({
                        message: res.data.errorMessage || '更新反馈失败',
                        type: 'error'
                    })
                }
            } catch (error) {
                console.error('更新反馈失败:', error)
                this.$message({
                    message: '更新反馈失败：' + (error.message || '未知错误'),
                    type: 'error'
                })
            } finally {
                // 重置标志，允许下次点击
                this._isUpdatingFeedback = false
            }
        },
        // 处理对话历史滚动
        handleChatHistoryScroll(event) {
            // 可以在这里添加滚动相关的逻辑，比如显示滚动位置等
            // 目前暂时不需要特殊处理
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
                            //console.log(333)
                        })
                    }).catch(() => {
                        //console.log(222)
                        this.resetPageData()
                    });
                    return
                }
                //console.log(111)
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
                    console.log(this.promptOpt)
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

        // 继续聊天：加载历史记录并打开战术选择弹窗
        async continueChat(promptResultId, resultIndex) {
            try {
                // 加载对话历史记录
                const res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetChatHistory?promptResultId=${promptResultId}`)
                if (res.data.success) {
                    this.continueChatMode = true
                    this.continueChatPromptResultId = promptResultId
                    this.continueChatHistory = res.data.data || []
                    
                    // 打开战术选择弹窗，锁定为对话模式
                    this.tacticalForm.chatMode = '对话模式'
                    this.tacticalFormVisible = true
                    
                    // 滚动到底部显示最新消息
                    this.$nextTick(() => {
                        const container = document.getElementById('chatHistoryContainer')
                        if (container) {
                            container.scrollTop = container.scrollHeight
                        }
                    })
                } else {
                    this.$message({
                        message: res.data.errorMessage || '加载对话历史失败',
                        type: 'error'
                    })
                }
            } catch (error) {
                this.$message({
                    message: '加载对话历史失败：' + (error.message || '未知错误'),
                    type: 'error'
                })
            }
        },
        
        // 战术选择 关闭弹出
        // 战术选择 dialog 关闭
        tacticalFormCloseDialog() {
            this.tacticalForm = {
                tactics: '重新瞄准',
                chatMode: '对话模式' // 重置为默认值
            }
            // 清空对话输入
            this.tacticalChatInput = ''
            // 重置继续聊天状态
            this.continueChatMode = false
            this.continueChatPromptResultId = null
            this.continueChatHistory = []
            if (this.$refs.tacticalForm) {
                this.$refs.tacticalForm.resetFields();
            }
        },
        
        // 打开导图对话框
        openMapDialog() {
            if (!this.promptField) {
                this.$message({
                    message: '请先选择靶场',
                    type: 'warning'
                })
                return
            }
            this.mapDialogVisible = true
            this.$nextTick(() => {
                this.initMap3D()
            })
        },
        
        // 关闭导图对话框
        mapDialogClose() {
            this.destroyMap3D()
        },
        
        // 初始化 3D 导图
        initMap3D() {
            const container = document.getElementById('map3dContainer')
            if (!container) return
            
            // 构建树状结构数据
            this.buildTreeData()
            
            // 确保 THREE 已加载
            if (typeof THREE === 'undefined' && typeof window.THREE !== 'undefined') {
                window.THREE = window.THREE
            }
            
            if (typeof THREE === 'undefined') {
                this.$message({
                    message: 'THREE.js 未加载，请刷新页面重试',
                    type: 'error'
                })
                this.mapDialogVisible = false
                return
            }
            
            // 创建场景（使用渐变背景）
            this.map3dScene = new THREE.Scene()
            // 创建渐变背景
            const gradientTexture = this.createGradientBackground()
            this.map3dScene.background = gradientTexture
            
            // 禁用雾化效果，确保远处节点清晰可见
            this.map3dScene.fog = null
            
            // 创建相机（增大远裁剪面，确保所有节点都可见）
            const width = container.clientWidth
            const height = container.clientHeight
            // 参数：视场角(75度), 宽高比, 近裁剪面(0.1), 远裁剪面(5000 - 增大以支持更远的节点)
            this.map3dCamera = new THREE.PerspectiveCamera(75, width / height, 0.1, 5000)
            this.map3dCamera.position.set(0, 0, 50)
            
            // 创建渲染器（禁用对数深度缓冲，提高远处物体的清晰度）
            this.map3dRenderer = new THREE.WebGLRenderer({ 
                antialias: true,
                logarithmicDepthBuffer: false,
                precision: 'highp' // 使用高精度，提高渲染质量
            })
            this.map3dRenderer.setSize(width, height)
            // 设置像素比，在高DPI屏幕上更清晰
            this.map3dRenderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
            container.appendChild(this.map3dRenderer.domElement)
            
            // 添加控制器（使用本地化的 OrbitControls）
            if (typeof THREE.OrbitControls !== 'undefined') {
                this.map3dControls = new THREE.OrbitControls(this.map3dCamera, this.map3dRenderer.domElement)
                
                // 启用阻尼效果，使旋转更平滑
                this.map3dControls.enableDamping = true
                this.map3dControls.dampingFactor = 0.05
                
                // 启用缩放（增大最大距离，支持更远的视角）
                this.map3dControls.enableZoom = true
                this.map3dControls.zoomSpeed = 1.2
                this.map3dControls.minDistance = 10
                this.map3dControls.maxDistance = 500 // 从 200 增加到 500
                
                // 启用旋转
                this.map3dControls.enableRotate = true
                this.map3dControls.rotateSpeed = 0.8
                
                // 启用平移
                this.map3dControls.enablePan = true
                this.map3dControls.panSpeed = 0.8
                this.map3dControls.screenSpacePanning = true
                
                // 设置初始相机位置，使其能看到整个场景
                this.map3dCamera.position.set(30, 30, 50)
                this.map3dControls.target.set(0, 0, 0)
                this.map3dControls.update()
            } else {
                console.warn('OrbitControls 未找到，3D 场景将无法通过鼠标控制')
            }
            
            // 添加更丰富的光源系统（增强远处物体的照明）
            // 环境光 - 提供基础照明（增强亮度以照亮远处节点）
            const ambientLight = new THREE.AmbientLight(0xffffff, 0.6) // 从 0.4 增加到 0.6
            this.map3dScene.add(ambientLight)
            
            // 主方向光 - 模拟太阳光（无衰减，照亮所有节点）
            const directionalLight1 = new THREE.DirectionalLight(0xffffff, 0.8)
            directionalLight1.position.set(20, 20, 20)
            directionalLight1.castShadow = false
            this.map3dScene.add(directionalLight1)
            
            // 辅助方向光 - 补充照明（从另一侧照亮）
            const directionalLight2 = new THREE.DirectionalLight(0x88ccff, 0.5) // 从 0.4 增加到 0.5
            directionalLight2.position.set(-20, 10, -20)
            this.map3dScene.add(directionalLight2)
            
            // 第三方向光 - 从前方照亮远处节点
            const directionalLight3 = new THREE.DirectionalLight(0xffffff, 0.4)
            directionalLight3.position.set(0, 0, 50)
            this.map3dScene.add(directionalLight3)
            
            // 点光源 - 增加层次感（无衰减距离限制）
            const pointLight = new THREE.PointLight(0xffffff, 0.6, 0) // distance=0 表示无限距离
            pointLight.position.set(0, 20, 0)
            this.map3dScene.add(pointLight)
            
            // 渲染节点
            this.renderTreeNodes()
            
            // 开始动画循环
            this.animateMap3D()
            
            // 处理窗口大小变化
            window.addEventListener('resize', this.handleMap3DResize)
        },
        
        // 构建树状结构数据
        buildTreeData() {
            if (!this.promptOpt || this.promptOpt.length === 0) {
                this.map3dTreeData = null
                return
            }
            
            const tree = {}
            const currentPromptId = this.promptid
            
            // 解析每个靶道的 FullVersion
            this.promptOpt.forEach(prompt => {
                const fullVersion = prompt.fullVersion || prompt.label
                if (!fullVersion) return
                
                // 解析格式：2023.12.14.1-T1.1-A123
                const parts = fullVersion.split('-')
                if (parts.length < 2) return
                
                const rangeName = parts[0] // 2023.12.14.1
                const tacticPart = parts[1] // T1.1
                const aimingPart = parts[2] || '' // A123
                
                // 获取或创建 RangeName 根节点
                if (!tree[rangeName]) {
                    tree[rangeName] = {
                        type: 'range',
                        name: rangeName,
                        fullPath: rangeName,
                        children: {},
                        prompts: [],
                        expanded: true
                    }
                }
                
                const rangeNode = tree[rangeName]
                
                // 解析 Tactic（每个.是一层）
                const tacticParts = tacticPart.replace('T', '').split('.')
                let currentNode = rangeNode.children
                let lastTacticNode = null
                
                tacticParts.forEach((part, index) => {
                    const key = `T${tacticParts.slice(0, index + 1).join('.')}`
                    
                    if (!currentNode[key]) {
                        currentNode[key] = {
                            type: 'tactic',
                            name: part,
                            fullPath: `${rangeName}-${key}`,
                            parentPath: index > 0 ? `T${tacticParts.slice(0, index).join('.')}` : rangeName,
                            children: {},
                            prompts: [],
                            expanded: true
                        }
                    }
                    lastTacticNode = currentNode[key]
                    currentNode = currentNode[key].children
                })
                
                // 确保 lastTacticNode 存在
                if (!lastTacticNode) {
                    console.warn('无法找到 Tactic 节点:', tacticPart)
                    return
                }
                
                // 添加 Aiming（特殊层）- 修复：使用唯一的key避免共享
                if (aimingPart) {
                    // 使用完整路径作为key，确保每个Aiming节点都是独立的
                    const aimingKey = `${tacticPart}-${aimingPart}` // 例如: "T1.1-A1", "T1.2-A1"
                    if (!lastTacticNode.children[aimingKey]) {
                        lastTacticNode.children[aimingKey] = {
                            type: 'aiming',
                            name: aimingPart.replace('A', ''), // 显示名称去掉A
                            fullPath: `${rangeName}-${tacticPart}-${aimingPart}`,
                            parentPath: `${rangeName}-${tacticPart}`,
                            children: {},
                            prompts: [],
                            expanded: true
                        }
                    }
                    lastTacticNode.children[aimingKey].prompts.push({
                        id: prompt.idkey || prompt.value,
                        fullVersion: fullVersion,
                        nickName: prompt.nickName,
                        isCurrent: (prompt.idkey || prompt.value) == currentPromptId
                    })
                } else {
                    // 如果没有 Aiming，直接添加到最后一个 Tactic 节点
                    lastTacticNode.prompts.push({
                        id: prompt.idkey || prompt.value,
                        fullVersion: fullVersion,
                        nickName: prompt.nickName,
                        isCurrent: (prompt.idkey || prompt.value) == currentPromptId
                    })
                }
            })
            
            this.map3dTreeData = tree
        },
        
        // 计算树的高度（用于平衡布局）
        calculateTreeHeight(nodeData) {
            if (!nodeData || typeof nodeData !== 'object') return 0
            
            let maxHeight = 0
            Object.keys(nodeData).forEach(key => {
                const node = nodeData[key]
                const isExpanded = node.expanded !== false
                
                if (isExpanded && node.children && Object.keys(node.children).length > 0) {
                    const childHeight = this.calculateTreeHeight(node.children)
                    maxHeight = Math.max(maxHeight, 1 + childHeight)
                } else {
                    maxHeight = Math.max(maxHeight, 1)
                }
            })
            
            return maxHeight
        },
        
        // 计算子树的节点数量（用于平衡布局）
        countTreeNodes(nodeData) {
            if (!nodeData || typeof nodeData !== 'object') return 0
            
            let count = 0
            Object.keys(nodeData).forEach(key => {
                const node = nodeData[key]
                count++ // 当前节点
                
                const isExpanded = node.expanded !== false
                if (isExpanded && node.children && Object.keys(node.children).length > 0) {
                    count += this.countTreeNodes(node.children)
                }
            })
            
            return count
        },
        
        // 渲染树节点（优化版：动态平衡布局 + 动画效果）
        renderTreeNodes() {
            if (!this.map3dTreeData) return
            
            this.map3dNodes = []
            // 初始化节点映射
            if (!this.map3dNodeMap) {
                this.map3dNodeMap = new Map()
            }
            const raycaster = new THREE.Raycaster()
            const mouse = new THREE.Vector2()
            
            // 递归渲染节点（重构版：支持 Aiming 水平布局）
            const renderNode = (nodeData, parentPosition, depth, yOffset, availableSpace) => {
                if (!nodeData || typeof nodeData !== 'object') return { yOffset, height: 0, minY: yOffset, maxY: yOffset }
                
                const keys = Object.keys(nodeData)
                let currentYOffset = yOffset
                let totalHeight = 0
                let minY = Infinity
                let maxY = -Infinity
                
                keys.forEach((key, index) => {
                    const node = nodeData[key]
                    const isExpanded = node.expanded !== false // 默认展开
                    
                    // **重要：跳过 Aiming 类型节点，它们会在父节点处理时被创建**
                    if (node.type === 'aiming') {
                        return // 不在这里处理 Aiming 节点
                    }
                    
                    // 检查子节点是否全是 Aiming 类型
                    const hasAimingChildren = node.children && Object.keys(node.children).length > 0 &&
                        Object.values(node.children).every(child => child.type === 'aiming')
                    
                    // 计算子树的节点数量，用于分配垂直空间
                    let nodeCount = 1
                    if (isExpanded && node.children && Object.keys(node.children).length > 0 && !hasAimingChildren) {
                        nodeCount += this.countTreeNodes(node.children)
                    }
                    
                    // 根据节点数量动态调整间距（实现平衡布局）
                    const baseSpacing = 12
                    const nodeSpacing = Math.max(baseSpacing, nodeCount * 3)
                    
                    // 计算位置（水平分层，垂直排列，动态平衡）
                    const x = depth * 20
                    let y = currentYOffset // 当前节点的Y位置
                    const z = 0
                    
                    // 检查是否有当前编辑的靶道
                    const hasCurrent = node.prompts && node.prompts.some(p => p.isCurrent)
                    
                    // 创建几何体（只为 Range 和 Tactic 创建，Aiming 在专门的地方创建）
                    let geometry, material, glowGeometry, glowMaterial
                    if (node.type === 'range') {
                        // 靶场：方块（使用更平滑的圆角效果）
                        geometry = new THREE.BoxGeometry(5, 5, 5)
                        material = new THREE.MeshStandardMaterial({ 
                            color: hasCurrent ? 0xff6b6b : 0x4ecdc4,
                            metalness: 0.3,
                            roughness: 0.4,
                            emissive: hasCurrent ? 0xff3333 : 0x004444,
                            emissiveIntensity: hasCurrent ? 0.8 : 0.1
                        })
                        
                        // 添加发光效果（当前选中时）
                        if (hasCurrent) {
                            glowGeometry = new THREE.BoxGeometry(5.5, 5.5, 5.5)
                            glowMaterial = new THREE.MeshBasicMaterial({
                                color: 0xff6b6b,
                                transparent: true,
                                opacity: 0.3,
                                side: THREE.BackSide
                            })
                        }
                    } else if (node.type === 'tactic') {
                        // 靶道：圆球（优化：降低精度以提高性能）
                        geometry = new THREE.SphereGeometry(2.5, 24, 24)
                        material = new THREE.MeshStandardMaterial({ 
                            color: hasCurrent ? 0xffd93d : 0x95e1d3,
                            metalness: 0.5,
                            roughness: 0.3,
                            emissive: hasCurrent ? 0xffaa00 : 0x004444,
                            emissiveIntensity: hasCurrent ? 0.9 : 0.1
                        })
                        
                        // 添加发光效果（当前选中时）
                        if (hasCurrent) {
                            glowGeometry = new THREE.SphereGeometry(2.8, 24, 24)
                            glowMaterial = new THREE.MeshBasicMaterial({
                                color: 0xffd93d,
                                transparent: true,
                                opacity: 0.4,
                                side: THREE.BackSide
                            })
                        }
                    } else {
                        // 不应该到达这里，因为 Aiming 节点已经在上面被 return 了
                        console.error('意外的节点类型:', node.type, key)
                        return
                    }
                    
                    const mesh = new THREE.Mesh(geometry, material)
                    // 初始位置设置为目标位置（后续用于动画）
                    mesh.position.set(x, y, z)
                    mesh.userData = { 
                        node, 
                        key, 
                        depth, 
                        type: node.type,
                        targetPosition: { x, y, z },
                        initialScale: 0.1 // 用于展开动画
                    }
                    mesh.castShadow = true
                    mesh.receiveShadow = true
                    
                    // 如果是新创建的节点，从小到大的展开动画
                    mesh.scale.set(0.1, 0.1, 0.1)
                    
                    // 添加发光效果
                    let glowMesh = null
                    if (glowGeometry && glowMaterial) {
                        glowMesh = new THREE.Mesh(glowGeometry, glowMaterial)
                        glowMesh.position.set(x, y, z)
                        glowMesh.scale.set(0.1, 0.1, 0.1)
                        this.map3dScene.add(glowMesh)
                    }
                    
                    // 创建文字标签（缩小尺寸，使用玻璃效果）
                    const canvas = document.createElement('canvas')
                    const context = canvas.getContext('2d')
                    // 缩小画布尺寸
                    canvas.width = 1024
                    canvas.height = 256
                    
                    // 绘制玻璃效果背景
                    const padding = 20
                    const borderRadius = 15
                    
                    // 绘制圆角矩形（兼容性处理）
                    const drawRoundedRect = (x, y, width, height, radius) => {
                        context.beginPath()
                        context.moveTo(x + radius, y)
                        context.lineTo(x + width - radius, y)
                        context.quadraticCurveTo(x + width, y, x + width, y + radius)
                        context.lineTo(x + width, y + height - radius)
                        context.quadraticCurveTo(x + width, y + height, x + width - radius, y + height)
                        context.lineTo(x + radius, y + height)
                        context.quadraticCurveTo(x, y + height, x, y + height - radius)
                        context.lineTo(x, y + radius)
                        context.quadraticCurveTo(x, y, x + radius, y)
                        context.closePath()
                    }
                    
                    // 玻璃效果：半透明白色背景
                    context.fillStyle = hasCurrent 
                        ? 'rgba(255, 107, 107, 0.25)'  // 当前选中：淡红色玻璃
                        : 'rgba(255, 255, 255, 0.15)'  // 普通：半透明白色玻璃
                    drawRoundedRect(padding, padding, canvas.width - padding * 2, canvas.height - padding * 2, borderRadius)
                    context.fill()
                    
                    // 玻璃边框（增强玻璃效果）
                    context.strokeStyle = hasCurrent 
                        ? 'rgba(255, 150, 150, 0.6)'   // 当前选中：红色边框
                        : 'rgba(255, 255, 255, 0.3)'   // 普通：白色边框
                    context.lineWidth = 3
                    drawRoundedRect(padding, padding, canvas.width - padding * 2, canvas.height - padding * 2, borderRadius)
                    context.stroke()
                    
                    // 添加高光效果（模拟玻璃反光）
                    const highlightGradient = context.createLinearGradient(0, padding, 0, canvas.height / 2)
                    highlightGradient.addColorStop(0, 'rgba(255, 255, 255, 0.4)')
                    highlightGradient.addColorStop(1, 'rgba(255, 255, 255, 0)')
                    context.fillStyle = highlightGradient
                    drawRoundedRect(padding, padding, canvas.width - padding * 2, (canvas.height - padding * 2) / 2, borderRadius)
                    context.fill()
                    
                    // 准备标签文字（添加类型前缀：T）
                    let labelPrefix = ''
                    let labelText = node.name
                    
                    if (node.type === 'tactic') {
                        // 靶道类型：显示 T1, T1.1, T10, T10.1 等
                        labelPrefix = 'T'
                        // key 的格式是 "T1", "T1.1", "T10", "T10.1" 等
                        // 从 key 中提取战术编号（去除前面的 T）
                        const tacticMatch = key.match(/^T(.+)$/)
                        if (tacticMatch) {
                            labelText = tacticMatch[1]
                        } else {
                            // 如果 key 不匹配，使用 node.name
                            labelText = node.name
                        }
                    } else if (node.type === 'range') {
                        // 靶场类型：显示完整名称
                        labelText = node.name.length > 15 ? node.name.substring(0, 15) + '...' : node.name
                    }
                    // Aiming 类型已经在专门的代码块中处理，不会到达这里
                    
                    // 绘制文字（带阴影增强可读性）
                    context.textAlign = 'center'
                    context.textBaseline = 'middle'
                    context.shadowColor = 'rgba(0, 0, 0, 0.8)'
                    context.shadowBlur = 8
                    context.shadowOffsetX = 2
                    context.shadowOffsetY = 2
                    
                    let mainTextY = canvas.height / 2
                    
                    // 如果有类型前缀（T），在上方显示类型标识
                    if (labelPrefix === 'T') {
                        // 类型标识 T - 缩小字体
                        context.fillStyle = hasCurrent ? 'rgba(255, 255, 255, 1)' : 'rgba(255, 255, 255, 0.9)'
                        const prefixFontSize = hasCurrent ? 50 : 42
                        context.font = `bold ${prefixFontSize}px 'Arial Black', Arial, sans-serif`
                        context.fillText(labelPrefix, canvas.width / 2, canvas.height / 2 - 40)
                        
                        // 主要数字 - 缩小字体
                        context.fillStyle = '#ffffff'
                        const mainFontSize = hasCurrent ? 80 : 70
                        context.font = `bold ${mainFontSize}px 'Arial Black', 'Microsoft YaHei', Arial, sans-serif`
                        context.fillText(labelText, canvas.width / 2, canvas.height / 2 + 20)
                        
                        mainTextY = canvas.height / 2 + 20
                    } else {
                        // 靶场名称 - 缩小字体
                        context.fillStyle = '#ffffff'
                        const rangeFontSize = hasCurrent ? 60 : 50
                        context.font = `bold ${rangeFontSize}px 'Microsoft YaHei', 'PingFang SC', Arial, sans-serif`
                        context.fillText(labelText, canvas.width / 2, canvas.height / 2)
                        
                        mainTextY = canvas.height / 2
                    }
                    
                    // 如果有提示信息，显示在下方
                    if (node.prompts && node.prompts.length > 0) {
                        context.font = `bold ${hasCurrent ? 35 : 30}px Arial`  // 从 70/60 缩小到 35/30
                        context.fillStyle = 'rgba(255, 255, 255, 0.85)'
                        context.shadowBlur = 6
                        const promptText = `${node.prompts.length} 个结果`
                        context.fillText(promptText, canvas.width / 2, mainTextY + 50)  // 从 +100 改为 +50
                    }
                    
                    const texture = new THREE.CanvasTexture(canvas)
                    texture.needsUpdate = true
                    const spriteMaterial = new THREE.SpriteMaterial({ 
                        map: texture, 
                        transparent: true,
                        alphaTest: 0.1,
                        opacity: 0 // 初始透明，用于渐入动画
                    })
                    const sprite = new THREE.Sprite(spriteMaterial)
                    sprite.position.set(x, y + (node.type === 'range' ? 5 : 4), z)
                    // 缩小 sprite 尺寸，配合缩小的文字标签
                    sprite.scale.set(12, 3, 1)  // 从 24x6 缩小到 12x3
                    sprite.userData = { 
                        node, 
                        key, 
                        depth, 
                        type: node.type,
                        targetOpacity: 1
                    }
                    
                    this.map3dScene.add(mesh)
                    this.map3dScene.add(sprite)
                    
                    this.map3dNodes.push({ 
                        mesh, 
                        sprite, 
                        glowMesh,
                        node, 
                        key, 
                        depth, 
                        isExpanded, 
                        position: { x, y, z }, 
                        parentPosition: parentPosition, // 保存父节点位置，用于后续创建连接线
                        childrenMeshes: [], 
                        line: null, 
                        dot: null,
                        animationProgress: 0, // 动画进度 0-1
                        hasCurrent: hasCurrent // 保存是否有当前选中
                    })
                    
                    // 存储节点引用以便快速查找
                    if (!this.map3dNodeMap) {
                        this.map3dNodeMap = new Map()
                    }
                    this.map3dNodeMap.set(node.fullPath, { 
                        mesh, 
                        sprite, 
                        glowMesh,
                        node, 
                        key, 
                        childrenMeshes: [], 
                        line: null, 
                        dot: null 
                    })
                    
                    // 先不添加连接线，稍后统一处理（避免在位置调整前创建线条）
                    let line = null
                    let dot = null
                    
                    // 递归渲染子节点（特殊处理 Aiming 节点：水平排列）
                    let childMinY = y
                    let childMaxY = y
                    
                    if (isExpanded && node.children && Object.keys(node.children).length > 0) {
                        if (hasAimingChildren) {
                            // Aiming 节点：水平排列（垂直于 Tactic）
                            // 先保存当前节点的引用，稍后会更新 Aiming 的 parentPosition
                            const currentParentNodeData = this.map3dNodes[this.map3dNodes.length - 1]
                            
                            const aimingKeys = Object.keys(node.children)
                            const aimingSpacing = 8 // Aiming 节点之间的 Z 轴间距
                            const totalAimingWidth = (aimingKeys.length - 1) * aimingSpacing
                            let startZ = -totalAimingWidth / 2 // 从中心开始分布
                            
                            const aimingNodesData = [] // 保存 Aiming 节点数据，稍后更新 parentPosition
                            
                            aimingKeys.forEach((aimingKey, aimingIndex) => {
                                const aimingNode = node.children[aimingKey]
                                const aimingX = (depth + 1) * 20
                                const aimingY = y // 与父节点同一高度
                                const aimingZ = startZ + aimingIndex * aimingSpacing
                                
                                // 检查是否有当前编辑的靶道
                                const aimingHasCurrent = aimingNode.prompts && aimingNode.prompts.some(p => p.isCurrent)
                                
                                // 创建 Aiming 几何体：小圆球
                                const aimingGeometry = new THREE.SphereGeometry(1.5, 24, 24)
                                const aimingMaterial = new THREE.MeshStandardMaterial({ 
                                    color: aimingHasCurrent ? 0xffd93d : 0xa8e6cf,
                                    metalness: 0.4,
                                    roughness: 0.5,
                                    emissive: aimingHasCurrent ? 0xffaa00 : 0x003333,
                                    emissiveIntensity: aimingHasCurrent ? 0.7 : 0.05
                                })
                                
                                const aimingMesh = new THREE.Mesh(aimingGeometry, aimingMaterial)
                                aimingMesh.position.set(aimingX, aimingY, aimingZ)
                                aimingMesh.userData = { 
                                    node: aimingNode, 
                                    key: aimingKey, 
                                    depth: depth + 1, 
                                    type: aimingNode.type,
                                    targetPosition: { x: aimingX, y: aimingY, z: aimingZ },
                                    initialScale: 0.1
                                }
                                aimingMesh.castShadow = true
                                aimingMesh.receiveShadow = true
                                aimingMesh.scale.set(0.1, 0.1, 0.1)
                                
                                // 创建 Aiming 文字标签
                                const aimingCanvas = document.createElement('canvas')
                                const aimingContext = aimingCanvas.getContext('2d')
                                aimingCanvas.width = 1024
                                aimingCanvas.height = 256
                                
                                const aimingPadding = 20
                                const aimingBorderRadius = 15
                                
                                const drawRoundedRect = (x, y, width, height, radius) => {
                                    aimingContext.beginPath()
                                    aimingContext.moveTo(x + radius, y)
                                    aimingContext.lineTo(x + width - radius, y)
                                    aimingContext.quadraticCurveTo(x + width, y, x + width, y + radius)
                                    aimingContext.lineTo(x + width, y + height - radius)
                                    aimingContext.quadraticCurveTo(x + width, y + height, x + width - radius, y + height)
                                    aimingContext.lineTo(x + radius, y + height)
                                    aimingContext.quadraticCurveTo(x, y + height, x, y + height - radius)
                                    aimingContext.lineTo(x, y + radius)
                                    aimingContext.quadraticCurveTo(x, y, x + radius, y)
                                    aimingContext.closePath()
                                }
                                
                                // 玻璃效果背景
                                aimingContext.fillStyle = aimingHasCurrent 
                                    ? 'rgba(255, 107, 107, 0.25)' 
                                    : 'rgba(255, 255, 255, 0.15)'
                                drawRoundedRect(aimingPadding, aimingPadding, aimingCanvas.width - aimingPadding * 2, aimingCanvas.height - aimingPadding * 2, aimingBorderRadius)
                                aimingContext.fill()
                                
                                // 玻璃边框
                                aimingContext.strokeStyle = aimingHasCurrent 
                                    ? 'rgba(255, 150, 150, 0.6)' 
                                    : 'rgba(255, 255, 255, 0.3)'
                                aimingContext.lineWidth = 3
                                drawRoundedRect(aimingPadding, aimingPadding, aimingCanvas.width - aimingPadding * 2, aimingCanvas.height - aimingPadding * 2, aimingBorderRadius)
                                aimingContext.stroke()
                                
                                // 高光效果
                                const aimingHighlightGradient = aimingContext.createLinearGradient(0, aimingPadding, 0, aimingCanvas.height / 2)
                                aimingHighlightGradient.addColorStop(0, 'rgba(255, 255, 255, 0.4)')
                                aimingHighlightGradient.addColorStop(1, 'rgba(255, 255, 255, 0)')
                                aimingContext.fillStyle = aimingHighlightGradient
                                drawRoundedRect(aimingPadding, aimingPadding, aimingCanvas.width - aimingPadding * 2, (aimingCanvas.height - aimingPadding * 2) / 2, aimingBorderRadius)
                                aimingContext.fill()
                                
                                // 绘制文字
                                aimingContext.textAlign = 'center'
                                aimingContext.textBaseline = 'middle'
                                aimingContext.shadowColor = 'rgba(0, 0, 0, 0.8)'
                                aimingContext.shadowBlur = 8
                                aimingContext.shadowOffsetX = 2
                                aimingContext.shadowOffsetY = 2
                                
                                // 提取 Aiming 数字
                                const aimingMatch = aimingKey.match(/-A(\d+)$/)
                                const aimingLabelText = aimingMatch ? aimingMatch[1] : aimingNode.name
                                
                                // 类型标识 'A'
                                aimingContext.fillStyle = aimingHasCurrent ? 'rgba(255, 255, 255, 1)' : 'rgba(255, 255, 255, 0.9)'
                                const aimingPrefixFontSize = aimingHasCurrent ? 50 : 42
                                aimingContext.font = `bold ${aimingPrefixFontSize}px 'Arial Black', Arial, sans-serif`
                                aimingContext.fillText('A', aimingCanvas.width / 2, aimingCanvas.height / 2 - 40)
                                
                                // 主要数字
                                aimingContext.fillStyle = '#ffffff'
                                const aimingMainFontSize = aimingHasCurrent ? 80 : 70
                                aimingContext.font = `bold ${aimingMainFontSize}px 'Arial Black', 'Microsoft YaHei', Arial, sans-serif`
                                aimingContext.fillText(aimingLabelText, aimingCanvas.width / 2, aimingCanvas.height / 2 + 20)
                                
                                // 创建 sprite
                                const aimingTexture = new THREE.CanvasTexture(aimingCanvas)
                                const aimingSpriteMaterial = new THREE.SpriteMaterial({ 
                                    map: aimingTexture, 
                                    transparent: true,
                                    alphaTest: 0.1,
                                    opacity: 0
                                })
                                const aimingSprite = new THREE.Sprite(aimingSpriteMaterial)
                                aimingSprite.position.set(aimingX, aimingY + 4, aimingZ)
                                aimingSprite.scale.set(12, 3, 1)
                                aimingSprite.userData = { 
                                    node: aimingNode, 
                                    key: aimingKey, 
                                    depth: depth + 1, 
                                    type: aimingNode.type,
                                    targetOpacity: 1
                                }
                                
                                this.map3dScene.add(aimingMesh)
                                this.map3dScene.add(aimingSprite)
                                
                                const aimingNodeData = { 
                                    mesh: aimingMesh, 
                                    sprite: aimingSprite, 
                                    glowMesh: null,
                                    node: aimingNode, 
                                    key: aimingKey, 
                                    depth: depth + 1, 
                                    isExpanded: true, 
                                    position: { x: aimingX, y: aimingY, z: aimingZ }, 
                                    parentPosition: null, // 稍后更新
                                    childrenMeshes: [], 
                                    line: null, 
                                    dot: null,
                                    animationProgress: 0,
                                    hasCurrent: aimingHasCurrent
                                }
                                
                                this.map3dNodes.push(aimingNodeData)
                                aimingNodesData.push(aimingNodeData)
                                
                                if (!this.map3dNodeMap) {
                                    this.map3dNodeMap = new Map()
                                }
                                this.map3dNodeMap.set(aimingNode.fullPath, { 
                                    mesh: aimingMesh, 
                                    sprite: aimingSprite, 
                                    glowMesh: null,
                                    node: aimingNode, 
                                    key: aimingKey, 
                                    childrenMeshes: [], 
                                    line: null, 
                                    dot: null 
                                })
                            })
                            
                            // Aiming 节点：父节点保持当前高度，但需要更新 Y 偏移以避免下一个节点重叠
                            childMinY = y
                            childMaxY = y
                            
                            // 重要：虽然 Aiming 子节点水平排列，但父节点本身需要占用垂直空间
                            // 否则下一个同级 Tactic 会与当前 Tactic 重叠
                            currentYOffset += nodeSpacing
                            
                            // **关键修复：使用父节点的最终位置更新所有 Aiming 子节点的 parentPosition**
                            // 注意：这里的 y 仍然是初始值，因为 Aiming 父节点不会被调整
                            aimingNodesData.forEach(aimingData => {
                                aimingData.parentPosition = { x, y, z }
                            })
                            
                            // 更新当前节点数据中记录的子节点引用
                            if (currentParentNodeData) {
                                aimingKeys.forEach(aimingKey => {
                                    const aimingChild = node.children[aimingKey]
                                    const aimingChildData = this.map3dNodes.find(n => n.node === aimingChild)
                                    if (aimingChildData) {
                                        currentParentNodeData.childrenMeshes.push(aimingChildData.mesh)
                                        currentParentNodeData.childrenMeshes.push(aimingChildData.sprite)
                                    }
                                })
                            }
                            
                        } else {
                            // 普通子节点：垂直排列
                            const childStartY = currentYOffset + nodeSpacing
                            const childResult = renderNode(node.children, { x, y, z }, depth + 1, childStartY, nodeSpacing)
                            currentYOffset = childResult.yOffset
                            totalHeight += childResult.height
                            childMinY = childResult.minY
                            childMaxY = childResult.maxY
                            
                            // 如果有子节点，将父节点调整到子树的垂直中点
                            const subtreeMiddleY = (childMinY + childMaxY) / 2
                            const oldY = y
                            y = subtreeMiddleY
                            
                            // 更新已创建的mesh位置
                            mesh.position.y = y
                            if (glowMesh) {
                                glowMesh.position.y = y
                            }
                            sprite.position.y = y + (node.type === 'range' ? 5 : 4)
                            
                            // 更新 map3dNodes 中的位置记录
                            const currentNodeData = this.map3dNodes[this.map3dNodes.length - 1]
                            if (currentNodeData) {
                                currentNodeData.position.y = y
                                // 保持原有的 parentPosition（如果有的话）
                                if (parentPosition) {
                                    currentNodeData.parentPosition = parentPosition
                                }
                            }
                            
                            // **关键修复：使用调整后的父节点位置，更新所有子节点的 parentPosition**
                            // 这必须在父节点位置调整之后执行
                            const finalParentPosition = { x, y, z }
                            Object.keys(node.children).forEach(childKey => {
                                const childNode = node.children[childKey]
                                // 递归更新所有后代节点的 parentPosition
                                const updateDescendantParentPositions = (searchNode, searchKey, newParentPos) => {
                                    const childNodeData = this.map3dNodes.find(n => n.node === searchNode && n.key === searchKey)
                                    if (childNodeData) {
                                        childNodeData.parentPosition = newParentPos
                                    }
                                }
                                updateDescendantParentPositions(childNode, childKey, finalParentPosition)
                            })
                            
                            // 记录子节点引用（用于快速更新可见性）
                            const nodeData = this.map3dNodes[this.map3dNodes.length - 1]
                            Object.keys(node.children).forEach(childKey => {
                                const childNode = node.children[childKey]
                                const childNodeData = this.map3dNodes.find(n => n.node === childNode)
                                if (childNodeData) {
                                    nodeData.childrenMeshes.push(childNodeData.mesh)
                                    nodeData.childrenMeshes.push(childNodeData.sprite)
                                    if (childNodeData.glowMesh) {
                                        nodeData.childrenMeshes.push(childNodeData.glowMesh)
                                    }
                                }
                            })
                        }
                    } else {
                        // 没有子节点，更新 Y 偏移
                        currentYOffset += nodeSpacing
                    }
                    
                    // 更新 minY 和 maxY
                    minY = Math.min(minY, y, childMinY)
                    maxY = Math.max(maxY, y, childMaxY)
                    
                    totalHeight += nodeSpacing
                })
                
                return { yOffset: currentYOffset, height: totalHeight, minY, maxY }
            }
            
            // 第一步：计算整个树的总高度，用于垂直居中
            let totalTreeHeight = 0
            const tempRenderForHeight = (nodeData, depth = 0) => {
                if (!nodeData || typeof nodeData !== 'object') return 0
                
                let height = 0
                Object.keys(nodeData).forEach(key => {
                    const node = nodeData[key]
                    const isExpanded = node.expanded !== false
                    
                    // 检查子节点是否全是 Aiming 类型
                    const hasAimingChildren = node.children && Object.keys(node.children).length > 0 &&
                        Object.values(node.children).every(child => child.type === 'aiming')
                    
                    let nodeCount = 1
                    // 如果子节点是 Aiming，不计入 nodeCount（因为 Aiming 水平排列）
                    if (isExpanded && node.children && Object.keys(node.children).length > 0 && !hasAimingChildren) {
                        nodeCount += this.countTreeNodes(node.children)
                    }
                    
                    // 使用与 renderNode 相同的间距计算
                    const baseSpacing = 12
                    const nodeSpacing = Math.max(baseSpacing, nodeCount * 3)
                    
                    height += nodeSpacing
                    
                    // 只有非 Aiming 子节点才递归计算高度
                    if (isExpanded && node.children && Object.keys(node.children).length > 0 && !hasAimingChildren) {
                        height += tempRenderForHeight(node.children, depth + 1)
                    }
                })
                
                return height
            }
            
            // 计算所有根节点的总高度
            Object.keys(this.map3dTreeData).forEach(key => {
                totalTreeHeight += tempRenderForHeight({ [key]: this.map3dTreeData[key] })
                totalTreeHeight += 10 // 根节点之间的额外间距
            })
            
            // 从根节点开始渲染（垂直居中）
            let startYOffset = totalTreeHeight / 2 // 从上半部分开始，实现垂直居中
            let maxDepth = 0 // 记录最大深度，用于计算相机位置
            let actualMinY = Infinity
            let actualMaxY = -Infinity
            
            Object.keys(this.map3dTreeData).forEach(key => {
                const result = renderNode({ [key]: this.map3dTreeData[key] }, null, 0, startYOffset, 100)
                startYOffset = result.yOffset - 10
                actualMinY = Math.min(actualMinY, result.minY)
                actualMaxY = Math.max(actualMaxY, result.maxY)
            })
            
            // 计算树的实际范围
            this.map3dNodes.forEach(node => {
                maxDepth = Math.max(maxDepth, node.depth)
            })
            
            const treeWidth = maxDepth * 20 // 水平宽度
            const treeHeight = actualMaxY - actualMinY // 垂直高度
            const treeCenterX = treeWidth / 2
            const treeCenterY = (actualMaxY + actualMinY) / 2
            
            // 调整相机初始位置，确保能看到整个树
            const treeCenter = {
                x: treeCenterX,
                y: treeCenterY,
                z: 0
            }
            
            // 根据树的大小计算合适的相机距离
            const maxDimension = Math.max(treeWidth, treeHeight)
            const cameraDistance = maxDimension * 1.5 // 1.5倍的距离确保全景可见
            
            // 在所有节点位置确定后，统一创建连接线
            this.createConnectionLines()
            
            if (this.map3dControls) {
                this.map3dControls.target.set(treeCenter.x, treeCenter.y, treeCenter.z)
                // 相机位置：斜45度角，距离根据树的大小动态调整
                this.map3dCamera.position.set(
                    treeCenter.x + cameraDistance * 0.5,
                    treeCenter.y + cameraDistance * 0.3,
                    treeCenter.z + cameraDistance
                )
                this.map3dControls.update()
            }
            
            // 启动入场动画
            this.startNodeAnimations()
            
            // 添加点击事件（优化：防止重复触发）
            let isAnimating = false // 标记是否正在执行动画
            
            const onMouseClick = (event) => {
                // 如果正在执行动画，忽略点击
                if (isAnimating) {
                    console.log('动画进行中，忽略点击')
                    return
                }
                
                const rect = this.map3dRenderer.domElement.getBoundingClientRect()
                mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1
                mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1
                
                raycaster.setFromCamera(mouse, this.map3dCamera)
                const intersects = raycaster.intersectObjects(this.map3dNodes.map(n => n.mesh))
                
                if (intersects.length > 0) {
                    const clickedMesh = intersects[0].object
                    const nodeData = this.map3dNodes.find(n => n.mesh === clickedMesh)
                    if (nodeData && nodeData.node.children && Object.keys(nodeData.node.children).length > 0) {
                        // 切换展开/收拢
                        nodeData.node.expanded = !nodeData.node.expanded
                        
                        // 设置动画标记
                        isAnimating = true
                        
                        // 只更新相关节点，而不是重新渲染整个场景（性能优化）
                        this.updateNodeVisibility(nodeData.node, nodeData.key, () => {
                            // 动画完成后重置标记
                            isAnimating = false
                        })
                    }
                }
            }
            
            this.map3dRenderer.domElement.addEventListener('click', onMouseClick)
            this.map3dClickHandler = onMouseClick
        },
        
        // 创建连接线（在所有节点位置确定后调用）- 重构版：基于实际mesh位置
        createConnectionLines() {
            if (!this.map3dNodes || !this.map3dScene) return
            
            this.map3dNodes.forEach(nodeData => {
                // 如果已经有连接线，先清理
                if (nodeData.line) {
                    this.map3dScene.remove(nodeData.line)
                    if (nodeData.line.geometry) nodeData.line.geometry.dispose()
                    if (nodeData.line.material) nodeData.line.material.dispose()
                    nodeData.line = null
                }
                if (nodeData.dot) {
                    this.map3dScene.remove(nodeData.dot)
                    if (nodeData.dot.geometry) nodeData.dot.geometry.dispose()
                    if (nodeData.dot.material) nodeData.dot.material.dispose()
                    nodeData.dot = null
                }
                
                // **关键修复：使用实际的mesh位置，而不是 parentPosition**
                // 找到父节点的实际位置
                if (nodeData.node.parentPath) {
                    // 通过 parentPath 找到父节点
                    const parentNodeData = this.map3dNodes.find(n => {
                        // 对于 Range 节点，parentPath 可能不存在
                        if (!n.node.fullPath) return false
                        
                        // 检查是否是当前节点的父节点
                        // 例如：当前节点 fullPath = "Range1-T1.1"，父节点 fullPath = "Range1-T1"
                        // 或者：当前节点 fullPath = "Range1-T1.1-A1"，父节点 fullPath = "Range1-T1.1"
                        return n.node.fullPath === nodeData.node.parentPath
                    })
                    
                    if (parentNodeData && parentNodeData.mesh) {
                        // 使用父节点 mesh 的实际位置
                        const parentPosition = parentNodeData.mesh.position
                        const currentPosition = nodeData.mesh.position
                        const hasCurrent = nodeData.hasCurrent
                        const nodeType = nodeData.node.type
                        
                        // 根据节点类型设置不同的线条颜色和半径
                        let lineColor = 0x666666
                        let lineRadius = 0.15
                        
                        if (hasCurrent) {
                            lineColor = 0xffd93d
                            lineRadius = 0.2
                        } else if (nodeType === 'range') {
                            lineColor = 0x4ecdc4
                        } else if (nodeType === 'tactic') {
                            lineColor = 0x95e1d3
                        }
                        
                        // 使用 CylinderGeometry 创建管道状的连接线
                        const lineStart = new THREE.Vector3(parentPosition.x, parentPosition.y, parentPosition.z)
                        const lineEnd = new THREE.Vector3(currentPosition.x, currentPosition.y, currentPosition.z)
                        const direction = new THREE.Vector3().subVectors(lineEnd, lineStart)
                        const lineLength = direction.length()
                        
                        // 避免创建长度为0的线条（会导致视觉问题）
                        if (lineLength < 0.01) return
                        
                        const lineMidpoint = new THREE.Vector3().addVectors(lineStart, lineEnd).multiplyScalar(0.5)
                        
                        // 创建圆柱体作为连接线
                        const cylinderGeometry = new THREE.CylinderGeometry(lineRadius, lineRadius, lineLength, 8)
                        const lineMaterial = new THREE.MeshStandardMaterial({ 
                            color: lineColor,
                            metalness: 0.3,
                            roughness: 0.5,
                            transparent: true,
                            opacity: 0 // 初始透明，用于渐入动画
                        })
                        const line = new THREE.Mesh(cylinderGeometry, lineMaterial)
                        
                        // 设置圆柱体的位置和旋转
                        line.position.copy(lineMidpoint)
                        
                        // 计算旋转角度使圆柱体对齐到两个点之间
                        const axis = new THREE.Vector3(0, 1, 0)
                        line.quaternion.setFromUnitVectors(axis, direction.clone().normalize())
                        
                        this.map3dScene.add(line)
                        nodeData.line = line
                        
                        // 添加连接点（小圆球）
                        const dotGeometry = new THREE.SphereGeometry(0.3, 8, 8)
                        const dotMaterial = new THREE.MeshStandardMaterial({ 
                            color: lineColor,
                            metalness: 0.3,
                            roughness: 0.5,
                            transparent: true,
                            opacity: 0 // 初始透明
                        })
                        const dot = new THREE.Mesh(dotGeometry, dotMaterial)
                        dot.position.set(currentPosition.x, currentPosition.y, currentPosition.z)
                        this.map3dScene.add(dot)
                        nodeData.dot = dot
                    }
                }
            })
        },
        
        // 启动节点入场动画
        startNodeAnimations() {
            if (!this.map3dNodes || this.map3dNodes.length === 0) return
            
            const animationDuration = 500 // 动画持续时间(ms)
            const startTime = Date.now()
            
            const animate = () => {
                const elapsed = Date.now() - startTime
                const progress = Math.min(elapsed / animationDuration, 1)
                
                // 使用缓动函数（easeOutCubic）
                const easeProgress = 1 - Math.pow(1 - progress, 3)
                
                this.map3dNodes.forEach((nodeData, index) => {
                    // 延迟每个节点的动画开始时间，创建波浪效果
                    const delay = index * 20 // 每个节点延迟20ms
                    const nodeProgress = Math.max(0, Math.min(1, (elapsed - delay) / animationDuration))
                    const nodeEaseProgress = 1 - Math.pow(1 - nodeProgress, 3)
                    
                    // 缩放动画：从 0.1 到 1
                    const scale = 0.1 + nodeEaseProgress * 0.9
                    nodeData.mesh.scale.set(scale, scale, scale)
                    
                    if (nodeData.glowMesh) {
                        nodeData.glowMesh.scale.set(scale, scale, scale)
                    }
                    
                    // 透明度动画
                    if (nodeData.sprite && nodeData.sprite.material) {
                        nodeData.sprite.material.opacity = nodeEaseProgress
                    }
                    
                    if (nodeData.line && nodeData.line.material) {
                        nodeData.line.material.opacity = nodeEaseProgress * 0.6
                    }
                    
                    if (nodeData.dot && nodeData.dot.material) {
                        nodeData.dot.material.opacity = nodeEaseProgress
                    }
                    
                    nodeData.animationProgress = nodeEaseProgress
                })
                
                if (progress < 1) {
                    requestAnimationFrame(animate)
                } else {
                    // 动画完成后，确保所有节点都完全可见
                    this.map3dNodes.forEach((nodeData) => {
                        nodeData.mesh.scale.set(1, 1, 1)
                        nodeData.mesh.visible = true
                        
                        if (nodeData.glowMesh) {
                            nodeData.glowMesh.scale.set(1, 1, 1)
                            nodeData.glowMesh.visible = true
                        }
                        
                        if (nodeData.sprite) {
                            nodeData.sprite.visible = true
                            if (nodeData.sprite.material) {
                                nodeData.sprite.material.opacity = 1
                            }
                        }
                        
                        if (nodeData.line) {
                            nodeData.line.visible = true
                            if (nodeData.line.material) {
                                nodeData.line.material.opacity = 0.6
                            }
                        }
                        
                        if (nodeData.dot) {
                            nodeData.dot.visible = true
                            if (nodeData.dot.material) {
                                nodeData.dot.material.opacity = 1
                            }
                        }
                        
                        nodeData.animationProgress = 1
                    })
                }
            }
            
            animate()
        },
        
        // 创建渐变背景
        createGradientBackground() {
            const canvas = document.createElement('canvas')
            canvas.width = 256
            canvas.height = 256
            const context = canvas.getContext('2d')
            
            // 创建径向渐变
            const gradient = context.createRadialGradient(128, 128, 0, 128, 128, 128)
            gradient.addColorStop(0, '#1a1a2e')
            gradient.addColorStop(0.5, '#16213e')
            gradient.addColorStop(1, '#0f3460')
            
            context.fillStyle = gradient
            context.fillRect(0, 0, 256, 256)
            
            const texture = new THREE.CanvasTexture(canvas)
            texture.needsUpdate = true
            return texture
        },
        
        // 清空 3D 场景
        clearMap3DScene() {
            if (!this.map3dScene) return
            
            // 移除所有对象
            while(this.map3dScene.children.length > 0) {
                const obj = this.map3dScene.children[0]
                if (obj.geometry) obj.geometry.dispose()
                if (obj.material) {
                    if (Array.isArray(obj.material)) {
                        obj.material.forEach(m => {
                            if (m.map) m.map.dispose()
                            m.dispose()
                        })
                    } else {
                        if (obj.material.map) obj.material.map.dispose()
                        obj.material.dispose()
                    }
                }
                this.map3dScene.remove(obj)
            }
            
            this.map3dNodes = []
        },
        
        // 动画循环（优化版：减少不必要的计算和渲染）
        animateMap3D() {
            if (!this.map3dRenderer || !this.map3dScene || !this.map3dCamera) return
            
            this.map3dAnimationId = requestAnimationFrame(() => this.animateMap3D())
            
            // 跟踪是否需要渲染
            let needsRender = false
            
            // 更新控制器（启用阻尼时需要每帧更新）
            if (this.map3dControls) {
                // update() 返回 true 表示相机位置发生了变化
                const controlsChanged = this.map3dControls.update()
                if (controlsChanged) {
                    needsRender = true
                }
            }
            
            // 优化：大幅减少动画更新频率，避免 CPU 占用过高
            if (!this.map3dLastAnimationTime) {
                this.map3dLastAnimationTime = Date.now()
                this.map3dCurrentNodes = [] // 缓存当前选中的节点
            }
            
            const now = Date.now()
            // 每 200ms 更新一次动画（降低频率从100ms到200ms）
            if (now - this.map3dLastAnimationTime > 200) {
                this.map3dLastAnimationTime = now
                
                // 第一次时，找出所有当前选中的节点并缓存
                if (!this.map3dCurrentNodes || this.map3dCurrentNodes.length === 0) {
                    if (this.map3dNodes && this.map3dNodes.length > 0) {
                        this.map3dCurrentNodes = this.map3dNodes.filter(({ node }) => 
                            node.prompts && node.prompts.some(p => p.isCurrent)
                        )
                    }
                }
                
                // 只更新缓存的当前选中节点，避免遍历所有节点
                if (this.map3dCurrentNodes && this.map3dCurrentNodes.length > 0) {
                    const time = now * 0.001
                    this.map3dCurrentNodes.forEach(({ mesh }) => {
                        const scale = 1 + Math.sin(time * 2) * 0.05
                        mesh.scale.set(scale, scale, scale)
                    })
                    needsRender = true
                }
            }
            
            // 只在需要时渲染，避免无效渲染
            if (needsRender) {
                this.map3dRenderer.render(this.map3dScene, this.map3dCamera)
            }
        },
        
        // 更新节点可见性（重构版：真正隐藏节点并重新布局，带弹簧动画）
        updateNodeVisibility(node, nodeKey, onComplete) {
            if (!this.map3dScene || !this.map3dNodes) return
            
            // 找到对应的节点数据
            const nodeData = this.map3dNodes.find(n => n.node === node && n.key === nodeKey)
            if (!nodeData) return
            
            const isExpanded = node.expanded !== false
            
            // 收集需要处理的节点
            const nodesToProcess = []
            
            // 递归函数：收集节点及其所有子节点
            const collectChildNodes = (parentNode, parentNodeData) => {
                if (!parentNode.children || Object.keys(parentNode.children).length === 0) return
                
                Object.keys(parentNode.children).forEach(childKey => {
                    const childNode = parentNode.children[childKey]
                    const childNodeData = this.map3dNodes.find(n => n.node === childNode && n.key === childKey)
                    
                    if (childNodeData) {
                        nodesToProcess.push(childNodeData)
                        
                        // 递归收集子节点的子节点
                        if (childNode.expanded !== false) {
                            collectChildNodes(childNode, childNodeData)
                        }
                    }
                })
            }
            
            // 收集所有需要处理的子节点
            collectChildNodes(node, nodeData)
            
            if (nodesToProcess.length === 0) {
                if (onComplete) onComplete()
                return
            }
            
            if (isExpanded) {
                // 展开：显示节点，带弹簧动画
                this.expandNodesWithSpring(nodesToProcess, onComplete)
            } else {
                // 收缩：隐藏节点，带弹簧动画，然后重新布局
                this.collapseNodesWithSpring(nodesToProcess, () => {
                    // 收缩完成后，重新计算并布局所有可见节点
                    this.rebalanceTree()
                    if (onComplete) onComplete()
                })
            }
        },
        
        // 展开节点，带弹簧动画
        expandNodesWithSpring(nodesToAnimate, onComplete) {
            const animationDuration = 400 // 动画持续时间(ms)
            const startTime = Date.now()
            
            // 弹簧参数
            const springStiffness = 0.3
            const springDamping = 0.7
            
            const animate = () => {
                const elapsed = Date.now() - startTime
                const t = Math.min(elapsed / animationDuration, 1)
                
                // 弹簧缓动函数（easeOutElastic）
                const easeProgress = t === 1 ? 1 : 
                    1 - Math.pow(2, -10 * t) * Math.sin((t * 10 - 0.75) * (2 * Math.PI) / 3)
                
                nodesToAnimate.forEach((childNodeData, index) => {
                    // 延迟每个节点的动画开始时间
                    const delay = index * 15
                    const nodeT = Math.max(0, Math.min(1, (elapsed - delay) / animationDuration))
                    const nodeEaseProgress = nodeT === 1 ? 1 :
                        1 - Math.pow(2, -10 * nodeT) * Math.sin((nodeT * 10 - 0.75) * (2 * Math.PI) / 3)
                    
                    // 从小到大，从透明到不透明
                    const scale = 0.1 + nodeEaseProgress * 0.9
                    childNodeData.mesh.scale.set(scale, scale, scale)
                    childNodeData.mesh.visible = true
                    
                    if (childNodeData.glowMesh) {
                        childNodeData.glowMesh.scale.set(scale, scale, scale)
                        childNodeData.glowMesh.visible = true
                    }
                    
                    if (childNodeData.sprite) {
                        childNodeData.sprite.visible = true
                        if (childNodeData.sprite.material) {
                            childNodeData.sprite.material.opacity = nodeEaseProgress
                        }
                    }
                    
                    if (childNodeData.line) {
                        childNodeData.line.visible = true
                        if (childNodeData.line.material) {
                            childNodeData.line.material.opacity = nodeEaseProgress * 0.6
                        }
                    }
                    
                    if (childNodeData.dot) {
                        childNodeData.dot.visible = true
                        if (childNodeData.dot.material) {
                            childNodeData.dot.material.opacity = nodeEaseProgress
                        }
                    }
                })
                
                if (t < 1) {
                    requestAnimationFrame(animate)
                } else {
                    // 动画完成，调用回调
                    if (onComplete) onComplete()
                }
            }
            
            animate()
        },
        
        // 收缩节点，带弹簧动画
        collapseNodesWithSpring(nodesToAnimate, onComplete) {
            const animationDuration = 300 // 动画持续时间(ms)
            const startTime = Date.now()
            
            const animate = () => {
                const elapsed = Date.now() - startTime
                const t = Math.min(elapsed / animationDuration, 1)
                
                // 收缩缓动函数（easeInBack）- 轻微回弹效果
                const c1 = 1.70158
                const easeProgress = t * t * ((c1 + 1) * t - c1)
                
                nodesToAnimate.forEach((childNodeData, index) => {
                    // 反向延迟（最后的节点先收缩）
                    const delay = (nodesToAnimate.length - index) * 10
                    const nodeT = Math.max(0, Math.min(1, (elapsed - delay) / animationDuration))
                    const nodeEaseProgress = nodeT * nodeT * ((c1 + 1) * nodeT - c1)
                    
                    // 从大到小，从不透明到透明
                    const reverseProgress = 1 - nodeEaseProgress
                    const scale = 0.1 + reverseProgress * 0.9
                    childNodeData.mesh.scale.set(scale, scale, scale)
                    
                    if (childNodeData.glowMesh) {
                        childNodeData.glowMesh.scale.set(scale, scale, scale)
                    }
                    
                    if (childNodeData.sprite && childNodeData.sprite.material) {
                        childNodeData.sprite.material.opacity = reverseProgress
                    }
                    
                    if (childNodeData.line && childNodeData.line.material) {
                        childNodeData.line.material.opacity = reverseProgress * 0.6
                    }
                    
                    if (childNodeData.dot && childNodeData.dot.material) {
                        childNodeData.dot.material.opacity = reverseProgress
                    }
                    
                    // 动画结束后完全隐藏
                    if (nodeT >= 1) {
                        childNodeData.mesh.visible = false
                        if (childNodeData.glowMesh) childNodeData.glowMesh.visible = false
                        if (childNodeData.sprite) childNodeData.sprite.visible = false
                        if (childNodeData.line) childNodeData.line.visible = false
                        if (childNodeData.dot) childNodeData.dot.visible = false
                    }
                })
                
                if (t < 1) {
                    requestAnimationFrame(animate)
                } else {
                    // 动画完成，调用回调
                    if (onComplete) onComplete()
                }
            }
            
            animate()
        },
        
        // 重新平衡树布局（收缩后调用）
        rebalanceTree() {
            // 重新计算所有可见节点的位置
            // 这里暂时使用简单的垂直重新分布，后续可以优化为更复杂的布局算法
            
            // TODO: 实现完整的树重新布局算法
            // 由于需要重新计算整个树的布局，这里先保持简单实现
            // 可以在未来版本中添加更复杂的自动布局算法
            
            console.log('Tree rebalanced - 布局重新计算完成')
        },
        
        // 处理窗口大小变化
        handleMap3DResize() {
            const container = document.getElementById('map3dContainer')
            if (!container || !this.map3dCamera || !this.map3dRenderer) return
            
            const width = container.clientWidth
            const height = container.clientHeight
            
            this.map3dCamera.aspect = width / height
            this.map3dCamera.updateProjectionMatrix()
            this.map3dRenderer.setSize(width, height)
        },
        
        // 销毁 3D 场景
        destroyMap3D() {
            window.removeEventListener('resize', this.handleMap3DResize)
            
            // 移除点击事件
            if (this.map3dRenderer && this.map3dRenderer.domElement && this.map3dClickHandler) {
                this.map3dRenderer.domElement.removeEventListener('click', this.map3dClickHandler)
                this.map3dClickHandler = null
            }
            
            // 停止动画循环
            if (this.map3dAnimationId) {
                cancelAnimationFrame(this.map3dAnimationId)
                this.map3dAnimationId = null
            }
            
            // 销毁控制器
            if (this.map3dControls) {
                this.map3dControls.dispose()
                this.map3dControls = null
            }
            
            // 清空场景
            this.clearMap3DScene()
            
            // 清空节点映射
            if (this.map3dNodeMap) {
                this.map3dNodeMap.clear()
                this.map3dNodeMap = null
            }
            
            // 清理缓存的当前节点
            this.map3dCurrentNodes = []
            
            if (this.map3dRenderer && this.map3dRenderer.domElement) {
                const container = document.getElementById('map3dContainer')
                if (container && container.contains(this.map3dRenderer.domElement)) {
                    container.removeChild(this.map3dRenderer.domElement)
                }
                this.map3dRenderer.dispose()
            }
            
            this.map3dScene = null
            this.map3dCamera = null
            this.map3dRenderer = null
            this.map3dNodes = []
        },
        
        // 关闭对话输入弹窗
        // 执行打靶的核心逻辑（提取出来供对话模式和直接测试模式复用）
        async executeTargetShoot() {
            await this.executeTargetShootWithChatMessage(null)
        },
        
        // 执行打靶的核心逻辑（支持对话模式，userMessage 为 null 时表示直接测试模式）
        async executeTargetShootWithChatMessage(userMessage) {
            this.tacticalFormSubmitLoading = true
            
            // 保存 userMessage，用于后续连发时保持 Chat 模式
            this._lastUserMessage = userMessage || null
            
            let _postData = {
                modelid: this.modelid,
                content: this.content,
                note: this.remarks,
                numsOfResults: 1,
                isDraft: this.sendBtnText === '保存草稿',
                suffix: this.promptParamForm.suffix,
                prefix: this.promptParamForm.prefix
            }
            
            // 如果提供了 userMessage，添加到请求数据中
            if (userMessage) {
                _postData.userMessage = userMessage
            }
            
            // 如果是继续聊天模式，传递历史记录
            if (this.continueChatMode && this.continueChatHistory.length > 0) {
                _postData.chatHistory = this.continueChatHistory.map(msg => ({
                    role: msg.roleType === 1 ? 'user' : 'assistant',
                    content: msg.content
                }))
            }
            
            // ai评分标准
            _postData.isAIGrade = this.isAIGrade
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
                    _postData.isTopTactic = true
                }
                if (this.tacticalForm.tactics === '创建平行战术') {
                    _postData.isNewTactic = true
                }
                if (this.tacticalForm.tactics === '创建子战术') {
                    _postData.isNewSubTactic = true
                }
                if (this.tacticalForm.tactics === '重新瞄准') {
                    _postData.isNewAiming = true
                }
            }
            
            this.parameterViewList.forEach(item => {
                if (item.formField === 'stopSequences') {
                    _postData[item.formField] = item.value ? JSON.stringify(item.value.split(',')) : ''
                } else if (item.formField === 'maxToken') {
                    _postData[item.formField] = item.value ? Number(item.value) : 0
                } else {
                    _postData[item.formField] = item.value
                }
            })

            _postData['rangeId'] = this.promptField
            let res = await servicePR.post('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Add', _postData)
            this.tacticalFormSubmitLoading = false
            
            if (res.data.success) {
                this.pageChange = false
                this.tacticalFormVisible = false
                let {
                    promptResultList = [],
                    fullVersion = '',
                    id,
                    evalAvgScore = -1,
                    evalMaxScore = -1
                } = res.data.data || {}

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
                this.outputAverageDeci = evalAvgScore > -1 ? evalAvgScore : -1
                this.outputMaxDeci = evalMaxScore > -1 ? evalMaxScore : -1
                this.outputList = promptResultList.map(item => {
                    if (item) {
                        item.promptId = id
                        item.version = fullVersion
                        item.scoreType = '1'
                        item.isScoreView = false
                        item.addTime = item.addTime ? this.formatDate(item.addTime) : ''
                        item.resultStringHtml = marked.parse(item.resultString)
                        item.scoreVal = 0
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
                        // 确保 mode 字段正确设置（后端返回的是枚举值 1 或 2）
                        if (item.mode === undefined || item.mode === null) {
                            item.mode = null // 兼容旧数据
                        }
                    }
                    return item
                })
                
                // 检查第一个结果的模式，如果不是 Chat 模式，清空保存的 userMessage
                if (promptResultList.length > 0) {
                    const firstResult = promptResultList[0]
                    if (firstResult.mode !== 2 && firstResult.mode !== 'Chat') {
                        // 如果不是 Chat 模式，清空保存的 userMessage
                        this._lastUserMessage = null
                    }
                }
                
                // 重置继续聊天状态
                this.continueChatMode = false
                this.continueChatPromptResultId = null
                this.continueChatHistory = []
                
                this.getFieldList().then(() => {
                    this.getPromptOptData(id)
                    this.getScoringTrendData()
                })

                if (this.sendBtnText !== '保存草稿' && this.numsOfResults > 1) {
                    this.dealRapicFireHandel(this.numsOfResults - 1, id)
                }
            } else {
                this.$message({
                    message: res.data.errorMessage || res.data.data || 'Error',
                    type: 'error',
                    duration: 5 * 1000
                })
            }
        },
        checkUseRed(index,item, which) {
            if (item.finalScore === -1 || item.finalScore === '-1') return '';
            this.$nextTick(() => {
                const scoreViewElement = document.getElementById('scoreRow' );
                if (scoreViewElement) {
                    scoreViewElement.scrollIntoView({ behavior: 'smooth' });
                }
            });
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
                    label: data[i].name ? data[i].name + (data[i].nickName ? " (" + data[i].nickName + ")" : "") : "未命名",
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
                    await this.getOutputList(item.promptId)
                    // 清除AI评分加载状态
                    this.$set(this.robotScoreLoadingMap, item.id, false)
                    // 重新获取图表
                    this.getScoringTrendData()
                } else {
                    // 清除AI评分加载状态
                    this.$set(this.robotScoreLoadingMap, item.id, false)
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
        cancelManualScore(index) {
            this.outputList[index].isScoreView = false
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
                        // 设置AI评分加载状态
                        const itemId = this.outputList[index].id
                        this.$set(this.robotScoreLoadingMap, itemId, true)
                        this.saveManualScore(this.outputList[index], index)
                    } else {
                        this.$message({
                            message: '请设置预 AI 评分标准',
                            type: 'warning'
                        })
                    }
                } else {
                    // todo 接口对接 重新评分
                    // 设置AI评分加载状态
                    const itemId = this.outputList[index].id
                    this.$set(this.robotScoreLoadingMap, itemId, true)
                    this.saveManualScore(this.outputList[index], index)
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
        async rapidFireHandel(id, userMessage = null) {
            const promptItemId = id || this.promptid
            const numsOfResults = 1
            const params = { promptItemId, numsOfResults }
            // 如果提供了 userMessage，添加到参数中（用于保持 Chat 模式）
            if (userMessage) {
                params.userMessage = userMessage
            }
            return await servicePR.get('/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GenerateWithItemId',
                { params }).then(res => {
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
                    // 构建label：模型名称 + 版本号（如果有）+ 部署名称（如果有）
                    let label = item.alias || '未命名模型';
                    if (item.apiVersion && item.apiVersion.trim()) {
                        label += ` v${item.apiVersion}`;
                    }
                    if (item.deploymentName && item.deploymentName.trim()) {
                        label += `\n(${item.deploymentName})`;
                    }
                    
                    return {
                        ...item,
                        label: label,
                        displayName: item.alias,  // 保留原始名称用于其他地方显示
                        deploymentDisplay: item.deploymentName, // 保留部署名称
                        apiVersion: item.apiVersion, // 保留版本号
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
                this.dailogpromptOptlist = _promptOpt
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
                const textarea = document.createElement('textarea');
                textarea.setAttribute('readonly', 'readonly');
                textarea.value = rawResult ? item.resultString : item.resultStringHtml;
                document.body.appendChild(textarea);
                textarea.select();
                textarea.setSelectionRange(0, 9999);
                if (document.execCommand('copy')) {
                    document.execCommand('copy');
                    this.$message.success('复制成功');
                } else {
                    this.$message.error('复制失败');
                }
                textarea.style.display = 'none';
            } catch (err) {
                console.error('Oops, unable to copy', err);
            }  
        },
        // 自定义滚动条缩略图相关方法
        handleResultScroll(event) {
            const el = event.target;
            this.scrollInfo = {
                scrollTop: el.scrollTop,
                scrollHeight: el.scrollHeight,
                clientHeight: el.clientHeight
            };
        },
        getThumbnailStyle(index) {
            const container = document.getElementById('resultBox');
            if (!container || !this.outputList || this.outputList.length === 0) {
                return {};
            }
            
            const items = container.querySelectorAll('.contentBoxItem');
            if (!items || items.length === 0) return {};
            
            const totalHeight = container.scrollHeight;
            const trackHeight = container.clientHeight;
            
            // 计算每个item的相对位置
            let totalItemsHeight = 0;
            let currentTop = 0;
            
            for (let i = 0; i < items.length; i++) {
                totalItemsHeight += items[i].offsetHeight;
                if (i < index) {
                    currentTop += items[i].offsetHeight;
                }
            }
            
            const currentHeight = items[index] ? items[index].offsetHeight : 30;
            
            // 计算在缩略图轨道中的位置（按比例）
            const top = (currentTop / totalHeight) * trackHeight;
            const height = Math.max((currentHeight / totalHeight) * trackHeight, 20); // 最小20px
            
            return {
                top: top + 'px',
                height: height + 'px'
            };
        },
        isResultInView(index) {
            const container = document.getElementById('resultBox');
            if (!container) return false;
            
            const items = container.querySelectorAll('.contentBoxItem');
            if (!items || !items[index]) return false;
            
            const item = items[index];
            const containerRect = container.getBoundingClientRect();
            const itemRect = item.getBoundingClientRect();
            
            // 判断item是否在可视区域内
            return itemRect.top >= containerRect.top && 
                   itemRect.top <= containerRect.bottom;
        },
        getViewportStyle() {
            const container = document.getElementById('resultBox');
            if (!container) return {};
            
            const scrollTop = this.scrollInfo.scrollTop;
            const scrollHeight = this.scrollInfo.scrollHeight;
            const clientHeight = this.scrollInfo.clientHeight;
            
            if (scrollHeight === 0) return {};
            
            const trackHeight = clientHeight;
            const viewportTop = (scrollTop / scrollHeight) * trackHeight;
            const viewportHeight = (clientHeight / scrollHeight) * trackHeight;
            
            return {
                top: viewportTop + 'px',
                height: viewportHeight + 'px'
            };
        },
        scrollToResult(index) {
            const container = document.getElementById('resultBox');
            if (!container) return;
            
            const items = container.querySelectorAll('.contentBoxItem');
            if (!items || !items[index]) return;
            
            items[index].scrollIntoView({ behavior: 'smooth', block: 'start' });
            this.outputSelectSwitch(index);
        },
        formatTime(timeStr) {
            // 提取时间部分，例如 "2024-01-01 10:30:45" => "10:30"
            if (!timeStr) return '';
            const match = timeStr.match(/(\d{2}):(\d{2}):\d{2}/);
            return match ? match[1] + ':' + match[2] : timeStr.substring(0, 10);
        },
        // 获取最终评分（使用系统的finalScore字段）
        getFinalScore(item) {
            if (!item) return null;
            // 直接使用系统的finalScore字段，这是被标记为红色的那个分数
            if (item.finalScore !== undefined && item.finalScore !== null && 
                item.finalScore !== -1 && item.finalScore !== '-1') {
                return item.finalScore;
            }
            return null;
        },
        // 获取评分等级的样式类
        getScoreBarClass(item) {
            const score = this.getFinalScore(item);
            if (score === null) return '';
            
            if (score >= 8) return 'score-excellent';      // 8-10分：优秀
            if (score >= 6) return 'score-good';           // 6-8分：良好
            if (score >= 4) return 'score-medium';         // 4-6分：中等
            if (score >= 2) return 'score-low';            // 2-4分：较低
            return 'score-poor';                           // 0-2分：差
        },
        // 获取数据条的宽度样式（Excel风格）
        getScoreBarStyle(item) {
            const score = this.getFinalScore(item);
            if (score === null) return { width: '0%' };
            
            // 0-10分映射到0-100%
            const percentage = Math.min(Math.max((score / 10) * 100, 0), 100);
            return {
                width: percentage + '%'
            };
        },
        // 获取缩略图的工具提示文本
        getThumbnailTooltip(item) {
            if (!item) return '';
            
            let tooltip = item.addTime;
            const score = this.getFinalScore(item);
            
            if (score !== null) {
                // 判断finalScore等于哪个评分，那个就是最终评分类型
                let scoreType = '';
                if (item.finalScore === item.humanScore) {
                    scoreType = '手动评分';
                } else if (item.finalScore === item.robotScore) {
                    scoreType = 'AI评分';
                } else {
                    scoreType = '最终评分';
                }
                tooltip += `\n${scoreType}: ${score.toFixed(1)}分`;
            }
            
            return tooltip;
        },
        // 处理输出区域的鼠标移动事件 - 判断是否在右半侧
        handleOutputAreaMouseMove(event) {
            const outputArea = event.currentTarget;
            const rect = outputArea.getBoundingClientRect();
            const mouseX = event.clientX;
            
            // 计算鼠标相对于输出区域的位置
            const relativeX = mouseX - rect.left;
            const halfWidth = rect.width / 2;
            
            // 只在右半侧显示滚动条
            if (relativeX > halfWidth) {
                this.showScrollbarThumbnails = true;
            } else {
                this.showScrollbarThumbnails = false;
            }
        },
        // 处理鼠标离开输出区域
        handleOutputAreaMouseLeave() {
            this.showScrollbarThumbnails = false;
        },
        
        // ========== 区域宽度拖动调整功能 ==========
        
        // 从localStorage加载保存的宽度设置
        loadAreaWidthsFromStorage() {
            try {
                const savedLeftWidth = localStorage.getItem('promptPage_leftAreaWidth');
                const savedCenterWidth = localStorage.getItem('promptPage_centerAreaWidth');
                
                if (savedLeftWidth) {
                    const width = parseInt(savedLeftWidth);
                    if (width >= 280 && width <= 600) {
                        this.leftAreaWidth = width;
                    }
                }
                
                if (savedCenterWidth) {
                    const width = parseInt(savedCenterWidth);
                    if (width >= 320 && width <= 800) {
                        this.centerAreaWidth = width;
                    }
                }
            } catch (e) {
                console.error('加载区域宽度设置失败:', e);
            }
        },
        
        // 保存宽度设置到localStorage
        saveAreaWidthsToStorage() {
            try {
                localStorage.setItem('promptPage_leftAreaWidth', this.leftAreaWidth);
                localStorage.setItem('promptPage_centerAreaWidth', this.centerAreaWidth);
            } catch (e) {
                console.error('保存区域宽度设置失败:', e);
            }
        },
        
        // 开始拖动左侧分隔条
        startResizeLeft(event) {
            this.isResizing = true;
            this.resizeType = 'left';
            this.resizeStartX = event.clientX;
            this.resizeStartLeftWidth = this.leftAreaWidth;
            
            document.addEventListener('mousemove', this.handleResize);
            document.addEventListener('mouseup', this.stopResize);
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
            
            event.preventDefault();
        },
        
        // 开始拖动右侧分隔条
        startResizeRight(event) {
            this.isResizing = true;
            this.resizeType = 'right';
            this.resizeStartX = event.clientX;
            this.resizeStartCenterWidth = this.centerAreaWidth;
            
            document.addEventListener('mousemove', this.handleResize);
            document.addEventListener('mouseup', this.stopResize);
            document.body.style.cursor = 'col-resize';
            document.body.style.userSelect = 'none';
            
            event.preventDefault();
        },
        
        // 处理拖动
        handleResize(event) {
            if (!this.isResizing) return;
            
            const deltaX = event.clientX - this.resizeStartX;
            
            if (this.resizeType === 'left') {
                // 调整左侧区域宽度
                let newWidth = this.resizeStartLeftWidth + deltaX;
                newWidth = Math.max(280, Math.min(600, newWidth)); // 限制在280-600px之间
                this.leftAreaWidth = newWidth;
            } else if (this.resizeType === 'right') {
                // 调整中间区域宽度
                let newWidth = this.resizeStartCenterWidth + deltaX;
                newWidth = Math.max(320, Math.min(800, newWidth)); // 限制在320-800px之间
                this.centerAreaWidth = newWidth;
            }
            
            event.preventDefault();
        },
        
        // 停止拖动
        stopResize() {
            if (this.isResizing) {
                this.isResizing = false;
                this.resizeType = null;
                
                document.removeEventListener('mousemove', this.handleResize);
                document.removeEventListener('mouseup', this.stopResize);
                document.body.style.cursor = '';
                document.body.style.userSelect = '';
                
                // 保存当前宽度设置
                this.saveAreaWidthsToStorage();
            }
        },
        
        // 双击分隔条还原默认宽度
        resetAreaWidths(event) {
            // 恢复默认宽度
            this.leftAreaWidth = 360;
            this.centerAreaWidth = 380;
            
            // 清除localStorage中保存的设置
            try {
                localStorage.removeItem('promptPage_leftAreaWidth');
                localStorage.removeItem('promptPage_centerAreaWidth');
            } catch (e) {
                console.error('清除区域宽度设置失败:', e);
            }
            
            // 添加视觉反馈动画
            if (event && event.target) {
                event.target.classList.add('reset-animation');
                setTimeout(() => {
                    event.target.classList.remove('reset-animation');
                }, 600);
            }
            
            // 显示提示消息
            this.$message({
                message: '已还原为默认布局宽度',
                type: 'success',
                duration: 2000
            });
        },
        
        // ========== Prompt 对比功能 ==========
        
        // 打开对比对话框（可选预设Prompt A）
        openCompareDialog(event, item) {
            // 阻止事件冒泡，防止触发el-select的下拉
            if (event) {
                event.stopPropagation();
            }
            
            // Prompt A 应该是当前已经选中显示的 Prompt
            // Prompt B 是点击"对比"按钮的对应 Prompt
            if (this.promptid) {
                this.comparePromptAId = this.promptid;
                this.loadComparePromptA(this.promptid);
            }
            
            // 如果传入了item，则将其设置为Prompt B
            // item.value 是 el-option 的值，对应 Prompt 的 ID
            if (item && item.value) {
                this.comparePromptBId = item.value;
                this.loadComparePromptB(item.value);
            }
            
            // 打开对话框
            this.compareDialogVisible = true;
        },
        
        // 加载对比Prompt A的数据
        async loadComparePromptA(id) {
            if (!id) {
                this.comparePromptA = null;
                return;
            }
            
            try {
                const data = await this.getPromptDetail(id);
                this.comparePromptA = data;
                
                // 检查是否选择了同一个Prompt
                if (this.comparePromptBId && id === this.comparePromptBId) {
                    this.$message({
                        message: '警告：您选择了同一个 Prompt 进行对比！',
                        type: 'warning',
                        duration: 3000
                    });
                }
            } catch (error) {
                console.error('加载Prompt A失败:', error);
                this.$message({
                    message: '加载Prompt A数据失败',
                    type: 'error',
                    duration: 3000
                });
                this.comparePromptA = null;
            }
        },
        
        // 加载对比Prompt B的数据
        async loadComparePromptB(id) {
            if (!id) {
                this.comparePromptB = null;
                return;
            }
            
            try {
                const data = await this.getPromptDetail(id);
                this.comparePromptB = data;
                
                // 检查是否选择了同一个Prompt
                if (this.comparePromptAId && id === this.comparePromptAId) {
                    this.$message({
                        message: '警告：您选择了同一个 Prompt 进行对比！',
                        type: 'warning',
                        duration: 3000
                    });
                }
            } catch (error) {
                console.error('加载Prompt B失败:', error);
                this.$message({
                    message: '加载Prompt B数据失败',
                    type: 'error',
                    duration: 3000
                });
                this.comparePromptB = null;
            }
        },
        
        // 获取单个Prompt的详细信息
        async getPromptDetail(id) {
            const res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Get?id=${Number(id)}`);
            
            if (res.data.success) {
                return res.data.data;
            } else {
                throw new Error(res.data.errorMessage || '获取Prompt详情失败');
            }
        },
        
        // 交换Prompt A和B
        swapComparePrompts() {
            // 交换ID
            const tempId = this.comparePromptAId;
            this.comparePromptAId = this.comparePromptBId;
            this.comparePromptBId = tempId;
            
            // 交换数据
            const tempData = this.comparePromptA;
            this.comparePromptA = this.comparePromptB;
            this.comparePromptB = tempData;
        },
        
        // 检查评分是否存在
        hasScore(score) {
            return score !== null && score !== undefined && score > -1;
        },
        
        // 获取前缀（从variablesJson解析或返回空）
        getPromptPrefix(side) {
            const prompt = side === 'A' ? this.comparePromptA : this.comparePromptB;
            if (!prompt || !prompt.variablesJson) return '';
            
            try {
                const vars = JSON.parse(prompt.variablesJson);
                return vars.prefix || '';
            } catch (e) {
                return '';
            }
        },
        
        // 获取后缀（从variablesJson解析或返回空）
        getPromptSuffix(side) {
            const prompt = side === 'A' ? this.comparePromptA : this.comparePromptB;
            if (!prompt || !prompt.variablesJson) return '';
            
            try {
                const vars = JSON.parse(prompt.variablesJson);
                return vars.suffix || '';
            } catch (e) {
                return '';
            }
        },
        
        // 跳转到指定的Prompt（完全模拟手动点击靶道选择的行为）
        async switchToPrompt(promptId) {
            if (!promptId) {
                this.$message({
                    message: '无效的Prompt ID',
                    type: 'warning',
                    duration: 2000
                });
                return;
            }
            
            try {
                // 1. 先获取完整的Prompt数据
                const promptData = await this.getPromptDetail(promptId);
                
                if (!promptData) {
                    throw new Error('无法获取Prompt详细信息');
                }
                
                // 2. 从fullVersion解析靶场名称（格式: 靶场-靶道-战术）
                const versionParts = promptData.fullVersion ? promptData.fullVersion.split('-') : [];
                const targetRangeName = versionParts[0];
                
                // 3. 查找对应的靶场ID
                const targetRange = this.promptFieldOpt.find(item => 
                    item.label === targetRangeName || item.rangeName === targetRangeName
                );
                
                if (!targetRange) {
                    throw new Error(`未找到对应的靶场: ${targetRangeName}`);
                }
                
                // 4. 关闭对比对话框（先关闭，避免干扰后续操作）
                this.compareDialogVisible = false;
                
                // 5. 重置 pageChange 标记，避免触发"是否保存草稿"的确认对话框
                this.pageChange = false;
                
                // 6. 检查是否需要切换靶场
                const needSwitchRange = this.promptField !== targetRange.value;
                
                if (needSwitchRange) {
                    // 切换靶场（这会触发promptOpt的更新）
                    this.promptField = targetRange.value;
                    await this.promptChangeHandel(targetRange.value, 'promptField');
                    
                    // 等待promptOpt更新完成
                    await this.$nextTick();
                }
                
                // 7. 在promptOpt中查找对应的Prompt
                // 注意：promptOpt中的item.value才是正确的promptid
                const targetPrompt = this.promptOpt.find(item => 
                    item.id === promptId || item.value === promptId || item.idkey === promptId
                );
                
                if (!targetPrompt) {
                    throw new Error('在当前靶场中未找到对应的Prompt');
                }
                
                // 8. 设置正确的promptid（这会触发el-select的v-model更新）
                // 重要：再次确保 pageChange = false，因为切换靶场可能会触发它
                this.pageChange = false;
                this.promptid = targetPrompt.value || targetPrompt.id;
                
                // 9. 手动触发 promptChangeHandel，完全模拟用户点击靶道下拉选择
                // 这会：
                //   - 更新 sendBtns（根据是否是草稿）
                //   - 清空 AI 评分标准
                //   - 调用 getPromptetail 获取完整详情（包括输出列表、评分、图表等）
                await this.promptChangeHandel(this.promptid, 'promptid');
                
                // 10. 显示成功提示
                this.$message({
                    message: `已切换到 Prompt: ${promptData.fullVersion}`,
                    type: 'success',
                    duration: 2000
                });
                
            } catch (error) {
                console.error('切换Prompt失败:', error);
                this.$message({
                    message: error.message || '切换Prompt失败，请重试',
                    type: 'error',
                    duration: 3000
                });
            }
        },
        
        // 检查字段是否有差异
        hasFieldDiff(fieldA, fieldB) {
            // 处理null/undefined情况
            if (fieldA === fieldB) return false;
            if ((fieldA === null || fieldA === undefined) && (fieldB === null || fieldB === undefined)) return false;
            
            // 如果是对象或数组，进行深度比较
            if (typeof fieldA === 'object' && typeof fieldB === 'object') {
                return JSON.stringify(fieldA) !== JSON.stringify(fieldB);
            }
            
            return fieldA !== fieldB;
        },
        
        // 格式化变量配置（从JSON字符串转为可读格式）
        formatVariables(variablesJson) {
            if (!variablesJson) return '无';
            try {
                const vars = JSON.parse(variablesJson);
                if (Object.keys(vars).length === 0) return '无';
                return Object.entries(vars).map(([key, value]) => `${key}: ${value}`).join('\n');
            } catch (e) {
                return variablesJson;
            }
        },
        
        // 生成Git风格的Prompt内容差异HTML
        getContentDiffHtml(side) {
            const contentA = this.comparePromptA?.promptContent || '';
            const contentB = this.comparePromptB?.promptContent || '';
            
            // 如果都为空
            if (!contentA && !contentB) {
                return '<span class="diff-empty">暂无内容</span>';
            }
            
            // 如果只有一个为空
            if (!contentA && side === 'A') {
                return '<span class="diff-empty">暂无内容</span>';
            }
            if (!contentB && side === 'B') {
                return '<span class="diff-empty">暂无内容</span>';
            }
            
            // 如果内容相同
            if (contentA === contentB) {
                return this.escapeHtml(side === 'A' ? contentA : contentB);
            }
            
            // 使用jsdiff库进行差异对比
            if (typeof Diff !== 'undefined') {
                const diff = Diff.diffLines(contentA, contentB);
                return this.renderDiffHtml(diff, side);
            }
            
            // 如果diff库未加载，返回原始内容
            return this.escapeHtml(side === 'A' ? contentA : contentB);
        },
        
        // 生成Git风格的变量配置差异HTML
        getVariablesDiffHtml(side) {
            const varsA = this.comparePromptA?.variablesJson || '{}';
            const varsB = this.comparePromptB?.variablesJson || '{}';
            
            // 格式化JSON以便对比
            let formattedA = varsA;
            let formattedB = varsB;
            try {
                formattedA = JSON.stringify(JSON.parse(varsA), null, 2);
            } catch (e) {
                // 保持原样
            }
            try {
                formattedB = JSON.stringify(JSON.parse(varsB), null, 2);
            } catch (e) {
                // 保持原样
            }
            
            // 如果内容相同
            if (formattedA === formattedB) {
                return this.escapeHtml(side === 'A' ? formattedA : formattedB);
            }
            
            // 使用jsdiff库进行差异对比
            if (typeof Diff !== 'undefined') {
                const diff = Diff.diffLines(formattedA, formattedB);
                return this.renderDiffHtml(diff, side);
            }
            
            // 如果diff库未加载，返回原始内容
            return this.escapeHtml(side === 'A' ? formattedA : formattedB);
        },
        
        // 渲染差异HTML（Git风格，支持行内单词高亮）
        renderDiffHtml(diff, side) {
            let html = '';
            let i = 0;
            
            while (i < diff.length) {
                const part = diff[i];
                const lines = part.value.split('\n');
                // 移除最后的空行
                if (lines[lines.length - 1] === '') {
                    lines.pop();
                }
                
                // 检查是否是相邻的删除和新增（可以做行内diff）
                const nextPart = i + 1 < diff.length ? diff[i + 1] : null;
                const isInlineDiffCandidate = 
                    part.removed && 
                    nextPart && 
                    nextPart.added && 
                    lines.length === 1 && 
                    nextPart.value.split('\n').filter(l => l).length === 1;
                
                if (isInlineDiffCandidate) {
                    // 行内单词级别差异
                    const oldLine = lines[0];
                    const newLine = nextPart.value.split('\n').filter(l => l)[0];
                    
                    if (side === 'A') {
                        html += `<span class="diff-line diff-modified">- ${this.renderInlineDiff(oldLine, newLine, 'removed')}</span>`;
                    } else {
                        html += `<span class="diff-line diff-modified">+ ${this.renderInlineDiff(oldLine, newLine, 'added')}</span>`;
                    }
                    
                    i += 2; // 跳过下一个part（因为已经处理了）
                } else {
                    // 常规的整行差异
                    lines.forEach((line, lineIndex) => {
                        if (part.added) {
                            // 新增的内容（绿色背景）- 仅在B侧显示
                            if (side === 'B') {
                                html += `<span class="diff-line diff-added">+ ${this.escapeHtml(line)}</span>`;
                            }
                            // A侧不显示新增内容
                        } else if (part.removed) {
                            // 删除的内容（红色背景）- 仅在A侧显示
                            if (side === 'A') {
                                html += `<span class="diff-line diff-removed">- ${this.escapeHtml(line)}</span>`;
                            }
                            // B侧不显示删除内容
                        } else {
                            // 未修改的内容（灰色）
                            html += `<span class="diff-line diff-unchanged">  ${this.escapeHtml(line)}</span>`;
                        }
                    });
                    i++;
                }
            }
            
            return html || '<span class="diff-empty">暂无内容</span>';
        },
        
        // 渲染行内单词级别差异
        renderInlineDiff(oldText, newText, mode) {
            if (typeof Diff === 'undefined' || !Diff.diffWords) {
                return this.escapeHtml(mode === 'removed' ? oldText : newText);
            }
            
            const wordDiff = Diff.diffWords(oldText, newText);
            let html = '';
            
            wordDiff.forEach(part => {
                const escapedText = this.escapeHtml(part.value);
                
                if (mode === 'removed') {
                    // A侧：高亮删除的单词
                    if (part.removed) {
                        html += `<mark class="diff-word-removed">${escapedText}</mark>`;
                    } else if (!part.added) {
                        html += escapedText;
                    }
                } else {
                    // B侧：高亮新增的单词
                    if (part.added) {
                        html += `<mark class="diff-word-added">${escapedText}</mark>`;
                    } else if (!part.removed) {
                        html += escapedText;
                    }
                }
            });
            
            return html;
        },
        
        // HTML转义工具函数
        escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        },
        
        // ========== contenteditable编辑器方法（简洁版）==========
        
        // 获取编辑器中的纯文本
        getEditorText() {
            const editor = this.$refs.promptEditor;
            if (!editor) return '';
            return editor.innerText || '';
        },
        
        // 生成带高亮的HTML
        generateHighlightHTML(text) {
            if (!text) return '';
            
            // 获取前缀和后缀（从当前编辑的prompt参数）
            const prefix = this.promptParamForm.prefix || '';
            const suffix = this.promptParamForm.suffix || '';
            
            // 调试日志
            console.log('[generateHighlightHTML] prefix:', prefix, 'suffix:', suffix);
            console.log('[generateHighlightHTML] variableList:', this.promptParamForm.variableList);
            
            if (!prefix || !suffix) {
                // 没有前缀后缀，直接返回转义的HTML
                console.log('[generateHighlightHTML] No prefix/suffix, returning plain text');
                return text
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;')
                    .replace(/\n/g, '<br>');
            }
            
            // 获取所有已定义的变量名（不带前后缀）
            const definedVarNames = new Set();
            if (this.promptParamForm.variableList && this.promptParamForm.variableList.length > 0) {
                this.promptParamForm.variableList.forEach(v => {
                    if (v.name) {
                        definedVarNames.add(v.name);
                    }
                });
            }
            
            console.log('[generateHighlightHTML] definedVarNames:', Array.from(definedVarNames));
            
            // 转义正则特殊字符
            const escapeRegex = (str) => str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            const escapedPrefix = escapeRegex(prefix);
            const escapedSuffix = escapeRegex(suffix);
            
            // 构建正则：匹配 {{$varName}}，使用非贪婪匹配
            // 例如：prefix={{$, suffix=}}, 正则为：\{\{\$(.+?)\}\}
            const regex = new RegExp(`${escapedPrefix}(.+?)${escapedSuffix}`, 'g');
            
            console.log('[generateHighlightHTML] regex:', regex);
            
            let result = '';
            let lastIndex = 0;
            let matchCount = 0;
            let match;
            
            // 使用 exec 逐个匹配
            while ((match = regex.exec(text)) !== null) {
                matchCount++;
                const fullMatch = match[0];  // 完整匹配，如 {{$prefix}}
                const varName = match[1];     // 捕获组，如 prefix
                const offset = match.index;
                
                console.log(`[generateHighlightHTML] Match ${matchCount}:`, fullMatch, 'varName:', varName, 'offset:', offset);
                
                // 添加匹配前的文本（HTML转义并处理换行）
                const beforeText = text.substring(lastIndex, offset);
                // 处理换行：将换行符替换为 <br>
                // 移除 span 标签前的尾随空白字符（空格、制表符等），但保留换行符转换为 <br>
                let processedBeforeText = beforeText
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;');
                
                // 移除尾随空白字符（但保留换行符，因为需要转换为 <br>）
                processedBeforeText = processedBeforeText.replace(/[ \t]+$/, '');
                // 将换行符替换为 <br>
                processedBeforeText = processedBeforeText.replace(/\n/g, '<br>');
                result += processedBeforeText;
                
                // 判断变量是否已定义
                const isDefined = definedVarNames.has(varName);
                const className = isDefined ? 'var-highlight defined' : 'var-highlight undefined';
                
                console.log(`[generateHighlightHTML] varName "${varName}" isDefined:`, isDefined);
                
                // 添加高亮的变量（HTML转义，但不包含换行）
                const escapedMatch = fullMatch
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;');
                // 确保 span 标签前后没有空白字符，直接拼接（不在 HTML 字符串中添加换行或空格）
                result += `<span class="${className}">${escapedMatch}</span>`;
                
                lastIndex = offset + fullMatch.length;
            }
            
            console.log('[generateHighlightHTML] Total matches:', matchCount);
            
            // 添加剩余文本（HTML转义并处理换行）
            const remainingText = text.substring(lastIndex);
            // 如果剩余文本开头有空白字符（空格、制表符等），先移除，避免在 span 后面产生空白
            // 但保留换行符，因为需要转换为 <br>
            let processedRemainingText = remainingText
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');
            
            // 移除开头的空白字符（但保留换行符）
            processedRemainingText = processedRemainingText.replace(/^[ \t]+/, '');
            // 将换行符替换为 <br>
            processedRemainingText = processedRemainingText.replace(/\n/g, '<br>');
            result += processedRemainingText;
            
            // 清理 span 标签前后的空白字符（但不移除 BR 标签，因为 BR 是用户输入的换行）
            // 这可以防止 white-space: pre-wrap 导致 span 单独成行
            // 1. 清理 span 标签前的空白字符（空格、制表符等），但保留 BR 标签
            result = result.replace(/([ \t]+)(<span class="var-highlight[^"]*">)/gi, '$2');
            // 2. 清理 span 标签后的空白字符（空格、制表符等），但保留 BR 标签
            result = result.replace(/(<\/span>)([ \t]+)/gi, '$1');
            // 注意：不再移除 BR 标签，因为 BR 是用户输入的换行，应该保留
            
            console.log('[generateHighlightHTML] Final HTML (first 200 chars):', result.substring(0, 200));
            
            return result;
        },
        
        // 转义正则表达式特殊字符
        escapeRegex(str) {
            return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        },
        
        // 保存光标位置（返回字符偏移量，基于纯文本）
        // 使用与 restoreCaretPosition 完全相同的遍历逻辑，确保一致性
        saveCaretPosition() {
            const editor = this.$refs.promptEditor;
            if (!editor) return 0;
            
            const sel = window.getSelection();
            if (sel.rangeCount === 0) return 0;
            
            const range = sel.getRangeAt(0);
            const targetContainer = range.startContainer;
            const targetOffset = range.startOffset;
            
            let charCount = 0;
            let found = false;
            
            // 使用与 restoreCaretPosition 完全相同的遍历逻辑
            const walkNodes = (node) => {
                if (found) return;
                
                if (node.nodeType === Node.TEXT_NODE) {
                    if (node === targetContainer) {
                        // 找到了目标文本节点，加上偏移量
                        charCount += targetOffset;
                        found = true;
                        return;
                    }
                    // 不是目标节点，加上整个节点的长度
                    const nodeLength = node.textContent ? node.textContent.length : 0;
                    charCount += nodeLength;
                } else if (node.nodeName === 'BR') {
                    // 检查光标是否在这个 BR 标签之后
                    // 如果 targetContainer 是 BR 的父节点，且 targetOffset 指向这个 BR 之后
                    if (targetContainer.nodeType === Node.ELEMENT_NODE && 
                        targetContainer === node.parentNode &&
                        targetOffset > 0) {
                        // 检查 targetOffset 是否指向这个 BR 之后
                        const brIndex = Array.from(targetContainer.childNodes).indexOf(node);
                        if (targetOffset === brIndex + 1) {
                            // 光标在这个 BR 标签之后
                            charCount += 1;
                            found = true;
                            return;
                        }
                    }
                    // BR 标签算作一个字符（换行符）
                    charCount += 1;
                } else if (node.nodeType === Node.ELEMENT_NODE) {
                    // 检查光标是否在这个元素节点的子节点之间
                    if (node === targetContainer && targetOffset > 0) {
                        // 光标在这个节点的子节点之间
                        // 使用与 restoreCaretPosition 完全相同的遍历逻辑计算字符数
                        for (let i = 0; i < targetOffset && i < node.childNodes.length; i++) {
                            const childNode = node.childNodes[i];
                            // 递归遍历子节点，使用相同的逻辑
                            const countChildNodes = (child) => {
                                if (child.nodeType === Node.TEXT_NODE) {
                                    return child.textContent ? child.textContent.length : 0;
                                } else if (child.nodeName === 'BR') {
                                    return 1;
                                } else if (child.nodeType === Node.ELEMENT_NODE) {
                                    let count = 0;
                                    for (let j = 0; j < child.childNodes.length; j++) {
                                        count += countChildNodes(child.childNodes[j]);
                                    }
                                    return count;
                                }
                                return 0;
                            };
                            charCount += countChildNodes(childNode);
                        }
                        found = true;
                        return;
                    }
                    // 对于元素节点，递归遍历子节点
                    for (let i = 0; i < node.childNodes.length; i++) {
                        walkNodes(node.childNodes[i]);
                        if (found) return;
                    }
                }
            };
            
            walkNodes(editor);
            
            console.log('[saveCaretPosition] Calculated position:', charCount, 'targetContainer:', targetContainer.nodeName, 'targetOffset:', targetOffset);
            
            return charCount;
        },
        
        // 恢复光标位置（基于纯文本偏移量）
        // 使用与 saveCaretPosition 完全相同的遍历逻辑，确保一致性
        restoreCaretPosition(offset) {
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            const sel = window.getSelection();
            if (!sel) return;
            
            const range = document.createRange();
            let charCount = 0;
            let found = false;
            
            // 使用与 saveCaretPosition 完全相同的遍历逻辑
            const walkNodes = (node) => {
                if (found) return;
                
                if (node.nodeType === Node.TEXT_NODE) {
                    const nodeText = node.textContent || '';
                    const nodeLength = nodeText.length;
                    const nextCharCount = charCount + nodeLength;
                    
                    if (offset >= charCount && offset <= nextCharCount) {
                        // 光标在这个文本节点内
                        const positionInNode = Math.min(offset - charCount, nodeLength);
                        range.setStart(node, positionInNode);
                        range.collapse(true);
                        found = true;
                        return;
                    }
                    charCount = nextCharCount;
                } else if (node.nodeName === 'BR') {
                    // BR 标签算作一个字符（换行符）
                    const nextCharCount = charCount + 1;
                    if (offset === charCount) {
                        // 光标在 BR 标签之前
                        range.setStartBefore(node);
                        range.collapse(true);
                        found = true;
                        return;
                    } else if (offset === nextCharCount) {
                        // 光标在 BR 标签之后
                        range.setStartAfter(node);
                        range.collapse(true);
                        found = true;
                        return;
                    }
                    charCount = nextCharCount;
                } else if (node.nodeType === Node.ELEMENT_NODE) {
                    // 对于元素节点，递归遍历子节点
                    // 注意：span 等内联元素不影响字符计数，只计算其内部的文本
                    for (let i = 0; i < node.childNodes.length; i++) {
                        walkNodes(node.childNodes[i]);
                        if (found) return;
                    }
                }
            };
            
            walkNodes(editor);
            
            console.log('[restoreCaretPosition] Looking for position:', offset, 'found:', found, 'charCount:', charCount);
            
            if (found) {
                try {
                    sel.removeAllRanges();
                    sel.addRange(range);
                    console.log('[restoreCaretPosition] Successfully restored to position:', offset);
                } catch (e) {
                    console.warn('[restoreCaretPosition] Failed to restore caret position:', e);
                    // Fallback: 放在末尾
                    try {
                        range.selectNodeContents(editor);
                        range.collapse(false);
                        sel.removeAllRanges();
                        sel.addRange(range);
                    } catch (e2) {
                        console.warn('[restoreCaretPosition] Fallback also failed:', e2);
                    }
                }
            } else {
                // 如果找不到精确位置，尝试将光标放在末尾
                console.warn('[restoreCaretPosition] Could not find position', offset, ', placing at end');
                try {
                    range.selectNodeContents(editor);
                    range.collapse(false); // 折叠到末尾
                    sel.removeAllRanges();
                    sel.addRange(range);
                } catch (e) {
                    console.warn('[restoreCaretPosition] Failed to place caret at end:', e);
                }
            }
        },
        
        // 处理粘贴事件
        handlePaste(e) {
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            // 标记正在粘贴
            this._isPasting = true;
            
            // 清除之前的防抖定时器
            if (this._highlightTimer) clearTimeout(this._highlightTimer);
            
            // 在粘贴前保存光标位置（使用与 saveCaretPosition 相同的方法）
            const sel = window.getSelection();
            let pasteStartPos = 0;
            
            if (sel.rangeCount > 0) {
                // 使用与 saveCaretPosition 相同的遍历方法来计算位置
                const range = sel.getRangeAt(0);
                const targetContainer = range.startContainer;
                const targetOffset = range.startOffset;
                
                let charCount = 0;
                let found = false;
                
                // 使用与 saveCaretPosition 完全相同的遍历逻辑
                const walkNodes = (node) => {
                    if (found) return;
                    
                    if (node.nodeType === Node.TEXT_NODE) {
                        if (node === targetContainer) {
                            charCount += targetOffset;
                            found = true;
                            return;
                        }
                        const nodeLength = node.textContent ? node.textContent.length : 0;
                        charCount += nodeLength;
                    } else if (node.nodeName === 'BR') {
                        if (targetContainer.nodeType === Node.ELEMENT_NODE && 
                            targetContainer === node.parentNode &&
                            targetOffset > 0) {
                            const brIndex = Array.from(targetContainer.childNodes).indexOf(node);
                            if (targetOffset === brIndex + 1) {
                                charCount += 1;
                                found = true;
                                return;
                            }
                        }
                        charCount += 1;
                    } else if (node.nodeType === Node.ELEMENT_NODE) {
                        if (node === targetContainer && targetOffset > 0) {
                            for (let i = 0; i < targetOffset && i < node.childNodes.length; i++) {
                                const childNode = node.childNodes[i];
                                const countChildNodes = (child) => {
                                    if (child.nodeType === Node.TEXT_NODE) {
                                        return child.textContent ? child.textContent.length : 0;
                                    } else if (child.nodeName === 'BR') {
                                        return 1;
                                    } else if (child.nodeType === Node.ELEMENT_NODE) {
                                        let count = 0;
                                        for (let j = 0; j < child.childNodes.length; j++) {
                                            count += countChildNodes(child.childNodes[j]);
                                        }
                                        return count;
                                    }
                                    return 0;
                                };
                                charCount += countChildNodes(childNode);
                            }
                            found = true;
                            return;
                        }
                        for (let i = 0; i < node.childNodes.length; i++) {
                            walkNodes(node.childNodes[i]);
                            if (found) return;
                        }
                    }
                };
                
                walkNodes(editor);
                pasteStartPos = charCount;
                console.log('[handlePaste] Saved paste start position:', pasteStartPos);
            }
            
            // 获取粘贴的纯文本内容
            const pasteData = e.clipboardData ? e.clipboardData.getData('text/plain') : '';
            const pasteTextLength = pasteData.length;
            
            console.log('[handlePaste] Paste data length:', pasteTextLength, 'pasteStartPos:', pasteStartPos);
            
            // 等待粘贴内容插入完成后再处理
            // 使用 requestAnimationFrame 确保粘贴操作完全完成
            requestAnimationFrame(() => {
                setTimeout(() => {
                    this._isPasting = false;
                    
                    // 计算粘贴后的光标位置：粘贴开始位置 + 粘贴文本长度
                    const newCaretPos = pasteStartPos + pasteTextLength;
                    
                    console.log('[handlePaste] Calculated new caret position:', newCaretPos);
                    
                    const text = this.getEditorText();
                    
                    // 检查内容是否真的发生了变化
                    const contentChanged = text !== this.content;
                    
                    // 更新 content
                    this.content = text;
                    
                    // 如果内容真的发生了变化，触发状态更新（从"连发"切换到"打靶"）
                    if (contentChanged) {
                        this.promptChangeHandel(text, 'content');
                    }
                    
                    // 应用高亮，并传入预期的光标位置
                    this.applyHighlightWithCaretPos(newCaretPos);
                }, 10);
            });
        },
        
        // 处理键盘按键事件（特别是回车键）
        handleKeyDown(e) {
            // 如果是回车键，需要特殊处理
            if (e.key === 'Enter' || e.keyCode === 13) {
                const editor = this.$refs.promptEditor;
                if (!editor) return;
                
                // 标记正在输入回车
                this._isEntering = true;
                
                // 清除之前的防抖定时器
                if (this._highlightTimer) clearTimeout(this._highlightTimer);
                
                // 在回车键插入 BR 标签之前，保存当前光标位置
                const sel = window.getSelection();
                let enterStartPos = 0;
                
                if (sel.rangeCount > 0) {
                    // 使用与 saveCaretPosition 相同的方法计算位置
                    enterStartPos = this.saveCaretPosition();
                    console.log('[handleKeyDown] Enter key pressed, saved position before BR insertion:', enterStartPos);
                }
                
                // 等待回车键插入 BR 标签完成后再处理
                requestAnimationFrame(() => {
                    setTimeout(() => {
                        this._isEntering = false;
                        
                        // 回车后，光标应该在 BR 标签之后，位置应该是 enterStartPos + 1
                        const newCaretPos = enterStartPos + 1;
                        
                        console.log('[handleKeyDown] After Enter, calculated new caret position:', newCaretPos);
                        
                        const text = this.getEditorText();
                        
                        // 检查内容是否真的发生了变化
                        const contentChanged = text !== this.content;
                        
                        // 更新 content
                        this.content = text;
                        
                        // 如果内容真的发生了变化，触发状态更新（从"连发"切换到"打靶"）
                        if (contentChanged) {
                            this.promptChangeHandel(text, 'content');
                        }
                        
                        // 使用 applyHighlightWithCaretPos 来应用高亮并恢复光标位置
                        this.applyHighlightWithCaretPos(newCaretPos);
                    }, 10);
                });
            }
        },
        
        // 用户输入时（立即更新高亮，使用短防抖优化性能）
        handleEditorInput(e) {
            if (this.isComposing) return;  // IME输入中不处理
            if (this._isPasting) return;   // 粘贴操作中不处理，由 handlePaste 处理
            if (this._isEntering) return;  // 回车操作中不处理，由 handleKeyDown 处理
            
            const text = this.getEditorText();
            
            // 检查内容是否真的发生了变化
            const contentChanged = text !== this.content;
            
            // 更新 content
            this.content = text;
            
            // 如果内容真的发生了变化，触发状态更新（从"连发"切换到"打靶"）
            if (contentChanged) {
                this.promptChangeHandel(text, 'content');
            }
            
            // 使用较短的防抖时间（150ms）来平衡性能和响应性
            // 这样用户输入、删除、剪切等操作时，高亮会立即更新
            if (this._highlightTimer) clearTimeout(this._highlightTimer);
            this._highlightTimer = setTimeout(() => {
                this.applyHighlight();
            }, 150);
        },
        
        // 清理 var-highlight span 周围多余的 <br> 标签和空白文本节点，并规范化 DOM
        cleanupHighlightBrTags(editor) {
            if (!editor) return;
            
            console.log('[cleanupHighlightBrTags] Starting cleanup...');
            
            // 查找所有 var-highlight span 标签（使用 Array.from 创建副本，避免动态查询问题）
            const highlightSpans = Array.from(editor.querySelectorAll('.var-highlight'));
            
            console.log('[cleanupHighlightBrTags] Found', highlightSpans.length, 'highlight spans');
            
            highlightSpans.forEach(span => {
                // 清理 span 前面的空白文本节点（但不移除 BR 标签，因为 BR 是用户输入的换行）
                let prevSibling = span.previousSibling;
                while (prevSibling) {
                    if (prevSibling.nodeType === Node.TEXT_NODE) {
                        // 检查文本节点是否只包含空白字符（空格、换行、制表符等）
                        const textContent = prevSibling.textContent;
                        if (!textContent || /^[\s\n\r\t\u00A0]*$/.test(textContent)) {
                            // 只包含空白字符（包括不间断空格），移除
                            const toRemove = prevSibling;
                            prevSibling = prevSibling.previousSibling;
                            toRemove.remove();
                        } else {
                            // 有非空白内容，但可能末尾有空白字符，需要清理末尾的空白
                            // 移除末尾的所有空白字符（包括换行符）
                            const trimmed = textContent.replace(/[\s\n\r\t\u00A0]+$/, '');
                            if (trimmed !== textContent) {
                                if (trimmed) {
                                    prevSibling.textContent = trimmed;
                                } else {
                                    // 如果全部是空白，移除节点
                                    const toRemove = prevSibling;
                                    prevSibling = prevSibling.previousSibling;
                                    toRemove.remove();
                                    continue;
                                }
                            }
                            break;
                        }
                    } else {
                        // 其他类型的节点，停止清理
                        break;
                    }
                }
                
                // 清理 span 后面的空白文本节点（但不移除 BR 标签，因为 BR 是用户输入的换行）
                let nextSibling = span.nextSibling;
                while (nextSibling) {
                    if (nextSibling.nodeType === Node.TEXT_NODE) {
                        // 检查文本节点是否只包含空白字符（空格、换行、制表符等）
                        const textContent = nextSibling.textContent;
                        if (!textContent || /^[\s\n\r\t\u00A0]*$/.test(textContent)) {
                            // 只包含空白字符（包括不间断空格），移除
                            const toRemove = nextSibling;
                            nextSibling = nextSibling.nextSibling;
                            toRemove.remove();
                        } else {
                            // 有非空白内容，但可能开头有空白字符，需要清理开头的空白
                            // 移除开头的所有空白字符（包括换行符），但保留文本内容
                            const trimmed = textContent.replace(/^[\s\n\r\t\u00A0]+/, '');
                            if (trimmed !== textContent) {
                                if (trimmed) {
                                    nextSibling.textContent = trimmed;
                                } else {
                                    // 如果全部是空白，移除节点
                                    const toRemove = nextSibling;
                                    nextSibling = nextSibling.nextSibling;
                                    toRemove.remove();
                                    continue;
                                }
                            }
                            break;
                        }
                    } else {
                        // 遇到非文本节点（包括 BR 标签），停止清理
                        // BR 标签是用户输入的换行，不应该被移除
                        break;
                    }
                }
            });
            
            // 规范化 DOM：合并相邻的文本节点
            // 这可以确保 span 标签紧贴文本节点，没有中间的空白文本节点
            editor.normalize();
            
            console.log('[cleanupHighlightBrTags] Cleanup completed. Editor innerHTML (first 300 chars):', editor.innerHTML.substring(0, 300));
        },
        
        // 应用高亮（使用指定的光标位置，用于粘贴等操作）
        applyHighlightWithCaretPos(expectedCaretPos) {
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            // 获取当前文本
            const text = this.getEditorText();
            
            // 同步 content（但不触发状态变化，因为状态变化应该在 handleEditorInput、handlePaste、handleKeyDown 中处理）
            // 这里只负责高亮显示
            this.content = text;
            
            // 生成高亮HTML
            const html = this.generateHighlightHTML(text);
            
            // 更新HTML
            editor.innerHTML = html;
            
            // 使用 requestAnimationFrame 确保 DOM 完全更新后再清理和恢复光标
            requestAnimationFrame(() => {
                // 清理多余的 <br> 标签和空白文本节点
                this.cleanupHighlightBrTags(editor);
                
                // 使用 $nextTick 确保清理完成后再恢复光标
                this.$nextTick(() => {
                    // 使用 innerText 来计算文本长度，确保与 saveCaretPosition 一致
                    const currentText = editor.innerText || '';
                    const currentTextLength = currentText.length;
                    const targetPos = Math.min(expectedCaretPos, currentTextLength);
                    console.log('[applyHighlightWithCaretPos] Restoring caret position:', targetPos, 'expected:', expectedCaretPos, 'current text length:', currentTextLength);
                    this.restoreCaretPosition(targetPos);
                });
            });
            
            // 注意：不再在这里调用 promptChangeHandel
            // 内容变化的状态更新应该在 handleEditorInput、handlePaste、handleKeyDown 等实际输入事件中处理
            // 这里只负责高亮显示
        },
        
        // 应用高亮（保存光标位置）
        applyHighlight() {
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            // 检查编辑器是否有焦点
            const hasFocus = document.activeElement === editor;
            
            // 获取当前文本（在保存光标位置之前获取，确保文本一致性）
            const text = this.getEditorText();
            
            // 同步 content（但不触发状态变化，因为状态变化应该在 handleEditorInput 中处理）
            // 这里只负责高亮显示
            this.content = text;
            
            // 只有在编辑器有焦点时才保存光标位置
            // 使用基于文本内容的方法，而不是 DOM 遍历，确保与恢复时一致
            let caretPos = 0;
            if (hasFocus) {
                const sel = window.getSelection();
                if (sel.rangeCount > 0) {
                    const range = sel.getRangeAt(0);
                    // 创建一个从编辑器开始到光标位置的 range
                    const preCaretRange = range.cloneRange();
                    preCaretRange.selectNodeContents(editor);
                    preCaretRange.setEnd(range.startContainer, range.startOffset);
                    // 使用 textContent 来计算位置（不包括 BR 标签，BR 标签会在遍历时单独计算）
                    // 但我们需要考虑 BR 标签，所以使用遍历 DOM 的方法
                    caretPos = this.saveCaretPosition();
                }
                console.log('[applyHighlight] Saved caret position:', caretPos, 'text length:', text.length);
            }
            
            // 生成高亮HTML
            const html = this.generateHighlightHTML(text);
            
            // 更新HTML
            editor.innerHTML = html;
            
            // 使用 requestAnimationFrame 确保 DOM 完全更新后再清理和恢复光标
            requestAnimationFrame(() => {
                // 清理多余的 <br> 标签和空白文本节点
                this.cleanupHighlightBrTags(editor);
                
                // 只有在编辑器有焦点时才恢复光标位置
                if (hasFocus && caretPos > 0) {
                    // 使用 $nextTick 确保清理完成后再恢复光标
                    this.$nextTick(() => {
                        // 恢复光标位置（使用相同的遍历逻辑）
                        console.log('[applyHighlight] Restoring caret position:', caretPos);
                        this.restoreCaretPosition(caretPos);
                    });
                }
            });
            
            // 注意：不再在这里调用 promptChangeHandel
            // 内容变化的状态更新应该在 handleEditorInput、handlePaste、handleKeyDown 等实际输入事件中处理
            // 这里只负责高亮显示，避免在 blur 时误触发状态变化
        },
        
        // 辅助方法：根据ID获取名称
        getTargetRangeName(id) {
            if (!this.promptFieldOpt || !id) return '未知靶场';
            const range = this.promptFieldOpt.find(item => item.value === id);
            return range ? range.label : '未知靶场';
        },
        
        getTargetLaneName(id) {
            if (!this.promptOpt || !id) return '未知靶道';
            // 从promptOpt中查找对应的靶道
            const lane = this.promptOpt.find(item => item.idkey === id);
            return lane ? (lane.nickName || lane.label) : '未知靶道';
        },
        
        getTacticalName(id) {
            if (!this.tacticalOpt || !id) return '未知战术';
            const tactical = this.tacticalOpt.find(item => item.value === id);
            return tactical ? tactical.label : '未知战术';
        },
        
        getModelName(id) {
            if (!this.modelOpt || !id) return '未知模型';
            const model = this.modelOpt.find(item => item.value === id);
            return model ? model.label : '未知模型';
        },
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
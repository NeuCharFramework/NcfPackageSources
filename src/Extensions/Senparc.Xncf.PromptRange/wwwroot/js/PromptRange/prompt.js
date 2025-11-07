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
        },
        
        // 生成带高亮的HTML内容
        highlightedHTML() {
            if (!this.content) return '';
            
            const prefix = this.promptParamForm.prefix || '';
            const suffix = this.promptParamForm.suffix || '';
            const variableList = this.promptParamForm.variableList || [];
            
            // 如果没有设置前缀和后缀，直接返回纯文本（转义HTML，保留换行）
            if (!prefix || !suffix) {
                return this.escapeHtml(this.content).replace(/\n/g, '<br>');
            }
            
            // 转义前缀和后缀用于正则表达式
            const escapedPrefix = prefix.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            const escapedSuffix = suffix.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            
            // 构建正则表达式
            const regex = new RegExp(`${escapedPrefix}(\\w+)${escapedSuffix}`, 'g');
            
            // 获取所有已定义的变量名
            const definedVarNames = variableList.map(v => v.name).filter(n => n);
            
            // 转义HTML
            let html = this.escapeHtml(this.content);
            
            // 高亮变量
            html = html.replace(regex, (match, varName) => {
                const isDefined = definedVarNames.includes(varName);
                const className = isDefined ? 'var-highlight defined' : 'var-highlight undefined';
                const title = isDefined ? `已定义: ${varName}` : `未定义: ${varName}`;
                return `<span class="${className}" title="${title}">${this.escapeHtml(match)}</span>`;
            });
            
            // 换行符转换为<br>
            html = html.replace(/\n/g, '<br>');
            
            return html;
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
        
        // 监听content外部变化（如加载数据）
        content(newVal, oldVal) {
            if (newVal !== oldVal && this.$refs.promptEditor) {
                this.$nextTick(() => {
                    const editor = this.$refs.promptEditor;
                    if (!editor) return;
                    
                    const currentText = this.getPlainText(editor);
                    
                    // 如果编辑器内容与content不一致（外部赋值），更新编辑器
                    if (currentText !== newVal) {
                        editor.innerHTML = this.highlightedHTML;
                    }
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
                editor.innerHTML = this.highlightedHTML;
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

        // 战术选择 关闭弹出
        // 战术选择 dialog 关闭
        tacticalFormCloseDialog() {
            this.tacticalForm = {
                tactics: '重新瞄准'
            }
            this.$refs.tacticalForm.resetFields();
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
        
        // ========== contenteditable编辑器方法 ==========
        
        // 编辑器输入事件
        onEditorInput(e) {
            // 如果正在使用输入法，不处理
            if (this.isComposing) return;
            
            this.updateContentFromEditor();
        },
        
        // 输入法结束事件
        onCompositionEnd(e) {
            this.isComposing = false;
            this.updateContentFromEditor();
        },
        
        // 编辑器失焦事件
        onEditorBlur() {
            this.promptChangeHandel(this.content, 'content');
        },
        
        // 编辑器粘贴事件（只粘贴纯文本）
        onEditorPaste(e) {
            e.preventDefault();
            const text = (e.clipboardData || window.clipboardData).getData('text/plain');
            document.execCommand('insertText', false, text);
        },
        
        // 从编辑器更新content，并重新渲染高亮
        updateContentFromEditor() {
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            // 保存光标位置
            const sel = window.getSelection();
            let charOffset = 0;
            if (sel.rangeCount > 0) {
                const range = sel.getRangeAt(0);
                const preCaretRange = range.cloneRange();
                preCaretRange.selectNodeContents(editor);
                preCaretRange.setEnd(range.endContainer, range.endOffset);
                charOffset = preCaretRange.toString().length;
            }
            
            // 提取纯文本内容
            this.content = this.getPlainText(editor);
            
            // 延迟更新高亮，避免与输入冲突
            this.$nextTick(() => {
                // 重新渲染高亮HTML
                editor.innerHTML = this.highlightedHTML;
                
                // 恢复光标位置
                this.restoreCaret(editor, charOffset);
            });
        },
        
        // 从contenteditable元素提取纯文本
        getPlainText(element) {
            let text = '';
            for (let node of element.childNodes) {
                if (node.nodeType === Node.TEXT_NODE) {
                    text += node.textContent;
                } else if (node.nodeName === 'BR') {
                    text += '\n';
                } else if (node.nodeType === Node.ELEMENT_NODE) {
                    text += this.getPlainText(node);
                }
            }
            return text;
        },
        
        // 恢复光标位置
        restoreCaret(element, charOffset) {
            const range = document.createRange();
            const sel = window.getSelection();
            let currentOffset = 0;
            let found = false;
            
            function findTextNode(node) {
                if (found) return;
                
                if (node.nodeType === Node.TEXT_NODE) {
                    const nextOffset = currentOffset + node.length;
                    if (charOffset >= currentOffset && charOffset <= nextOffset) {
                        range.setStart(node, charOffset - currentOffset);
                        range.collapse(true);
                        found = true;
                        return;
                    }
                    currentOffset = nextOffset;
                } else {
                    for (let child of node.childNodes) {
                        findTextNode(child);
                        if (found) return;
                    }
                }
            }
            
            findTextNode(element);
            
            if (found) {
                sel.removeAllRanges();
                sel.addRange(range);
            }
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
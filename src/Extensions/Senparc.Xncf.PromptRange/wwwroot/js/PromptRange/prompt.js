var app = new Vue({
    el: "#app",
    data() {
        return {
            isAIGrade: true,
            devHost: 'http://pr-felixj.frp.senparc.com',
            // Optimization function
            optimizeDialogVisible: false,
            optimizeRequirement: '',
            optimizing: false,
            optimizeProgressText: '',          // Optimize the progress description in the pop-up window (Agent → Refresh → Target Practice → Rating)
            optimizeErrorText: '',             // When optimization fails, it will remain in the pop-up window for developers to view.
            autoShootAfterOptimize: true,      // 🆕 Target shooting immediately after creation (selected by default)
            autoAIGradeAfterShoot: false,      // 🆕 AI scoring after target shooting (not selected by default)
            // PromptCatalyzer initialization function
            promptCatalyzerInitVisible: false,      // Initialize dialog visibility
            availableModelsForInit: [],             // List of available AI Models
            selectedModelIdForInit: null,           // Selected Model ID
            loadingModels: false,                   // The status of loading the Model list
            initializing: false,                    // Initializing state
            pageChange: false, // Is there any change to the page?
            isAvg: true, // Whether to score equally, default false, not evenly
            // Configuration input ---start
            promptField: '', // Range list
            promptFieldOpt: [], // Range list
            promptOpt: [], // prompt list
            modelOpt: [], // Model list
            waitRefreshModel: false, // Whether to wait to refresh the model list
            promptid: '',// Choose a shooting range
            modelid: '',// Select model
            content: '',// prompt input content
            remarks: '', // prompt input remarks
            isComposing: false, // Whether input method (IME composition) is being used
            numsOfResults: 1, // Number of bursts of prompt (number of launches) 1-10
            numsOfResultsOpt: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], // Number of bursts of prompt (number of launches) 1-10
            // Parameter settings View configuration list
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
            promptLeftShow: false, // The entire area on the left side of the prompt is shown and hidden.
            parameterViewShow: false, // Model parameter settings show and hide false is displayed by default trun is hidden
            targetShootLoading: false, // Target shooting loading
            dodgersLoading: false, // burst loading
            // Configure input ---end
            // prompt request parameters ---start
            promptParamVisible: false,// Prompt request parameters show/hide true means display false means hide by default
            promptParamFormLoading: false,
            promptParamForm: {
                prefix: '',
                suffix: '',
                variableList: []
            },
            // prompt request parameters ---end
            // sendBtns: target practice, burst, save draft
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
            // Output ---start
            outputAverageDeci: -1,// Average score of output list
            outputMaxDeci: -1, // Output the highest score of the list
            outputActive: '', // Select the output list to view|score
            outputList: [],  // Output list
            robotScoreLoadingMap: {}, // AI rating loading status mapping {itemId: true/false}
            chartData: [], // chart data
            chartInstance: null, // Chart example
            // Output ---end
            // Version record ---start
            versionDrawer: false,// drawer
            versionSearchVal: '', // Version search
            versionTreeProps: {
                children: 'children',
                label: 'label'
            },
            versionTreeData: [],
            // Version record ---end
            // Tactical options
            tacticalFormVisible: false,
            tacticalFormSubmitLoading: false,
            tacticalForm: {
                tactics: '重新瞄准',
                chatMode: '对话模式' // Conversation mode/direct testing, default conversation mode
            },
            // Dialogue input in tactical selection pop-up window
            tacticalChatInput: '', // User input in conversation mode
            // Continue chatting related status
            continueChatMode: false, // Whether in continue chat mode
            continueChatPromptResultId: null, // PromptResult ID to continue chatting with
            continueChatHistory: [], // Continue chat history
            continueChatSystemMessage: '', // SystemMessage(Prompt) used when continuing chat
            systemMessageCollapse: [], // SystemMessage accordion status
            // Map related status
            mapDialogVisible: false, // Map dialog display status
            map3dScene: null, // three.js scene
            map3dCamera: null, // three.js camera
            map3dRenderer: null, // three.js renderer
            map3dControls: null, // camera controller
            map3dNodes: [], // 3D node array
            map3dTreeData: null, // tree structure data
            map3dClickHandler: null, // Click event handler
            map3dAnimationId: null, // Animation ID
            map3dNeedsAnimationUpdate: false, // Do you need to update the animation?
            map3dNodeMap: new Map(), // Node mapping for quick lookups
            map3dLastAnimationTime: 0, // Last animation update time (for throttling)
            map3dCurrentNodes: [], // Cache the currently selected node (performance optimization)
            // range
            fieldFormVisible: false,
            fieldFormSubmitLoading: false,
            fieldForm: {
                alias: ''
            },
            // ai scoring criteria
            aiScoreFormVisible: false,
            aiScoreFormSubmitLoading: false,
            aiScoreForm: {
                resultList: [{
                    id: 1,
                    label: '预期结果',
                    value: ''
                }]
            },

            // Model
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
            // Form validation rules
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
            uploadPluginVisible: false, // Plugin upload dialog to show and hide
            uploadPluginDropAreaVisible: true,// Upload area visible and hidden
            uploadPluginDropHover: false,// Drag and drop files Hover
            uploadPluginData: [], // File list of Plugin folder
            jsZip: null, // Compressed instance
            expectedPluginVisible: false, // Plugin exports dialog to show or hide
            // Export Plugin selection data
            expectedPluginFoem: {
                checkList: [], // Selected data tree 
            },
            expectedPluginFieldList: [],
            exportPluginExpandAll: false, // Whether to expand all nodes
            exportPluginSelectedCount: 0, // Number of target lanes selected
            defaultProps: {
                children: 'children',
                label: 'label'
            },
            contentTextareaRows: 14, //prompt input box number of lines
            dialogVisible: false,
            targetlaneName: '',
            dailogpromptOptlist: [],
            box1Hidden: false,
            box2Hidden: false,
            box3Hidden: false,
            lastClickedBox: null,
            centerAreaMaximized: false, // Whether the middle area is maximized
            rightAreaMaximized: false,  // Whether the right area is maximized
            isBoxVisible: true, // Control the display and hidden state of the box
            foldsidebarShow: false,
            // Area width control
            leftAreaWidth: 360,    // Left area width (default 360px)
            centerAreaWidth: 380,  // Middle area width (default 380px)
            isResizing: false,     // Is dragging
            resizeType: null,      // Drag type: 'left' or 'right'
            resizeStartX: 0,       // The X coordinate where the drag starts
            resizeStartLeftWidth: 0,   // The width of the left area when dragging starts
            resizeStartCenterWidth: 0, // The width of the middle area when dragging starts
            // Prompt comparison function
            compareDialogVisible: false,  // Compare dialog display status
            comparePromptAId: null,       // The ID of Prompt A compared
            comparePromptBId: null,       // Compare the ID of Prompt B
            comparePromptA: null,         // Comparative complete data of Prompt A
            comparePromptB: null,         // Complete data of Prompt B compared
            // Custom scroll bar thumbnail
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
        
        // Get a list of selectable prompts (used in comparison dialogs)
        availablePrompts() {
            return this.promptOpt || [];
        },
        
        // Get the display information of Prompt A
        comparePromptAInfo() {
            if (!this.comparePromptA) return null;
            
            // Parse name from fullVersion (format: range-range-tactical)
            const versionParts = this.comparePromptA.fullVersion ? this.comparePromptA.fullVersion.split('-') : [];
            
            return {
                targetRangeName: versionParts[0] || '未知靶场',
                targetLaneName: versionParts[1] || '未知靶道',
                tacticalName: versionParts[2] || '未知战术',
                modelName: this.getModelName(this.comparePromptA.modelId)
            };
        },
        
        // Get the display information of Prompt B
        comparePromptBInfo() {
            if (!this.comparePromptB) return null;
            
            // Parse name from fullVersion (format: range-range-tactical)
            const versionParts = this.comparePromptB.fullVersion ? this.comparePromptB.fullVersion.split('-') : [];
            
            return {
                targetRangeName: versionParts[0] || '未知靶场',
                targetLaneName: versionParts[1] || '未知靶道',
                tacticalName: versionParts[2] || '未知战术',
                modelName: this.getModelName(this.comparePromptB.modelId)
            };
        },
        
        // Check if it is the same prompt
        isSamePrompt() {
            if (!this.comparePromptA || !this.comparePromptB) return false;
            if (!this.comparePromptAId || !this.comparePromptBId) return false;
            
            // Determine whether it is the same prompt by ID
            return this.comparePromptAId === this.comparePromptBId;
        },
        
        // Get the current tactical number (for display)
        currentTacticalInfo() {
            if (!this.promptDetail || !this.promptDetail.fullVersion) {
                return {
                    tactic: '--',
                    fullTactical: '--'
                };
            }
            
            // Parse the current version number: the format is RangeName-T{Tactic}-A{Aiming}
            // For example: 2023.12.14.1-T1.2.3-A1
            const versionParts = this.promptDetail.fullVersion.split('-');
            if (versionParts.length < 2) {
                return {
                    tactic: '--',
                    fullTactical: '--'
                };
            }
            
            // versionParts[0] = RangeName (for example: 2023.12.14.1)
            // versionParts[1] = T{Tactic} (for example: T1.2.3)
            // versionParts[2] = A{Aiming} (for example: A1, optional)
            const tacticPart = versionParts[1] || ''; // T1.2.3
            const aimingPart = versionParts[2] || ''; // A1
            
            return {
                tactic: tacticPart || '--',
                fullTactical: aimingPart ? `${tacticPart}-${aimingPart}` : tacticPart || '--'
            };
        },
        
        // Calculate the next tactic number (dynamic prompt for tactic selection pop-up window)
        nextTacticalNumbers() {
            if (!this.promptDetail || !this.promptDetail.fullVersion) {
                return {
                    topTactic: 'T1',
                    parallelTactic: 'T1.2.4',
                    subTactic: 'T1.2.3.1',
                    newAiming: 'T1.2.3-A2'
                };
            }
            
            // Parse the current version number: the format is RangeName-T{Tactic}-A{Aiming}
            // For example: 2023.12.14.1-T1.2.3-A1
            const versionParts = this.promptDetail.fullVersion.split('-');
            if (versionParts.length < 2) {
                return {
                    topTactic: 'T1',
                    parallelTactic: 'T1.2.4',
                    subTactic: 'T1.2.3.1',
                    newAiming: 'T1.2.3-A2'
                };
            }
            
            // versionParts[0] = RangeName
            // versionParts[1] = T{Tactic} (for example: T1.2.3)
            // versionParts[2] = A{Aiming} (for example: A1, optional)
            let tacticPart = versionParts[1] || ''; // T1.2.3
            let aimingPart = versionParts[2] || 'A1'; // A1
            
            // Check if the tactics section starts with T, if not it may be a formatting error
            if (!tacticPart.startsWith('T')) {
                // If the format of the version number is incorrect, the default value will be returned.
                return {
                    topTactic: 'T1',
                    parallelTactic: 'T1.2.4',
                    subTactic: 'T1.2.3.1',
                    newAiming: 'T1.2.3-A2'
                };
            }
            
            // Extract aiming number: A1 -> 1
            const currentAiming = aimingPart.replace(/^A/, '');
            const aimingNumber = parseInt(currentAiming) || 1;
            
            // Analyze tactical number: T1.2.3 -> [1, 2, 3]
            // Remove the leading T, then split by . and convert to numbers
            const tacticStr = tacticPart.replace(/^T/, ''); // 1.2.3
            if (!tacticStr || tacticStr.length === 0) {
                // If the tactical number cannot be parsed, return the default value
                return {
                    topTactic: 'T1',
                    parallelTactic: 'T1.2.4',
                    subTactic: 'T1.2.3.1',
                    newAiming: 'T1.2.3-A2'
                };
            }
            
            // Parse tactical number array
            const tacticNumbers = [];
            const parts = tacticStr.split('.');
            for (let i = 0; i < parts.length; i++) {
                const num = parseInt(parts[i]);
                if (isNaN(num)) {
                    // If any part cannot be parsed as a number, return the default value
                    return {
                        topTactic: 'T1',
                        parallelTactic: 'T1.2.4',
                        subTactic: 'T1.2.3.1',
                        newAiming: 'T1.2.3-A2'
                    };
                }
                tacticNumbers.push(num);
            }
            
            if (tacticNumbers.length === 0) {
                // If parsing fails, return the default value
                return {
                    topTactic: 'T1',
                    parallelTactic: 'T1.2.4',
                    subTactic: 'T1.2.3.1',
                    newAiming: 'T1.2.3-A2'
                };
            }
            
            // 1. Create top-level tactics: current top-level number +1
            // For example: currently it is T1.2.3, the next top tactic is T2
            // Note: This is just a prediction, the actual number needs to be determined by back-end query database
            const currentTopTactic = tacticNumbers[0] || 1;
            const nextTopTactic = currentTopTactic + 1;
            
            // 2. Create parallel tactics: under the same parent level, the last number +1
            // For example: T1.2.3 -> T1.2.4
            const nextParallelTactic = [...tacticNumbers];
            if (nextParallelTactic.length > 0) {
                nextParallelTactic[nextParallelTactic.length - 1] = nextParallelTactic[nextParallelTactic.length - 1] + 1;
            }
            
            // 3. Create a sub-tactic: add .1 under the current tactic
            // For example: T1.2.3 -> T1.2.3.1
            const nextSubTactic = [...tacticNumbers, 1];
            
            // 4. Re-aim: Current aiming number +1
            // For example: T1.2.3-A1 -> T1.2.3-A2
            const nextAiming = aimingNumber + 1;
            
            return {
                topTactic: `T${nextTopTactic}`,
                parallelTactic: `T${nextParallelTactic.join('.')}`,
                subTactic: `T${nextSubTactic.join('.')}`,
                newAiming: `${tacticPart}-A${nextAiming}`
            };
        },
        
        // Detect variables in Prompt
        detectedVariables() {
            if (!this.content) return [];
            
            const prefix = this.promptParamForm.prefix || '';
            const suffix = this.promptParamForm.suffix || '';
            const variableList = this.promptParamForm.variableList || [];
            
            // If no prefix or suffix is ​​set, an empty array is returned.
            if (!prefix || !suffix) return [];
            
            // Escape prefixes and suffixes for regular expressions
            const escapedPrefix = prefix.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            const escapedSuffix = suffix.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            
            // Build a regular expression: match prefix + variable name + suffix
            const regex = new RegExp(`${escapedPrefix}(\\w+)${escapedSuffix}`, 'g');
            
            // Get all defined variable names
            const definedVarNames = variableList.map(v => v.name).filter(n => n);
            
            // Find all matching variables
            const variables = [];
            const seen = new Set();
            let match;
            
            while ((match = regex.exec(this.content)) !== null) {
                const fullMatch = match[0];
                const varName = match[1];
                
                // avoid duplication
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
        
        // Monitor external content changes (such as updating the editor when loading data)
        content(newVal, oldVal) {
            const editor = this.$refs.promptEditor;
            if (!editor || this.isComposing) return;
            
            const currentText = editor.innerText || '';
            // If the editor content is inconsistent with content (external assignment), update the editor
            if (currentText !== newVal && newVal !== oldVal) {
                this.$nextTick(() => {
                    const html = this.generateHighlightHTML(newVal);
                    editor.innerHTML = html;
                });
            }
        }
    },
    created() {
        // Close the browser | Refresh the browser | Close the page | Open a new page Prompt for data changes to save data
        // Add beforeunload event listener
        window.addEventListener('beforeunload', this.beforeunloadHandler);
        
        // Load saved width settings on page creation
        this.loadAreaWidthsFromStorage();
    },
    mounted() {
        // Get target lane list
        setTimeout(() => {
            this.getFieldList()
            // Get model list
            this.getModelOptData()
          
        }, 100)
        // Get score trend graph
        // this.getScoringTrendData()
        // Chart adaptive
        const self = this;
        const viewElem = document.body;
        const resizeObserver = new ResizeObserver(() => {
            // Add an if constraint and execute resize() when Echarts exists. Otherwise, an error will be reported when the chart does not exist.
            if (self.chartInstance) {
                self.chartInstance.resize();
            }
        });
        resizeObserver.observe(viewElem);
        setTimeout(() => {
          this.getTargetRangeIdFromUrl();
      }, 200)
      
        // Initialize contenteditable editor
        this.$nextTick(() => {
            const editor = this.$refs.promptEditor;
            if (editor && this.content) {
                const html = this.generateHighlightHTML(this.content);
                editor.innerHTML = html;
            }
        });
        
        // Initialize code block copy function
        this.initCodeCopyButtons();
      
    },
    beforeDestroy() {
        // Remove event listeners before destroying
        window.removeEventListener('beforeunload', this.beforeunloadHandler);
        
        // Remove drag-related event listeners before component destruction
        document.removeEventListener('mousemove', this.handleResize);
        document.removeEventListener('mouseup', this.stopResize);
    },
    methods: {
        //Get the path id page data echo
        getTargetRangeIdFromUrl() {
             // Add security check to prevent $route from being undefined
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
               
                    // Get target lane list
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

                            // Get details
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
                    // Get score trend chart data
                     this.getScoringTrendData()
                

            }
        },
        //Sidebar collapse operation
        foldsidebar() {
            this.isBoxVisible = !this.isBoxVisible;
            if (this.foldsidebarShow) {
             
                this.foldsidebarShow = false

            } else {
                this.foldsidebarShow = true
               
            }
        },
      
        //Enlarge input area
       
        Amplification(boxClicked) {
            if (this.lastClickedBox === boxClicked) {
                // Click the same area again to restore all areas
                this.box1Hidden = false;
                this.box2Hidden = false;
                this.box3Hidden = false;
                this.centerAreaMaximized = false;
                this.rightAreaMaximized = false;
                this.lastClickedBox = null;
                this.getScoringTrendData();
            } else {
                // Click on different areas to maximize
                if (boxClicked === 'box1') {
                    // Maximize the right output area: hide the middle area, hide the analysis chart, expand the right area
                    this.box1Hidden = false;
                    this.box2Hidden = true;  // Hide the middle prompt area
                    this.box3Hidden = true;  // Hide analysis chart
                    this.centerAreaMaximized = false;
                    this.rightAreaMaximized = true;
                } else if (boxClicked === 'box2') {
                    // Maximize the middle prompt area: hide all content on the right and expand the middle area
                    this.box1Hidden = true;  // Hide the right output area
                    this.box2Hidden = false;
                    this.box3Hidden = true;  // Hide analysis chart
                    this.centerAreaMaximized = true;
                    this.rightAreaMaximized = false;
                } else if (boxClicked === 'box3') {
                    // Keep original logic
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
        // range delete
        fieldDeleteHandel(e, id) {
            // Prevent events from bubbling up
            e.stopPropagation();
            // Pop up prompt box
            this.$confirm('此操作将永久删除该靶场下的所有内容', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(async () => {
                let res = await servicePR.delete(`/api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.DeleteAsync?rangeId=${id}`)
                if (res.data.success) {
                    this.$message.success('删除成功')
                    let _isReset = id == this.promptField
                    // Retrieve range list
                    this.getFieldList().then(() => {
                        if (_isReset) {
                            this.promptField = ''
                            // Reset page data
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
        // Delete target lane
        promptDeleteHandel(e, id) {
            // Prevent events from bubbling up
            e.stopPropagation();
            // Pop up prompt box
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
        // Note loses focus Save
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
            // Prevent events from bubbling up
          //  e.stopPropagation();
            //A prompt box will pop up, enter the new shooting range name, confirm and submit. After cancelling, it will prompt that the operation has been cancelled.
          //  this.$prompt('Please enter the new target name', 'Prompt', {
           //     confirmButtonText: 'OK',
           //     cancelButtonText: 'Cancel',
            //    inputErrorMessage: 'Target channel name cannot be empty',
           // }).then(async ({ value }) => {
           //     this.btnEditHandle({ id: item.id, nickName: value })
           // }).catch(() => {
               // this.$message({
              //      type: 'info',
             //       message: 'Operation canceled'
           //     });
         //   });
        // },
        //Reset target lane name
        promptNameRest(e,id) {
            // Prevent events from bubbling up
            e.stopPropagation();
            // Pop up prompt box
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
        // Target lane name popup
        promptNameField(e, item) {
            e.stopPropagation();
            this.targetlaneNameid = item.id;
     
            this.dialogVisible = true
        },
        handleSelect(item) {
            console.log(item);
        },
        // Modify target lane name pop-up window to confirm the operation
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
                // If "name:" and "|version number:" are found, extract the text between them
                this.targetlaneName = this.targetlaneName.substring(startIndex + prefix.length, endIndex);
                console.log(this.targetlaneName);
                this.btnEditHandle({ id: this.targetlaneNameid, nickName: this.targetlaneName })
          
                this.dialogVisible = false
                this.targetlaneName = ''
            } else {
                // If "name:" or "|version number:" is not found, then perform fallback logic
                this.btnEditHandle({ id: this.targetlaneNameid, nickName: this.targetlaneName })
               
                this.dialogVisible = false
                this.targetlaneName = ''
                console.log(this.targetlaneName);
            }
           
        },
        //Get the target lane pop-up window input list and filter the target lanes without names.
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
            // Call callback to return the data of the suggestion list
            cb(results);
        },
        createFilter(queryString) {
            return (restaurant) => {
                console.log('Filtering restaurant:', restaurant.label, 'with query:', queryString);
                const label = restaurant.label || '';
                return label.toLowerCase().includes(queryString.toLowerCase());
            };
        },
        // Tactics selection dialog submission
        async tacticalFormSubmitBtn() {
            // If you continue in chat mode, handle it directly without verifying the tactical fields.
            if (this.continueChatMode && this.continueChatPromptResultId) {
                // Check if there is any input
                if (!this.tacticalChatInput || !this.tacticalChatInput.trim()) {
                    this.$message({
                        message: '请输入对话内容',
                        type: 'warning',
                        duration: 3000
                    })
                    return
                }
                
                // Call continue chat API
                this.continueChatSubmit(this.continueChatPromptResultId, this.tacticalChatInput.trim())
                return
            }
            
            // Normal mode, need to verify the form
            // Note: When shooting for the first time (without promptid), there is no need to verify the "tactics" field, because the T1-A1 target lane will be automatically created
            // First check whether the "Target Test" field is selected
            if (!this.tacticalForm.chatMode) {
                this.$message({
                    message: '请选择打靶测试模式',
                    type: 'warning',
                    duration: 3000
                })
                return
            }
            
            // If there is promptid, the "tactics" field needs to be verified
            if (this.promptid && !this.tacticalForm.tactics) {
                this.$message({
                    message: '请选择战术',
                    type: 'warning',
                    duration: 3000
                })
                return
            }
            
            // If you choose conversation mode, you need to check whether there is input content
            if (this.tacticalForm.chatMode === '对话模式') {
                // Check if there is any input
                if (!this.tacticalChatInput || !this.tacticalChatInput.trim()) {
                    this.$message({
                        message: '请输入对话内容',
                        type: 'warning',
                        duration: 3000
                    })
                    return
                }
                
                // Perform target practice, passing the input as userMessage
                await this.executeTargetShootWithChatMessage(this.tacticalChatInput.trim())
                // Clear conversation input
                this.tacticalChatInput = ''
                return
            }
            
            // Direct test mode and continue the original process
            await this.executeTargetShoot();
        },
        
        // Continue Chat Submit
        async continueChatSubmit(promptResultId, userMessage) {
            this.tacticalFormSubmitLoading = true
            try {
                const res = await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.ContinueChat`, {
                    promptResultId: promptResultId,
                    userMessage: userMessage || ''
                })
                
                if (res.data.success) {
                    const newChatMessages = res.data.data || []
                    
                    // Verify that new messages have valid IDs
                    const invalidMessages = newChatMessages.filter(msg => {
                        const msgId = typeof msg.id === 'string' ? parseInt(msg.id, 10) : msg.id
                        return !msgId || msgId === 0 || isNaN(msgId)
                    })
                    
                    if (invalidMessages.length > 0) {
                        console.error('错误：部分消息没有有效的 ID，重新加载历史记录以确保获取正确的 ID', invalidMessages)
                        // If the message does not have an ID, reload the history to ensure you get the correct ID
                        const reloadRes = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetChatHistory?promptResultId=${promptResultId}`)
                        if (reloadRes.data.success) {
                            this.continueChatHistory = reloadRes.data.data || []
                            console.log('重新加载后的历史记录:', this.continueChatHistory)
                            
                            // Verify that the reloaded records all have valid IDs
                            const stillInvalid = this.continueChatHistory.filter(msg => {
                                const msgId = typeof msg.id === 'string' ? parseInt(msg.id, 10) : msg.id
                                return !msgId || msgId === 0 || isNaN(msgId)
                            })
                            if (stillInvalid.length > 0) {
                                console.error('严重错误：重新加载后仍有消息 ID 无效:', stillInvalid)
                            }
                        }
                    } else {
                        // Append new conversations to history
                        console.log('追加新消息到历史记录:', newChatMessages.map(m => ({ id: m.id, roleType: m.roleType, sequence: m.sequence })))
                        this.continueChatHistory.push(...newChatMessages)
                        console.log('更新后的历史记录（最后5条）:', this.continueChatHistory.slice(-5).map(m => ({ id: m.id, idType: typeof m.id, roleType: m.roleType, sequence: m.sequence })))
                    }
                    
                    // Find the corresponding output item and update it
                    const resultIndex = this.outputList.findIndex(item => item.id === promptResultId)
                    if (resultIndex !== -1) {
                        const resultItem = this.outputList[resultIndex]
                        
                        // Update display: Append latest AI reply to ResultString
                        const latestAssistantMessage = this.continueChatHistory.find(msg => msg.roleType === 2 && msg.sequence === Math.max(...this.continueChatHistory.map(m => m.sequence)))
                        if (latestAssistantMessage) {
                            // Append to existing ResultString (formatted as conversational)
                            const currentResult = resultItem.resultString || ''
                            const separator = currentResult ? '\n\n---\n\n' : ''
                            const newContent = `**用户**: ${userMessage}\n\n**助手**: ${latestAssistantMessage.content}`
                            resultItem.resultString = currentResult + separator + newContent
                            resultItem.resultStringHtml = marked.parse(resultItem.resultString)
                        }
                    }
                    
                    // Refresh conversation history display
                    this.$forceUpdate()
                    
                    // Add code block copy button
                    this.$nextTick(() => {
                        this.addCopyButtonsToCodeBlocks();
                    });
                    
                    // Scroll to bottom for latest news
                    this.$nextTick(() => {
                        const container = document.getElementById('chatHistoryContainer')
                        if (container) {
                            container.scrollTop = container.scrollHeight
                        }
                    })
                    
                    // Clear the input box but keep the popup open to continue the conversation
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
        * Target shooting event
        * isDraft whether to save the draft
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
            // Pop-up window logic 1, if there is a promptid and the draft is not saved, a pop-up window will appear.
            if (this.promptid && !isDraft) {
                this.tacticalFormVisible = true
                return
            }
            // Pop-up window logic 2, if there is a promptid and the draft is not saved, a pop-up window will appear.
            let _isPromptDraft = false
            let _findPrompt = this.promptOpt.find(item => item.value == this.promptid)
            if (isDraft && _findPrompt) {
                _isPromptDraft = _findPrompt.isDraft
            }

            if (isDraft && !_isPromptDraft && this.promptOpt.length !== 0) {
                this.tacticalFormVisible = true
                return
            }
            
            // Pop-up window logic 3: A pop-up window should also pop up when shooting for the first time (without promptid), allowing the user to choose conversation mode or direct test mode
            // Note: Do not pass promptid when shooting for the first time, let the backend create a new target lane (T1-A1)
            if (!this.promptid && !isDraft) {
                this.tacticalFormVisible = true
                return
            }


            this.targetShootLoading = true
            let _postData = {
                //promptid: this.promptid,//Select the shooting range
                modelid: this.modelid,// Select model
                content: this.content,// prompt input content
                note: this.remarks, // prompt input remarks,
                numsOfResults: 1,
                //numsOfResults: isDraft?this.numsOfResults:1,
                isDraft: isDraft,
                suffix: this.promptParamForm.suffix,
                prefix: this.promptParamForm.prefix,

            }
            // ai scoring criteria
            _postData.isAIGrade = this.isAIGrade
            if (this.aiScoreForm.resultList.length > 0) {
                let _list = this.aiScoreForm.resultList.map(item => item.value)
                _list = _list.filter(item => item)
                if (_list.length > 0) {
                    _postData.expectedResultsJson = JSON.stringify(_list)
                }
            }
            
            // Request parameters
            if (this.promptParamForm.variableList.length > 0) {
                _postData.variableDictJson = this.convertData(this.promptParamForm.variableList)
            }

            this.parameterViewList.forEach(item => {
                console.log('item' + item);
                // todo is handled separately
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
            // To submit this.promptField
            _postData['rangeId'] = this.promptField

            if (isDraft && _isPromptDraft) {
                return await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.UpdateDraftAsync?promptItemId=${this.promptid}`, _postData).then(res => {
                    this.targetShootLoading = false
                    if (res.data.success) {
                        this.pageChange = false
                        // Prompt to save successfully
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
                            // Prompt to save successfully
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
                        // copy data
                        let copyResultData = JSON.parse(JSON.stringify(res.data.data))
                        delete copyResultData.promptResultList
                        let vArr = copyResultData.fullVersion.split('-')
                        copyResultData.promptFieldStr = vArr[0] || ''
                        copyResultData.promptStr = vArr[1] || ''
                        copyResultData.tacticsStr = vArr[2] || ''
                        this.promptDetail = copyResultData
                        // average score 
                        this.outputAverageDeci = evalAvgScore > -1 ? evalAvgScore : -1;
                        // highest score
                        this.outputMaxDeci = evalMaxScore > -1 ? evalMaxScore : -1;
                        // Output list
                        this.outputList = promptResultList.map(item => {
                            if (item) {
                                item.promptId = id
                                item.version = fullVersion
                                item.scoreType = '1' // 1 ai, 2 manual 
                                item.isScoreView = false // Whether to display the rating view
                                //time format addTime
                                item.addTime = item.addTime ? this.formatDate(item.addTime) : ''

                                //Use MarkDown format to display the output results
                                item.resultStringHtml = marked.parse(item.resultString);

                                // Manual scoring
                                item.scoreVal = 0
                                // ai scoring expected results
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
                        
                        // Add code block copy button
                        this.$nextTick(() => {
                            this.addCopyButtonsToCodeBlocks();
                        });
                        
                        //After submitting your data, select the correct range and lane
                        this.getFieldList().then(() => {

                            this.getPromptOptData(id)
                            // Get score trend chart data
                            this.getScoringTrendData()
                        })

                        if (this.sendBtnText !== '保存草稿' && this.numsOfResults > 1) {
                            //Enter the burst mode and call the burst interface N times according to the number of numOfResults-1
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
         * Continuous events
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
            
            // Note: Now the backend will automatically determine the type based on the first PromptResult
            // If the first result is Chat mode, the backend will get the userMessage from the conversation record
            // So the front end does not need to pass userMessage, the back end will handle it automatically.
            // But for compatibility, if the front end has a saved userMessage, it can still be passed
            
            let promises = [];
            for (let i = 0; i < howmany; i++) {
                // The backend will automatically determine the mode of the first result, and the frontend does not need to pass userMessage
                // But if the front end has a saved userMessage, it can be passed to maintain consistency
                promises.push(this.rapidFireHandel(id, this._lastUserMessage || null));
            }
            await Promise.all(promises)
            // Retrieve shooting range list
            this.getPromptOptData()
            this.targetShootLoading = false
            this.dodgersLoading = false
        },
        // Import plugins dialog close callback
        uploadPluginCloseDialog() {
            // Clear fileData
            this.uploadPluginDropAreaVisible = true
            this.uploadPluginData = []
            this.jsZip = null
        },
        // Import plugins when dragging back and forth in the drag area
        pluginDropOverHandler(e) {
            e.stopPropagation();
            e.preventDefault();
            this.uploadPluginDropHover = true;
        },
        // Import plugins when entering the drag area for the first time
        pluginDropEnterHandler(e) {
            e.stopPropagation();
            e.preventDefault();
            this.uploadPluginDropHover = true;
        },
        // Import plugins drag and drop
        pluginDropLeaveHandler(e) {
            e.stopPropagation();
            e.preventDefault();
            this.uploadPluginDropHover = false;
        },
        // Import plugins drag and drop select folder
        enentPluginDrop(e) {
            this.uploadPluginDropHover = false
            let items = e.dataTransfer.items;
            if (!items) return
            this.uploadPluginDropAreaVisible = false
            this.jsZip = new JSZip();
            for (let i = 0; i <= items.length - 1; i++) {
                let item = items[i];
                if (item.kind === "file") {
                    // FileSystemFileEntry or FileSystemDirectoryEntry object
                    let entry = item.webkitGetAsEntry();
                    // Recursively obtain all files contained under entry
                    this.getFileFromEntryRecursively(entry);
                }
            }

            // console.log('Drop',items);
            e.stopPropagation();
            e.preventDefault();
        },
        // Drag and drop to upload files
        getFileFromEntryRecursively(entry) {
            //let _this = this
            if (entry.isFile) {
                // document
                entry.file(
                    file => {
                        //console.log('Drop file', { file, path: _path });
                        // If you want to retain the drag-and-drop hierarchy, you can only get it from entry
                        // The purpose of taking path is to get the first-level name of the uploaded folder
                        let _path = entry.fullPath
                        if (entry.fullPath.startsWith('/')) {
                            _path = entry.fullPath.slice(1)
                        }
                        this.forEachZip(file, _path);
                        // file list
                        this.uploadPluginData.push({ name: file.name, path: _path })
                    },
                    e => { console.log(e); }
                );
            } else {
                // folder
                let reader = entry.createReader();
                reader.readEntries(
                    entries => {
                        entries.forEach(entry => this.getFileFromEntryRecursively(entry));
                    },
                    e => { console.log(e); }
                );
            }
        },
        // Import plugins Click Select Folder
        enentPluginInput() {
            let input = document.createElement("input");
            input.type = "file";
            input.setAttribute("allowdirs", "true");
            input.setAttribute("directory", "true");
            input.setAttribute("webkitdirectory", "true"); //After setting up webkitdirectory, you can select a folder to upload.
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
                // Process all sub-files in a folder
                for (let i = 0; i <= files.length - 1; i++) {
                    _this.uploadPluginData.push({ name: files[i].name, path: files[i].webkitRelativePath })
                    // The purpose of taking path is to get the first-level name of the uploaded folder
                    _this.forEachZip(files[i], files[i].webkitRelativePath);

                }

                document.querySelector("body").removeChild(input);
            };
        },
        // Add uploaded files to compressed package
        forEachZip(file, path) {
            //console.log('forEachZip files：', file, path)
            // Categorize processed files into specified folders
            let _path = path
            let _index = path.indexOf('/')
            if (_index > -1) {
                _path = _path.slice(_index + 1)
            }
            this.jsZip.file(`${_path}`, file);
        },
        // Import plugins upload button
        submitUploadPlugins() {
            let _fileData = JSON.parse(JSON.stringify(this.uploadPluginData))
            if (_fileData.length === 0) {
                this.$message.warning('请选择文件夹')
                return
            }
            if (this.isPageLoading) return
            this.isPageLoading = true
            let name = _fileData[0].path.split('/')[0] || 'plugin'
            // Generate compressed file
            this.jsZip.generateAsync({ type: "blob" }).then((content) => {
                //Convert blob type to file type for uploading
                let zipFile = new File([content], `${name}.zip`, {
                    type: "application/zip",
                });
                //Make a size limit
                //let isLt2M = zipFile.size / 1024 / 1024 < 80;
                //if (!isLt2M) {
                //    this.fileList = [];
                //    this.$message({
                //        message: "The uploaded file size cannot exceed 80MB!",
                //        type: "warning",
                //    });
                //    return false;
                //} else {
                //    let filedata = new FormData();
                //    // filedata.append("file", zipFile);
                //    filedata.append("zipFile", zipFile);
                //    this.folderHandlesubmit(filedata); //Upload event, filedata is already a compressed file
                //    //saveAs(content, `${name}.zip`); //For downloading, you can download the file to check whether the uploaded one is correct.
                //}
                let filedata = new FormData();
                // filedata.append("file", zipFile);
                filedata.append("zipFile", zipFile);
                this.folderHandlesubmit(filedata); //Upload event, filedata is already a compressed file
                //saveAs(content, `${name}.zip`); //For downloading, you can download the file to check whether the uploaded one is correct.
            }).catch(() => {
                this.isPageLoading = false
            })
        },
        // Upload Plugins api
        folderHandlesubmit(formData) {
            //ajax upload formData
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
                    // Update range data
                    this.getFieldList().then(() => {
                        if (this.promptFieldOpt && this.promptFieldOpt.length > 0) {
                            this.promptField = this.promptFieldOpt[this.promptFieldOpt.length - 1].id
                            // Reset page data
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
        // Export plugins dialog close callback
        expectedPluginCloseDialog() {
            this.expectedPluginFoem = {
                checkList: []
            }
            this.expectedPluginFieldList = [] // tree data list
            this.exportPluginSelectedCount = 0 // Reset selected quantity
            this.exportPluginExpandAll = false // Reset expanded state
            this.$refs.expectedPluginFoem.resetFields();
        },
        // export plugins dialog open
        async expectedPluginOpen() {
            // Get tree data shooting range list
            let _valList = this.promptFieldOpt
            let promises = [];
            for (let i = 0; i < _valList.length; i++) {
                // Get target lane list
                promises.push(this.getPromptOptData(_valList[i].id, true));
            }
            this.expectedPluginVisible = true
            await Promise.all(promises)
            // Wait for DOM to update before setting selection and update counts
            this.$nextTick(() => {
            // Set default selected
            this.$refs.expectedPluginTree.setCheckedKeys(this.expectedPluginFoem.checkList)
                
                // Make sure checkList only contains leaf nodes
                const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
                const leafNodeKeys = [];
                
                checkedNodes.forEach(node => {
                    // Only record leaf nodes (target lanes)
                    if (!node.children || node.children.length === 0) {
                        leafNodeKeys.push(node.idkey);
                    }
                });
                
                this.expectedPluginFoem.checkList = leafNodeKeys;
                
                // Update selected quantity
                this.updateExportPluginSelectedCount()
            })
        },
        // Export plugins dialog tree selected changes
        treeCheckChange(data, currentCheck, childrenCheck) {
            // Update the selected quantity (use $nextTick to ensure the Tree state is updated)
            this.$nextTick(() => {
                // Use Tree API to re-fetch all selected nodes (only leaf nodes)
                if (!this.$refs.expectedPluginTree) return;
                
                const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
                const leafNodeKeys = [];
                
                checkedNodes.forEach(node => {
                    // Only record leaf nodes (target lanes)
                    if (!node.children || node.children.length === 0) {
                        leafNodeKeys.push(node.idkey);
                    }
                });
                
                // Update checkList to include only leaf nodes
                this.expectedPluginFoem.checkList = leafNodeKeys;
                
                // update count
                this.updateExportPluginSelectedCount();
            });
        },
        
        // Update the number of selected target lanes
        updateExportPluginSelectedCount() {
            if (!this.$refs.expectedPluginTree) {
                this.exportPluginSelectedCount = 0;
                return;
            }
            
            // Use the API of the Tree component to get all selected nodes (including the child nodes of the parent node in the semi-selected state)
            const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
            const halfCheckedNodes = this.$refs.expectedPluginTree.getHalfCheckedNodes();
            
            // Count all selected child nodes (target lane)
            // Characteristics of the target lane: There is an idkey and contains an underscore, or there is no children
            let count = 0;
            
            // Count target lanes in fully selected nodes
            checkedNodes.forEach(node => {
                // If it is a leaf node (target channel), statistics
                if (!node.children || node.children.length === 0) {
                    count++;
                }
            });
            
            // For a parent node in a half-selected state, it is necessary to count its selected child nodes.
            // Element UI Tree's getCheckedNodes() already contains all selected child nodes, so no additional processing is required.
            
            this.exportPluginSelectedCount = count;
        },
        
        // Export plugin - select all
        exportPluginSelectAll() {
            if (!this.$refs.expectedPluginTree) return;
            
            // Get the keys of all nodes (including parent nodes and child nodes)
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
            
            // Set selected
            this.$refs.expectedPluginTree.setCheckedKeys(allKeys);
            
            // After waiting for the DOM to be updated, only the leaf nodes are recorded.
            this.$nextTick(() => {
                const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
                const leafNodeKeys = [];
                
                checkedNodes.forEach(node => {
                    // Only record leaf nodes (target lanes)
                    if (!node.children || node.children.length === 0) {
                        leafNodeKeys.push(node.idkey);
                    }
                });
                
                this.expectedPluginFoem.checkList = leafNodeKeys;
                this.updateExportPluginSelectedCount();
            });
            
            this.$message.success('已全选所有靶道');
        },
        
        // Export plugin - inverse selection
        exportPluginInvertSelection() {
            if (!this.$refs.expectedPluginTree) return;
            
            // Collect the keys of all leaf nodes (target lanes)
            const allLeafKeys = [];
            const collectLeafKeys = (nodes) => {
                nodes.forEach(node => {
                    if (!node.children || node.children.length === 0) {
                        // This is the leaf node (target lane)
                        allLeafKeys.push(node.idkey);
                    } else {
                        // This is the parent node (shooting range), continue the recursion
                        collectLeafKeys(node.children);
                    }
                });
            };
            collectLeafKeys(this.expectedPluginFieldList);
            
            // Get the currently selected leaf node key (use leafOnly=true parameter)
            const currentCheckedLeafKeys = this.$refs.expectedPluginTree.getCheckedKeys(true);
            
            // Inverse selection: all leaf nodes - currently selected leaf nodes
            const invertedLeafKeys = allLeafKeys.filter(key => !currentCheckedLeafKeys.includes(key));
            
            // Set the result after inverse selection (only set leaf nodes)
            this.$refs.expectedPluginTree.setCheckedKeys(invertedLeafKeys);
            
            // Update status after waiting for DOM update
            this.$nextTick(() => {
                this.expectedPluginFoem.checkList = invertedLeafKeys;
                this.updateExportPluginSelectedCount();
            });
            
            this.$message.success('已反选');
        },
        
        // Export plugin - clear selection
        exportPluginClearAll() {
            if (!this.$refs.expectedPluginTree) return;
            
            this.$refs.expectedPluginTree.setCheckedKeys([]);
            this.expectedPluginFoem.checkList = [];
            
            // Wait for the DOM to be updated before counting
            this.$nextTick(() => {
                this.updateExportPluginSelectedCount();
            });
            
            this.$message.info('已清空所有选择');
        },
        
        // Export plugin - toggle expand/collapse
        exportPluginToggleExpand() {
            this.exportPluginExpandAll = !this.exportPluginExpandAll;
            
            // The tree needs to be re-rendered to apply the expanded state
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
        // Export plugins Confirm
        btnExpectedPlugins() {
            this.$refs.expectedPluginFoem.validate(async (valid) => {
                if (valid) {
                    //console.log('Export plugins confirmation', this.expectedPluginFoem.checkList)
                    //return
                    this.isPageLoading = true
                    // Export plugins
                    let _zipname = 'plugins'
                    let _rangeIds = []
                    let _ids = []
                    this.expectedPluginFoem.checkList.forEach(item => {
                        // Determine whether it is included - if included, it is the target lane, otherwise it is the shooting range
                        if (item.indexOf('_') > -1) {
                            let _itemArr = item.split('_')
                            _ids.push(Number(_itemArr[1]))
                            _rangeIds.push(Number(_itemArr[0]))
                        } else {
                            _rangeIds.push(Number(item))
                        }
                    })
                    // _rangeIds and _ids deduplication
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
                            rangeIds: _rangeIds,//range
                            ids: _ids//target lane
                        }
                    }).then((res) => {
                        this.isPageLoading = false
                        this.expectedPluginVisible = false
                        const blob = new Blob([res.data], { type: 'application/zip' });
                        const url = URL.createObjectURL(blob);
                        const a = document.createElement('a');
                        a.href = url;
                        a.download = `${_zipname}.zip`; // Set the downloaded file name, which can be modified as needed
                        a.click(); // Trigger click event to start downloading
                        // Remove <a> tag after download is complete
                        URL.revokeObjectURL(url); // Release the URL object
                        a.parentNode && a.parentNode.removeChild(a); // Remove <a> tag from DOM
                        
                        this.$message.success('导出成功！');
                    }).catch((error) => {
                        this.isPageLoading = false
                        console.error('导出失败:', error);
                        
                        let errorMessage = '导出 plugins 失败';
                        if (error.response) {
                            // Server returns error response
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
        // ai rating delete
        deleteAiScoreBtn(index) {
            //console.log('delete', index)
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
        // Add new shooting range
        addPromptField() {
            // If the range changes, the range
            this.fieldFormVisible = true
        },
        // Number of bursts Quantity changes
        changeNumsBtn(command = 1) {
            this.numsOfResults = command
        },
        // Target button type switching
        changeBtn(command) {
            this.sendBtnText = command
        },
        // Click the target button to trigger the corresponding type of event
        clickSendBtn() {
            const command = this.sendBtnText
            //console.log('clicked' + command)
            if (command === '打靶') {
                this.targetShootHandel()
            } else if (command === '保存草稿') {
                this.targetShootHandel(true)
            } else if (command === '连发') {
                this.dealRapicFireHandel(this.numsOfResults)
            }
        },
        // beforeunload event handler function
        beforeunloadHandler(e) {
            //console.log('Browser close|Browser refresh|Page close|Open new page')
            // If the data has not changed, there is no need to prompt the user to save it.
            if (this.pageChange) {
                // Show custom dialog
                let confirmationMessage = '您的数据已经修改，是否保存为草稿？';
                // Block default behavior
                e.preventDefault();
                // Compatible with older browsers
                e.returnValue = confirmationMessage;
                return confirmationMessage;
            }
            //setTimeout(function () {
            //    // Pop up a custom modal box
            //    var modal = document.createElement("div");
            //    modal.innerHTML = "Are you sure you want to leave this page?";
            //    var btn = document.createElement("button");
            //    btn.textContent = "Stay on the page";
            //    btn.onclick = function () {
            //        //Cancel the default beforeunload behavior
            //        e.preventDefault();
            //        // Close the custom modal box
            //        modal.remove();
            //    };
            //    modal.appendChild(btn);
            //    document.body.appendChild(modal);
            //}, 0);
        },
        copyInfo(source) {
            let fullVersion = '';
            let label = '';
            
            // If parameters are passed in, copy the version number in the comparison window
            if (source === 'A' || source === 'B') {
                const prompt = source === 'A' ? this.comparePromptA : this.comparePromptB;
                if (!prompt || !prompt.fullVersion) {
                    this.$message.warning('无法获取版本号');
                    return;
                }
                fullVersion = prompt.fullVersion;
                label = `Prompt ${source} 版本号`;
            } else {
                // Otherwise copy the currently selected Prompt
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
            
            // Copy results to clipboard
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
        // Format time
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
        // Format chat time (more concise format)
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
            
            // Show specific date if more than 7 days
            var hh = (date.getHours() < 10 ? '0' + date.getHours() : date.getHours()) + ':'
            var mm = (date.getMinutes() < 10 ? '0' + date.getMinutes() : date.getMinutes())
            var MM = (date.getMonth() + 1 < 10 ? '0' + (date.getMonth() + 1) : date.getMonth() + 1) + '-'
            var DD = date.getDate() < 10 ? '0' + date.getDate() : date.getDate()
            return MM + DD + ' ' + hh + mm
        },
        // Format chat content (support markdown)
        formatChatContent(content) {
            if (!content) return ''
            // Use marked to parse markdown
            return marked.parse(content)
        },
        // Switch chat feedback (Like/Unlike)
        async toggleChatFeedback(chatId, feedback) {
            // Prevent repeated clicks: If it is being processed, return directly
            if (this._isUpdatingFeedback) {
                return
            }
            this._isUpdatingFeedback = true
            
            try {
                // Verify chatId is valid
                if (!chatId || chatId === 0 || chatId === '0') {
                    console.error('无效的 chatId:', chatId, '类型:', typeof chatId)
                    console.error('当前历史记录:', this.continueChatHistory)
                    this.$message({
                        message: '对话记录ID无效，请刷新页面后重试',
                        type: 'error'
                    })
                    return
                }
                
                // Make sure chatId is a numeric type
                const numericChatId = typeof chatId === 'string' ? parseInt(chatId, 10) : chatId
                if (isNaN(numericChatId) || numericChatId <= 0) {
                    console.error('无效的 chatId（转换后）:', numericChatId, '原始值:', chatId)
                    this.$message({
                        message: '对话记录ID无效，请刷新页面后重试',
                        type: 'error'
                    })
                    return
                }
                
                // Find the current message (check both id and numericChatId)
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
                
                // Verify that the message ID is valid
                const msgId = typeof currentMsg.id === 'string' ? parseInt(currentMsg.id, 10) : currentMsg.id
                if (!msgId || msgId === 0 || isNaN(msgId)) {
                    console.error('消息 ID 无效:', currentMsg.id, '类型:', typeof currentMsg.id)
                    this.$message({
                        message: '消息ID无效，请刷新页面后重试',
                        type: 'error'
                    })
                    return
                }
                
                // If you click on the currently selected feedback, cancel the feedback (set to null)
                const newFeedback = currentMsg.userFeedback === feedback ? null : feedback
                
                // Call the API to update the feedback (using a valid message ID)
                // Note: Using the Request DTO format, the first letter of the attribute name needs to be capitalized
                const res = await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.UpdateChatFeedback`, {
                    ChatId: msgId,
                    Feedback: newFeedback
                })
                
                if (res.data.success) {
                    // Update local data
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
                // Reset flag to allow next click
                this._isUpdatingFeedback = false
            }
        },
        // Handle conversation history scrolling
        handleChatHistoryScroll(event) {
            // You can add scrolling-related logic here, such as displaying the scroll position, etc.
            // No special treatment is required at the moment
        },
        // Output Score Trend Chart Initialization
        chartInitialization() {
            let scoreChart = document.getElementById('promptPage_scoreChart');
            let chartOption = {
                tooltip: {
                    show: true,
                    confine: true, //Whether to limit the tooltip box to the area of ​​the chart.
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
                    //    interval: 10 //Display all x-axis
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
                    boxHeight: 150, // Height z-axis of 3D chart
                    boxWidth: 400, // Width of 3D chart x-axis
                    boxDepth: 150, // Depth y-axis of 3D chart
                    // The entire chart background can be a custom color or picture
                    environment: '#fff',
                    //Coordinate axis axis (line) control
                    axisLine: {
                        show: true,//This parameter needs to be set to true
                        // interval:200,//The display interval of the x, y coordinate axis scale labels, valid in the category axis.
                        lineStyle: {//axis style
                            color: 'rgba(0,0,0,0.3)',
                            opacity: 1,//(Single scales will not be affected)
                            width: 2//line width
                        }
                    },
                    // axis label
                    axisLabel: {
                        show: true,//Whether to display the scale (number on the scale, or category)
                        interval: 0,//The display interval of coordinate axis scale labels, valid in category axis.
                        formatter: function (v) {
                            return typeof v === 'number' ? v.toFixed(1) : v
                        },
                        textStyle: {
                            color: '#32b8be',//tick label style
                            //color: function (value, index) {
                            //    return value >= 6 ? 'green' : 'red';//Display color according to the range, the home page is valid for the value
                            //},
                            //  borderWidth:"",//The stroke width of the text.
                            //  borderColor:'',//The stroke color of the text.
                            fontSize: 12,//Tick ​​label font size
                            fontWeight: '400',//Thickness
                        }
                    },
                    //scale
                    axisTick: {
                        show: true,//Whether to display
                        // interval:100, //Display interval of coordinate axis scale labels, valid in category axis
                        //length: 5,//The length of the coordinate axis scale
                        //lineStyle: {//For example, if the style is too ugly, just make do with it.
                        //    color: '#000',//color
                        //    opacity: 1,
                        //    width: 5//Thickness (although width is expressed as height), corresponding to length*(width)
                        //}
                    },
                    //Dividers on a flat surface.
                    splitLine: {
                        show: true,//Three-dimensional grid lines
                        // interval:100, //Display interval of coordinate axis scale labels, valid in category axis
                        lineStyle: {//axis style
                            color: 'rgba(0,0,0,0.05)',
                            opacity: 1,//(Single scales will not be affected)
                            width: 1//line width
                        },
                        //splitArea: {
                        //    show: true,
                        //    //interval:100,//Display interval of coordinate axis scale labels, valid in category axis
                        //    areaStyle: {
                        //        color: ['rgba(250,250,250,0.2)', 'rgba(200,200,200,0.3)', 'rgba(250,250,250,0.2)', 'rgba(200,200,200,0.2)']
                        //    }
                        //},
                    },
                    // Coordinate axis indicator line.
                    axisPointer: {
                        show: false,//The display line of the mouse on the chart
                        // lineStyle:{
                        //     color:'#000',//color
                        //     opacity:1,
                        //     width:5//Thickness (although width is expressed as height), corresponding to length*(width)
                        // }
                    },
                    //viewControl is used for mouse rotation, zoom and other perspective control. (The following is suitable for earth rotation, etc.)
                    viewControl: {
                        minBeta: 0, //Minimum rotation angle
                        maxBeta: 90, //Maximum rotation angle
                        minAlpha: 0, //Minimum rotation angle
                        maxAlpha: 90, //Maximum rotation angle
                        rotateSensitivity: 10,//Rotation sensitivity, the larger the value, the faster the rotation
                        // projection: 'orthographic' //The default is perspective projection 'perspective', and it also supports setting to orthogonal projection 'orthographic'.
                        // autoRotate:true,//There will be an automatic rotation viewing animation, and each dimension information can be viewed
                        // autoRotateDirection:'ccw',//The direction of the object's autobiography. The default is 'cw', which means the direction is clockwise when viewed from top to bottom, or 'ccw', which means the direction is counterclockwise when viewed from top to bottom.
                        // autoRotateSpeed:12,//The speed of the object’s autorotation
                        // autoRotateAfterStill:2,//The time interval for resuming automatic rotation after the mouse is stationary. Valid after turning on autoRotate.
                        distance: 350,//The distance between the default viewing angle and the subject (commonly used)
                        alpha: 1,//The viewing angle is around the x-axis, that is, the angle of up and down rotation (together with beta, it controls the visual field imaging effect)
                        beta: 30,//The angle of view rotates around the y-axis, which is the angle of left-right rotation.
                        // center:[]//The center point of the perspective, the rotation will also rotate around this center point, the default is [0,0,0]
                        animation: true,
                    },
                    //Lighting related settings
                    //light: {
                    //    main: {
                    //        color: '#fff',//The lighting color will be mixed with the set color
                    //        intensity: 1.2,//The intensity of the main light source (the intensity of the light)
                    //        shadow: false, //Whether the main light source casts shadows. Off by default.
                    //        // alpha:0//The main light source rotates around the x-axis, that is, the angle of up and down rotation. Use beta to control the direction of the light source (combine with beta to determine the position of the sun)
                    //        // beta:10//The main light source rotates around the y-axis, that is, the angle of left and right rotation.
                    //    },
                    //    ambient: {//Global ambient light settings.
                    //        intensity: 0.3,
                    //        color: '#fff'//Affects the color of the bar
                    //    },
                    //    //ambientCubemap: {//The texture will be used as the ambient light of the light source
                    //    //  texture: 'pisa.hdr',
                    //    // // Exposure value used when parsing hdr
                    //    // exposure: 1.0
                    //    // }
                    //},
                    // postEffect:{//Relevant configuration of post-processing special effects. Post-processing special effects can add highlights, depth of field, ambient light occlusion (SSAO), color adjustment and other effects to the picture. It can make the whole picture more textured.
                    //     show:true,
                    //     bloom:{
                    //         enable:true//Highlight special effect, suitable for globe
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
                        data: item,    //The data in each area corresponds to one-to-one
                        itemStyle: {
                            opacity: 0.7
                        },
                        label: {
                            position: 'top',
                            show: true,
                            formatter: function (params) {
                                const promptItem = that.promptOpt.find(item => item.id === that.promptid)
                                const fullVersion = promptItem ? promptItem.fullVersion : ''
                                return params.data[3].fullVersion === fullVersion ? '当前' : ' ';  // Fix label content to ""
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
            // Listen for click events
            chartInstance.on('click', (params) => {
                // console.log('click params：', params)
                const promptItem = this.promptOpt.find(item => item.fullVersion === params.data[3].fullVersion)
                if (promptItem) {
                    // Set overbearing selected
                    this.promptid = promptItem.id
                    // Get target lane details
                    this.getPromptetail(promptItem.id, true)
                    this.chartInstance.resize()
                    // Get output list and average score
                    //this.getOutputList()
                    // Get score trend chart data
                    // this.getScoringTrendData()
                }
            })
            //Listen to the chart mouse move event mouseover globalout
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
            //     //Add the corresponding scatter3D
            //     _scatterSeries.push({
            //         type: 'scatter3D',
            //         name: params.seriesName,
            //         symbol: 'circle', // Set the dot style to circle
            //         symbolSize: 10, //Set the size of the dots
            //         label: {
            //             show: false, //Set label display
            //             formatter: function (params) {
            //                 return ''; // Fix label content to ""
            //             }
            //         },
            //         data: [params.data] //One-to-one correspondence between data in each area
            //     })
            //     chartInstance.setOption({ series: [..._series, ..._scatterSeries] });
            // })
            //Listen to chart mouse out events
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
        // Output Get rating trend chart data
        async getScoringTrendData() {
            this.chartData = {
                xData: [],
                yData: [],
                seriesData: []
            }
            if (this.promptid) {
                //console.log('Get rating trend chart data', this.isAvg)
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
                // Initialization chart interface call successful
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
        // Range|Range|Model Selection Changes
        promptChangeHandel(val, itemKey, oldVal) {
            // When the target lane changes, reset the target button
            this.numsOfResults = 1
            //console.log(this.promptFieldOldVal,'|', val, '|', itemKey, '|', oldVal)
            if (itemKey === 'promptField') {
                // If the range changes, the range
                if (this.pageChange && this.modelid) {
                    // Tip: There are data changes. Do you want to save it as a draft?
                    this.$confirm('您的数据已经修改，是否保存为草稿？', '提示', {
                        confirmButtonText: '保存',
                        cancelButtonText: '不保存',
                        type: 'warning'
                    }).then(() => {
                        // save draft
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
                // Reset page data
                this.resetPageData()
            } else if (itemKey === 'promptid') {

                if (this.pageChange && this.modelid) {
                    // Tip: There are data changes. Do you want to save it as a draft?
                    this.$confirm('您的数据已经修改，是否保存为草稿？', '提示', {
                        confirmButtonText: '保存',
                        cancelButtonText: '不保存',
                        type: 'warning'
                    }).then(() => {
                        // save draft
                        this.targetShootHandel(true).then(() => {
                            this.resetPageData()
                            this.getPromptetail(val, true, true)
                        })
                        // Reset page change history
                        this.pageChange = false
                        // Retrieve target list
                        //this.getFieldList()
                    }).catch(() => {
                        // Reset page change history
                        this.pageChange = false
                        // The position of val in promptOpt
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

                        // Clear ai scoring criteria
                        this.aiScoreForm = {
                            resultList: []
                        }

                        // Get target lane details
                        this.getPromptetail(val, true, true)

                    });
                } else {
                    // Reset page change history
                    this.pageChange = false
                    // Clear ai scoring criteria
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
                    // target lane
                    this.getPromptetail(val, true, true)
                }

            } else {

                // other
                //if (itemKey === 'modelid'){}
                // Page change record
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
        // Reset page data
        async resetPageData() {
            // Reset page change history
            this.pageChange = false
            // range
            this.promptid = '' // target lane
            this.modelid = '' // Model
            // Parameter settings View configuration list
            this.resetConfigurineParam(false)
            // Enter Prompt to reset
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
            // ai scoring criteria reset
            this.aiScoreForm = {
                resultList: []
            }
            this.numsOfResults = 1
            if (this.promptField) {
                // Get target lane list
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

                        // Get details
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
                // Get score trend chart data
                await this.getScoringTrendData()
            }

        },

        // Continue chatting: Load history and open tactical selection popup
        async continueChat(promptResultId, resultIndex) {
            try {
                // Get both conversation history and prompt content using new API
                const res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetChatHistoryWithPrompt?promptResultId=${promptResultId}`)
                if (res.data.success) {
                    this.continueChatMode = true
                    this.continueChatPromptResultId = promptResultId
                    this.continueChatHistory = res.data.data.chatHistory || []
                    this.continueChatSystemMessage = res.data.data.promptContent || ''
                    
                    // Open the tactical selection pop-up window and lock it in dialogue mode
                    this.tacticalForm.chatMode = '对话模式'
                    this.tacticalFormVisible = true
                    
                    // Scroll to bottom for latest news
                    this.$nextTick(() => {
                        const container = document.getElementById('chatHistoryContainer')
                        if (container) {
                            container.scrollTop = container.scrollHeight
                        }
                        // Add code block copy button
                        this.addCopyButtonsToCodeBlocks();
                    })
                } else {
                    // Downgrade scenario: If new API fails, use old API
                    const fallbackRes = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetChatHistory?promptResultId=${promptResultId}`)
                    if (fallbackRes.data.success) {
                        this.continueChatMode = true
                        this.continueChatPromptResultId = promptResultId
                        this.continueChatHistory = fallbackRes.data.data || []
                        // Try to get Prompt content from outputList
                        const resultItem = this.outputList.find(item => item.id === promptResultId)
                        if (resultItem && resultItem.promptId && this.promptDetail && this.promptDetail.id === resultItem.promptId) {
                            this.continueChatSystemMessage = this.promptDetail.promptContent || this.content || ''
                        } else {
                            this.continueChatSystemMessage = this.content || ''
                        }
                        
                        this.tacticalForm.chatMode = '对话模式'
                        this.tacticalFormVisible = true
                        
                        this.$nextTick(() => {
                            const container = document.getElementById('chatHistoryContainer')
                            if (container) {
                                container.scrollTop = container.scrollHeight
                            }
                            this.addCopyButtonsToCodeBlocks();
                        })
                    } else {
                        this.$message({
                            message: fallbackRes.data.errorMessage || '加载对话历史失败',
                            type: 'error'
                        })
                    }
                }
            } catch (error) {
                this.$message({
                    message: '加载对话历史失败：' + (error.message || '未知错误'),
                    type: 'error'
                })
            }
        },
        
        // Handle keyboard events of dialog input boxes (shortcut key support)
        handleChatInputKeydown(e) {
            // Ctrl+Enter (Windows/Linux) or Cmd+Enter (Mac)
            if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
                e.preventDefault()
                e.stopPropagation()
                // trigger commit
                this.tacticalFormSubmitBtn()
            }
        },
        
        // Tactical Options Close Popup
        // Tactics selection dialog close
        tacticalFormCloseDialog() {
            this.tacticalForm = {
                tactics: '重新瞄准',
                chatMode: '对话模式' // reset to default
            }
            // Clear conversation input
            this.tacticalChatInput = ''
            // Reset chat status
            this.continueChatMode = false
            this.continueChatPromptResultId = null
            this.continueChatHistory = []
            this.continueChatSystemMessage = ''
            this.systemMessageCollapse = []
            if (this.$refs.tacticalForm) {
                // Use clearValidate to clear validation status instead of resetFields
                // Because some fields may be displayed conditionally (v-if), resetFields may error
                this.$refs.tacticalForm.clearValidate();
            }
        },
        
        // Open the map dialog box
        openMapDialog() {
            if (!this.promptField) {
                this.$message({
                    message: '请先选择靶场',
                    type: 'warning'
                })
                return
            }
            
            // Check whether the current shooting range has target track data
            if (!this.promptOpt || this.promptOpt.length === 0) {
                this.$message({
                    message: '当前靶场还没有进行过打靶，请打靶后再来看吧！',
                    type: 'info',
                    duration: 3000
                })
                return
            }
            
            this.mapDialogVisible = true
            this.$nextTick(() => {
                this.initMap3D()
            })
        },
        
        // Close the map dialog box
        mapDialogClose() {
            this.destroyMap3D()
        },
        
        // Initialize 3D map
        initMap3D() {
            const container = document.getElementById('map3dContainer')
            if (!container) return
            
            // Build tree-structured data
            this.buildTreeData()
            
            // Make sure THREE is loaded
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
            
            // Create a scene (using a gradient background)
            this.map3dScene = new THREE.Scene()
            // Create a gradient background
            const gradientTexture = this.createGradientBackground()
            this.map3dScene.background = gradientTexture
            
            // Disable the fog effect to ensure that distant nodes are clearly visible
            this.map3dScene.fog = null
            
            // Create camera (increase far clipping plane, make sure all nodes are visible)
            const width = container.clientWidth
            const height = container.clientHeight
            // Parameters: Field of view (75 degrees), aspect ratio, near clipping plane (0.1), far clipping plane (5000 - increased to support further nodes)
            this.map3dCamera = new THREE.PerspectiveCamera(75, width / height, 0.1, 5000)
            this.map3dCamera.position.set(0, 0, 50)
            
            // Create renderer (disables logarithmic depth buffering, improves clarity of distant objects)
            this.map3dRenderer = new THREE.WebGLRenderer({ 
                antialias: true,
                logarithmicDepthBuffer: false,
                precision: 'highp' // Use high precision to improve rendering quality
            })
            this.map3dRenderer.setSize(width, height)
            // Set pixel ratio for better clarity on high DPI screens
            this.map3dRenderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
            container.appendChild(this.map3dRenderer.domElement)
            
            // Add a controller (using localized OrbitControls)
            if (typeof THREE.OrbitControls !== 'undefined') {
                this.map3dControls = new THREE.OrbitControls(this.map3dCamera, this.map3dRenderer.domElement)
                
                // Enable damping effect for smoother rotation
                this.map3dControls.enableDamping = true
                this.map3dControls.dampingFactor = 0.05
                
                // Enable zoom (increases maximum distance, supports further viewing angles)
                this.map3dControls.enableZoom = true
                this.map3dControls.zoomSpeed = 1.2
                this.map3dControls.minDistance = 10
                this.map3dControls.maxDistance = 500 // Increased from 200 to 500
                
                // Enable rotation
                this.map3dControls.enableRotate = true
                this.map3dControls.rotateSpeed = 0.8
                
                // Enable panning
                this.map3dControls.enablePan = true
                this.map3dControls.panSpeed = 0.8
                this.map3dControls.screenSpacePanning = true
                
                // Set the initial camera position so that it can see the entire scene
                this.map3dCamera.position.set(30, 30, 50)
                this.map3dControls.target.set(0, 0, 0)
                this.map3dControls.update()
            } else {
                console.warn('OrbitControls 未找到，3D 场景将无法通过鼠标控制')
            }
            
            // Add a richer light source system (enhance lighting of distant objects)
            // Ambient Light - Provides basic lighting (increases brightness to illuminate distant nodes)
            const ambientLight = new THREE.AmbientLight(0xffffff, 0.6) // Increased from 0.4 to 0.6
            this.map3dScene.add(ambientLight)
            
            // Main directional light - simulates sunlight (no attenuation, illuminates all nodes)
            const directionalLight1 = new THREE.DirectionalLight(0xffffff, 0.8)
            directionalLight1.position.set(20, 20, 20)
            directionalLight1.castShadow = false
            this.map3dScene.add(directionalLight1)
            
            // Auxiliary directional light - supplementary lighting (light from the other side)
            const directionalLight2 = new THREE.DirectionalLight(0x88ccff, 0.5) // Increased from 0.4 to 0.5
            directionalLight2.position.set(-20, 10, -20)
            this.map3dScene.add(directionalLight2)
            
            // Third directional light - illuminates distant nodes from the front
            const directionalLight3 = new THREE.DirectionalLight(0xffffff, 0.4)
            directionalLight3.position.set(0, 0, 50)
            this.map3dScene.add(directionalLight3)
            
            // Point light source - increase the sense of layering (no attenuation distance limit)
            const pointLight = new THREE.PointLight(0xffffff, 0.6, 0) // distance=0 means infinite distance
            pointLight.position.set(0, 20, 0)
            this.map3dScene.add(pointLight)
            
            // render node
            this.renderTreeNodes()
            
            // Start animation loop
            this.animateMap3D()
            
            // Handling window size changes
            window.addEventListener('resize', this.handleMap3DResize)
        },
        
        // Build tree-structured data
        buildTreeData() {
            if (!this.promptOpt || this.promptOpt.length === 0) {
                this.map3dTreeData = null
                return
            }
            
            const tree = {}
            const currentPromptId = this.promptid
            
            // Parse the FullVersion of each target lane
            this.promptOpt.forEach(prompt => {
                const fullVersion = prompt.fullVersion || prompt.label
                if (!fullVersion) return
                
                // Parsing format: 2023.12.14.1-T1.1-A123
                const parts = fullVersion.split('-')
                if (parts.length < 2) return
                
                const rangeName = parts[0] // 2023.12.14.1
                const tacticPart = parts[1] // T1.1
                const aimingPart = parts[2] || '' // A123
                
                // Get or create the RangeName root node
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
                
                // Parse Tactic (each . is a layer)
                const tacticParts = tacticPart.replace('T', '').split('.')
                let currentNode = rangeNode.children
                let lastTacticNode = null
                
                tacticParts.forEach((part, index) => {
                    const key = `T${tacticParts.slice(0, index + 1).join('.')}`
                    
                    if (!currentNode[key]) {
                        // Fix parentPath: should contain full path prefix
                        const parentFullPath = index > 0 
                            ? `${rangeName}-T${tacticParts.slice(0, index).join('.')}` // For example: "Range1-T2.1"
                            : rangeName // For example: "Range1"
                        
                        currentNode[key] = {
                            type: 'tactic',
                            name: part,
                            fullPath: `${rangeName}-${key}`,
                            parentPath: parentFullPath,
                            children: {},
                            prompts: [],
                            expanded: true
                        }
                    }
                    lastTacticNode = currentNode[key]
                    currentNode = currentNode[key].children
                })
                
                // Make sure lastTacticNode exists
                if (!lastTacticNode) {
                    console.warn('无法找到 Tactic 节点:', tacticPart)
                    return
                }
                
                // Add Aiming (special layer) - Fix: Use unique key to avoid sharing
                if (aimingPart) {
                    // Use the full path as the key to ensure that each Aiming node is independent
                    const aimingKey = `${tacticPart}-${aimingPart}` // For example: "T1.1-A1", "T1.2-A1"
                    if (!lastTacticNode.children[aimingKey]) {
                        lastTacticNode.children[aimingKey] = {
                            type: 'aiming',
                            name: aimingPart.replace('A', ''), // Remove A from display name
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
                    // If there is no Aiming, add it directly to the last Tactic node
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
        
        // ---Optimization function ---
        // Check if PromptCatalyzer has been initialized
        async checkPromptCatalyzerStatus() {
            try {
                const response = await servicePR.get('/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/CheckStatus');
                console.log('CheckStatus 完整响应:', response);
                
                // NCF AppResponseBase return format: { success: true, data: { isInitialized: true, ... } }
                if (response.data && response.data.success && response.data.data) {
                    return response.data.data.isInitialized;
                }
                return false;
            } catch (error) {
                console.error('检查初始化状态失败:', error);
                return false;
            }
        },

        // Get the list of available AI Models
        async loadAvailableModels() {
            this.loadingModels = true;
            try {
                const response = await servicePR.get('/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/GetAvailableModels');
                console.log('GetAvailableModels 完整响应:', response);
                
                // NCF AppResponseBase return format: { success: true, data: { models: [...], recommendedModelId: 1 } }
                if (response.data && response.data.success) {
                    this.availableModelsForInit = response.data.data.models || [];
                    
                    // Automatically select recommended Models
                    if (response.data.data.recommendedModelId) {
                        this.selectedModelIdForInit = response.data.data.recommendedModelId;
                    } else if (this.availableModelsForInit.length > 0) {
                        this.selectedModelIdForInit = this.availableModelsForInit[0].id;
                    }
                    
                    console.log('加载到', this.availableModelsForInit.length, '个可用 Model');
                } else {
                    // Handling error responses
                    const errorMsg = response.data?.errorMessage || '获取模型列表失败';
                    this.$message.error(errorMsg);
                    console.error('获取模型失败:', errorMsg);
                }
            } catch (error) {
                console.error('加载 AI Model 列表失败:', error);
                this.$message.error('加载 AI Model 列表失败: ' + (error.response?.data?.errorMessage || error.message));
            } finally {
                this.loadingModels = false;
            }
        },

        // Perform initialization
        async executeInitialization() {
            if (!this.selectedModelIdForInit) {
                this.$message.warning('请选择一个 AI Model');
                return;
            }

            this.initializing = true;
            try {
                console.log('开始初始化 PromptCatalyzer，使用 Model ID:', this.selectedModelIdForInit);
                
                const response = await servicePR.post('/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/Initialize', {
                    modelId: this.selectedModelIdForInit
                });

                console.log('Initialize 完整响应:', response);
                
                // NCF AppResponseBase return format: { success: true, data: { promptCode: "...", ... } }
                if (response.data && response.data.success) {
                    const initData = response.data.data || {};
                    this.$message({
                        message: `✅ 初始化成功！已创建 PromptCatalyzer Agent，PromptCode: ${initData.promptCode || '已创建'}`,
                        type: 'success',
                        duration: 6000,
                        showClose: true
                    });
                    
                    this.promptCatalyzerInitVisible = false;
                    
                    // After initialization is successful, refresh the page data
                    console.log('初始化成功，刷新页面数据...');
                    await this.getFieldList();
                    
                    // Continue with optimization
                    this.proceedWithOptimization();
                } else {
                    this.$message.error('初始化失败: ' + (response.data.errorMessage || '未知错误'));
                }
            } catch (error) {
                console.error('初始化失败:', error);
                const errorMsg = error.response?.data?.error || error.response?.data?.message || error.message;
                this.$message({
                    message: '初始化失败: ' + errorMsg,
                    type: 'error',
                    duration: 8000,
                    showClose: true
                });
            } finally {
                this.initializing = false;
            }
        },

        // Continue optimization (after initialization is complete)
        proceedWithOptimization() {
            // Reopen the optimization dialog
            this.optimizeRequirement = '';
            this.optimizeErrorText = '';
            this.optimizeDialogVisible = true;
        },
        
        // ⭐ NEW: Check scores and prompt for optimization
        async checkScoreAndSuggestOptimization(resultData, scoreType) {
            try {
                // Get final score
                let finalScore = null;
                if (resultData && typeof resultData === 'object') {
                    finalScore = resultData.finalScore;
                } else if (typeof resultData === 'number') {
                    finalScore = resultData;
                }
                
                // If score cannot be obtained, do not prompt
                if (finalScore === null || finalScore === undefined || finalScore === -1) {
                    console.log('无法获取有效分数，跳过优化提示');
                    return;
                }
                
                console.log(`${scoreType}完成，最终分数: ${finalScore}`);
                
                // Set the threshold for optimization suggestions (optimization is recommended when the score is lower than 6 points)
                const optimizationThreshold = 6.0;
                
                if (finalScore < optimizationThreshold) {
                    // The score is low, prompting the user whether optimization is needed
                    this.$confirm(
                        `当前 Prompt 的${scoreType}为 ${finalScore.toFixed(1)} 分（低于 ${optimizationThreshold} 分）。是否使用 AI 自动优化功能来改进 Prompt？`,
                        '💡 建议优化',
                        {
                            confirmButtonText: '立即优化',
                            cancelButtonText: '暂不优化',
                            type: 'warning',
                            center: true
                        }
                    ).then(async () => {
                        // User confirmation optimization
                        console.log('用户确认进行优化');
                        await this.openOptimizeDialog();
                    }).catch(() => {
                        // User cancels
                        console.log('用户取消优化');
                    });
                } else {
                    // Higher scores show a simple success message
                    console.log(`分数${finalScore.toFixed(1)}较高，无需提示优化`);
                }
            } catch (error) {
                console.error('检查分数并提示优化时出错:', error);
                // Do not display error messages to avoid affecting user experience
            }
        },
        
        // ⭐ New: Check whether optimization is needed based on the average score of PromptItem
        async checkPromptAverageScoreAndSuggest() {
            try {
                // Get the currently selected Prompt information
                if (!this.promptid) {
                    return;
                }
                
                const selectedPrompt = this.promptOpt.find(item => item.value === this.promptid);
                if (!selectedPrompt || !selectedPrompt.evalAvgScore) {
                    return;
                }
                
                const avgScore = selectedPrompt.evalAvgScore;
                
                // Do not prompt if average score is -1 or invalid
                if (avgScore === -1 || avgScore === null || avgScore === undefined) {
                    console.log('当前 Prompt 尚无平均分数');
                    return;
                }
                
                console.log(`当前 Prompt 平均分数: ${avgScore}`);
                
                // Set thresholds for optimization recommendations
                const optimizationThreshold = 6.0;
                
                // If the average score is lower than the threshold and the current Range has multiple test results, it will prompt optimization
                if (avgScore < optimizationThreshold) {
                    // Use Notification instead of Confirm to avoid blocking user operations
                    this.$notify({
                        title: '💡 优化建议',
                        message: `当前 Prompt 的平均分为 ${avgScore.toFixed(1)} 分，建议使用 AI 自动优化功能来改进。点击"优化"按钮开始。`,
                        type: 'warning',
                        duration: 8000,
                        position: 'bottom-right',
                        showClose: true
                    });
                    
                    console.log('平均分较低，已提示用户优化');
                } else {
                    console.log(`平均分 ${avgScore.toFixed(1)} 较高，无需提示优化`);
                }
            } catch (error) {
                console.error('检查平均分并提示优化时出错:', error);
            }
        },

        async openOptimizeDialog() {
            if (!this.promptid) {
                this.$message.warning('请先选择一个Prompt！');
                return;
            }
            
            // Check if it has been initialized
            console.log('检查 PromptCatalyzer 初始化状态...');
            const isInitialized = await this.checkPromptCatalyzerStatus();
            
            if (!isInitialized) {
                // Not initialized, display initialization dialog box
                this.$message({
                    message: '🚀 首次使用需要初始化 PromptCatalyzer，正在加载可用的 AI Model...',
                    type: 'info',
                    duration: 3000
                });
                
                // Load the list of available Models
                await this.loadAvailableModels();
                
                if (this.availableModelsForInit.length === 0) {
                    this.$message.error('没有找到可用的 AI Model，请先在 AIKernel 模块中配置 Chat 类型的 Model');
                    return;
                }
                
                // Show initialization dialog
                this.promptCatalyzerInitVisible = true;
                return;
            }
            
            // Already initialized, open the optimization dialog box directly
            console.log('PromptCatalyzer 已初始化，打开优化对话框');
            this.optimizeRequirement = '';
            this.optimizeErrorText = '';
            this.optimizeDialogVisible = true;
        },
        /// Synchronize the expected results of the current target lane to aiScoreForm to avoid "post-target AI scoring" relying only on the form memory and missing the expectedResultsJson in the details.
        syncAiScoreFormFromPromptDetail() {
            if (!this.promptDetail || !this.promptDetail.expectedResultsJson) {
                return;
            }
            try {
                const arr = JSON.parse(this.promptDetail.expectedResultsJson);
                if (!Array.isArray(arr) || arr.length === 0) {
                    return;
                }
                const hasValues = arr.some(x => x !== undefined && x !== null && String(x).trim() !== '');
                if (!hasValues) {
                    return;
                }
                const formHasValues = this.aiScoreForm.resultList && this.aiScoreForm.resultList.some(r => r && r.value);
                if (!formHasValues) {
                    this.aiScoreForm.resultList = arr.map((item, index) => ({
                        id: index + 1,
                        label: '预期结果',
                        value: typeof item === 'string' ? item : String(item)
                    }));
                }
            } catch (e) {
                console.warn('syncAiScoreFormFromPromptDetail:', e);
            }
        },
        async executeOptimize() {
            if (this.optimizing) {
                this.$message.warning('优化正在进行中，请勿重复点击');
                return;
            }
            if (!this.promptid) {
                this.$message.warning('请先选择一个Prompt！');
                return;
            }
            
            // Get the currently selected Prompt Code
            let promptCode = '';
            const selectedPrompt = this.promptOpt.find(item => item.value === this.promptid);
            if (selectedPrompt) {
                if (selectedPrompt.fullVersion) {
                    promptCode = selectedPrompt.fullVersion;
                } else if (selectedPrompt.label && selectedPrompt.label.includes('-T')) {
                    promptCode = selectedPrompt.label; 
                }
            }
            
            // If not found, try to get it from details
            if (!promptCode && this.promptDetail && this.promptDetail.fullVersion) {
                promptCode = this.promptDetail.fullVersion;
            }

            if (!promptCode) {
                this.$message.error('无法获取当前Prompt的版本号(Prompt Code)');
                return;
            }

            // Get the details of the current Prompt (including parameters)
            const promptDetail = this.promptDetail;
            if (!promptDetail) {
                this.$message.error('无法获取当前 Prompt 的详细信息');
                return;
            }

            // Seize the space as early as possible to avoid double-clicking after the verification is passed and before sending the request to generate two HTTP requests.
            this.optimizing = true;
            this.optimizeErrorText = '';
            this.optimizeProgressText = 'Agent 正在推理并调用工具（可能需要数分钟，请勿关闭此窗口）…';
            try {
                console.log('开始优化 Prompt（Agent 主路径）:', promptCode);
                
                const requestData = {
                    promptCode: promptCode,
                    promptContent: promptDetail.promptContent || this.content,
                    userRequirement: this.optimizeRequirement || '提高 Prompt 的质量和效果',
                    context: {
                        modelId: this.modelid || promptDetail.modelId,
                        currentTemperature: promptDetail.temperature || this.parameterViewList.find(p => p.formField === 'temperature')?.value || 0.7,
                        currentTopP: promptDetail.topP || this.parameterViewList.find(p => p.formField === 'topP')?.value || 0.9,
                        currentMaxTokens: promptDetail.maxToken || this.parameterViewList.find(p => p.formField === 'maxToken')?.value || 2000,
                        currentFrequencyPenalty: promptDetail.frequencyPenalty || this.parameterViewList.find(p => p.formField === 'frequencyPenalty')?.value || 0,
                        currentPresencePenalty: promptDetail.presencePenalty || this.parameterViewList.find(p => p.formField === 'presencePenalty')?.value || 0,
                        autoShootAfterOptimize: this.autoShootAfterOptimize,
                        autoAIGradeAfterShoot: this.autoAIGradeAfterShoot
                    }
                };

                const response = await servicePR.post(
                    '/api/Senparc.Xncf.AgentsManager/PromptOptimizationAppService/OptimizeAsync',
                    requestData,
                    { timeout: 900000 }
                );

                if (response.data && response.data.success === false) {
                    const topErr = response.data.errorMessage || response.data.message || '请求失败';
                    this.optimizeErrorText = String(topErr);
                    this.$message.error('优化失败：' + topErr);
                    return;
                }

                if (response.data && response.data.success && response.data.data) {
                    const optimizeResult = response.data.data;
                    if (!optimizeResult.success) {
                        const err = optimizeResult.errorMessage || optimizeResult.evaluationReason || '优化未成功';
                        this.optimizeErrorText = String(err);
                        this.$message.error('优化失败：' + err);
                        return;
                    }

                    this.optimizeProgressText = 'Agent 已完成，正在刷新靶道并选中新版本…';
                    await this.getFieldList();
                    // getFieldList only updates the shooting range list; the target drop-down promptOpt must be pulled by getPromptOptData, otherwise the list will still be old and "Prompt Code not matched" will be misjudged.
                    await this.getPromptOptData();

                    const code = optimizeResult.newPromptCode || optimizeResult.NewPromptCode;
                    const newPrompt = this.promptOpt.find(p =>
                        p.fullVersion === code ||
                        p.label === code ||
                        (p.label && code && p.label.indexOf(code) >= 0));
                    if (!newPrompt) {
                        const w = '已创建新版本，但未在列表中匹配到 Prompt Code：' + code;
                        this.optimizeErrorText = w;
                        this.$message.warning(w);
                        this.optimizeDialogVisible = false;
                        return;
                    }

                    this.promptid = newPrompt.value;
                    this.pageChange = false;
                    await this.getPromptetail(this.promptid, true, true);

                    const _fitem = this.promptOpt.find(item => item.value === this.promptid);
                    if (_fitem) {
                        if (_fitem.isDraft) {
                            this.sendBtns = [{ text: '打靶' }, { text: '保存草稿' }];
                            this.sendBtnText = '打靶';
                        } else {
                            this.sendBtns = [{ text: '连发' }, { text: '保存草稿' }];
                            this.sendBtnText = '连发';
                        }
                    }

                    if (this.autoShootAfterOptimize) {
                        this.syncAiScoreFormFromPromptDetail();
                        this.optimizeProgressText = '正在打靶（与手动打靶相同，可在主界面查看输出）…';
                        // A new version has been created for optimization, and the "burst" mode is used during target shooting (no new version will be created)
                        const savedTactics = this.tacticalForm.tactics;
                        this.tacticalForm.tactics = '连发';
                        try {
                            await this.executeTargetShootWithChatMessage(null);
                        } finally {
                            this.tacticalForm.tactics = savedTactics;
                        }
                        await this.$nextTick();
                    }

                    if (this.autoShootAfterOptimize && this.autoAIGradeAfterShoot && this.outputList && this.outputList.length > 0) {
                        const item = this.outputList[0];
                        let filled = item.alResultList && item.alResultList.some(r => r && r.value);
                        if (!filled && this.aiScoreForm.resultList && this.aiScoreForm.resultList.some(r => r && r.value)) {
                            item.alResultList = this.aiScoreForm.resultList.map((x, idx) => ({
                                id: idx + 1,
                                label: '预期结果',
                                value: x.value
                            }));
                            filled = true;
                        }
                        if (!filled && this.promptDetail && this.promptDetail.expectedResultsJson) {
                            try {
                                const arr = JSON.parse(this.promptDetail.expectedResultsJson);
                                if (Array.isArray(arr) && arr.some(x => x !== undefined && x !== null && String(x).trim() !== '')) {
                                    item.alResultList = arr.map((v, idx) => ({
                                        id: idx + 1,
                                        label: '预期结果',
                                        value: typeof v === 'string' ? v : String(v)
                                    }));
                                    filled = true;
                                }
                            } catch (e) {
                                console.warn('autoAIGrade expectedResultsJson:', e);
                            }
                        }
                        if (filled) {
                            this.optimizeProgressText = '正在 AI 评分（与手动 AI 评分相同）…';
                            item.scoreType = '1';
                            this.$set(this.robotScoreLoadingMap, item.id, true);
                            await this.saveManualScore(item, 0);
                        } else {
                            this.$message.warning('未配置预期结果，已跳过 AI 评分');
                        }
                    } else if (this.autoShootAfterOptimize && this.autoAIGradeAfterShoot && (!this.outputList || this.outputList.length === 0)) {
                        this.$message.warning('自动打靶未返回输出记录，已跳过 AI 评分；可在主界面手动打靶后再评分');
                    }

                    let message = `✅ 优化完成\n\n新 Prompt：${code}`;
                    const evalReason = optimizeResult.evaluationReason || optimizeResult.EvaluationReason;
                    if (evalReason) {
                        message += `\n\n${evalReason}`;
                    }
                    this.$message({ message, type: 'success', duration: 10000, showClose: true });

                    this.optimizeProgressText = '';
                    this.optimizeErrorText = '';
                    this.optimizeDialogVisible = false;
                } else {
                    const errorMsg = response.data?.errorMessage || '未返回有效的优化结果';
                    this.optimizeErrorText = String(errorMsg);
                    this.$message.error('优化失败：' + errorMsg);
                }
            } catch (error) {
                console.error('优化失败:', error);
                const resp = error.response?.data;
                let detail = resp?.errorMessage || resp?.message || error.message || String(error);
                if (resp && typeof resp === 'object' && !resp.errorMessage) {
                    try {
                        detail += '\n\n' + JSON.stringify(resp, null, 2);
                    } catch (e) { /* ignore */ }
                }
                this.optimizeErrorText = detail;
                this.$message({
                    message: '❌ 优化失败: ' + (resp?.errorMessage || resp?.message || error.message),
                    type: 'error',
                    duration: 8000,
                    showClose: true
                });
            } finally {
                this.optimizing = false;
                this.optimizeProgressText = '';
            }
        },
        // Calculate the height of the tree (for balanced layout)
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
        
        // @deprecated No longer used: Since the A node extends in the Z axis, the T node spacing has been changed to a fixed value
        // Count the number of nodes in a subtree (for balanced layout)
        countTreeNodes(nodeData) {
            if (!nodeData || typeof nodeData !== 'object') return 0
            
            let count = 0
            Object.keys(nodeData).forEach(key => {
                const node = nodeData[key]
                count++ // current node
                
                const isExpanded = node.expanded !== false
                if (isExpanded && node.children && Object.keys(node.children).length > 0) {
                    count += this.countTreeNodes(node.children)
                }
            })
            
            return count
        },
        
        // **Calculate statistics for all ratings (used for ranking)**
        calculateScoreStatistics() {
            const scores = []
            
            // Iterate through all promptOpt to collect ratings
            if (this.promptOpt && this.promptOpt.length > 0) {
                this.promptOpt.forEach(prompt => {
                    let score = null
                    
                    // Prefer using evalMaxScore
                    if (prompt.evalMaxScore !== undefined && 
                        prompt.evalMaxScore !== null && 
                        prompt.evalMaxScore !== -1 && 
                        prompt.evalMaxScore !== '-1') {
                        score = prompt.evalMaxScore
                    }
                    // If not, use evalAvgScore
                    else if (prompt.evalAvgScore !== undefined && 
                             prompt.evalAvgScore !== null && 
                             prompt.evalAvgScore !== -1 && 
                             prompt.evalAvgScore !== '-1') {
                        score = prompt.evalAvgScore
                    }
                    
                    if (score !== null) {
                        scores.push(score)
                    }
                })
            }
            
            if (scores.length === 0) {
                return null
            }
            
            // Sort (largest to smallest)
            scores.sort((a, b) => b - a)
            
            const result = {
                scores: scores,
                min: Math.min(...scores),
                max: Math.max(...scores),
                count: scores.length,
                // Calculate percentile points (for grading)
                getPercentileRank: (score) => {
                    // Returns the score's ranking percentage among all scores (0-100)
                    const rank = scores.filter(s => s > score).length
                    return ((scores.length - rank) / scores.length) * 100
                }
            }
            
            console.log('📊 评分统计:', {
                总数: result.count,
                最高分: result.max,
                最低分: result.min,
                分数列表: scores.slice(0, 10) // Only show the first 10
            })
            
            return result
        },
        
        // Rendering tree nodes (optimized version: dynamic balanced layout + animation effect)
        renderTreeNodes() {
            if (!this.map3dTreeData) return
            
            // **Calculate rating statistics**
            this.scoreStatistics = this.calculateScoreStatistics()
            
            this.map3dNodes = []
            // Initialize node mapping
            if (!this.map3dNodeMap) {
                this.map3dNodeMap = new Map()
            }
            const raycaster = new THREE.Raycaster()
            const mouse = new THREE.Vector2()
            
            // **New rendering logic: simplified fixed spacing layout**
            // currentX: current X-axis position (global counter, incremented by each node)
            let currentX = 0
            
            const renderNode = (nodeData, parentPosition, depth) => {
                if (!nodeData || typeof nodeData !== 'object') return
                
                const keys = Object.keys(nodeData)
                
                keys.forEach((key, index) => {
                    const node = nodeData[key]
                    const isExpanded = node.expanded !== false // Expand by default
                    
                    // **Important: Skip Aiming type nodes, they will be created when the parent node is processed**
                    if (node.type === 'aiming') {
                        return // Aiming nodes are not handled here
                    }
                    
                    // **New logic: Separate Aiming child nodes and Tactic child nodes**
                    // Any T node may have both A nodes and T child nodes
                    let aimingChildren = {}
                    let tacticChildren = {}
                    
                    if (node.children && Object.keys(node.children).length > 0) {
                        Object.keys(node.children).forEach(childKey => {
                            const child = node.children[childKey]
                            if (child.type === 'aiming') {
                                aimingChildren[childKey] = child
                            } else {
                                tacticChildren[childKey] = child
                            }
                        })
                    }
                    
                    // **New simplified layout: fixed spacing, sequential arrangement**
                    const nodeSpacing = 15  // fixed spacing
                    
                    // Current node position
                    const x = currentX  // X-axis position (global counter)
                    const y = -depth * 20   // Y-axis depth (level down)
                    const z = 0             // T nodes are unified on the Z=0 plane
                    
                    // Increment the X-axis position (reserved for the next sibling node)
                    currentX += nodeSpacing
                    
                    console.log(`📍 创建T节点: ${key}, 位置(${x}, ${y}, ${z}), depth=${depth}`)
                    
                    // Check if there is a currently edited target lane
                    const hasCurrent = node.prompts && node.prompts.some(p => p.isCurrent)
                    
                    // Create geometry (only created for Range and Tactic, Aiming is created in a dedicated place)
                    let geometry, material, glowGeometry, glowMaterial
                    if (node.type === 'range') {
                        // Shooting Range: Block (uses smoother rounded corners)
                        geometry = new THREE.BoxGeometry(5, 5, 5)
                        material = new THREE.MeshStandardMaterial({ 
                            color: hasCurrent ? 0xff6b6b : 0x4ecdc4,
                            metalness: 0.3,
                            roughness: 0.4,
                            emissive: hasCurrent ? 0xff3333 : 0x004444,
                            emissiveIntensity: hasCurrent ? 0.8 : 0.1
                        })
                        
                        // Add glow effect (when currently selected)
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
                        // Tactic: Cylinder (representing path/channel)
                        // CylinderGeometry(radiusTop, radiusBottom, height, radialSegments)
                        geometry = new THREE.CylinderGeometry(2, 2, 4, 32)
                        material = new THREE.MeshStandardMaterial({ 
                            color: hasCurrent ? 0xffd93d : 0x95e1d3,
                            metalness: 0.5,
                            roughness: 0.3,
                            emissive: hasCurrent ? 0xffaa00 : 0x004444,
                            emissiveIntensity: hasCurrent ? 0.9 : 0.1
                        })
                        
                        // Add glow effect (when currently selected)
                        if (hasCurrent) {
                            glowGeometry = new THREE.CylinderGeometry(2.3, 2.3, 4.3, 32)
                            glowMaterial = new THREE.MeshBasicMaterial({
                                color: 0xffd93d,
                                transparent: true,
                                opacity: 0.4,
                                side: THREE.BackSide
                            })
                        }
                    } else {
                        // Shouldn't get here because the Aiming node has already been returned above
                        console.error('意外的节点类型:', node.type, key)
                        return
                    }
                    
                    const mesh = new THREE.Mesh(geometry, material)
                    // The initial position is set to the target position (subsequently used for animation)
                    mesh.position.set(x, y, z)
                    
                    // If it is a cylinder (tactic node), it needs to be rotated 90 degrees to make it stand upright.
                    if (node.type === 'tactic') {
                        mesh.rotation.x = Math.PI / 2  // Rotate 90 degrees around the X axis
                    }
                    
                    mesh.userData = { 
                        node, 
                        key, 
                        depth, 
                        type: node.type,
                        targetPosition: { x, y, z },
                        initialScale: 0.1 // for expansion animation
                    }
                    mesh.castShadow = true
                    mesh.receiveShadow = true
                    
                    // If it is a newly created node, the animation will expand from small to large.
                    mesh.scale.set(0.1, 0.1, 0.1)
                    
                    // Add glow effect
                    let glowMesh = null
                    if (glowGeometry && glowMaterial) {
                        glowMesh = new THREE.Mesh(glowGeometry, glowMaterial)
                        glowMesh.position.set(x, y, z)
                        
                        // If it is a cylinder (tactic node), the lighting effect also needs to be rotated
                        if (node.type === 'tactic') {
                            glowMesh.rotation.x = Math.PI / 2
                        }
                        
                        glowMesh.scale.set(0.1, 0.1, 0.1)
                        this.map3dScene.add(glowMesh)
                    }
                    
                    // Prepare label text (calculated in advance, used for dynamic width)
                    let labelPrefix = ''
                    let labelText = node.name
                    
                    if (node.type === 'tactic') {
                        labelPrefix = 'T'
                        const tacticMatch = key.match(/^T(.+)$/)
                        if (tacticMatch) {
                            labelText = tacticMatch[1]
                        } else {
                            labelText = node.name
                        }
                    } else if (node.type === 'range') {
                        labelText = node.name.length > 15 ? node.name.substring(0, 15) + '...' : node.name
                    }
                    
                    // Create text labels (larger text, dynamic width)
                    const canvas = document.createElement('canvas')
                    const context = canvas.getContext('2d')
                    
                    // Dynamically calculate canvas size based on text content and type (double again)
                    const prefixFontSize = hasCurrent ? 180 : 160  // Increased from 90/80 to 180/160
                    const mainFontSize = hasCurrent ? 280 : 240   // Increased from 140/120 to 280/240
                    const rangeFontSize = hasCurrent ? 220 : 190   // Increased from 110/95 to 220/190
                    
                    // Precomputed text width
                    if (labelPrefix === 'T') {
                        context.font = `bold ${mainFontSize}px 'Arial Black', 'Microsoft YaHei', Arial, sans-serif`
                    } else {
                        context.font = `bold ${rangeFontSize}px 'Microsoft YaHei', 'PingFang SC', Arial, sans-serif`
                    }
                    const textWidth = context.measureText(labelText).width
                    
                    const padding = 50  // increased from 30 to 50
                    const borderRadius = 30  // increased from 20 to 30
                    
                    // Dynamically set the canvas size (when the font is increased, the canvas must also be larger)
                    canvas.width = Math.max(800, textWidth + padding * 4)
                    canvas.height = 512  // Increased from 256 to 512
                    
                    // Draw a rounded rectangle (compatibility processing)
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
                    
                    // Glass effect: translucent white background
                    context.fillStyle = hasCurrent 
                        ? 'rgba(255, 107, 107, 0.25)'  // Currently selected: light red glass
                        : 'rgba(255, 255, 255, 0.15)'  // Ordinary: translucent white glass
                    drawRoundedRect(padding, padding, canvas.width - padding * 2, canvas.height - padding * 2, borderRadius)
                    context.fill()
                    
                    // Glass frame (enhanced glass effect)
                    context.strokeStyle = hasCurrent 
                        ? 'rgba(255, 150, 150, 0.6)'   // Currently selected: red border
                        : 'rgba(255, 255, 255, 0.3)'   // Normal: white border
                    context.lineWidth = 3
                    drawRoundedRect(padding, padding, canvas.width - padding * 2, canvas.height - padding * 2, borderRadius)
                    context.stroke()
                    
                    // Add highlight effect (simulate glass reflection)
                    const highlightGradient = context.createLinearGradient(0, padding, 0, canvas.height / 2)
                    highlightGradient.addColorStop(0, 'rgba(255, 255, 255, 0.4)')
                    highlightGradient.addColorStop(1, 'rgba(255, 255, 255, 0)')
                    context.fillStyle = highlightGradient
                    drawRoundedRect(padding, padding, canvas.width - padding * 2, (canvas.height - padding * 2) / 2, borderRadius)
                    context.fill()
                    
                    // Draw text (with shading to enhance readability, larger font size)
                    context.textAlign = 'center'
                    context.textBaseline = 'middle'
                    context.shadowColor = 'rgba(0, 0, 0, 0.8)'
                    context.shadowBlur = 20  // increased from 10 to 20
                    context.shadowOffsetX = 5  // Increased from 3 to 5
                    context.shadowOffsetY = 5  // Increased from 3 to 5
                    
                    let mainTextY = canvas.height / 2
                    
                    // If there is a type prefix (T), T and the number are displayed on the same line
                    if (labelPrefix === 'T') {
                        // Combined text: T + number
                        const combinedText = `T${labelText}`
                        
                        // Use uniform large font display
                        context.fillStyle = '#ffffff'
                        context.font = `bold ${mainFontSize}px 'Arial Black', 'Microsoft YaHei', Arial, sans-serif`
                        context.fillText(combinedText, canvas.width / 2, canvas.height / 2)
                        
                        mainTextY = canvas.height / 2
                    } else {
                        // Range name (larger font)
                        context.fillStyle = '#ffffff'
                        context.font = `bold ${rangeFontSize}px 'Microsoft YaHei', 'PingFang SC', Arial, sans-serif`
                        context.fillText(labelText, canvas.width / 2, canvas.height / 2)
                        
                        mainTextY = canvas.height / 2
                    }
                    
                    // If there is a prompt message, it will be displayed below
                    if (node.prompts && node.prompts.length > 0) {
                        context.font = `bold ${hasCurrent ? 110 : 96}px Arial`  // Increased from 55/48 to 110/96
                        context.fillStyle = 'rgba(255, 255, 255, 0.85)'
                        context.shadowBlur = 12
                        const promptText = `${node.prompts.length} 个结果`
                        context.fillText(promptText, canvas.width / 2, mainTextY + 120)  // Changed from +70 to +120
                    }
                    
                    const texture = new THREE.CanvasTexture(canvas)
                    texture.needsUpdate = true
                    const spriteMaterial = new THREE.SpriteMaterial({ 
                        map: texture, 
                        transparent: true,
                        alphaTest: 0.1,
                        opacity: 0 // Initial transparency, used for fade-in animations
                    })
                    const sprite = new THREE.Sprite(spriteMaterial)
                    // **Horizontal layout: The label is below the node (in the negative direction of the Y axis), and the Z axis is slightly forward to avoid being blocked**
                    const labelZOffset = 3  // The Z-axis is offset forward to avoid being blocked by nodes.
                    sprite.position.set(x, y - (node.type === 'range' ? 5 : 4), z + labelZOffset)
                    // Dynamically set the sprite size (according to the canvas width, the label will be larger when the font is increased)
                    const spriteWidth = (canvas.width / 1024) * 18  // Increased from 12 to 18
                    const spriteHeight = (canvas.height / 256) * 4.5  // Increased from 3 to 4.5
                    sprite.scale.set(spriteWidth, spriteHeight, 1)
                    sprite.userData = { 
                        node, 
                        key, 
                        depth, 
                        type: node.type,
                        targetOpacity: 1
                    }
                    
                    this.map3dScene.add(mesh)
                    this.map3dScene.add(sprite)
                    
                    // **Key fix: Use Object.freeze to prevent Vue reactive listening from causing stack overflow**
                    // Three.js objects should not be listened to by Vue, otherwise it will cause performance issues and circular references
                    
                    // Freeze Three.js related objects to prevent Vue from adding responsive getters/setters
                    Object.freeze(mesh)
                    Object.freeze(sprite)
                    if (glowMesh) Object.freeze(glowMesh)
                    
                    const createdNodeData = {
                        mesh, 
                        sprite, 
                        glowMesh,
                        node, 
                        key, 
                        depth, 
                        isExpanded, 
                        position: { x, y, z }, 
                        parentPosition: parentPosition,
                        childrenMeshes: [],  // This array may be large and does not require responsiveness
                        line: null, 
                        dot: null,
                        animationProgress: 0,
                        hasCurrent: hasCurrent
                    }
                    
                    this.map3dNodes.push(createdNodeData)
                    
                    // Store node references for quick lookup
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
                    
                    // Do not add connecting lines first and process them later (avoid creating lines before position adjustment)
                    let line = null
                    let dot = null
                    
                    // **1. Process the Aiming child node first (extend on the Z axis, facing the camera)**
                    if (isExpanded && Object.keys(aimingChildren).length > 0) {
                        // Aiming node: Z-axis arrangement (extending towards the camera direction)
                        // Save the reference to the current node first, and update Aiming's parentPosition later.
                        const currentParentNodeData = this.map3dNodes[this.map3dNodes.length - 1]
                        
                        const aimingKeys = Object.keys(aimingChildren)
                        const aimingSpacing = 16 // Aiming Z-axis spacing between nodes (increase by 1x: 8 -> 16)
                        
                        // **New strategy: A nodes start from the position of the parent T node and are arranged in sequence toward the camera direction (+Z)**
                        // A1 is at the position closest to T, A2 is in front of A1, A3 is in front of A2...
                        // No more use of centrosymmetry or hash offsets
                        
                        const aimingNodesData = [] // Save Aiming node data and update parentPosition later
                        
                        aimingKeys.forEach((aimingKey, aimingIndex) => {
                            const aimingNode = aimingChildren[aimingKey]
                            // **New layout: A nodes are arranged sequentially on the Z axis toward the camera**
                            // X and Y remain the same as the parent T node, only incrementing on the Z axis
                            const aimingX = x // Same horizontal position as parent node
                            const aimingY = y // Same depth as parent node (not one level lower)
                            // Z axis: Starting from the T node position, each A node increases aimingSpacing
                            // A1: z + aimingSpacing, A2: z + 2*aimingSpacing, ...
                            const aimingZ = z + (aimingIndex + 1) * aimingSpacing
                                
                            console.log('🎯 创建Aiming节点:', {
                                key: aimingKey,
                                parentKey: key,
                                parentPos: { x, y, z },
                                aimingPos: { x: aimingX, y: aimingY, z: aimingZ },
                                aimingIndex,
                                totalAiming: aimingKeys.length,
                                strategy: `A${aimingIndex + 1} 在 T 前方 ${(aimingIndex + 1) * aimingSpacing} 单位（朝向摄像机）`
                            })
                                
                                // Check if there is a currently edited target lane
                                const aimingHasCurrent = aimingNode.prompts && aimingNode.prompts.some(p => p.isCurrent)
                                
                                // **Change size and color based on rating**
                                let sphereSize = 1.5  // default size
                                let sphereColor = 0xa8e6cf  // Default color (light green)
                                let emissiveColor = 0x003333
                                let emissiveIntensity = 0.05
                                let score = null  // **Promoted to the outer scope for subsequent use of glowing effects**
                                
                                // If there is rating data, adjust it based on the rating
                                if (aimingNode.prompts && aimingNode.prompts.length > 0) {
                                    const promptId = aimingNode.prompts[0].id
                                    const fullPromptData = this.promptOpt.find(p => 
                                        (p.idkey || p.value) == promptId
                                    )
                                    
                                    // **Debug: View data structure**
                                    console.log('🔍 Aiming节点评分数据检查:', {
                                        promptId,
                                        fullPromptData,
                                        evalMaxScore: fullPromptData?.evalMaxScore,
                                        finalScore: fullPromptData?.finalScore
                                    })
                                    
                                    // **Prefer using evalMaxScore (highest score) as the score**
                                    // score has been defined in the outer layer and can be assigned directly.
                                    if (fullPromptData) {
                                        // Try finalScore first
                                        if (fullPromptData.finalScore !== undefined && 
                                            fullPromptData.finalScore !== null && 
                                            fullPromptData.finalScore !== -1 && 
                                            fullPromptData.finalScore !== '-1') {
                                            score = fullPromptData.finalScore
                                        }
                                        // If there is no finalScore, use evalMaxScore
                                        else if (fullPromptData.evalMaxScore !== undefined && 
                                                 fullPromptData.evalMaxScore !== null && 
                                                 fullPromptData.evalMaxScore !== -1 && 
                                                 fullPromptData.evalMaxScore !== '-1') {
                                            score = fullPromptData.evalMaxScore
                                        }
                                        // If evalMaxScore is not available, use evalAvgScore
                                        else if (fullPromptData.evalAvgScore !== undefined && 
                                                 fullPromptData.evalAvgScore !== null && 
                                                 fullPromptData.evalAvgScore !== -1 && 
                                                 fullPromptData.evalAvgScore !== '-1') {
                                            score = fullPromptData.evalAvgScore
                                        }
                                    }
                                    
                                    if (score !== null && this.scoreStatistics) {
                                        // **Set size and color based on relative ranking**
                                        const percentile = this.scoreStatistics.getPercentileRank(score)
                                        const allScores = this.scoreStatistics.scores
                                        const rank = allScores.filter(s => s > score).length + 1
                                        const isFirst = rank === 1
                                        
                                        console.log('✅ 使用评分:', score, '排名:', rank, '百分位:', percentile.toFixed(1) + '%', isFirst ? '🥇第一名!' : '')
                                        
                                        // **Special treatment: Node ranked first**
                                        if (isFirst) {
                                            // 1st Place - Extra Large, Purple (Different from Yellow Highlight)
                                            sphereSize = 2.5
                                            sphereColor = 0xb24df5  // Purple (obviously different from yellow)
                                            emissiveColor = 0xff00ff  // Purple red glow
                                            emissiveIntensity = 0.6
                                        }
                                        // Ranked according to ranking percentile (Top 20%, 20-40%, 40-60%, 60-80%, Bottom 20%)
                                        else if (percentile >= 80) {
                                            // Top 20% - Largest, bright green
                                            sphereSize = 2.2
                                            sphereColor = 0x00d4aa
                                            emissiveColor = 0x00ffcc
                                            emissiveIntensity = 0.4
                                        } else if (percentile >= 60) {
                                            // 20-40% - Larger, greener
                                            sphereSize = 1.9
                                            sphereColor = 0x52c41a
                                            emissiveColor = 0x66ff66
                                            emissiveIntensity = 0.3
                                        } else if (percentile >= 40) {
                                            // 40-60% - Normal, Orange
                                            sphereSize = 1.6
                                            sphereColor = 0xfaad14
                                            emissiveColor = 0xffcc00
                                            emissiveIntensity = 0.2
                                        } else if (percentile >= 20) {
                                            // 60-80% - smaller, light red
                                            sphereSize = 1.3
                                            sphereColor = 0xff7875
                                            emissiveColor = 0xff6666
                                            emissiveIntensity = 0.15
                                        } else {
                                            // Bottom 20% - smallest, red
                                            sphereSize = 1.0
                                            sphereColor = 0xf5222d
                                            emissiveColor = 0xff3333
                                            emissiveIntensity = 0.1
                                        }
                                    }
                                }
                                
                                // If it is the currently selected node, it will be highlighted in yellow.
                                if (aimingHasCurrent) {
                                    sphereColor = 0xffd93d
                                    emissiveColor = 0xffaa00
                                    emissiveIntensity = 0.7
                                }
                                
                                // Create aiming geometry: small sphere (size determined by score)
                                const aimingGeometry = new THREE.SphereGeometry(sphereSize, 24, 24)
                                const aimingMaterial = new THREE.MeshStandardMaterial({ 
                                    color: sphereColor,
                                    metalness: 0.4,
                                    roughness: 0.5,
                                    emissive: emissiveColor,
                                    emissiveIntensity: emissiveIntensity
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
                                
                                // **Add special glow effect for first place**
                                let aimingGlowMesh = null
                                if (score !== null && this.scoreStatistics) {
                                    const allScores = this.scoreStatistics.scores
                                    const rank = allScores.filter(s => s > score).length + 1
                                    if (rank === 1) {
                                        // Create purple glowing shell (distinguishable from yellow highlight)
                                        const aimingGlowGeometry = new THREE.SphereGeometry(sphereSize * 1.3, 24, 24)
                                        const aimingGlowMaterial = new THREE.MeshBasicMaterial({
                                            color: 0xb24df5,  // Purple
                                            transparent: true,
                                            opacity: 0.35,
                                            side: THREE.BackSide
                                        })
                                        aimingGlowMesh = new THREE.Mesh(aimingGlowGeometry, aimingGlowMaterial)
                                        aimingGlowMesh.position.copy(aimingMesh.position)
                                        aimingGlowMesh.scale.set(0.1, 0.1, 0.1)
                                        this.map3dScene.add(aimingGlowMesh)
                                    }
                                }
                                
                                // Create Aiming text labels (larger text, dynamic width)
                                const aimingCanvas = document.createElement('canvas')
                                const aimingContext = aimingCanvas.getContext('2d')
                                
                                // Extract Aiming numbers (ahead of time, used to calculate width)
                                const aimingMatch = aimingKey.match(/-A(\d+)$/)
                                const aimingLabelText = aimingMatch ? aimingMatch[1] : aimingNode.name
                                
                                // Dynamically calculate canvas size based on text content (double again)
                                const aimingPrefixFontSize = aimingHasCurrent ? 180 : 160  // Increased from 90/80 to 180/160
                                const aimingMainFontSize = aimingHasCurrent ? 280 : 240   // Increased from 140/120 to 280/240
                                
                                // Combined text (for width calculation)
                                const aimingCombinedText = `A${aimingLabelText}`
                                
                                // Precompute text width (using combined text)
                                aimingContext.font = `bold ${aimingMainFontSize}px 'Arial Black', 'Microsoft YaHei', Arial, sans-serif`
                                const textWidth = aimingContext.measureText(aimingCombinedText).width
                                
                                const aimingPadding = 50  // increased from 30 to 50
                                const aimingBorderRadius = 30  // increased from 20 to 30
                                
                                // Dynamically set the canvas size (according to the text content, the canvas will be larger when the font is increased)
                                aimingCanvas.width = Math.max(800, textWidth + aimingPadding * 4)
                                aimingCanvas.height = 512  // Increased from 256 to 512
                                
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
                                
                                // glass effect background
                                aimingContext.fillStyle = aimingHasCurrent 
                                    ? 'rgba(255, 107, 107, 0.25)' 
                                    : 'rgba(255, 255, 255, 0.15)'
                                drawRoundedRect(aimingPadding, aimingPadding, aimingCanvas.width - aimingPadding * 2, aimingCanvas.height - aimingPadding * 2, aimingBorderRadius)
                                aimingContext.fill()
                                
                                // glass frame
                                aimingContext.strokeStyle = aimingHasCurrent 
                                    ? 'rgba(255, 150, 150, 0.6)' 
                                    : 'rgba(255, 255, 255, 0.3)'
                                aimingContext.lineWidth = 3
                                drawRoundedRect(aimingPadding, aimingPadding, aimingCanvas.width - aimingPadding * 2, aimingCanvas.height - aimingPadding * 2, aimingBorderRadius)
                                aimingContext.stroke()
                                
                                // Highlight effect
                                const aimingHighlightGradient = aimingContext.createLinearGradient(0, aimingPadding, 0, aimingCanvas.height / 2)
                                aimingHighlightGradient.addColorStop(0, 'rgba(255, 255, 255, 0.4)')
                                aimingHighlightGradient.addColorStop(1, 'rgba(255, 255, 255, 0)')
                                aimingContext.fillStyle = aimingHighlightGradient
                                drawRoundedRect(aimingPadding, aimingPadding, aimingCanvas.width - aimingPadding * 2, (aimingCanvas.height - aimingPadding * 2) / 2, aimingBorderRadius)
                                aimingContext.fill()
                                
                                // Draw text (A and number are on the same line, aimingCombinedText is defined above)
                                aimingContext.textAlign = 'center'
                                aimingContext.textBaseline = 'middle'
                                aimingContext.shadowColor = 'rgba(0, 0, 0, 0.8)'
                                aimingContext.shadowBlur = 20
                                aimingContext.shadowOffsetX = 5
                                aimingContext.shadowOffsetY = 5
                                
                                // Display using a uniform large font (combined text has been defined previously)
                                aimingContext.fillStyle = '#ffffff'
                                aimingContext.font = `bold ${aimingMainFontSize}px 'Arial Black', 'Microsoft YaHei', Arial, sans-serif`
                                aimingContext.fillText(aimingCombinedText, aimingCanvas.width / 2, aimingCanvas.height / 2)
                                
                                // Create a sprite
                                const aimingTexture = new THREE.CanvasTexture(aimingCanvas)
                                const aimingSpriteMaterial = new THREE.SpriteMaterial({ 
                                    map: aimingTexture, 
                                    transparent: true,
                                    alphaTest: 0.1,
                                    opacity: 0
                                })
                                const aimingSprite = new THREE.Sprite(aimingSpriteMaterial)
                                // **Horizontal layout: label is below the node, Z-axis is slightly forward to avoid being obscured by the ball**
                                const aimingLabelZOffset = 3  // Z axis offset forward
                                aimingSprite.position.set(aimingX, aimingY - 4, aimingZ + aimingLabelZOffset)
                                // Dynamically set the sprite size (according to the canvas width, the label will be larger when the font is increased)
                                const spriteWidth = (aimingCanvas.width / 1024) * 18  // Increased from 12 to 18
                                const spriteHeight = (aimingCanvas.height / 256) * 4.5  // Increased from 3 to 4.5
                                aimingSprite.scale.set(spriteWidth, spriteHeight, 1)
                                aimingSprite.userData = { 
                                    node: aimingNode, 
                                    key: aimingKey, 
                                    depth: depth + 1, 
                                    type: aimingNode.type,
                                    targetOpacity: 1
                                }
                                
                                this.map3dScene.add(aimingMesh)
                                this.map3dScene.add(aimingSprite)
                                
                                // Freeze Three.js objects to prevent Vue responsive listening from causing stack overflow
                                Object.freeze(aimingMesh)
                                Object.freeze(aimingSprite)
                                if (aimingGlowMesh) Object.freeze(aimingGlowMesh)
                                
                                const aimingNodeData = { 
                                    mesh: aimingMesh, 
                                    sprite: aimingSprite, 
                                    glowMesh: aimingGlowMesh,
                                    node: aimingNode, 
                                    key: aimingKey, 
                                    depth: depth + 1, 
                                    isExpanded: true, 
                                    position: { x: aimingX, y: aimingY, z: aimingZ }, 
                                    parentPosition: { x, y, z },
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
                            
                            // Update the child node reference recorded in the current node data
                            if (currentParentNodeData) {
                                aimingKeys.forEach(aimingKey => {
                                    const aimingChild = aimingChildren[aimingKey]
                                    const aimingChildData = this.map3dNodes.find(n => n.node === aimingChild)
                                    if (aimingChildData) {
                                        currentParentNodeData.childrenMeshes.push(aimingChildData.mesh)
                                        currentParentNodeData.childrenMeshes.push(aimingChildData.sprite)
                                        if (aimingChildData.glowMesh) {
                                            currentParentNodeData.childrenMeshes.push(aimingChildData.glowMesh)
                                        }
                                    }
                                })
                            }
                    }
                    
                    // **2. Process the Tactic child node again (continue downward on the Y axis and call recursively)**
                    if (isExpanded && Object.keys(tacticChildren).length > 0) {
                        // Simple recursive rendering of child nodes, currentX will automatically accumulate
                        renderNode(tacticChildren, { x, y, z }, depth + 1)
                        
                        // Record child node references (for quick visibility updates)
                        const currentNodeData = this.map3dNodes.find(n => n.key === key && n.depth === depth)
                        if (currentNodeData) {
                            Object.keys(tacticChildren).forEach(childKey => {
                                const childNode = tacticChildren[childKey]
                                const childNodeData = this.map3dNodes.find(n => n.node === childNode)
                                if (childNodeData) {
                                    currentNodeData.childrenMeshes.push(childNodeData.mesh)
                                    currentNodeData.childrenMeshes.push(childNodeData.sprite)
                                    if (childNodeData.glowMesh) {
                                        currentNodeData.childrenMeshes.push(childNodeData.glowMesh)
                                    }
                                }
                            })
                        }
                    }
                })
            }
            
            // **Render all root nodes directly**
            currentX = 0  // Reset the X-axis starting point
            Object.keys(this.map3dTreeData).forEach(key => {
                renderNode({ [key]: this.map3dTreeData[key] }, null, 0)
            })
            
            // **Special handling: Move the Range node to the horizontal midpoint of its child node**
            const rangeNodes = this.map3dNodes.filter(n => n.node.type === 'range')
            rangeNodes.forEach(rangeNodeData => {
                // Find all child T nodes of this Range
                const childTacticNodes = this.map3dNodes.filter(n => 
                    n.depth === 1 && n.parentPosition && 
                    Math.abs(n.parentPosition.y - rangeNodeData.position.y) < 1
                )
                
                if (childTacticNodes.length > 0) {
                    // Calculate the X-axis range of child nodes
                    const childXPositions = childTacticNodes.map(n => n.position.x)
                    const minX = Math.min(...childXPositions)
                    const maxX = Math.max(...childXPositions)
                    const centerX = (minX + maxX) / 2
                    
                    // Move the Range node to the midpoint
                    rangeNodeData.mesh.position.x = centerX
                    if (rangeNodeData.glowMesh) {
                        rangeNodeData.glowMesh.position.x = centerX
                    }
                    rangeNodeData.sprite.position.x = centerX
                    rangeNodeData.position.x = centerX
                    
                    console.log(`📦 调整Range节点 ${rangeNodeData.key} 到中点: ${centerX} (子节点范围: ${minX} ~ ${maxX})`)
                    
                    // Update the parentPosition of all child nodes of the Range at the same time
                    childTacticNodes.forEach(childData => {
                        childData.parentPosition = {
                            x: centerX,
                            y: rangeNodeData.position.y,
                            z: rangeNodeData.position.z
                        }
                    })
                }
            })
            
            // **Calculate the actual extent of the tree**
            let maxDepth = 0
            let minX = Infinity
            let maxX = -Infinity
            let minZ = 0
            let maxZ = 0
            
            this.map3dNodes.forEach(nodeData => {
                maxDepth = Math.max(maxDepth, nodeData.depth)
                
                // Calculate the X-axis range (only T nodes are calculated, excluding A nodes)
                if (nodeData.node.type !== 'aiming') {
                    minX = Math.min(minX, nodeData.position.x)
                    maxX = Math.max(maxX, nodeData.position.x)
                }
                
                // Calculate the Z-axis range (only calculate the A node)
                if (nodeData.node.type === 'aiming') {
                    minZ = Math.min(minZ, nodeData.position.z)
                    maxZ = Math.max(maxZ, nodeData.position.z)
                }
            })
            
            const treeWidth = maxX - minX  // Horizontal width (X-axis)
            const treeDepth = maxDepth * 20  // Depth (Y-axis)
            const treeZSpan = maxZ - minZ  // Z-axis span (A node)
            
            const treeCenterX = (maxX + minX) / 2
            const treeCenterY = -treeDepth / 2  // Expand downward, the center point is at negative Y
            const treeCenterZ = (maxZ + minZ) / 2  // Z axis center (center of Aiming node)
            
            console.log('📐 树的范围计算:', {
                treeWidth: treeWidth.toFixed(2),
                treeDepth: treeDepth.toFixed(2),
                treeZSpan: treeZSpan.toFixed(2),
                xRange: `${minX.toFixed(2)} ~ ${maxX.toFixed(2)}`,
                yRange: `0 ~ ${(-maxDepth * 20).toFixed(2)}`,
                zRange: `${minZ.toFixed(2)} ~ ${maxZ.toFixed(2)}`
            })
            
            // Adjust the initial position of the camera to ensure that the entire tree can be seen
            const treeCenter = {
                x: treeCenterX,
                y: treeCenterY,
                z: treeCenterZ
            }
            
            // **Optimize camera distance calculation: use smarter algorithm**
            // Consider 3 dimensions but give different weights
            const effectiveWidth = treeWidth
            const effectiveDepth = treeDepth
            const effectiveZSpan = treeZSpan
            
            // Use the bounding box diagonal length as a baseline
            const boundingBoxDiagonal = Math.sqrt(
                effectiveWidth * effectiveWidth + 
                effectiveDepth * effectiveDepth + 
                effectiveZSpan * effectiveZSpan
            )
            
            // Adjust distance coefficient based on node density
            const nodeCount = this.map3dNodes.length
            let distanceMultiplier = 1.2  // Basic coefficient
            
            // The more nodes there are, the distance coefficient decreases slightly (to avoid pulling too far)
            if (nodeCount > 100) {
                distanceMultiplier = 1.0
            } else if (nodeCount > 50) {
                distanceMultiplier = 1.1
            } else if (nodeCount < 20) {
                distanceMultiplier = 1.3
            }
            
            const cameraDistance = boundingBoxDiagonal * distanceMultiplier
            
            console.log('📷 相机距离计算:', {
                treeWidth,
                treeDepth,
                treeZSpan,
                boundingBoxDiagonal: boundingBoxDiagonal.toFixed(2),
                nodeCount,
                distanceMultiplier,
                finalDistance: cameraDistance.toFixed(2)
            })
            
            // After the positions of all nodes are determined, connect lines are created uniformly
            this.createConnectionLines()
            
            if (this.map3dControls) {
                this.map3dControls.target.set(treeCenter.x, treeCenter.y, treeCenter.z)
                
                // **Optimized camera position: viewed from the top right front**
                // This way you can see clearly at the same time:
                // - Arrangement of T nodes on the X axis
                // - Depth level of Y axis
                // - A node distribution on Z axis
                this.map3dCamera.position.set(
                    treeCenter.x + cameraDistance * 0.4,   // X-axis: slightly to the right
                    treeCenter.y + cameraDistance * 0.5,   // Y-axis: Viewed from above (higher)
                    treeCenter.z + cameraDistance * 0.8    // Z axis: Viewed from the front
                )
                this.map3dControls.update()
                
                console.log('📷 相机位置已设置:', {
                    target: treeCenter,
                    position: this.map3dCamera.position
                })
            }
            
            // Start entry animation
            this.startNodeAnimations()
            
            // Add click event (simplified version: inhale/pop effect)
            let isAnimating = false // Whether the marker is performing animation
            
            const onMouseClick = (event) => {
                // Ignore clicks if animation is in progress
                if (isAnimating) {
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
                    
                    // Only process nodes with child nodes
                    if (nodeData && nodeData.node.children && Object.keys(nodeData.node.children).length > 0) {
                        // Toggle expanded/collapsed state
                        const wasExpanded = nodeData.node.expanded !== false
                        nodeData.node.expanded = !wasExpanded
                        
                        // Set animated markers
                        isAnimating = true
                        
                        // Execute animation
                        if (nodeData.node.expanded) {
                            // Expand: Pop-up animation
                            this.animateNodesPopOut(nodeData, () => {
                                isAnimating = false
                            })
                        } else {
                            // Collapse: Inhale animation
                            this.animateNodesSuckIn(nodeData, () => {
                                isAnimating = false
                            })
                        }
                    }
                }
            }
            
            this.map3dRenderer.domElement.addEventListener('click', onMouseClick)
            this.map3dClickHandler = onMouseClick
            
            // **Add double-click event: select the target channel corresponding to the Aiming node**
            const onMouseDoubleClick = (event) => {
                const rect = this.map3dRenderer.domElement.getBoundingClientRect()
                mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1
                mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1
                
                raycaster.setFromCamera(mouse, this.map3dCamera)
                
                // Detect mesh and sprite
                const detectableObjects = []
                this.map3dNodes.forEach(n => {
                    if (n.mesh && n.mesh.visible) {
                        detectableObjects.push(n.mesh)
                    }
                    if (n.sprite && n.sprite.visible) {
                        detectableObjects.push(n.sprite)
                    }
                })
                
                const intersects = raycaster.intersectObjects(detectableObjects, false)
                
                if (intersects.length > 0) {
                    const intersectedObject = intersects[0].object
                    const nodeData = this.map3dNodes.find(n => 
                        n.mesh === intersectedObject || n.sprite === intersectedObject
                    )
                    
                    // Only handle Aiming nodes
                    if (nodeData && nodeData.node.type === 'aiming') {
                        // Get the promptId corresponding to the Aiming node
                        if (nodeData.node.prompts && nodeData.node.prompts.length > 0) {
                            const promptId = nodeData.node.prompts[0].id
                            
                            console.log('🎯 双击Aiming节点，选中靶道:', {
                                nodeKey: nodeData.key,
                                promptId: promptId,
                                fullPath: nodeData.node.fullPath
                            })
                            
                            // **Close the 3D map floating window (close it first to avoid conflict with the confirmation dialog box)**
                            this.mapDialogVisible = false
                            
                            // **Reuse the original target lane switching logic (including save draft prompts)**
                            // Note: Setting v-model programmatically will not trigger the @change event
                            // You need to manually call the promptChangeHandel function to trigger the complete logic
                            // This function automatically handles:
                            // 1. Check pageChange status
                            // 2. If there are any modifications, a confirmation box "Whether to save as a draft" will pop up.
                            // 3. Save or not save according to user choice
                            // 4. Call getPromptetail to obtain target channel details
                            
                            // Save old value (for change detection)
                            const oldPromptId = this.promptid
                            
                            // Set new value
                            this.promptid = promptId
                            
                            // Manually trigger change processing logic
                            this.promptChangeHandel(promptId, 'promptid', oldPromptId)
                        }
                    }
                }
            }
            
            this.map3dRenderer.domElement.addEventListener('dblclick', onMouseDoubleClick)
            this.map3dDoubleClickHandler = onMouseDoubleClick
            
            // **Add mouse movement event: display node information tooltip + edge highlight**
            let hoveredNode = null  // currently hovering node
            let currentHighlightMesh = null  // Current High Gloss Shell
            
            // Create tooltip element
            const createTooltip = () => {
                let tooltip = document.getElementById('map3d-tooltip')
                if (!tooltip) {
                    tooltip = document.createElement('div')
                    tooltip.id = 'map3d-tooltip'
                    tooltip.style.cssText = `
                        position: fixed;
                        background: rgba(0, 0, 0, 0.85);
                        color: white;
                        padding: 12px 16px;
                        border-radius: 8px;
                        font-size: 14px;
                        pointer-events: none;
                        z-index: 10000;
                        display: none;
                        max-width: 300px;
                        box-shadow: 0 4px 12px rgba(0,0,0,0.3);
                        border: 1px solid rgba(255,255,255,0.2);
                        line-height: 1.6;
                    `
                    document.body.appendChild(tooltip)
                }
                return tooltip
            }
            
            const tooltip = createTooltip()
            
            const onMouseMove = (event) => {
                const rect = this.map3dRenderer.domElement.getBoundingClientRect()
                mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1
                mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1
                
                raycaster.setFromCamera(mouse, this.map3dCamera)
                
                // **Detect mesh and sprite (text labels) simultaneously**
                const detectableObjects = []
                this.map3dNodes.forEach(n => {
                    if (n.mesh && n.mesh.visible) {
                        detectableObjects.push(n.mesh)
                    }
                    if (n.sprite && n.sprite.visible) {
                        detectableObjects.push(n.sprite)
                    }
                })
                
                const intersects = raycaster.intersectObjects(detectableObjects, false)
                
                if (intersects.length > 0) {
                    const intersectedObject = intersects[0].object
                    // Find the corresponding nodeData based on the intersecting objects (mesh or sprite)
                    const nodeData = this.map3dNodes.find(n => 
                        n.mesh === intersectedObject || n.sprite === intersectedObject
                    )
                    
                    if (nodeData) {
                        // If it is a new node, update tooltip and highlight
                        if (hoveredNode !== nodeData) {
                            // **Remove previous highlights**
                            if (currentHighlightMesh) {
                                this.map3dScene.remove(currentHighlightMesh)
                                if (currentHighlightMesh.geometry) currentHighlightMesh.geometry.dispose()
                                if (currentHighlightMesh.material) currentHighlightMesh.material.dispose()
                                currentHighlightMesh = null
                            }
                            
                            hoveredNode = nodeData
                            
                            // **Add edge highlight effect**
                            if (nodeData.mesh && nodeData.mesh.visible) {
                                let highlightGeometry, highlightSize
                                
                                // Create differently shaped highlights based on node type
                                if (nodeData.node.type === 'range') {
                                    // Squares: Use slightly larger squares for edges
                                    highlightSize = 5.3  // The original size is 5
                                    highlightGeometry = new THREE.BoxGeometry(highlightSize, highlightSize, highlightSize)
                                } else {
                                    // Sphere: Use a slightly larger sphere as the edge
                                    const originalScale = nodeData.mesh.scale.x
                                    const originalGeometry = nodeData.mesh.geometry
                                    if (originalGeometry && originalGeometry.parameters && originalGeometry.parameters.radius) {
                                        highlightSize = originalGeometry.parameters.radius * 1.15 * originalScale  // Magnify 15%
                                    } else {
                                        highlightSize = 2.5 * 1.15 * originalScale  // default size
                                    }
                                    highlightGeometry = new THREE.SphereGeometry(highlightSize, 32, 32)
                                }
                                
                                // Create a specular material (edge ​​glow effect)
                                const highlightMaterial = new THREE.MeshBasicMaterial({
                                    color: 0xffffff,  // White
                                    transparent: true,
                                    opacity: 0.3,
                                    side: THREE.BackSide,  // Only the back side is shown, creating an edge effect
                                    depthWrite: false
                                })
                                
                                currentHighlightMesh = new THREE.Mesh(highlightGeometry, highlightMaterial)
                                currentHighlightMesh.position.copy(nodeData.mesh.position)
                                currentHighlightMesh.scale.copy(nodeData.mesh.scale)
                                this.map3dScene.add(currentHighlightMesh)
                            }
                            
                            // Build tooltip content
                            let tooltipContent = ''
                            const node = nodeData.node
                            
                            if (node.type === 'range') {
                                // Range information
                                tooltipContent = `
                                    <div style="font-weight: bold; margin-bottom: 8px; color: #4ecdc4;">📦 靶场</div>
                                    <div>${node.name}</div>
                                    ${node.prompts && node.prompts.length > 0 ? `<div style="margin-top: 4px; color: #aaa;">包含 ${node.prompts.length} 个结果</div>` : ''}
                                `
                            } else if (node.type === 'tactic') {
                                // Tactic information
                                const tacticNumber = nodeData.key.replace('T', '')
                                tooltipContent = `
                                    <div style="font-weight: bold; margin-bottom: 8px; color: #95e1d3;">🎯 战术节点</div>
                                    <div>编号: T${tacticNumber}</div>
                                    ${node.prompts && node.prompts.length > 0 ? `<div style="margin-top: 4px; color: #aaa;">${node.prompts.length} 个结果</div>` : ''}
                                `
                            } else if (node.type === 'aiming') {
                                // Aiming information (including ratings)
                                const aimingNumber = nodeData.key.match(/-A(\d+)$/)?.[1] || nodeData.key
                                tooltipContent = `
                                    <div style="font-weight: bold; margin-bottom: 8px; color: #a8e6cf;">🎲 瞄准点</div>
                                    <div>编号: A${aimingNumber}</div>
                                `
                                
                                // **Get complete rating data from promptOpt**
                                if (node.prompts && node.prompts.length > 0) {
                                    const promptId = node.prompts[0].id
                                    // Find complete data from promptOpt
                                    const fullPromptData = this.promptOpt.find(p => 
                                        (p.idkey || p.value) == promptId
                                    )
                                    
                                    console.log('🔍 Tooltip评分数据检查:', {
                                        promptId,
                                        fullPromptData,
                                        evalMaxScore: fullPromptData?.evalMaxScore,
                                        evalAvgScore: fullPromptData?.evalAvgScore
                                    })
                                    
                                    if (fullPromptData) {
                                        // **Get final score: use evalMaxScore (highest score) first**
                                        let finalScore = null
                                        
                                        // Try finalScore first
                                        if (fullPromptData.finalScore !== undefined && 
                                            fullPromptData.finalScore !== null && 
                                            fullPromptData.finalScore !== -1 && 
                                            fullPromptData.finalScore !== '-1') {
                                            finalScore = fullPromptData.finalScore
                                        }
                                        // If there is no finalScore, use evalMaxScore
                                        else if (fullPromptData.evalMaxScore !== undefined && 
                                                 fullPromptData.evalMaxScore !== null && 
                                                 fullPromptData.evalMaxScore !== -1 && 
                                                 fullPromptData.evalMaxScore !== '-1') {
                                            finalScore = fullPromptData.evalMaxScore
                                        }
                                        // If evalMaxScore is not available, use evalAvgScore
                                        else if (fullPromptData.evalAvgScore !== undefined && 
                                                 fullPromptData.evalAvgScore !== null && 
                                                 fullPromptData.evalAvgScore !== -1 && 
                                                 fullPromptData.evalAvgScore !== '-1') {
                                            finalScore = fullPromptData.evalAvgScore
                                        }
                                        
                                        console.log('✅ Tooltip使用评分:', finalScore)
                                        
                                        // If there is rating data, display the rating area
                                        if (finalScore !== null) {
                                            
                                            tooltipContent += `<div style="margin-top: 8px; border-top: 1px solid rgba(255,255,255,0.2); padding-top: 8px;">`
                                            
                                            // **Show final score (large, eye-catching) + ranking**
                                            if (finalScore !== null) {
                                                // **Set color and size based on relative ranking**
                                                let scoreColor = '#999'
                                                let scoreSize = '20px'
                                                let scoreEmoji = '📊'
                                                let rankBadge = ''
                                                let rankText = ''
                                                
                                                if (this.scoreStatistics) {
                                                    const percentile = this.scoreStatistics.getPercentileRank(finalScore)
                                                    const allScores = this.scoreStatistics.scores
                                                    const rank = allScores.filter(s => s > finalScore).length + 1
                                                    const total = allScores.length
                                                    const isFirst = rank === 1
                                                    
                                                    rankText = `排名: ${rank}/${total}`
                                                    
                                                    // **Special Treatment: Ranked First**
                                                    if (isFirst) {
                                                        scoreColor = '#b24df5'  // Purple (distinguishable from yellow highlight)
                                                        scoreSize = '26px'
                                                        scoreEmoji = '🥇'
                                                        rankBadge = '<span style="background: linear-gradient(135deg, #b24df5 0%, #da6aff 50%, #b24df5 100%); color: #fff; padding: 3px 8px; border-radius: 4px; font-size: 11px; margin-left: 6px; font-weight: bold; box-shadow: 0 2px 8px rgba(178,77,245,0.5);">👑 第一名</span>'
                                                    }
                                                    // Ranked by Ranking Percentile
                                                    else if (percentile >= 80) {
                                                        // Top 20%
                                                        scoreColor = '#00d4aa'
                                                        scoreSize = '24px'
                                                        scoreEmoji = '🏆'
                                                        rankBadge = '<span style="background: #00d4aa; color: #000; padding: 2px 6px; border-radius: 4px; font-size: 10px; margin-left: 6px; font-weight: bold;">Top 20%</span>'
                                                    } else if (percentile >= 60) {
                                                        // 20-40%
                                                        scoreColor = '#52c41a'
                                                        scoreSize = '22px'
                                                        scoreEmoji = '✨'
                                                        rankBadge = '<span style="background: #52c41a; color: #fff; padding: 2px 6px; border-radius: 4px; font-size: 10px; margin-left: 6px; font-weight: bold;">Top 40%</span>'
                                                    } else if (percentile >= 40) {
                                                        // 40-60%
                                                        scoreColor = '#faad14'
                                                        scoreSize = '20px'
                                                        scoreEmoji = '📊'
                                                        rankBadge = '<span style="background: #faad14; color: #000; padding: 2px 6px; border-radius: 4px; font-size: 10px; margin-left: 6px; font-weight: bold;">中游</span>'
                                                    } else if (percentile >= 20) {
                                                        // 60-80%
                                                        scoreColor = '#ff7875'
                                                        scoreSize = '18px'
                                                        scoreEmoji = '📉'
                                                        rankBadge = '<span style="background: #ff7875; color: #fff; padding: 2px 6px; border-radius: 4px; font-size: 10px; margin-left: 6px; font-weight: bold;">靠后</span>'
                                                    } else {
                                                        // Bottom 20%
                                                        scoreColor = '#f5222d'
                                                        scoreSize = '18px'
                                                        scoreEmoji = '📊'
                                                        rankBadge = '<span style="background: #f5222d; color: #fff; padding: 2px 6px; border-radius: 4px; font-size: 10px; margin-left: 6px; font-weight: bold;">Bottom 20%</span>'
                                                    }
                                                }
                                                
                                                tooltipContent += `
                                                    <div style="margin-bottom: 8px;">
                                                        ${scoreEmoji} <span style="color: ${scoreColor}; font-weight: bold; font-size: ${scoreSize};">${finalScore.toFixed(1)}</span>
                                                        <span style="color: #888; font-size: 12px;"> / 10</span>
                                                        ${rankBadge}
                                                    </div>
                                                `
                                                
                                                // Show ranking information
                                                if (rankText) {
                                                    tooltipContent += `<div style="font-size: 12px; color: #aaa; margin-bottom: 8px;">${rankText}</div>`
                                                }
                                            }
                                            
                                            // **Show detailed rating**
                                            // Show average score
                                            if (fullPromptData.evalAvgScore !== undefined && 
                                                fullPromptData.evalAvgScore !== null && 
                                                fullPromptData.evalAvgScore !== -1 && 
                                                fullPromptData.evalAvgScore !== '-1') {
                                                tooltipContent += `<div style="font-size: 12px; color: #ccc; margin-top: 4px;">📊 平均分: <span style="color: #95e1d3;">${fullPromptData.evalAvgScore.toFixed(1)}</span></div>`
                                            }
                                            
                                            // Shows the highest score (if not used as the primary score)
                                            if (fullPromptData.evalMaxScore !== undefined && 
                                                fullPromptData.evalMaxScore !== null && 
                                                fullPromptData.evalMaxScore !== -1 && 
                                                fullPromptData.evalMaxScore !== '-1' &&
                                                finalScore !== fullPromptData.evalMaxScore) {
                                                tooltipContent += `<div style="font-size: 12px; color: #ccc; margin-top: 4px;">⭐ 最高分: <span style="color: #ffd93d;">${fullPromptData.evalMaxScore.toFixed(1)}</span></div>`
                                            }
                                            
                                            tooltipContent += `</div>`
                                        }
                                    }
                                }
                                
                                // **Add double click prompt (Aiming node only)**
                                tooltipContent += `<div style="margin-top: 8px; padding-top: 8px; border-top: 1px solid rgba(255,255,255,0.1); font-size: 11px; color: #999;">💡 双击可快速选中此靶道</div>`
                            }
                            
                            tooltip.innerHTML = tooltipContent
                        }
                        
                        // Update tooltip position
                        tooltip.style.display = 'block'
                        tooltip.style.left = (event.clientX + 15) + 'px'
                        tooltip.style.top = (event.clientY + 15) + 'px'
                        
                        // Change mouse style
                        this.map3dRenderer.domElement.style.cursor = 'pointer'
                    }
                } else {
                    // Not hovering over any node
                    if (hoveredNode) {
                        hoveredNode = null
                        tooltip.style.display = 'none'
                        this.map3dRenderer.domElement.style.cursor = 'default'
                        
                        // **Remove highlight effect**
                        if (currentHighlightMesh) {
                            this.map3dScene.remove(currentHighlightMesh)
                            if (currentHighlightMesh.geometry) currentHighlightMesh.geometry.dispose()
                            if (currentHighlightMesh.material) currentHighlightMesh.material.dispose()
                            currentHighlightMesh = null
                        }
                    }
                }
            }
            
            this.map3dRenderer.domElement.addEventListener('mousemove', onMouseMove)
            this.map3dMouseMoveHandler = onMouseMove
        },
        
        // Create connection line (called after all node positions are determined) - dynamic binding version
        createConnectionLines() {
            if (!this.map3dNodes || !this.map3dScene) return
            
            this.map3dNodes.forEach(nodeData => {
                // If there is already a connection cable, clean it first
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
                
                // Find the parent node via parentPath
                if (nodeData.node.parentPath) {
                    const parentNodeData = this.map3dNodes.find(n => {
                        if (!n.node.fullPath) return false
                        return n.node.fullPath === nodeData.node.parentPath
                    })
                    
                    if (parentNodeData && parentNodeData.mesh) {
                        // **Save parent node reference for dynamic updates**
                        nodeData.parentNodeData = parentNodeData
                        
                        const hasCurrent = nodeData.hasCurrent
                        
                        // **Use bright white or something close to white**
                        let lineColor = 0xffffff  // Default white
                        
                        if (hasCurrent) {
                            lineColor = 0xffd93d  // The current node uses yellow
                        }
                        
                        // Use Line instead of Cylinder so endpoints can be updated dynamically
                        // Create line geometry (two points)
                        const lineGeometry = new THREE.BufferGeometry()
                        const positions = new Float32Array(6) // 2 points × 3 coordinates
                        lineGeometry.setAttribute('position', new THREE.BufferAttribute(positions, 3))
                        
                        // Use LineBasicMaterial instead of cylinder
                        const lineMaterial = new THREE.LineBasicMaterial({
                            color: lineColor,
                            linewidth: 2,
                            transparent: true,
                            opacity: 0 // Initial transparency, used for fade-in animations
                        })
                        
                        const line = new THREE.Line(lineGeometry, lineMaterial)
                        this.map3dScene.add(line)
                        nodeData.line = line
                        nodeData.lineColor = lineColor
                        
                        // Add connection points (small balls) - also follow dynamically, use bright colors
                        const dotGeometry = new THREE.SphereGeometry(0.3, 8, 8)
                        const dotMaterial = new THREE.MeshStandardMaterial({ 
                            color: 0xffffff,  // use white
                            emissive: hasCurrent ? 0xffd93d : 0xffffff,  // Glow color
                            emissiveIntensity: hasCurrent ? 0.5 : 0.2,
                            metalness: 0.3,
                            roughness: 0.5,
                            transparent: true,
                            opacity: 0 // initial transparency
                        })
                        const dot = new THREE.Mesh(dotGeometry, dotMaterial)
                        this.map3dScene.add(dot)
                        nodeData.dot = dot
                        
                        // **Update the connection line position immediately**
                        this.updateConnectionLine(nodeData)
                    } else {
                        // Debugging: Logging when the parent node cannot be found
                        console.warn('找不到父节点:', {
                            fullPath: nodeData.node.fullPath,
                            parentPath: nodeData.node.parentPath,
                            nodeType: nodeData.node.type,
                            key: nodeData.key
                        })
                        
                        // Try to output the fullPath of all nodes to help debugging
                        if (nodeData.node.type === 'aiming') {
                            console.log('所有节点的 fullPath:', this.map3dNodes.map(n => n.node.fullPath))
                        }
                    }
                }
            })
        },
        
        // Update the position of a single connecting line (dynamically bound to node position)
        updateConnectionLine(nodeData) {
            if (!nodeData.line || !nodeData.parentNodeData) return
            
            const parentPosition = nodeData.parentNodeData.mesh.position
            const currentPosition = nodeData.mesh.position
            
            // Update the endpoint position of the line
            const positions = nodeData.line.geometry.attributes.position.array
            positions[0] = parentPosition.x
            positions[1] = parentPosition.y
            positions[2] = parentPosition.z
            positions[3] = currentPosition.x
            positions[4] = currentPosition.y
            positions[5] = currentPosition.z
            nodeData.line.geometry.attributes.position.needsUpdate = true
            
            // Update connection point location
            if (nodeData.dot) {
                nodeData.dot.position.set(currentPosition.x, currentPosition.y, currentPosition.z)
            }
        },
        
        // Update the position of all connecting lines
        updateAllConnectionLines() {
            if (!this.map3dNodes) return
            
            this.map3dNodes.forEach(nodeData => {
                if (nodeData.line && nodeData.parentNodeData) {
                    this.updateConnectionLine(nodeData)
                }
            })
        },
        
        // Start node entry animation
        startNodeAnimations() {
            if (!this.map3dNodes || this.map3dNodes.length === 0) return
            
            const animationDuration = 500 // Animation duration (ms)
            const startTime = Date.now()
            
            const animate = () => {
                const elapsed = Date.now() - startTime
                const progress = Math.min(elapsed / animationDuration, 1)
                
                // Use the easing function (easeOutCubic)
                const easeProgress = 1 - Math.pow(1 - progress, 3)
                
                this.map3dNodes.forEach((nodeData, index) => {
                    // Delay the animation start time of each node to create a wave effect
                    const delay = index * 20 // 20ms delay per node
                    const nodeProgress = Math.max(0, Math.min(1, (elapsed - delay) / animationDuration))
                    const nodeEaseProgress = 1 - Math.pow(1 - nodeProgress, 3)
                    
                    // Scale animation: from 0.1 to 1
                    const scale = 0.1 + nodeEaseProgress * 0.9
                    nodeData.mesh.scale.set(scale, scale, scale)
                    
                    if (nodeData.glowMesh) {
                        nodeData.glowMesh.scale.set(scale, scale, scale)
                    }
                    
                    // transparency animation
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
                
                // **Key: Update connection line positions during animation**
                this.updateAllConnectionLines()
                
                if (progress < 1) {
                    requestAnimationFrame(animate)
                } else {
                    // Once the animation is complete, make sure all nodes are fully visible
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
        
        // Create a gradient background
        createGradientBackground() {
            const canvas = document.createElement('canvas')
            canvas.width = 256
            canvas.height = 256
            const context = canvas.getContext('2d')
            
            // Create a radial gradient
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
        
        // Clear 3D scene
        clearMap3DScene() {
            if (!this.map3dScene) return
            
            // Remove all objects
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
        
        // Animation loop (optimized version: reduce unnecessary calculation and rendering)
        animateMap3D() {
            if (!this.map3dRenderer || !this.map3dScene || !this.map3dCamera) return
            
            this.map3dAnimationId = requestAnimationFrame(() => this.animateMap3D())
            
            // Track whether rendering is required
            let needsRender = false
            
            // Update controller (requires update every frame when damping is enabled)
            if (this.map3dControls) {
                // update() returns true indicating that the camera position has changed
                const controlsChanged = this.map3dControls.update()
                if (controlsChanged) {
                    needsRender = true
                }
            }
            
            // Optimization: Significantly reduce animation update frequency to avoid excessive CPU usage
            if (!this.map3dLastAnimationTime) {
                this.map3dLastAnimationTime = Date.now()
                this.map3dCurrentNodes = [] // Cache the currently selected node
            }
            
            const now = Date.now()
            // Update animation every 200ms (reduce frequency from 100ms to 200ms)
            if (now - this.map3dLastAnimationTime > 200) {
                this.map3dLastAnimationTime = now
                
                // The first time, find all currently selected nodes and cache them
                if (!this.map3dCurrentNodes || this.map3dCurrentNodes.length === 0) {
                    if (this.map3dNodes && this.map3dNodes.length > 0) {
                        this.map3dCurrentNodes = this.map3dNodes.filter(({ node }) => 
                            node.prompts && node.prompts.some(p => p.isCurrent)
                        )
                    }
                }
                
                // **Update connection line position (make sure to always follow the node)**
                this.updateAllConnectionLines()
                
                // Only update the currently selected node in the cache to avoid traversing all nodes
                if (this.map3dCurrentNodes && this.map3dCurrentNodes.length > 0) {
                    const time = now * 0.001
                    this.map3dCurrentNodes.forEach(({ mesh }) => {
                        const scale = 1 + Math.sin(time * 2) * 0.05
                        mesh.scale.set(scale, scale, scale)
                    })
                    needsRender = true
                }
            }
            
            // Only render when needed to avoid invalid rendering
            if (needsRender) {
                this.map3dRenderer.render(this.map3dScene, this.map3dCamera)
            }
        },
        
        // Pop-up animation (expand child nodes)
        animateNodesPopOut(parentNodeData, onComplete) {
            if (!this.map3dScene || !this.map3dNodes) return
            
            // **Recursively collect all child nodes that should be displayed**
            // If the child node itself is in the expanded state (expanded !== false), then recursively collect its child nodes
            const childNodes = []
            
            const collectChildren = (parentNode) => {
                if (!parentNode.children || Object.keys(parentNode.children).length === 0) return
                
                Object.keys(parentNode.children).forEach(childKey => {
                    const childNode = parentNode.children[childKey]
                    const childNodeData = this.map3dNodes.find(n => n.node === childNode && n.key === childKey)
                    if (childNodeData) {
                        childNodes.push(childNodeData)
                        
                        // **Key fix: If this child node is in expanded state, recursively collect its child nodes**
                        // This allows you to expand all levels at once
                        if (childNode.expanded !== false) {
                            collectChildren(childNode)
                        }
                    }
                })
            }
            
            collectChildren(parentNodeData.node)
            
            if (childNodes.length === 0) {
                if (onComplete) onComplete()
                return
            }
            
            // Pop-up animation: starting from the parent node position, scaling + displacement to the target position
            // **Shorten the animation time of a single node to make the domino effect more obvious**
            const animationDuration = 250  // Reduced from 350ms to 250ms
            const startTime = Date.now()
            
            // **Fix: Save the target position of each child node and its immediate parent node position**
            childNodes.forEach(childData => {
                // **Critical Fix: For Aiming nodes, recalculate Z axis position**
                if (childData.node.type === 'aiming') {
                    // Find parent node
                    const parentData = this.map3dNodes.find(n => 
                        n.node.fullPath === childData.node.parentPath
                    )
                    
                    if (parentData && parentData.node.children) {
                        // Recalculate the Z-axis offset of the Aiming node
                        const aimingKeys = Object.keys(parentData.node.children).filter(k => 
                            parentData.node.children[k].type === 'aiming'
                        )
                        const aimingIndex = aimingKeys.indexOf(childData.key)
                        
                        if (aimingIndex !== -1) {
                            const aimingSpacing = 16 // Remain the same as when it was created (increased by 1x)
                            
                            // **New strategy: A nodes start from the position of the parent node and are arranged in sequence toward the camera direction (+Z)**
                            // A1: parentZ + aimingSpacing, A2: parentZ + 2*aimingSpacing, ...
                            const parentZ = parentData.mesh.position.z
                            const correctZ = parentZ + (aimingIndex + 1) * aimingSpacing
                            
                            // **New layout: Coordinates of Aiming nodes recalculated**
                            // X-axis: Same as parent node (Aiming node is at the same horizontal position)
                            const correctX = parentData.mesh.position.x
                            // Y axis: Same as the parent node (Aiming does not go down one level, but extends on the Z axis)
                            const correctY = parentData.mesh.position.y
                            
                            // Use recalculated correct positions (X, Y, Z are all recalculated)
                            childData._targetPosition = {
                                x: correctX,  // Same X as parent node
                                y: correctY,  // Same Y as parent node (not down)
                                z: correctZ   // Extends from the parent node toward the camera
                            }
                        } else {
                            childData._targetPosition = { ...childData.position }
                        }
                    } else {
                        childData._targetPosition = { ...childData.position }
                    }
                } else {
                    // Non-aiming nodes: use saved original position
                    childData._targetPosition = { ...childData.position }
                }
                
                // **Key Fix: Find the direct parent node position of this node**
                // Instead of using the top-level parentNodeData
                if (childData.parentNodeData && childData.parentNodeData.mesh) {
                    // If there is parentNodeData (from connection line logic), use it
                    childData._parentPosition = {
                        x: childData.parentNodeData.mesh.position.x,
                        y: childData.parentNodeData.mesh.position.y,
                        z: childData.parentNodeData.mesh.position.z
                    }
                } else {
                    // Otherwise, try to find the direct parent node
                    const directParentData = this.map3dNodes.find(n => 
                        n.node.fullPath === childData.node.parentPath
                    )
                    if (directParentData && directParentData.mesh) {
                        childData._parentPosition = {
                            x: directParentData.mesh.position.x,
                            y: directParentData.mesh.position.y,
                            z: directParentData.mesh.position.z
                        }
                    } else {
                        // If not found, use the top-most parent node (cover the bottom)
                        childData._parentPosition = { ...parentNodeData.position }
                    }
                }
            })
            
            const animate = () => {
                const elapsed = Date.now() - startTime
                const progress = Math.min(elapsed / animationDuration, 1)
                
                // Easing using easeOutBack (slight pop effect)
                const c1 = 1.70158
                const c3 = c1 + 1
                const easeProgress = 1 + c3 * Math.pow(progress - 1, 3) + c1 * Math.pow(progress - 1, 2)
                
                childNodes.forEach((childData, index) => {
                    // **Significantly increases latency, creating a noticeable domino effect**
                    const delay = index * 80  // Increased from 50ms to 80ms to make the interval between each node more obvious
                    const nodeProgress = Math.max(0, Math.min(1, (elapsed - delay) / animationDuration))
                    
                    // **If the animation has not started yet (nodeProgress === 0), hide the node**
                    if (nodeProgress === 0) {
                        childData.mesh.visible = false
                        if (childData.glowMesh) childData.glowMesh.visible = false
                        if (childData.sprite) childData.sprite.visible = false
                        if (childData.line) childData.line.visible = false
                        if (childData.dot) childData.dot.visible = false
                        return
                    }
                    
                    const nodeEase = 1 + c3 * Math.pow(nodeProgress - 1, 3) + c1 * Math.pow(nodeProgress - 1, 2)
                    
                    // Interpolate from parent node position to target position
                    const x = childData._parentPosition.x + (childData._targetPosition.x - childData._parentPosition.x) * nodeEase
                    const y = childData._parentPosition.y + (childData._targetPosition.y - childData._parentPosition.y) * nodeEase
                    const z = childData._parentPosition.z + (childData._targetPosition.z - childData._parentPosition.z) * nodeEase
                    
                    // Zoom: zoom in from 0.1 to 1
                    const scale = 0.1 + nodeEase * 0.9
                    
                    // Update node position and scale
                    childData.mesh.position.set(x, y, z)
                    childData.mesh.scale.set(scale, scale, scale)
                    childData.mesh.visible = true
                    
                    if (childData.glowMesh) {
                        childData.glowMesh.position.set(x, y, z)
                        childData.glowMesh.scale.set(scale, scale, scale)
                        childData.glowMesh.visible = true
                    }
                    
                    if (childData.sprite) {
                        childData.sprite.position.set(x, y + (childData.node.type === 'range' ? 5 : 4), z)
                        childData.sprite.visible = true
                        if (childData.sprite.material) {
                            childData.sprite.material.opacity = nodeEase
                        }
                    }
                    
                    if (childData.line && childData.line.material) {
                        childData.line.visible = true
                        childData.line.material.opacity = nodeEase * 0.8
                    }
                    
                    if (childData.dot && childData.dot.material) {
                        childData.dot.visible = true
                        childData.dot.material.opacity = nodeEase
                    }
                })
                
                // Update connection line
                this.updateAllConnectionLines()
                
                if (progress < 1) {
                    requestAnimationFrame(animate)
                } else {
                    // **Animation complete, return to normal position and ensure full visibility**
                    childNodes.forEach(childData => {
                        // restore location
                        childData.mesh.position.set(childData._targetPosition.x, childData._targetPosition.y, childData._targetPosition.z)
                        childData.mesh.scale.set(1, 1, 1)
                        childData.mesh.visible = true
                        
                        if (childData.glowMesh) {
                            childData.glowMesh.position.set(childData._targetPosition.x, childData._targetPosition.y, childData._targetPosition.z)
                            childData.glowMesh.scale.set(1, 1, 1)
                            childData.glowMesh.visible = true
                        }
                        
                        if (childData.sprite) {
                            const spriteY = childData._targetPosition.y + (childData.node.type === 'range' ? 5 : 4)
                            childData.sprite.position.set(childData._targetPosition.x, spriteY, childData._targetPosition.z)
                            childData.sprite.visible = true
                            if (childData.sprite.material) {
                                childData.sprite.material.opacity = 1
                            }
                        }
                        
                        if (childData.line) {
                            childData.line.visible = true
                            if (childData.line.material) {
                                childData.line.material.opacity = 0.8
                            }
                        }
                        
                        if (childData.dot) {
                            childData.dot.visible = true
                            if (childData.dot.material) {
                                childData.dot.material.opacity = 1
                            }
                        }
                    })
                    this.updateAllConnectionLines()
                    if (onComplete) onComplete()
                }
            }
            
            animate()
        },
        
        // Inhale animation (collapse child nodes)
        animateNodesSuckIn(parentNodeData, onComplete) {
            if (!this.map3dScene || !this.map3dNodes) return
            
            // Collect all direct child nodes
            const childNodes = []
            const collectChildren = (node) => {
                if (!node.children || Object.keys(node.children).length === 0) return
                
                Object.keys(node.children).forEach(childKey => {
                    const childNode = node.children[childKey]
                    const childNodeData = this.map3dNodes.find(n => n.node === childNode && n.key === childKey)
                    if (childNodeData) {
                        childNodes.push(childNodeData)
                        // Recursively collect all child nodes
                        collectChildren(childNode)
                    }
                })
            }
            
            collectChildren(parentNodeData.node)
            
            if (childNodes.length === 0) {
                if (onComplete) onComplete()
                return
            }
            
            // Inhalation animation: scale + displacement from current position to parent node position, then hide
            // **Shorten the animation time of a single node to make the domino effect more obvious**
            const animationDuration = 200  // Reduced from 300ms to 200ms
            const startTime = Date.now()
            
            // Save the starting position of each child node
            childNodes.forEach(childData => {
                childData._startPosition = {
                    x: childData.mesh.position.x,
                    y: childData.mesh.position.y,
                    z: childData.mesh.position.z
                }
            })
            
            const animate = () => {
                const elapsed = Date.now() - startTime
                const progress = Math.min(elapsed / animationDuration, 1)
                
                // Easing (accelerating inhalation) using easeInCubic
                const easeProgress = progress * progress * progress
                
                childNodes.forEach((childData, index) => {
                    // **Significantly increases reverse latency, creating a noticeable reverse domino effect**
                    const delay = (childNodes.length - index) * 60  // Increased from 35ms to 60ms
                    const nodeProgress = Math.max(0, Math.min(1, (elapsed - delay) / animationDuration))
                    const nodeEase = nodeProgress * nodeProgress * nodeProgress
                    
                    // Interpolate from current position to parent node position
                    const x = childData._startPosition.x + (parentNodeData.position.x - childData._startPosition.x) * nodeEase
                    const y = childData._startPosition.y + (parentNodeData.position.y - childData._startPosition.y) * nodeEase
                    const z = childData._startPosition.z + (parentNodeData.position.z - childData._startPosition.z) * nodeEase
                    
                    // Zoom: from 1 to 0.1
                    const scale = 1 - nodeEase * 0.9
                    const opacity = 1 - nodeEase
                    
                    // Update node position and scale
                    childData.mesh.position.set(x, y, z)
                    childData.mesh.scale.set(scale, scale, scale)
                    
                    if (childData.glowMesh) {
                        childData.glowMesh.position.set(x, y, z)
                        childData.glowMesh.scale.set(scale, scale, scale)
                    }
                    
                    if (childData.sprite) {
                        childData.sprite.position.set(x, y + (childData.node.type === 'range' ? 5 : 4), z)
                        if (childData.sprite.material) {
                            childData.sprite.material.opacity = opacity
                        }
                    }
                    
                    if (childData.line && childData.line.material) {
                        childData.line.material.opacity = opacity * 0.8
                    }
                    
                    if (childData.dot && childData.dot.material) {
                        childData.dot.material.opacity = opacity
                    }
                })
                
                // Update connection line
                this.updateAllConnectionLines()
                
                if (progress < 1) {
                    requestAnimationFrame(animate)
                } else {
                    // The animation is completed and all nodes are completely hidden.
                    childNodes.forEach(childData => {
                        childData.mesh.visible = false
                        if (childData.glowMesh) childData.glowMesh.visible = false
                        if (childData.sprite) childData.sprite.visible = false
                        if (childData.line) childData.line.visible = false
                        if (childData.dot) childData.dot.visible = false
                    })
                    if (onComplete) onComplete()
                }
            }
            
            animate()
        },
        
        // Handling window size changes
        handleMap3DResize() {
            const container = document.getElementById('map3dContainer')
            if (!container || !this.map3dCamera || !this.map3dRenderer) return
            
            const width = container.clientWidth
            const height = container.clientHeight
            
            this.map3dCamera.aspect = width / height
            this.map3dCamera.updateProjectionMatrix()
            this.map3dRenderer.setSize(width, height)
        },
        
        // Destroy 3D scene
        destroyMap3D() {
            window.removeEventListener('resize', this.handleMap3DResize)
            
            // Remove click event
            if (this.map3dRenderer && this.map3dRenderer.domElement && this.map3dClickHandler) {
                this.map3dRenderer.domElement.removeEventListener('click', this.map3dClickHandler)
                this.map3dClickHandler = null
            }
            
            // **Remove double click event**
            if (this.map3dRenderer && this.map3dRenderer.domElement && this.map3dDoubleClickHandler) {
                this.map3dRenderer.domElement.removeEventListener('dblclick', this.map3dDoubleClickHandler)
                this.map3dDoubleClickHandler = null
            }
            
            // **Remove mouse movement events**
            if (this.map3dRenderer && this.map3dRenderer.domElement && this.map3dMouseMoveHandler) {
                this.map3dRenderer.domElement.removeEventListener('mousemove', this.map3dMouseMoveHandler)
                this.map3dMouseMoveHandler = null
            }
            
            // **Remove tooltip element**
            const tooltip = document.getElementById('map3d-tooltip')
            if (tooltip) {
                tooltip.remove()
            }
            
            // Stop animation loop
            if (this.map3dAnimationId) {
                cancelAnimationFrame(this.map3dAnimationId)
                this.map3dAnimationId = null
            }
            
            // Destroy the controller
            if (this.map3dControls) {
                this.map3dControls.dispose()
                this.map3dControls = null
            }
            
            // Clear scene
            this.clearMap3DScene()
            
            // Clear node mapping
            if (this.map3dNodeMap) {
                this.map3dNodeMap.clear()
                this.map3dNodeMap = null
            }
            
            // Clear cached current node
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
        
        // Close the dialog input pop-up window
        // Execute the core logic of target practice (extracted for reuse in dialogue mode and direct test mode)
        async executeTargetShoot() {
            await this.executeTargetShootWithChatMessage(null)
        },
        
        // Execute the core logic of target shooting (supports conversation mode, when userMessage is null, it indicates direct test mode)
        async executeTargetShootWithChatMessage(userMessage) {
            this.tacticalFormSubmitLoading = true
            
            // Save userMessage to maintain Chat mode for subsequent bursts of messages
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
            
            // If userMessage is provided, add to request data
            if (userMessage) {
                _postData.userMessage = userMessage
            }
            
            // If you continue chat mode, pass the history
            if (this.continueChatMode && this.continueChatHistory.length > 0) {
                _postData.chatHistory = this.continueChatHistory.map(msg => ({
                    role: msg.roleType === 1 ? 'user' : 'assistant',
                    content: msg.content
                }))
            }
            
            // ai scoring criteria
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
                //Create top-level tactics, create parallel tactics, create sub-tactics, re-target
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
                        // Make sure the mode field is set correctly (the backend returns an enum value of 1 or 2)
                        if (item.mode === undefined || item.mode === null) {
                            item.mode = null // Compatible with old data
                        }
                    }
                    return item
                })
                
                // Check the mode of the first result, if it is not Chat mode, clear the saved userMessage
                if (promptResultList.length > 0) {
                    const firstResult = promptResultList[0]
                    if (firstResult.mode !== 2 && firstResult.mode !== 'Chat') {
                        // If it is not Chat mode, clear the saved userMessage
                        this._lastUserMessage = null
                    }
                }
                
                // Reset chat status
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
        // Version record Get version record Tree data
        async getVersionRecordData() {
            //find rangeName by id

            // const rangeName
            let _find = this.promptFieldOpt.find(item => item.value === this.promptField)
            const name = _find ? _find.rangeName : ''

            let res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.GetTacticTree?rangeName=${name}`)
            if (res.data.success) {
                //console.log('Get version record data', res.data.data.rootNodeList)
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
        //Tree data formatting Parameter data: the data to be formatted, child is the subarray value name of the data to be formatted
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
        // Version record view
        seeVersionRecord() {
            this.versionDrawer = true
            // Retrieve data
            this.getVersionRecordData()
        },
        // Version record tree control filter node
        versionTreeFilterNode(value, data) {
            if (!value) return true;
            return data.label.indexOf(value) > -1;
        },
        // Version history Drawer closed
        versionDrawerClose() {
            this.versionSearchVal = ''
        },
        // Is the version record public?
        versionRecordIsPublic(itemData) {
            // console.log('Is the version record public:', itemData)
            // to do interface docking async await
            // Tips to stay tuned
            this.$message.warning('敬请期待')
        },
        // Version record edit
        versionRecordEdit(itemData) {
            //console.log('Version record editor:', itemData)
            // to do interface docking async await
            // Set overbearing selected
            this.promptid = itemData.id
            // Get target lane details
            this.getPromptetail(itemData.id, true, true)
            // Get output list and average score
            //this.getOutputList()
            // Get score trend chart data
            this.getScoringTrendData()
            // close drawer
            this.versionDrawer = false
        },
        // version record generated code
        versionRecordGenerateCode(itemData) {
            //console.log('Version record generated code:', itemData)
            // Tips to stay tuned
            this.$message.warning('敬请期待')
            // to do interface docking async await
        },
        // version record delete
        versionRecordDelete(itemData) {
            //console.log('Version record deleted:', itemData)
            this.$message.warning('敬请期待')
            // to do interface docking async await
            //this.$confirm('This operation will permanently delete the target version, do you want to continue?', 'Prompt', {
            //    confirmButtonText: 'OK',
            //    cancelButtonText: 'Cancel',
            //    type: 'warning'
            //}).then(() => {
            //    // docking interface delete
            //    this.btnDeleteHandle(itemData.id,true)

            //}).catch(() => {
            //    this.$message({
            //        type: 'info',
            //        message: 'Deletion canceled'
            //    });
            //});
        },
        // Version record View notes
        versionRecordViewNotes(itemData) {
            //console.log('Version record view remarks:', itemData)
            // to do interface docking async await
        },


        // Get output list
        async getOutputList(promptId) {
            let res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetByItemId?promptItemId=${promptId}`)
            //console.log('getOutputList:', res)
            if (res.data.success) {
                let { promptResults = [], promptItem = {} } = res.data.data || {}
                // Average score _totalScore/promptResults retain integer
                this.outputAverageDeci = promptItem.evalAvgScore > -1 ? promptItem.evalAvgScore : -1; // Keep integer
                this.outputMaxDeci = promptItem.evalMaxScore > -1 ? promptItem.evalMaxScore : -1; // Keep integer

                // Output list
                this.outputList = promptResults.map(item => {
                    if (item) {
                        item.promptId = this.promptDetail.id
                        item.version = this.promptDetail.fullVersion
                        item.scoreType = '1' // 1 ai, 2 manual
                        item.isScoreView = false // Whether to display the rating view
                        item.addTime = item.addTime ? this.formatDate(item.addTime) : ''

                        //Use MarkDown format to display the output results
                        item.resultStringHtml = marked.parse(item.resultString);

                        // Manual scoring
                        item.scoreVal = item.humanScore > -1 ? item.humanScore : 0
                        // ai scoring expected results
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
                
                // Add code block copy button
                this.$nextTick(() => {
                    this.addCopyButtonsToCodeBlocks();
                });
            } else {
                app.$message({
                    message: res.data.errorMessage || res.data.data || 'Error',
                    type: 'error',
                    duration: 5 * 1000
                });
            }
        },
        // Output Save Rating
        async saveManualScore(item, index) {
            //console.log('manualScorVal', this.promptSelectVal, this.manualScorVal)
            if (item.scoreType === '1') {
                let _list = item.alResultList.map(item => item.value)
                let res = await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.RobotScore?isRefresh=true&promptResultId=${item.id}`, _list)
                if (res.data.success) {
                    //console.log('testHandel res data:', res.data.data)
                    // Retrieve shooting range list
                    this.getPromptOptData()
                    // Retrieve output list
                    await this.getOutputList(item.promptId)
                    // Clear AI scoring loading status
                    this.$set(this.robotScoreLoadingMap, item.id, false)
                    // Retrieve chart
                    this.getScoringTrendData()
                    
                    // ⭐ NEW: Check scores and prompt for optimization
                    await this.checkScoreAndSuggestOptimization(res.data.data || item, 'AI评分');
                } else {
                    // Clear AI scoring loading status
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
                    // Retrieve shooting range list
                    this.getPromptOptData()
                    // Retrieve output list
                    this.getOutputList(item.promptId)
                    // Retrieve chart
                    this.getScoringTrendData()
                    
                    // ⭐ NEW: Check scores and prompt for optimization
                    await this.checkScoreAndSuggestOptimization(res.data.data || item, '手动评分');
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
        // Output selected switch
        outputSelectSwitch(index) {
            if (this.outputActive !== '' && this.outputActive !== index) {
                this.outputList[this.outputActive].isScoreView = false
            }
            this.outputActive = index
        },
        // Output Show scoring view
        showRatingView(index, scoreType) {
            // If it is AI scoring, the scoring view will not be displayed. If there is no expected result, it will remind you to set the expected result.
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
                        // Set AI rating loading status
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
                    // todo interface docking re-score
                    // Set AI rating loading status
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
        // Output switch ai rating
        alBtnScoring(index) {
            this.outputList[index].scoreType = '1'
        },
        // Output ai score Add result row
        addAlScoring(index) {
            let _len = this.outputList[index].alResultList.length
            this.outputList[index].alResultList.push({
                id: _len + 1,
                label: `预期结果`,
                value: ''
            })
        },
        // Output Toggle manual scoring
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
            // Add to parameter if userMessage is provided (used to maintain Chat mode)
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
                    this.outputAverageDeci = res.data.data.promptItem.evalAvgScore > -1 ? res.data.data.promptItem.evalAvgScore : -1; // Keep integer
                    this.outputMaxDeci = res.data.data.promptItem.evalMaxScore > -1 ? res.data.data.promptItem.evalMaxScore : -1; // Keep integer
                    //Output list 
                    res.data.data.promptResults.map(item => {
                        item.promptId = promptItemId
                        item.scoreType = '1' // 1 ai, 2 manual 
                        item.isScoreView = false // Whether to display the rating view
                        //time format addTime
                        item.addTime = item.addTime ? this.formatDate(item.addTime) : ''

                        //Use MarkDown format to display the output results
                        item.resultStringHtml = marked.parse(item.resultString);

                        // Manual scoring
                        item.scoreVal = 0
                        // ai scoring expected results
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

        // Configuration reset parameters
        resetConfigurineParam(isPageChange) {
            // todo determines whether to record page change records
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
            //console.log('Configuration parameters reset:', this.parameterViewList)
            // Parameter settings View configuration list
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
        // Configuration parameter setting input callback
        parameterInputHandle(val, item) {
            // Page change record
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
            // Determine the content of the restricted input based on the parameters in the item
            if (isStr) {
                // string type
            } else {
                // if (isSlider || isStr )
                //Data with sliding selection must be numeric
                let _val = val.replace(/[^\d]/g, '')
                //floor
                _val = Math.round(_val / sliderStep) * sliderStep
                //console.log('parameterInputHandle _val:', _val)
                //and is less than sliderMax and is greater than sliderMin. The number of reserved digits is the same as sliderStep.
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
        // Configuration Enter Prompt Reset 
        resetInputPrompt() {
            //console.log('Enter Prompt to reset:', this.content)
            this.content = ''// prompt input content
            //this.remarks = '' // prompt input comments
        },
        deleteModel(item) {
            //Delete model confirm
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
                            // Reset model list
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

        //  prompt request parameters close
        promptParamFormClose() {
            this.promptParamForm = {
                prefix: '',
                suffix: '',
                variableList: []
            }
            this.$refs.promptParamForm.resetFields();
            this.promptParamVisible = true;
        },
        // prompt request parameter add variable line btn
        addVariableBtn() {
            this.promptParamForm.variableList.push({
                name: '',
                value: ''
            })
        },
        toAIKernel() {
            window.open('/Admin/AIKernel/Index?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69')
        },
        // prompt request parameter delete variable line btn
        deleteVariableBtn(index) {
            this.promptParamForm.variableList.splice(index, 1)
        },
        // prompt request parameters submit
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
                // Prompt refresh completed
                this.$message({
                    message: '刷新完成！',
                    type: 'success'
                })
            })
        },
        // Configure Get model drop-down list data
        async getModelOptData() {
            let res = await servicePR.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetListAsync', {})
            //console.log('getModelOptData:', res)
            if (res.data.success) {
                //console.log('getModelOptData:', res.data)
                let _optList = res.data.data || []
                this.modelOpt = _optList.map(item => {
                    // Build label: model name + version number (if any) + deployment name (if any)
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
                        displayName: item.alias,  // Keep the original name for display elsewhere
                        deploymentDisplay: item.deploymentName, // Keep deployment name
                        apiVersion: item.apiVersion, // Keep version number
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
        // New model dialog close
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
        // Add new model dialog submission
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
                        // Retrieve model list
                        await this.getModelOptData().then(() => {
                            this.modelid = res.data.data.id
                        })
                        // Prompt added successfully
                        this.$message({
                            message: '添加成功！',
                            type: 'success'
                        })
                        // close dialog
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


        // Close the new shooting range dialog
        fieldFormCloseDialog() {
            this.fieldForm = {
                alias: ''
            }
            this.$refs.fieldForm.resetFields();
        },
        // dialog add shooting range submit button
        fieldFormSubmitBtn() {
            const that = this
            this.$refs.fieldForm.validate(async (valid) => {
                if (valid) {
                    this.fieldFormVisible = false
                    // post interface /api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.AddAsync'
                    const res = await servicePR.post('/api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.AddAsync?alias='
                        + that.fieldForm.alias, {})
                    if (res.data.success) {
                        // Retrieve range list
                        await this.getFieldList().then(() => {
                            that.promptField = res.data.data.id
                            this.resetPageData()
                        })
                        // Prompt added successfully
                        this.$message({
                            message: '添加成功！',
                            type: 'success'
                        })
                        // close dialog
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
        // Configure Get Range Dropdown List Data
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
            //A prompt box will pop up, enter the new shooting range name, confirm and submit. After cancelling, it will prompt that the operation has been cancelled.
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
        // Get target lane dropdown list data
        async getPromptOptData(id, isExpected) {
            // find rangeName by id (unify it into a string when compared with promptField to avoid el-select storing string and the interface returning number, resulting in the range not being found)
            const matchField = (v) => String(v) === String(this.promptField)
            let _find = this.promptFieldOpt.find(item => matchField(item.value))
            if (isExpected) {
                _find = this.promptFieldOpt.find(item => String(item.value) === String(id))
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
                    // Export tree data 
                    this.expectedPluginFieldList.push({
                        id: _find.id,
                        label: _find.label, // Range name
                        idkey: `${_find.value}`, // shooting range id
                        children: _promptOpt.map(item => {
                            this.expectedPluginFoem.checkList.push(`${_find.value}_${item.id}`)
                            return {
                                id: item.id,
                                label: item.label, // Range name
                                idkey: `${_find.value}_${item.id}`, // shooting range id
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
        // Get prompt details
        async getPromptetail(id, overwrite, isChart) {
            let res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Get?id=${Number(id)}`,)
            /*console.log('getPromptetail:', res)*/
            if (res.data.success) {
                //console.log('getPromptetail:', res.data)
                // copy data
                let copyResultData = JSON.parse(JSON.stringify(res.data.data))
                let vArr = copyResultData.fullVersion.split('-')
                copyResultData.promptFieldStr = vArr[0] || ''
                copyResultData.promptStr = vArr[1] || ''
                copyResultData.tacticsStr = vArr[2] || ''
                if (copyResultData.stopSequences) {
                    copyResultData.stopSequences = JSON.parse(copyResultData.stopSequences).join(',')
                }
                this.promptDetail = copyResultData
                //If there is no result obtained, the previous expectedJson will be continued.
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
                    // Retrieve output list
                    this.getOutputList(this.promptDetail.id)
                    if (isChart) {
                        // Retrieve chart
                        this.getScoringTrendData()
                    }


                    // Parameter override
                    let _parameterViewList = JSON.parse(JSON.stringify(this.parameterViewList))

                    this.parameterViewList = _parameterViewList.map(item => {
                        if (item) {
                            item.value = this.promptDetail[item.formField] || item.value
                        }
                        return item
                    })

                    // Determine whether there is a selected model in the model list
                    let _findIndex = this.modelOpt.findIndex(item => item.value === this.promptDetail.modelId)
                    if (_findIndex > -1) {
                        // Model coverage
                        this.modelid = this.promptDetail.modelId
                    } else {
                        this.modelid = ''
                    }
                    // prompt input content
                    this.content = this.promptDetail.promptContent || ''
                    // prompt input remarks
                    this.remarks = this.promptDetail.note || ''
                    // prompt request parameters
                    let _promptParamForm = JSON.parse(JSON.stringify(this.promptParamForm))
                    _promptParamForm.prefix = this.promptDetail.prefix || ''
                    _promptParamForm.suffix = this.promptDetail.suffix || ''
                    _promptParamForm.variableList = []
                    if (this.promptDetail.variableDictJson) {
                        let _variableDictJson = JSON.parse(this.promptDetail.variableDictJson)
                        // _variableDictJson is not an empty object, so assign values ​​in a loop
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

                    //Add the interface return content to the expected result of ai
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
                    
                    // ⭐ New: Check average score and prompt for optimization
                    await this.checkPromptAverageScoreAndSuggest();
                }


            } else {
                app.$message({
                    message: res.data.errorMessage || res.data.data || 'Error',
                    type: 'error',
                    duration: 5 * 1000
                })
            }
        },
        // Delete prompt 
        async btnDeleteHandle(id, versionRecord) {
            const res = await servicePR.request({
                method: 'delete',
                url: `/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.DeleteAsync?id=${id}`,
            });
            if (res.data.success) {
                // Re-acquire the target lane list. If the currently selected target lane is deleted, reset the target lane selection value and reset the model, parameters, input content, notes, output list, average score, chart, and AI score expected results.
                if (id === this.promptid) {
                    this.resetPageData()
                } else {
                    // Re-obtain prompt list
                    await this.getPromptOptData(this.promptid)
                }

                if (versionRecord) {
                    // Retrieve version records
                    await this.getVersionRecordData()
                }


                // Delete successfully
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
        // Modify prompt alias
        async btnEditHandle(item, isSave) {

            if (!item) return
            const res = await servicePR.request({
                method: 'post',
                url: `/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Modify`,
                data: item
            });
            if (res.data.success) {
                if (isSave) {
                    // Prompt to save successfully
                    this.$message({
                        message: '保存成功！',
                        type: 'success'
                    })
                } else {
                    // Re-obtain prompt list
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

        // ai scoring setting dialog new result line btn
        aiScoreFormAddRow() {
            let _len = this.aiScoreForm.resultList.length
            this.aiScoreForm.resultList.push({
                id: _len + 1,
                label: `预期结果`,
                value: ''
            })
        },
        // ai scoring settings open dialog 
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
            //    //Reset if no expected result
            //    this.aiScoreForm = {
            //        resultList: [{
            //            id: 1,
            //            label: 'Expected result 1',
            //            value: ''
            //        }]
            //    }
            //}
            // Determine whether this.aiScoreForm.resultList has a value            
            this.aiScoreFormVisible = !this.aiScoreFormVisible
        },
        // Close ai scoring settings dialog
        aiScoreFormCloseDialog() {
            this.$refs.aiScoreForm.resetFields();
        },
        aiScoreFormClose() {
            this.aiScoreFormVisible = false
            // Determine whether the comparison details of this.aiScoreForm.resultList and this.promptDetail.expectedResultsJson have changed
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
        // dialog ai rating settings submit button
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
                        // Retrieve details
                        this.getPromptetail(this.promptid, false)
                        // close dialog
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
        // Copy Prompt test results
        copyPromptResult(item, rawResult) {

            // Copy results to clipboard
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
        
        /**
         * Initialization code block copy button
         * Add copy button to all <pre><code> code blocks
         */
        initCodeCopyButtons() {
            const self = this;
            
            // Use MutationObserver to monitor DOM changes and automatically add copy buttons for new code blocks
            const observer = new MutationObserver((mutations) => {
                mutations.forEach((mutation) => {
                    if (mutation.addedNodes.length) {
                        self.addCopyButtonsToCodeBlocks();
                    }
                });
            });
            
            // Monitor output area and conversation history area
            const outputArea = document.querySelector('.outputArea_contentBox');
            const chatArea = document.querySelector('.chat-history-container');
            
            if (outputArea) {
                observer.observe(outputArea, {
                    childList: true,
                    subtree: true
                });
            }
            
            if (chatArea) {
                observer.observe(chatArea, {
                    childList: true,
                    subtree: true
                });
            }
            
            // initial addition
            this.addCopyButtonsToCodeBlocks();
        },
        
        /**
         * Add copy button to all code blocks
         */
        addCopyButtonsToCodeBlocks() {
            const self = this;
            
            // Find all code blocks (output area + conversation history area)
            const codeBlocks = document.querySelectorAll(
                '.contentRow pre:not(.copy-btn-added), .chat-message-content pre:not(.copy-btn-added)'
            );
            
            codeBlocks.forEach((pre) => {
                // Tags have been added to avoid duplication
                pre.classList.add('copy-btn-added');
                
                // Create a copy button
                const copyBtn = document.createElement('button');
                copyBtn.className = 'code-copy-btn';
                copyBtn.innerHTML = '<i class="el-icon-document-copy"></i> Copy';
                copyBtn.title = '复制代码';
                
                // Bind click event
                copyBtn.addEventListener('click', function(e) {
                    e.preventDefault();
                    e.stopPropagation();
                    
                    // Get code content
                    const codeElement = pre.querySelector('code') || pre;
                    const code = codeElement.textContent || codeElement.innerText;
                    
                    // Copy using CopyHelper
                    if (window.PromptRangeUtils && window.PromptRangeUtils.CopyHelper) {
                        const success = window.PromptRangeUtils.CopyHelper.copyToClipboard(
                            code, 
                            '代码复制成功', 
                            '代码复制失败',
                            true
                        );
                        
                        if (success) {
                            // Show copy success status
                            copyBtn.classList.add('copied');
                            copyBtn.innerHTML = '<i class="el-icon-check"></i> Copied!';
                            
                            // Restore after 2 seconds
                            setTimeout(() => {
                                copyBtn.classList.remove('copied');
                                copyBtn.innerHTML = '<i class="el-icon-document-copy"></i> Copy';
                            }, 2000);
                        }
                    } else {
                        // Downgrade option: Use traditional methods
                        self.fallbackCopyCode(code, copyBtn);
                    }
                });
                
                // Add button to pre element
                pre.appendChild(copyBtn);
            });
        },
        
        /**
         * Downgrade copy code method
         * @param {string} code - the code to copy
         * @param {HTMLElement} button - copy button element
         */
        fallbackCopyCode(code, button) {
            try {
                const textarea = document.createElement('textarea');
                textarea.value = code;
                textarea.style.position = 'fixed';
                textarea.style.top = '0';
                textarea.style.left = '-9999px';
                textarea.setAttribute('readonly', '');
                document.body.appendChild(textarea);
                
                textarea.select();
                textarea.setSelectionRange(0, textarea.value.length);
                
                const success = document.execCommand('copy');
                document.body.removeChild(textarea);
                
                if (success) {
                    this.$message.success('代码复制成功');
                    button.classList.add('copied');
                    button.innerHTML = '<i class="el-icon-check"></i> Copied!';
                    
                    setTimeout(() => {
                        button.classList.remove('copied');
                        button.innerHTML = '<i class="el-icon-document-copy"></i> Copy';
                    }, 2000);
                } else {
                    this.$message.error('代码复制失败');
                }
            } catch (err) {
                console.error('Copy failed:', err);
                this.$message.error('代码复制失败');
            }
        },
        // Methods related to customizing scroll bar thumbnails
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
            
            // Calculate the relative position of each item
            let totalItemsHeight = 0;
            let currentTop = 0;
            
            for (let i = 0; i < items.length; i++) {
                totalItemsHeight += items[i].offsetHeight;
                if (i < index) {
                    currentTop += items[i].offsetHeight;
                }
            }
            
            const currentHeight = items[index] ? items[index].offsetHeight : 30;
            
            // Calculate position in thumbnail track (proportionally)
            const top = (currentTop / totalHeight) * trackHeight;
            const height = Math.max((currentHeight / totalHeight) * trackHeight, 20); // Minimum 20px
            
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
            
            // Determine whether the item is within the visible area
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
            // Extract the time part, for example "2024-01-01 10:30:45" => "10:30"
            if (!timeStr) return '';
            const match = timeStr.match(/(\d{2}):(\d{2}):\d{2}/);
            return match ? match[1] + ':' + match[2] : timeStr.substring(0, 10);
        },
        // Get the final score (using the system's finalScore field)
        getFinalScore(item) {
            if (!item) return null;
            // Directly use the system's finalScore field, which is the score marked in red
            if (item.finalScore !== undefined && item.finalScore !== null && 
                item.finalScore !== -1 && item.finalScore !== '-1') {
                return item.finalScore;
            }
            return null;
        },
        // Get the style class of the rating level
        getScoreBarClass(item) {
            const score = this.getFinalScore(item);
            if (score === null) return '';
            
            if (score >= 8) return 'score-excellent';      // 8-10 points: excellent
            if (score >= 6) return 'score-good';           // 6-8 points: good
            if (score >= 4) return 'score-medium';         // 4-6 points: medium
            if (score >= 2) return 'score-low';            // 2-4 points: lower
            return 'score-poor';                           // 0-2 points: poor
        },
        // Get the width style of the data bar (Excel style)
        getScoreBarStyle(item) {
            const score = this.getFinalScore(item);
            if (score === null) return { width: '0%' };
            
            // 0-10 points map to 0-100%
            const percentage = Math.min(Math.max((score / 10) * 100, 0), 100);
            return {
                width: percentage + '%'
            };
        },
        // Get the tooltip text of the thumbnail
        getThumbnailTooltip(item) {
            if (!item) return '';
            
            let tooltip = item.addTime;
            const score = this.getFinalScore(item);
            
            if (score !== null) {
                // Determine which score the finalScore is equal to, that is the final score type
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
        // Handle mouse movement events in the output area - determine whether it is on the right half
        handleOutputAreaMouseMove(event) {
            const outputArea = event.currentTarget;
            const rect = outputArea.getBoundingClientRect();
            const mouseX = event.clientX;
            
            // Calculate mouse position relative to output area
            const relativeX = mouseX - rect.left;
            const halfWidth = rect.width / 2;
            
            // Show scrollbar only on right half
            if (relativeX > halfWidth) {
                this.showScrollbarThumbnails = true;
            } else {
                this.showScrollbarThumbnails = false;
            }
        },
        // Handle mouse leaving the output area
        handleOutputAreaMouseLeave() {
            this.showScrollbarThumbnails = false;
        },
        
        // ========== Area width drag adjustment function ==========
        
        // Load saved width settings from localStorage
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
        
        // Save width settings to localStorage
        saveAreaWidthsToStorage() {
            try {
                localStorage.setItem('promptPage_leftAreaWidth', this.leftAreaWidth);
                localStorage.setItem('promptPage_centerAreaWidth', this.centerAreaWidth);
            } catch (e) {
                console.error('保存区域宽度设置失败:', e);
            }
        },
        
        // Start dragging the left divider bar
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
        
        // Start dragging the right divider bar
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
        
        // Handle drag
        handleResize(event) {
            if (!this.isResizing) return;
            
            const deltaX = event.clientX - this.resizeStartX;
            
            if (this.resizeType === 'left') {
                // Adjust left area width
                let newWidth = this.resizeStartLeftWidth + deltaX;
                newWidth = Math.max(280, Math.min(600, newWidth)); // Limit to 280-600px
                this.leftAreaWidth = newWidth;
            } else if (this.resizeType === 'right') {
                // Adjust the width of the middle area
                let newWidth = this.resizeStartCenterWidth + deltaX;
                newWidth = Math.max(320, Math.min(800, newWidth)); // Limit to 320-800px
                this.centerAreaWidth = newWidth;
            }
            
            event.preventDefault();
        },
        
        // Stop dragging
        stopResize() {
            if (this.isResizing) {
                this.isResizing = false;
                this.resizeType = null;
                
                document.removeEventListener('mousemove', this.handleResize);
                document.removeEventListener('mouseup', this.stopResize);
                document.body.style.cursor = '';
                document.body.style.userSelect = '';
                
                // Save current width setting
                this.saveAreaWidthsToStorage();
            }
        },
        
        // Double-click the separator bar to restore the default width
        resetAreaWidths(event) {
            // Restore default width
            this.leftAreaWidth = 360;
            this.centerAreaWidth = 380;
            
            // Clear saved settings in localStorage
            try {
                localStorage.removeItem('promptPage_leftAreaWidth');
                localStorage.removeItem('promptPage_centerAreaWidth');
            } catch (e) {
                console.error('清除区域宽度设置失败:', e);
            }
            
            // Add visual feedback animation
            if (event && event.target) {
                event.target.classList.add('reset-animation');
                setTimeout(() => {
                    event.target.classList.remove('reset-animation');
                }, 600);
            }
            
            // Show prompt message
            this.$message({
                message: '已还原为默认布局宽度',
                type: 'success',
                duration: 2000
            });
        },
        
        // ========== Prompt comparison function ==========
        
        // Open the comparison dialog (optional preset Prompt A)
        openCompareDialog(event, item) {
            // Prevent events from bubbling and triggering the el-select drop-down
            if (event) {
                event.stopPropagation();
            }
            
            // Prompt A should be the currently selected and displayed Prompt
            // Prompt B is the corresponding prompt for clicking the "Compare" button
            if (this.promptid) {
                this.comparePromptAId = this.promptid;
                this.loadComparePromptA(this.promptid);
            }
            
            // If item is passed in, set it to Prompt B
            // item.value is the value of el-option, corresponding to the ID of Prompt
            if (item && item.value) {
                this.comparePromptBId = item.value;
                this.loadComparePromptB(item.value);
            }
            
            // Open dialog
            this.compareDialogVisible = true;
        },
        
        // Load data comparing Prompt A
        async loadComparePromptA(id) {
            if (!id) {
                this.comparePromptA = null;
                return;
            }
            
            try {
                const data = await this.getPromptDetail(id);
                this.comparePromptA = data;
                
                // Check if the same prompt is selected
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
        
        // Load data comparing Prompt B
        async loadComparePromptB(id) {
            if (!id) {
                this.comparePromptB = null;
                return;
            }
            
            try {
                const data = await this.getPromptDetail(id);
                this.comparePromptB = data;
                
                // Check if the same prompt is selected
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
        
        // Get details of a single prompt
        async getPromptDetail(id) {
            const res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Get?id=${Number(id)}`);
            
            if (res.data.success) {
                return res.data.data;
            } else {
                throw new Error(res.data.errorMessage || '获取Prompt详情失败');
            }
        },
        
        // Swap Prompt A and B
        swapComparePrompts() {
            // Exchange ID
            const tempId = this.comparePromptAId;
            this.comparePromptAId = this.comparePromptBId;
            this.comparePromptBId = tempId;
            
            // exchange data
            const tempData = this.comparePromptA;
            this.comparePromptA = this.comparePromptB;
            this.comparePromptB = tempData;
        },
        
        // Check if the rating exists
        hasScore(score) {
            return score !== null && score !== undefined && score > -1;
        },
        
        // Get the prefix (directly from the prefix field)
        getPromptPrefix(side) {
            const prompt = side === 'A' ? this.comparePromptA : this.comparePromptB;
            if (!prompt) return '';
            return prompt.prefix || '';
        },
        
        // Get the suffix (directly from the suffix field)
        getPromptSuffix(side) {
            const prompt = side === 'A' ? this.comparePromptA : this.comparePromptB;
            if (!prompt) return '';
            return prompt.suffix || '';
        },
        
        // Jump to the specified Prompt (completely simulates the behavior of manually clicking on the target channel selection)
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
                // 1. Get the complete Prompt data first
                const promptData = await this.getPromptDetail(promptId);
                
                if (!promptData) {
                    throw new Error('无法获取Prompt详细信息');
                }
                
                // 2. Parse the shooting range name from fullVersion (format: shooting range-range-tactics)
                const versionParts = promptData.fullVersion ? promptData.fullVersion.split('-') : [];
                const targetRangeName = versionParts[0];
                
                // 3. Find the corresponding shooting range ID
                const targetRange = this.promptFieldOpt.find(item => 
                    item.label === targetRangeName || item.rangeName === targetRangeName
                );
                
                if (!targetRange) {
                    throw new Error(`未找到对应的靶场: ${targetRangeName}`);
                }
                
                // 4. Close the comparison dialog box (close it first to avoid interfering with subsequent operations)
                this.compareDialogVisible = false;
                
                // 5. Reset the pageChange mark to avoid triggering the "Do you want to save the draft" confirmation dialog box?
                this.pageChange = false;
                
                // 6. Check whether you need to switch shooting ranges
                const needSwitchRange = this.promptField !== targetRange.value;
                
                if (needSwitchRange) {
                    // Switch ranges (this triggers an update of promptOpt)
                    this.promptField = targetRange.value;
                    await this.promptChangeHandel(targetRange.value, 'promptField');
                    
                    // Wait for promptOpt update to complete
                    await this.$nextTick();
                }
                
                // 7. Find the corresponding prompt in promptOpt
                // Note: item.value in promptOpt is the correct promptid
                const targetPrompt = this.promptOpt.find(item => 
                    item.id === promptId || item.value === promptId || item.idkey === promptId
                );
                
                if (!targetPrompt) {
                    throw new Error('在当前靶场中未找到对应的Prompt');
                }
                
                // 8. Set the correct promptid (this will trigger the v-model update of el-select)
                // IMPORTANT: Again make sure pageChange = false as switching ranges may trigger this
                this.pageChange = false;
                this.promptid = targetPrompt.value || targetPrompt.id;
                
                // 9. Manually trigger promptChangeHandel to completely simulate the user clicking on the target drop-down selection
                // This will:
                //   - Update sendBtns (depending on whether it is a draft)
                //   - Clear AI scoring criteria
                //   - Call getPromptetail to get complete details (including output list, ratings, charts, etc.)
                await this.promptChangeHandel(this.promptid, 'promptid');
                
                // 10. Display success prompt
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
        
        // Check if fields are different
        hasFieldDiff(fieldA, fieldB) {
            // Handling null/undefined cases
            if (fieldA === fieldB) return false;
            if ((fieldA === null || fieldA === undefined) && (fieldB === null || fieldB === undefined)) return false;
            
            // If it is an object or array, perform a deep comparison
            if (typeof fieldA === 'object' && typeof fieldB === 'object') {
                return JSON.stringify(fieldA) !== JSON.stringify(fieldB);
            }
            
            return fieldA !== fieldB;
        },
        
        // Format variable configuration (convert from JSON string to readable format)
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
        
        // Generate Git-style Prompt content diff HTML
        getContentDiffHtml(side) {
            const contentA = this.comparePromptA?.promptContent || '';
            const contentB = this.comparePromptB?.promptContent || '';
            
            // if both are empty
            if (!contentA && !contentB) {
                return '<span class="diff-empty">暂无内容</span>';
            }
            
            // If only one is empty
            if (!contentA && side === 'A') {
                return '<span class="diff-empty">暂无内容</span>';
            }
            if (!contentB && side === 'B') {
                return '<span class="diff-empty">暂无内容</span>';
            }
            
            // If the content is the same
            if (contentA === contentB) {
                return this.escapeHtml(side === 'A' ? contentA : contentB);
            }
            
            // Difference comparison using jsdiff library
            if (typeof Diff !== 'undefined') {
                const diff = Diff.diffLines(contentA, contentB);
                return this.renderDiffHtml(diff, side);
            }
            
            // If the diff library is not loaded, return the original content
            return this.escapeHtml(side === 'A' ? contentA : contentB);
        },
        
        // Generate Git-style variable configuration diff HTML
        getVariablesDiffHtml(side) {
            const varsA = this.comparePromptA?.variableDictJson || '{}';
            const varsB = this.comparePromptB?.variableDictJson || '{}';
            
            // Format JSON for comparison
            let formattedA = varsA;
            let formattedB = varsB;
            try {
                formattedA = JSON.stringify(JSON.parse(varsA), null, 2);
            } catch (e) {
                // stay as is
            }
            try {
                formattedB = JSON.stringify(JSON.parse(varsB), null, 2);
            } catch (e) {
                // stay as is
            }
            
            // If the content is the same
            if (formattedA === formattedB) {
                return this.escapeHtml(side === 'A' ? formattedA : formattedB);
            }
            
            // Difference comparison using jsdiff library
            if (typeof Diff !== 'undefined') {
                const diff = Diff.diffLines(formattedA, formattedB);
                return this.renderDiffHtml(diff, side);
            }
            
            // If the diff library is not loaded, return the original content
            return this.escapeHtml(side === 'A' ? formattedA : formattedB);
        },
        
        // Rendering differential HTML (Git style, supports inline word highlighting)
        renderDiffHtml(diff, side) {
            let html = '';
            let i = 0;
            
            while (i < diff.length) {
                const part = diff[i];
                const lines = part.value.split('\n');
                // Remove last blank line
                if (lines[lines.length - 1] === '') {
                    lines.pop();
                }
                
                // Check whether there are adjacent deletions and additions (inline diff can be done)
                const nextPart = i + 1 < diff.length ? diff[i + 1] : null;
                const isInlineDiffCandidate = 
                    part.removed && 
                    nextPart && 
                    nextPart.added && 
                    lines.length === 1 && 
                    nextPart.value.split('\n').filter(l => l).length === 1;
                
                if (isInlineDiffCandidate) {
                    // Inline word level differences
                    const oldLine = lines[0];
                    const newLine = nextPart.value.split('\n').filter(l => l)[0];
                    
                    if (side === 'A') {
                        html += `<span class="diff-line diff-modified">- ${this.renderInlineDiff(oldLine, newLine, 'removed')}</span>`;
                    } else {
                        html += `<span class="diff-line diff-modified">+ ${this.renderInlineDiff(oldLine, newLine, 'added')}</span>`;
                    }
                    
                    i += 2; // Skip the next part (because it has already been processed)
                } else {
                    // regular whole line diff
                    lines.forEach((line, lineIndex) => {
                        if (part.added) {
                            // New content (green background) - only shown on side B
                            if (side === 'B') {
                                html += `<span class="diff-line diff-added">+ ${this.escapeHtml(line)}</span>`;
                            }
                            // New content is not displayed on side A
                        } else if (part.removed) {
                            // Deleted content (red background) - only shown on side A
                            if (side === 'A') {
                                html += `<span class="diff-line diff-removed">- ${this.escapeHtml(line)}</span>`;
                            }
                            // Deleted content is not displayed on side B
                        } else {
                            // Unmodified content (gray)
                            html += `<span class="diff-line diff-unchanged">  ${this.escapeHtml(line)}</span>`;
                        }
                    });
                    i++;
                }
            }
            
            return html || '<span class="diff-empty">暂无内容</span>';
        },
        
        // Rendering intraline word-level differences
        renderInlineDiff(oldText, newText, mode) {
            if (typeof Diff === 'undefined' || !Diff.diffWords) {
                return this.escapeHtml(mode === 'removed' ? oldText : newText);
            }
            
            const wordDiff = Diff.diffWords(oldText, newText);
            let html = '';
            
            wordDiff.forEach(part => {
                const escapedText = this.escapeHtml(part.value);
                
                if (mode === 'removed') {
                    // Side A: Highlight deleted words
                    if (part.removed) {
                        html += `<mark class="diff-word-removed">${escapedText}</mark>`;
                    } else if (!part.added) {
                        html += escapedText;
                    }
                } else {
                    // Side B: Highlight newly added words
                    if (part.added) {
                        html += `<mark class="diff-word-added">${escapedText}</mark>`;
                    } else if (!part.removed) {
                        html += escapedText;
                    }
                }
            });
            
            return html;
        },
        
        // HTML escape tool function
        escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        },
        
        // ========== contenteditable editor method (concise version) ==========
        
        // Get plain text in editor
        getEditorText() {
            const editor = this.$refs.promptEditor;
            if (!editor) return '';
            return editor.innerText || '';
        },
        
        // Generate HTML with highlighting
        generateHighlightHTML(text) {
            if (!text) return '';
            
            // Get the prefix and suffix (from the currently edited prompt parameter)
            const prefix = this.promptParamForm.prefix || '';
            const suffix = this.promptParamForm.suffix || '';
            
            // debug log
            console.log('[generateHighlightHTML] prefix:', prefix, 'suffix:', suffix);
            console.log('[generateHighlightHTML] variableList:', this.promptParamForm.variableList);
            
            if (!prefix || !suffix) {
                // There is no prefix or suffix, and the escaped HTML is returned directly.
                console.log('[generateHighlightHTML] No prefix/suffix, returning plain text');
                return text
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;')
                    .replace(/\n/g, '<br>');
            }
            
            // Get all defined variable names (without prefix and suffix)
            const definedVarNames = new Set();
            if (this.promptParamForm.variableList && this.promptParamForm.variableList.length > 0) {
                this.promptParamForm.variableList.forEach(v => {
                    if (v.name) {
                        definedVarNames.add(v.name);
                    }
                });
            }
            
            console.log('[generateHighlightHTML] definedVarNames:', Array.from(definedVarNames));
            
            // Escape regular special characters
            const escapeRegex = (str) => str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            const escapedPrefix = escapeRegex(prefix);
            const escapedSuffix = escapeRegex(suffix);
            
            // Build regex: match {{$varName}}, use non-greedy matching
            // For example: prefix={{$, suffix=}}, the regular expression is: \{\{\$(.+?)\}\}
            const regex = new RegExp(`${escapedPrefix}(.+?)${escapedSuffix}`, 'g');
            
            console.log('[generateHighlightHTML] regex:', regex);
            
            let result = '';
            let lastIndex = 0;
            let matchCount = 0;
            let match;
            
            // Use exec to match one by one
            while ((match = regex.exec(text)) !== null) {
                matchCount++;
                const fullMatch = match[0];  // Complete match, such as {{$prefix}}
                const varName = match[1];     // Capturing groups, such as prefix
                const offset = match.index;
                
                console.log(`[generateHighlightHTML] Match ${matchCount}:`, fullMatch, 'varName:', varName, 'offset:', offset);
                
                // Add text before match (HTML escape and handle newlines)
                const beforeText = text.substring(lastIndex, offset);
                // Handle newlines: replace newlines with <br>
                // NOTE: Trailing whitespace characters before span tags are no longer removed as the user may enter the space intentionally
                let processedBeforeText = beforeText
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;');
                
                // Replace newlines with <br> (keep all spaces and tabs as the user may have entered them intentionally)
                processedBeforeText = processedBeforeText.replace(/\n/g, '<br>');
                result += processedBeforeText;
                
                // Determine whether the variable has been defined
                const isDefined = definedVarNames.has(varName);
                const className = isDefined ? 'var-highlight defined' : 'var-highlight undefined';
                
                console.log(`[generateHighlightHTML] varName "${varName}" isDefined:`, isDefined);
                
                // Add highlighted variables (HTML escaped, but without newlines)
                const escapedMatch = fullMatch
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;');
                // Make sure there are no whitespace characters before and after the span tag, and splice it directly (do not add newlines or spaces in the HTML string)
                result += `<span class="${className}">${escapedMatch}</span>`;
                
                lastIndex = offset + fullMatch.length;
            }
            
            console.log('[generateHighlightHTML] Total matches:', matchCount);
            
            // Add remaining text (HTML escaping and handling newlines)
            const remainingText = text.substring(lastIndex);
            // If there are whitespace characters (spaces, tabs, etc.) at the beginning of the remaining text, remove them first to avoid creating whitespace after span
            // But keep the newlines as they need to be converted to <br>
            let processedRemainingText = remainingText
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');
            
            // Remove leading whitespace characters (but keep newlines)
            processedRemainingText = processedRemainingText.replace(/^[ \t]+/, '');
            // Replace newlines with <br>
            processedRemainingText = processedRemainingText.replace(/\n/g, '<br>');
            result += processedRemainingText;
            
            // Note: Whitespace characters (spaces, tabs, etc.) before and after the span tag are no longer cleaned
            // Because the user may enter spaces before and after the span, these spaces should be preserved
            // The cleanup function cleanupHighlightBrTags will handle excess blank text nodes
            
            console.log('[generateHighlightHTML] Final HTML (first 200 chars):', result.substring(0, 200));
            
            return result;
        },
        
        // Escape regular expression special characters
        escapeRegex(str) {
            return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        },
        
        // Save cursor position (returns character offset, based on plain text)
        // Use exactly the same traversal logic as restoreCaretPosition to ensure consistency
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
            
            // Uses exactly the same traversal logic as restoreCaretPosition
            const walkNodes = (node) => {
                if (found) return;
                
                if (node.nodeType === Node.TEXT_NODE) {
                    if (node === targetContainer) {
                        // The target text node is found, plus the offset
                        charCount += targetOffset;
                        found = true;
                        return;
                    }
                    // Not the target node, plus the length of the entire node
                    const nodeLength = node.textContent ? node.textContent.length : 0;
                    charCount += nodeLength;
                } else if (node.nodeName === 'BR') {
                    // Check if the cursor is after this BR tag
                    // If targetContainer is the parent node of BR and targetOffset points to after this BR
                    if (targetContainer.nodeType === Node.ELEMENT_NODE && 
                        targetContainer === node.parentNode &&
                        targetOffset > 0) {
                        // After checking whether targetOffset points to this BR
                        const brIndex = Array.from(targetContainer.childNodes).indexOf(node);
                        if (targetOffset === brIndex + 1) {
                            // The cursor is after this BR tag
                            charCount += 1;
                            found = true;
                            return;
                        }
                    }
                    // BR tag counts as one character (newline character)
                    charCount += 1;
                } else if (node.nodeType === Node.ELEMENT_NODE) {
                    // Checks whether the cursor is between the child nodes of this element node
                    if (node === targetContainer && targetOffset > 0) {
                        // The cursor is between the child nodes of this node
                        // Count the number of characters using exactly the same traversal logic as restoreCaretPosition
                        for (let i = 0; i < targetOffset && i < node.childNodes.length; i++) {
                            const childNode = node.childNodes[i];
                            // Recursively traverse child nodes, using the same logic
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
                    // For element nodes, recursively traverse the child nodes
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
        
        // Restore cursor position (based on plain text offset)
        // Use exactly the same traversal logic as saveCaretPosition to ensure consistency
        restoreCaretPosition(offset) {
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            const sel = window.getSelection();
            if (!sel) return;
            
            const range = document.createRange();
            let charCount = 0;
            let found = false;
            
            // Uses exactly the same traversal logic as saveCaretPosition
            const walkNodes = (node) => {
                if (found) return;
                
                if (node.nodeType === Node.TEXT_NODE) {
                    const nodeText = node.textContent || '';
                    const nodeLength = nodeText.length;
                    const nextCharCount = charCount + nodeLength;
                    
                    if (offset >= charCount && offset <= nextCharCount) {
                        // The cursor is within this text node
                        const positionInNode = Math.min(offset - charCount, nodeLength);
                        range.setStart(node, positionInNode);
                        range.collapse(true);
                        found = true;
                        return;
                    }
                    charCount = nextCharCount;
                } else if (node.nodeName === 'BR') {
                    // BR tag counts as one character (newline character)
                    const nextCharCount = charCount + 1;
                    if (offset === charCount) {
                        // The cursor is before the BR tag
                        range.setStartBefore(node);
                        range.collapse(true);
                        found = true;
                        return;
                    } else if (offset === nextCharCount) {
                        // The cursor is after the BR tag
                        range.setStartAfter(node);
                        range.collapse(true);
                        found = true;
                        return;
                    }
                    charCount = nextCharCount;
                } else if (node.nodeType === Node.ELEMENT_NODE) {
                    // For element nodes, recursively traverse the child nodes
                    // Note: Inline elements such as span do not affect the character count, only the text inside them is counted.
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
                    // Fallback: put at the end
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
                // If you can't find the exact location, try placing the cursor at the end
                console.warn('[restoreCaretPosition] Could not find position', offset, ', placing at end');
                try {
                    range.selectNodeContents(editor);
                    range.collapse(false); // fold to end
                    sel.removeAllRanges();
                    sel.addRange(range);
                } catch (e) {
                    console.warn('[restoreCaretPosition] Failed to place caret at end:', e);
                }
            }
        },
        
        // Handle paste event
        handlePaste(e) {
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            // Tag is pasting
            this._isPasting = true;
            
            // Clear previous anti-shake timer
            if (this._highlightTimer) clearTimeout(this._highlightTimer);
            
            // Save cursor position before pasting (using the same method as saveCaretPosition)
            const sel = window.getSelection();
            let pasteStartPos = 0;
            
            if (sel.rangeCount > 0) {
                // Use the same traversal method as saveCaretPosition to calculate the position
                const range = sel.getRangeAt(0);
                const targetContainer = range.startContainer;
                const targetOffset = range.startOffset;
                
                let charCount = 0;
                let found = false;
                
                // Uses exactly the same traversal logic as saveCaretPosition
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
            
            // Get pasted plain text content
            const pasteData = e.clipboardData ? e.clipboardData.getData('text/plain') : '';
            const pasteTextLength = pasteData.length;
            
            console.log('[handlePaste] Paste data length:', pasteTextLength, 'pasteStartPos:', pasteStartPos);
            
            // Wait for the pasted content to be inserted before processing
            // Use requestAnimationFrame to ensure the paste operation is fully completed
            requestAnimationFrame(() => {
                setTimeout(() => {
                    this._isPasting = false;
                    
                    // Calculate the cursor position after pasting: paste start position + paste text length
                    const newCaretPos = pasteStartPos + pasteTextLength;
                    
                    console.log('[handlePaste] Calculated new caret position:', newCaretPos);
                    
                    const text = this.getEditorText();
                    
                    // Check if the content has actually changed
                    const contentChanged = text !== this.content;
                    
                    // Update content
                    this.content = text;
                    
                    // If the content really changes, trigger a status update (switch from "Burst" to "Target Shooting")
                    if (contentChanged) {
                        this.promptChangeHandel(text, 'content');
                    }
                    
                    // Apply highlighting, passing in the expected cursor position
                    this.applyHighlightWithCaretPos(newCaretPos);
                }, 10);
            });
        },
        
        // Handle keyboard key events (especially the Enter key)
        handleKeyDown(e) {
            // If it is the Enter key, special processing is required
            if (e.key === 'Enter' || e.keyCode === 13) {
                const editor = this.$refs.promptEditor;
                if (!editor) return;
                
                // Mark Enter Enter
                this._isEntering = true;
                
                // Clear previous anti-shake timer
                if (this._highlightTimer) clearTimeout(this._highlightTimer);
                
                // Save the current cursor position before entering the BR tag
                const sel = window.getSelection();
                let enterStartPos = 0;
                
                if (sel.rangeCount > 0) {
                    // Calculate the position using the same method as saveCaretPosition
                    enterStartPos = this.saveCaretPosition();
                    console.log('[handleKeyDown] Enter key pressed, saved position before BR insertion:', enterStartPos);
                }
                
                // Wait for the Enter key to be inserted into the BR tag before processing.
                requestAnimationFrame(() => {
                    setTimeout(() => {
                        this._isEntering = false;
                        
                        // After pressing Enter, the cursor should be after the BR label and the position should be enterStartPos + 1
                        const newCaretPos = enterStartPos + 1;
                        
                        console.log('[handleKeyDown] After Enter, calculated new caret position:', newCaretPos);
                        
                        const text = this.getEditorText();
                        
                        // Check if the content has actually changed
                        const contentChanged = text !== this.content;
                        
                        // Update content
                        this.content = text;
                        
                        // If the content really changes, trigger a status update (switch from "Burst" to "Target Shooting")
                        if (contentChanged) {
                            this.promptChangeHandel(text, 'content');
                        }
                        
                        // Use applyHighlightWithCaretPos to apply highlight and restore cursor position
                        this.applyHighlightWithCaretPos(newCaretPos);
                    }, 10);
                });
            }
        },
        
        // On user input (immediately updates highlights, uses short stabilization to optimize performance)
        handleEditorInput(e) {
            if (this.isComposing) return;  // IME input is not processed
            if (this._isPasting) return;   // Not processed during paste operation, handled by handlePaste
            if (this._isEntering) return;  // It is not processed during the carriage return operation and is processed by handleKeyDown.
            
            const text = this.getEditorText();
            
            // Check if the content has actually changed
            const contentChanged = text !== this.content;
            
            // Update content
            this.content = text;
            
            // If the content really changes, trigger a status update (switch from "Burst" to "Target Shooting")
            if (contentChanged) {
                this.promptChangeHandel(text, 'content');
            }
            
            // Use a short anti-shake time (150ms) to balance performance and responsiveness
            // In this way, the highlight will be updated immediately when the user enters, deletes, cuts, etc.
            if (this._highlightTimer) clearTimeout(this._highlightTimer);
            this._highlightTimer = setTimeout(() => {
                this.applyHighlight();
            }, 150);
        },
        
        // Clean up excess <br> tags and blank text nodes around var-highlight span and normalize DOM
        cleanupHighlightBrTags(editor) {
            if (!editor) return;
            
            console.log('[cleanupHighlightBrTags] Starting cleanup...');
            
            // Find all var-highlight span tags (use Array.from to create a copy to avoid dynamic query issues)
            const highlightSpans = Array.from(editor.querySelectorAll('.var-highlight'));
            
            console.log('[cleanupHighlightBrTags] Found', highlightSpans.length, 'highlight spans');
            
            highlightSpans.forEach(span => {
                // Clean up the blank text nodes in front of the span
                // Only remove text nodes that are completely blank (containing no visible characters), leaving text nodes that contain user-entered spaces
                let prevSibling = span.previousSibling;
                while (prevSibling) {
                    if (prevSibling.nodeType === Node.TEXT_NODE) {
                        const textContent = prevSibling.textContent;
                        // Only remove completely blank text nodes (containing only whitespace characters and a length of 0, or whitespace automatically added by the browser)
                        // Preserve text nodes that contain user-entered spaces (even if there are only spaces, they should be preserved because the user may enter spaces intentionally)
                        if (!textContent || textContent.length === 0) {
                            // Completely empty text nodes, removed
                            const toRemove = prevSibling;
                            prevSibling = prevSibling.previousSibling;
                            toRemove.remove();
                        } else {
                            // Text nodes with content, reserved (including nodes containing only spaces, as the user may enter spaces intentionally)
                            break;
                        }
                    } else {
                        // Nodes of other types (including BR tags), stop cleaning
                        break;
                    }
                }
                
                // Clean up the blank text nodes after span
                // Only remove text nodes that are completely blank, leaving text nodes that contain user-entered spaces
                let nextSibling = span.nextSibling;
                while (nextSibling) {
                    if (nextSibling.nodeType === Node.TEXT_NODE) {
                        const textContent = nextSibling.textContent;
                        // Remove only completely blank text nodes
                        // Preserve text nodes containing user-entered spaces
                        if (!textContent || textContent.length === 0) {
                            // Completely empty text nodes, removed
                            const toRemove = nextSibling;
                            nextSibling = nextSibling.nextSibling;
                            toRemove.remove();
                        } else {
                            // Text nodes with content, reserved (including nodes containing only spaces, as the user may enter spaces intentionally)
                            break;
                        }
                    } else {
                        // Stop cleaning when encountering non-text nodes (including BR tags)
                        // The BR tag is a user-entered line break and should not be removed
                        break;
                    }
                }
            });
            
            // Normalize DOM: merge adjacent text nodes
            // This ensures that the span tags fit snugly into the text nodes, with no intervening white space text nodes
            editor.normalize();
            
            console.log('[cleanupHighlightBrTags] Cleanup completed. Editor innerHTML (first 300 chars):', editor.innerHTML.substring(0, 300));
        },
        
        // Apply highlighting (using the specified cursor position, for operations such as pasting)
        applyHighlightWithCaretPos(expectedCaretPos) {
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            // Get the current text
            const text = this.getEditorText();
            
            // Synchronize content (but do not trigger state changes, because state changes should be handled in handleEditorInput, handlePaste, handleKeyDown)
            // This is only responsible for highlighting
            this.content = text;
            
            // Generate highlighted HTML
            const html = this.generateHighlightHTML(text);
            
            // Update HTML
            editor.innerHTML = html;
            
            // Use requestAnimationFrame to ensure the DOM is fully updated before cleaning and restoring the cursor
            requestAnimationFrame(() => {
                // Clean up redundant <br> tags and blank text nodes
                this.cleanupHighlightBrTags(editor);
                
                // Use $nextTick to ensure cleanup is complete before restoring the cursor
                this.$nextTick(() => {
                    // Use innerText to calculate the text length and ensure it is consistent with saveCaretPosition
                    const currentText = editor.innerText || '';
                    const currentTextLength = currentText.length;
                    const targetPos = Math.min(expectedCaretPos, currentTextLength);
                    console.log('[applyHighlightWithCaretPos] Restoring caret position:', targetPos, 'expected:', expectedCaretPos, 'current text length:', currentTextLength);
                    this.restoreCaretPosition(targetPos);
                });
            });
            
            // NOTE: promptChangeHandel is no longer called here
            // Status updates of content changes should be handled in actual input events such as handleEditorInput, handlePaste, handleKeyDown, etc.
            // This is only responsible for highlighting
        },
        
        // Apply highlighting (save cursor position)
        applyHighlight() {
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            // Check if the editor has focus
            const hasFocus = document.activeElement === editor;
            
            // Get the current text (get it before saving the cursor position to ensure text consistency)
            const text = this.getEditorText();
            
            // Synchronize content (but not trigger state changes, since state changes should be handled in handleEditorInput)
            // This is only responsible for highlighting
            this.content = text;
            
            // Save cursor position only when editor has focus
            // Use a text content-based approach rather than DOM traversal to ensure consistency with recovery
            let caretPos = 0;
            if (hasFocus) {
                const sel = window.getSelection();
                if (sel.rangeCount > 0) {
                    const range = sel.getRangeAt(0);
                    // Create a range from the beginning of the editor to the cursor position
                    const preCaretRange = range.cloneRange();
                    preCaretRange.selectNodeContents(editor);
                    preCaretRange.setEnd(range.startContainer, range.startOffset);
                    // Use textContent to calculate position (excluding BR tags, which are calculated separately during traversal)
                    // But we need to consider the BR tag, so we use the method of traversing the DOM
                    caretPos = this.saveCaretPosition();
                }
                console.log('[applyHighlight] Saved caret position:', caretPos, 'text length:', text.length);
            }
            
            // Generate highlighted HTML
            const html = this.generateHighlightHTML(text);
            
            // Update HTML
            editor.innerHTML = html;
            
            // Use requestAnimationFrame to ensure the DOM is fully updated before cleaning and restoring the cursor
            requestAnimationFrame(() => {
                // Clean up redundant <br> tags and blank text nodes
                this.cleanupHighlightBrTags(editor);
                
                // Only restore cursor position when editor has focus
                if (hasFocus && caretPos > 0) {
                    // Use $nextTick to ensure cleanup is complete before restoring the cursor
                    this.$nextTick(() => {
                        // Restore cursor position (using the same traversal logic)
                        console.log('[applyHighlight] Restoring caret position:', caretPos);
                        this.restoreCaretPosition(caretPos);
                    });
                }
            });
            
            // NOTE: promptChangeHandel is no longer called here
            // Status updates of content changes should be handled in actual input events such as handleEditorInput, handlePaste, handleKeyDown, etc.
            // This is only responsible for highlighting to avoid accidentally triggering state changes during blur.
        },
        
        // Helper method: Get name based on ID
        getTargetRangeName: function(id) {
            // Using the NameHelper utility class
            return window.PromptRangeUtils.NameHelper.getName(
                this.promptFieldOpt, id, '未知靶场'
            );
        },
        
        getTargetLaneName: function(id) {
            // Find the corresponding target channel from promptOpt (use custom field name idkey)
            var lane = window.PromptRangeUtils.NameHelper.getOption(
                this.promptOpt, id, 'idkey'
            );
            return lane ? (lane.nickName || lane.label || '未知靶道') : '未知靶道';
        },
        
        getTacticalName: function(id) {
            // Using the NameHelper utility class
            return window.PromptRangeUtils.NameHelper.getName(
                this.tacticalOpt, id, '未知战术'
            );
        },
        
        getModelName: function(id) {
            // Using the NameHelper utility class
            return window.PromptRangeUtils.NameHelper.getName(
                this.modelOpt, id, '未知模型'
            );
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
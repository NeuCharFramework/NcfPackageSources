var app = new Vue({
    el: "#app",
    data() {
        return {
            isAIGrade: true,
            devHost: 'http://pr-felixj.frp.senparc.com',
            // optimizefeature
            optimizeDialogVisible: false,
            optimizeRequirement: '',
            optimizing: false,
            optimizeProgressText: '',          //Legacy comment translated to English.
            optimizeErrorText: '',             //Legacy comment translated to English.
            autoShootAfterOptimize: true,      //Legacy comment translated to English.
            autoAIGradeAfterShoot: false,      //Legacy comment translated to English.
            // PromptCatalyzer initializefeature
            promptCatalyzerInitVisible: false,      //Legacy comment translated to English.
            availableModelsForInit: [],             //Legacy comment translated to English.
            selectedModelIdForInit: null,           //Legacy comment translated to English.
            loadingModels: false,                   //Legacy comment translated to English.
            initializing: false,                    //Legacy comment translated to English.
            pageChange: false, //Legacy comment translated to English.
            isAvg: true, //Legacy comment translated to English.
            // configuration input ---start
            promptField: '', //Legacy comment translated to English.
            promptFieldOpt: [], //Legacy comment translated to English.
            promptOpt: [], //Legacy comment translated to English.
            modelOpt: [], //Legacy comment translated to English.
            vectorOpt: [], //Legacy comment translated to English.
            vectorLoading: false,
            configModelTypeLabelMap: {
                1: 'TextCompletion',
                2: 'Chat',
                3: 'TextEmbedding',
                4: 'TextToImage',
                5: 'ImageToText',
                6: 'TextToSpeech',
                7: 'SpeechToText',
                8: 'SpeechRecognition'
            },
            vectorDbTypeLabelMap: {
                0: 'Memory',
                1: 'HardDisk',
                2: 'Redis',
                3: 'Milvus',
                4: 'Chroma',
                5: 'PostgreSQL',
                6: 'Sqlite',
                7: 'SqlServer',
                8: 'Qdrant',
                17: 'VolatileInMemory'
            },
            waitRefreshModel: false, //Legacy comment translated to English.
            promptid: '',//Legacy comment translated to English.
            modelid: '',//Legacy comment translated to English.
            content: '',// prompt inputcontent
            remarks: '', //Legacy comment translated to English.
            ttsForm: {
                voice: 'alloy',
                format: 'mp3',
                speedRatio: 1.0
            },
            sttForm: {
                audioFileName: '',
                audioBase64: '',
                audioLocalRelativePath: '',
                language: '',
                fileSize: 0
            },
            sttAcceptedExtensions: '.flac,.m4a,.mp3,.mp4,.mpeg,.mpga,.oga,.ogg,.wav,.webm',
            embeddingForm: {
                vectorDbId: null,
                collectionName: '',
                searchTopK: 3,
                searchQuery: ''
            },
            isComposing: false, //Legacy comment translated to English.
            numsOfResults: 1, //Legacy comment translated to English.
            numsOfResultsOpt: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10], //Legacy comment translated to English.
            //Legacy comment translated to English.
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
            promptLeftShow: false, //Legacy comment translated to English.
            parameterViewShow: false, //Legacy comment translated to English.
            targetShootLoading: false, //Legacy comment translated to English.
            dodgersLoading: false, //Legacy comment translated to English.
            // configuration input ---end
            // promptrequestparameters ---start
            promptParamVisible: false,//Legacy comment translated to English.
            promptParamFormLoading: false,
            promptParamForm: {
                prefix: '',
                suffix: '',
                variableList: []
            },
            // promptrequestparameters ---end
            //Legacy comment translated to English.
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
            // output ---start
            outputAverageDeci: -1,//Legacy comment translated to English.
            outputMaxDeci: -1, //Legacy comment translated to English.
            outputActive: '', //Legacy comment translated to English.
            outputList: [],  //Legacy comment translated to English.
            robotScoreLoadingMap: {}, //Legacy comment translated to English.
            chartData: [], //Legacy comment translated to English.
            chartInstance: null, //Legacy comment translated to English.
            // output ---end
            // versionrecord ---start
            versionDrawer: false,//Legacy comment translated to English.
            versionSearchVal: '', // versionsearch
            versionTreeProps: {
                children: 'children',
                label: 'label'
            },
            versionTreeData: [],
            // versionrecord ---end
            //Legacy comment translated to English.
            tacticalFormVisible: false,
            tacticalFormSubmitLoading: false,
            tacticalForm: {
                tactics: '重新瞄准',
                chatMode: '对话模式' //Legacy comment translated to English.
            },
            //Legacy comment translated to English.
            tacticalChatInput: '', //Legacy comment translated to English.
            //Legacy comment translated to English.
            continueChatMode: false, //Legacy comment translated to English.
            continueChatPromptResultId: null, //Legacy comment translated to English.
            continueChatHistory: [], //Legacy comment translated to English.
            continueChatPendingUserMessageId: null, //Legacy comment translated to English.
            continueChatSystemMessage: '', //Legacy comment translated to English.
            continueChatUsageSummary: {
                promptCostToken: 0,
                resultCostToken: 0,
                totalCostToken: 0
            },
            systemMessageCollapse: [], //Legacy comment translated to English.
            promptStreamSource: null,
            promptStreamId: '',
            promptStreaming: false,
            promptStreamingTempResultId: null,
            promptStreamingBuffer: '',
            promptStreamAvailable: true,
            useStreamOutput: true,
            promptStreamPreferenceStorageKey: 'PromptRange.UseStreamOutput',
            promptRangeTexts: {
                zh: {
                    streamOptionLabel: '流式输出',
                    streamOptionTooltip: '开启后，打靶/连发/继续对话会实时输出；关闭后使用整块输出。',
                    switchOn: '开',
                    switchOff: '关',
                    streamFallbackWarning: '实时流连接失败，已自动切换为普通模式（刷新页面可重试）',
                    streamUnsupportedWarning: '当前浏览器不支持流式输出，已切换为普通模式'
                },
                en: {
                    streamOptionLabel: 'Streaming Output',
                    streamOptionTooltip: 'When enabled, shooting/rapid fire/continue chat uses streaming output. When disabled, it uses full-response output.',
                    switchOn: 'On',
                    switchOff: 'Off',
                    streamFallbackWarning: 'Streaming connection failed. Switched to normal mode automatically (refresh to retry).',
                    streamUnsupportedWarning: 'Streaming is not supported by this browser. Switched to normal mode.'
                }
            },
            //Legacy comment translated to English.
            mapDialogVisible: false, //Legacy comment translated to English.
            map3dScene: null, //Legacy comment translated to English.
            map3dCamera: null, //Legacy comment translated to English.
            map3dRenderer: null, //Legacy comment translated to English.
            map3dControls: null, //Legacy comment translated to English.
            map3dNodes: [], //Legacy comment translated to English.
            map3dTreeData: null, //Legacy comment translated to English.
            map3dClickHandler: null, //Legacy comment translated to English.
            map3dAnimationId: null, //Legacy comment translated to English.
            map3dNeedsAnimationUpdate: false, //Legacy comment translated to English.
            map3dNodeMap: new Map(), //Legacy comment translated to English.
            map3dLastAnimationTime: 0, //Legacy comment translated to English.
            map3dCurrentNodes: [], //Legacy comment translated to English.
            _map3dKeydownHandler: null, //Legacy comment translated to English.
            // range
            fieldFormVisible: false,
            fieldFormSubmitLoading: false,
            fieldForm: {
                alias: ''
            },
            //Legacy comment translated to English.
            aiScoreFormVisible: false,
            aiScoreFormSubmitLoading: false,
            aiScoreForm: {
                resultList: [{
                    id: 1,
                    label: '预期结果',
                    value: ''
                }]
            },

            // model
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
            //Legacy comment translated to English.
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
            uploadPluginVisible: false, //Legacy comment translated to English.
            uploadPluginDropAreaVisible: true,//Legacy comment translated to English.
            uploadPluginDropHover: false,//Legacy comment translated to English.
            uploadPluginData: [], //Legacy comment translated to English.
            jsZip: null, //Legacy comment translated to English.
            expectedPluginVisible: false, //Legacy comment translated to English.
            //Legacy comment translated to English.
            expectedPluginFoem: {
                checkList: [], //Legacy comment translated to English.
            },
            expectedPluginFieldList: [],
            exportPluginExpandAll: false, //Legacy comment translated to English.
            exportPluginSelectedCount: 0, //Legacy comment translated to English.
            defaultProps: {
                children: 'children',
                label: 'label'
            },
            contentTextareaRows: 14, //Legacy comment translated to English.
            dialogVisible: false,
            targetlaneName: '',
            dailogpromptOptlist: [],
            box1Hidden: false,
            box2Hidden: false,
            box3Hidden: false,
            lastClickedBox: null,
            centerAreaMaximized: false, //Legacy comment translated to English.
            rightAreaMaximized: false,  //Legacy comment translated to English.
            isBoxVisible: true, //Legacy comment translated to English.
            foldsidebarShow: false,
            //Legacy comment translated to English.
            leftAreaWidth: 360,    //Legacy comment translated to English.
            centerAreaWidth: 380,  //Legacy comment translated to English.
            isResizing: false,     //Legacy comment translated to English.
            resizeType: null,      //Legacy comment translated to English.
            resizeStartX: 0,       //Legacy comment translated to English.
            resizeStartLeftWidth: 0,   //Legacy comment translated to English.
            resizeStartCenterWidth: 0, //Legacy comment translated to English.
            //Legacy comment translated to English.
            compareDialogVisible: false,  //Legacy comment translated to English.
            comparePromptAId: null,       //Legacy comment translated to English.
            comparePromptBId: null,       //Legacy comment translated to English.
            comparePromptA: null,         //Legacy comment translated to English.
            comparePromptB: null,         //Legacy comment translated to English.
            //Legacy comment translated to English.
            createAgentDialogVisible: false,  //Legacy comment translated to English.
            createAgentLoading: false,         //Legacy comment translated to English.
            createAgentForm: {
                name: '',          // agentname
                scopeType: 'full', //Legacy comment translated to English.
                promptCode: '',    //Legacy comment translated to English.
                description: '',   // description
                functionCallNames: '' // Function Calls
            },
            createAgentExistingList: [], //Legacy comment translated to English.
            //Legacy comment translated to English.
            showScrollbarThumbnails: false,
            scrollInfo: {
                scrollTop: 0,
                scrollHeight: 0,
                clientHeight: 0
            },
            hashChangeHandler: null,
            isApplyingHashRoute: false
        };
    },
    computed: {
        isPageLoading() {
            let result = this.tacticalFormSubmitLoading || this.modelFormSubmitLoading || this.aiScoreFormSubmitLoading || this.targetShootLoading || this.dodgersLoading
            return result
        },
        isPromptExecutionLoading() {
            return this.tacticalFormSubmitLoading || this.targetShootLoading || this.dodgersLoading || this.promptStreaming
        },
        selectedModelOption() {
            const currentModelId = this.modelid
            if (!currentModelId || !Array.isArray(this.modelOpt)) {
                return null
            }
            return this.modelOpt.find(item => String(item.value) === String(currentModelId)) || null
        },
        currentModelTypeCode() {
            if (!this.selectedModelOption) {
                return 0
            }
            return this.parseConfigModelTypeValue(this.selectedModelOption.configModelTypeCode || this.selectedModelOption.configModelType)
        },
        isChatModelSelected() {
            return this.currentModelTypeCode === 2
        },
        isTextEmbeddingModelSelected() {
            return this.currentModelTypeCode === 3
        },
        isTextToImageModelSelected() {
            return this.currentModelTypeCode === 4
        },
        isTextToSpeechModelSelected() {
            return this.currentModelTypeCode === 6
        },
        isSpeechToTextModelSelected() {
            return this.currentModelTypeCode === 7 || this.currentModelTypeCode === 8
        },
        supportsChatMode() {
            return this.isChatModelSelected
        },
        promptInputTitle() {
            if (this.isSpeechToTextModelSelected) {
                return '语音识别输入'
            }
            return '输入Prompt'
        },
        shouldShowPromptEditor() {
            return !this.isSpeechToTextModelSelected
        },
        
        //Legacy comment translated to English.
        createAgentPromptCode() {
            if (!this.promptDetail || !this.promptDetail.fullVersion) {
                return ''
            }
            const fullVersion = this.promptDetail.fullVersion
            const parts = fullVersion.split('-')
            const rangeName = parts[0] || ''
            const tacticPart = parts[1] || ''
            
            switch (this.createAgentForm.scopeType) {
                case 'range':
                    return rangeName
                case 'tactic':
                    return tacticPart ? `${rangeName}-${tacticPart}` : rangeName
                case 'full':
                default:
                    return fullVersion
            }
        },
        
        //Legacy comment translated to English.
        availablePrompts() {
            return this.promptOpt || [];
        },
        
        //Legacy comment translated to English.
        comparePromptAInfo() {
            if (!this.comparePromptA) return null;
            
            //Legacy comment translated to English.
            const versionParts = this.comparePromptA.fullVersion ? this.comparePromptA.fullVersion.split('-') : [];
            
            return {
                targetRangeName: versionParts[0] || '未知靶场',
                targetLaneName: versionParts[1] || '未知靶道',
                tacticalName: versionParts[2] || '未知战术',
                modelName: this.getModelName(this.comparePromptA.modelId)
            };
        },
        
        //Legacy comment translated to English.
        comparePromptBInfo() {
            if (!this.comparePromptB) return null;
            
            //Legacy comment translated to English.
            const versionParts = this.comparePromptB.fullVersion ? this.comparePromptB.fullVersion.split('-') : [];
            
            return {
                targetRangeName: versionParts[0] || '未知靶场',
                targetLaneName: versionParts[1] || '未知靶道',
                tacticalName: versionParts[2] || '未知战术',
                modelName: this.getModelName(this.comparePromptB.modelId)
            };
        },
        
        //Legacy comment translated to English.
        isSamePrompt() {
            if (!this.comparePromptA || !this.comparePromptB) return false;
            if (!this.comparePromptAId || !this.comparePromptBId) return false;
            
            //Legacy comment translated to English.
            return this.comparePromptAId === this.comparePromptBId;
        },
        
        //Legacy comment translated to English.
        currentTacticalInfo() {
            if (!this.promptDetail || !this.promptDetail.fullVersion) {
                return {
                    tactic: '--',
                    fullTactical: '--'
                };
            }
            
            //Legacy comment translated to English.
            // for example：2023.12.14.1-T1.2.3-A1
            const versionParts = this.promptDetail.fullVersion.split('-');
            if (versionParts.length < 2) {
                return {
                    tactic: '--',
                    fullTactical: '--'
                };
            }
            
            // versionParts[0] = RangeName (for example：2023.12.14.1)
            // versionParts[1] = T{Tactic} (for example：T1.2.3)
            //Legacy comment translated to English.
            const tacticPart = versionParts[1] || ''; // T1.2.3
            const aimingPart = versionParts[2] || ''; // A1
            
            return {
                tactic: tacticPart || '--',
                fullTactical: aimingPart ? `${tacticPart}-${aimingPart}` : tacticPart || '--'
            };
        },
        
        //Legacy comment translated to English.
        nextTacticalNumbers() {
            if (!this.promptDetail || !this.promptDetail.fullVersion) {
                return {
                    topTactic: 'T1',
                    parallelTactic: 'T1.2.4',
                    subTactic: 'T1.2.3.1',
                    newAiming: 'T1.2.3-A2'
                };
            }
            
            //Legacy comment translated to English.
            // for example：2023.12.14.1-T1.2.3-A1
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
            // versionParts[1] = T{Tactic} (for example：T1.2.3)
            //Legacy comment translated to English.
            let tacticPart = versionParts[1] || ''; // T1.2.3
            let aimingPart = versionParts[2] || 'A1'; // A1
            
            //Legacy comment translated to English.
            if (!tacticPart.startsWith('T')) {
                //Legacy comment translated to English.
                return {
                    topTactic: 'T1',
                    parallelTactic: 'T1.2.4',
                    subTactic: 'T1.2.3.1',
                    newAiming: 'T1.2.3-A2'
                };
            }
            
            //Legacy comment translated to English.
            const currentAiming = aimingPart.replace(/^A/, '');
            const aimingNumber = parseInt(currentAiming) || 1;
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            const tacticStr = tacticPart.replace(/^T/, ''); // 1.2.3
            if (!tacticStr || tacticStr.length === 0) {
                //Legacy comment translated to English.
                return {
                    topTactic: 'T1',
                    parallelTactic: 'T1.2.4',
                    subTactic: 'T1.2.3.1',
                    newAiming: 'T1.2.3-A2'
                };
            }
            
            //Legacy comment translated to English.
            const tacticNumbers = [];
            const parts = tacticStr.split('.');
            for (let i = 0; i < parts.length; i++) {
                const num = parseInt(parts[i]);
                if (isNaN(num)) {
                    //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                return {
                    topTactic: 'T1',
                    parallelTactic: 'T1.2.4',
                    subTactic: 'T1.2.3.1',
                    newAiming: 'T1.2.3-A2'
                };
            }
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            const currentTopTactic = tacticNumbers[0] || 1;
            const nextTopTactic = currentTopTactic + 1;
            
            //Legacy comment translated to English.
            // for example：T1.2.3 -> T1.2.4
            const nextParallelTactic = [...tacticNumbers];
            if (nextParallelTactic.length > 0) {
                nextParallelTactic[nextParallelTactic.length - 1] = nextParallelTactic[nextParallelTactic.length - 1] + 1;
            }
            
            //Legacy comment translated to English.
            // for example：T1.2.3 -> T1.2.3.1
            const nextSubTactic = [...tacticNumbers, 1];
            
            //Legacy comment translated to English.
            // for example：T1.2.3-A1 -> T1.2.3-A2
            const nextAiming = aimingNumber + 1;
            
            return {
                topTactic: `T${nextTopTactic}`,
                parallelTactic: `T${nextParallelTactic.join('.')}`,
                subTactic: `T${nextSubTactic.join('.')}`,
                newAiming: `${tacticPart}-A${nextAiming}`
            };
        },
        
        //Legacy comment translated to English.
        detectedVariables() {
            if (!this.content) return [];
            
            const prefix = this.promptParamForm.prefix || '';
            const suffix = this.promptParamForm.suffix || '';
            const variableList = this.promptParamForm.variableList || [];
            
            //Legacy comment translated to English.
            if (!prefix || !suffix) return [];
            
            //Legacy comment translated to English.
            const escapedPrefix = prefix.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            const escapedSuffix = suffix.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            
            //Legacy comment translated to English.
            const regex = new RegExp(`${escapedPrefix}(\\w+)${escapedSuffix}`, 'g');
            
            //Legacy comment translated to English.
            const definedVarNames = variableList.map(v => v.name).filter(n => n);
            
            //Legacy comment translated to English.
            const variables = [];
            const seen = new Set();
            let match;
            
            while ((match = regex.exec(this.content)) !== null) {
                const fullMatch = match[0];
                const varName = match[1];
                
                //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        content(newVal, oldVal) {
            if (newVal === this._editorContentStamp) return;

            const editor = this.$refs.promptEditor;
            if (!editor || this.isComposing) return;
            
            const currentText = this.getEditorText();
            //Legacy comment translated to English.
            if (currentText !== newVal && newVal !== oldVal) {
                this.$nextTick(() => {
                    if (this.isComposing || this.content !== newVal || newVal === this._editorContentStamp) {
                        console.log('[IME] skip external content render during composition');
                        return;
                    }
                    const latestEditor = this.$refs.promptEditor;
                    if (!latestEditor) return;
                    if (this.getEditorText() === newVal) return;
                    console.log('[prompt editor] apply external content to editor, length:', (newVal || '').length);
                    const html = this.generateHighlightHTML(newVal);
                    latestEditor.innerHTML = html;
                });
            }
        }
    },
    created() {
        this.loadPromptStreamPreference();
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        if (!this.isMacEmbeddedWebView()) {
            window.addEventListener('beforeunload', this.beforeunloadHandler);
        }
        
        //Legacy comment translated to English.
        this.loadAreaWidthsFromStorage();
    },
    mounted() {
        //Legacy comment translated to English.
        setTimeout(async () => {
            await this.getFieldList()
            //Legacy comment translated to English.
            this.getModelOptData()

            this.hashChangeHandler = () => {
                this.applyPromptHashRoute()
            }
            window.addEventListener('hashchange', this.hashChangeHandler)
            const alias = new URLSearchParams(window.location.search || '').get('alias')
            const rangeOption = alias
                ? this.promptFieldOpt.find(item => item.alias === alias)
                : null
            if (rangeOption) {
                this.pageChange = false
                this.promptField = rangeOption.value
                await this.promptChangeHandel(this.promptField, 'promptField', null, true)
            }
            await this.applyPromptHashRoute()
          
        }, 100)
        //Legacy comment translated to English.
        // this.getScoringTrendData()
        //Legacy comment translated to English.
        const self = this;
        const viewElem = document.body;
        const resizeObserver = new ResizeObserver(() => {
            //Legacy comment translated to English.
            if (self.chartInstance) {
                self.chartInstance.resize();
            }
        });
        resizeObserver.observe(viewElem);
        //Legacy comment translated to English.
        this.$nextTick(() => {
            const editor = this.$refs.promptEditor;
            if (editor && this.content) {
                const html = this.generateHighlightHTML(this.content);
                editor.innerHTML = html;
            }
        });
        
        //Legacy comment translated to English.
        this.initCodeCopyButtons();
      
    },
    beforeDestroy() {
        //Legacy comment translated to English.
        window.removeEventListener('beforeunload', this.beforeunloadHandler);
        this.closePromptStream();
        if (this.hashChangeHandler) {
            window.removeEventListener('hashchange', this.hashChangeHandler);
            this.hashChangeHandler = null;
        }
        
        //Legacy comment translated to English.
        document.removeEventListener('mousemove', this.handleResize);
        document.removeEventListener('mouseup', this.stopResize);
        this.clearHighlightTimer();
        if (this._compositionFlushTimer) {
            clearTimeout(this._compositionFlushTimer);
            this._compositionFlushTimer = null;
        }
        if (this._imeFallbackTimer) {
            clearTimeout(this._imeFallbackTimer);
            this._imeFallbackTimer = null;
        }
    },
    methods: {
        getPromptRangeLocale() {
            const raw = (document.documentElement.lang || navigator.language || 'en').toLowerCase();
            if (raw.startsWith('zh')) {
                return 'zh';
            }
            return 'en';
        },
        getPromptRangeText(key) {
            const locale = this.getPromptRangeLocale();
            const localeTexts = this.promptRangeTexts[locale] || this.promptRangeTexts.en || {};
            return localeTexts[key] || (this.promptRangeTexts.en && this.promptRangeTexts.en[key]) || key;
        },
        loadPromptStreamPreference() {
            if (typeof EventSource === 'undefined') {
                this.useStreamOutput = false;
                this.promptStreamAvailable = false;
                return;
            }

            let savedValue = null;
            try {
                savedValue = window.localStorage.getItem(this.promptStreamPreferenceStorageKey);
            } catch (e) {
                savedValue = null;
            }

            if (savedValue === 'false' || savedValue === '0') {
                this.useStreamOutput = false;
            } else if (savedValue === 'true' || savedValue === '1') {
                this.useStreamOutput = true;
            }
        },
        savePromptStreamPreference(value) {
            try {
                window.localStorage.setItem(this.promptStreamPreferenceStorageKey, value ? 'true' : 'false');
            } catch (e) {
                // ignore storage errors (private mode / quota / policy)
            }
        },
        handleUseStreamOutputChange(value) {
            if (value && typeof EventSource === 'undefined') {
                this.useStreamOutput = false;
                this.promptStreamAvailable = false;
                this.savePromptStreamPreference(false);
                this.$message({
                    message: this.getPromptRangeText('streamUnsupportedWarning'),
                    type: 'warning',
                    duration: 3500
                });
                return;
            }

            if (!value) {
                this.closePromptStream();
            } else {
                this.promptStreamAvailable = true;
            }

            this.savePromptStreamPreference(!!value);
        },
        parsePromptHashRoute() {
            const raw = (window.location.hash || '').replace(/^#/, '');
            const route = { rangeId: null, promptId: null };
            if (raw) {
                const params = new URLSearchParams(raw);
                route.rangeId = Number(params.get('rangeId') || 0) || null;
                route.promptId = Number(params.get('promptId') || 0) || null;
            }

            // Backward compatibility: old query route (targetrangeId/targetlaneId)
            if ((!route.rangeId || !route.promptId) && this.$route && this.$route.query) {
                route.rangeId = route.rangeId || (Number(this.$route.query.targetrangeId || 0) || null);
                route.promptId = route.promptId || (Number(this.$route.query.targetlaneId || 0) || null);
            }
            return route;
        },
        setPromptHashRoute(route) {
            if (this.isApplyingHashRoute) {
                return;
            }
            const params = new URLSearchParams();
            const rangeId = Number(route.rangeId || 0) || null;
            const promptId = Number(route.promptId || 0) || null;
            if (rangeId) {
                params.set('rangeId', String(rangeId));
            }
            if (promptId) {
                params.set('promptId', String(promptId));
            }
            const nextHash = params.toString();
            if ((window.location.hash || '').replace(/^#/, '') === nextHash) {
                return;
            }
            window.location.hash = nextHash;
        },
        buildPromptHashRoute(extra = {}) {
            const base = {
                rangeId: Number(this.promptField || 0) || null,
                promptId: Number(this.promptid || 0) || null
            };
            return Object.assign(base, extra);
        },
        navigatePromptByHash(route) {
            this.setPromptHashRoute(route);
            this.applyPromptHashRoute();
        },
        async applyPromptHashRoute() {
            if (this.isApplyingHashRoute) {
                return;
            }
            const route = this.parsePromptHashRoute();
            if (!route.rangeId && !route.promptId) {
                return;
            }

            this.isApplyingHashRoute = true;
            try {
                if (!Array.isArray(this.promptFieldOpt) || this.promptFieldOpt.length === 0) {
                    await this.getFieldList();
                }

                if (route.rangeId) {
                    const rangeOption = this.promptFieldOpt.find(item => String(item.value) === String(route.rangeId) || String(item.id) === String(route.rangeId));
                    if (rangeOption) {
                        this.promptField = rangeOption.value;
                        await this.promptChangeHandel(this.promptField, 'promptField', null, true);
                    }
                }

                if (route.promptId) {
                    const promptOption = (this.promptOpt || []).find(item => String(item.value) === String(route.promptId) || String(item.id) === String(route.promptId));
                    if (promptOption) {
                        this.promptid = promptOption.value;
                        await this.promptChangeHandel(this.promptid, 'promptid', null, true);
                    }
                }
            } finally {
                this.isApplyingHashRoute = false;
            }
        },
        createPromptStreamId() {
            return `pr_stream_${Date.now()}_${Math.random().toString(36).slice(2, 10)}`
        },
        closePromptStream() {
            if (this.promptStreamSource) {
                this.promptStreamSource.close()
            }
            if (this._resultBoxScrollRafId) {
                cancelAnimationFrame(this._resultBoxScrollRafId)
                this._resultBoxScrollRafId = null
            }
            this.promptStreamSource = null
            this.promptStreamId = ''
            this.promptStreaming = false
            this.promptStreamingBuffer = ''
            this.promptStreamingTempResultId = null
        },
        clearOutputGeneratingPlaceholder() {
            if (!Array.isArray(this.outputList) || this.outputList.length === 0) {
                this.promptStreamingTempResultId = null
                return
            }

            const knownId = this.promptStreamingTempResultId
            this.outputList = this.outputList.filter(item => {
                if (!item) return false
                if (item._generating === true) return false
                const isKnownTemp = knownId
                    && String(item.id) === String(knownId)
                    && typeof item.id === 'string'
                    && item.id.indexOf('streaming_') === 0
                if (isKnownTemp) return false
                return true
            })
            this.promptStreamingTempResultId = null
        },
        cleanupOutputListAfterResultUpsert() {
            if (!Array.isArray(this.outputList) || this.outputList.length === 0) {
                return
            }

            const seenPersistentIds = new Set()
            const normalizedList = []

            for (let i = 0; i < this.outputList.length; i++) {
                const item = this.outputList[i]
                if (!item) {
                    continue
                }

                const idStr = item.id === undefined || item.id === null ? '' : String(item.id)
                const isStreamingTemp = idStr.indexOf('streaming_') === 0

                //Legacy comment translated to English.
                if (isStreamingTemp && item._generating !== true) {
                    continue
                }

                if (!isStreamingTemp && idStr) {
                    if (seenPersistentIds.has(idStr)) {
                        continue
                    }
                    seenPersistentIds.add(idStr)
                }

                normalizedList.push(item)
            }

            if (normalizedList.length !== this.outputList.length) {
                this.outputList = normalizedList
            }
        },
        ensureOutputGeneratingPlaceholder() {
            this.clearOutputGeneratingPlaceholder()
            this.cleanupOutputListAfterResultUpsert()
            if (!this.promptStreamingTempResultId) {
                this.promptStreamingTempResultId = `streaming_${Date.now()}`
            }

            const placeholder = this.normalizeOutputResultItem({
                id: this.promptStreamingTempResultId,
                resultString: 'Generating...',
                addTime: new Date().toISOString(),
                costTime: 0,
                promptCostToken: 0,
                resultCostToken: 0,
                totalCostToken: 0
            }, this.promptid, this.promptDetail?.fullVersion || '')

            placeholder._generating = true
            this.outputList = (this.outputList || []).concat([placeholder])
            this.forceScrollResultBoxToBottom()
            return placeholder.id
        },
        forceScrollResultBoxToBottom() {
            this.$nextTick(() => {
                const container = document.getElementById('resultBox')
                if (!container) {
                    return
                }
                container.scrollTop = container.scrollHeight
            })
        },
        scheduleResultBoxFollowBottom() {
            if (this._resultBoxScrollRafId) {
                return
            }
            this._resultBoxScrollRafId = requestAnimationFrame(() => {
                this._resultBoxScrollRafId = null
                const container = document.getElementById('resultBox')
                if (!container) {
                    return
                }
                container.scrollTop = container.scrollHeight
            })
        },
        ensureContinueChatGeneratingMessage() {
            const now = new Date().toISOString()
            let tempMessage = this.continueChatHistory.find(msg => msg.id === 'streaming_assistant')
            if (!tempMessage) {
                tempMessage = {
                    id: 'streaming_assistant',
                    roleType: 2,
                    content: 'Generating...',
                    sequence: (this.continueChatHistory[this.continueChatHistory.length - 1]?.sequence || 0) + 1,
                    addTime: now,
                    _generating: true
                }
                this.continueChatHistory.push(tempMessage)
            } else {
                if (tempMessage._generating !== true && String(tempMessage.content || '').trim()) {
                    return
                }
                tempMessage.content = 'Generating...'
                tempMessage.addTime = now
                tempMessage._generating = true
            }
            this.continueChatHistory = [...this.continueChatHistory]
            this.$nextTick(() => {
                const container = document.getElementById('chatHistoryContainer')
                if (container) {
                    container.scrollTop = container.scrollHeight
                }
            })
        },
        openPromptStream(streamId, mode = 'shoot') {
            if (!this.useStreamOutput || !streamId || typeof EventSource === 'undefined' || !this.promptStreamAvailable) {
                return Promise.resolve(false)
            }

            this.closePromptStream()
            this.promptStreamId = streamId
            this.promptStreaming = true

            return new Promise((resolve) => {
                const source = new EventSource(`/api/Senparc.Xncf.PromptRange/PromptStream/Subscribe?streamId=${encodeURIComponent(streamId)}&_ts=${Date.now()}`, { withCredentials: true })
                this.promptStreamSource = source
                let settled = false
                const settle = (ok) => {
                    if (settled) return
                    settled = true
                    clearTimeout(timeoutId)
                    resolve(ok)
                }
                const timeoutId = setTimeout(() => settle(true), 8000)

                source.addEventListener('chunk', (event) => {
                    const payload = this.safeParsePromptStreamEvent(event)
                    if (!payload) return
                    this.handlePromptStreamChunk(payload, mode)
                })

                source.addEventListener('final', (event) => {
                    const payload = this.safeParsePromptStreamEvent(event)
                    if (!payload) return
                    this.handlePromptStreamFinal(payload, mode)
                })

                source.addEventListener('complete', () => {
                    this.closePromptStream()
                })

                source.onopen = () => {
                    this.promptStreamAvailable = true
                    settle(true)
                }

                source.onerror = () => {
                    if (source.readyState === EventSource.CONNECTING) {
                        return
                    }
                    if (source.readyState === EventSource.CLOSED && !settled) {
                        if (this.promptStreamAvailable) {
                            this.promptStreamAvailable = false
                            this.$message({
                                message: this.getPromptRangeText('streamFallbackWarning'),
                                type: 'warning',
                                duration: 3500
                            })
                        }
                        settle(false)
                    }
                    if (source.readyState === EventSource.CLOSED) {
                        this.closePromptStream()
                    }
                }
            })
        },
        safeParsePromptStreamEvent(event) {
            if (!event || !event.data) return null
            try {
                return JSON.parse(event.data)
            } catch (e) {
                console.error('Prompt stream parse error:', e, event.data)
                return null
            }
        },
        buildDefaultOutputResultItem() {
            return {
                scoreType: '1',
                isScoreView: false,
                scoreVal: 0,
                alResultList: [{
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
        },
        normalizeOutputResultItem(item, promptId, fullVersion, promptItem = {}) {
            const normalized = Object.assign({}, this.buildDefaultOutputResultItem(), item || {})
            normalized.promptId = promptId || normalized.promptId
            normalized.version = fullVersion || normalized.version
            normalized.addTime = normalized.addTime ? this.formatDate(normalized.addTime) : ''
            normalized.resultString = normalized.resultString || ''
            this.applyPromptResultView(normalized)
            normalized.scoreVal = normalized.humanScore > -1 ? normalized.humanScore : 0

            if (promptItem && promptItem.expectedResultsJson) {
                try {
                    const expectedList = JSON.parse(promptItem.expectedResultsJson)
                    normalized.alResultList = expectedList.map((value, index) => ({
                        id: index + 1,
                        label: '预期结果',
                        value
                    }))
                } catch (e) {
                    console.warn('expectedResultsJson parse failed', e)
                }
            }

            return normalized
        },
        applyPromptResultView(resultItem) {
            if (!resultItem) {
                return
            }

            const typedPayload = this.tryParseTypedPromptPayload(resultItem.resultString)
            if (typedPayload) {
                const resultType = (typedPayload.__promptRangeResultType || '').toString().trim()
                if (resultType === 'TextToImage') {
                    resultItem.resultViewType = 'TextToImage'
                    resultItem.textToImage = this.buildTextToImageViewModel(typedPayload)
                    resultItem.textToSpeech = null
                    resultItem.speechToText = null
                    resultItem.textEmbedding = null

                    const safeImageUrl = this.safeHttpUrl(resultItem.textToImage.imageUrl)
                    const previewHtml = safeImageUrl
                        ? `<p><img src="${this.escapeHtml(safeImageUrl)}" alt="generated image" /></p>`
                        : '<p>' + ncfST('图片已生成，但未获得可访问地址。') + '</p>'
                    const promptHtml = resultItem.textToImage.revisedPrompt
                        ? `<p>${this.escapeHtml(resultItem.textToImage.revisedPrompt)}</p>`
                        : ''
                    resultItem.resultStringHtml = `${previewHtml}${promptHtml}`
                    return
                }

                if (resultType === 'TextToSpeech') {
                    resultItem.resultViewType = 'TextToSpeech'
                    resultItem.textToImage = null
                    resultItem.speechToText = null
                    resultItem.textEmbedding = null
                    resultItem.textToSpeech = this.buildTextToSpeechViewModel(typedPayload)
                    resultItem.resultStringHtml = this.renderSafeMarkdown(resultItem.textToSpeech.text || '')
                    return
                }

                if (resultType === 'SpeechToText') {
                    resultItem.resultViewType = 'SpeechToText'
                    resultItem.textToImage = null
                    resultItem.textToSpeech = null
                    resultItem.textEmbedding = null
                    resultItem.speechToText = this.buildSpeechToTextViewModel(typedPayload)
                    resultItem.resultStringHtml = this.renderSafeMarkdown(resultItem.speechToText.transcript || '')
                    if (resultItem.speechToText.sourceAudioLocalRelativePath) {
                        this.sttForm.audioLocalRelativePath = resultItem.speechToText.sourceAudioLocalRelativePath
                        this.sttForm.audioBase64 = ''
                        if (!this.sttForm.audioFileName) {
                            this.sttForm.audioFileName = resultItem.speechToText.sourceAudioFileName || ''
                        }
                    }
                    return
                }

                if (resultType === 'TextEmbedding') {
                    resultItem.resultViewType = 'TextEmbedding'
                    resultItem.textToImage = null
                    resultItem.textToSpeech = null
                    resultItem.speechToText = null
                    resultItem.textEmbedding = this.buildTextEmbeddingViewModel(typedPayload)
                    this.ensureTextEmbeddingResultState(resultItem)
                    resultItem.resultStringHtml = this.renderSafeMarkdown(resultItem.textEmbedding.sourceText || '')
                    return
                }
            }

            resultItem.resultViewType = 'Markdown'
            resultItem.textToImage = null
            resultItem.textToSpeech = null
            resultItem.speechToText = null
            resultItem.textEmbedding = null
            resultItem.resultStringHtml = this.renderSafeMarkdown(resultItem.resultString || '')
        },
        tryParseTypedPromptPayload(rawResultString) {
            if (!rawResultString || typeof rawResultString !== 'string') {
                return null
            }

            const trimmed = rawResultString.trim()
            if (!trimmed || trimmed[0] !== '{') {
                return null
            }

            try {
                const parsed = JSON.parse(trimmed)
                if (!parsed || typeof parsed !== 'object') {
                    return null
                }

                return parsed.__promptRangeResultType ? parsed : null
            } catch (e) {
                return null
            }
        },
        buildTextToImageViewModel(payload) {
            const localRelativePath = (payload.localRelativePath || '').toString().trim()
            const remoteImageUrl = (payload.remoteImageUrl || '').toString().trim()
            const directImageUrl = (payload.imageUrl || '').toString().trim()
            const normalizedStorageType = (payload.storageType || '').toString().trim() || 'Unknown'

            let imageUrl = directImageUrl
            if (!imageUrl && localRelativePath) {
                imageUrl = this.buildPromptImageUrlByPath(localRelativePath)
            }
            if (!imageUrl && remoteImageUrl) {
                imageUrl = remoteImageUrl
            }

            return {
                imageUrl,
                storageType: normalizedStorageType,
                localRelativePath,
                remoteImageUrl,
                prompt: payload.prompt || '',
                revisedPrompt: payload.revisedPrompt || '',
                contentType: payload.contentType || '',
                width: payload.width || 0,
                height: payload.height || 0
            }
        },
        buildTextToSpeechViewModel(payload) {
            const localRelativePath = (payload.localRelativePath || '').toString().trim()
            const directAudioUrl = (payload.audioUrl || '').toString().trim()
            let audioUrl = directAudioUrl
            if (!audioUrl && localRelativePath) {
                audioUrl = this.buildPromptAudioUrlByPath(localRelativePath)
            }

            return {
                text: payload.text || '',
                voice: payload.voice || '',
                format: payload.format || '',
                speedRatio: payload.speedRatio || 1.0,
                contentType: payload.contentType || '',
                storageType: payload.storageType || '',
                localRelativePath,
                audioUrl
            }
        },
        buildSpeechToTextViewModel(payload) {
            const sourceAudioLocalRelativePath = (payload.sourceAudioLocalRelativePath || '').toString().trim()
            const sourceAudioUrl = (payload.sourceAudioUrl || '').toString().trim()
            let audioUrl = sourceAudioUrl
            if (!audioUrl && sourceAudioLocalRelativePath) {
                audioUrl = this.buildPromptAudioUrlByPath(sourceAudioLocalRelativePath)
            }

            return {
                transcript: payload.transcript || '',
                language: payload.language || '',
                duration: payload.duration || '',
                sourceAudioFileName: payload.sourceAudioFileName || '',
                sourceAudioContentType: payload.sourceAudioContentType || '',
                sourceAudioLocalRelativePath,
                sourceAudioUrl: audioUrl
            }
        },
        buildTextEmbeddingViewModel(payload) {
            const searchHits = Array.isArray(payload.searchHits) ? payload.searchHits : []
            return {
                vectorDbId: payload.vectorDbId || 0,
                vectorDbAlias: payload.vectorDbAlias || '',
                vectorDbType: payload.vectorDbType || '',
                collectionName: payload.collectionName || '',
                sourceText: payload.sourceText || '',
                sourceTextLength: payload.sourceTextLength || 0,
                embeddingDimension: payload.embeddingDimension || 0,
                upsertSourceId: payload.upsertSourceId || '',
                upsertSourceName: payload.upsertSourceName || '',
                searchQuery: payload.searchQuery || '',
                searchTopK: payload.searchTopK || 3,
                searchHits: searchHits.map(hit => ({
                    sourceId: hit.sourceId || '',
                    sourceName: hit.sourceName || '',
                    sourceLink: hit.sourceLink || '',
                    text: hit.text || '',
                    score: typeof hit.score === 'number' ? hit.score : Number(hit.score || 0)
                }))
            }
        },
        ensureTextEmbeddingResultState(resultItem) {
            if (!resultItem || !resultItem.textEmbedding) {
                return
            }
            if (resultItem.embeddingQueryText === undefined) {
                resultItem.embeddingQueryText = resultItem.textEmbedding.searchQuery || ''
            }
            if (resultItem.embeddingQueryTopK === undefined) {
                resultItem.embeddingQueryTopK = resultItem.textEmbedding.searchTopK || 3
            }
            if (!Array.isArray(resultItem.embeddingQueryResults)) {
                resultItem.embeddingQueryResults = resultItem.textEmbedding.searchHits || []
            }
            if (resultItem.embeddingQueryLoading === undefined) {
                resultItem.embeddingQueryLoading = false
            }
        },
        async runTextEmbeddingQuery(resultItem) {
            if (!resultItem || !resultItem.id) {
                return
            }
            const query = (resultItem.embeddingQueryText || '').trim()
            const topK = Number(resultItem.embeddingQueryTopK || 3)
            if (!query) {
                this.$message({
                    message: '请输入要查询的文本',
                    type: 'warning',
                    duration: 3000
                })
                return
            }

            resultItem.embeddingQueryLoading = true
            try {
                const res = await servicePR.post('/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.TextEmbeddingSearch', {
                    promptResultId: resultItem.id,
                    query: query,
                    topK: topK
                })
                if (!res.data.success) {
                    this.$message({
                        message: res.data.errorMessage || res.data.data || 'Embedding 查询失败',
                        type: 'error',
                        duration: 5 * 1000
                    })
                    return
                }
                const hits = Array.isArray(res.data.data && res.data.data.hits) ? res.data.data.hits : []
                resultItem.embeddingQueryResults = hits.map(hit => ({
                    sourceId: hit.sourceId || '',
                    sourceName: hit.sourceName || '',
                    sourceLink: hit.sourceLink || '',
                    text: hit.text || '',
                    score: typeof hit.score === 'number' ? hit.score : Number(hit.score || 0)
                }))
            } catch (e) {
                this.$message({
                    message: 'Embedding 查询失败：' + (e.message || '未知错误'),
                    type: 'error',
                    duration: 5 * 1000
                })
            } finally {
                resultItem.embeddingQueryLoading = false
            }
        },
        buildPromptImageUrlByPath(localRelativePath) {
            const normalized = (localRelativePath || '')
                .toString()
                .replace(/\\/g, '/')
                .replace(/^\/+/, '')
            if (!normalized) {
                return ''
            }
            return `/api/Senparc.Xncf.PromptRange/PromptImage/Get?path=${encodeURIComponent(normalized)}`
        },
        buildPromptAudioUrlByPath(localRelativePath) {
            const normalized = (localRelativePath || '')
                .toString()
                .replace(/\\/g, '/')
                .replace(/^\/+/, '')
            if (!normalized) {
                return ''
            }
            return `/api/Senparc.Xncf.PromptRange/PromptAudio/Get?path=${encodeURIComponent(normalized)}`
        },
        parseConfigModelTypeValue(typeValue) {
            if (typeValue === undefined || typeValue === null) {
                return 0
            }

            if (typeof typeValue === 'string') {
                const trimmed = typeValue.trim()
                if (!trimmed) {
                    return 0
                }
                if (/^\d+$/.test(trimmed)) {
                    return Number(trimmed)
                }

                const entry = Object.entries(this.configModelTypeLabelMap).find(([, name]) => name.toLowerCase() === trimmed.toLowerCase())
                return entry ? Number(entry[0]) : 0
            }

            const code = Number(typeValue)
            return Number.isFinite(code) ? code : 0
        },
        getConfigModelTypeLabel(typeValue) {
            const code = this.parseConfigModelTypeValue(typeValue)
            return this.configModelTypeLabelMap[code] || 'Unknown'
        },
        parseVectorDbTypeValue(typeValue) {
            if (typeValue === undefined || typeValue === null) {
                return -1
            }

            if (typeof typeValue === 'string') {
                const trimmed = typeValue.trim()
                if (!trimmed) {
                    return -1
                }
                if (/^\d+$/.test(trimmed)) {
                    return Number(trimmed)
                }

                const entry = Object.entries(this.vectorDbTypeLabelMap).find(([, name]) => name.toLowerCase() === trimmed.toLowerCase())
                return entry ? Number(entry[0]) : -1
            }

            const code = Number(typeValue)
            return Number.isFinite(code) ? code : -1
        },
        getVectorDbTypeLabel(typeValue) {
            const code = this.parseVectorDbTypeValue(typeValue)
            return this.vectorDbTypeLabelMap[code] || 'Unknown'
        },
        isInMemoryVectorDbType(typeValue) {
            const code = this.parseVectorDbTypeValue(typeValue)
            return code === 0 || code === 17
        },
        syncContinueChatUsageFromResult(promptResultId) {
            const resultItem = this.outputList.find(item => item.id === promptResultId)
            if (!resultItem) {
                this.continueChatUsageSummary = {
                    promptCostToken: 0,
                    resultCostToken: 0,
                    totalCostToken: 0
                }
                return
            }

            this.continueChatUsageSummary = {
                promptCostToken: resultItem.promptCostToken || 0,
                resultCostToken: resultItem.resultCostToken || 0,
                totalCostToken: resultItem.totalCostToken || 0
            }
        },
        removeContinueChatStreamingMessage() {
            const tempIndex = this.continueChatHistory.findIndex(msg => msg.id === 'streaming_assistant')
            if (tempIndex > -1) {
                this.continueChatHistory.splice(tempIndex, 1)
                this.continueChatHistory = [...this.continueChatHistory]
            }
        },
        appendContinueChatPendingUserMessage(userMessage) {
            const content = typeof userMessage === 'string' ? userMessage.trim() : ''
            if (!content) {
                return null
            }

            this.removeContinueChatPendingUserMessage()
            const tempId = `streaming_user_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`
            const sequence = (this.continueChatHistory[this.continueChatHistory.length - 1]?.sequence || 0) + 1
            this.continueChatPendingUserMessageId = tempId
            this.continueChatHistory.push({
                id: tempId,
                roleType: 1,
                content,
                sequence,
                addTime: new Date().toISOString(),
                _pending: true
            })
            this.continueChatHistory = [...this.continueChatHistory]
            this.$nextTick(() => {
                const container = document.getElementById('chatHistoryContainer')
                if (container) {
                    container.scrollTop = container.scrollHeight
                }
            })
            return tempId
        },
        confirmContinueChatPendingUserMessage() {
            const pendingId = this.continueChatPendingUserMessageId
            if (!pendingId) {
                return
            }

            const tempMessage = this.continueChatHistory.find(msg => String(msg?.id) === String(pendingId))
            if (tempMessage) {
                tempMessage._pending = false
                this.continueChatHistory = [...this.continueChatHistory]
            }
            this.continueChatPendingUserMessageId = null
        },
        removeContinueChatPendingUserMessage() {
            const pendingId = this.continueChatPendingUserMessageId
            if (!pendingId) {
                return
            }

            const tempIndex = this.continueChatHistory.findIndex(msg => String(msg?.id) === String(pendingId))
            if (tempIndex > -1) {
                this.continueChatHistory.splice(tempIndex, 1)
                this.continueChatHistory = [...this.continueChatHistory]
            }
            this.continueChatPendingUserMessageId = null
        },
        removeGeneratingOutputPlaceholdersExcept(keepId) {
            if (!Array.isArray(this.outputList) || this.outputList.length === 0) {
                return
            }

            const keepIdStr = keepId !== undefined && keepId !== null ? String(keepId) : ''
            let changed = false
            let keptGenerating = false
            const filtered = []

            for (let i = 0; i < this.outputList.length; i++) {
                const item = this.outputList[i]
                if (!item) {
                    changed = true
                    continue
                }

                if (item._generating === true) {
                    const isKeep = keepIdStr && String(item.id) === keepIdStr
                    if (isKeep && !keptGenerating) {
                        filtered.push(item)
                        keptGenerating = true
                    } else {
                        changed = true
                    }
                    continue
                }

                filtered.push(item)
            }

            if (changed) {
                this.outputList = filtered
            }
        },
        ensureStreamingOutputPlaceholder(payload = null, allowCreate = true) {
            const knownTempId = this.promptStreamingTempResultId
            let index = knownTempId
                ? this.outputList.findIndex(item => String(item?.id) === String(knownTempId))
                : -1
            if (index > -1) {
                this.removeGeneratingOutputPlaceholdersExcept(knownTempId)
                return this.outputList.findIndex(item => String(item?.id) === String(knownTempId))
            }

            const promptResultId = Number(payload?.promptResultId || 0) || null
            if (promptResultId) {
                index = this.outputList.findIndex(item => String(item?.id) === String(promptResultId))
                if (index > -1) {
                    this.promptStreamingTempResultId = this.outputList[index].id
                    this.removeGeneratingOutputPlaceholdersExcept(this.promptStreamingTempResultId)
                    return this.outputList.findIndex(item => String(item?.id) === String(this.promptStreamingTempResultId))
                }
            }

            for (let i = this.outputList.length - 1; i >= 0; i--) {
                const item = this.outputList[i]
                if (item && item._generating === true) {
                    this.promptStreamingTempResultId = item.id
                    this.removeGeneratingOutputPlaceholdersExcept(this.promptStreamingTempResultId)
                    return this.outputList.findIndex(el => String(el?.id) === String(this.promptStreamingTempResultId))
                }
            }

            for (let i = this.outputList.length - 1; i >= 0; i--) {
                const item = this.outputList[i]
                if (item && typeof item.id === 'string' && item.id.indexOf('streaming_') === 0) {
                    this.promptStreamingTempResultId = item.id
                    return i
                }
            }

            if (!allowCreate) {
                return -1
            }

            if (!this.promptStreamingTempResultId) {
                this.promptStreamingTempResultId = `streaming_${Date.now()}`
            }

            const placeholder = this.normalizeOutputResultItem({
                id: this.promptStreamingTempResultId,
                resultString: 'Generating...',
                addTime: new Date().toISOString(),
                costTime: 0,
                promptCostToken: 0,
                resultCostToken: 0,
                totalCostToken: 0
            }, this.promptid, this.promptDetail?.fullVersion || '')
            placeholder._generating = true

            this.outputList = (this.outputList || []).concat([placeholder])
            this.forceScrollResultBoxToBottom()
            return this.outputList.length - 1
        },
        renderStreamingOutputHtml(text) {
            return this.escapeHtml(text || '').replace(/\n/g, '<br>')
        },
        isStreamingResultItem(item) {
            if (!item) {
                return false
            }
            if (item._generating === true) {
                return !this.promptStreamingTempResultId || String(item.id) === String(this.promptStreamingTempResultId)
            }
            if (!this.promptStreaming || !this.promptStreamingTempResultId) {
                return false
            }
            return String(item.id) === String(this.promptStreamingTempResultId)
        },
        handlePromptStreamChunk(payload, mode) {
            if (mode === 'continueChat') {
                this.handleContinueChatStreamChunk(payload)
                return
            }

            const index = this.ensureStreamingOutputPlaceholder(payload, false)
            if (index < 0) return
            const item = this.outputList[index]
            if (!item) return

            if (item._generating === true) {
                item.resultString = ''
                item._generating = false
            }
            item.resultString = `${item.resultString || ''}${payload.text || ''}`
            item.resultStringHtml = this.renderStreamingOutputHtml(item.resultString)
            item.costTime = payload.responseMilliseconds || item.costTime || 0
            item.promptCostToken = payload.promptTokens || item.promptCostToken || 0
            item.resultCostToken = payload.completionTokens || item.resultCostToken || 0
            item.totalCostToken = payload.totalTokens || item.totalCostToken || 0

            this.scheduleResultBoxFollowBottom()
        },
        handlePromptStreamFinal(payload, mode) {
            if (mode === 'continueChat') {
                this.handleContinueChatStreamFinal(payload)
                return
            }

            const index = this.ensureStreamingOutputPlaceholder(payload, false)
            if (index < 0) return
            const item = this.outputList[index]
            if (!item) return

            if (payload.text !== undefined && payload.text !== null) {
                item.resultString = payload.text
            }
            this.applyPromptResultView(item)
            if (payload.promptResultId && (String(item.id).indexOf('streaming_') === 0 || String(item.id) === String(this.promptStreamingTempResultId))) {
                item.id = payload.promptResultId
            }
            item._generating = false
            item.costTime = payload.responseMilliseconds || item.costTime || 0
            item.promptCostToken = payload.promptTokens || item.promptCostToken || 0
            item.resultCostToken = payload.completionTokens || item.resultCostToken || 0
            item.totalCostToken = payload.totalTokens || item.totalCostToken || 0
            if (payload.promptResultId) {
                this.promptStreamingTempResultId = payload.promptResultId
            }
            this.forceScrollResultBoxToBottom()
        },
        handleContinueChatStreamChunk(payload) {
            if (!this.continueChatMode) return
            const now = new Date().toISOString()
            let tempMessage = this.continueChatHistory.find(msg => msg.id === 'streaming_assistant')
            if (!tempMessage) {
                tempMessage = {
                    id: 'streaming_assistant',
                    roleType: 2,
                    content: '',
                    sequence: (this.continueChatHistory[this.continueChatHistory.length - 1]?.sequence || 0) + 1,
                    addTime: now,
                    _generating: false
                }
                this.continueChatHistory.push(tempMessage)
            }
            if (tempMessage._generating === true) {
                tempMessage.content = ''
            }
            tempMessage.content = `${tempMessage.content || ''}${payload.text || ''}`
            tempMessage.addTime = now
            tempMessage._generating = false
            this.continueChatUsageSummary.promptCostToken = payload.promptTokens || this.continueChatUsageSummary.promptCostToken
            this.continueChatUsageSummary.resultCostToken = payload.completionTokens || this.continueChatUsageSummary.resultCostToken
            this.continueChatUsageSummary.totalCostToken = payload.totalTokens || this.continueChatUsageSummary.totalCostToken
            this.continueChatHistory = [...this.continueChatHistory]
        },
        handleContinueChatStreamFinal(payload) {
            if (!this.continueChatMode) return
            const tempMessage = this.continueChatHistory.find(msg => msg.id === 'streaming_assistant')
            if (tempMessage) {
                tempMessage.content = payload.text || tempMessage.content || ''
                tempMessage.addTime = new Date().toISOString()
                tempMessage._generating = false
            }

            this.continueChatUsageSummary.promptCostToken = payload.promptTokens || this.continueChatUsageSummary.promptCostToken
            this.continueChatUsageSummary.resultCostToken = payload.completionTokens || this.continueChatUsageSummary.resultCostToken
            this.continueChatUsageSummary.totalCostToken = payload.totalTokens || this.continueChatUsageSummary.totalCostToken
            this.continueChatHistory = [...this.continueChatHistory]
        },
        //Legacy comment translated to English.
        getTargetRangeIdFromUrl() {
             //Legacy comment translated to English.
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
               
                    //Legacy comment translated to English.
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

                            // get details
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
                    //Legacy comment translated to English.
                     this.getScoringTrendData()
                

            }
        },
        //Legacy comment translated to English.
        foldsidebar() {
            this.isBoxVisible = !this.isBoxVisible;
            if (this.foldsidebarShow) {
             
                this.foldsidebarShow = false

            } else {
                this.foldsidebarShow = true
               
            }
        },
      
        //Legacy comment translated to English.
       
        preservePromptContentBeforeAmplification() {
            const editor = this.$refs.promptEditor;
            if (!editor || this.isComposing || editor.offsetParent === null) {
                return this.content;
            }

            const editorText = this.getEditorText();
            if (editorText || !this.content) {
                this.assignContentFromEditor(editorText);
            }
            return editorText || this.content;
        },

        Amplification(boxClicked) {
            const promptContent = this.preservePromptContentBeforeAmplification();
            this.clearHighlightTimer();

            if (this.lastClickedBox === boxClicked) {
                //Legacy comment translated to English.
                this.box1Hidden = false;
                this.box2Hidden = false;
                this.box3Hidden = false;
                this.centerAreaMaximized = false;
                this.rightAreaMaximized = false;
                this.lastClickedBox = null;
                this.getScoringTrendData();
            } else {
                //Legacy comment translated to English.
                if (boxClicked === 'box1') {
                    //Legacy comment translated to English.
                    this.box1Hidden = false;
                    this.box2Hidden = true;  //Legacy comment translated to English.
                    this.box3Hidden = true;  //Legacy comment translated to English.
                    this.centerAreaMaximized = false;
                    this.rightAreaMaximized = true;
                } else if (boxClicked === 'box2') {
                    //Legacy comment translated to English.
                    this.box1Hidden = true;  //Legacy comment translated to English.
                    this.box2Hidden = false;
                    this.box3Hidden = true;  //Legacy comment translated to English.
                    this.centerAreaMaximized = true;
                    this.rightAreaMaximized = false;
                } else if (boxClicked === 'box3') {
                    //Legacy comment translated to English.
                    this.box1Hidden = false;
                    this.box2Hidden = false;
                    this.box3Hidden = false;
                }
                this.lastClickedBox = boxClicked;
            }

            this.$nextTick(() => {
                const editor = this.$refs.promptEditor;
                if (!editor || !promptContent || this.getEditorText() === promptContent) return;
                editor.innerHTML = this.generateHighlightHTML(promptContent);
            });
            },
        style(val) {
            const length = 10,
                progress = val - 0,
                left = progress / length * 100
            return {
                paddingLeft: `${left}%`,
            }
        },
        // rangedelete
        fieldDeleteHandel(e, id) {
            // stop event propagation
            e.stopPropagation();
            //Legacy comment translated to English.
            this.$confirm('此操作将永久删除该靶场下的所有内容', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(async () => {
                let res = await servicePR.delete(`/api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.DeleteAsync?rangeId=${id}`)
                if (res.data.success) {
                    this.$message.success('删除成功')
                    let _isReset = id == this.promptField
                    //Legacy comment translated to English.
                    this.getFieldList().then(() => {
                        if (_isReset) {
                            this.promptField = ''
                            // reset page data
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
        //Legacy comment translated to English.
        promptDeleteHandel(e, id) {
            // stop event propagation
            e.stopPropagation();
            //Legacy comment translated to English.
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
        //Legacy comment translated to English.
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
            // stop event propagation
          //  e.stopPropagation();
            //Legacy comment translated to English.
          //Legacy comment translated to English.
           //Legacy comment translated to English.
           //Legacy comment translated to English.
            //Legacy comment translated to English.
           // }).then(async ({ value }) => {
           //     this.btnEditHandle({ id: item.id, nickName: value })
           // }).catch(() => {
               // this.$message({
              //      type: 'info',
             //Legacy comment translated to English.
           //     });
         //   });
        // },
        //Legacy comment translated to English.
        promptNameRest(e,id) {
            // stop event propagation
            e.stopPropagation();
            //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        promptNameField(e, item) {
            e.stopPropagation();
            this.targetlaneNameid = item.id;
     
            this.dialogVisible = true
        },
        handleSelect(item) {
            console.log(item);
        },
        //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                this.targetlaneName = this.targetlaneName.substring(startIndex + prefix.length, endIndex);
                console.log(this.targetlaneName);
                this.btnEditHandle({ id: this.targetlaneNameid, nickName: this.targetlaneName })
          
                this.dialogVisible = false
                this.targetlaneName = ''
            } else {
                //Legacy comment translated to English.
                this.btnEditHandle({ id: this.targetlaneNameid, nickName: this.targetlaneName })
               
                this.dialogVisible = false
                this.targetlaneName = ''
                console.log(this.targetlaneName);
            }
           
        },
        //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            cb(results);
        },
        createFilter(queryString) {
            return (restaurant) => {
                console.log('Filtering restaurant:', restaurant.label, 'with query:', queryString);
                const label = restaurant.label || '';
                return label.toLowerCase().includes(queryString.toLowerCase());
            };
        },
        //Legacy comment translated to English.
        async tacticalFormSubmitBtn() {
            //Legacy comment translated to English.
            if (this.continueChatMode && this.continueChatPromptResultId) {
                // checkwhetherhasinputcontent
                if (!this.tacticalChatInput || !this.tacticalChatInput.trim()) {
                    this.$message({
                        message: '请输入对话内容',
                        type: 'warning',
                        duration: 3000
                    })
                    return
                }
                
                //Legacy comment translated to English.
                this.continueChatSubmit(this.continueChatPromptResultId, this.tacticalChatInput.trim())
                return
            }

            if (!this.supportsChatMode) {
                this.tacticalForm.chatMode = '直接测试'
            }
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            if (!this.tacticalForm.chatMode) {
                this.$message({
                    message: '请选择打靶测试模式',
                    type: 'warning',
                    duration: 3000
                })
                return
            }
            
            //Legacy comment translated to English.
            if (this.promptid && !this.tacticalForm.tactics) {
                this.$message({
                    message: '请选择战术',
                    type: 'warning',
                    duration: 3000
                })
                return
            }
            
            //Legacy comment translated to English.
            if (this.supportsChatMode && this.tacticalForm.chatMode === '对话模式') {
                // checkwhetherhasinputcontent
                if (!this.tacticalChatInput || !this.tacticalChatInput.trim()) {
                    this.$message({
                        message: '请输入对话内容',
                        type: 'warning',
                        duration: 3000
                    })
                    return
                }
                
                //Legacy comment translated to English.
                await this.executeTargetShootWithChatMessage(this.tacticalChatInput.trim())
                //Legacy comment translated to English.
                this.tacticalChatInput = ''
                return
            }
            
            //Legacy comment translated to English.
            await this.executeTargetShoot();
        },
        
        //Legacy comment translated to English.
        async continueChatSubmit(promptResultId, userMessage) {
            const normalizedUserMessage = typeof userMessage === 'string' ? userMessage.trim() : ''
            if (!normalizedUserMessage) {
                this.$message({
                    message: '请输入对话内容',
                    type: 'warning',
                    duration: 3000
                })
                return
            }
            this.tacticalFormSubmitLoading = true
            this.removeContinueChatStreamingMessage()
            this.appendContinueChatPendingUserMessage(normalizedUserMessage)
            this.ensureContinueChatGeneratingMessage()
            const streamId = this.createPromptStreamId()
            const streamReady = await this.openPromptStream(streamId, 'continueChat')
            try {
                const res = await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.ContinueChat`, {
                    promptResultId: promptResultId,
                    userMessage: normalizedUserMessage,
                    streamId: streamReady ? streamId : null
                })
                
                if (res.data.success) {
                    const reloadRes = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetChatHistory?promptResultId=${promptResultId}`)
                    if (reloadRes.data.success) {
                        this.continueChatHistory = reloadRes.data.data || []
                        this.continueChatPendingUserMessageId = null
                    } else {
                        this.confirmContinueChatPendingUserMessage()
                    }
                    this.removeContinueChatStreamingMessage()
                    
                    //Legacy comment translated to English.
                    const resultIndex = this.outputList.findIndex(item => item.id === promptResultId)
                    if (resultIndex !== -1) {
                        const resultItem = this.outputList[resultIndex]
                        
                        //Legacy comment translated to English.
                        const assistantMessages = this.continueChatHistory.filter(msg => msg.roleType === 2)
                        const latestAssistantMessage = assistantMessages.length > 0 ? assistantMessages[assistantMessages.length - 1] : null
                        if (latestAssistantMessage) {
                            //Legacy comment translated to English.
                            const currentResult = resultItem.resultString || ''
                            const separator = currentResult ? '\n\n---\n\n' : ''
                            const newContent = `**${ncfST('用户')}**: ${normalizedUserMessage}\n\n**${ncfST('助手')}**: ${latestAssistantMessage.content}`
                            resultItem.resultString = currentResult + separator + newContent
                            resultItem.resultStringHtml = this.renderSafeMarkdown(resultItem.resultString)
                            resultItem.promptCostToken = this.continueChatUsageSummary.promptCostToken || resultItem.promptCostToken || 0
                            resultItem.resultCostToken = this.continueChatUsageSummary.resultCostToken || resultItem.resultCostToken || 0
                            resultItem.totalCostToken = this.continueChatUsageSummary.totalCostToken || resultItem.totalCostToken || 0
                            this.$set(this.outputList, resultIndex, Object.assign({}, resultItem))
                        }
                    }
                    
                    //Legacy comment translated to English.
                    this.$forceUpdate()
                    
                    // add a button to copy code blocks
                    this.$nextTick(() => {
                        this.addCopyButtonsToCodeBlocks();
                    });
                    
                    //Legacy comment translated to English.
                    this.$nextTick(() => {
                        const container = document.getElementById('chatHistoryContainer')
                        if (container) {
                            container.scrollTop = container.scrollHeight
                        }
                    })
                    
                    //Legacy comment translated to English.
                    this.tacticalChatInput = ''
                    this.$message({
                        message: '对话已追加',
                        type: 'success',
                        duration: 2000
                    })
                } else {
                    this.removeContinueChatStreamingMessage()
                    this.removeContinueChatPendingUserMessage()
                    this.closePromptStream()
                    this.$message({
                        message: res.data.errorMessage || '继续聊天失败',
                        type: 'error'
                    })
                }
            } catch (error) {
                this.closePromptStream()
                this.$message({
                    message: '继续聊天失败：' + (error.message || '未知错误'),
                    type: 'error'
                })
                this.removeContinueChatStreamingMessage()
                this.removeContinueChatPendingUserMessage()
            } finally {
                this.tacticalFormSubmitLoading = false
            }
        },
        /*
        * Legacy comment translated to English.

        * Legacy comment translated to English.

        */
        async targetShootHandel(isDraft = false, isSaveDirect) {
            if (!this.modelid) {
                this.$message({
                    message: '请选择模型！',
                    type: 'warning'
                })
                return
            }
            if (!this.ensurePromptContentForCurrentModel()) {
                return
            }
            if (!isDraft && !this.content) {
                this.$message({
                    message: '请输入内容！',
                    type: 'warning'
                })
                return
            }
            //Legacy comment translated to English.
            if (this.promptid && !isDraft) {
                this.tacticalFormVisible = true
                return
            }
            //Legacy comment translated to English.
            let _isPromptDraft = false
            let _findPrompt = this.promptOpt.find(item => item.value == this.promptid)
            if (isDraft && _findPrompt) {
                _isPromptDraft = _findPrompt.isDraft
            }

            if (isDraft && !_isPromptDraft && this.promptOpt.length !== 0) {
                this.tacticalFormVisible = true
                return
            }
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            if (!this.promptid && !isDraft) {
                this.tacticalFormVisible = true
                return
            }


            this.targetShootLoading = true
            let _postData = {
                //Legacy comment translated to English.
                modelid: this.modelid,//Legacy comment translated to English.
                content: this.content,// prompt inputcontent
                note: this.remarks, //Legacy comment translated to English.
                numsOfResults: 1,
                //numsOfResults: isDraft?this.numsOfResults:1,
                isDraft: isDraft,
                suffix: this.promptParamForm.suffix,
                prefix: this.promptParamForm.prefix,

            }
            const executionOptions = this.buildExecutionOptionsPayload()
            if (executionOptions) {
                _postData.executionOptions = executionOptions
            }
            //Legacy comment translated to English.
            _postData.isAIGrade = this.isAIGrade
            if (this.aiScoreForm.resultList.length > 0) {
                let _list = this.aiScoreForm.resultList.map(item => item.value)
                _list = _list.filter(item => item)
                if (_list.length > 0) {
                    _postData.expectedResultsJson = JSON.stringify(_list)
                }
            }
            
            // requestparameters
            if (this.promptParamForm.variableList.length > 0) {
                _postData.variableDictJson = this.convertData(this.promptParamForm.variableList)
            }

            this.parameterViewList.forEach(item => {
                console.log('item' + item);
                //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            _postData['rangeId'] = this.promptField

            if (isDraft && _isPromptDraft) {
                return await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.UpdateDraftAsync?promptItemId=${this.promptid}`, _postData).then(res => {
                    this.targetShootLoading = false
                    if (res.data.success) {
                        this.pageChange = false
                        //Legacy comment translated to English.
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
                            //Legacy comment translated to English.
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
                            evalMaxScore = -1,
                            promptItem = {}
                        } = res.data.data || {}
                        //Legacy comment translated to English.
                        let copyResultData = JSON.parse(JSON.stringify(res.data.data))
                        delete copyResultData.promptResultList
                        let vArr = copyResultData.fullVersion.split('-')
                        copyResultData.promptFieldStr = vArr[0] || ''
                        copyResultData.promptStr = vArr[1] || ''
                        copyResultData.tacticsStr = vArr[2] || ''
                        this.promptDetail = copyResultData
                        //Legacy comment translated to English.
                        this.outputAverageDeci = evalAvgScore > -1 ? evalAvgScore : -1;
                        //Legacy comment translated to English.
                        this.outputMaxDeci = evalMaxScore > -1 ? evalMaxScore : -1;
                        //Legacy comment translated to English.
                        this.outputList = promptResultList.map(item => this.normalizeOutputResultItem(item, id, fullVersion, promptItem))
                        
                        // add a button to copy code blocks
                        this.$nextTick(() => {
                            this.addCopyButtonsToCodeBlocks();
                        });
                        
                        //Legacy comment translated to English.
                        this.getFieldList().then(() => {

                            this.getPromptOptData(id)
                            //Legacy comment translated to English.
                            this.getScoringTrendData()
                        })

                        if (this.sendBtnText !== '保存草稿' && this.numsOfResults > 1) {
                            //Legacy comment translated to English.
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
         * Legacy comment translated to English.

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
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            
            for (let i = 0; i < howmany; i++) {
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                await this.rapidFireHandel(id, this._lastUserMessage || null);
            }
            //Legacy comment translated to English.
            this.clearOutputGeneratingPlaceholder()
            this.closePromptStream()
            //Legacy comment translated to English.
            this.getPromptOptData()
            this.targetShootLoading = false
            this.dodgersLoading = false
        },
        //Legacy comment translated to English.
        uploadPluginCloseDialog() {
            // clearfileData
            this.uploadPluginDropAreaVisible = true
            this.uploadPluginData = []
            this.jsZip = null
        },
        //Legacy comment translated to English.
        pluginDropOverHandler(e) {
            e.stopPropagation();
            e.preventDefault();
            this.uploadPluginDropHover = true;
        },
        //Legacy comment translated to English.
        pluginDropEnterHandler(e) {
            e.stopPropagation();
            e.preventDefault();
            this.uploadPluginDropHover = true;
        },
        //Legacy comment translated to English.
        pluginDropLeaveHandler(e) {
            e.stopPropagation();
            e.preventDefault();
            this.uploadPluginDropHover = false;
        },
        //Legacy comment translated to English.
        enentPluginDrop(e) {
            this.uploadPluginDropHover = false
            let items = e.dataTransfer.items;
            if (!items) return
            this.uploadPluginDropAreaVisible = false
            this.jsZip = new JSZip();
            for (let i = 0; i <= items.length - 1; i++) {
                let item = items[i];
                if (item.kind === "file") {
                    //Legacy comment translated to English.
                    let entry = item.webkitGetAsEntry();
                    //Legacy comment translated to English.
                    this.getFileFromEntryRecursively(entry);
                }
            }

            // console.log('Drop',items);
            e.stopPropagation();
            e.preventDefault();
        },
        //Legacy comment translated to English.
        getFileFromEntryRecursively(entry) {
            //let _this = this
            if (entry.isFile) {
                // file
                entry.file(
                    file => {
                        //console.log('Drop file', { file, path: _path });
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        let _path = entry.fullPath
                        if (entry.fullPath.startsWith('/')) {
                            _path = entry.fullPath.slice(1)
                        }
                        this.forEachZip(file, _path);
                        //Legacy comment translated to English.
                        this.uploadPluginData.push({ name: file.name, path: _path })
                    },
                    e => { console.log(e); }
                );
            } else {
                //Legacy comment translated to English.
                let reader = entry.createReader();
                reader.readEntries(
                    entries => {
                        entries.forEach(entry => this.getFileFromEntryRecursively(entry));
                    },
                    e => { console.log(e); }
                );
            }
        },
        //Legacy comment translated to English.
        enentPluginInput() {
            let input = document.createElement("input");
            input.type = "file";
            input.setAttribute("allowdirs", "true");
            input.setAttribute("directory", "true");
            input.setAttribute("webkitdirectory", "true"); //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                for (let i = 0; i <= files.length - 1; i++) {
                    _this.uploadPluginData.push({ name: files[i].name, path: files[i].webkitRelativePath })
                    //Legacy comment translated to English.
                    _this.forEachZip(files[i], files[i].webkitRelativePath);

                }

                document.querySelector("body").removeChild(input);
            };
        },
        //Legacy comment translated to English.
        forEachZip(file, path) {
            //console.log('forEachZip files：', file, path)
            //Legacy comment translated to English.
            let _path = path
            let _index = path.indexOf('/')
            if (_index > -1) {
                _path = _path.slice(_index + 1)
            }
            this.jsZip.file(`${_path}`, file);
        },
        //Legacy comment translated to English.
        submitUploadPlugins() {
            let _fileData = JSON.parse(JSON.stringify(this.uploadPluginData))
            if (_fileData.length === 0) {
                this.$message.warning('请选择文件夹')
                return
            }
            if (this.isPageLoading) return
            this.isPageLoading = true
            let name = _fileData[0].path.split('/')[0] || 'plugin'
            //Legacy comment translated to English.
            this.jsZip.generateAsync({ type: "blob" }).then((content) => {
                //Legacy comment translated to English.
                let zipFile = new File([content], `${name}.zip`, {
                    type: "application/zip",
                });
                //Legacy comment translated to English.
                //let isLt2M = zipFile.size / 1024 / 1024 < 80;
                //if (!isLt2M) {
                //    this.fileList = [];
                //    this.$message({
                //Legacy comment translated to English.
                //        type: "warning",
                //    });
                //    return false;
                //} else {
                //    let filedata = new FormData();
                //    // filedata.append("file", zipFile);
                //    filedata.append("zipFile", zipFile);
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //}
                let filedata = new FormData();
                // filedata.append("file", zipFile);
                filedata.append("zipFile", zipFile);
                this.folderHandlesubmit(filedata); //Legacy comment translated to English.
                //Legacy comment translated to English.
            }).catch(() => {
                this.isPageLoading = false
            })
        },
        // upload Plugins api
        folderHandlesubmit(formData) {
            //ajaxuploadformData
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
                    // updaterangedata
                    this.getFieldList().then(() => {
                        if (this.promptFieldOpt && this.promptFieldOpt.length > 0) {
                            this.promptField = this.promptFieldOpt[this.promptFieldOpt.length - 1].id
                            // reset page data
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
        //Legacy comment translated to English.
        expectedPluginCloseDialog() {
            this.expectedPluginFoem = {
                checkList: []
            }
            this.expectedPluginFieldList = [] //Legacy comment translated to English.
            this.exportPluginSelectedCount = 0 //Legacy comment translated to English.
            this.exportPluginExpandAll = false //Legacy comment translated to English.
            this.$refs.expectedPluginFoem.resetFields();
        },
        //Legacy comment translated to English.
        async expectedPluginOpen() {
            //Legacy comment translated to English.
            let _valList = this.promptFieldOpt
            let promises = [];
            for (let i = 0; i < _valList.length; i++) {
                //Legacy comment translated to English.
                promises.push(this.getPromptOptData(_valList[i].id, true));
            }
            this.expectedPluginVisible = true
            await Promise.all(promises)
            //Legacy comment translated to English.
            this.$nextTick(() => {
            //Legacy comment translated to English.
            this.$refs.expectedPluginTree.setCheckedKeys(this.expectedPluginFoem.checkList)
                
                //Legacy comment translated to English.
                const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
                const leafNodeKeys = [];
                
                checkedNodes.forEach(node => {
                    //Legacy comment translated to English.
                    if (!node.children || node.children.length === 0) {
                        leafNodeKeys.push(node.idkey);
                    }
                });
                
                this.expectedPluginFoem.checkList = leafNodeKeys;
                
                //Legacy comment translated to English.
                this.updateExportPluginSelectedCount()
            })
        },
        //Legacy comment translated to English.
        treeCheckChange(data, currentCheck, childrenCheck) {
            //Legacy comment translated to English.
            this.$nextTick(() => {
                //Legacy comment translated to English.
                if (!this.$refs.expectedPluginTree) return;
                
                const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
                const leafNodeKeys = [];
                
                checkedNodes.forEach(node => {
                    //Legacy comment translated to English.
                    if (!node.children || node.children.length === 0) {
                        leafNodeKeys.push(node.idkey);
                    }
                });
                
                //Legacy comment translated to English.
                this.expectedPluginFoem.checkList = leafNodeKeys;
                
                //Legacy comment translated to English.
                this.updateExportPluginSelectedCount();
            });
        },
        
        //Legacy comment translated to English.
        updateExportPluginSelectedCount() {
            if (!this.$refs.expectedPluginTree) {
                this.exportPluginSelectedCount = 0;
                return;
            }
            
            //Legacy comment translated to English.
            const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
            const halfCheckedNodes = this.$refs.expectedPluginTree.getHalfCheckedNodes();
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            let count = 0;
            
            //Legacy comment translated to English.
            checkedNodes.forEach(node => {
                //Legacy comment translated to English.
                if (!node.children || node.children.length === 0) {
                    count++;
                }
            });
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            
            this.exportPluginSelectedCount = count;
        },
        
        //Legacy comment translated to English.
        exportPluginSelectAll() {
            if (!this.$refs.expectedPluginTree) return;
            
            //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            this.$refs.expectedPluginTree.setCheckedKeys(allKeys);
            
            //Legacy comment translated to English.
            this.$nextTick(() => {
                const checkedNodes = this.$refs.expectedPluginTree.getCheckedNodes();
                const leafNodeKeys = [];
                
                checkedNodes.forEach(node => {
                    //Legacy comment translated to English.
                    if (!node.children || node.children.length === 0) {
                        leafNodeKeys.push(node.idkey);
                    }
                });
                
                this.expectedPluginFoem.checkList = leafNodeKeys;
                this.updateExportPluginSelectedCount();
            });
            
            this.$message.success('已全选所有靶道');
        },
        
        //Legacy comment translated to English.
        exportPluginInvertSelection() {
            if (!this.$refs.expectedPluginTree) return;
            
            //Legacy comment translated to English.
            const allLeafKeys = [];
            const collectLeafKeys = (nodes) => {
                nodes.forEach(node => {
                    if (!node.children || node.children.length === 0) {
                        //Legacy comment translated to English.
                        allLeafKeys.push(node.idkey);
                    } else {
                        //Legacy comment translated to English.
                        collectLeafKeys(node.children);
                    }
                });
            };
            collectLeafKeys(this.expectedPluginFieldList);
            
            //Legacy comment translated to English.
            const currentCheckedLeafKeys = this.$refs.expectedPluginTree.getCheckedKeys(true);
            
            //Legacy comment translated to English.
            const invertedLeafKeys = allLeafKeys.filter(key => !currentCheckedLeafKeys.includes(key));
            
            //Legacy comment translated to English.
            this.$refs.expectedPluginTree.setCheckedKeys(invertedLeafKeys);
            
            //Legacy comment translated to English.
            this.$nextTick(() => {
                this.expectedPluginFoem.checkList = invertedLeafKeys;
                this.updateExportPluginSelectedCount();
            });
            
            this.$message.success('已反选');
        },
        
        //Legacy comment translated to English.
        exportPluginClearAll() {
            if (!this.$refs.expectedPluginTree) return;
            
            this.$refs.expectedPluginTree.setCheckedKeys([]);
            this.expectedPluginFoem.checkList = [];
            
            //Legacy comment translated to English.
            this.$nextTick(() => {
                this.updateExportPluginSelectedCount();
            });
            
            this.$message.info('已清空所有选择');
        },
        
        //Legacy comment translated to English.
        exportPluginToggleExpand() {
            this.exportPluginExpandAll = !this.exportPluginExpandAll;
            
            //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        btnExpectedPlugins() {
            this.$refs.expectedPluginFoem.validate(async (valid) => {
                if (valid) {
                    //Legacy comment translated to English.
                    //return
                    this.isPageLoading = true
                    //Legacy comment translated to English.
                    let _zipname = 'plugins'
                    let _rangeIds = []
                    let _ids = []
                    this.expectedPluginFoem.checkList.forEach(item => {
                        //Legacy comment translated to English.
                        if (item.indexOf('_') > -1) {
                            let _itemArr = item.split('_')
                            _ids.push(Number(_itemArr[1]))
                            _rangeIds.push(Number(_itemArr[0]))
                        } else {
                            _rangeIds.push(Number(item))
                        }
                    })
                    //Legacy comment translated to English.
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
                            ids: _ids//Legacy comment translated to English.
                        }
                    }).then((res) => {
                        this.isPageLoading = false
                        this.expectedPluginVisible = false
                        const blob = new Blob([res.data], { type: 'application/zip' });
                        const url = URL.createObjectURL(blob);
                        const a = document.createElement('a');
                        a.href = url;
                        a.download = `${_zipname}.zip`; //Legacy comment translated to English.
                        a.click(); //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        URL.revokeObjectURL(url); //Legacy comment translated to English.
                        a.parentNode && a.parentNode.removeChild(a); //Legacy comment translated to English.
                        
                        this.$message.success('导出成功！');
                    }).catch((error) => {
                        this.isPageLoading = false
                        console.error('导出失败:', error);
                        
                        let errorMessage = '导出 plugins 失败';
                        if (error.response) {
                            //Legacy comment translated to English.
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
        //Legacy comment translated to English.
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
        // addrange
        addPromptField() {
            //Legacy comment translated to English.
            this.fieldFormVisible = true
        },
        //Legacy comment translated to English.
        changeNumsBtn(command = 1) {
            this.numsOfResults = command
        },
        //Legacy comment translated to English.
        changeBtn(command) {
            this.sendBtnText = command
        },
        //Legacy comment translated to English.
        clickSendBtn() {
            const command = this.sendBtnText
            //Legacy comment translated to English.
            if (command === '打靶') {
                this.targetShootHandel()
            } else if (command === '保存草稿') {
                this.targetShootHandel(true)
            } else if (command === '连发') {
                this.dealRapicFireHandel(this.numsOfResults)
            }
        },
        //Legacy comment translated to English.
        isMacEmbeddedWebView() {
            const userAgent = navigator.userAgent || ''
            return /Macintosh/i.test(userAgent) &&
                /AppleWebKit/i.test(userAgent) &&
                !/Safari\//i.test(userAgent)
        },
        beforeunloadHandler(e) {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            if (this.pageChange) {
                //Legacy comment translated to English.
                let confirmationMessage = '您的数据已经修改，是否保存为草稿？';
                //Legacy comment translated to English.
                e.preventDefault();
                //Legacy comment translated to English.
                e.returnValue = confirmationMessage;
                return confirmationMessage;
            }
            //setTimeout(function () {
            //Legacy comment translated to English.
            //    var modal = document.createElement("div");
            //Legacy comment translated to English.
            //    var btn = document.createElement("button");
            //Legacy comment translated to English.
            //    btn.onclick = function () {
            //Legacy comment translated to English.
            //        e.preventDefault();
            //Legacy comment translated to English.
            //        modal.remove();
            //    };
            //    modal.appendChild(btn);
            //    document.body.appendChild(modal);
            //}, 0);
        },
        async copyInfo(source) {
            let fullVersion = '';
            let label = '';
            
            //Legacy comment translated to English.
            if (source === 'A' || source === 'B') {
                const prompt = source === 'A' ? this.comparePromptA : this.comparePromptB;
                if (!prompt || !prompt.fullVersion) {
                    this.$message.warning('无法获取版本号');
                    return;
                }
                fullVersion = prompt.fullVersion;
                label = `Prompt ${source} 版本号`;
            } else {
                //Legacy comment translated to English.
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
            
            const message = label ? `复制${label}【${fullVersion}】成功` : `复制【${fullVersion}】成功`;
            await window.PromptRangeUtils.CopyHelper.copyToClipboardAsync(fullVersion, message, '复制失败', true)
        },
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            var hh = (date.getHours() < 10 ? '0' + date.getHours() : date.getHours()) + ':'
            var mm = (date.getMinutes() < 10 ? '0' + date.getMinutes() : date.getMinutes())
            var MM = (date.getMonth() + 1 < 10 ? '0' + (date.getMonth() + 1) : date.getMonth() + 1) + '-'
            var DD = date.getDate() < 10 ? '0' + date.getDate() : date.getDate()
            return MM + DD + ' ' + hh + mm
        },
        //Legacy comment translated to English.
        formatChatContent(content) {
            if (!content) return ''
            return this.renderSafeMarkdown(content)
        },
        renderSafeMarkdown(content) {
            const escapedContent = this.escapeHtml(content || '')
            if (typeof marked === 'undefined') {
                return escapedContent.replace(/\n/g, '<br>')
            }

            const viewModel = this
            const renderer = new marked.Renderer()
            renderer.link = function ({ href, title, tokens }) {
                const safeHref = viewModel.safeHttpUrl(href)
                const label = this.parser.parseInline(tokens)
                if (!safeHref) {
                    return label
                }
                const safeTitle = title ? ` title="${viewModel.escapeHtml(title)}"` : ''
                return `<a href="${viewModel.escapeHtml(safeHref)}"${safeTitle}>${label}</a>`
            }
            renderer.image = function ({ href, title, text }) {
                const safeHref = viewModel.safeHttpUrl(href)
                if (!safeHref) {
                    return viewModel.escapeHtml(text)
                }
                const safeTitle = title ? ` title="${viewModel.escapeHtml(title)}"` : ''
                return `<img src="${viewModel.escapeHtml(safeHref)}" alt="${viewModel.escapeHtml(text)}"${safeTitle}>`
            }

            return marked.parse(escapedContent, { renderer })
        },
        safeHttpUrl(value) {
            try {
                const url = new URL(String(value || ''), window.location.origin)
                return url.protocol === 'http:' || url.protocol === 'https:' ? url.href : ''
            } catch (e) {
                return ''
            }
        },
        //Legacy comment translated to English.
        async toggleChatFeedback(chatId, feedback) {
            //Legacy comment translated to English.
            if (this._isUpdatingFeedback) {
                return
            }
            this._isUpdatingFeedback = true
            
            try {
                //Legacy comment translated to English.
                if (!chatId || chatId === 0 || chatId === '0') {
                    console.error('无效的 chatId:', chatId, '类型:', typeof chatId)
                    console.error('当前历史记录:', this.continueChatHistory)
                    this.$message({
                        message: '对话记录ID无效，请刷新页面后重试',
                        type: 'error'
                    })
                    return
                }
                
                //Legacy comment translated to English.
                const numericChatId = typeof chatId === 'string' ? parseInt(chatId, 10) : chatId
                if (isNaN(numericChatId) || numericChatId <= 0) {
                    console.error('无效的 chatId（转换后）:', numericChatId, '原始值:', chatId)
                    this.$message({
                        message: '对话记录ID无效，请刷新页面后重试',
                        type: 'error'
                    })
                    return
                }
                
                //Legacy comment translated to English.
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
                
                //Legacy comment translated to English.
                const msgId = typeof currentMsg.id === 'string' ? parseInt(currentMsg.id, 10) : currentMsg.id
                if (!msgId || msgId === 0 || isNaN(msgId)) {
                    console.error('消息 ID 无效:', currentMsg.id, '类型:', typeof currentMsg.id)
                    this.$message({
                        message: '消息ID无效，请刷新页面后重试',
                        type: 'error'
                    })
                    return
                }
                
                //Legacy comment translated to English.
                const newFeedback = currentMsg.userFeedback === feedback ? null : feedback
                
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                const res = await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.UpdateChatFeedback`, {
                    ChatId: msgId,
                    Feedback: newFeedback
                })
                
                if (res.data.success) {
                    //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                this._isUpdatingFeedback = false
            }
        },
        //Legacy comment translated to English.
        handleChatHistoryScroll(event) {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
        },
        //Legacy comment translated to English.
        chartInitialization() {
            let scoreChart = document.getElementById('promptPage_scoreChart');
            let chartOption = {
                tooltip: {
                    show: true,
                    confine: true, //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
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
                    boxHeight: 150, //Legacy comment translated to English.
                    boxWidth: 400, //Legacy comment translated to English.
                    boxDepth: 150, //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    environment: '#fff',
                    //Legacy comment translated to English.
                    axisLine: {
                        show: true,//Legacy comment translated to English.
                        //Legacy comment translated to English.
                        lineStyle: {//Legacy comment translated to English.
                            color: 'rgba(0,0,0,0.3)',
                            opacity: 1,//Legacy comment translated to English.
                            width: 2//Legacy comment translated to English.
                        }
                    },
                    //Legacy comment translated to English.
                    axisLabel: {
                        show: true,//Legacy comment translated to English.
                        interval: 0,//Legacy comment translated to English.
                        formatter: function (v) {
                            return typeof v === 'number' ? v.toFixed(1) : v
                        },
                        textStyle: {
                            color: '#32b8be',//Legacy comment translated to English.
                            //color: function (value, index) {
                            //Legacy comment translated to English.
                            //},
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            fontSize: 12,//Legacy comment translated to English.
                            fontWeight: '400',//Legacy comment translated to English.
                        }
                    },
                    //Legacy comment translated to English.
                    axisTick: {
                        show: true,//Legacy comment translated to English.
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        //    color: '#000',//color
                        //    opacity: 1,
                        //Legacy comment translated to English.
                        //}
                    },
                    //Legacy comment translated to English.
                    splitLine: {
                        show: true,//Legacy comment translated to English.
                        //Legacy comment translated to English.
                        lineStyle: {//Legacy comment translated to English.
                            color: 'rgba(0,0,0,0.05)',
                            opacity: 1,//Legacy comment translated to English.
                            width: 1//Legacy comment translated to English.
                        },
                        //splitArea: {
                        //    show: true,
                        //Legacy comment translated to English.
                        //    areaStyle: {
                        //        color: ['rgba(250,250,250,0.2)', 'rgba(200,200,200,0.3)', 'rgba(250,250,250,0.2)', 'rgba(200,200,200,0.2)']
                        //    }
                        //},
                    },
                    //Legacy comment translated to English.
                    axisPointer: {
                        show: false,//Legacy comment translated to English.
                        // lineStyle:{
                        //     color:'#000',//color
                        //     opacity:1,
                        //Legacy comment translated to English.
                        // }
                    },
                    //Legacy comment translated to English.
                    viewControl: {
                        minBeta: 0, //Legacy comment translated to English.
                        maxBeta: 90, //Legacy comment translated to English.
                        minAlpha: 0, //Legacy comment translated to English.
                        maxAlpha: 90, //Legacy comment translated to English.
                        rotateSensitivity: 10,//Legacy comment translated to English.
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        distance: 350,//Legacy comment translated to English.
                        alpha: 1,//Legacy comment translated to English.
                        beta: 30,//Legacy comment translated to English.
                        //Legacy comment translated to English.
                        animation: true,
                    },
                    //Legacy comment translated to English.
                    //light: {
                    //    main: {
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    //    },
                    //Legacy comment translated to English.
                    //        intensity: 0.3,
                    //Legacy comment translated to English.
                    //    },
                    //Legacy comment translated to English.
                    //    //  texture: 'pisa.hdr',
                    //Legacy comment translated to English.
                    //    // exposure: 1.0
                    //    // }
                    //},
                    //Legacy comment translated to English.
                    //     show:true,
                    //     bloom:{
                    //Legacy comment translated to English.
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
                        data: item,    //Legacy comment translated to English.
                        itemStyle: {
                            opacity: 0.7
                        },
                        label: {
                            position: 'top',
                            show: true,
                            formatter: function (params) {
                                const promptItem = that.promptOpt.find(item => item.id === that.promptid)
                                const fullVersion = promptItem ? promptItem.fullVersion : ''
                                return params.data[3].fullVersion === fullVersion ? '当前' : ' ';  //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            chartInstance.on('click', (params) => {
                // console.log('click params：', params)
                const promptItem = this.promptOpt.find(item => item.fullVersion === params.data[3].fullVersion)
                if (promptItem) {
                    //Legacy comment translated to English.
                    this.promptid = promptItem.id
                    //Legacy comment translated to English.
                    this.getPromptetail(promptItem.id, true)
                    this.chartInstance.resize()
                    //Legacy comment translated to English.
                    //this.getOutputList()
                    //Legacy comment translated to English.
                    // this.getScoringTrendData()
                }
            })
            //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            //     _scatterSeries.push({
            //         type: 'scatter3D',
            //         name: params.seriesName,
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //         label: {
            //             show: false,  // settings label display
            //             formatter: function (params) {
            //Legacy comment translated to English.
            //             }
            //         },
            //Legacy comment translated to English.
            //     })
            //     chartInstance.setOption({ series: [..._series, ..._scatterSeries] });
            // })
            //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        async getScoringTrendData() {
            this.chartData = {
                xData: [],
                yData: [],
                seriesData: []
            }
            if (this.promptid) {
                //Legacy comment translated to English.
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
                //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        async promptChangeHandel(val, itemKey, oldVal, fromHash = false) {
            if ((itemKey === 'promptField' || itemKey === 'promptid') && !fromHash && !this.isApplyingHashRoute) {
                const route = this.buildPromptHashRoute({
                    rangeId: itemKey === 'promptField' ? Number(val || 0) || null : Number(this.promptField || 0) || null,
                    promptId: itemKey === 'promptid' ? Number(val || 0) || null : null
                });
                this.navigatePromptByHash(route);
                return;
            }

            //Legacy comment translated to English.
            this.numsOfResults = 1
            //console.log(this.promptFieldOldVal,'|', val, '|', itemKey, '|', oldVal)
            if (itemKey === 'promptField') {
                if (fromHash || this.isApplyingHashRoute) {
                    await this.resetPageData()
                    return
                }
                //Legacy comment translated to English.
                if (this.pageChange && this.modelid) {
                    //Legacy comment translated to English.
                    this.$confirm('您的数据已经修改，是否保存为草稿？', '提示', {
                        confirmButtonText: '保存',
                        cancelButtonText: '不保存',
                        type: 'warning'
                    }).then(() => {
                        //Legacy comment translated to English.
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
                // reset page data
                this.resetPageData()
            } else if (itemKey === 'promptid') {
                if (fromHash || this.isApplyingHashRoute) {
                    this.pageChange = false
                    this.aiScoreForm = {
                        resultList: []
                    }
                    let _fitem = this.promptOpt.find(item => item.value === val)
                    if (_fitem && _fitem.isDraft) {
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
                    this.getPromptetail(val, true, true)
                    return
                }

                if (this.pageChange && this.modelid) {
                    //Legacy comment translated to English.
                    this.$confirm('您的数据已经修改，是否保存为草稿？', '提示', {
                        confirmButtonText: '保存',
                        cancelButtonText: '不保存',
                        type: 'warning'
                    }).then(() => {
                        //Legacy comment translated to English.
                        this.targetShootHandel(true).then(() => {
                            this.resetPageData()
                            this.getPromptetail(val, true, true)
                        })
                        // reset page-change tracking
                        this.pageChange = false
                        //Legacy comment translated to English.
                        //this.getFieldList()
                    }).catch(() => {
                        // reset page-change tracking
                        this.pageChange = false
                        //Legacy comment translated to English.
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

                        //Legacy comment translated to English.
                        this.aiScoreForm = {
                            resultList: []
                        }

                        //Legacy comment translated to English.
                        this.getPromptetail(val, true, true)

                    });
                } else {
                    // reset page-change tracking
                    this.pageChange = false
                    //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
                    this.getPromptetail(val, true, true)
                }

            } else {

                //Legacy comment translated to English.
                if (itemKey === 'modelid') {
                    this.onModelSelectionChanged()
                }
                //Legacy comment translated to English.
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
        // reset page data
        async resetPageData() {
            // reset page-change tracking
            this.pageChange = false
            // range
            this.promptid = '' //Legacy comment translated to English.
            this.modelid = '' // model
            //Legacy comment translated to English.
            this.resetConfigurineParam(false)
            //Legacy comment translated to English.
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
            this.ttsForm = {
                voice: 'alloy',
                format: 'mp3',
                speedRatio: 1.0
            }
            this.sttForm = {
                audioFileName: '',
                audioBase64: '',
                audioLocalRelativePath: '',
                language: '',
                fileSize: 0
            }
            this.embeddingForm = {
                vectorDbId: null,
                collectionName: '',
                searchTopK: 3,
                searchQuery: ''
            }
            //Legacy comment translated to English.
            this.aiScoreForm = {
                resultList: []
            }
            this.numsOfResults = 1
            if (this.promptField) {
                //Legacy comment translated to English.
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

                        // get details
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
                //Legacy comment translated to English.
                await this.getScoringTrendData()
            }

        },

        //Legacy comment translated to English.
        async continueChat(promptResultId, resultIndex) {
            try {
                //Legacy comment translated to English.
                const res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetChatHistoryWithPrompt?promptResultId=${promptResultId}`)
                if (res.data.success) {
                    this.continueChatMode = true
                    this.continueChatPromptResultId = promptResultId
                    this.continueChatHistory = res.data.data.chatHistory || []
                    this.continueChatSystemMessage = res.data.data.promptContent || ''
                    this.syncContinueChatUsageFromResult(promptResultId)
                    
                    //Legacy comment translated to English.
                    this.tacticalForm.chatMode = '对话模式'
                    this.tacticalFormVisible = true
                    
                    //Legacy comment translated to English.
                    this.$nextTick(() => {
                        const container = document.getElementById('chatHistoryContainer')
                        if (container) {
                            container.scrollTop = container.scrollHeight
                        }
                        // add a button to copy code blocks
                        this.addCopyButtonsToCodeBlocks();
                    })
                } else {
                    //Legacy comment translated to English.
                    const fallbackRes = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetChatHistory?promptResultId=${promptResultId}`)
                    if (fallbackRes.data.success) {
                        this.continueChatMode = true
                        this.continueChatPromptResultId = promptResultId
                        this.continueChatHistory = fallbackRes.data.data || []
                        this.syncContinueChatUsageFromResult(promptResultId)
                        //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        handleChatInputKeydown(e) {
            // Ctrl+Enter (Windows/Linux) or Cmd+Enter (Mac)
            if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
                e.preventDefault()
                e.stopPropagation()
                //Legacy comment translated to English.
                this.tacticalFormSubmitBtn()
            }
        },
        
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        tacticalFormCloseDialog() {
            const wasContinueChatMode = this.continueChatMode
            this.tacticalForm = {
                tactics: '重新瞄准',
                chatMode: '对话模式' //Legacy comment translated to English.
            }
            //Legacy comment translated to English.
            this.tacticalChatInput = ''
            //Legacy comment translated to English.
            this.continueChatMode = false
            this.continueChatPromptResultId = null
            this.continueChatHistory = []
            this.continueChatPendingUserMessageId = null
            this.continueChatSystemMessage = ''
            this.continueChatUsageSummary = {
                promptCostToken: 0,
                resultCostToken: 0,
                totalCostToken: 0
            }
            if (wasContinueChatMode) {
                this.removeContinueChatStreamingMessage()
                this.closePromptStream()
            }
            this.systemMessageCollapse = []
            if (this.$refs.tacticalForm) {
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                this.$refs.tacticalForm.clearValidate();
            }
        },
        
        //Legacy comment translated to English.
        openCreateAgentDialog() {
            if (!this.promptField) {
                this.$message({
                    message: '请先选择靶场',
                    type: 'warning'
                })
                return
            }
            if (!this.promptid || !this.promptDetail || !this.promptDetail.fullVersion) {
                this.$message({
                    message: '请先选择靶道以确定 PromptCode',
                    type: 'warning'
                })
                return
            }
            //Legacy comment translated to English.
            this.createAgentForm = {
                name: this.promptDetail.nickName || this.promptDetail.fullVersion,
                scopeType: 'full',
                promptCode: '',
                description: '',
                functionCallNames: ''
            }
            this.createAgentExistingList = []
            this.createAgentDialogVisible = true
            //Legacy comment translated to English.
            this.$nextTick(() => {
                this.checkExistingAgentsByPromptCode()
            })
        },

        //Legacy comment translated to English.
        onCreateAgentScopeChange() {
            this.checkExistingAgentsByPromptCode()
        },

        //Legacy comment translated to English.
        async checkExistingAgentsByPromptCode() {
            const promptCode = this.createAgentPromptCode
            if (!promptCode) return
            try {
                const res = await servicePR.get(
                    `/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.GetListByPromptCode?promptCode=${encodeURIComponent(promptCode)}`
                )
                if (res.data && res.data.success) {
                    this.createAgentExistingList = res.data.data || []
                }
            } catch (e) {
                //Legacy comment translated to English.
                console.warn('检查智能体时出错（请确认 AgentsManager 模块已安装）:', e)
                this.createAgentExistingList = []
            }
        },

        //Legacy comment translated to English.
        async submitCreateAgent() {
            if (!this.createAgentForm.name) {
                this.$message({ message: '请输入智能体名称', type: 'warning' })
                return
            }
            const promptCode = this.createAgentPromptCode
            if (!promptCode) {
                this.$message({ message: '无效的 PromptCode', type: 'error' })
                return
            }

            //Legacy comment translated to English.
            if (this.createAgentExistingList.length > 0) {
                try {
                    await this.$confirm(
                        `当前 PromptCode（${promptCode}）已有 ${this.createAgentExistingList.length} 个智能体使用。是否继续创建新智能体？`,
                        '确认创建',
                        { confirmButtonText: '继续创建', cancelButtonText: '取消', type: 'warning' }
                    )
                } catch {
                    return
                }
            }

            this.createAgentLoading = true
            try {
                const agentData = {
                    id: 0,
                    name: this.createAgentForm.name,
                    systemMessage: promptCode,
                    promptCode: promptCode,
                    enable: true,
                    description: this.createAgentForm.description || '',
                    hookRobotType: 0,
                    hookRobotParameter: '',
                    avastar: '/images/AgentsManager/avatar/avatar1.png',
                    functionCallNames: this.createAgentForm.functionCallNames || '',
                    mcpEndpoints: ''
                }
                const res = await servicePR.post(
                    '/api/Senparc.Xncf.AgentsManager/AgentTemplateAppService/Xncf.AgentsManager_AgentTemplateAppService.SetItem',
                    agentData
                )
                if (res.data && res.data.success) {
                    this.$message({ message: `智能体「${this.createAgentForm.name}」创建成功！`, type: 'success' })
                    this.createAgentDialogVisible = false
                } else {
                    this.$message({ message: '创建失败：' + (res.data?.msg || '未知错误'), type: 'error' })
                }
            } catch (e) {
                console.error('创建智能体失败:', e)
                this.$message({ message: '创建失败，请确认 AgentsManager 模块已安装', type: 'error' })
            } finally {
                this.createAgentLoading = false
            }
        },

        //Legacy comment translated to English.
        openMapDialog() {
            if (!this.promptField) {
                this.$message({
                    message: '请先选择靶场',
                    type: 'warning'
                })
                return
            }
            
            //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        mapDialogClose() {
            this.destroyMap3D()
            //Legacy comment translated to English.
            if (document.fullscreenElement) {
                document.exitFullscreen()
            }
        },

        //Legacy comment translated to English.
        resetMap3DView() {
            if (!this.map3dCamera || !this.map3dControls) return
            this.map3dCamera.position.set(30, 30, 50)
            this.map3dControls.target.set(0, 0, 0)
            this.map3dControls.update()
            this.map3dNeedsAnimationUpdate = true
        },

        //Legacy comment translated to English.
        fitMap3DView() {
            if (!this.map3dCamera || !this.map3dControls || !this.map3dNodes || this.map3dNodes.length === 0) return
            //Legacy comment translated to English.
            let minX = Infinity, maxX = -Infinity
            let minY = Infinity, maxY = -Infinity
            let minZ = Infinity, maxZ = -Infinity
            this.map3dNodes.forEach(node => {
                if (node.position) {
                    minX = Math.min(minX, node.position.x)
                    maxX = Math.max(maxX, node.position.x)
                    minY = Math.min(minY, node.position.y)
                    maxY = Math.max(maxY, node.position.y)
                    minZ = Math.min(minZ, node.position.z)
                    maxZ = Math.max(maxZ, node.position.z)
                }
            })
            const centerX = (minX + maxX) / 2
            const centerY = (minY + maxY) / 2
            const centerZ = (minZ + maxZ) / 2
            const size = Math.max(maxX - minX, maxY - minY, maxZ - minZ)
            const distance = size * 1.5 + 50
            this.map3dCamera.position.set(centerX + distance * 0.4, centerY + distance * 0.4, centerZ + distance)
            this.map3dControls.target.set(centerX, centerY, centerZ)
            this.map3dControls.update()
            this.map3dNeedsAnimationUpdate = true
        },

        //Legacy comment translated to English.
        toggleMap3DFullscreen() {
            const container = document.getElementById('map3dContainer')
            if (!container) return
            if (!document.fullscreenElement) {
                container.requestFullscreen().catch(e => {
                    console.warn('全屏请求失败:', e)
                })
            } else {
                document.exitFullscreen()
            }
        },

        //Legacy comment translated to English.
        registerMap3DKeyboard() {
            if (this._map3dKeydownHandler) return
            const SPEED = 3
            this._map3dKeydownHandler = (e) => {
                if (!this.mapDialogVisible || !this.map3dCamera) return
                if (e.ctrlKey || e.metaKey || e.altKey) return

                //Legacy comment translated to English.
                const key = (e.key || '').toLowerCase()
                const code = e.code || ''
                let actionKey = key

                if (code.startsWith('Key') && code.length === 4) {
                    actionKey = code.slice(3).toLowerCase()
                } else if (code === 'NumpadAdd') {
                    actionKey = '+'
                } else if (code === 'NumpadSubtract') {
                    actionKey = '-'
                } else if (code === 'Equal' && (key === '' || key === 'unidentified' || key === 'process')) {
                    actionKey = '='
                } else if (code === 'Minus' && (key === '' || key === 'unidentified' || key === 'process')) {
                    actionKey = '-'
                }

                const hasControls = !!(this.map3dControls && this.map3dControls.target)
                const moveBy = (x, y, z) => {
                    this.map3dCamera.position.x += x
                    this.map3dCamera.position.y += y
                    this.map3dCamera.position.z += z
                    if (hasControls) {
                        this.map3dControls.target.x += x
                        this.map3dControls.target.y += y
                        this.map3dControls.target.z += z
                    }
                }

                let moved = false
                //Legacy comment translated to English.
                switch (actionKey) {
                    case 'w': moveBy(0, SPEED, 0); moved = true; break
                    case 's': moveBy(0, -SPEED, 0); moved = true; break
                    case 'a': moveBy(-SPEED, 0, 0); moved = true; break
                    case 'd': moveBy(SPEED, 0, 0); moved = true; break
                    case 'q': moveBy(0, 0, SPEED); moved = true; break
                    case 'e': moveBy(0, 0, -SPEED); moved = true; break
                    case 'r': this.resetMap3DView(); moved = true; break
                    case 'f': this.fitMap3DView(); moved = true; break
                    case '+':
                    case '=': this.map3dCamera.position.z -= SPEED * 2; moved = true; break
                    case '-': this.map3dCamera.position.z += SPEED * 2; moved = true; break
                }
                if (moved) {
                    if (this.map3dControls) {
                        this.map3dControls.update()
                    }
                    this.map3dNeedsAnimationUpdate = true
                    console.debug('[Map3D Keyboard] key:', actionKey, {
                        rawKey: e.key,
                        code: e.code,
                        camera: {
                            x: this.map3dCamera.position.x,
                            y: this.map3dCamera.position.y,
                            z: this.map3dCamera.position.z
                        },
                        target: hasControls ? {
                            x: this.map3dControls.target.x,
                            y: this.map3dControls.target.y,
                            z: this.map3dControls.target.z
                        } : null
                    })
                    e.preventDefault()
                    e.stopPropagation()
                }
            }
            window.addEventListener('keydown', this._map3dKeydownHandler, true)
            console.debug('[Map3D Keyboard] shortcut listener registered')
        },

        //Legacy comment translated to English.
        initMap3D() {
            const container = document.getElementById('map3dContainer')
            if (!container) return
            
            //Legacy comment translated to English.
            this.buildTreeData()
            
            //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            this.map3dScene = new THREE.Scene()
            //Legacy comment translated to English.
            const gradientTexture = this.createGradientBackground()
            this.map3dScene.background = gradientTexture
            
            //Legacy comment translated to English.
            this.map3dScene.fog = null
            
            //Legacy comment translated to English.
            const width = container.clientWidth
            const height = container.clientHeight
            //Legacy comment translated to English.
            this.map3dCamera = new THREE.PerspectiveCamera(75, width / height, 0.1, 5000)
            this.map3dCamera.position.set(0, 0, 50)
            
            //Legacy comment translated to English.
            this.map3dRenderer = new THREE.WebGLRenderer({ 
                antialias: true,
                logarithmicDepthBuffer: false,
                precision: 'highp' //Legacy comment translated to English.
            })
            this.map3dRenderer.setSize(width, height)
            //Legacy comment translated to English.
            this.map3dRenderer.setPixelRatio(Math.min(window.devicePixelRatio, 2))
            container.appendChild(this.map3dRenderer.domElement)
            
            //Legacy comment translated to English.
            if (typeof THREE.OrbitControls !== 'undefined') {
                this.map3dControls = new THREE.OrbitControls(this.map3dCamera, this.map3dRenderer.domElement)
                
                //Legacy comment translated to English.
                this.map3dControls.enableDamping = true
                this.map3dControls.dampingFactor = 0.05
                
                //Legacy comment translated to English.
                this.map3dControls.enableZoom = true
                this.map3dControls.zoomSpeed = 1.2
                this.map3dControls.minDistance = 10
                this.map3dControls.maxDistance = 500 //Legacy comment translated to English.
                
                //Legacy comment translated to English.
                this.map3dControls.enableRotate = true
                this.map3dControls.rotateSpeed = 0.8
                
                //Legacy comment translated to English.
                this.map3dControls.enablePan = true
                this.map3dControls.panSpeed = 0.8
                this.map3dControls.screenSpacePanning = true
                
                //Legacy comment translated to English.
                this.map3dCamera.position.set(30, 30, 50)
                this.map3dControls.target.set(0, 0, 0)
                this.map3dControls.update()
            } else {
                console.warn('OrbitControls 未找到，3D 场景将无法通过鼠标控制')
            }
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            const ambientLight = new THREE.AmbientLight(0xffffff, 0.6) //Legacy comment translated to English.
            this.map3dScene.add(ambientLight)
            
            //Legacy comment translated to English.
            const directionalLight1 = new THREE.DirectionalLight(0xffffff, 0.8)
            directionalLight1.position.set(20, 20, 20)
            directionalLight1.castShadow = false
            this.map3dScene.add(directionalLight1)
            
            //Legacy comment translated to English.
            const directionalLight2 = new THREE.DirectionalLight(0x88ccff, 0.5) //Legacy comment translated to English.
            directionalLight2.position.set(-20, 10, -20)
            this.map3dScene.add(directionalLight2)
            
            //Legacy comment translated to English.
            const directionalLight3 = new THREE.DirectionalLight(0xffffff, 0.4)
            directionalLight3.position.set(0, 0, 50)
            this.map3dScene.add(directionalLight3)
            
            //Legacy comment translated to English.
            const pointLight = new THREE.PointLight(0xffffff, 0.6, 0) //Legacy comment translated to English.
            pointLight.position.set(0, 20, 0)
            this.map3dScene.add(pointLight)
            
            //Legacy comment translated to English.
            this.renderTreeNodes()
            
            //Legacy comment translated to English.
            this.animateMap3D()
            
            //Legacy comment translated to English.
            window.addEventListener('resize', this.handleMap3DResize)

            //Legacy comment translated to English.
            this.registerMap3DKeyboard()

            //Legacy comment translated to English.
            this.$nextTick(() => {
                setTimeout(() => this.fitMap3DView(), 200)
            })
        },
        
        //Legacy comment translated to English.
        buildTreeData() {
            if (!this.promptOpt || this.promptOpt.length === 0) {
                this.map3dTreeData = null
                return
            }
            
            const tree = {}
            const currentPromptId = this.promptid
            
            //Legacy comment translated to English.
            this.promptOpt.forEach(prompt => {
                const fullVersion = prompt.fullVersion || prompt.label
                if (!fullVersion) return
                
                //Legacy comment translated to English.
                const parts = fullVersion.split('-')
                if (parts.length < 2) return
                
                const rangeName = parts[0] // 2023.12.14.1
                const tacticPart = parts[1] // T1.1
                const aimingPart = parts[2] || '' // A123
                
                //Legacy comment translated to English.
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
                
                //Legacy comment translated to English.
                const tacticParts = tacticPart.replace('T', '').split('.')
                let currentNode = rangeNode.children
                let lastTacticNode = null
                
                tacticParts.forEach((part, index) => {
                    const key = `T${tacticParts.slice(0, index + 1).join('.')}`
                    
                    if (!currentNode[key]) {
                        //Legacy comment translated to English.
                        const parentFullPath = index > 0 
                            ? `${rangeName}-T${tacticParts.slice(0, index).join('.')}` // for example: "Range1-T2.1"
                            : rangeName // for example: "Range1"
                        
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
                
                //Legacy comment translated to English.
                if (!lastTacticNode) {
                    console.warn('无法找到 Tactic 节点:', tacticPart)
                    return
                }
                
                //Legacy comment translated to English.
                if (aimingPart) {
                    //Legacy comment translated to English.
                    const aimingKey = `${tacticPart}-${aimingPart}` // for example: "T1.1-A1", "T1.2-A1"
                    if (!lastTacticNode.children[aimingKey]) {
                        lastTacticNode.children[aimingKey] = {
                            type: 'aiming',
                            name: aimingPart.replace('A', ''), //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
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
        
        // --- optimizefeature ---
        //Legacy comment translated to English.
        async checkPromptCatalyzerStatus() {
            try {
                const response = await servicePR.get('/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/CheckStatus');
                console.log('CheckStatus 完整响应:', response);
                
                //Legacy comment translated to English.
                if (response.data && response.data.success && response.data.data) {
                    return response.data.data.isInitialized;
                }
                return false;
            } catch (error) {
                console.error('检查初始化状态失败:', error);
                return false;
            }
        },

        //Legacy comment translated to English.
        async loadAvailableModels() {
            this.loadingModels = true;
            try {
                const response = await servicePR.get('/api/Senparc.Xncf.AgentsManager/PromptCatalyzerInitAppService/GetAvailableModels');
                console.log('GetAvailableModels 完整响应:', response);
                
                //Legacy comment translated to English.
                if (response.data && response.data.success) {
                    this.availableModelsForInit = response.data.data.models || [];
                    
                    //Legacy comment translated to English.
                    if (response.data.data.recommendedModelId) {
                        this.selectedModelIdForInit = response.data.data.recommendedModelId;
                    } else if (this.availableModelsForInit.length > 0) {
                        this.selectedModelIdForInit = this.availableModelsForInit[0].id;
                    }
                    
                    console.log('加载到', this.availableModelsForInit.length, '个可用 Model');
                } else {
                    // handlingerrorresponse
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

        // executeinitialize
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
                
                //Legacy comment translated to English.
                if (response.data && response.data.success) {
                    const initData = response.data.data || {};
                    this.$message({
                        message: `✅ 初始化成功！已创建 PromptCatalyzer Agent，PromptCode: ${initData.promptCode || '已创建'}`,
                        type: 'success',
                        duration: 6000,
                        showClose: true
                    });
                    
                    this.promptCatalyzerInitVisible = false;
                    
                    //Legacy comment translated to English.
                    console.log('初始化成功，刷新页面数据...');
                    await this.getFieldList();
                    
                    //Legacy comment translated to English.
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

        //Legacy comment translated to English.
        proceedWithOptimization() {
            //Legacy comment translated to English.
            this.optimizeRequirement = '';
            this.optimizeErrorText = '';
            this.optimizeDialogVisible = true;
        },
        
        //Legacy comment translated to English.
        async checkScoreAndSuggestOptimization(resultData, scoreType) {
            try {
                //Legacy comment translated to English.
                let finalScore = null;
                if (resultData && typeof resultData === 'object') {
                    finalScore = resultData.finalScore;
                } else if (typeof resultData === 'number') {
                    finalScore = resultData;
                }
                
                //Legacy comment translated to English.
                if (finalScore === null || finalScore === undefined || finalScore === -1) {
                    console.log('无法获取有效分数，跳过优化提示');
                    return;
                }
                
                console.log(`${scoreType}完成，最终分数: ${finalScore}`);
                
                //Legacy comment translated to English.
                const optimizationThreshold = 6.0;
                
                if (finalScore < optimizationThreshold) {
                    //Legacy comment translated to English.
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
                        //Legacy comment translated to English.
                        console.log('用户确认进行优化');
                        await this.openOptimizeDialog();
                    }).catch(() => {
                        //Legacy comment translated to English.
                        console.log('用户取消优化');
                    });
                } else {
                    //Legacy comment translated to English.
                    console.log(`分数${finalScore.toFixed(1)}较高，无需提示优化`);
                }
            } catch (error) {
                console.error('检查分数并提示优化时出错:', error);
                //Legacy comment translated to English.
            }
        },
        
        //Legacy comment translated to English.
        async checkPromptAverageScoreAndSuggest() {
            try {
                //Legacy comment translated to English.
                if (!this.promptid) {
                    return;
                }
                
                const selectedPrompt = this.promptOpt.find(item => item.value === this.promptid);
                if (!selectedPrompt || !selectedPrompt.evalAvgScore) {
                    return;
                }
                
                const avgScore = selectedPrompt.evalAvgScore;
                
                //Legacy comment translated to English.
                if (avgScore === -1 || avgScore === null || avgScore === undefined) {
                    console.log('当前 Prompt 尚无平均分数');
                    return;
                }
                
                console.log(`当前 Prompt 平均分数: ${avgScore}`);
                
                //Legacy comment translated to English.
                const optimizationThreshold = 6.0;
                
                //Legacy comment translated to English.
                if (avgScore < optimizationThreshold) {
                    //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            console.log('检查 PromptCatalyzer 初始化状态...');
            const isInitialized = await this.checkPromptCatalyzerStatus();
            
            if (!isInitialized) {
                //Legacy comment translated to English.
                this.$message({
                    message: '🚀 首次使用需要初始化 PromptCatalyzer，正在加载可用的 AI Model...',
                    type: 'info',
                    duration: 3000
                });
                
                //Legacy comment translated to English.
                await this.loadAvailableModels();
                
                if (this.availableModelsForInit.length === 0) {
                    this.$message.error('没有找到可用的 AI Model，请先在 AIKernel 模块中配置 Chat 类型的 Model');
                    return;
                }
                
                //Legacy comment translated to English.
                this.promptCatalyzerInitVisible = true;
                return;
            }
            
            //Legacy comment translated to English.
            console.log('PromptCatalyzer 已初始化，打开优化对话框');
            this.optimizeRequirement = '';
            this.optimizeErrorText = '';
            this.optimizeDialogVisible = true;
        },
        //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            let promptCode = '';
            const selectedPrompt = this.promptOpt.find(item => item.value === this.promptid);
            if (selectedPrompt) {
                if (selectedPrompt.fullVersion) {
                    promptCode = selectedPrompt.fullVersion;
                } else if (selectedPrompt.label && selectedPrompt.label.includes('-T')) {
                    promptCode = selectedPrompt.label; 
                }
            }
            
            //Legacy comment translated to English.
            if (!promptCode && this.promptDetail && this.promptDetail.fullVersion) {
                promptCode = this.promptDetail.fullVersion;
            }

            if (!promptCode) {
                this.$message.error('无法获取当前Prompt的版本号(Prompt Code)');
                return;
            }

            //Legacy comment translated to English.
            const promptDetail = this.promptDetail;
            if (!promptDetail) {
                this.$message.error('无法获取当前 Prompt 的详细信息');
                return;
            }

            //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
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
                        //Legacy comment translated to English.
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
                        const latestIndex = this.outputList.length - 1;
                        const item = this.outputList[latestIndex];
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
                            await this.saveManualScore(item, latestIndex);
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
        //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        countTreeNodes(nodeData) {
            if (!nodeData || typeof nodeData !== 'object') return 0
            
            let count = 0
            Object.keys(nodeData).forEach(key => {
                const node = nodeData[key]
                count++ //Legacy comment translated to English.
                
                const isExpanded = node.expanded !== false
                if (isExpanded && node.children && Object.keys(node.children).length > 0) {
                    count += this.countTreeNodes(node.children)
                }
            })
            
            return count
        },
        
        //Legacy comment translated to English.
        calculateScoreStatistics() {
            const scores = []
            
            //Legacy comment translated to English.
            if (this.promptOpt && this.promptOpt.length > 0) {
                this.promptOpt.forEach(prompt => {
                    let score = null
                    
                    //Legacy comment translated to English.
                    if (prompt.evalMaxScore !== undefined && 
                        prompt.evalMaxScore !== null && 
                        prompt.evalMaxScore !== -1 && 
                        prompt.evalMaxScore !== '-1') {
                        score = prompt.evalMaxScore
                    }
                    //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            scores.sort((a, b) => b - a)
            
            const result = {
                scores: scores,
                min: Math.min(...scores),
                max: Math.max(...scores),
                count: scores.length,
                //Legacy comment translated to English.
                getPercentileRank: (score) => {
                    //Legacy comment translated to English.
                    const rank = scores.filter(s => s > score).length
                    return ((scores.length - rank) / scores.length) * 100
                }
            }
            
            console.log('📊 评分统计:', {
                总数: result.count,
                最高分: result.max,
                最低分: result.min,
                分数列表: scores.slice(0, 10) //Legacy comment translated to English.
            })
            
            return result
        },
        
        //Legacy comment translated to English.
        renderTreeNodes() {
            if (!this.map3dTreeData) return
            
            //Legacy comment translated to English.
            this.scoreStatistics = this.calculateScoreStatistics()
            
            this.map3dNodes = []
            //Legacy comment translated to English.
            if (!this.map3dNodeMap) {
                this.map3dNodeMap = new Map()
            }
            const raycaster = new THREE.Raycaster()
            const mouse = new THREE.Vector2()
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            let currentX = 0
            
            const renderNode = (nodeData, parentPosition, depth) => {
                if (!nodeData || typeof nodeData !== 'object') return
                
                const keys = Object.keys(nodeData)
                
                keys.forEach((key, index) => {
                    const node = nodeData[key]
                    const isExpanded = node.expanded !== false //Legacy comment translated to English.
                    
                    //Legacy comment translated to English.
                    if (node.type === 'aiming') {
                        return //Legacy comment translated to English.
                    }
                    
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
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
                    
                    //Legacy comment translated to English.
                    const nodeSpacing = 15  //Legacy comment translated to English.
                    
                    //Legacy comment translated to English.
                    const x = currentX  //Legacy comment translated to English.
                    const y = -depth * 20   //Legacy comment translated to English.
                    const z = 0             //Legacy comment translated to English.
                    
                    //Legacy comment translated to English.
                    currentX += nodeSpacing
                    
                    console.log(`📍 创建T节点: ${key}, 位置(${x}, ${y}, ${z}), depth=${depth}`)
                    
                    //Legacy comment translated to English.
                    const hasCurrent = node.prompts && node.prompts.some(p => p.isCurrent)
                    
                    //Legacy comment translated to English.
                    let geometry, material, glowGeometry, glowMaterial
                    if (node.type === 'range') {
                        //Legacy comment translated to English.
                        geometry = new THREE.BoxGeometry(5, 5, 5)
                        material = new THREE.MeshStandardMaterial({ 
                            color: hasCurrent ? 0xff6b6b : 0x4ecdc4,
                            metalness: 0.3,
                            roughness: 0.4,
                            emissive: hasCurrent ? 0xff3333 : 0x004444,
                            emissiveIntensity: hasCurrent ? 0.8 : 0.1
                        })
                        
                        //Legacy comment translated to English.
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
                        //Legacy comment translated to English.
                        // CylinderGeometry(radiusTop, radiusBottom, height, radialSegments)
                        geometry = new THREE.CylinderGeometry(2, 2, 4, 32)
                        material = new THREE.MeshStandardMaterial({ 
                            color: hasCurrent ? 0xffd93d : 0x95e1d3,
                            metalness: 0.5,
                            roughness: 0.3,
                            emissive: hasCurrent ? 0xffaa00 : 0x004444,
                            emissiveIntensity: hasCurrent ? 0.9 : 0.1
                        })
                        
                        //Legacy comment translated to English.
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
                        //Legacy comment translated to English.
                        console.error('意外的节点类型:', node.type, key)
                        return
                    }
                    
                    const mesh = new THREE.Mesh(geometry, material)
                    //Legacy comment translated to English.
                    mesh.position.set(x, y, z)
                    
                    //Legacy comment translated to English.
                    if (node.type === 'tactic') {
                        mesh.rotation.x = Math.PI / 2  //Legacy comment translated to English.
                    }
                    
                    mesh.userData = { 
                        node, 
                        key, 
                        depth, 
                        type: node.type,
                        targetPosition: { x, y, z },
                        initialScale: 0.1 //Legacy comment translated to English.
                    }
                    mesh.castShadow = true
                    mesh.receiveShadow = true
                    
                    //Legacy comment translated to English.
                    mesh.scale.set(0.1, 0.1, 0.1)
                    
                    //Legacy comment translated to English.
                    let glowMesh = null
                    if (glowGeometry && glowMaterial) {
                        glowMesh = new THREE.Mesh(glowGeometry, glowMaterial)
                        glowMesh.position.set(x, y, z)
                        
                        //Legacy comment translated to English.
                        if (node.type === 'tactic') {
                            glowMesh.rotation.x = Math.PI / 2
                        }
                        
                        glowMesh.scale.set(0.1, 0.1, 0.1)
                        this.map3dScene.add(glowMesh)
                    }
                    
                    //Legacy comment translated to English.
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
                    
                    //Legacy comment translated to English.
                    const canvas = document.createElement('canvas')
                    const context = canvas.getContext('2d')
                    
                    //Legacy comment translated to English.
                    const prefixFontSize = hasCurrent ? 180 : 160  //Legacy comment translated to English.
                    const mainFontSize = hasCurrent ? 280 : 240   //Legacy comment translated to English.
                    const rangeFontSize = hasCurrent ? 220 : 190   //Legacy comment translated to English.
                    
                    //Legacy comment translated to English.
                    if (labelPrefix === 'T') {
                        context.font = `bold ${mainFontSize}px 'Arial Black', 'Microsoft YaHei', Arial, sans-serif`
                    } else {
                        context.font = `bold ${rangeFontSize}px 'Microsoft YaHei', 'PingFang SC', Arial, sans-serif`
                    }
                    const textWidth = context.measureText(labelText).width
                    
                    const padding = 50  //Legacy comment translated to English.
                    const borderRadius = 30  //Legacy comment translated to English.
                    
                    //Legacy comment translated to English.
                    canvas.width = Math.max(800, textWidth + padding * 4)
                    canvas.height = 512  //Legacy comment translated to English.
                    
                    //Legacy comment translated to English.
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
                    
                    //Legacy comment translated to English.
                    context.fillStyle = hasCurrent 
                        ? 'rgba(255, 107, 107, 0.25)'  //Legacy comment translated to English.
                        : 'rgba(255, 255, 255, 0.15)'  //Legacy comment translated to English.
                    drawRoundedRect(padding, padding, canvas.width - padding * 2, canvas.height - padding * 2, borderRadius)
                    context.fill()
                    
                    //Legacy comment translated to English.
                    context.strokeStyle = hasCurrent 
                        ? 'rgba(255, 150, 150, 0.6)'   //Legacy comment translated to English.
                        : 'rgba(255, 255, 255, 0.3)'   //Legacy comment translated to English.
                    context.lineWidth = 3
                    drawRoundedRect(padding, padding, canvas.width - padding * 2, canvas.height - padding * 2, borderRadius)
                    context.stroke()
                    
                    //Legacy comment translated to English.
                    const highlightGradient = context.createLinearGradient(0, padding, 0, canvas.height / 2)
                    highlightGradient.addColorStop(0, 'rgba(255, 255, 255, 0.4)')
                    highlightGradient.addColorStop(1, 'rgba(255, 255, 255, 0)')
                    context.fillStyle = highlightGradient
                    drawRoundedRect(padding, padding, canvas.width - padding * 2, (canvas.height - padding * 2) / 2, borderRadius)
                    context.fill()
                    
                    //Legacy comment translated to English.
                    context.textAlign = 'center'
                    context.textBaseline = 'middle'
                    context.shadowColor = 'rgba(0, 0, 0, 0.8)'
                    context.shadowBlur = 20  //Legacy comment translated to English.
                    context.shadowOffsetX = 5  //Legacy comment translated to English.
                    context.shadowOffsetY = 5  //Legacy comment translated to English.
                    
                    let mainTextY = canvas.height / 2
                    
                    //Legacy comment translated to English.
                    if (labelPrefix === 'T') {
                        //Legacy comment translated to English.
                        const combinedText = `T${labelText}`
                        
                        //Legacy comment translated to English.
                        context.fillStyle = '#ffffff'
                        context.font = `bold ${mainFontSize}px 'Arial Black', 'Microsoft YaHei', Arial, sans-serif`
                        context.fillText(combinedText, canvas.width / 2, canvas.height / 2)
                        
                        mainTextY = canvas.height / 2
                    } else {
                        //Legacy comment translated to English.
                        context.fillStyle = '#ffffff'
                        context.font = `bold ${rangeFontSize}px 'Microsoft YaHei', 'PingFang SC', Arial, sans-serif`
                        context.fillText(labelText, canvas.width / 2, canvas.height / 2)
                        
                        mainTextY = canvas.height / 2
                    }
                    
                    //Legacy comment translated to English.
                    if (node.prompts && node.prompts.length > 0) {
                        context.font = `bold ${hasCurrent ? 110 : 96}px Arial`  //Legacy comment translated to English.
                        context.fillStyle = 'rgba(255, 255, 255, 0.85)'
                        context.shadowBlur = 12
                        const promptText = ncfST('{0} 个结果', node.prompts.length)
                        context.fillText(promptText, canvas.width / 2, mainTextY + 120)  //Legacy comment translated to English.
                    }
                    
                    const texture = new THREE.CanvasTexture(canvas)
                    texture.needsUpdate = true
                    const spriteMaterial = new THREE.SpriteMaterial({ 
                        map: texture, 
                        transparent: true,
                        alphaTest: 0.1,
                        opacity: 0 //Legacy comment translated to English.
                    })
                    const sprite = new THREE.Sprite(spriteMaterial)
                    //Legacy comment translated to English.
                    const labelZOffset = 3  //Legacy comment translated to English.
                    sprite.position.set(x, y - (node.type === 'range' ? 5 : 4), z + labelZOffset)
                    //Legacy comment translated to English.
                    const spriteWidth = (canvas.width / 1024) * 18  //Legacy comment translated to English.
                    const spriteHeight = (canvas.height / 256) * 4.5  //Legacy comment translated to English.
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
                    
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    
                    //Legacy comment translated to English.
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
                        childrenMeshes: [],  //Legacy comment translated to English.
                        line: null, 
                        dot: null,
                        animationProgress: 0,
                        hasCurrent: hasCurrent
                    }
                    
                    this.map3dNodes.push(createdNodeData)
                    
                    //Legacy comment translated to English.
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
                    
                    //Legacy comment translated to English.
                    let line = null
                    let dot = null
                    
                    //Legacy comment translated to English.
                    if (isExpanded && Object.keys(aimingChildren).length > 0) {
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        const currentParentNodeData = this.map3dNodes[this.map3dNodes.length - 1]
                        
                        const aimingKeys = Object.keys(aimingChildren)
                        const aimingSpacing = 16 //Legacy comment translated to English.
                        
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        
                        const aimingNodesData = [] //Legacy comment translated to English.
                        
                        aimingKeys.forEach((aimingKey, aimingIndex) => {
                            const aimingNode = aimingChildren[aimingKey]
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            const aimingX = x //Legacy comment translated to English.
                            const aimingY = y //Legacy comment translated to English.
                            //Legacy comment translated to English.
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
                                
                                //Legacy comment translated to English.
                                const aimingHasCurrent = aimingNode.prompts && aimingNode.prompts.some(p => p.isCurrent)
                                
                                //Legacy comment translated to English.
                                let sphereSize = 1.5  //Legacy comment translated to English.
                                let sphereColor = 0xa8e6cf  //Legacy comment translated to English.
                                let emissiveColor = 0x003333
                                let emissiveIntensity = 0.05
                                let score = null  //Legacy comment translated to English.
                                
                                //Legacy comment translated to English.
                                if (aimingNode.prompts && aimingNode.prompts.length > 0) {
                                    const promptId = aimingNode.prompts[0].id
                                    const fullPromptData = this.promptOpt.find(p => 
                                        (p.idkey || p.value) == promptId
                                    )
                                    
                                    //Legacy comment translated to English.
                                    console.log('🔍 Aiming节点评分数据检查:', {
                                        promptId,
                                        fullPromptData,
                                        evalMaxScore: fullPromptData?.evalMaxScore,
                                        finalScore: fullPromptData?.finalScore
                                    })
                                    
                                    //Legacy comment translated to English.
                                    //Legacy comment translated to English.
                                    if (fullPromptData) {
                                        //Legacy comment translated to English.
                                        if (fullPromptData.finalScore !== undefined && 
                                            fullPromptData.finalScore !== null && 
                                            fullPromptData.finalScore !== -1 && 
                                            fullPromptData.finalScore !== '-1') {
                                            score = fullPromptData.finalScore
                                        }
                                        //Legacy comment translated to English.
                                        else if (fullPromptData.evalMaxScore !== undefined && 
                                                 fullPromptData.evalMaxScore !== null && 
                                                 fullPromptData.evalMaxScore !== -1 && 
                                                 fullPromptData.evalMaxScore !== '-1') {
                                            score = fullPromptData.evalMaxScore
                                        }
                                        //Legacy comment translated to English.
                                        else if (fullPromptData.evalAvgScore !== undefined && 
                                                 fullPromptData.evalAvgScore !== null && 
                                                 fullPromptData.evalAvgScore !== -1 && 
                                                 fullPromptData.evalAvgScore !== '-1') {
                                            score = fullPromptData.evalAvgScore
                                        }
                                    }
                                    
                                    if (score !== null && this.scoreStatistics) {
                                        //Legacy comment translated to English.
                                        const percentile = this.scoreStatistics.getPercentileRank(score)
                                        const allScores = this.scoreStatistics.scores
                                        const rank = allScores.filter(s => s > score).length + 1
                                        const isFirst = rank === 1
                                        
                                        console.log('✅ 使用评分:', score, '排名:', rank, '百分位:', percentile.toFixed(1) + '%', isFirst ? '🥇第一名!' : '')
                                        
                                        //Legacy comment translated to English.
                                        if (isFirst) {
                                            //Legacy comment translated to English.
                                            sphereSize = 2.5
                                            sphereColor = 0xb24df5  //Legacy comment translated to English.
                                            emissiveColor = 0xff00ff  //Legacy comment translated to English.
                                            emissiveIntensity = 0.6
                                        }
                                        //Legacy comment translated to English.
                                        else if (percentile >= 80) {
                                            //Legacy comment translated to English.
                                            sphereSize = 2.2
                                            sphereColor = 0x00d4aa
                                            emissiveColor = 0x00ffcc
                                            emissiveIntensity = 0.4
                                        } else if (percentile >= 60) {
                                            //Legacy comment translated to English.
                                            sphereSize = 1.9
                                            sphereColor = 0x52c41a
                                            emissiveColor = 0x66ff66
                                            emissiveIntensity = 0.3
                                        } else if (percentile >= 40) {
                                            //Legacy comment translated to English.
                                            sphereSize = 1.6
                                            sphereColor = 0xfaad14
                                            emissiveColor = 0xffcc00
                                            emissiveIntensity = 0.2
                                        } else if (percentile >= 20) {
                                            //Legacy comment translated to English.
                                            sphereSize = 1.3
                                            sphereColor = 0xff7875
                                            emissiveColor = 0xff6666
                                            emissiveIntensity = 0.15
                                        } else {
                                            //Legacy comment translated to English.
                                            sphereSize = 1.0
                                            sphereColor = 0xf5222d
                                            emissiveColor = 0xff3333
                                            emissiveIntensity = 0.1
                                        }
                                    }
                                }
                                
                                //Legacy comment translated to English.
                                if (aimingHasCurrent) {
                                    sphereColor = 0xffd93d
                                    emissiveColor = 0xffaa00
                                    emissiveIntensity = 0.7
                                }
                                
                                //Legacy comment translated to English.
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
                                
                                //Legacy comment translated to English.
                                let aimingGlowMesh = null
                                if (score !== null && this.scoreStatistics) {
                                    const allScores = this.scoreStatistics.scores
                                    const rank = allScores.filter(s => s > score).length + 1
                                    if (rank === 1) {
                                        //Legacy comment translated to English.
                                        const aimingGlowGeometry = new THREE.SphereGeometry(sphereSize * 1.3, 24, 24)
                                        const aimingGlowMaterial = new THREE.MeshBasicMaterial({
                                            color: 0xb24df5,  //Legacy comment translated to English.
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
                                
                                //Legacy comment translated to English.
                                const aimingCanvas = document.createElement('canvas')
                                const aimingContext = aimingCanvas.getContext('2d')
                                
                                //Legacy comment translated to English.
                                const aimingMatch = aimingKey.match(/-A(\d+)$/)
                                const aimingLabelText = aimingMatch ? aimingMatch[1] : aimingNode.name
                                
                                //Legacy comment translated to English.
                                const aimingPrefixFontSize = aimingHasCurrent ? 180 : 160  //Legacy comment translated to English.
                                const aimingMainFontSize = aimingHasCurrent ? 280 : 240   //Legacy comment translated to English.
                                
                                //Legacy comment translated to English.
                                const aimingCombinedText = `A${aimingLabelText}`
                                
                                //Legacy comment translated to English.
                                aimingContext.font = `bold ${aimingMainFontSize}px 'Arial Black', 'Microsoft YaHei', Arial, sans-serif`
                                const textWidth = aimingContext.measureText(aimingCombinedText).width
                                
                                const aimingPadding = 50  //Legacy comment translated to English.
                                const aimingBorderRadius = 30  //Legacy comment translated to English.
                                
                                //Legacy comment translated to English.
                                aimingCanvas.width = Math.max(800, textWidth + aimingPadding * 4)
                                aimingCanvas.height = 512  //Legacy comment translated to English.
                                
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
                                
                                //Legacy comment translated to English.
                                aimingContext.fillStyle = aimingHasCurrent 
                                    ? 'rgba(255, 107, 107, 0.25)' 
                                    : 'rgba(255, 255, 255, 0.15)'
                                drawRoundedRect(aimingPadding, aimingPadding, aimingCanvas.width - aimingPadding * 2, aimingCanvas.height - aimingPadding * 2, aimingBorderRadius)
                                aimingContext.fill()
                                
                                //Legacy comment translated to English.
                                aimingContext.strokeStyle = aimingHasCurrent 
                                    ? 'rgba(255, 150, 150, 0.6)' 
                                    : 'rgba(255, 255, 255, 0.3)'
                                aimingContext.lineWidth = 3
                                drawRoundedRect(aimingPadding, aimingPadding, aimingCanvas.width - aimingPadding * 2, aimingCanvas.height - aimingPadding * 2, aimingBorderRadius)
                                aimingContext.stroke()
                                
                                //Legacy comment translated to English.
                                const aimingHighlightGradient = aimingContext.createLinearGradient(0, aimingPadding, 0, aimingCanvas.height / 2)
                                aimingHighlightGradient.addColorStop(0, 'rgba(255, 255, 255, 0.4)')
                                aimingHighlightGradient.addColorStop(1, 'rgba(255, 255, 255, 0)')
                                aimingContext.fillStyle = aimingHighlightGradient
                                drawRoundedRect(aimingPadding, aimingPadding, aimingCanvas.width - aimingPadding * 2, (aimingCanvas.height - aimingPadding * 2) / 2, aimingBorderRadius)
                                aimingContext.fill()
                                
                                //Legacy comment translated to English.
                                aimingContext.textAlign = 'center'
                                aimingContext.textBaseline = 'middle'
                                aimingContext.shadowColor = 'rgba(0, 0, 0, 0.8)'
                                aimingContext.shadowBlur = 20
                                aimingContext.shadowOffsetX = 5
                                aimingContext.shadowOffsetY = 5
                                
                                //Legacy comment translated to English.
                                aimingContext.fillStyle = '#ffffff'
                                aimingContext.font = `bold ${aimingMainFontSize}px 'Arial Black', 'Microsoft YaHei', Arial, sans-serif`
                                aimingContext.fillText(aimingCombinedText, aimingCanvas.width / 2, aimingCanvas.height / 2)
                                
                                // create sprite
                                const aimingTexture = new THREE.CanvasTexture(aimingCanvas)
                                const aimingSpriteMaterial = new THREE.SpriteMaterial({ 
                                    map: aimingTexture, 
                                    transparent: true,
                                    alphaTest: 0.1,
                                    opacity: 0
                                })
                                const aimingSprite = new THREE.Sprite(aimingSpriteMaterial)
                                //Legacy comment translated to English.
                                const aimingLabelZOffset = 3  //Legacy comment translated to English.
                                aimingSprite.position.set(aimingX, aimingY - 4, aimingZ + aimingLabelZOffset)
                                //Legacy comment translated to English.
                                const spriteWidth = (aimingCanvas.width / 1024) * 18  //Legacy comment translated to English.
                                const spriteHeight = (aimingCanvas.height / 256) * 4.5  //Legacy comment translated to English.
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
                                
                                //Legacy comment translated to English.
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
                            
                            //Legacy comment translated to English.
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
                    
                    //Legacy comment translated to English.
                    if (isExpanded && Object.keys(tacticChildren).length > 0) {
                        //Legacy comment translated to English.
                        renderNode(tacticChildren, { x, y, z }, depth + 1)
                        
                        //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            currentX = 0  //Legacy comment translated to English.
            Object.keys(this.map3dTreeData).forEach(key => {
                renderNode({ [key]: this.map3dTreeData[key] }, null, 0)
            })
            
            //Legacy comment translated to English.
            const rangeNodes = this.map3dNodes.filter(n => n.node.type === 'range')
            rangeNodes.forEach(rangeNodeData => {
                //Legacy comment translated to English.
                const childTacticNodes = this.map3dNodes.filter(n => 
                    n.depth === 1 && n.parentPosition && 
                    Math.abs(n.parentPosition.y - rangeNodeData.position.y) < 1
                )
                
                if (childTacticNodes.length > 0) {
                    //Legacy comment translated to English.
                    const childXPositions = childTacticNodes.map(n => n.position.x)
                    const minX = Math.min(...childXPositions)
                    const maxX = Math.max(...childXPositions)
                    const centerX = (minX + maxX) / 2
                    
                    //Legacy comment translated to English.
                    rangeNodeData.mesh.position.x = centerX
                    if (rangeNodeData.glowMesh) {
                        rangeNodeData.glowMesh.position.x = centerX
                    }
                    rangeNodeData.sprite.position.x = centerX
                    rangeNodeData.position.x = centerX
                    
                    console.log(`📦 调整Range节点 ${rangeNodeData.key} 到中点: ${centerX} (子节点范围: ${minX} ~ ${maxX})`)
                    
                    //Legacy comment translated to English.
                    childTacticNodes.forEach(childData => {
                        childData.parentPosition = {
                            x: centerX,
                            y: rangeNodeData.position.y,
                            z: rangeNodeData.position.z
                        }
                    })
                }
            })
            
            //Legacy comment translated to English.
            let maxDepth = 0
            let minX = Infinity
            let maxX = -Infinity
            let minZ = 0
            let maxZ = 0
            
            this.map3dNodes.forEach(nodeData => {
                maxDepth = Math.max(maxDepth, nodeData.depth)
                
                //Legacy comment translated to English.
                if (nodeData.node.type !== 'aiming') {
                    minX = Math.min(minX, nodeData.position.x)
                    maxX = Math.max(maxX, nodeData.position.x)
                }
                
                //Legacy comment translated to English.
                if (nodeData.node.type === 'aiming') {
                    minZ = Math.min(minZ, nodeData.position.z)
                    maxZ = Math.max(maxZ, nodeData.position.z)
                }
            })
            
            const treeWidth = maxX - minX  //Legacy comment translated to English.
            const treeDepth = maxDepth * 20  //Legacy comment translated to English.
            const treeZSpan = maxZ - minZ  //Legacy comment translated to English.
            
            const treeCenterX = (maxX + minX) / 2
            const treeCenterY = -treeDepth / 2  //Legacy comment translated to English.
            const treeCenterZ = (maxZ + minZ) / 2  //Legacy comment translated to English.
            
            console.log('📐 树的范围计算:', {
                treeWidth: treeWidth.toFixed(2),
                treeDepth: treeDepth.toFixed(2),
                treeZSpan: treeZSpan.toFixed(2),
                xRange: `${minX.toFixed(2)} ~ ${maxX.toFixed(2)}`,
                yRange: `0 ~ ${(-maxDepth * 20).toFixed(2)}`,
                zRange: `${minZ.toFixed(2)} ~ ${maxZ.toFixed(2)}`
            })
            
            //Legacy comment translated to English.
            const treeCenter = {
                x: treeCenterX,
                y: treeCenterY,
                z: treeCenterZ
            }
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            const effectiveWidth = treeWidth
            const effectiveDepth = treeDepth
            const effectiveZSpan = treeZSpan
            
            //Legacy comment translated to English.
            const boundingBoxDiagonal = Math.sqrt(
                effectiveWidth * effectiveWidth + 
                effectiveDepth * effectiveDepth + 
                effectiveZSpan * effectiveZSpan
            )
            
            //Legacy comment translated to English.
            const nodeCount = this.map3dNodes.length
            let distanceMultiplier = 1.2  //Legacy comment translated to English.
            
            //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            this.createConnectionLines()
            
            if (this.map3dControls) {
                this.map3dControls.target.set(treeCenter.x, treeCenter.y, treeCenter.z)
                
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                this.map3dCamera.position.set(
                    treeCenter.x + cameraDistance * 0.4,   //Legacy comment translated to English.
                    treeCenter.y + cameraDistance * 0.5,   //Legacy comment translated to English.
                    treeCenter.z + cameraDistance * 0.8    //Legacy comment translated to English.
                )
                this.map3dControls.update()
                
                console.log('📷 相机位置已设置:', {
                    target: treeCenter,
                    position: this.map3dCamera.position
                })
            }
            
            //Legacy comment translated to English.
            this.startNodeAnimations()
            
            //Legacy comment translated to English.
            let isAnimating = false //Legacy comment translated to English.
            
            const onMouseClick = (event) => {
                //Legacy comment translated to English.
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
                    
                    //Legacy comment translated to English.
                    if (nodeData && nodeData.node.children && Object.keys(nodeData.node.children).length > 0) {
                        //Legacy comment translated to English.
                        const wasExpanded = nodeData.node.expanded !== false
                        nodeData.node.expanded = !wasExpanded
                        
                        //Legacy comment translated to English.
                        isAnimating = true
                        
                        //Legacy comment translated to English.
                        if (nodeData.node.expanded) {
                            //Legacy comment translated to English.
                            this.animateNodesPopOut(nodeData, () => {
                                isAnimating = false
                            })
                        } else {
                            //Legacy comment translated to English.
                            this.animateNodesSuckIn(nodeData, () => {
                                isAnimating = false
                            })
                        }
                    }
                }
            }
            
            this.map3dRenderer.domElement.addEventListener('click', onMouseClick)
            this.map3dClickHandler = onMouseClick
            
            //Legacy comment translated to English.
            const onMouseDoubleClick = (event) => {
                const rect = this.map3dRenderer.domElement.getBoundingClientRect()
                mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1
                mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1
                
                raycaster.setFromCamera(mouse, this.map3dCamera)
                
                //Legacy comment translated to English.
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
                    
                    //Legacy comment translated to English.
                    if (nodeData && nodeData.node.type === 'aiming') {
                        //Legacy comment translated to English.
                        if (nodeData.node.prompts && nodeData.node.prompts.length > 0) {
                            const promptId = nodeData.node.prompts[0].id
                            
                            console.log('🎯 双击Aiming节点，选中靶道:', {
                                nodeKey: nodeData.key,
                                promptId: promptId,
                                fullPath: nodeData.node.fullPath
                            })
                            
                            //Legacy comment translated to English.
                            this.mapDialogVisible = false
                            
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            // 1. check pageChange status
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            
                            //Legacy comment translated to English.
                            const oldPromptId = this.promptid
                            
                            //Legacy comment translated to English.
                            this.promptid = promptId
                            
                            //Legacy comment translated to English.
                            this.promptChangeHandel(promptId, 'promptid', oldPromptId)
                        }
                    }
                }
            }
            
            this.map3dRenderer.domElement.addEventListener('dblclick', onMouseDoubleClick)
            this.map3dDoubleClickHandler = onMouseDoubleClick
            
            //Legacy comment translated to English.
            let hoveredNode = null  //Legacy comment translated to English.
            let currentHighlightMesh = null  //Legacy comment translated to English.
            
            //Legacy comment translated to English.
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
                
                //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
                    const nodeData = this.map3dNodes.find(n => 
                        n.mesh === intersectedObject || n.sprite === intersectedObject
                    )
                    
                    if (nodeData) {
                        //Legacy comment translated to English.
                        if (hoveredNode !== nodeData) {
                            //Legacy comment translated to English.
                            if (currentHighlightMesh) {
                                this.map3dScene.remove(currentHighlightMesh)
                                if (currentHighlightMesh.geometry) currentHighlightMesh.geometry.dispose()
                                if (currentHighlightMesh.material) currentHighlightMesh.material.dispose()
                                currentHighlightMesh = null
                            }
                            
                            hoveredNode = nodeData
                            
                            //Legacy comment translated to English.
                            if (nodeData.mesh && nodeData.mesh.visible) {
                                let highlightGeometry, highlightSize
                                
                                //Legacy comment translated to English.
                                if (nodeData.node.type === 'range') {
                                    //Legacy comment translated to English.
                                    highlightSize = 5.3  //Legacy comment translated to English.
                                    highlightGeometry = new THREE.BoxGeometry(highlightSize, highlightSize, highlightSize)
                                } else {
                                    //Legacy comment translated to English.
                                    const originalScale = nodeData.mesh.scale.x
                                    const originalGeometry = nodeData.mesh.geometry
                                    if (originalGeometry && originalGeometry.parameters && originalGeometry.parameters.radius) {
                                        highlightSize = originalGeometry.parameters.radius * 1.15 * originalScale  //Legacy comment translated to English.
                                    } else {
                                        highlightSize = 2.5 * 1.15 * originalScale  //Legacy comment translated to English.
                                    }
                                    highlightGeometry = new THREE.SphereGeometry(highlightSize, 32, 32)
                                }
                                
                                //Legacy comment translated to English.
                                const highlightMaterial = new THREE.MeshBasicMaterial({
                                    color: 0xffffff,  //Legacy comment translated to English.
                                    transparent: true,
                                    opacity: 0.3,
                                    side: THREE.BackSide,  //Legacy comment translated to English.
                                    depthWrite: false
                                })
                                
                                currentHighlightMesh = new THREE.Mesh(highlightGeometry, highlightMaterial)
                                currentHighlightMesh.position.copy(nodeData.mesh.position)
                                currentHighlightMesh.scale.copy(nodeData.mesh.scale)
                                this.map3dScene.add(currentHighlightMesh)
                            }
                            
                            //Legacy comment translated to English.
                            let tooltipContent = ''
                            const node = nodeData.node
                            
                            if (node.type === 'range') {
                                // rangeinformation
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
                                //Legacy comment translated to English.
                                const aimingNumber = nodeData.key.match(/-A(\d+)$/)?.[1] || nodeData.key
                                tooltipContent = `
                                    <div style="font-weight: bold; margin-bottom: 8px; color: #a8e6cf;">🎲 瞄准点</div>
                                    <div>编号: A${aimingNumber}</div>
                                `
                                
                                //Legacy comment translated to English.
                                if (node.prompts && node.prompts.length > 0) {
                                    const promptId = node.prompts[0].id
                                    //Legacy comment translated to English.
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
                                        //Legacy comment translated to English.
                                        let finalScore = null
                                        
                                        //Legacy comment translated to English.
                                        if (fullPromptData.finalScore !== undefined && 
                                            fullPromptData.finalScore !== null && 
                                            fullPromptData.finalScore !== -1 && 
                                            fullPromptData.finalScore !== '-1') {
                                            finalScore = fullPromptData.finalScore
                                        }
                                        //Legacy comment translated to English.
                                        else if (fullPromptData.evalMaxScore !== undefined && 
                                                 fullPromptData.evalMaxScore !== null && 
                                                 fullPromptData.evalMaxScore !== -1 && 
                                                 fullPromptData.evalMaxScore !== '-1') {
                                            finalScore = fullPromptData.evalMaxScore
                                        }
                                        //Legacy comment translated to English.
                                        else if (fullPromptData.evalAvgScore !== undefined && 
                                                 fullPromptData.evalAvgScore !== null && 
                                                 fullPromptData.evalAvgScore !== -1 && 
                                                 fullPromptData.evalAvgScore !== '-1') {
                                            finalScore = fullPromptData.evalAvgScore
                                        }
                                        
                                        console.log('✅ Tooltip使用评分:', finalScore)
                                        
                                        //Legacy comment translated to English.
                                        if (finalScore !== null) {
                                            
                                            tooltipContent += `<div style="margin-top: 8px; border-top: 1px solid rgba(255,255,255,0.2); padding-top: 8px;">`
                                            
                                            //Legacy comment translated to English.
                                            if (finalScore !== null) {
                                                //Legacy comment translated to English.
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
                                                    
                                                    //Legacy comment translated to English.
                                                    if (isFirst) {
                                                        scoreColor = '#b24df5'  //Legacy comment translated to English.
                                                        scoreSize = '26px'
                                                        scoreEmoji = '🥇'
                                                        rankBadge = '<span style="background: linear-gradient(135deg, #b24df5 0%, #da6aff 50%, #b24df5 100%); color: #fff; padding: 3px 8px; border-radius: 4px; font-size: 11px; margin-left: 6px; font-weight: bold; box-shadow: 0 2px 8px rgba(178,77,245,0.5);">👑 第一名</span>'
                                                    }
                                                    //Legacy comment translated to English.
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
                                                
                                                //Legacy comment translated to English.
                                                if (rankText) {
                                                    tooltipContent += `<div style="font-size: 12px; color: #aaa; margin-bottom: 8px;">${rankText}</div>`
                                                }
                                            }
                                            
                                            //Legacy comment translated to English.
                                            //Legacy comment translated to English.
                                            if (fullPromptData.evalAvgScore !== undefined && 
                                                fullPromptData.evalAvgScore !== null && 
                                                fullPromptData.evalAvgScore !== -1 && 
                                                fullPromptData.evalAvgScore !== '-1') {
                                                tooltipContent += `<div style="font-size: 12px; color: #ccc; margin-top: 4px;">${ncfST('📊 平均分:')} <span style="color: #95e1d3;">${fullPromptData.evalAvgScore.toFixed(1)}</span></div>`
                                            }
                                            
                                            //Legacy comment translated to English.
                                            if (fullPromptData.evalMaxScore !== undefined && 
                                                fullPromptData.evalMaxScore !== null && 
                                                fullPromptData.evalMaxScore !== -1 && 
                                                fullPromptData.evalMaxScore !== '-1' &&
                                                finalScore !== fullPromptData.evalMaxScore) {
                                                tooltipContent += `<div style="font-size: 12px; color: #ccc; margin-top: 4px;">${ncfST('⭐ 最高分:')} <span style="color: #ffd93d;">${fullPromptData.evalMaxScore.toFixed(1)}</span></div>`
                                            }
                                            
                                            tooltipContent += `</div>`
                                        }
                                    }
                                }
                                
                                //Legacy comment translated to English.
                                tooltipContent += `<div style="margin-top: 8px; padding-top: 8px; border-top: 1px solid rgba(255,255,255,0.1); font-size: 11px; color: #999;">💡 双击可快速选中此靶道</div>`
                            }
                            
                            tooltip.innerHTML = tooltipContent
                        }
                        
                        //Legacy comment translated to English.
                        tooltip.style.display = 'block'
                        tooltip.style.left = (event.clientX + 15) + 'px'
                        tooltip.style.top = (event.clientY + 15) + 'px'
                        
                        //Legacy comment translated to English.
                        this.map3dRenderer.domElement.style.cursor = 'pointer'
                    }
                } else {
                    //Legacy comment translated to English.
                    if (hoveredNode) {
                        hoveredNode = null
                        tooltip.style.display = 'none'
                        this.map3dRenderer.domElement.style.cursor = 'default'
                        
                        //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        createConnectionLines() {
            if (!this.map3dNodes || !this.map3dScene) return
            
            this.map3dNodes.forEach(nodeData => {
                //Legacy comment translated to English.
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
                
                //Legacy comment translated to English.
                if (nodeData.node.parentPath) {
                    const parentNodeData = this.map3dNodes.find(n => {
                        if (!n.node.fullPath) return false
                        return n.node.fullPath === nodeData.node.parentPath
                    })
                    
                    if (parentNodeData && parentNodeData.mesh) {
                        //Legacy comment translated to English.
                        nodeData.parentNodeData = parentNodeData
                        
                        const hasCurrent = nodeData.hasCurrent
                        
                        //Legacy comment translated to English.
                        let lineColor = 0xffffff  //Legacy comment translated to English.
                        
                        if (hasCurrent) {
                            lineColor = 0xffd93d  //Legacy comment translated to English.
                        }
                        
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        const lineGeometry = new THREE.BufferGeometry()
                        const positions = new Float32Array(6) //Legacy comment translated to English.
                        lineGeometry.setAttribute('position', new THREE.BufferAttribute(positions, 3))
                        
                        //Legacy comment translated to English.
                        const lineMaterial = new THREE.LineBasicMaterial({
                            color: lineColor,
                            linewidth: 2,
                            transparent: true,
                            opacity: 0 //Legacy comment translated to English.
                        })
                        
                        const line = new THREE.Line(lineGeometry, lineMaterial)
                        this.map3dScene.add(line)
                        nodeData.line = line
                        nodeData.lineColor = lineColor
                        
                        //Legacy comment translated to English.
                        const dotGeometry = new THREE.SphereGeometry(0.3, 8, 8)
                        const dotMaterial = new THREE.MeshStandardMaterial({ 
                            color: 0xffffff,  //Legacy comment translated to English.
                            emissive: hasCurrent ? 0xffd93d : 0xffffff,  //Legacy comment translated to English.
                            emissiveIntensity: hasCurrent ? 0.5 : 0.2,
                            metalness: 0.3,
                            roughness: 0.5,
                            transparent: true,
                            opacity: 0 //Legacy comment translated to English.
                        })
                        const dot = new THREE.Mesh(dotGeometry, dotMaterial)
                        this.map3dScene.add(dot)
                        nodeData.dot = dot
                        
                        //Legacy comment translated to English.
                        this.updateConnectionLine(nodeData)
                    } else {
                        //Legacy comment translated to English.
                        console.warn('找不到父节点:', {
                            fullPath: nodeData.node.fullPath,
                            parentPath: nodeData.node.parentPath,
                            nodeType: nodeData.node.type,
                            key: nodeData.key
                        })
                        
                        //Legacy comment translated to English.
                        if (nodeData.node.type === 'aiming') {
                            console.log('所有节点的 fullPath:', this.map3dNodes.map(n => n.node.fullPath))
                        }
                    }
                }
            })
        },
        
        //Legacy comment translated to English.
        updateConnectionLine(nodeData) {
            if (!nodeData.line || !nodeData.parentNodeData) return
            
            const parentPosition = nodeData.parentNodeData.mesh.position
            const currentPosition = nodeData.mesh.position
            
            //Legacy comment translated to English.
            const positions = nodeData.line.geometry.attributes.position.array
            positions[0] = parentPosition.x
            positions[1] = parentPosition.y
            positions[2] = parentPosition.z
            positions[3] = currentPosition.x
            positions[4] = currentPosition.y
            positions[5] = currentPosition.z
            nodeData.line.geometry.attributes.position.needsUpdate = true
            
            //Legacy comment translated to English.
            if (nodeData.dot) {
                nodeData.dot.position.set(currentPosition.x, currentPosition.y, currentPosition.z)
            }
        },
        
        //Legacy comment translated to English.
        updateAllConnectionLines() {
            if (!this.map3dNodes) return
            
            this.map3dNodes.forEach(nodeData => {
                if (nodeData.line && nodeData.parentNodeData) {
                    this.updateConnectionLine(nodeData)
                }
            })
        },
        
        //Legacy comment translated to English.
        startNodeAnimations() {
            if (!this.map3dNodes || this.map3dNodes.length === 0) return
            
            const animationDuration = 500 //Legacy comment translated to English.
            const startTime = Date.now()
            
            const animate = () => {
                const elapsed = Date.now() - startTime
                const progress = Math.min(elapsed / animationDuration, 1)
                
                //Legacy comment translated to English.
                const easeProgress = 1 - Math.pow(1 - progress, 3)
                
                this.map3dNodes.forEach((nodeData, index) => {
                    //Legacy comment translated to English.
                    const delay = index * 20 //Legacy comment translated to English.
                    const nodeProgress = Math.max(0, Math.min(1, (elapsed - delay) / animationDuration))
                    const nodeEaseProgress = 1 - Math.pow(1 - nodeProgress, 3)
                    
                    //Legacy comment translated to English.
                    const scale = 0.1 + nodeEaseProgress * 0.9
                    nodeData.mesh.scale.set(scale, scale, scale)
                    
                    if (nodeData.glowMesh) {
                        nodeData.glowMesh.scale.set(scale, scale, scale)
                    }
                    
                    //Legacy comment translated to English.
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
                
                //Legacy comment translated to English.
                this.updateAllConnectionLines()
                
                if (progress < 1) {
                    requestAnimationFrame(animate)
                } else {
                    //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        createGradientBackground() {
            const canvas = document.createElement('canvas')
            canvas.width = 256
            canvas.height = 256
            const context = canvas.getContext('2d')
            
            //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        clearMap3DScene() {
            if (!this.map3dScene) return
            
            //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        animateMap3D() {
            if (!this.map3dRenderer || !this.map3dScene || !this.map3dCamera) return
            
            this.map3dAnimationId = requestAnimationFrame(() => this.animateMap3D())
            
            //Legacy comment translated to English.
            let needsRender = false
            
            //Legacy comment translated to English.
            if (this.map3dNeedsAnimationUpdate) {
                needsRender = true
                this.map3dNeedsAnimationUpdate = false
            }
            
            //Legacy comment translated to English.
            if (this.map3dControls) {
                //Legacy comment translated to English.
                const controlsChanged = this.map3dControls.update()
                if (controlsChanged) {
                    needsRender = true
                }
            }
            
            //Legacy comment translated to English.
            if (!this.map3dLastAnimationTime) {
                this.map3dLastAnimationTime = Date.now()
                this.map3dCurrentNodes = [] //Legacy comment translated to English.
            }
            
            const now = Date.now()
            //Legacy comment translated to English.
            if (now - this.map3dLastAnimationTime > 200) {
                this.map3dLastAnimationTime = now
                
                //Legacy comment translated to English.
                if (!this.map3dCurrentNodes || this.map3dCurrentNodes.length === 0) {
                    if (this.map3dNodes && this.map3dNodes.length > 0) {
                        this.map3dCurrentNodes = this.map3dNodes.filter(({ node }) => 
                            node.prompts && node.prompts.some(p => p.isCurrent)
                        )
                    }
                }
                
                //Legacy comment translated to English.
                this.updateAllConnectionLines()
                
                //Legacy comment translated to English.
                if (this.map3dCurrentNodes && this.map3dCurrentNodes.length > 0) {
                    const time = now * 0.001
                    this.map3dCurrentNodes.forEach(({ mesh }) => {
                        const scale = 1 + Math.sin(time * 2) * 0.05
                        mesh.scale.set(scale, scale, scale)
                    })
                    needsRender = true
                }
            }
            
            //Legacy comment translated to English.
            if (needsRender) {
                this.map3dRenderer.render(this.map3dScene, this.map3dCamera)
            }
        },
        
        //Legacy comment translated to English.
        animateNodesPopOut(parentNodeData, onComplete) {
            if (!this.map3dScene || !this.map3dNodes) return
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            const childNodes = []
            
            const collectChildren = (parentNode) => {
                if (!parentNode.children || Object.keys(parentNode.children).length === 0) return
                
                Object.keys(parentNode.children).forEach(childKey => {
                    const childNode = parentNode.children[childKey]
                    const childNodeData = this.map3dNodes.find(n => n.node === childNode && n.key === childKey)
                    if (childNodeData) {
                        childNodes.push(childNodeData)
                        
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            const animationDuration = 250  //Legacy comment translated to English.
            const startTime = Date.now()
            
            //Legacy comment translated to English.
            childNodes.forEach(childData => {
                //Legacy comment translated to English.
                if (childData.node.type === 'aiming') {
                    //Legacy comment translated to English.
                    const parentData = this.map3dNodes.find(n => 
                        n.node.fullPath === childData.node.parentPath
                    )
                    
                    if (parentData && parentData.node.children) {
                        //Legacy comment translated to English.
                        const aimingKeys = Object.keys(parentData.node.children).filter(k => 
                            parentData.node.children[k].type === 'aiming'
                        )
                        const aimingIndex = aimingKeys.indexOf(childData.key)
                        
                        if (aimingIndex !== -1) {
                            const aimingSpacing = 16 //Legacy comment translated to English.
                            
                            //Legacy comment translated to English.
                            // A1: parentZ + aimingSpacing, A2: parentZ + 2*aimingSpacing, ...
                            const parentZ = parentData.mesh.position.z
                            const correctZ = parentZ + (aimingIndex + 1) * aimingSpacing
                            
                            //Legacy comment translated to English.
                            //Legacy comment translated to English.
                            const correctX = parentData.mesh.position.x
                            //Legacy comment translated to English.
                            const correctY = parentData.mesh.position.y
                            
                            //Legacy comment translated to English.
                            childData._targetPosition = {
                                x: correctX,  //Legacy comment translated to English.
                                y: correctY,  //Legacy comment translated to English.
                                z: correctZ   //Legacy comment translated to English.
                            }
                        } else {
                            childData._targetPosition = { ...childData.position }
                        }
                    } else {
                        childData._targetPosition = { ...childData.position }
                    }
                } else {
                    //Legacy comment translated to English.
                    childData._targetPosition = { ...childData.position }
                }
                
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                if (childData.parentNodeData && childData.parentNodeData.mesh) {
                    //Legacy comment translated to English.
                    childData._parentPosition = {
                        x: childData.parentNodeData.mesh.position.x,
                        y: childData.parentNodeData.mesh.position.y,
                        z: childData.parentNodeData.mesh.position.z
                    }
                } else {
                    //Legacy comment translated to English.
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
                        //Legacy comment translated to English.
                        childData._parentPosition = { ...parentNodeData.position }
                    }
                }
            })
            
            const animate = () => {
                const elapsed = Date.now() - startTime
                const progress = Math.min(elapsed / animationDuration, 1)
                
                //Legacy comment translated to English.
                const c1 = 1.70158
                const c3 = c1 + 1
                const easeProgress = 1 + c3 * Math.pow(progress - 1, 3) + c1 * Math.pow(progress - 1, 2)
                
                childNodes.forEach((childData, index) => {
                    //Legacy comment translated to English.
                    const delay = index * 80  //Legacy comment translated to English.
                    const nodeProgress = Math.max(0, Math.min(1, (elapsed - delay) / animationDuration))
                    
                    //Legacy comment translated to English.
                    if (nodeProgress === 0) {
                        childData.mesh.visible = false
                        if (childData.glowMesh) childData.glowMesh.visible = false
                        if (childData.sprite) childData.sprite.visible = false
                        if (childData.line) childData.line.visible = false
                        if (childData.dot) childData.dot.visible = false
                        return
                    }
                    
                    const nodeEase = 1 + c3 * Math.pow(nodeProgress - 1, 3) + c1 * Math.pow(nodeProgress - 1, 2)
                    
                    //Legacy comment translated to English.
                    const x = childData._parentPosition.x + (childData._targetPosition.x - childData._parentPosition.x) * nodeEase
                    const y = childData._parentPosition.y + (childData._targetPosition.y - childData._parentPosition.y) * nodeEase
                    const z = childData._parentPosition.z + (childData._targetPosition.z - childData._parentPosition.z) * nodeEase
                    
                    //Legacy comment translated to English.
                    const scale = 0.1 + nodeEase * 0.9
                    
                    //Legacy comment translated to English.
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
                
                //Legacy comment translated to English.
                this.updateAllConnectionLines()
                
                if (progress < 1) {
                    requestAnimationFrame(animate)
                } else {
                    //Legacy comment translated to English.
                    childNodes.forEach(childData => {
                        //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        animateNodesSuckIn(parentNodeData, onComplete) {
            if (!this.map3dScene || !this.map3dNodes) return
            
            //Legacy comment translated to English.
            const childNodes = []
            const collectChildren = (node) => {
                if (!node.children || Object.keys(node.children).length === 0) return
                
                Object.keys(node.children).forEach(childKey => {
                    const childNode = node.children[childKey]
                    const childNodeData = this.map3dNodes.find(n => n.node === childNode && n.key === childKey)
                    if (childNodeData) {
                        childNodes.push(childNodeData)
                        //Legacy comment translated to English.
                        collectChildren(childNode)
                    }
                })
            }
            
            collectChildren(parentNodeData.node)
            
            if (childNodes.length === 0) {
                if (onComplete) onComplete()
                return
            }
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            const animationDuration = 200  //Legacy comment translated to English.
            const startTime = Date.now()
            
            //Legacy comment translated to English.
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
                
                //Legacy comment translated to English.
                const easeProgress = progress * progress * progress
                
                childNodes.forEach((childData, index) => {
                    //Legacy comment translated to English.
                    const delay = (childNodes.length - index) * 60  //Legacy comment translated to English.
                    const nodeProgress = Math.max(0, Math.min(1, (elapsed - delay) / animationDuration))
                    const nodeEase = nodeProgress * nodeProgress * nodeProgress
                    
                    //Legacy comment translated to English.
                    const x = childData._startPosition.x + (parentNodeData.position.x - childData._startPosition.x) * nodeEase
                    const y = childData._startPosition.y + (parentNodeData.position.y - childData._startPosition.y) * nodeEase
                    const z = childData._startPosition.z + (parentNodeData.position.z - childData._startPosition.z) * nodeEase
                    
                    //Legacy comment translated to English.
                    const scale = 1 - nodeEase * 0.9
                    const opacity = 1 - nodeEase
                    
                    //Legacy comment translated to English.
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
                
                //Legacy comment translated to English.
                this.updateAllConnectionLines()
                
                if (progress < 1) {
                    requestAnimationFrame(animate)
                } else {
                    //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        handleMap3DResize() {
            const container = document.getElementById('map3dContainer')
            if (!container || !this.map3dCamera || !this.map3dRenderer) return
            
            const width = container.clientWidth
            const height = container.clientHeight
            
            this.map3dCamera.aspect = width / height
            this.map3dCamera.updateProjectionMatrix()
            this.map3dRenderer.setSize(width, height)
        },
        
        //Legacy comment translated to English.
        destroyMap3D() {
            window.removeEventListener('resize', this.handleMap3DResize)
            
            //Legacy comment translated to English.
            if (this._map3dKeydownHandler) {
                window.removeEventListener('keydown', this._map3dKeydownHandler, true)
                this._map3dKeydownHandler = null
            }
            
            //Legacy comment translated to English.
            if (this.map3dRenderer && this.map3dRenderer.domElement && this.map3dClickHandler) {
                this.map3dRenderer.domElement.removeEventListener('click', this.map3dClickHandler)
                this.map3dClickHandler = null
            }
            
            //Legacy comment translated to English.
            if (this.map3dRenderer && this.map3dRenderer.domElement && this.map3dDoubleClickHandler) {
                this.map3dRenderer.domElement.removeEventListener('dblclick', this.map3dDoubleClickHandler)
                this.map3dDoubleClickHandler = null
            }
            
            //Legacy comment translated to English.
            if (this.map3dRenderer && this.map3dRenderer.domElement && this.map3dMouseMoveHandler) {
                this.map3dRenderer.domElement.removeEventListener('mousemove', this.map3dMouseMoveHandler)
                this.map3dMouseMoveHandler = null
            }
            
            //Legacy comment translated to English.
            const tooltip = document.getElementById('map3d-tooltip')
            if (tooltip) {
                tooltip.remove()
            }
            
            //Legacy comment translated to English.
            if (this.map3dAnimationId) {
                cancelAnimationFrame(this.map3dAnimationId)
                this.map3dAnimationId = null
            }
            
            //Legacy comment translated to English.
            if (this.map3dControls) {
                this.map3dControls.dispose()
                this.map3dControls = null
            }
            
            //Legacy comment translated to English.
            this.clearMap3DScene()
            
            //Legacy comment translated to English.
            if (this.map3dNodeMap) {
                this.map3dNodeMap.clear()
                this.map3dNodeMap = null
            }
            
            //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        async executeTargetShoot() {
            await this.executeTargetShootWithChatMessage(null)
        },
        
        //Legacy comment translated to English.
        async executeTargetShootWithChatMessage(userMessage) {
            if (!this.ensurePromptContentForCurrentModel()) {
                return
            }
            this.tacticalFormSubmitLoading = true
            const selectedTactics = this.tacticalForm.tactics
            if (this.tacticalFormVisible) {
                this.tacticalFormVisible = false
            }
            this.ensureOutputGeneratingPlaceholder()
            const streamId = this.createPromptStreamId()
            const streamReady = await this.openPromptStream(streamId, 'shoot')
            
            //Legacy comment translated to English.
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

            const executionOptions = this.buildExecutionOptionsPayload()
            if (executionOptions) {
                _postData.executionOptions = executionOptions
            }
            
            //Legacy comment translated to English.
            if (userMessage) {
                _postData.userMessage = userMessage
            }
            
            //Legacy comment translated to English.
            if (this.continueChatMode && this.continueChatHistory.length > 0) {
                _postData.chatHistory = this.continueChatHistory.map(msg => ({
                    role: msg.roleType === 1 ? 'user' : 'assistant',
                    content: msg.content
                }))
            }
            
            //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                if (selectedTactics === '创建顶级战术') {
                    _postData.isTopTactic = true
                }
                if (selectedTactics === '创建平行战术') {
                    _postData.isNewTactic = true
                }
                if (selectedTactics === '创建子战术') {
                    _postData.isNewSubTactic = true
                }
                if (selectedTactics === '重新瞄准') {
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
            if (streamReady) {
                _postData.streamId = streamId
            }

            try {
                let res = await servicePR.post('/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Add', _postData)
                
                if (res.data.success) {
                    this.pageChange = false
                    let {
                        promptResultList = [],
                        fullVersion = '',
                        id,
                        evalAvgScore = -1,
                        evalMaxScore = -1,
                        promptItem = {}
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
                    this.outputList = promptResultList.map(item => this.normalizeOutputResultItem(item, id, fullVersion, promptItem))
                    const latestResult = this.outputList.length > 0 ? this.outputList[this.outputList.length - 1] : null
                    if (latestResult && latestResult.id !== undefined && latestResult.id !== null && latestResult.id !== '') {
                        this.scrollToBtm(latestResult.id)
                    } else {
                        this.forceScrollResultBoxToBottom()
                    }
                    
                    //Legacy comment translated to English.
                    if (promptResultList.length > 0) {
                        const firstResult = promptResultList[0]
                        if (firstResult.mode !== 2 && firstResult.mode !== 'Chat') {
                            //Legacy comment translated to English.
                            this._lastUserMessage = null
                        }
                    }
                    
                    //Legacy comment translated to English.
                    this.continueChatMode = false
                    this.continueChatPromptResultId = null
                    this.continueChatHistory = []
                    this.continueChatPendingUserMessageId = null
                    this.continueChatUsageSummary = {
                        promptCostToken: 0,
                        resultCostToken: 0,
                        totalCostToken: 0
                    }
                    
                    this.getFieldList().then(() => {
                        this.getPromptOptData(id)
                        this.getScoringTrendData()
                    })

                    if (this.sendBtnText !== '保存草稿' && this.numsOfResults > 1) {
                        await this.dealRapicFireHandel(this.numsOfResults - 1, id)
                    }
                } else {
                    this.clearOutputGeneratingPlaceholder()
                    this.closePromptStream()
                    this.$message({
                        message: res.data.errorMessage || res.data.data || 'Error',
                        type: 'error',
                        duration: 5 * 1000
                    })
                }
            } catch (error) {
                this.clearOutputGeneratingPlaceholder()
                this.closePromptStream()
                this.$message({
                    message: error?.message || '请求失败',
                    type: 'error',
                    duration: 5 * 1000
                })
            } finally {
                this.tacticalFormSubmitLoading = false
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
        //Legacy comment translated to English.
        async getVersionRecordData() {
            //find rangeName by id

            // const rangeName
            let _find = this.promptFieldOpt.find(item => item.value === this.promptField)
            const name = _find ? _find.rangeName : ''

            let res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.GetTacticTree?rangeName=${name}`)
            if (res.data.success) {
                //console.log('getversionrecorddata', res.data.data.rootNodeList)
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
        //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        seeVersionRecord() {
            this.versionDrawer = true
            // reload data
            this.getVersionRecordData()
        },
        //Legacy comment translated to English.
        versionTreeFilterNode(value, data) {
            if (!value) return true;
            return data.label.indexOf(value) > -1;
        },
        //Legacy comment translated to English.
        versionDrawerClose() {
            this.versionSearchVal = ''
        },
        //Legacy comment translated to English.
        versionRecordIsPublic(itemData) {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            this.$message.warning('敬请期待')
        },
        // versionrecord edit
        versionRecordEdit(itemData) {
            //console.log('versionrecord edit:', itemData)
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            this.promptid = itemData.id
            //Legacy comment translated to English.
            this.getPromptetail(itemData.id, true, true)
            //Legacy comment translated to English.
            //this.getOutputList()
            //Legacy comment translated to English.
            this.getScoringTrendData()
            //Legacy comment translated to English.
            this.versionDrawer = false
        },
        // versionrecord generatecode
        versionRecordGenerateCode(itemData) {
            //console.log('versionrecord generatecode:', itemData)
            //Legacy comment translated to English.
            this.$message.warning('敬请期待')
            //Legacy comment translated to English.
        },
        // versionrecord delete
        versionRecordDelete(itemData) {
            //console.log('versionrecord delete:', itemData)
            this.$message.warning('敬请期待')
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //    type: 'warning'
            //}).then(() => {
            //Legacy comment translated to English.
            //    this.btnDeleteHandle(itemData.id,true)

            //}).catch(() => {
            //    this.$message({
            //        type: 'info',
            //Legacy comment translated to English.
            //    });
            //});
        },
        //Legacy comment translated to English.
        versionRecordViewNotes(itemData) {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
        },


        //Legacy comment translated to English.
        async getOutputList(promptId) {
            let res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GetByItemId?promptItemId=${promptId}`)
            //console.log('getOutputList:', res)
            if (res.data.success) {
                let { promptResults = [], promptItem = {} } = res.data.data || {}
                //Legacy comment translated to English.
                this.outputAverageDeci = promptItem.evalAvgScore > -1 ? promptItem.evalAvgScore : -1; //Legacy comment translated to English.
                this.outputMaxDeci = promptItem.evalMaxScore > -1 ? promptItem.evalMaxScore : -1; //Legacy comment translated to English.

                //Legacy comment translated to English.
                this.outputList = promptResults.map(item => this.normalizeOutputResultItem(
                    item,
                    this.promptDetail?.id,
                    this.promptDetail?.fullVersion,
                    promptItem
                ))
                if (this.continueChatMode && this.continueChatPromptResultId) {
                    this.syncContinueChatUsageFromResult(this.continueChatPromptResultId)
                }
                
                // add a button to copy code blocks
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
        //Legacy comment translated to English.
        async saveManualScore(item, index) {
            //console.log('manualScorVal', this.promptSelectVal, this.manualScorVal)
            if (item.scoreType === '1') {
                let _list = item.alResultList.map(item => item.value)
                let res = await servicePR.post(`/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.RobotScore?isRefresh=true&promptResultId=${item.id}`, _list)
                if (res.data.success) {
                    //console.log('testHandel res data:', res.data.data)
                    //Legacy comment translated to English.
                    this.getPromptOptData()
                    //Legacy comment translated to English.
                    await this.getOutputList(item.promptId)
                    //Legacy comment translated to English.
                    this.$set(this.robotScoreLoadingMap, item.id, false)
                    //Legacy comment translated to English.
                    this.getScoringTrendData()
                    
                    //Legacy comment translated to English.
                    await this.checkScoreAndSuggestOptimization(res.data.data || item, 'AI评分');
                } else {
                    //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
                    this.getPromptOptData()
                    //Legacy comment translated to English.
                    this.getOutputList(item.promptId)
                    //Legacy comment translated to English.
                    this.getScoringTrendData()
                    
                    //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        outputSelectSwitch(index) {
            if (this.outputActive !== '' && this.outputActive !== index) {
                this.outputList[this.outputActive].isScoreView = false
            }
            this.outputActive = index
        },
        //Legacy comment translated to English.
        showRatingView(index, scoreType) {
            //Legacy comment translated to English.
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
                        //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        alBtnScoring(index) {
            this.outputList[index].scoreType = '1'
        },
        //Legacy comment translated to English.
        addAlScoring(index) {
            let _len = this.outputList[index].alResultList.length
            this.outputList[index].alResultList.push({
                id: _len + 1,
                label: `预期结果`,
                value: ''
            })
        },
        //Legacy comment translated to English.
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
            const postData = { promptItemId, numsOfResults }
            this.ensureOutputGeneratingPlaceholder()
            const streamId = this.createPromptStreamId()
            const streamReady = await this.openPromptStream(streamId, 'shoot')
            if (streamReady) {
                postData.streamId = streamId
            }
            //Legacy comment translated to English.
            if (userMessage) {
                postData.userMessage = userMessage
            }
            const executionOptions = this.buildExecutionOptionsPayload()
            if (executionOptions) {
                postData.executionOptions = executionOptions
            }
            return await servicePR.post('/api/Senparc.Xncf.PromptRange/PromptResultAppService/Xncf.PromptRange_PromptResultAppService.GenerateWithItemIdPost',
                postData).then(res => {
                    //console.log('testHandel res ', res.data)
                    if (!res.data.success) {
                        this.clearOutputGeneratingPlaceholder()
                        app.$message({
                            message: res.data.errorMessage || res.data.data || 'Error',
                            type: 'error',
                            duration: 5 * 1000
                        });
                        return
                    }
                    this.outputAverageDeci = res.data.data.promptItem.evalAvgScore > -1 ? res.data.data.promptItem.evalAvgScore : -1; //Legacy comment translated to English.
                    this.outputMaxDeci = res.data.data.promptItem.evalMaxScore > -1 ? res.data.data.promptItem.evalMaxScore : -1; //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    let latestResultId = null
                    const findExistingResultIndexById = (resultId) => {
                        if (resultId === undefined || resultId === null || resultId === '') {
                            return -1
                        }
                        return this.outputList.findIndex(outputItem =>
                            String(outputItem?.id) === String(resultId)
                        )
                    }
                    const findStreamingPlaceholderIndex = () => {
                        if (this.promptStreamingTempResultId !== undefined && this.promptStreamingTempResultId !== null && this.promptStreamingTempResultId !== '') {
                            const exactMatchIndex = this.outputList.findIndex(outputItem =>
                                String(outputItem.id) === String(this.promptStreamingTempResultId)
                            )
                            if (exactMatchIndex > -1) {
                                return exactMatchIndex
                            }
                        }

                        for (let i = this.outputList.length - 1; i >= 0; i--) {
                            const outputItem = this.outputList[i]
                            if (!outputItem) continue
                            if (outputItem._generating === true) {
                                return i
                            }
                            if (typeof outputItem.id === 'string' && outputItem.id.indexOf('streaming_') === 0) {
                                return i
                            }
                        }
                        return -1
                    }
                    res.data.data.promptResults.map(item => {
                        const normalized = this.normalizeOutputResultItem(
                            item,
                            promptItemId,
                            this.promptDetail?.fullVersion,
                            res.data.data.promptItem || {}
                        )

                        //Legacy comment translated to English.
                        const existingIndex = findExistingResultIndexById(normalized.id)
                        if (existingIndex > -1) {
                            this.$set(this.outputList, existingIndex, normalized)
                            latestResultId = normalized.id
                            return
                        }

                        let replacedStreamingPlaceholder = false
                        const placeholderIndex = findStreamingPlaceholderIndex()
                        if (placeholderIndex > -1) {
                            this.$set(this.outputList, placeholderIndex, normalized)
                            replacedStreamingPlaceholder = true
                        }
                        if (!replacedStreamingPlaceholder) {
                            this.outputList.push(normalized)
                        }
                        latestResultId = normalized.id
                    })
                    this.cleanupOutputListAfterResultUpsert()
                    if (!latestResultId) {
                        this.clearOutputGeneratingPlaceholder()
                        this.closePromptStream()
                        return
                    }
                    this.scrollToBtm(latestResultId)
                }).catch(() => {
                    this.clearOutputGeneratingPlaceholder()
                    this.closePromptStream()
                })
        },

        scrollToResultItemBottom(resultId, behavior = 'auto') {
            this.$nextTick(() => {
                const container = document.getElementById('resultBox')
                if (!container) {
                    return
                }

                if (resultId === undefined || resultId === null || resultId === '') {
                    container.scrollTop = container.scrollHeight
                    return
                }

                const locateTarget = () => {
                    const items = container.querySelectorAll('.contentBoxItem')
                    const targetIndex = this.outputList.findIndex(item => String(item.id) === String(resultId))
                    if (targetIndex > -1 && items[targetIndex]) {
                        return items[targetIndex]
                    }

                    for (let i = 0; i < items.length; i++) {
                        if (String(items[i].getAttribute('data-result-id')) === String(resultId)) {
                            return items[i]
                        }
                    }
                    return null
                }

                let target = locateTarget()
                if (target) {
                    target.scrollIntoView({ behavior, block: 'end' })
                    const latestResult = Array.isArray(this.outputList) && this.outputList.length > 0
                        ? this.outputList[this.outputList.length - 1]
                        : null
                    if (latestResult && String(latestResult.id) === String(resultId)) {
                        container.scrollTop = container.scrollHeight
                    }
                    return
                }

                requestAnimationFrame(() => {
                    target = locateTarget()
                    if (target) {
                        target.scrollIntoView({ behavior, block: 'end' })
                        const latestResult = Array.isArray(this.outputList) && this.outputList.length > 0
                            ? this.outputList[this.outputList.length - 1]
                            : null
                        if (latestResult && String(latestResult.id) === String(resultId)) {
                            container.scrollTop = container.scrollHeight
                        }
                    } else {
                        container.scrollTop = container.scrollHeight
                    }
                })
            })
        },
        scrollToBtm(resultId = null) {
            this.scrollToResultItemBottom(resultId)
        },

        //Legacy comment translated to English.
        resetConfigurineParam(isPageChange) {
            //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        parameterInputHandle(val, item) {
            //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            if (isStr) {
                //Legacy comment translated to English.
            } else {
                // if (isSlider || isStr )
                //Legacy comment translated to English.
                let _val = val.replace(/[^\d]/g, '')
                //floor
                _val = Math.round(_val / sliderStep) * sliderStep
                //console.log('parameterInputHandle _val:', _val)
                //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        resetInputPrompt() {
            //Legacy comment translated to English.
            this.content = ''// prompt inputcontent
            if (this.isSpeechToTextModelSelected) {
                this.clearSpeechInput()
            }
            //Legacy comment translated to English.
        },
        deleteModel(item) {
            //deletemodel confirm
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
                            //Legacy comment translated to English.
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

        //  promptrequestparameters disable
        promptParamFormClose() {
            this.promptParamForm = {
                prefix: '',
                suffix: '',
                variableList: []
            }
            this.$refs.promptParamForm.resetFields();
            this.promptParamVisible = true;
        },
        //Legacy comment translated to English.
        addVariableBtn() {
            this.promptParamForm.variableList.push({
                name: '',
                value: ''
            })
        },
        toAIKernel() {
            window.location.assign('/Admin/AIKernel/Index?uid=796D12D8-580B-40F3-A6E8-A5D9D2EABB69')
        },
        //Legacy comment translated to English.
        deleteVariableBtn(index) {
            this.promptParamForm.variableList.splice(index, 1)
        },
        //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                this.$message({
                    message: '刷新完成！',
                    type: 'success'
                })
            })
        },
        //Legacy comment translated to English.
        async getModelOptData() {
            let res = await servicePR.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetListAsync', {})
            //console.log('getModelOptData:', res)
            if (res.data.success) {
                //console.log('getModelOptData:', res.data)
                let _optList = res.data.data || []
                this.modelOpt = _optList.map(item => {
                    const configModelTypeCode = this.parseConfigModelTypeValue(item.configModelType)
                    const configModelTypeName = this.getConfigModelTypeLabel(configModelTypeCode)

                    //Legacy comment translated to English.
                    let label = `${item.alias || '未命名模型'} [${configModelTypeName}]`
                    if (item.apiVersion && item.apiVersion.trim()) {
                        label += ` v${item.apiVersion}`;
                    }
                    if (item.deploymentName && item.deploymentName.trim()) {
                        label += `\n(${item.deploymentName})`;
                    }
                    
                    return {
                        ...item,
                        label: label,
                        displayName: item.alias,  //Legacy comment translated to English.
                        selectLabel: label,
                        configModelTypeCode: configModelTypeCode,
                        configModelTypeName: configModelTypeName,
                        deploymentDisplay: item.deploymentName, //Legacy comment translated to English.
                        apiVersion: item.apiVersion, //Legacy comment translated to English.
                        value: item.id,
                        disabled: false
                    }
                })
                //Legacy comment translated to English.
                this.$nextTick(() => {
                    this.onModelSelectionChanged()
                })
            } else {
                app.$message({
                    message: res.data.errorMessage || res.data.data || 'Error',
                    type: 'error',
                    duration: 5 * 1000
                })
            }
        },
        async getVectorOptData(forceRefresh = false) {
            if (this.vectorLoading) {
                return
            }
            if (!forceRefresh && Array.isArray(this.vectorOpt) && this.vectorOpt.length > 0) {
                return
            }

            this.vectorLoading = true
            try {
                const res = await servicePR.post('/api/Senparc.Xncf.AIKernel/AIVectorAppService/Xncf.AIKernel_AIVectorAppService.GetListAsync', {})
                if (!res.data.success) {
                    this.$message({
                        message: res.data.errorMessage || res.data.data || '向量库列表加载失败',
                        type: 'error',
                        duration: 5 * 1000
                    })
                    return
                }

                const list = Array.isArray(res.data.data) ? res.data.data : []
                this.vectorOpt = list.map(item => ({
                    ...item,
                    value: item.id,
                    vectorDbTypeCode: this.parseVectorDbTypeValue(item.vectorDBType),
                    vectorDbTypeLabel: this.getVectorDbTypeLabel(item.vectorDBType),
                    label: `${item.alias || item.name || '未命名向量库'} [${this.getVectorDbTypeLabel(item.vectorDBType)}]`
                }))

                if (this.isTextEmbeddingModelSelected && !this.embeddingForm.vectorDbId && this.vectorOpt.length > 0) {
                    const preferredVector = this.vectorOpt.find(item => this.isInMemoryVectorDbType(item.vectorDbTypeCode)) || this.vectorOpt[0]
                    this.embeddingForm.vectorDbId = preferredVector.value
                }
            } catch (e) {
                this.$message({
                    message: '向量库列表加载失败：' + (e.message || '未知错误'),
                    type: 'error',
                    duration: 5 * 1000
                })
            } finally {
                this.vectorLoading = false
            }
        },
        onModelSelectionChanged() {
            if (this.isSpeechToTextModelSelected) {
                this.tacticalForm.chatMode = '直接测试'
            } else if (this.supportsChatMode && !this.tacticalForm.chatMode) {
                this.tacticalForm.chatMode = '对话模式'
            } else if (!this.supportsChatMode && this.tacticalForm.chatMode === '对话模式') {
                this.tacticalForm.chatMode = '直接测试'
            }

            if (this.isTextEmbeddingModelSelected) {
                this.getVectorOptData()
            }
        },
        openSpeechFilePicker() {
            if (this.$refs && this.$refs.sttFileInput) {
                this.$refs.sttFileInput.click()
            }
        },
        clearSpeechInput() {
            this.sttForm.audioFileName = ''
            this.sttForm.audioBase64 = ''
            this.sttForm.audioLocalRelativePath = ''
            this.sttForm.fileSize = 0
            if (this.$refs && this.$refs.sttFileInput) {
                this.$refs.sttFileInput.value = ''
            }
            this.pageChange = true
        },
        async handleSpeechFileChange(event) {
            const file = event && event.target && event.target.files && event.target.files[0]
            if (!file) {
                return
            }

            try {
                const base64 = await this.readFileAsBase64(file)
                this.sttForm.audioFileName = file.name || ''
                this.sttForm.audioBase64 = base64
                this.sttForm.audioLocalRelativePath = ''
                this.sttForm.fileSize = Number(file.size || 0)
                if (!this.content || !this.content.trim()) {
                    this.content = ncfST('请识别音频内容：{0}', file.name || 'audio')
                }
                this.promptChangeHandel(file.name || 'audio', 'content')
            } catch (e) {
                this.$message({
                    message: '读取音频失败：' + (e.message || '未知错误'),
                    type: 'error',
                    duration: 5 * 1000
                })
            }
        },
        readFileAsBase64(file) {
            return new Promise((resolve, reject) => {
                const reader = new FileReader()
                reader.onload = () => {
                    const raw = typeof reader.result === 'string' ? reader.result : ''
                    resolve(raw)
                }
                reader.onerror = () => reject(reader.error || new Error('FileReader failed'))
                reader.readAsDataURL(file)
            })
        },
        ensurePromptContentForCurrentModel() {
            if (this.isTextEmbeddingModelSelected && !this.embeddingForm.vectorDbId) {
                this.$message({
                    message: '请选择 VectorDB',
                    type: 'warning',
                    duration: 3000
                })
                return false
            }
            if (!this.isSpeechToTextModelSelected) {
                return true
            }
            if (!this.sttForm.audioBase64 && !this.sttForm.audioLocalRelativePath) {
                this.$message({
                    message: '请先上传音频文件',
                    type: 'warning',
                    duration: 3000
                })
                return false
            }
            if (!this.content || !this.content.trim()) {
                const fallbackName = this.sttForm.audioFileName || 'audio'
                this.content = ncfST('请识别音频内容：{0}', fallbackName)
            }
            return true
        },
        buildExecutionOptionsPayload() {
            const payload = {}
            if (this.isTextToSpeechModelSelected) {
                payload.textToSpeech = {
                    voice: (this.ttsForm.voice || 'alloy').trim() || 'alloy',
                    format: (this.ttsForm.format || 'mp3').trim() || 'mp3',
                    speedRatio: Number(this.ttsForm.speedRatio || 1.0)
                }
            }

            if (this.isSpeechToTextModelSelected) {
                payload.speechToText = {
                    audioFileName: this.sttForm.audioFileName || null,
                    audioBase64: this.sttForm.audioBase64 || null,
                    audioLocalRelativePath: this.sttForm.audioLocalRelativePath || null,
                    language: this.sttForm.language || null
                }
            }

            if (this.isTextEmbeddingModelSelected) {
                payload.textEmbedding = {
                    vectorDbId: this.embeddingForm.vectorDbId || null,
                    collectionName: this.embeddingForm.collectionName || null,
                    searchTopK: Number(this.embeddingForm.searchTopK || 3),
                    searchQuery: this.embeddingForm.searchQuery || null
                }
            }

            return Object.keys(payload).length > 0 ? payload : null
        },
        // addmodel dialog disable
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
        //Legacy comment translated to English.
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
                        //Legacy comment translated to English.
                        await this.getModelOptData().then(() => {
                            this.modelid = res.data.data.id
                        })
                        //Legacy comment translated to English.
                        this.$message({
                            message: '添加成功！',
                            type: 'success'
                        })
                        // disabledialog
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


        // disableaddrange dialog
        fieldFormCloseDialog() {
            this.fieldForm = {
                alias: ''
            }
            this.$refs.fieldForm.resetFields();
        },
        //Legacy comment translated to English.
        fieldFormSubmitBtn() {
            const that = this
            this.$refs.fieldForm.validate(async (valid) => {
                if (valid) {
                    this.fieldFormVisible = false
                    // post interface /api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.AddAsync'
                    const res = await servicePR.post('/api/Senparc.Xncf.PromptRange/PromptRangeAppService/Xncf.PromptRange_PromptRangeAppService.AddAsync?alias='
                        + that.fieldForm.alias, {})
                    if (res.data.success) {
                        //Legacy comment translated to English.
                        await this.getFieldList().then(() => {
                            that.promptField = res.data.data.id
                            this.resetPageData()
                        })
                        //Legacy comment translated to English.
                        this.$message({
                            message: '添加成功！',
                            type: 'success'
                        })
                        // disabledialog
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
        //Legacy comment translated to English.
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
            //Legacy comment translated to English.
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
        //Legacy comment translated to English.
        async getPromptOptData(id, isExpected) {
            //Legacy comment translated to English.
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
                        label: ncfST('名称：{0} | 版本号：{1} | 平均分：{2} | 最高分：{3} {4}', item.nickName || ncfST('未设置'), item.fullVersion, avg, max, item.isDraft ? ncfST('(草稿)') : ''),
                        value: item.id,
                        disabled: false,
                    }
                })
                this.dailogpromptOptlist = _promptOpt
                if (isExpected) {
                    this.expectedPluginFoem.checkList.push(`${_find.value}`)
                    //Legacy comment translated to English.
                    this.expectedPluginFieldList.push({
                        id: _find.id,
                        label: _find.label, // rangename
                        idkey: `${_find.value}`, // rangeid
                        children: _promptOpt.map(item => {
                            this.expectedPluginFoem.checkList.push(`${_find.value}_${item.id}`)
                            return {
                                id: item.id,
                                label: item.label, // rangename
                                idkey: `${_find.value}_${item.id}`, // rangeid
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
        //Legacy comment translated to English.
        async getPromptetail(id, overwrite, isChart) {
            let res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Get?id=${Number(id)}`,)
            /*console.log('getPromptetail:', res)*/
            if (res.data.success) {
                //console.log('getPromptetail:', res.data)
                //Legacy comment translated to English.
                let copyResultData = JSON.parse(JSON.stringify(res.data.data))
                let vArr = copyResultData.fullVersion.split('-')
                copyResultData.promptFieldStr = vArr[0] || ''
                copyResultData.promptStr = vArr[1] || ''
                copyResultData.tacticsStr = vArr[2] || ''
                if (copyResultData.stopSequences) {
                    copyResultData.stopSequences = JSON.parse(copyResultData.stopSequences).join(',')
                }
                this.promptDetail = copyResultData
                //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
                    this.getOutputList(this.promptDetail.id)
                    if (isChart) {
                        //Legacy comment translated to English.
                        this.getScoringTrendData()
                    }


                    //Legacy comment translated to English.
                    let _parameterViewList = JSON.parse(JSON.stringify(this.parameterViewList))

                    this.parameterViewList = _parameterViewList.map(item => {
                        if (item) {
                            item.value = this.promptDetail[item.formField] || item.value
                        }
                        return item
                    })

                    //Legacy comment translated to English.
                    let _findIndex = this.modelOpt.findIndex(item => item.value === this.promptDetail.modelId)
                    if (_findIndex > -1) {
                        //Legacy comment translated to English.
                        this.modelid = this.promptDetail.modelId
                    } else {
                        this.modelid = ''
                    }
                    this.onModelSelectionChanged()
                    // prompt inputcontent
                    this.content = this.promptDetail.promptContent || ''
                    //Legacy comment translated to English.
                    this.remarks = this.promptDetail.note || ''
                    // promptrequestparameters
                    let _promptParamForm = JSON.parse(JSON.stringify(this.promptParamForm))
                    _promptParamForm.prefix = this.promptDetail.prefix || ''
                    _promptParamForm.suffix = this.promptDetail.suffix || ''
                    _promptParamForm.variableList = []
                    if (this.promptDetail.variableDictJson) {
                        let _variableDictJson = JSON.parse(this.promptDetail.variableDictJson)
                        //Legacy comment translated to English.
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

                    //Legacy comment translated to English.
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
                    
                    //Legacy comment translated to English.
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
        // delete prompt 
        async btnDeleteHandle(id, versionRecord) {
            const res = await servicePR.request({
                method: 'delete',
                url: `/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.DeleteAsync?id=${id}`,
            });
            if (res.data.success) {
                //Legacy comment translated to English.
                if (id === this.promptid) {
                    this.resetPageData()
                } else {
                    //Legacy comment translated to English.
                    await this.getPromptOptData(this.promptid)
                }

                if (versionRecord) {
                    //Legacy comment translated to English.
                    await this.getVersionRecordData()
                }


                // deletesuccess
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
        //Legacy comment translated to English.
        async btnEditHandle(item, isSave) {

            if (!item) return
            const res = await servicePR.request({
                method: 'post',
                url: `/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Modify`,
                data: item
            });
            if (res.data.success) {
                if (isSave) {
                    //Legacy comment translated to English.
                    this.$message({
                        message: '保存成功！',
                        type: 'success'
                    })
                } else {
                    //Legacy comment translated to English.
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

        //Legacy comment translated to English.
        aiScoreFormAddRow() {
            let _len = this.aiScoreForm.resultList.length
            this.aiScoreForm.resultList.push({
                id: _len + 1,
                label: `预期结果`,
                value: ''
            })
        },
        //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            //    this.aiScoreForm = {
            //        resultList: [{
            //            id: 1,
            //Legacy comment translated to English.
            //            value: ''
            //        }]
            //    }
            //}
            //Legacy comment translated to English.
            this.aiScoreFormVisible = !this.aiScoreFormVisible
        },
        //Legacy comment translated to English.
        aiScoreFormCloseDialog() {
            this.$refs.aiScoreForm.resetFields();
        },
        aiScoreFormClose() {
            this.aiScoreFormVisible = false
            //Legacy comment translated to English.
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
        //Legacy comment translated to English.
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
                        //Legacy comment translated to English.
                        this.getPromptetail(this.promptid, false)
                        // disabledialog
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
        //Legacy comment translated to English.
        async copyPromptResult(item, rawResult) {
            const text = rawResult ? item.resultString : item.resultStringHtml;
            await window.PromptRangeUtils.CopyHelper.copyToClipboardAsync(text, '复制成功', '复制失败', true)
        },
        
        /**

         * Legacy comment translated to English.

         * Legacy comment translated to English.

         */
        initCodeCopyButtons() {
            const self = this;
            
            //Legacy comment translated to English.
            const observer = new MutationObserver((mutations) => {
                mutations.forEach((mutation) => {
                    if (mutation.addedNodes.length) {
                        self.addCopyButtonsToCodeBlocks();
                    }
                });
            });
            
            //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            this.addCopyButtonsToCodeBlocks();
        },
        
        /**

         * Legacy comment translated to English.

         */
        addCopyButtonsToCodeBlocks() {
            const self = this;
            
            //Legacy comment translated to English.
            const codeBlocks = document.querySelectorAll(
                '.contentRow pre:not(.copy-btn-added), .chat-message-content pre:not(.copy-btn-added)'
            );
            
            codeBlocks.forEach((pre) => {
                //Legacy comment translated to English.
                pre.classList.add('copy-btn-added');
                
                //Legacy comment translated to English.
                const copyBtn = document.createElement('button');
                copyBtn.className = 'code-copy-btn';
                copyBtn.innerHTML = '<i class="el-icon-document-copy"></i> Copy';
                copyBtn.title = '复制代码';
                
                //Legacy comment translated to English.
                copyBtn.addEventListener('click', async function(e) {
                    e.preventDefault();
                    e.stopPropagation();
                    
                    // getcodecontent
                    const codeElement = pre.querySelector('code') || pre;
                    const code = codeElement.textContent || codeElement.innerText;
                    
                    //Legacy comment translated to English.
                    if (window.PromptRangeUtils && window.PromptRangeUtils.CopyHelper) {
                        const success = await window.PromptRangeUtils.CopyHelper.copyToClipboardAsync(
                            code, 
                            '代码复制成功', 
                            '代码复制失败',
                            true
                        );
                        
                        if (success) {
                            //Legacy comment translated to English.
                            copyBtn.classList.add('copied');
                            copyBtn.innerHTML = '<i class="el-icon-check"></i> Copied!';
                            
                            //Legacy comment translated to English.
                            setTimeout(() => {
                                copyBtn.classList.remove('copied');
                                copyBtn.innerHTML = '<i class="el-icon-document-copy"></i> Copy';
                            }, 2000);
                        }
                    } else {
                        //Legacy comment translated to English.
                        self.fallbackCopyCode(code, copyBtn);
                    }
                });
                
                //Legacy comment translated to English.
                pre.appendChild(copyBtn);
            });
        },
        
        /**

         * Legacy comment translated to English.

         * English documentation comment.

         * English documentation comment.

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
        //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            let totalItemsHeight = 0;
            let currentTop = 0;
            
            for (let i = 0; i < items.length; i++) {
                totalItemsHeight += items[i].offsetHeight;
                if (i < index) {
                    currentTop += items[i].offsetHeight;
                }
            }
            
            const currentHeight = items[index] ? items[index].offsetHeight : 30;
            
            //Legacy comment translated to English.
            const top = (currentTop / totalHeight) * trackHeight;
            const height = Math.max((currentHeight / totalHeight) * trackHeight, 20); //Legacy comment translated to English.
            
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
            
            //Legacy comment translated to English.
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
            //Legacy comment translated to English.
            if (!timeStr) return '';
            const match = timeStr.match(/(\d{2}):(\d{2}):\d{2}/);
            return match ? match[1] + ':' + match[2] : timeStr.substring(0, 10);
        },
        //Legacy comment translated to English.
        getFinalScore(item) {
            if (!item) return null;
            //Legacy comment translated to English.
            if (item.finalScore !== undefined && item.finalScore !== null && 
                item.finalScore !== -1 && item.finalScore !== '-1') {
                return item.finalScore;
            }
            return null;
        },
        //Legacy comment translated to English.
        getScoreBarClass(item) {
            const score = this.getFinalScore(item);
            if (score === null) return '';
            
            if (score >= 8) return 'score-excellent';      //Legacy comment translated to English.
            if (score >= 6) return 'score-good';           //Legacy comment translated to English.
            if (score >= 4) return 'score-medium';         //Legacy comment translated to English.
            if (score >= 2) return 'score-low';            //Legacy comment translated to English.
            return 'score-poor';                           //Legacy comment translated to English.
        },
        //Legacy comment translated to English.
        getScoreBarStyle(item) {
            const score = this.getFinalScore(item);
            if (score === null) return { width: '0%' };
            
            //Legacy comment translated to English.
            const percentage = Math.min(Math.max((score / 10) * 100, 0), 100);
            return {
                width: percentage + '%'
            };
        },
        //Legacy comment translated to English.
        getThumbnailTooltip(item) {
            if (!item) return '';
            
            let tooltip = item.addTime;
            const score = this.getFinalScore(item);
            
            if (score !== null) {
                //Legacy comment translated to English.
                let scoreType = '';
                if (item.finalScore === item.humanScore) {
                    scoreType = ncfST('手动评分');
                } else if (item.finalScore === item.robotScore) {
                    scoreType = ncfST('AI评分');
                } else {
                    scoreType = ncfST('最终评分');
                }
                tooltip += `\n${ncfST('{0}: {1}分', scoreType, score.toFixed(1))}`;
            }
            
            return tooltip;
        },
        //Legacy comment translated to English.
        handleOutputAreaMouseMove(event) {
            const outputArea = event.currentTarget;
            const rect = outputArea.getBoundingClientRect();
            const mouseX = event.clientX;
            
            //Legacy comment translated to English.
            const relativeX = mouseX - rect.left;
            const halfWidth = rect.width / 2;
            
            //Legacy comment translated to English.
            if (relativeX > halfWidth) {
                this.showScrollbarThumbnails = true;
            } else {
                this.showScrollbarThumbnails = false;
            }
        },
        //Legacy comment translated to English.
        handleOutputAreaMouseLeave() {
            this.showScrollbarThumbnails = false;
        },
        
        //Legacy comment translated to English.
        
        //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        saveAreaWidthsToStorage() {
            try {
                localStorage.setItem('promptPage_leftAreaWidth', this.leftAreaWidth);
                localStorage.setItem('promptPage_centerAreaWidth', this.centerAreaWidth);
            } catch (e) {
                console.error('保存区域宽度设置失败:', e);
            }
        },
        
        //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        handleResize(event) {
            if (!this.isResizing) return;
            
            const deltaX = event.clientX - this.resizeStartX;
            
            if (this.resizeType === 'left') {
                //Legacy comment translated to English.
                let newWidth = this.resizeStartLeftWidth + deltaX;
                newWidth = Math.max(280, Math.min(600, newWidth)); //Legacy comment translated to English.
                this.leftAreaWidth = newWidth;
            } else if (this.resizeType === 'right') {
                //Legacy comment translated to English.
                let newWidth = this.resizeStartCenterWidth + deltaX;
                newWidth = Math.max(320, Math.min(800, newWidth)); //Legacy comment translated to English.
                this.centerAreaWidth = newWidth;
            }
            
            event.preventDefault();
        },
        
        //Legacy comment translated to English.
        stopResize() {
            if (this.isResizing) {
                this.isResizing = false;
                this.resizeType = null;
                
                document.removeEventListener('mousemove', this.handleResize);
                document.removeEventListener('mouseup', this.stopResize);
                document.body.style.cursor = '';
                document.body.style.userSelect = '';
                
                //Legacy comment translated to English.
                this.saveAreaWidthsToStorage();
            }
        },
        
        //Legacy comment translated to English.
        resetAreaWidths(event) {
            //Legacy comment translated to English.
            this.leftAreaWidth = 360;
            this.centerAreaWidth = 380;
            
            //Legacy comment translated to English.
            try {
                localStorage.removeItem('promptPage_leftAreaWidth');
                localStorage.removeItem('promptPage_centerAreaWidth');
            } catch (e) {
                console.error('清除区域宽度设置失败:', e);
            }
            
            //Legacy comment translated to English.
            if (event && event.target) {
                event.target.classList.add('reset-animation');
                setTimeout(() => {
                    event.target.classList.remove('reset-animation');
                }, 600);
            }
            
            //Legacy comment translated to English.
            this.$message({
                message: '已还原为默认布局宽度',
                type: 'success',
                duration: 2000
            });
        },
        
        //Legacy comment translated to English.
        
        //Legacy comment translated to English.
        openCompareDialog(event, item) {
            //Legacy comment translated to English.
            if (event) {
                event.stopPropagation();
            }
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            if (this.promptid) {
                this.comparePromptAId = this.promptid;
                this.loadComparePromptA(this.promptid);
            }
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            if (item && item.value) {
                this.comparePromptBId = item.value;
                this.loadComparePromptB(item.value);
            }
            
            //Legacy comment translated to English.
            this.compareDialogVisible = true;
        },
        
        //Legacy comment translated to English.
        async loadComparePromptA(id) {
            if (!id) {
                this.comparePromptA = null;
                return;
            }
            
            try {
                const data = await this.getPromptDetail(id);
                this.comparePromptA = data;
                
                //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        async loadComparePromptB(id) {
            if (!id) {
                this.comparePromptB = null;
                return;
            }
            
            try {
                const data = await this.getPromptDetail(id);
                this.comparePromptB = data;
                
                //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        async getPromptDetail(id) {
            const res = await servicePR.get(`/api/Senparc.Xncf.PromptRange/PromptItemAppService/Xncf.PromptRange_PromptItemAppService.Get?id=${Number(id)}`);
            
            if (res.data.success) {
                return res.data.data;
            } else {
                throw new Error(res.data.errorMessage || '获取Prompt详情失败');
            }
        },
        
        //Legacy comment translated to English.
        swapComparePrompts() {
            //Legacy comment translated to English.
            const tempId = this.comparePromptAId;
            this.comparePromptAId = this.comparePromptBId;
            this.comparePromptBId = tempId;
            
            //Legacy comment translated to English.
            const tempData = this.comparePromptA;
            this.comparePromptA = this.comparePromptB;
            this.comparePromptB = tempData;
        },
        
        //Legacy comment translated to English.
        hasScore(score) {
            return score !== null && score !== undefined && score > -1;
        },
        
        //Legacy comment translated to English.
        getPromptPrefix(side) {
            const prompt = side === 'A' ? this.comparePromptA : this.comparePromptB;
            if (!prompt) return '';
            return prompt.prefix || '';
        },
        
        //Legacy comment translated to English.
        getPromptSuffix(side) {
            const prompt = side === 'A' ? this.comparePromptA : this.comparePromptB;
            if (!prompt) return '';
            return prompt.suffix || '';
        },
        
        //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                const promptData = await this.getPromptDetail(promptId);
                
                if (!promptData) {
                    throw new Error('无法获取Prompt详细信息');
                }
                
                //Legacy comment translated to English.
                const versionParts = promptData.fullVersion ? promptData.fullVersion.split('-') : [];
                const targetRangeName = versionParts[0];
                
                //Legacy comment translated to English.
                const targetRange = this.promptFieldOpt.find(item => 
                    item.label === targetRangeName || item.rangeName === targetRangeName
                );
                
                if (!targetRange) {
                    throw new Error(`未找到对应的靶场: ${targetRangeName}`);
                }
                
                //Legacy comment translated to English.
                this.compareDialogVisible = false;
                
                //Legacy comment translated to English.
                this.pageChange = false;
                
                //Legacy comment translated to English.
                const needSwitchRange = this.promptField !== targetRange.value;
                
                if (needSwitchRange) {
                    //Legacy comment translated to English.
                    this.promptField = targetRange.value;
                    await this.promptChangeHandel(targetRange.value, 'promptField');
                    
                    // waitpromptOptupdatecomplete
                    await this.$nextTick();
                }
                
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                const targetPrompt = this.promptOpt.find(item => 
                    item.id === promptId || item.value === promptId || item.idkey === promptId
                );
                
                if (!targetPrompt) {
                    throw new Error('在当前靶场中未找到对应的Prompt');
                }
                
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                this.pageChange = false;
                this.promptid = targetPrompt.value || targetPrompt.id;
                
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                await this.promptChangeHandel(this.promptid, 'promptid');
                
                //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        hasFieldDiff(fieldA, fieldB) {
            //Legacy comment translated to English.
            if (fieldA === fieldB) return false;
            if ((fieldA === null || fieldA === undefined) && (fieldB === null || fieldB === undefined)) return false;
            
            //Legacy comment translated to English.
            if (typeof fieldA === 'object' && typeof fieldB === 'object') {
                return JSON.stringify(fieldA) !== JSON.stringify(fieldB);
            }
            
            return fieldA !== fieldB;
        },
        
        //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        getContentDiffHtml(side) {
            const contentA = this.comparePromptA?.promptContent || '';
            const contentB = this.comparePromptB?.promptContent || '';
            
            //Legacy comment translated to English.
            if (!contentA && !contentB) {
                return '<span class="diff-empty">暂无内容</span>';
            }
            
            //Legacy comment translated to English.
            if (!contentA && side === 'A') {
                return '<span class="diff-empty">暂无内容</span>';
            }
            if (!contentB && side === 'B') {
                return '<span class="diff-empty">暂无内容</span>';
            }
            
            //Legacy comment translated to English.
            if (contentA === contentB) {
                return this.escapeHtml(side === 'A' ? contentA : contentB);
            }
            
            //Legacy comment translated to English.
            if (typeof Diff !== 'undefined') {
                const diff = Diff.diffLines(contentA, contentB);
                return this.renderDiffHtml(diff, side);
            }
            
            //Legacy comment translated to English.
            return this.escapeHtml(side === 'A' ? contentA : contentB);
        },
        
        //Legacy comment translated to English.
        getVariablesDiffHtml(side) {
            const varsA = this.comparePromptA?.variableDictJson || '{}';
            const varsB = this.comparePromptB?.variableDictJson || '{}';
            
            //Legacy comment translated to English.
            let formattedA = varsA;
            let formattedB = varsB;
            try {
                formattedA = JSON.stringify(JSON.parse(varsA), null, 2);
            } catch (e) {
                //Legacy comment translated to English.
            }
            try {
                formattedB = JSON.stringify(JSON.parse(varsB), null, 2);
            } catch (e) {
                //Legacy comment translated to English.
            }
            
            //Legacy comment translated to English.
            if (formattedA === formattedB) {
                return this.escapeHtml(side === 'A' ? formattedA : formattedB);
            }
            
            //Legacy comment translated to English.
            if (typeof Diff !== 'undefined') {
                const diff = Diff.diffLines(formattedA, formattedB);
                return this.renderDiffHtml(diff, side);
            }
            
            //Legacy comment translated to English.
            return this.escapeHtml(side === 'A' ? formattedA : formattedB);
        },
        
        //Legacy comment translated to English.
        renderDiffHtml(diff, side) {
            let html = '';
            let i = 0;
            
            while (i < diff.length) {
                const part = diff[i];
                const lines = part.value.split('\n');
                //Legacy comment translated to English.
                if (lines[lines.length - 1] === '') {
                    lines.pop();
                }
                
                //Legacy comment translated to English.
                const nextPart = i + 1 < diff.length ? diff[i + 1] : null;
                const isInlineDiffCandidate = 
                    part.removed && 
                    nextPart && 
                    nextPart.added && 
                    lines.length === 1 && 
                    nextPart.value.split('\n').filter(l => l).length === 1;
                
                if (isInlineDiffCandidate) {
                    //Legacy comment translated to English.
                    const oldLine = lines[0];
                    const newLine = nextPart.value.split('\n').filter(l => l)[0];
                    
                    if (side === 'A') {
                        html += `<span class="diff-line diff-modified">- ${this.renderInlineDiff(oldLine, newLine, 'removed')}</span>`;
                    } else {
                        html += `<span class="diff-line diff-modified">+ ${this.renderInlineDiff(oldLine, newLine, 'added')}</span>`;
                    }
                    
                    i += 2; //Legacy comment translated to English.
                } else {
                    //Legacy comment translated to English.
                    lines.forEach((line, lineIndex) => {
                        if (part.added) {
                            //Legacy comment translated to English.
                            if (side === 'B') {
                                html += `<span class="diff-line diff-added">+ ${this.escapeHtml(line)}</span>`;
                            }
                            //Legacy comment translated to English.
                        } else if (part.removed) {
                            //Legacy comment translated to English.
                            if (side === 'A') {
                                html += `<span class="diff-line diff-removed">- ${this.escapeHtml(line)}</span>`;
                            }
                            //Legacy comment translated to English.
                        } else {
                            //Legacy comment translated to English.
                            html += `<span class="diff-line diff-unchanged">  ${this.escapeHtml(line)}</span>`;
                        }
                    });
                    i++;
                }
            }
            
            return html || '<span class="diff-empty">暂无内容</span>';
        },
        
        //Legacy comment translated to English.
        renderInlineDiff(oldText, newText, mode) {
            if (typeof Diff === 'undefined' || !Diff.diffWords) {
                return this.escapeHtml(mode === 'removed' ? oldText : newText);
            }
            
            const wordDiff = Diff.diffWords(oldText, newText);
            let html = '';
            
            wordDiff.forEach(part => {
                const escapedText = this.escapeHtml(part.value);
                
                if (mode === 'removed') {
                    //Legacy comment translated to English.
                    if (part.removed) {
                        html += `<mark class="diff-word-removed">${escapedText}</mark>`;
                    } else if (!part.added) {
                        html += escapedText;
                    }
                } else {
                    //Legacy comment translated to English.
                    if (part.added) {
                        html += `<mark class="diff-word-added">${escapedText}</mark>`;
                    } else if (!part.removed) {
                        html += escapedText;
                    }
                }
            });
            
            return html;
        },
        
        //Legacy comment translated to English.
        escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        },
        
        //Legacy comment translated to English.
        
        //Legacy comment translated to English.
        getEditorText() {
            const editor = this.$refs.promptEditor;
            if (!editor) return '';

            //Legacy comment translated to English.
            //Legacy comment translated to English.
            const innerText = editor.innerText || '';
            if (innerText || !editor.textContent) {
                return innerText;
            }

            let text = '';
            const appendText = (node) => {
                if (node.nodeType === Node.TEXT_NODE) {
                    text += node.textContent || '';
                } else if (node.nodeName === 'BR') {
                    text += '\n';
                } else if (node.nodeType === Node.ELEMENT_NODE) {
                    Array.from(node.childNodes).forEach(appendText);
                }
            };
            Array.from(editor.childNodes).forEach(appendText);
            return text;
        },
        
        //Legacy comment translated to English.
        generateHighlightHTML(text) {
            if (!text) return '';
            
            //Legacy comment translated to English.
            const prefix = this.promptParamForm.prefix || '';
            const suffix = this.promptParamForm.suffix || '';
            
            //Legacy comment translated to English.
            console.log('[generateHighlightHTML] prefix:', prefix, 'suffix:', suffix);
            console.log('[generateHighlightHTML] variableList:', this.promptParamForm.variableList);
            
            if (!prefix || !suffix) {
                //Legacy comment translated to English.
                console.log('[generateHighlightHTML] No prefix/suffix, returning plain text');
                return text
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;')
                    .replace(/\n/g, '<br>');
            }
            
            //Legacy comment translated to English.
            const definedVarNames = new Set();
            if (this.promptParamForm.variableList && this.promptParamForm.variableList.length > 0) {
                this.promptParamForm.variableList.forEach(v => {
                    if (v.name) {
                        definedVarNames.add(v.name);
                    }
                });
            }
            
            console.log('[generateHighlightHTML] definedVarNames:', Array.from(definedVarNames));
            
            //Legacy comment translated to English.
            const escapeRegex = (str) => str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            const escapedPrefix = escapeRegex(prefix);
            const escapedSuffix = escapeRegex(suffix);
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            const regex = new RegExp(`${escapedPrefix}(.+?)${escapedSuffix}`, 'g');
            
            console.log('[generateHighlightHTML] regex:', regex);
            
            let result = '';
            let lastIndex = 0;
            let matchCount = 0;
            let match;
            
            //Legacy comment translated to English.
            while ((match = regex.exec(text)) !== null) {
                matchCount++;
                const fullMatch = match[0];  //Legacy comment translated to English.
                const varName = match[1];     //Legacy comment translated to English.
                const offset = match.index;
                
                console.log(`[generateHighlightHTML] Match ${matchCount}:`, fullMatch, 'varName:', varName, 'offset:', offset);
                
                //Legacy comment translated to English.
                const beforeText = text.substring(lastIndex, offset);
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                let processedBeforeText = beforeText
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;');
                
                //Legacy comment translated to English.
                processedBeforeText = processedBeforeText.replace(/\n/g, '<br>');
                result += processedBeforeText;
                
                //Legacy comment translated to English.
                const isDefined = definedVarNames.has(varName);
                const className = isDefined ? 'var-highlight defined' : 'var-highlight undefined';
                
                console.log(`[generateHighlightHTML] varName "${varName}" isDefined:`, isDefined);
                
                //Legacy comment translated to English.
                const escapedMatch = fullMatch
                    .replace(/&/g, '&amp;')
                    .replace(/</g, '&lt;')
                    .replace(/>/g, '&gt;');
                //Legacy comment translated to English.
                result += `<span class="${className}">${escapedMatch}</span>`;
                
                lastIndex = offset + fullMatch.length;
            }
            
            console.log('[generateHighlightHTML] Total matches:', matchCount);
            
            //Legacy comment translated to English.
            const remainingText = text.substring(lastIndex);
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            let processedRemainingText = remainingText
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');
            
            //Legacy comment translated to English.
            processedRemainingText = processedRemainingText.replace(/^[ \t]+/, '');
            //Legacy comment translated to English.
            processedRemainingText = processedRemainingText.replace(/\n/g, '<br>');
            result += processedRemainingText;
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            
            console.log('[generateHighlightHTML] Final HTML (first 200 chars):', result.substring(0, 200));
            
            return result;
        },
        
        //Legacy comment translated to English.
        escapeRegex(str) {
            return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        },
        
        //Legacy comment translated to English.
        //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            const walkNodes = (node) => {
                if (found) return;
                
                if (node.nodeType === Node.TEXT_NODE) {
                    if (node === targetContainer) {
                        //Legacy comment translated to English.
                        charCount += targetOffset;
                        found = true;
                        return;
                    }
                    //Legacy comment translated to English.
                    const nodeLength = node.textContent ? node.textContent.length : 0;
                    charCount += nodeLength;
                } else if (node.nodeName === 'BR') {
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    if (targetContainer.nodeType === Node.ELEMENT_NODE && 
                        targetContainer === node.parentNode &&
                        targetOffset > 0) {
                        //Legacy comment translated to English.
                        const brIndex = Array.from(targetContainer.childNodes).indexOf(node);
                        if (targetOffset === brIndex + 1) {
                            //Legacy comment translated to English.
                            charCount += 1;
                            found = true;
                            return;
                        }
                    }
                    //Legacy comment translated to English.
                    charCount += 1;
                } else if (node.nodeType === Node.ELEMENT_NODE) {
                    //Legacy comment translated to English.
                    if (node === targetContainer && targetOffset > 0) {
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        for (let i = 0; i < targetOffset && i < node.childNodes.length; i++) {
                            const childNode = node.childNodes[i];
                            //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
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
        
        //Legacy comment translated to English.
        //Legacy comment translated to English.
        restoreCaretPosition(offset) {
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            const sel = window.getSelection();
            if (!sel) return;
            
            const range = document.createRange();
            let charCount = 0;
            let found = false;
            
            //Legacy comment translated to English.
            const walkNodes = (node) => {
                if (found) return;
                
                if (node.nodeType === Node.TEXT_NODE) {
                    const nodeText = node.textContent || '';
                    const nodeLength = nodeText.length;
                    const nextCharCount = charCount + nodeLength;
                    
                    if (offset >= charCount && offset <= nextCharCount) {
                        //Legacy comment translated to English.
                        const positionInNode = Math.min(offset - charCount, nodeLength);
                        range.setStart(node, positionInNode);
                        range.collapse(true);
                        found = true;
                        return;
                    }
                    charCount = nextCharCount;
                } else if (node.nodeName === 'BR') {
                    //Legacy comment translated to English.
                    const nextCharCount = charCount + 1;
                    if (offset === charCount) {
                        //Legacy comment translated to English.
                        range.setStartBefore(node);
                        range.collapse(true);
                        found = true;
                        return;
                    } else if (offset === nextCharCount) {
                        //Legacy comment translated to English.
                        range.setStartAfter(node);
                        range.collapse(true);
                        found = true;
                        return;
                    }
                    charCount = nextCharCount;
                } else if (node.nodeType === Node.ELEMENT_NODE) {
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
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
                    //Legacy comment translated to English.
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
                //Legacy comment translated to English.
                console.warn('[restoreCaretPosition] Could not find position', offset, ', placing at end');
                try {
                    range.selectNodeContents(editor);
                    range.collapse(false); //Legacy comment translated to English.
                    sel.removeAllRanges();
                    sel.addRange(range);
                } catch (e) {
                    console.warn('[restoreCaretPosition] Failed to place caret at end:', e);
                }
            }
        },
        
        //Legacy comment translated to English.
        assignContentFromEditor(text) {
            this._editorContentStamp = text;
            this.content = text;
        },

        clearHighlightTimer() {
            if (this._highlightTimer) {
                clearTimeout(this._highlightTimer);
                this._highlightTimer = null;
            }
        },

        //Legacy comment translated to English.
        scheduleHighlight() {
            if (this.isComposing) {
                console.log('[IME] highlight not scheduled because composition is active');
                return;
            }
            this.clearHighlightTimer();
            this._highlightTimer = setTimeout(() => {
                this._highlightTimer = null;
                if (this.isComposing) {
                    console.log('[IME] skip highlight because composition is active');
                    return;
                }
                this.applyHighlight();
            }, 150);
        },

        isImeKeyboardEvent(e) {
            if (!e) return false;
            //Legacy comment translated to English.
            return !!(e.isComposing || e.key === 'Process' || e.keyCode === 229);
        },

        //Legacy comment translated to English.
        captureEditorText() {
            if (this.isComposing) {
                console.log('[IME] capture skipped during composition');
                return;
            }
            const editor = this.$refs.promptEditor;
            if (!editor) return;

            const hasFocus = document.activeElement === editor;
            const text = hasFocus ? this.getEditorText() : this.content;
            const contentChanged = text !== this.content;
            console.log('[prompt editor] capture text, changed:', contentChanged, 'length:', text.length);
            this.assignContentFromEditor(text);
            if (contentChanged) {
                this.promptChangeHandel(text, 'content');
            }
            this.scheduleHighlight();
        },

        handleCompositionStart(e) {
            this.isComposing = true;
            this._imeSessionStarted = true;
            this.clearHighlightTimer();
            if (this._compositionFlushTimer) {
                clearTimeout(this._compositionFlushTimer);
                this._compositionFlushTimer = null;
            }
            if (this._imeFallbackTimer) {
                clearTimeout(this._imeFallbackTimer);
                this._imeFallbackTimer = null;
            }
            console.log('[IME] compositionstart', e && e.data);
        },

        handleCompositionUpdate(e) {
            //Legacy comment translated to English.
            this.isComposing = true;
            this._imeSessionStarted = true;
            this.clearHighlightTimer();
            console.log('[IME] compositionupdate', e && e.data);
        },

        handleCompositionEnd(e) {
            console.log('[IME] compositionend', e && e.data);
            this.clearHighlightTimer();
            if (this._imeFallbackTimer) {
                clearTimeout(this._imeFallbackTimer);
                this._imeFallbackTimer = null;
            }
            if (this._compositionFlushTimer) {
                clearTimeout(this._compositionFlushTimer);
            }
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            this.isComposing = true;
            this._compositionFlushTimer = setTimeout(() => {
                this._compositionFlushTimer = null;
                this._imeSessionStarted = false;
                this.isComposing = false;
                console.log('[IME] composition flushed');
                this.captureEditorText();
                //Legacy comment translated to English.
                requestAnimationFrame(() => {
                    if (this.isComposing) return;
                    const editor = this.$refs.promptEditor;
                    const latestText = editor && document.activeElement === editor
                        ? this.getEditorText()
                        : this.content;
                    if (latestText !== this.content) {
                        console.log('[IME] post-composition text changed, recapture. length:', latestText.length);
                        this.captureEditorText();
                    }
                });
            }, 0);
        },

        handleEditorBlur() {
            if (this.isComposing) {
                console.log('[IME] ignore blur during composition');
                return;
            }
            //Legacy comment translated to English.
            this.clearHighlightTimer();
        },

        handleEditorKeyUp() {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            if (this.isComposing) {
                this.clearHighlightTimer();
            }
        },

        armImeFallback() {
            if (this._imeFallbackTimer) clearTimeout(this._imeFallbackTimer);
            //Legacy comment translated to English.
            this._imeFallbackTimer = setTimeout(() => {
                this._imeFallbackTimer = null;
                if (this.isComposing && !this._imeSessionStarted) {
                    console.log('[IME] fallback unlock without composition session');
                    this.isComposing = false;
                    this.captureEditorText();
                }
            }, 80);
        },

        //Legacy comment translated to English.
        handlePaste(e) {
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            //Legacy comment translated to English.
            this._isPasting = true;
            
            //Legacy comment translated to English.
            if (this._highlightTimer) clearTimeout(this._highlightTimer);
            
            //Legacy comment translated to English.
            const sel = window.getSelection();
            let pasteStartPos = 0;
            
            if (sel.rangeCount > 0) {
                //Legacy comment translated to English.
                const range = sel.getRangeAt(0);
                const targetContainer = range.startContainer;
                const targetOffset = range.startOffset;
                
                let charCount = 0;
                let found = false;
                
                //Legacy comment translated to English.
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
            
            //Legacy comment translated to English.
            const pasteData = e.clipboardData ? e.clipboardData.getData('text/plain') : '';
            const pasteTextLength = pasteData.length;
            
            console.log('[handlePaste] Paste data length:', pasteTextLength, 'pasteStartPos:', pasteStartPos);
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            requestAnimationFrame(() => {
                setTimeout(() => {
                    this._isPasting = false;
                    if (this.isComposing) {
                        console.log('[IME] skip paste highlight during composition');
                        return;
                    }
                    
                    //Legacy comment translated to English.
                    const newCaretPos = pasteStartPos + pasteTextLength;
                    
                    console.log('[handlePaste] Calculated new caret position:', newCaretPos);
                    
                    const text = document.activeElement === editor
                        ? this.getEditorText()
                        : this.content;
                    
                    //Legacy comment translated to English.
                    const contentChanged = text !== this.content;
                    
                    //Legacy comment translated to English.
                    this.assignContentFromEditor(text);
                    
                    //Legacy comment translated to English.
                    if (contentChanged) {
                        this.promptChangeHandel(text, 'content');
                    }
                    
                    //Legacy comment translated to English.
                    this.applyHighlightWithCaretPos(newCaretPos);
                }, 10);
            });
        },
        
        //Legacy comment translated to English.
        handleKeyDown(e) {
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            if (this.isImeKeyboardEvent(e) || this.isComposing) {
                const wasComposing = this.isComposing || this._imeSessionStarted;
                this.isComposing = true;
                this.clearHighlightTimer();
                if (!wasComposing) {
                    this.armImeFallback();
                }
                console.log('[IME] keydown during composition, skip editor mutation', e.key, e.keyCode);
                return;
            }

            //Legacy comment translated to English.
            if (e.key === 'Enter' || e.keyCode === 13) {
                const editor = this.$refs.promptEditor;
                if (!editor) return;
                
                //Legacy comment translated to English.
                this._isEntering = true;
                
                //Legacy comment translated to English.
                if (this._highlightTimer) clearTimeout(this._highlightTimer);
                
                //Legacy comment translated to English.
                const sel = window.getSelection();
                let enterStartPos = 0;
                
                if (sel.rangeCount > 0) {
                    //Legacy comment translated to English.
                    enterStartPos = this.saveCaretPosition();
                    console.log('[handleKeyDown] Enter key pressed, saved position before BR insertion:', enterStartPos);
                }
                
                //Legacy comment translated to English.
                requestAnimationFrame(() => {
                    setTimeout(() => {
                        this._isEntering = false;
                        if (this.isComposing) {
                            console.log('[IME] skip enter highlight during composition');
                            return;
                        }
                        
                        //Legacy comment translated to English.
                        const newCaretPos = enterStartPos + 1;
                        
                        console.log('[handleKeyDown] After Enter, calculated new caret position:', newCaretPos);
                        
                        const text = document.activeElement === editor
                            ? this.getEditorText()
                            : this.content;
                        
                        //Legacy comment translated to English.
                        const contentChanged = text !== this.content;
                        
                        //Legacy comment translated to English.
                        this.assignContentFromEditor(text);
                        
                        //Legacy comment translated to English.
                        if (contentChanged) {
                            this.promptChangeHandel(text, 'content');
                        }
                        
                        //Legacy comment translated to English.
                        this.applyHighlightWithCaretPos(newCaretPos);
                    }, 10);
                });
            }
        },
        
        //Legacy comment translated to English.
        handleEditorInput(e) {
            const inputType = e && e.inputType ? String(e.inputType) : '';
            const isCompositionInput = !!(e && (e.isComposing || inputType.indexOf('Composition') !== -1));
            if (this.isComposing || isCompositionInput) {
                console.log('[IME] ignore input during composition', inputType, e && e.data);
                this.clearHighlightTimer();
                return;
            }
            if (this._isPasting) return;   //Legacy comment translated to English.
            if (this._isEntering) return;  //Legacy comment translated to English.

            this.captureEditorText();
        },
        
        //Legacy comment translated to English.
        cleanupHighlightBrTags(editor) {
            if (!editor) return;
            
            console.log('[cleanupHighlightBrTags] Starting cleanup...');
            
            //Legacy comment translated to English.
            const highlightSpans = Array.from(editor.querySelectorAll('.var-highlight'));
            
            console.log('[cleanupHighlightBrTags] Found', highlightSpans.length, 'highlight spans');
            
            highlightSpans.forEach(span => {
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                let prevSibling = span.previousSibling;
                while (prevSibling) {
                    if (prevSibling.nodeType === Node.TEXT_NODE) {
                        const textContent = prevSibling.textContent;
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        if (!textContent || textContent.length === 0) {
                            //Legacy comment translated to English.
                            const toRemove = prevSibling;
                            prevSibling = prevSibling.previousSibling;
                            toRemove.remove();
                        } else {
                            //Legacy comment translated to English.
                            break;
                        }
                    } else {
                        //Legacy comment translated to English.
                        break;
                    }
                }
                
                //Legacy comment translated to English.
                //Legacy comment translated to English.
                let nextSibling = span.nextSibling;
                while (nextSibling) {
                    if (nextSibling.nodeType === Node.TEXT_NODE) {
                        const textContent = nextSibling.textContent;
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        if (!textContent || textContent.length === 0) {
                            //Legacy comment translated to English.
                            const toRemove = nextSibling;
                            nextSibling = nextSibling.nextSibling;
                            toRemove.remove();
                        } else {
                            //Legacy comment translated to English.
                            break;
                        }
                    } else {
                        //Legacy comment translated to English.
                        //Legacy comment translated to English.
                        break;
                    }
                }
            });
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            editor.normalize();
            
            console.log('[cleanupHighlightBrTags] Cleanup completed. Editor innerHTML (first 300 chars):', editor.innerHTML.substring(0, 300));
        },
        
        //Legacy comment translated to English.
        applyHighlightWithCaretPos(expectedCaretPos) {
            if (this.isComposing) {
                console.log('[IME] applyHighlightWithCaretPos blocked during composition');
                return;
            }
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            //Legacy comment translated to English.
            const hasFocus = document.activeElement === editor;
            const text = hasFocus ? this.getEditorText() : this.content;
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            this.assignContentFromEditor(text);
            
            //Legacy comment translated to English.
            const html = this.generateHighlightHTML(text);
            
            // updateHTML
            editor.innerHTML = html;
            
            //Legacy comment translated to English.
            requestAnimationFrame(() => {
                if (this.isComposing) {
                    console.log('[IME] skip caret restore during composition');
                    return;
                }
                //Legacy comment translated to English.
                this.cleanupHighlightBrTags(editor);
                
                //Legacy comment translated to English.
                this.$nextTick(() => {
                    if (this.isComposing) return;
                    //Legacy comment translated to English.
                    const currentText = editor.innerText || '';
                    const currentTextLength = currentText.length;
                    const targetPos = Math.min(expectedCaretPos, currentTextLength);
                    console.log('[applyHighlightWithCaretPos] Restoring caret position:', targetPos, 'expected:', expectedCaretPos, 'current text length:', currentTextLength);
                    this.restoreCaretPosition(targetPos);
                });
            });
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
        },
        
        //Legacy comment translated to English.
        applyHighlight(textOverride) {
            if (this.isComposing) {
                console.log('[IME] applyHighlight blocked during composition');
                return;
            }
            const editor = this.$refs.promptEditor;
            if (!editor) return;
            
            //Legacy comment translated to English.
            const hasFocus = document.activeElement === editor;
            
            //Legacy comment translated to English.
            const text = typeof textOverride === 'string'
                ? textOverride
                : (hasFocus ? this.getEditorText() : this.content);
            if (typeof textOverride !== 'string') {
                this.assignContentFromEditor(text);
            }
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            let caretPos = 0;
            if (hasFocus) {
                const sel = window.getSelection();
                if (sel.rangeCount > 0) {
                    const range = sel.getRangeAt(0);
                    //Legacy comment translated to English.
                    const preCaretRange = range.cloneRange();
                    preCaretRange.selectNodeContents(editor);
                    preCaretRange.setEnd(range.startContainer, range.startOffset);
                    //Legacy comment translated to English.
                    //Legacy comment translated to English.
                    caretPos = this.saveCaretPosition();
                }
                console.log('[applyHighlight] Saved caret position:', caretPos, 'text length:', text.length);
            }
            
            //Legacy comment translated to English.
            const html = this.generateHighlightHTML(text);
            
            // updateHTML
            editor.innerHTML = html;
            
            //Legacy comment translated to English.
            requestAnimationFrame(() => {
                if (this.isComposing) {
                    console.log('[IME] skip highlight cleanup during composition');
                    return;
                }
                //Legacy comment translated to English.
                this.cleanupHighlightBrTags(editor);
                
                //Legacy comment translated to English.
                if (hasFocus && caretPos > 0) {
                    //Legacy comment translated to English.
                    this.$nextTick(() => {
                        if (this.isComposing) return;
                        //Legacy comment translated to English.
                        console.log('[applyHighlight] Restoring caret position:', caretPos);
                        this.restoreCaretPosition(caretPos);
                    });
                }
            });
            
            //Legacy comment translated to English.
            //Legacy comment translated to English.
            //Legacy comment translated to English.
        },
        
        //Legacy comment translated to English.
        getTargetRangeName: function(id) {
            //Legacy comment translated to English.
            return window.PromptRangeUtils.NameHelper.getName(
                this.promptFieldOpt, id, '未知靶场'
            );
        },
        
        getTargetLaneName: function(id) {
            //Legacy comment translated to English.
            var lane = window.PromptRangeUtils.NameHelper.getOption(
                this.promptOpt, id, 'idkey'
            );
            return lane ? (lane.nickName || lane.label || '未知靶道') : '未知靶道';
        },
        
        getTacticalName: function(id) {
            //Legacy comment translated to English.
            return window.PromptRangeUtils.NameHelper.getName(
                this.tacticalOpt, id, '未知战术'
            );
        },
        
        getModelName: function(id) {
            //Legacy comment translated to English.
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

var app = new Vue({
    el: "#app",
    data() {
        var validateCode = (rule, value, callback) => {
            callback();
        };
        return {
            activeNavKey: 'list', // list | recall | guide
            defaultMSG: null,
            editorData: '',
            elSize: 'medium', //Legacy comment translated to English.
            //Legacy comment translated to English.
            visible: {
                drawerGroup: false, // group add|edit
                dialogFile: false,   //Legacy comment translated to English.
                embeddingProgress: false,  //Legacy comment translated to English.
                embeddingResult: false,    //Legacy comment translated to English.
                searchSettingsDrawer: false,
                paragraphDetailDialog: false,
            },
            //Legacy comment translated to English.
            selectedKnowledgeBaseId: null,
            recallContent: '',
            topK: 5,
            recallRerankMode: '', //Legacy comment translated to English.
            recallLoading: false,
            recallResults: [],
            lastElapsedMilliseconds: null,
            recordList: [],
            recordPage: 1,
            recordPageSize: 5,
            paragraphDetailItem: null,
            recallKbOptions: [], //Legacy comment translated to English.
            embeddingProgressPercent: 0,
            embeddingProgressStatus: '',   // '' | success | exception
            embeddingProgressText: ncfST('Frontend.134'),
            embeddingResultText: '',
            embeddingTaskName: '',
            embeddingDoneCount: 0,
            embeddingTotalCount: 1,
            embeddingPanelCollapsed: false,
            embeddingStartedAt: null,
            embeddingResultCard: {
                isError: false,
                title: '',
                timeText: '',
                taskName: '',
                total: 0,
                success: 0,
                fail: 0,
                detail: ''
            },
            _embeddingTimer: null,
            _embeddingResultTimer: null,
            configUploadFileList: [], //Legacy comment translated to English.
            form:
            {
                content: ''
            },
            config:
            {
                initialFrameHeight: 500
            },
            //pagination parameters
            paginationQuery:
            {
                total: 5
            },
            //pagination API parameters
            listQuery: {
                pageIndex: 1,
                pageSize: 20,
                keyword: '',
                orderField: ''
            },
            selectDefaultEmbeddingModel: [],
            embeddingModelData: [],
            selectDefaultVectorDB: [],
            vectorDBData: [],
            vectorDBDataAll: [],
            selectDefaultChatModel: [],
            chatModelData: [],
            page: {
                page: 1,
                size: 10,
                modelCount: 999
            },
            filterTableHeader: {
                embeddingModelId: [],
                vectorDBId: [],
                chatModelId: [],
                name: [],
                content: []
            },
            colData: [
                { title: ncfST('Frontend.177'), istrue: false },
                { title: ncfST('Frontend.198'), istrue: false },
                { title: ncfST('Frontend.203'), istrue: false },
                { title: ncfST('Frontend.011'), istrue: true },
                { title: ncfST('Frontend.090'), istrue: true },
            ],
            checkBoxGroup: [
                ncfST('Frontend.177'), ncfST('Frontend.198'), ncfST('Frontend.203'), ncfST('Frontend.011'), ncfST('Frontend.090')
            ],
            checkedColumns: [],
            contentTypeData: [
                { value: 1, label: ncfST('Frontend.090') },
                { value: 2, label: ncfST('Frontend.255') },
                { value: 3, label: ncfST('Frontend.254') }
            ],
            keyword: '',
            multipleSelection: '',
            radio: '',
            props: { multiple: true },
            //Legacy comment translated to English.
            tableData: [],
            navigationTarget: {
                knowledgeBaseId: 0,
                focus: '',
                handled: false
            },
            uid: '',
            fileList: [],
            sizeForm: {
                name: '',
                region: '',
                date1: '',
                date2: '',
                delivery: false,
                type: [],
                resource: '',
                desc: ''
            },
            size: 'mini',
            dialogImageUrl: '',
            dialogVisible: false,
            dialog:
            {
                title: ncfST('Frontend.215'),
                visible: false,
                data:
                {
                    id: '', embeddingModelId: null, vectorDBId: null, chatModelId: null, name: '', content: '',
                    retrievalConfig: {
                        chunkingStrategy: 'Fixed',
                        chunkSize: 800,
                        chunkOverlap: 120,
                        rerankMode: 'None',
                        rerankModelId: 0,
                        rerankCandidateCount: 20,
                        lexicalWeight: 0.35
                    }
                },
                rules:
                {
                    name:
                        [
                            { required: true, message: ncfST('Frontend.243'), trigger: "blur" }
                        ],
                    embeddingModelId:
                        [
                            { required: true, message: ncfST('Frontend.244'), trigger: "change" }
                        ],
                    vectorDBId:
                        [
                            { required: true, message: ncfST('Frontend.245'), trigger: "change" }
                        ]
                },
                updateLoading: false,
                disabled: false,
                checkStrictly: true //Legacy comment translated to English.
            },
            detailDialog:
            {
                title: ncfST('Frontend.137'),
                visible: false,
                data:
                {
                    id: '', embeddingModelId: null, vectorDBId: null, chatModelId: null, name: '', content: ''
                },
                rules:
                {
                    name:
                        [
                            { required: true, message: ncfST('Frontend.136'), trigger: "blur" }
                        ]
                },
                updateLoading: false,
                disabled: false,
                checkStrictly: true //Legacy comment translated to English.
            },
            //Legacy comment translated to English.
            groupForm: {
                contentType: 2, // contenttype，default「file」
                files: [], //Legacy comment translated to English.
                content: '', // content
                knowledgeBasesId: '' //Legacy comment translated to English.
            },
            groupFormRules: {
                name: [
                    { required: true, message: ncfST('Frontend.162'), trigger: 'blur' },
                ],
                members: [
                    { required: true, message: ncfST('Frontend.162'), trigger: 'change' },
                ],
                adminAgentTemplateId: [
                    { required: true, message: ncfST('Frontend.162'), trigger: 'change' },
                ],
                enterAgentTemplateId: [
                    { required: true, message: ncfST('Frontend.138'), trigger: 'change' },
                ],
                // description: [
                //     { required: true, message: 'please enter a value', trigger: 'blur' },
                // ],
            },
            // group add|edit agent
            groupAgentQueryList: {
                pageIndex: 0,
                pageSize: 0,
                filter: '', // filter text
                timeSort: false, // descending by default
                proce: false, // in progress
                stop: false, // disabled
                stand: false, // standby
            },
            groupAgentList: [], //Legacy comment translated to English.
            //Legacy comment translated to English.
            fileQueryList: {
                pageIndex: 1,
                pageSize: 10,
                filter: '', //Legacy comment translated to English.
            },
            fileListTotal: 0, //Legacy comment translated to English.
            fileList: [], //Legacy comment translated to English.
            fileNamesToSelect: [], //Legacy comment translated to English.
        }
    },
    computed: {
        recallKnowledgeBaseList() {
            const source = (this.recallKbOptions && this.recallKbOptions.length)
                ? this.recallKbOptions
                : (this.tableData || []);
            return source.map(item => {
                const ready = this.isKnowledgeBaseReady(item);
                return {
                    id: item.id,
                        name: item.name || (ncfST('Frontend.234') + '-' + item.id),
                    ready: ready,
                    retrievalConfig: item.retrievalConfig || null,
                    label: (item.name || (ncfST('Frontend.234') + '-' + item.id)) + (ready ? '' : '（' + ncfST('Frontend.220') + '）')
                };
            });
        },
        selectedKnowledgeBase() {
            return this.recallKnowledgeBaseList.find(item => Number(item.id) === Number(this.selectedKnowledgeBaseId)) || null;
        },
        recallRerankSummary() {
            const kb = this.selectedKnowledgeBase;
            if (!kb) {
                return ncfST('Frontend.103');
            }
            const cfg = kb.retrievalConfig || {};
            const mode = cfg.rerankMode || 'None';
            if (mode === 'None') {
                return ncfST('Frontend.188');
            }
            const candidates = cfg.rerankCandidateCount || 20;
            if (mode === 'Lexical') {
                const weight = cfg.lexicalWeight == null ? 0.35 : cfg.lexicalWeight;
                return ncfST('Frontend.240') + ' (' + candidates + ', ' + ncfST('Frontend.238') + ' ' + weight + ')';
            }
            const modelText = cfg.rerankModelId > 0 ? ('#' + cfg.rerankModelId) : ncfST('Frontend.232');
            return ncfST('Frontend.179') + ' (' + candidates + ', ' + modelText + ')';
        },
        recordTotal() {
            return this.recordList.length;
        },
        recordPageList() {
            const start = (this.recordPage - 1) * this.recordPageSize;
            return this.recordList.slice(start, start + this.recordPageSize);
        }
    },
    created: function () {
        let that = this
        that.initializeKnowledgeBaseNavigation()
        that.getList();
        that.getEmbeddingModelList();
        that.getVectorDBList();
        that.getChatModelList();
        // getfiledata
        that.getFileListData('file');

        //Legacy comment translated to English.
        that.checkedColumns = that.colData.filter(item => item.istrue).map(item => item.title);
    },
    watch:
    {
        'dialog.visible': function (val, old) {
            // disabledialog，clear
            if (!val) {
                this.dialog.data = {
                    id: '', embeddingModelId: null, vectorDBId: null, chatModelId: null, name: '', content: '',
                    retrievalConfig: this.defaultRetrievalConfig()
                };
                this.dialog.updateLoading = false;
                this.dialog.disabled = false;
            }
        },
        'checkedColumns': function (val) {
            let arr = this.checkBoxGroup.filter(i => !val.includes(i));
            this.colData.filter(i => {
                if (arr.indexOf(i.title) != -1) {
                    i.istrue = false;
                } else {
                    i.istrue = true;
                }
            });
            this.reload = Math.random()
        },
        recordList: function () {
            const maxPage = Math.max(1, Math.ceil(this.recordTotal / this.recordPageSize));
            if (this.recordPage > maxPage) this.recordPage = maxPage;
        }
    },
    methods:
    {
        onNavClick(key) {
            this.activeNavKey = key;
            if (key === 'recall') {
                this.loadRecallKnowledgeBaseList();
            }
        },
        formatSelectLabel(item, fallbackPrefix) {
            if (!item) {
                return '';
            }
            return item.alias || item.name || ((fallbackPrefix || '选项') + '-' + item.id);
        },
        isKnowledgeBaseReady(row) {
            return !!(row && row.vectorCollectionName && row.embeddedTime);
        },
        onEmbeddingResultConfirm() {
            this.closeEmbeddingResultToast();
        },
        closeEmbeddingResultToast() {
            if (this._embeddingResultTimer) {
                clearTimeout(this._embeddingResultTimer);
                this._embeddingResultTimer = null;
            }
            this.visible.embeddingResult = false;
            this.embeddingResultText = '';
            this.getList();
            if (this.activeNavKey === 'recall' || (this.recallKbOptions && this.recallKbOptions.length)) {
                this.loadRecallKnowledgeBaseList();
            }
        },
        formatEmbeddingTime(dateObj) {
            var d = dateObj instanceof Date ? dateObj : new Date();
            var pad = function (n) { return n < 10 ? '0' + n : '' + n; };
            return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate())
                + ' ' + pad(d.getHours()) + ':' + pad(d.getMinutes()) + ':' + pad(d.getSeconds());
        },
        parseEmbeddingResultStats(message) {
            var text = String(message || '');
            var totalMatch = text.match(/总计[:：]\s*(\d+)/);
            var successMatch = text.match(/成功[:：]\s*(\d+)/);
            var failMatch = text.match(/失败[:：]\s*(\d+)/);
            var total = totalMatch ? parseInt(totalMatch[1], 10) : 0;
            var success = successMatch ? parseInt(successMatch[1], 10) : (total || 0);
            var fail = failMatch ? parseInt(failMatch[1], 10) : 0;
            if (!total && success) {
                total = success + fail;
            }
            return { total: total, success: success, fail: fail };
        },
        showEmbeddingResultToast(options) {
            var that = this;
            var opts = options || {};
            var stats = that.parseEmbeddingResultStats(opts.message || '');
            if (opts.total != null) stats.total = opts.total;
            if (opts.success != null) stats.success = opts.success;
            if (opts.fail != null) stats.fail = opts.fail;
            if (!stats.total && !opts.isError) {
                stats.total = 1;
                stats.success = 1;
                stats.fail = 0;
            }
            that.embeddingResultCard = {
                isError: !!opts.isError,
                title: opts.title || (opts.isError ? ncfST('Frontend.163') : ncfST('Frontend.161')),
                timeText: that.formatEmbeddingTime(opts.startedAt || that.embeddingStartedAt || new Date()),
                taskName: opts.taskName || that.embeddingTaskName || ncfST('Frontend.197'),
                total: stats.total,
                success: stats.success,
                fail: stats.fail,
                detail: opts.detail || ''
            };
            that.embeddingResultText = opts.message || '';
            that.visible.embeddingProgress = false;
            that.visible.embeddingResult = true;
            if (that._embeddingResultTimer) {
                clearTimeout(that._embeddingResultTimer);
            }
            that._embeddingResultTimer = setTimeout(function () {
                that.closeEmbeddingResultToast();
            }, 12000);
        },
        async loadRecallKnowledgeBaseList() {
            try {
                const res = await service.get('/Admin/KnowledgeBase/Index?handler=KnowledgeBases&pageIndex=1&pageSize=200&keyword=&orderField=AddTime%20Desc');
                const payload = res && res.data && res.data.data ? res.data.data : {};
                this.recallKbOptions = Array.isArray(payload.list) ? payload.list : [];
            } catch (e) {
                console.error(e);
                this.$message.error(ncfST('Frontend.279', ''));
            }
        },
        handleRowCommand(command, row) {
            if (command === 'edit') {
                this.handleEdit(0, row, 'edit');
            } else if (command === 'recall') {
                this.selectedKnowledgeBaseId = row && row.id != null ? Number(row.id) : null;
                this.activeNavKey = 'recall';
                this.loadRecallKnowledgeBaseList();
            } else if (command === 'delete') {
                this.$confirm(ncfST('Frontend.150', row && row.name ? row.name : ''), ncfST('Frontend.151'), {
                    confirmButtonText: ncfST('Frontend.007'),
                    cancelButtonText: ncfST('Frontend.097'),
                    type: 'warning'
                }).then(() => {
                    this.handleDelete(0, row);
                }).catch(() => {});
            }
        },
        formatScore(score) {
            const value = Number(score);
            return Number.isFinite(value) ? value.toFixed(4) : '—';
        },
        handleRecordSizeChange(value) {
            this.recordPageSize = value;
            this.recordPage = 1;
        },
        handleRecordPageChange(value) {
            this.recordPage = value;
        },
        openParagraphDetail(item) {
            this.paragraphDetailItem = item;
            this.visible.paragraphDetailDialog = true;
        },
        async doRecall() {
            const knowledgeBase = this.selectedKnowledgeBase;
            const content = (this.recallContent || '').trim();
            const recallUrl = '/api/Senparc.Xncf.KnowledgeBase/RecallTestAppService/Xncf.KnowledgeBase_RecallTestAppService.RecallTest';
            if (!knowledgeBase) {
                this.$message.warning(ncfST('Frontend.103'));
                return;
            }
            if (!knowledgeBase.ready) {
                this.$message.warning(ncfST('Frontend.280'));
                return;
            }
            if (!content) {
                this.$message.warning(ncfST('Frontend.281'));
                return;
            }

            this.recallLoading = true;
            this.recallResults = [];
            this.lastElapsedMilliseconds = null;
            try {
                const response = await service.post(recallUrl, {
                    id: Number(knowledgeBase.id),
                    content: content,
                    topK: this.topK,
                    rerankMode: this.recallRerankMode || ''
                });
                const body = response && response.data;
                const results = body && Object.prototype.hasOwnProperty.call(body, 'data') ? body.data : body;
                if (!Array.isArray(results)) throw new Error(ncfST('Frontend.282'));

                this.recallResults = results.map(function (item, index) {
                    return Object.assign({}, item, {
                        rank: item.rank || index + 1,
                        content: item.content || '',
                        sourceName: item.sourceName || '',
                        sourceLink: item.sourceLink || ''
                    });
                });
                this.lastElapsedMilliseconds = this.recallResults.length ? this.recallResults[0].elapsedMilliseconds : null;
                const scores = this.recallResults.map(item => Number(item.score)).filter(Number.isFinite);
                this.recordList.unshift({
                    queryContent: content,
                    knowledgeBaseName: knowledgeBase.name,
                    resultCount: this.recallResults.length,
                    highestScore: scores.length ? Math.max.apply(null, scores) : null,
                    time: new Date().toLocaleString('zh-CN', { hour12: false })
                });
                this.recordPage = 1;
                this.$message.success(this.recallResults.length ? ncfST('Frontend.283') : ncfST('Frontend.284'));
            } catch (error) {
                const response = error && error.response && error.response.data;
                const msg = (response && (response.errorMessage || response.message || response.title)) || (error && error.message) || ncfST('Frontend.278');
                this.$notify({ title: ncfST('Frontend.285'), message: msg, type: 'error', duration: 6000 });
            } finally {
                this.recallLoading = false;
            }
        },
        initializeKnowledgeBaseNavigation() {
            const query = new URLSearchParams(window.location.search || '')
            const knowledgeBaseId = Number(query.get('knowledgeBaseId') || 0)
            if (!Number.isInteger(knowledgeBaseId) || knowledgeBaseId <= 0) {
                return
            }

            this.navigationTarget = {
                knowledgeBaseId: knowledgeBaseId,
                focus: query.get('focus') === 'materials' ? 'materials' : 'edit',
                handled: false
            }
            this.listQuery.pageIndex = 1
        },
        applyKnowledgeBaseNavigation(rows) {
            const target = this.navigationTarget || {}
            const knowledgeBaseId = Number(target.knowledgeBaseId || 0)
            if (target.handled || !Number.isInteger(knowledgeBaseId) || knowledgeBaseId <= 0) {
                return
            }

            target.handled = true
            const knowledgeBase = (rows || []).find(item => Number(item.id) === knowledgeBaseId)
            if (!knowledgeBase) {
                this.$message.warning('未找到要打开的知识库，可能已删除或当前账号没有权限。')
                return
            }

            this.$nextTick(() => {
                this.$refs.multipleTable?.setCurrentRow(knowledgeBase)
                if (target.focus === 'materials') {
                    this.handleElVisibleOpenBtn('drawerGroup', knowledgeBase)
                } else {
                    this.handleEdit(0, knowledgeBase, 'edit')
                }
            })
        },
        handleChange(value) {
        },
        handleRemove(file, fileList) {
        },
        handlePictureCardPreview(file) {
            let that = this
            that.dialogImageUrl = file.url;
            that.dialogVisible = true;
        },
        uploadSuccess(res, file, fileList) {
            let that = this;
            // uploadsuccess
            that.fileList = fileList;
            //debugger
            if (res.stateCode == 0) {
                that.$notify({
                    title: ncfST('Frontend.048'),
                    message: ncfST('Frontend.139'),
                    type: 'success'
                });
                //Legacy comment translated to English.
                that.$refs.uploadRef.clearFiles();
            } else {
                that.$notify.error({
                    title: ncfST('Frontend.049'),
                    message: ncfST('Frontend.132')
                });
            }
        },
        uploadError() {
            let that = this;
            that.$notify.error({
                title: ncfST('Frontend.049'),
                message: ncfST('Frontend.132')
            });
        },
        //Legacy comment translated to English.
        async configUploadSuccess(res, file, fileList) {
            const that = this;
            const ok = (res && (res.stateCode === 0 || res.success === true));
            const fileId = res && res.data != null ? res.data : null;
            if (ok && fileId != null) {
                const id = typeof fileId === 'number' ? fileId : parseInt(fileId, 10);
                if (!isNaN(id)) {
                    that.groupForm.files = that.groupForm.files || [];
                    if (!that.groupForm.files.some(item => Number(item.id) === id)) {
                        that.groupForm.files.push({ id: id, name: file.name || file.fileName || '' });
                    }
                }
                await that.getFileListData('file');
                that.$nextTick(() => {
                    const row = that.fileList.find(i => i.id === id);
                    if (row) that.toggleSelection(that.groupForm.files.map(f => that.fileList.find(i => i.id === f.id)).filter(Boolean));
                });
                that.$notify({ title: ncfST('Frontend.048'), message: ncfST('Frontend.143'), type: 'success' });
            } else {
                that.$notify.error({ title: ncfST('Frontend.049'), message: ncfST('Frontend.140') });
            }
            that.$nextTick(() => {
                if (that.$refs.configUploadRef) that.$refs.configUploadRef.clearFiles();
            });
        },
        configUploadError() {
            this.$notify.error({ title: ncfST('Frontend.049'), message: ncfST('Frontend.132') });
        },
        handleDialogFileClose() {
            this.configUploadFileList = [];
            if (this.$refs.configUploadRef) this.$refs.configUploadRef.clearFiles();
        },
        async getEmbeddingModelList() {
            //debugger
            let that = this
            let param = {
                page: that.page.page,
                size: that.page.modelCount,
            }
            await axios.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetSelectionListAsync', param)
                .then(res => {
                    that.embeddingModelData = res.data.data.data;
                })

        },
        async getVectorDBList() {
            let that = this
            let param = {
                page: that.page.page,
                size: that.page.modelCount,
            }
            await axios.post('/api/Senparc.Xncf.AIKernel/AIVectorAppService/Xncf.AIKernel_AIVectorAppService.GetSelectionListAsync', param)
                .then(res => {
                    const list = (res.data && res.data.data && res.data.data.data) || [];
                    //Legacy comment translated to English.
                    that.vectorDBData = (Array.isArray(list) ? list : []).filter(item => that.isSupportedVectorDb(item));
                    that.vectorDBDataAll = Array.isArray(list) ? list : [];
                })
        },
        isSupportedVectorDb(item) {
            if (!item) {
                return false;
            }
            var typeVal = item.vectorDBType != null ? item.vectorDBType : item.VectorDBType;
            var typeName = String(typeVal == null ? '' : typeVal).toLowerCase();
            var alias = String(item.alias || item.Alias || item.name || item.Name || '').toLowerCase();

            if (typeName === 'memory' || typeName === 'volatileinmemory' || typeName === '0') {
                return false;
            }
            if (alias === 'memory' || alias.indexOf('memory') >= 0) {
                return false;
            }
            if (typeName === 'redis' || typeName === 'qdrant') {
                return true;
            }
            if (alias.indexOf('redis') >= 0 || alias.indexOf('qdrant') >= 0) {
                return true;
            }
            if (typeof typeVal === 'number') {
                return typeVal !== 0;
            }
            return true;
        },
        resolveSaveResponse(res) {
            var body = res && res.data;
            if (!body) {
                return { ok: false, message: ncfST('Frontend.149') + ncfST('Frontend.148') };
            }
            var payload = body;
            if (body.data && typeof body.data === 'object' && !Array.isArray(body.data)) {
                if (Object.prototype.hasOwnProperty.call(body.data, 'success')
                    || Object.prototype.hasOwnProperty.call(body.data, 'Success')) {
                    payload = body.data;
                }
            }
            var success = payload.success === true || payload.Success === true;
            var message = payload.msg || payload.Msg || body.msg || body.Msg
                || body.exception || body.Exception
                || (success ? ncfST('Frontend.147') : ncfST('Frontend.148'));
            return { ok: success, message: String(message) };
        },
        async getChatModelList() {
            let that = this
            let param = {
                page: that.page.page,
                size: that.page.modelCount,
            }
            await axios.post('/api/Senparc.Xncf.AIKernel/AIModelAppService/Xncf.AIKernel_AIModelAppService.GetSelectionListAsync', param)
                .then(res => {
                    that.chatModelData = res.data.data.data;
                })
        },
        //Legacy comment translated to English.
        async getFileListData(listType, page) {
            const that = this
            if (listType === 'file') {
                if (page != null) that.fileQueryList.pageIndex = page
            }
            const queryList = {}
            if (listType === 'file') {
                Object.assign(queryList, that.fileQueryList)
            }
            await axios.get(`/api/Senparc.Xncf.FileManager/FileTemplateAppService/Xncf.FileManager_FileTemplateAppService.GetList?${getInterfaceQueryStr(queryList)}`)
                .then(res => {
                    const data = res?.data ?? {}
                    if (data.success) {
                        const payload = data?.data ?? {}
                        const fileData = payload.list ?? []
                        if (listType === 'file') {
                            that.$set(that, 'fileList', fileData)
                            that.fileListTotal = payload.totalCount ?? 0
                            that.$nextTick(() => {
                                if (that.visible.drawerGroup && that.groupForm.files.length > 0) {
                                    const filterList = fileData.filter(i => that.groupForm.files.some(item => item.id === i.id))
                                    that.toggleSelection(filterList)
                                }
                            })
                        }
                    } else {
                        app.$message({ message: '资料列表加载失败，请稍后重试。', type: 'error', duration: 5 * 1000 })
                    }
                }).catch((err) => {
                    that.$notify({ title: '错误', message: '资料列表加载失败，请稍后重试。', type: 'error', duration: 3000 })
                })
        },
        //Legacy comment translated to English.
        handleConfigFilePageChange(page) {
            this.fileQueryList.pageIndex = page
            this.getFileListData('file')
        },
        //Legacy comment translated to English.
        handleConfigFileSizeChange(size) {
            this.fileQueryList.pageSize = size
            this.fileQueryList.pageIndex = 1
            this.getFileListData('file')
        },
        //Legacy comment translated to English.
        async loadConfigKnowledgeBaseItems(knowledgeBaseId) {
            const that = this
            if (!knowledgeBaseId) {
                that.getFileListData('file')
                return
            }
            const url = '/api/Senparc.Xncf.KnowledgeBase/KnowledgeBaseItemAppService/Xncf.KnowledgeBase_KnowledgeBaseItemAppService.GetConfigurationByKnowledgeBaseId'
            try {
                const res = await axios.get(`${url}?knowledgeBaseId=${knowledgeBaseId}`)
                const data = res?.data ?? {}
                const list = (data.success && data.data) ? (Array.isArray(data.data) ? data.data : []) : []
                const inlineContent = list
                    .filter(i => i.contentType === 0)
                    .map(i => i.content)
                    .filter(Boolean)
                    .join('\n\n')
                that.groupForm.content = inlineContent || ''
                if (list.length === 0) {
                    that.groupForm.contentType = 2
                    that.groupForm.files = []
                    that.getFileListData('file')
                    return
                }
                const hasFileType = list.some(i => i.contentType === 100 || i.contentType === 200 || i.contentType === 300 || i.contentType === 400)
                if (hasFileType) {
                    const fileIds = [...new Set(list.filter(i => i.ncfFileId).map(i => Number(i.ncfFileId)))]
                    const fileNameById = new Map()
                    list.filter(i => i.ncfFileId).forEach(i => {
                        const id = Number(i.ncfFileId)
                        if (!fileNameById.has(id)) fileNameById.set(id, i.fileName || `File #${id}`)
                    })
                    that.groupForm.contentType = 2
                    that.groupForm.files = fileIds.map(id => ({ id: id, name: fileNameById.get(id) || `File #${id}` }))
                    await that.getFileListData('file')
                    that.$nextTick(() => {
                        const matched = that.fileList.filter(f => fileIds.includes(Number(f.id)))
                        that.toggleSelection(matched)
                        that.fileNamesToSelect = []
                    })
                } else {
                    that.groupForm.contentType = 1
                    that.groupForm.files = []
                    that.getFileListData('file')
                }
            } catch (e) {
                that.getFileListData('file')
            }
        },
        //Legacy comment translated to English.
        async getList() {
            let that = this
            //that.uid = resizeUrl().uid
            let { pageIndex, pageSize, keyword, orderField } = that.listQuery;
            if (orderField == '' || orderField == undefined) {
                orderField = 'AddTime Desc';
            }
            if (that.keyword != '' && that.keyword != undefined) {
                keyword = that.keyword;
            }
            const knowledgeBaseId = Number(that.navigationTarget?.knowledgeBaseId || 0)

            await service.get(`/Admin/KnowledgeBase/Index?handler=KnowledgeBases&pageIndex=${pageIndex}&pageSize=${pageSize}&keyword=${encodeURIComponent(keyword || '')}&orderField=${encodeURIComponent(orderField || '')}&knowledgeBaseId=${knowledgeBaseId > 0 ? knowledgeBaseId : ''}`).then(res => {//Legacy comment translated to English.
                that.filterTableHeader.embeddingModelId = res.data.data.list.map(z => ({
                    text: z.embeddingModelId,
                    value: z.embeddingModelId
                }));
                that.filterTableHeader.vectorDBId = res.data.data.list.map(z => ({
                    text: z.vectorDBId,
                    value: z.vectorDBId
                }));
                that.filterTableHeader.chatModelId = res.data.data.list.map(z => ({
                    text: z.chatModelId,
                    value: z.chatModelId
                }));
                that.filterTableHeader.name = res.data.data.list.map(z => ({
                    text: z.name,
                    value: z.name
                }));
                that.filterTableHeader.content = res.data.data.list.map(z => ({
                    text: z.content,
                    value: z.content
                }));

                that.tableData = res.data.data.list;
                that.paginationQuery.total = res.data.data.totalCount;
                that.applyKnowledgeBaseNavigation(that.tableData)
            });
        },
        async getCategoryList() {
            let that = this
            //Legacy comment translated to English.
            await service.get('/Admin/KnowledgeBase/Index?handler=KnowledgeBasesCategory').then(res => {
                that.categoryData = res.data.data.list;
            });
        },
        //Legacy comment translated to English.
        defaultRetrievalConfig() {
            return {
                chunkingStrategy: 'Fixed',
                chunkSize: 800,
                chunkOverlap: 120,
                rerankMode: 'None',
                rerankModelId: 0,
                rerankCandidateCount: 20,
                lexicalWeight: 0.35
            };
        },
        //Legacy comment translated to English.
        normalizeRetrievalConfig(raw) {
            const defaults = this.defaultRetrievalConfig();
            if (!raw || typeof raw !== 'object') {
                return defaults;
            }
            return {
                chunkingStrategy: raw.chunkingStrategy || defaults.chunkingStrategy,
                chunkSize: Number(raw.chunkSize) > 0 ? Number(raw.chunkSize) : defaults.chunkSize,
                chunkOverlap: Number(raw.chunkOverlap) >= 0 ? Number(raw.chunkOverlap) : defaults.chunkOverlap,
                rerankMode: raw.rerankMode || defaults.rerankMode,
                rerankModelId: Number(raw.rerankModelId) > 0 ? Number(raw.rerankModelId) : 0,
                rerankCandidateCount: Number(raw.rerankCandidateCount) > 0 ? Number(raw.rerankCandidateCount) : defaults.rerankCandidateCount,
                lexicalWeight: (raw.lexicalWeight == null) ? defaults.lexicalWeight : Number(raw.lexicalWeight)
            };
        },
        //Legacy comment translated to English.
        handleEdit(index, row, flag) {
            let that = this;
            that.dialog.visible = false;
            that.$nextTick(() => {
                that.dialog.visible = true;
            });

            if (flag === 'add') {
                that.dialog.title = ncfST('Frontend.135');
                that.dialog.data = {
                    id: 0,
                    embeddingModelId: null,
                    vectorDBId: null,
                    chatModelId: null,
                    name: '',
                    content: '',
                    retrievalConfig: that.defaultRetrievalConfig()
                };
                that.dialogImageUrl = '';
                return;
            }

            //Legacy comment translated to English.
            let { id, embeddingModelId, vectorDBId, chatModelId, name, content, retrievalConfig } = row;
            that.dialog.data = {
                id: id || 0,
                embeddingModelId: embeddingModelId ? parseInt(embeddingModelId, 10) : null,
                vectorDBId: vectorDBId ? parseInt(vectorDBId, 10) : null,
                chatModelId: chatModelId ? parseInt(chatModelId, 10) : null,
                name: name || '',
                content: content || '',
                retrievalConfig: that.normalizeRetrievalConfig(retrievalConfig)
            };

            if (flag === 'edit') {
                that.dialog.title = ncfST('Frontend.141');
            }
        },
        //Legacy comment translated to English.
        recursionFunc(row, source, dest) {
            if (row.chatModelId === null) {
                return;
            }
            for (let i in source) {
                let ele = source[i];
                if (row.chatModelId === ele.id) {
                    this.recursionFunc(ele, this.chatModelData, dest);
                    dest.push(ele.id);
                }
                else {
                    this.recursionFunc(row, ele.children, dest);
                }
            }
        },
        //Legacy comment translated to English.
        async saveSubmitFormData(saveType, serviceForm = {}) {
            const that = this;
            if (saveType === 'drawerGroup') {
                const contentType = serviceForm.contentType;
                const isFileType = contentType === 2; // 2=file

                if (isFileType) {
                    const serviceURL = '/api/Senparc.Xncf.KnowledgeBase/KnowledgeBaseAppService/Xncf.KnowledgeBase_KnowledgeBaseAppService.SyncFilesToKnowledgeBase';
                    const selectedFiles = serviceForm.files || [];
                    const fileIds = selectedFiles.map(f => (typeof f.id === 'number' ? f.id : parseInt(f.id, 10))).filter(id => !isNaN(id));

                    const requestData = {
                        knowledgeBaseId: parseInt(serviceForm.knowledgeBasesId, 10),
                        fileIds: fileIds
                    };

                    try {
                        const res = await service.post(serviceURL, requestData);
                        const success = res && (res.data === true || (res.data && (res.data.success === true || res.data.data === true)));
                        if (success) {
                            that.$notify({ title: ncfST('Frontend.048'), message: ncfST('Frontend.186'), type: 'success', duration: 3500 });
                            that.visible.drawerGroup = false;
                            that.getList();
                        } else {
                            that.$notify({ title: ncfST('Frontend.049'), message: ncfST('Frontend.144'), type: 'error', duration: 3000 });
                        }
                    } catch (err) {
                        that.$notify({ title: ncfST('Frontend.049'), message: ncfST('Frontend.144'), type: 'error', duration: 3000 });
                    }
                } else if (contentType === 1) {
                    const serviceURL = '/api/Senparc.Xncf.KnowledgeBase/KnowledgeBaseAppService/Xncf.KnowledgeBase_KnowledgeBaseAppService.SetKnowledgeBaseDetail';
                    const requestData = {
                        knowledgeBasesId: parseInt(serviceForm.knowledgeBasesId, 10),
                        contentType: 0,
                        content: serviceForm.content || ''
                    };
                    try {
                        const res = await service.post(serviceURL, requestData);
                        const success = res && (res.data === true || (res.data && (res.data.success === true || res.data.data === true)));
                        if (!success) throw new Error(ncfST('Frontend.148'));
                        that.$notify({ title: ncfST('Frontend.048'), message: ncfST('Frontend.143'), type: 'success', duration: 2000 });
                        that.visible.drawerGroup = false;
                        that.getList();
                    } catch (err) {
                        that.$notify({ title: ncfST('Frontend.049'), message: ncfST('Frontend.148'), type: 'error', duration: 3000 });
                    }
                } else {
                    that.$notify({
                        title: ncfST('Frontend.158'),
                        message: '外部数据采集需要单独配置抓取、权限与清洗策略，本版本不会伪造保存成功。',
                        type: 'warning',
                        duration: 4000
                    });
                }
            }
        },
        selectEmbeddingModel() {
            let that = this
            for (let i = 0; i < that.embeddingModelData.length; i++) {
                if (that.selectDefaultEmbeddingModel[0] == that.embeddingModelData[i].id) {
                    that.dialog.data.embeddingModelId = that.embeddingModelData[i].id;
                    //that.dialog.data.columnName = that.devicesData[i].name;
                }
            }
        },
        selectVectorDB() {
            let that = this
            for (let i = 0; i < that.vectorDBData.length; i++) {
                if (that.selectDefaultVectorDB[0] == that.vectorDBData[i].id) {
                    that.dialog.data.vectorDBId = that.vectorDBData[i].id;
                    //that.dialog.data.columnName = that.devicesData[i].name;
                }
            }
        },
        selectChatModel() {
            let that = this
            for (let i = 0; i < that.chatModelData.length; i++) {
                if (that.selectDefaultChatModel[0] == that.chatModelData[i].id) {
                    that.dialog.data.chatModelId = that.chatModelData[i].id;
                    //that.dialog.data.columnName = that.devicesData[i].name;
                }
            }
        },
        //Legacy comment translated to English.
        updateData() {
            let that = this;
            that.$refs['dataForm'].validate(valid => {
                if (!valid) {
                    return;
                }
                that.dialog.updateLoading = true;
                let data = {
                    id: that.dialog.data.id || 0,
                    embeddingModelId: parseInt(that.dialog.data.embeddingModelId, 10) || 0,
                    vectorDBId: parseInt(that.dialog.data.vectorDBId, 10) || 0,
                    chatModelId: parseInt(that.dialog.data.chatModelId, 10) || 0,
                    name: that.dialog.data.name,
                    content: that.dialog.data.content || '',
                    retrievalConfig: that.dialog.data.retrievalConfig || that.defaultRetrievalConfig(),
                    NcfFileIds: null
                };
                console.log('保存知识库数据：' + JSON.stringify(data));
                service.post("/Admin/KnowledgeBase/Edit?handler=Save", data).then(res => {
                    console.log('保存响应：', res);
                    const result = that.resolveSaveResponse(res);
                    if (result.ok) {
                        that.getList();
                        that.$notify({
                            title: "成功",
                            message: result.message || "知识库保存成功",
                            type: "success",
                            duration: 2000
                        });
                        that.dialog.visible = false;
                    } else {
                        that.$notify({
                            title: "失败",
                            message: result.message || "保存失败，请检查数据",
                            type: "error",
                            duration: 5000
                        });
                    }
                    that.dialog.updateLoading = false;
                }).catch(err => {
                    console.error('保存错误：', err);
                    const response = err && err.response && err.response.data;
                    const message = (response && (response.msg || response.Msg || response.message || response.Message || response.exception))
                        || (err && err.message)
                        || '保存出错，请稍后重试';
                    that.$notify({
                        title: "错误",
                        message: String(message),
                        type: "error",
                        duration: 5000
                    });
                    that.dialog.updateLoading = false;
                });
            });
        },
        // delete
        handleDelete(index, row) {
            let that = this
            let ids = [row.id];
            service.post("/Admin/KnowledgeBase/edit?handler=Delete", ids).then(res => {
                if (res.data.success) {
                    that.getList();
                    that.$notify({
                        title: "Success",
                        message: "删除成功",
                        type: "success",
                        duration: 2000
                    });
                }
            });
        },
        handleSelectionChange(val) {
            let that = this
            that.multipleSelection = val;
            if (that.multipleSelection.length == 1) {
                //that.newsId = that.multipleSelection[0].id;
                //that.dialogVote.data.newsId = that.multipleSelection[0].id;
            }
        },
        //Legacy comment translated to English.
        handleConfigFileSelectionChange(val) {
            const currentPageIds = new Set((this.fileList || []).map(r => Number(r.id)))
            const preserved = (this.groupForm.files || []).filter(f => !currentPageIds.has(Number(f.id)))
            const current = (val || []).map(r => ({
                id: r.id,
                name: r.fileName != null ? r.fileName : (r.name || '')
            }))
            const merged = [...preserved, ...current]
            this.groupForm.files = [...new Map(merged.map(f => [Number(f.id), f])).values()]
        },
        handleDbClick(row, column, event) {
            let that = this
            //that.multipleSelection = val;
            //if (that.multipleSelection.length == 1) {
            //  //that.newsId = that.multipleSelection[0].id;
            //  //that.dialogVote.data.newsId = that.multipleSelection[0].id;
            //}
            that.detailDialog.visible = true;
        },
        //Legacy comment translated to English.
        handleFilterChange(value, filterType) {
            if (filterType === 'groupAgent') {
                this.groupAgentQueryList.filter = value
                this.getAgentListData('groupAgent', 1)
            }
        },
        //Legacy comment translated to English.
        handleConfigFileSearch() {
            this.fileQueryList.pageIndex = 1
            this.getFileListData('file')
        },
        //Legacy comment translated to English.
        removeConfigFileSelection(row) {
            this.groupForm.files = (this.groupForm.files || []).filter(f => Number(f.id) !== Number(row.id))
            this.$nextTick(() => {
                const table = this.$refs.groupAgentTable
                if (table) table.toggleRowSelection(row, false)
            })
        },
        clearConfigFileSelection() {
            if (!(this.groupForm.files || []).length) {
                this.$message.info('当前没有已选择的资料')
                return
            }
            this.$confirm('仅取消当前知识库的待保存关联，不会删除文件。确认继续吗？', '清空选择', {
                confirmButtonText: '清空', cancelButtonText: '取消', type: 'warning'
            }).then(() => {
                this.groupForm.files = []
                this.$nextTick(() => this.$refs.groupAgentTable?.clearSelection())
            }).catch(() => {})
        },
        getCurrentRow(row) {
            let that = this
            //Legacy comment translated to English.
            //that.templateSelection = row;
            that.multipleSelection = row;
            //that.newsId = that.multipleSelection.id;
            //that.dialogVote.data.newsId = that.multipleSelection.id;
        },
        filterTag(value, row) {
            return row.tag === value;
        },
        filterHandler(value, row, column) {
            const property = column['property'];
            return row[property] === value;
        },
        handleSearch() {
            let that = this
            that.listQuery.pageIndex = 1;
            that.getList();
        },
        resetCondition() {
            let that = this
            that.keyword = '';
            that.listQuery.pageIndex = 1;
            that.getList();
        },
        setRecommendFormat(row, column, cellValue, index) {
            if (cellValue) {
                return "Y";
            }
            return "N";
        },
        setBodyFormat(row, column, cellValue, index) {
            if (cellValue == undefined) {
                return '-';
            }
            else {
                return cellValue.substring(0, 16);
            }
        },

        setContentFormat(row, column, cellValue, index) {
            if (cellValue == undefined) {
                return '-';
            }
            else {
                return cellValue.replace(/<[^>]+>/gim, '').replace(/\[(\w+)[^\]]*](.*?)\[\/\1]/g, '$2 ').substring(0, 16);
            }
        },
        handleClick() {

        },
        onSubmit() {
        },
        handleEmbeddingBtn(btnType, item) {
            const that = this;
            if (btnType === 'embedding') {
                this.$confirm(`确认对知识库 "${item.name}" 进行向量化处理吗？`, '向量化确认', {
                    confirmButtonText: '确定',
                    cancelButtonText: '取消',
                    type: 'warning'
                }).then(() => {
                    that.embeddingProgressPercent = 0;
                    that.embeddingProgressStatus = '';
                    that.embeddingProgressText = ncfST('Frontend.134');
                    that.embeddingTaskName = item && item.name ? item.name : ('知识库-' + (item && item.id ? item.id : ''));
                    that.embeddingDoneCount = 0;
                    that.embeddingTotalCount = 1;
                    that.embeddingPanelCollapsed = false;
                    that.embeddingStartedAt = new Date();
                    that.visible.embeddingResult = false;
                    that.visible.embeddingProgress = true;

                    var progressVal = 0;
                    that._embeddingTimer = setInterval(function () {
                        progressVal += Math.random() * 8 + 4;
                        if (progressVal > 90) progressVal = 90;
                        that.embeddingProgressPercent = Math.floor(progressVal);
                    that.embeddingProgressText = ncfST('Frontend.160') + ' ' + that.embeddingProgressPercent + '%';
                    }, 400);

                    const serviceURL = '/api/Senparc.Xncf.KnowledgeBase/KnowledgeBaseAppService/Xncf.KnowledgeBase_KnowledgeBaseAppService.EmbeddingKnowledgeBase';
                    const dataTemp = { id: item && item.id != null ? item.id : '' };

                    service.post(serviceURL, dataTemp).then(res => {
                        if (that._embeddingTimer) {
                            clearInterval(that._embeddingTimer);
                            that._embeddingTimer = null;
                        }
                        that.embeddingProgressPercent = 100;
                        that.embeddingProgressStatus = 'success';
                        that.embeddingDoneCount = 1;
                        that.embeddingProgressText = ncfST('Frontend.161');

                        var body = res && res.data;
                        var success = body && (body.success === true);
                        var resultMessage = (body && body.data != null) ? (typeof body.data === 'string' ? body.data : (body.data.data != null ? body.data.data : '')) : '';
                        if (success) {
                            setTimeout(function () {
                                that.showEmbeddingResultToast({
                                    isError: false,
                                    title: ncfST('Frontend.161'),
                                    taskName: that.embeddingTaskName,
                                    startedAt: that.embeddingStartedAt,
                                    message: resultMessage || ncfST('Frontend.162', item.name || '')
                                });
                            }, 500);
                        } else {
                            that.embeddingProgressStatus = 'exception';
                            that.embeddingProgressText = ncfST('Frontend.163');
                            setTimeout(function () {
                                that.showEmbeddingResultToast({
                                    isError: true,
                                    title: ncfST('Frontend.163'),
                                    taskName: that.embeddingTaskName,
                                    startedAt: that.embeddingStartedAt,
                                    total: 1,
                                    success: 0,
                                    fail: 1,
                                    detail: that.embeddingProgressText,
                                    message: that.embeddingProgressText
                                });
                            }, 800);
                        }
                    }).catch(err => {
                        if (that._embeddingTimer) {
                            clearInterval(that._embeddingTimer);
                            that._embeddingTimer = null;
                        }
                        that.embeddingProgressStatus = 'exception';
                        that.embeddingProgressPercent = Math.max(that.embeddingProgressPercent, 50);
                        that.embeddingProgressText = ncfST('Frontend.163');
                        setTimeout(function () {
                            that.showEmbeddingResultToast({
                                isError: true,
                                title: ncfST('Frontend.163'),
                                taskName: that.embeddingTaskName,
                                startedAt: that.embeddingStartedAt,
                                total: 1,
                                success: 0,
                                fail: 1,
                                detail: err.message || ncfST('Frontend.163'),
                                message: err.message || ncfST('Frontend.163')
                            });
                        }, 800);
                    });
                }).catch(function () {});
            }
        },
        //Legacy comment translated to English.
        handleElVisibleOpenBtn(btnType, item) {
            const that = this
            let visibleKey = btnType
            if (btnType === 'drawerGroup') {
                visibleKey = 'drawerGroup'
                that.groupForm = {
                    contentType: 2,
                    files: [],
                    content: '',
                    knowledgeBasesId: item?.id ?? ''
                }
                that.fileQueryList.pageIndex = 1
                that.fileNamesToSelect = []
                that.visible[visibleKey] = true
                that.$nextTick(() => that.$refs.groupAgentTable?.clearSelection())
                that.loadConfigKnowledgeBaseItems(item?.id)
                return
            }
            if (btnType === 'dialogFile') {
                visibleKey = 'dialogFile'
            }
            this.visible[visibleKey] = true
        },
        //Legacy comment translated to English.
        handleElVisibleClose(btnType) {
            // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
            this.$confirm('确认关闭？')
                .then(_ => {
                    let refName = '', formName = ''
                    // group
                    if (btnType === 'drawerGroup') {
                        refName = 'groupELForm'
                        formName = 'groupForm'
                        this.visible[btnType] = false

                        //Legacy comment translated to English.
                        //this.$set(this, 'groupAgentQueryList', this.$options.data().groupAgentQueryList)
                        //this.groupAgentList = []
                    }

                    if (formName) {
                        this.$set(this, `${formName}`, this.$options.data()[formName])
                        // Object.assign(this[formName],this.$options.data()[formName])
                    }
                    if (refName) {
                        this.$refs[refName].resetFields();
                    }
                    this.$nextTick(() => {
                        this.visible[btnType] = false
                    })
                    //Legacy comment translated to English.
                    if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
                        this.functionCallTags = []
                        this.functionCallInputVisible = false
                        this.functionCallInputValue = ''
                    }
                })
                .catch(_ => { });
        },
        //Legacy comment translated to English.
        handleElVisibleSubmit(btnType) {
            // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
            let refName = '', formName = ''
            // group
            if (btnType === 'drawerGroup') {
                refName = 'groupELForm'
                formName = 'groupForm'
            }
            if (!refName) return
            this.$refs[refName].validate((valid) => {
                if (valid) {
                    //debugger
                    const submitForm = this[formName] ?? {}
                    //Legacy comment translated to English.
                    this.saveSubmitFormData(btnType, submitForm)
                    //debugger
                    // this.visible[btnType] = false
                } else {
                    return false;
                }
            });
        },
        //Legacy comment translated to English.
        groupMembersCancel(item, index) {
            this.groupForm.files.splice(index, 1);
            this.$nextTick(() => {
                const tbl = this.$refs.groupAgentTable;
                if (!tbl) return;
                tbl.clearSelection();
                this.groupForm.files.forEach(f => {
                    const row = this.fileList.find(i => i.id === f.id);
                    if (row) tbl.toggleRowSelection(row, true);
                });
            });
        },
        //Legacy comment translated to English.
        async handleEditDrawerOpenBtn(btnType, item) {
            // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
            let formName = ''
            // agent
            if (['dialogGroupAgent'].includes(btnType)) {
                formName = 'agentForm'
            }

            if (formName) {
                if (btnType === 'drawerAgent' && item) {
                    //Legacy comment translated to English.
                    const formData = item.agentTemplateDto ? { ...item.agentTemplateDto } : { ...item };

                    //Legacy comment translated to English.
                    this.functionCallTags = formData.functionCallNames ? formData.functionCallNames.split(',').filter(Boolean) : [];

                    //Legacy comment translated to English.
                    Object.assign(this[formName], formData);

                } else if (btnType === 'drawerGroup') {
                    if (item.chatGroupDto) {
                        Object.assign(this[formName], {
                            ...item.chatGroupDto,
                            members: item.agentTemplateDtoList || item.chatGroupMembers || []
                        })
                    } else {
                        await serviceAM.post(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetChatGroupItem?id=${item.id}`)
                            .then(res => {
                                const data = res?.data ?? {}
                                if (data.success) {
                                    const groupDetail = data?.data ?? {}
                                    Object.assign(this[formName], {
                                        ...groupDetail.chatGroupDto,
                                        members: groupDetail.agentTemplateDtoList || groupDetail.chatGroupMembers || []
                                    })
                                }
                            })
                    }
                    //Legacy comment translated to English.
                    // this.getAgentListData('groupAgent')
                } else if (btnType === 'drawerTaskStart') {
                    Object.assign(this[formName], {
                        ...item
                        // groupName: item?.name ?? ''
                    })
                } else {
                    Object.assign(this[formName], item)
                }
                //Legacy comment translated to English.
                // this.$set(this, `${formName}`, deepClone(item))
                //Legacy comment translated to English.
                this.handleElVisibleOpenBtn(btnType)
            }
        },
        //Legacy comment translated to English.
        toggleSelection(rows) {
            if (rows) {
                rows.forEach(row => {
                    this.$refs?.groupAgentTable?.toggleRowSelection(row, true);
                });
            } else {
                this.$refs?.groupAgentTable?.clearSelection();
            }
        },
    }
});



/**

* Legacy comment translated to English.

* English documentation comment.

*/
function getInterfaceQueryStr(queryObj) {
    if (!queryObj) return ''
    //Legacy comment translated to English.
    return Object.entries(queryObj)
        .filter(([key, value]) => {
            //Legacy comment translated to English.
            if (typeof value === 'string') {
                return value !== ''
            } else if (typeof value === 'object' && value instanceof Array) {
                return value.length > 0
            } else if (typeof value === 'number') {
                return true
            } else {
                // if(typeof value === 'undefined')
                return false
            }
        })
        .map(
            ([key, value]) => {
                if (Array.isArray(value)) {
                    let str = ""
                    for (let index in value) {
                        str += `${index > 0 ? '&' : ''}${encodeURIComponent(key)}=${encodeURIComponent(value[index])}`
                    }
                    return str
                }
                return `${encodeURIComponent(key)}=${encodeURIComponent(value)}`
            }
        )
        .join('&')
}

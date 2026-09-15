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
            elSize: 'medium', // el 组件尺寸大小 默认为空  medium、small、mini
            // 显隐 visible
            visible: {
                drawerGroup: false, // 组 新增|编辑
                dialogFile: false,   // 配置抽屉内「新建文件」上传弹框
                embeddingProgress: false,  // 向量化进度弹窗
                embeddingResult: false,    // 向量化结果展示弹窗
                searchSettingsDrawer: false,
                paragraphDetailDialog: false,
            },
            // 召回测试
            selectedKnowledgeBaseId: null,
            recallContent: '',
            topK: 5,
            recallLoading: false,
            recallResults: [],
            lastElapsedMilliseconds: null,
            recordList: [],
            recordPage: 1,
            recordPageSize: 5,
            paragraphDetailItem: null,
            recallKbOptions: [], // 召回下拉用的全量知识库（独立于列表分页）
            embeddingProgressPercent: 0,
            embeddingProgressStatus: '',   // '' | success | exception
            embeddingProgressText: '正在准备...',
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
            configUploadFileList: [], // 新建文件弹框内的上传列表（仅展示用，关闭时清空）
            form:
            {
                content: ''
            },
            config:
            {
                initialFrameHeight: 500
            },
            //分页参数
            paginationQuery:
            {
                total: 5
            },
            //分页接口传参
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
                { title: "Embedding模型Id", istrue: false },
                { title: "向量数据库Id", istrue: false },
                { title: "对话模型Id", istrue: false },
                { title: "名称", istrue: true },
                { title: "内容", istrue: true },
            ],
            checkBoxGroup: [
                "Embedding模型Id", "向量数据库Id", "对话模型Id", "名称", "内容"
            ],
            checkedColumns: [],
            contentTypeData: [
                { value: 1, label: '输入' },
                { value: 2, label: '文件' },
                { value: 3, label: '采集外部数据' }
            ],
            keyword: '',
            multipleSelection: '',
            radio: '',
            props: { multiple: true },
            // 表格数据
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
                title: '添加知识库',
                visible: false,
                data:
                {
                    id: '', embeddingModelId: null, vectorDBId: null, chatModelId: null, name: '', content: ''
                },
                rules:
                {
                    name:
                        [
                            { required: true, message: "请输入知识库名称", trigger: "blur" }
                        ],
                    embeddingModelId:
                        [
                            { required: true, message: "请选择 Embedding 模型", trigger: "change" }
                        ],
                    vectorDBId:
                        [
                            { required: true, message: "请选择向量数据库", trigger: "change" }
                        ]
                },
                updateLoading: false,
                disabled: false,
                checkStrictly: true // 是否严格的遵守父子节点不互相关联
            },
            detailDialog:
            {
                title: '知识库管理详情',
                visible: false,
                data:
                {
                    id: '', embeddingModelId: null, vectorDBId: null, chatModelId: null, name: '', content: ''
                },
                rules:
                {
                    name:
                        [
                            { required: true, message: "知识库管理名称为必填项", trigger: "blur" }
                        ]
                },
                updateLoading: false,
                disabled: false,
                checkStrictly: true // 是否严格的遵守父子节点不互相关联
            },
            // 组 新增|编辑（内容类型：1=输入 2=文件 3=采集外部数据，默认文件）
            groupForm: {
                contentType: 2, // 内容类型，默认「文件」
                files: [], // 文件列表
                content: '', // 内容
                knowledgeBasesId: '' //知识库ID
            },
            groupFormRules: {
                name: [
                    { required: true, message: '请填写', trigger: 'blur' },
                ],
                members: [
                    { required: true, message: '请填写', trigger: 'change' },
                ],
                adminAgentTemplateId: [
                    { required: true, message: '请填写', trigger: 'change' },
                ],
                enterAgentTemplateId: [
                    { required: true, message: '请选择', trigger: 'change' },
                ],
                // description: [
                //     { required: true, message: '请填写', trigger: 'blur' },
                // ],
            },
            // 组 新增|编辑 智能体
            groupAgentQueryList: {
                pageIndex: 0,
                pageSize: 0,
                filter: '', // 筛选文本
                timeSort: false, // 默认降序
                proce: false, // 进行中
                stop: false, // 停用
                stand: false, // 待命
            },
            groupAgentList: [], // 组新增时的智能体列表
            // 配置页文件列表查询与分页
            fileQueryList: {
                pageIndex: 1,
                pageSize: 10,
                filter: '', // 文件名称筛选
            },
            fileListTotal: 0, // 文件列表总条数，用于分页
            fileList: [], // 配置页文件列表
            fileNamesToSelect: [], // 打开配置时待选中的文件名（从 KnowledgeBaseItem 回显用）
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
                    name: item.name || ('知识库-' + item.id),
                    ready: ready,
                    label: (item.name || ('知识库-' + item.id)) + (ready ? '' : '（尚未向量化）')
                };
            });
        },
        selectedKnowledgeBase() {
            return this.recallKnowledgeBaseList.find(item => Number(item.id) === Number(this.selectedKnowledgeBaseId)) || null;
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
        // 获取文件数据
        that.getFileListData('file');

        //TODO:初始化设置选中的字段
        that.checkedColumns = that.colData.filter(item => item.istrue).map(item => item.title);
    },
    watch:
    {
        'dialog.visible': function (val, old) {
            // 关闭dialog，清空
            if (!val) {
                this.dialog.data = {
                    id: '', embeddingModelId: null, vectorDBId: null, chatModelId: null, name: '', content: ''
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
                title: opts.title || (opts.isError ? '向量化任务执行失败!' : '向量化请求已经执行完毕!'),
                timeText: that.formatEmbeddingTime(opts.startedAt || that.embeddingStartedAt || new Date()),
                taskName: opts.taskName || that.embeddingTaskName || '知识库向量化',
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
                this.$message.error('无法获取知识库列表，请稍后重试');
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
                this.$confirm('确认删除此知识库管理吗？', '删除确认', {
                    confirmButtonText: '删除',
                    cancelButtonText: '取消',
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
                this.$message.warning('请先选择知识库');
                return;
            }
            if (!knowledgeBase.ready) {
                this.$message.warning('该知识库尚未向量化，请先执行“向量化”');
                return;
            }
            if (!content) {
                this.$message.warning('请输入要测试的问题');
                return;
            }

            this.recallLoading = true;
            this.recallResults = [];
            this.lastElapsedMilliseconds = null;
            try {
                const response = await service.post(recallUrl, {
                    id: Number(knowledgeBase.id),
                    content: content,
                    topK: this.topK
                });
                const body = response && response.data;
                const results = body && Object.prototype.hasOwnProperty.call(body, 'data') ? body.data : body;
                if (!Array.isArray(results)) throw new Error('服务未返回有效的召回结果');

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
                this.$message.success(this.recallResults.length ? '召回测试完成，请核对来源与内容。' : '测试完成，但没有返回匹配片段。');
            } catch (error) {
                const response = error && error.response && error.response.data;
                const msg = (response && (response.errorMessage || response.message || response.title)) || (error && error.message) || '请求未完成，请检查知识库和模型配置。';
                this.$notify({ title: '召回未完成', message: msg, type: 'error', duration: 6000 });
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
            // 上传成功
            that.fileList = fileList;
            //debugger
            if (res.stateCode == 0) {
                that.$notify({
                    title: '成功',
                    message: '恭喜你，上传成功',
                    type: 'success'
                });
                // 清空文件列表（关键）
                that.$refs.uploadRef.clearFiles();
            } else {
                that.$notify.error({
                    title: '失败',
                    message: '上传失败，请重新上传'
                });
            }
        },
        uploadError() {
            let that = this;
            that.$notify.error({
                title: '失败',
                message: '上传失败，请重新上传'
            });
        },
        // 配置抽屉「新建文件」弹框：上传成功后将文件写入 FileManage，并加入当前已选列表、刷新文件列表并勾选新文件
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
                that.$notify({ title: '成功', message: '资料已上传并勾选，请点击“确认”保存关联', type: 'success' });
            } else {
                that.$notify.error({ title: '失败', message: '上传失败，请重试' });
            }
            that.$nextTick(() => {
                if (that.$refs.configUploadRef) that.$refs.configUploadRef.clearFiles();
            });
        },
        configUploadError() {
            this.$notify.error({ title: '失败', message: '上传失败，请重新上传' });
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
                    // KnowledgeBase 仅支持可持久化的 Redis / Qdrant；Memory 等内存库后端会直接拒绝
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
                return { ok: false, message: '保存失败：未收到服务端响应' };
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
                || (success ? '知识库保存成功' : '保存失败，请检查数据');
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
        // 获取 文件 数据（支持分页与筛选）
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
        // 配置页文件列表：页码变化（首页/上一页/下一页/尾页/跳转）
        handleConfigFilePageChange(page) {
            this.fileQueryList.pageIndex = page
            this.getFileListData('file')
        },
        // 配置页文件列表：每页条数变化
        handleConfigFileSizeChange(size) {
            this.fileQueryList.pageSize = size
            this.fileQueryList.pageIndex = 1
            this.getFileListData('file')
        },
        // 打开配置时：根据当前知识库拉取 KnowledgeBaseItem 关联项，回填文件选中或内容
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
        // 获取列表
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

            await service.get(`/Admin/KnowledgeBase/Index?handler=KnowledgeBases&pageIndex=${pageIndex}&pageSize=${pageSize}&keyword=${encodeURIComponent(keyword || '')}&orderField=${encodeURIComponent(orderField || '')}&knowledgeBaseId=${knowledgeBaseId > 0 ? knowledgeBaseId : ''}`).then(res => {// 使用 map 转换为目标格式的对象数组
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
            //获取分类列表数据
            await service.get('/Admin/KnowledgeBase/Index?handler=KnowledgeBasesCategory').then(res => {
                that.categoryData = res.data.data.list;
            });
        },
        // 编辑 // 新增知识库（下拉直接绑定 dialog.data.*Id）
        handleEdit(index, row, flag) {
            let that = this;
            that.dialog.visible = false;
            that.$nextTick(() => {
                that.dialog.visible = true;
            });

            if (flag === 'add') {
                that.dialog.title = '添加知识库';
                that.dialog.data = {
                    id: 0,
                    embeddingModelId: null,
                    vectorDBId: null,
                    chatModelId: null,
                    name: '',
                    content: ''
                };
                that.dialogImageUrl = '';
                return;
            }

            // 编辑 - 使用现有数据
            let { id, embeddingModelId, vectorDBId, chatModelId, name, content } = row;
            that.dialog.data = {
                id: id || 0,
                embeddingModelId: embeddingModelId ? parseInt(embeddingModelId, 10) : null,
                vectorDBId: vectorDBId ? parseInt(vectorDBId, 10) : null,
                chatModelId: chatModelId ? parseInt(chatModelId, 10) : null,
                name: name || '',
                content: content || ''
            };

            if (flag === 'edit') {
                that.dialog.title = '编辑知识库';
            }
        },
        // 设置父级菜单默认显示 递归
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
        // 保存 submitForm 数据（配置抽屉确认：内容类型为文件时导入选中文件，否则不校验文件）
        async saveSubmitFormData(saveType, serviceForm = {}) {
            const that = this;
            if (saveType === 'drawerGroup') {
                const contentType = serviceForm.contentType;
                const isFileType = contentType === 2; // 2=文件

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
                            that.$notify({ title: '成功', message: `已保存 ${fileIds.length} 份资料关联；请点击“向量化”使召回结果更新。`, type: 'success', duration: 3500 });
                            that.visible.drawerGroup = false;
                            that.getList();
                        } else {
                            that.$notify({ title: '失败', message: '文件导入失败，请稍后重试。', type: 'error', duration: 3000 });
                        }
                    } catch (err) {
                        that.$notify({ title: '错误', message: '文件导入失败，请稍后重试。', type: 'error', duration: 3000 });
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
                        if (!success) throw new Error('内容保存失败');
                        that.$notify({ title: '成功', message: '知识库文本内容已同步', type: 'success', duration: 2000 });
                        that.visible.drawerGroup = false;
                        that.getList();
                    } catch (err) {
                        that.$notify({ title: '错误', message: '内容保存失败，请稍后重试。', type: 'error', duration: 3000 });
                    }
                } else {
                    that.$notify({
                        title: '尚未支持',
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
        // 更新新增、编辑（文件改为在「配置」中上传并关联，此处不再传文件）
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
        // 删除
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
        // 配置抽屉内「文件列表」表格勾选变化：同步到 groupForm.files，保存时用于关联知识库
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
        // 筛选输入变化
        handleFilterChange(value, filterType) {
            if (filterType === 'groupAgent') {
                this.groupAgentQueryList.filter = value
                this.getAgentListData('groupAgent', 1)
            }
        },
        // 配置页文件列表：按文件名称搜索（调用接口过滤，并回到第一页）
        handleConfigFileSearch() {
            this.fileQueryList.pageIndex = 1
            this.getFileListData('file')
        },
        // 配置抽屉只管理“关联”。删除文件会影响其他知识库和物理资源，必须回文件资源管理页单独执行。
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
            //获取选中数据
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
                    that.embeddingProgressText = '正在准备...';
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
                        that.embeddingProgressText = '正在向量化处理中... ' + that.embeddingProgressPercent + '%';
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
                        that.embeddingProgressText = '向量化完成';

                        var body = res && res.data;
                        var success = body && (body.success === true);
                        var resultMessage = (body && body.data != null) ? (typeof body.data === 'string' ? body.data : (body.data.data != null ? body.data.data : '')) : '';
                        if (success) {
                            setTimeout(function () {
                                that.showEmbeddingResultToast({
                                    isError: false,
                                    title: '向量化请求已经执行完毕!',
                                    taskName: that.embeddingTaskName,
                                    startedAt: that.embeddingStartedAt,
                                    message: resultMessage || ('知识库「' + (item.name || '') + '」向量化已完成。')
                                });
                            }, 500);
                        } else {
                            that.embeddingProgressStatus = 'exception';
                            that.embeddingProgressText = '向量化失败，请稍后重试。';
                            setTimeout(function () {
                                that.showEmbeddingResultToast({
                                    isError: true,
                                    title: '向量化任务执行失败!',
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
                        that.embeddingProgressText = '处理出错，请稍后重试。';
                        setTimeout(function () {
                            that.showEmbeddingResultToast({
                                isError: true,
                                title: '向量化任务执行失败!',
                                taskName: that.embeddingTaskName,
                                startedAt: that.embeddingStartedAt,
                                total: 1,
                                success: 0,
                                fail: 1,
                                detail: err.message || '向量化处理出错，请检查配置',
                                message: err.message || '向量化处理出错，请检查配置'
                            });
                        }, 800);
                    });
                }).catch(function () {});
            }
        },
        // Dailog|抽屉 打开 按钮
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
        // Dailog|抽屉 关闭 按钮
        handleElVisibleClose(btnType) {
            // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
            this.$confirm('确认关闭？')
                .then(_ => {
                    let refName = '', formName = ''
                    // 组
                    if (btnType === 'drawerGroup') {
                        refName = 'groupELForm'
                        formName = 'groupForm'
                        this.visible[btnType] = false

                        //// 重置 组获取智能体query
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
                    // 清理 Function Calls 数据
                    if (['drawerAgent', 'dialogGroupAgent'].includes(btnType)) {
                        this.functionCallTags = []
                        this.functionCallInputVisible = false
                        this.functionCallInputValue = ''
                    }
                })
                .catch(_ => { });
        },
        // Dailog|抽屉 提交 按钮
        handleElVisibleSubmit(btnType) {
            // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
            let refName = '', formName = ''
            // 组
            if (btnType === 'drawerGroup') {
                refName = 'groupELForm'
                formName = 'groupForm'
            }
            if (!refName) return
            this.$refs[refName].validate((valid) => {
                if (valid) {
                    //debugger
                    const submitForm = this[formName] ?? {}
                    //提交数据给后端
                    this.saveSubmitFormData(btnType, submitForm)
                    //debugger
                    // this.visible[btnType] = false
                } else {
                    return false;
                }
            });
        },
        // 配置抽屉「已选择」文件列表中移除某一项，并同步表格勾选
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
        // 编辑 Dailog|抽屉 按钮 
        async handleEditDrawerOpenBtn(btnType, item) {
            // drawerAgent dialogGroupAgent drawerGroup drawerGroupStart
            let formName = ''
            // 智能体
            if (['dialogGroupAgent'].includes(btnType)) {
                formName = 'agentForm'
            }

            if (formName) {
                if (btnType === 'drawerAgent' && item) {
                    // 创建一个新的对象来存储表单数据
                    const formData = item.agentTemplateDto ? { ...item.agentTemplateDto } : { ...item };

                    // 确保 functionCallNames 被正确初始化
                    this.functionCallTags = formData.functionCallNames ? formData.functionCallNames.split(',').filter(Boolean) : [];

                    // 将数据赋值给表单
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
                    // // 获取 全部智能体数据
                    // this.getAgentListData('groupAgent')
                } else if (btnType === 'drawerTaskStart') {
                    Object.assign(this[formName], {
                        ...item
                        // groupName: item?.name ?? ''
                    })
                } else {
                    Object.assign(this[formName], item)
                }
                // 回显 表单值
                // this.$set(this, `${formName}`, deepClone(item))
                // 打开 抽屉
                this.handleElVisibleOpenBtn(btnType)
            }
        },
        // 组 新增|编辑 智能体table 切换table 选中
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
* 处理接口 query 参数 转换为 string
* @param {Object} queryObj // 原地址
*/
function getInterfaceQueryStr(queryObj) {
    if (!queryObj) return ''
    // 将对象转换为 URL 参数字符串
    return Object.entries(queryObj)
        .filter(([key, value]) => {
            // 过滤掉空值
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

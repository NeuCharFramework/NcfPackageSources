(function () {
    const pageUrl = window.location.pathname || '/Admin/FileManager/Index';
    const maxFilesPerUpload = 20;
    const maxTotalUploadBytes = 100 * 1024 * 1024;
    const maxFileSizeBytes = 50 * 1024 * 1024;

    function unwrap(response) {
        return response && response.data && response.data.data !== undefined ? response.data.data : response.data;
    }

    function errorMessage(error) {
        const data = error && error.response && error.response.data;
        const message = typeof data === 'string' ? data : (data && (data.errorMessage || data.message || data.title));
        if (message) return message;
        if (error && error.response && error.response.status === 400) {
            return '请求被服务器拒绝，请刷新页面后重试。';
        }
        return (error && error.message) || '请求失败';
    }

    function getRequestVerificationToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!tokenInput || !tokenInput.value) {
            throw new Error('页面防伪令牌缺失，请刷新页面后重试。');
        }
        return tokenInput.value;
    }

    function post(url, data, config) {
        const requestConfig = config || {};
        requestConfig.headers = Object.assign({}, requestConfig.headers, {
            RequestVerificationToken: getRequestVerificationToken(),
            'x-requested-with': 'XMLHttpRequest'
        });
        return axios.post(url, data, requestConfig);
    }

    new Vue({
        el: '#app',
        data: function () {
            return {
                tableData: [],
                tableLoading: false,
                page: { page: 1, size: 10 },
                total: 0,
                resourceScope: 100,
                currentFolderId: null,
                folderPath: [],
                folderTree: [],
                folderTreeKey: 0,
                folderTreeProps: {
                    label: 'name',
                    children: 'children',
                    isLeaf: function (data) { return data && data.hasChildren === false; }
                },
                uploadDialog: { visible: false, fileList: [], uploading: false, progress: 0, mode: 'files', folderRootName: '' },
                folderDialog: { visible: false, loading: false, editing: false, form: { id: null, name: '', description: '' } },
                noteDialog: { visible: false, loading: false, row: null, note: '' },
                guideDialogVisible: false,
                treeFilter: '',
                fileSearchKeyword: '',
                orgName: '山西米立信息技术有限公司',
                rootFolderName: '企业文档',
                activeNavKey: 'enterprise',
                navItems: [
                    { key: 'home', label: '首页', icon: 'el-icon-s-home' },
                    { key: 'favorite', label: '收藏', icon: 'el-icon-star-off' },
                    { key: 'enterprise', label: '企业文档', icon: 'el-icon-folder', scope: 100 },
                    { key: 'team', label: '团队文档', icon: 'el-icon-user' },
                    { key: 'group', label: '内部群文档', icon: 'el-icon-chat-dot-round' },
                    { key: 'mine', label: '我的文档', icon: 'el-icon-document' },
                    { key: 'shared', label: '共享文档', icon: 'el-icon-share' },
                    { key: 'project', label: '项目文档', icon: 'el-icon-files', scope: 200 },
                    { key: 'recycle', label: '回收站', icon: 'el-icon-delete' },
                    { key: 'divider', label: '', divider: true },
                    { key: 'dashboard', label: '数据面板', icon: 'el-icon-data-line' },
                    { key: 'tags', label: '标签管理', icon: 'el-icon-price-tag' },
                    { key: 'settings', label: '设置', icon: 'el-icon-setting' }
                ]
            };
        },
        watch: {
            treeFilter: function (value) {
                if (this.$refs.folderTree) this.$refs.folderTree.filter(value);
            }
        },
        computed: {
            isSiteAsset: function () { return this.resourceScope === 200; },
            resourceScopeName: function () { return this.isSiteAsset ? '站点静态资源' : '知识库资料'; },
            canGoParent: function () { return this.currentFolderId != null; },
            displayTableData: function () {
                const keyword = (this.fileSearchKeyword || '').trim().toLowerCase();
                if (!keyword) return this.tableData;
                return this.tableData.filter(function (row) {
                    return (row.fileName || '').toLowerCase().indexOf(keyword) !== -1
                        || (row.description || '').toLowerCase().indexOf(keyword) !== -1;
                });
            },
            scopeHint: function () {
                return this.isSiteAsset
                    ? '站点静态资源默认私有；公开后会生成带 SHA-256 指纹的 /assets/ URL。为防止同源脚本注入，不接受 HTML、SVG、JavaScript 或压缩包。'
                    : '知识库源文件只能是可安全提取的文本和 Office Open XML 文件。它们不会生成公开 URL，也不能在此处发布。';
            },
            uploadExtensionHint: function () {
                return this.isSiteAsset
                    ? '支持 JPG、PNG、GIF、WebP、AVIF、ICO、音视频和字体格式。'
                    : '支持文本、JSON/XML/YAML、代码、DOCX、XLSX、PPTX。';
            },
            uploadTargetText: function () {
                const currentFolder = this.folderPath.length ? this.folderPath[this.folderPath.length - 1].name : this.rootFolderName;
                return this.resourceScopeName + ' / ' + currentFolder;
            },
            uploadFolderSummary: function () {
                if (this.uploadDialog.mode !== 'folder' || !this.uploadDialog.fileList.length) return '';
                return '已选择文件夹“' + this.uploadDialog.folderRootName + '”，共 ' + this.uploadDialog.fileList.length + ' 个文件；上传后会在左侧文件树中还原原始目录结构。';
            }
        },
        created: function () {
            this.restoreRouteState();
            this.syncActiveNavFromScope();
            this.enterFolder(this.currentFolderId);
        },
        methods: {
            syncActiveNavFromScope: function () {
                this.activeNavKey = this.resourceScope === 200 ? 'project' : 'enterprise';
            },
            onNavClick: function (item) {
                if (!item || item.divider) return;
                if (item.key === 'settings') {
                    this.guideDialogVisible = true;
                    return;
                }
                if (item.scope === 100 || item.scope === 200) {
                    if (this.resourceScope === item.scope) {
                        this.activeNavKey = item.key;
                        return;
                    }
                    this.resourceScope = item.scope;
                    this.activeNavKey = item.key;
                    this.changeResourceScope();
                    return;
                }
                this.activeNavKey = item.key;
                this.$message.info('「' + item.label + '」功能即将开放');
            },
            restoreRouteState: function () {
                const query = new URLSearchParams(window.location.search);
                const scope = Number(query.get('scope'));
                const folderId = Number(query.get('folderId'));
                if (scope === 100 || scope === 200) this.resourceScope = scope;
                this.currentFolderId = Number.isInteger(folderId) && folderId > 0 ? folderId : null;
            },
            syncRouteState: function () {
                const url = new URL(window.location.href);
                url.searchParams.set('scope', String(this.resourceScope));
                if (this.currentFolderId == null) url.searchParams.delete('folderId');
                else url.searchParams.set('folderId', String(this.currentFolderId));
                window.history.replaceState({}, '', url.toString());
            },
            dateFormatter: function (date) { return date ? new Date(date).toLocaleString() : ''; },
            formatFileSize: function (size) {
                const units = ['B', 'KB', 'MB', 'GB'];
                let index = 0;
                let value = Number(size || 0);
                while (value >= 1024 && index < units.length - 1) { value /= 1024; index++; }
                return value.toFixed(index === 0 ? 0 : 2) + ' ' + units[index];
            },
            fileTypeLabel: function (fileType) {
                return ({ 0: '文本', 1: 'Word', 2: 'PowerPoint', 3: 'Excel', 4: '代码', 999: '其他' })[fileType] || '其他';
            },
            updatedByLabel: function (row) { return '-'; },
            filterFolderNode: function (value, data) {
                if (!value) return true;
                return (data.name || '').toLowerCase().indexOf(value.toLowerCase()) !== -1;
            },
            handleCreateCommand: function (command) {
                if (command === 'folder') this.showCreateFolderDialog();
            },
            handleUploadCommand: function (command) {
                if (command === 'files') {
                    this.showUploadDialog();
                    return;
                }
                if (command === 'folder') {
                    this.showUploadDialog();
                    this.$nextTick(function () { this.chooseUploadFolder(); }.bind(this));
                }
            },
            handleRowCommand: function (command, row) {
                if (command === 'download') return this.downloadFile(row);
                if (command === 'togglePublish') return this.setPublication(row, row.accessLevel !== 100);
                if (command === 'copyUrl') return this.copyPublicUrl(row);
                if (command === 'editNote') return this.showNoteDialog(row);
                if (command === 'delete') return this.deleteFile(row);
            },
            enterParentFolder: function () {
                if (this.folderPath.length > 1) {
                    this.enterFolder(this.folderPath[this.folderPath.length - 2].id);
                    return;
                }
                this.enterFolder(null);
            },
            showNoteDialog: function (row) {
                this.noteDialog = { visible: true, loading: false, row: row, note: row.description || '' };
            },
            submitNote: async function () {
                const row = this.noteDialog.row;
                if (!row) return;
                this.noteDialog.loading = true;
                try {
                    row.description = this.noteDialog.note || '';
                    await this.handleNoteChange(row);
                    this.noteDialog.visible = false;
                } finally {
                    this.noteDialog.loading = false;
                }
            },
            getList: async function () {
                this.tableLoading = true;
                try {
                    let url = pageUrl + '?handler=List&page=' + this.page.page + '&pageSize=' + this.page.size + '&resourceScope=' + this.resourceScope;
                    if (this.currentFolderId != null) url += '&folderId=' + encodeURIComponent(this.currentFolderId);
                    const result = unwrap(await axios.get(url));
                    this.tableData = result && (result.data || result.items) || [];
                    this.total = result && (result.totalCount || result.total) || 0;
                } catch (error) {
                    this.$message.error('获取文件列表失败：' + errorMessage(error));
                } finally {
                    this.tableLoading = false;
                }
            },
            loadFolderChildren: async function (node, resolve) {
                try {
                    const parentId = node.level === 0 ? null : node.data.id;
                    let url = pageUrl + '?handler=Folders&resourceScope=' + this.resourceScope;
                    if (parentId != null) url += '&parentId=' + encodeURIComponent(parentId);
                    const list = unwrap(await axios.get(url)) || [];
                    resolve(list.map(function (folder) {
                        return { id: folder.id, name: folder.name, description: folder.description || '', parentId: folder.parentId, hasChildren: true };
                    }));
                } catch (error) {
                    this.$message.error('获取文件夹失败：' + errorMessage(error));
                    resolve([]);
                }
            },
            changeResourceScope: async function () {
                this.currentFolderId = null;
                this.folderPath = [];
                this.page.page = 1;
                this.treeFilter = '';
                this.fileSearchKeyword = '';
                this.syncActiveNavFromScope();
                this.reloadFolderTree();
                await this.enterFolder(null);
            },
            onFolderNodeClick: function (data) { this.enterFolder(data.id); },
            enterFolder: async function (id) {
                this.currentFolderId = id == null ? null : Number(id);
                this.page.page = 1;
                if (this.currentFolderId != null) {
                    try {
                        await this.loadFolderPath();
                    } catch (error) {
                        this.currentFolderId = null;
                        this.folderPath = [];
                        this.$message.warning('目标文件夹不可用，已回到根目录。');
                    }
                } else {
                    this.folderPath = [];
                }
                await this.getList();
                this.syncRouteState();
            },
            loadFolderPath: async function () {
                if (this.currentFolderId == null) {
                    this.folderPath = [];
                    return;
                }
                const url = pageUrl + '?handler=FolderPath&folderId=' + encodeURIComponent(this.currentFolderId) + '&resourceScope=' + this.resourceScope;
                const folders = unwrap(await axios.get(url));
                if (!Array.isArray(folders) || folders.length === 0) throw new Error('Folder path not found');
                this.folderPath = folders.map(function (folder) { return { id: folder.id, name: folder.name }; });
            },
            reloadFolderTree: function () {
                this.folderTree = [];
                this.folderTreeKey += 1;
            },
            handleCurrentChange: function (value) { this.page.page = value; this.getList(); },
            handleSizeChange: function (value) { this.page.size = value; this.page.page = 1; this.getList(); },
            handleNoteChange: async function (row) {
                try {
                    await post(pageUrl + '?handler=EditNote', { id: row.id, note: row.description || '' });
                    this.$message.success('备注已更新');
                } catch (error) {
                    this.$message.error('更新备注失败：' + errorMessage(error));
                }
            },
            downloadFile: function (row) { window.location.assign(pageUrl + '?handler=Download&id=' + encodeURIComponent(row.id)); },
            deleteFile: async function (row) {
                try {
                    const referenceHint = this.isSiteAsset ? '' : '；若它仍被知识库关联，系统会阻止删除';
                    await this.$confirm('删除后无法恢复' + referenceHint + '，确认删除“' + row.fileName + '”吗？', '确认删除', { type: 'warning' });
                    await post(pageUrl + '?handler=Delete&id=' + encodeURIComponent(row.id));
                    this.$message.success('已删除');
                    await this.getList();
                } catch (error) {
                    if (error !== 'cancel') this.$message.error('删除失败：' + errorMessage(error));
                }
            },
            setPublication: async function (row, publish) {
                try {
                    await post(pageUrl + '?handler=SetSiteAssetPublication', { id: row.id, publish: publish });
                    this.$message.success(publish ? '资源已公开' : '资源已设为私有');
                    await this.getList();
                } catch (error) {
                    this.$message.error('更新发布状态失败：' + errorMessage(error));
                }
            },
            copyPublicUrl: async function (row) {
                const url = window.location.origin + row.publicUrl;
                try {
                    await navigator.clipboard.writeText(url);
                    this.$message.success('公开 URL 已复制');
                } catch (_) {
                    this.$prompt('请复制以下公开 URL', '公开 URL', { inputValue: url, inputType: 'textarea' });
                }
            },
            resetUploadDialog: function (visible) {
                this.uploadDialog = { visible: visible, fileList: [], uploading: false, progress: 0, mode: 'files', folderRootName: '' };
                if (this.$refs.upload) this.$refs.upload.clearFiles();
            },
            showUploadDialog: function () { this.resetUploadDialog(true); },
            handleFileChange: function (file, fileList) {
                if (file.description === undefined) this.$set(file, 'description', '');
                this.uploadDialog.fileList = fileList;
                this.uploadDialog.mode = 'files';
                this.uploadDialog.folderRootName = '';
            },
            chooseUploadFolder: function () {
                const input = this.$refs.folderUploadInput;
                if (!input || !('webkitdirectory' in input)) {
                    this.$message.warning('当前浏览器不支持选择文件夹，请使用最新版 Chrome、Edge 或 Safari。');
                    return;
                }
                input.click();
            },
            handleFolderSelection: function (event) {
                const rawFiles = Array.prototype.slice.call((event.target && event.target.files) || []);
                if (!rawFiles.length) return;

                const paths = rawFiles.map(function (file) { return file.webkitRelativePath || ''; });
                const rootNames = Array.from(new Set(paths.map(function (path) { return path.split('/')[0]; }).filter(Boolean)));
                if (paths.some(function (path) { return !path; }) || rootNames.length !== 1) {
                    this.$message.error('浏览器未提供完整的文件夹相对路径，请重新选择一个文件夹。');
                    return;
                }

                if (this.$refs.upload) this.$refs.upload.clearFiles();
                this.uploadDialog.fileList = rawFiles.map(function (rawFile, index) {
                    return {
                        uid: 'folder-' + Date.now() + '-' + index,
                        name: rawFile.name,
                        size: rawFile.size,
                        status: 'ready',
                        raw: rawFile,
                        relativePath: rawFile.webkitRelativePath,
                        description: ''
                    };
                });
                this.uploadDialog.mode = 'folder';
                this.uploadDialog.folderRootName = rootNames[0];
                event.target.value = '';
            },
            beforeUpload: function () { return false; },
            createUploadBatches: function (fileList) {
                const batches = [];
                let batch = [];
                let batchBytes = 0;
                fileList.forEach(function (file) {
                    const rawFile = file.raw || file;
                    if (!rawFile || !rawFile.size) throw new Error('存在无法读取的文件，请重新选择。');
                    if (rawFile.size > maxFileSizeBytes) throw new Error('文件“' + file.name + '”超过 50 MB，无法上传。');
                    if (batch.length && (batch.length >= maxFilesPerUpload || batchBytes + rawFile.size > maxTotalUploadBytes)) {
                        batches.push(batch);
                        batch = [];
                        batchBytes = 0;
                    }
                    batch.push(file);
                    batchBytes += rawFile.size;
                });
                if (batch.length) batches.push(batch);
                return batches;
            },
            submitUpload: async function () {
                if (!this.uploadDialog.fileList.length) { this.$message.warning('请选择要上传的文件'); return; }
                this.uploadDialog.uploading = true;
                this.uploadDialog.progress = 0;
                let uploadedCount = 0;
                try {
                    const files = this.uploadDialog.fileList.slice();
                    const batches = this.createUploadBatches(files);
                    for (let batchIndex = 0; batchIndex < batches.length; batchIndex++) {
                        const formData = new FormData();
                        batches[batchIndex].forEach(function (file) {
                            const rawFile = file.raw || file;
                            formData.append('files', rawFile);
                            formData.append('descriptions', file.description || '');
                            formData.append('relativePaths', file.relativePath || rawFile.webkitRelativePath || '');
                        });
                        formData.append('resourceScope', this.resourceScope);
                        if (this.currentFolderId != null) formData.append('folderId', this.currentFolderId);
                        // 同时放入表单和请求头，兼容 Razor Pages 的 multipart 防伪验证。
                        formData.append('__RequestVerificationToken', getRequestVerificationToken());
                        await post(pageUrl + '?handler=Upload', formData);
                        uploadedCount += batches[batchIndex].length;
                        this.uploadDialog.progress = Math.round(uploadedCount * 100 / files.length);
                    }
                    this.$message.success(this.uploadDialog.mode === 'folder'
                        ? '文件夹上传完成，已保留原始目录结构。'
                        : '上传成功；站点静态资源仍需显式公开后才可被站点引用。');
                    this.uploadDialog.visible = false;
                    this.reloadFolderTree();
                    await this.getList();
                } catch (error) {
                    const partialMessage = uploadedCount > 0 ? '，已完成 ' + uploadedCount + ' 个文件' : '';
                    this.$message.error('上传失败' + partialMessage + '：' + errorMessage(error));
                } finally {
                    this.uploadDialog.uploading = false;
                }
            },
            cancelUpload: function () { this.resetUploadDialog(false); },
            showCreateFolderDialog: function () {
                this.folderDialog = { visible: true, loading: false, editing: false, form: { id: null, name: '', description: '' } };
            },
            showEditFolderDialog: function (folder) {
                this.folderDialog = { visible: true, loading: false, editing: true, form: { id: folder.id, name: folder.name, description: folder.description || '' } };
            },
            submitFolder: async function () {
                const form = this.folderDialog.form;
                if (!form.name || !form.name.trim()) { this.$message.warning('请输入文件夹名称'); return; }
                this.folderDialog.loading = true;
                try {
                    if (this.folderDialog.editing) {
                        await post(pageUrl + '?handler=UpdateFolder', { id: form.id, name: form.name, description: form.description || '' });
                    } else {
                        await post(pageUrl + '?handler=CreateFolder', {
                            name: form.name,
                            description: form.description || '',
                            parentId: this.currentFolderId,
                            resourceScope: this.resourceScope
                        });
                    }
                    this.$message.success(this.folderDialog.editing ? '文件夹已更新' : '文件夹已创建');
                    this.folderDialog.visible = false;
                    this.reloadFolderTree();
                } catch (error) {
                    this.$message.error('保存文件夹失败：' + errorMessage(error));
                } finally {
                    this.folderDialog.loading = false;
                }
            },
            deleteFolder: async function (folder) {
                try {
                    await this.$confirm('仅空文件夹可以删除。确认删除“' + folder.name + '”吗？', '确认删除', { type: 'warning' });
                    await post(pageUrl + '?handler=DeleteFolder&id=' + encodeURIComponent(folder.id));
                    if (this.currentFolderId === folder.id) await this.enterFolder(null);
                    this.reloadFolderTree();
                    this.$message.success('文件夹已删除');
                } catch (error) {
                    if (error !== 'cancel') this.$message.error('删除文件夹失败：' + errorMessage(error));
                }
            }
        }
    });
})();

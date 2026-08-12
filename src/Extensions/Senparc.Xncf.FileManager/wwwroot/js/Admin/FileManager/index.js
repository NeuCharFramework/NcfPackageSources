(function () {
    const pageUrl = window.location.pathname || '/Admin/FileManager/Index';

    function unwrap(response) {
        return response && response.data && response.data.data !== undefined ? response.data.data : response.data;
    }

    function errorMessage(error) {
        const data = error && error.response && error.response.data;
        return (data && (data.message || data.title)) || (error && error.message) || '请求失败';
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
                uploadDialog: { visible: false, fileList: [], uploading: false },
                folderDialog: { visible: false, loading: false, editing: false, form: { id: null, name: '', description: '' } },
                guideDialogVisible: false
            };
        },
        computed: {
            isSiteAsset: function () { return this.resourceScope === 200; },
            resourceScopeName: function () { return this.isSiteAsset ? '站点静态资源' : '知识库资料'; },
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
                const currentFolder = this.folderPath.length ? this.folderPath[this.folderPath.length - 1].name : '根目录';
                return this.resourceScopeName + ' / ' + currentFolder;
            }
        },
        created: function () {
            this.restoreRouteState();
            this.enterFolder(this.currentFolderId);
        },
        methods: {
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
                    await axios.post(pageUrl + '?handler=EditNote', { id: row.id, note: row.description || '' });
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
                    await axios.post(pageUrl + '?handler=Delete&id=' + encodeURIComponent(row.id));
                    this.$message.success('已删除');
                    await this.getList();
                } catch (error) {
                    if (error !== 'cancel') this.$message.error('删除失败：' + errorMessage(error));
                }
            },
            setPublication: async function (row, publish) {
                try {
                    await axios.post(pageUrl + '?handler=SetSiteAssetPublication', { id: row.id, publish: publish });
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
            showUploadDialog: function () { this.uploadDialog.visible = true; this.uploadDialog.fileList = []; },
            handleFileChange: function (file, fileList) {
                if (file.description === undefined) this.$set(file, 'description', '');
                this.uploadDialog.fileList = fileList;
            },
            beforeUpload: function () { return false; },
            submitUpload: async function () {
                if (!this.uploadDialog.fileList.length) { this.$message.warning('请选择要上传的文件'); return; }
                this.uploadDialog.uploading = true;
                try {
                    const formData = new FormData();
                    this.uploadDialog.fileList.forEach(function (file) {
                        formData.append('files', file.raw);
                        formData.append('descriptions', file.description || '');
                    });
                    formData.append('resourceScope', this.resourceScope);
                    if (this.currentFolderId != null) formData.append('folderId', this.currentFolderId);
                    await axios.post(pageUrl + '?handler=Upload', formData, { headers: { 'Content-Type': 'multipart/form-data' } });
                    this.$message.success('上传成功；站点静态资源仍需显式公开后才可被站点引用。');
                    this.uploadDialog.visible = false;
                    await this.getList();
                } catch (error) {
                    this.$message.error('上传失败：' + errorMessage(error));
                } finally {
                    this.uploadDialog.uploading = false;
                }
            },
            cancelUpload: function () { this.uploadDialog.visible = false; },
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
                        await axios.post(pageUrl + '?handler=UpdateFolder', { id: form.id, name: form.name, description: form.description || '' });
                    } else {
                        await axios.post(pageUrl + '?handler=CreateFolder', {
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
                    await axios.post(pageUrl + '?handler=DeleteFolder&id=' + encodeURIComponent(folder.id));
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

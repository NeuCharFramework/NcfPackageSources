new Vue({
    el: "#app",
    data() {
        return {
            tableData: [],
            tableLoading: false,
            page: 1,
            pageSize: 10,
            total: 0,
            uploadDialogVisible: false,
            editNoteDialogVisible: false,
            editForm: {
                id: null,
                note: ''
            }
        }
    },
    mounted() {
        this.getList()
    },
    methods: {
        // 获取CSRF Token
        getCsrfToken() {
            return document.querySelector('input[name="__RequestVerificationToken"]').value;
        },
        // 获取文件列表
        async getList() {
            this.tableLoading = true
            try {
                const res = await service.get(`/Admin/FileManager/Index?handler=List&page=${this.page}&pageSize=${this.pageSize}`)
                this.tableData = res.data.data.items
                this.total = res.data.data.total
            } catch (error) {
                console.error(error)
                this.$message.error('获取列表失败')
            }
            this.tableLoading = false
        },
        // 格式化文件大小
        formatFileSize(size) {
            if (size < 1024) return size + ' B'
            if (size < 1024 * 1024) return (size / 1024).toFixed(2) + ' KB'
            if (size < 1024 * 1024 * 1024) return (size / 1024 / 1024).toFixed(2) + ' MB'
            return (size / 1024 / 1024 / 1024).toFixed(2) + ' GB'
        },
        // 日期格式化
        dateformatter(date) {
            return moment(date).format('YYYY-MM-DD HH:mm:ss')
        },
        // 分页处理
        handleCurrentChange(val) {
            this.page = val
            this.getList()
        },
        handleSizeChange(val) {
            this.pageSize = val
            this.page = 1
            this.getList()
        },
        // 上传文件
        uploadFile() {
            this.uploadDialogVisible = true
        },
        // 上传成功处理
        handleUploadSuccess(response) {
            this.$message.success('上传成功')
            this.uploadDialogVisible = false
            this.getList()
        },
        // 上传失败处理
        handleUploadError(err) {
            this.$message.error('上传失败')
            console.error(err)
        },
        // 编辑备注
        editNote(row) {
            this.editForm.id = row.id
            this.editForm.note = row.note
            this.editNoteDialogVisible = true
        },
        // 提交编辑备注
        async submitEditNote() {
            try {
                await service.post('/Admin/FileManager/Index?handler=EditNote', this.editForm)
                this.$message.success('修改成功')
                this.editNoteDialogVisible = false
                this.getList()
            } catch (error) {
                console.error(error)
                this.$message.error('修改失败')
            }
        },
        // 删除文件
        deleteFile(row) {
            this.$confirm('确认删除该文件吗?', '提示', {
                confirmButtonText: '确定',
                cancelButtonText: '取消',
                type: 'warning'
            }).then(async () => {
                try {
                    await service.post('/Admin/FileManager/Index?handler=Delete', { id: row.id })
                    this.$message.success('删除成功')
                    this.getList()
                } catch (error) {
                    console.error(error)
                    this.$message.error('删除失败')
                }
            }).catch(() => {})
        }
    }
}) 
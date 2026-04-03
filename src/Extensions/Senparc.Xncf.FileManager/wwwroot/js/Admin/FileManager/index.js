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
        // Get CSRF Token
        getCsrfToken() {
            return document.querySelector('input[name="__RequestVerificationToken"]').value;
        },
        // Get file list
        async getList() {
            this.tableLoading = true
            try {
                const res = await service.get(`/Admin/FileManager/Index?handler=List&page=${this.page}&pageSize=${this.pageSize}`)
                this.tableData = res.data.data
                this.total = res.data.data.total
            } catch (error) {
                console.error(error)
                this.$message.error('获取列表失败')
            }
            this.tableLoading = false
        },
        // Format file size
        formatFileSize(size) {
            if (size < 1024) return size + ' B'
            if (size < 1024 * 1024) return (size / 1024).toFixed(2) + ' KB'
            if (size < 1024 * 1024 * 1024) return (size / 1024 / 1024).toFixed(2) + ' MB'
            return (size / 1024 / 1024 / 1024).toFixed(2) + ' GB'
        },
        // date formatting
        dateformatter(date) {
            return moment(date).format('YYYY-MM-DD HH:mm:ss')
        },
        // Pagination
        handleCurrentChange(val) {
            this.page = val
            this.getList()
        },
        handleSizeChange(val) {
            this.pageSize = val
            this.page = 1
            this.getList()
        },
        // Upload files
        uploadFile() {
            this.uploadDialogVisible = true
        },
        // Upload successfully processed
        handleUploadSuccess(response) {
            this.$message.success('上传成功')
            this.uploadDialogVisible = false
            this.getList()
        },
        // Upload failure handling
        handleUploadError(err) {
            this.$message.error('上传失败')
            console.error(err)
        },
        // Editor's Notes
        editNote(row) {
            this.editForm.id = row.id
            this.editForm.note = row.note
            this.editNoteDialogVisible = true
        },
        // Submit Editor's Note
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
        // Delete files
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
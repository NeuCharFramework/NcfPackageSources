new Vue({
    el: '.admin-site',
    data() {
        return {
            loading: false,
            tableData: [],
            dialogVisible: false,
            dialogTitle: '',
            form: {
                name: '',
                startUrl: '',
                maxThread: 4,
                maxBuildMinutes: 10,
                maxDeep: 5,
                maxPageCount: 500
            },
            rules: {
                name: [
                    { required: true, message: '请输入任务名称', trigger: 'blur' }
                ],
                startUrl: [
                    { required: true, message: '请输入起始URL', trigger: 'blur' }
                ]
            }
        }
    },
    created() {
        this.getList()
    },
    methods: {
        getStatusType(status) {
            const statusMap = {
                '-1': 'danger',  // Error
                '0': 'info',     // Waiting
                '1': 'warning',  // Running
                '2': 'success'   // Completed
            }
            return statusMap[status]
        },
        getStatusText(status) {
            const statusMap = {
                '-1': '出错',
                '0': '等待开始',
                '1': '进行中',
                '2': '已完成'
            }
            return statusMap[status]
        },
        async getList() {
            this.loading = true
            try {
                const res = await axios.get('/Admin/SenMapic/Task/List')
                this.tableData = res.data
            } catch (error) {
                this.$message.error('获取任务列表失败')
            }
            this.loading = false
        },
        handleAdd() {
            this.dialogTitle = '新建任务'
            this.dialogVisible = true
            this.form = {
                name: '',
                startUrl: '',
                maxThread: 4,
                maxBuildMinutes: 10,
                maxDeep: 5,
                maxPageCount: 500
            }
        },
        async handleSubmit() {
            this.$refs.form.validate(async (valid) => {
                if (valid) {
                    try {
                        await axios.post('/Admin/SenMapic/Task/Create', this.form)
                        this.$message.success('创建成功')
                        this.dialogVisible = false
                        this.getList()
                    } catch (error) {
                        this.$message.error('创建失败')
                    }
                }
            })
        },
        async handleStart(row) {
            try {
                await axios.post(`/Admin/SenMapic/Task/Start/${row.id}`)
                this.$message.success('任务已启动')
                this.getList()
            } catch (error) {
                this.$message.error('启动失败')
            }
        },
        async handleDelete(row) {
            try {
                await this.$confirm('确认删除该任务?', '提示', {
                    type: 'warning'
                })
                await axios.delete(`/Admin/SenMapic/Task/Delete/${row.id}`)
                this.$message.success('删除成功')
                this.getList()
            } catch (error) {
                if (error !== 'cancel') {
                    this.$message.error('删除失败')
                }
            }
        }
    }
}) 
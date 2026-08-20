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
                maxPageCount: 500,
                startImmediately: false
            },
            rules: {
                name: [
                    { required: true, message: ncfT('SenMapic.Validation.EnterTaskName'), trigger: 'blur' }
                ],
                startUrl: [
                    { required: true, message: ncfT('SenMapic.Validation.EnterStartUrl'), trigger: 'blur' }
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
                '-1': ncfT('SenMapic.Status.Error'),
                '0': ncfT('SenMapic.Status.Waiting'),
                '1': ncfT('SenMapic.Status.Running'),
                '2': ncfT('SenMapic.Status.Completed')
            }
            return statusMap[status]
        },
        async getList() {
            this.loading = true
            try {
                const res = await axios.get('/Admin/SenMapic/Task/List')
                this.tableData = res.data
            } catch (error) {
                this.$message.error(ncfT('SenMapic.Message.LoadTasksFailed'))
            }
            this.loading = false
        },
        handleAdd() {
            this.dialogTitle = ncfT('SenMapic.Task.Create')
            this.dialogVisible = true
            this.form = {
                name: '',
                startUrl: '',
                maxThread: 4,
                maxBuildMinutes: 10,
                maxDeep: 5,
                maxPageCount: 500,
                startImmediately: false
            }
        },
        async handleSubmit() {
            this.$refs.form.validate(async (valid) => {
                if (valid) {
                    try {
                        await axios.post('/Admin/SenMapic/Task/Create', this.form)
                        this.$message.success(ncfT('SenMapic.Message.CreateSucceeded'))
                        this.dialogVisible = false
                        this.getList()
                    } catch (error) {
                        this.$message.error(ncfT('SenMapic.Message.CreateFailed'))
                    }
                }
            })
        },
        async handleStart(row) {
            try {
                await axios.post(`/Admin/SenMapic/Task/Start/${row.id}`)
                this.$message.success(ncfT('SenMapic.Message.TaskStarted'))
                this.getList()
            } catch (error) {
                this.$message.error(ncfT('SenMapic.Message.StartFailed'))
            }
        },
        async handleDelete(row) {
            try {
                await this.$confirm(ncfT('SenMapic.Message.ConfirmDeleteTask'), ncfT('SenMapic.Common.Prompt'), {
                    type: 'warning'
                })
                await axios.delete(`/Admin/SenMapic/Task/Delete/${row.id}`)
                this.$message.success(ncfT('SenMapic.Message.DeleteSucceeded'))
                this.getList()
            } catch (error) {
                if (error !== 'cancel') {
                    this.$message.error(ncfT('SenMapic.Message.DeleteFailed'))
                }
            }
        }
    }
})

(function () {
  const apiRoot = '/api/Senparc.Xncf.AgentsManager/AgentExecutionAppService/Xncf.AgentsManager_AgentExecutionAppService'
  const streamRoot = '/api/Senparc.Xncf.AgentsManager/AgentExecutionStream/Subscribe'

  const vm = new Vue({
    el: '#agent-executions-app',
    data() {
      return {
        agents: [],
        tasks: [],
        detail: null,
        events: [],
        humanRequests: [],
        filter: '',
        status: null,
        loading: false,
        detailLoading: false,
        starting: false,
        startDialogVisible: false,
        eventSource: null,
        startForm: {
          agentTemplateId: null,
          name: '',
          input: '',
          allowFunctionCalls: false
        }
      }
    },
    mounted() {
      window.app = this
      this.loadAgents()
      this.loadTasks().then(() => {
        const taskId = Number(new URLSearchParams(window.location.search).get('taskId') || 0)
        if (taskId > 0) this.loadDetail(taskId)
      })
    },
    beforeDestroy() {
      this.closeStream()
    },
    methods: {
      async loadAgents() {
        const response = await serviceAM.get(`${apiRoot}.GetAgents`)
        this.agents = response?.data?.data || []
      },
      async loadTasks() {
        this.loading = true
        try {
          const params = {
            filter: this.filter || '',
            pageIndex: 0,
            pageSize: 100
          }
          if (this.status !== null && this.status !== '') params.status = this.status
          const response = await serviceAM.get(`${apiRoot}.GetList`, { params })
          this.tasks = response?.data?.data?.tasks || []
          if (this.detail) {
            const current = this.tasks.find(item => Number(item.id) === Number(this.detail.id))
            if (current) await this.loadDetail(current.id, false)
          }
        } finally {
          this.loading = false
        }
      },
      async loadDetail(id, openStream = true) {
        if (!id) return
        this.detailLoading = true
        try {
          const response = await serviceAM.get(`${apiRoot}.GetItem`, { params: { id } })
          const detail = response?.data?.data || null
          this.detail = detail
          this.events = Array.isArray(detail?.events) ? detail.events : []
          await this.loadHumanRequests(id)
          if (openStream) this.openStream(id, Number(detail?.status || 0))
        } finally {
          this.detailLoading = false
        }
      },
      async loadHumanRequests(id) {
        const response = await serviceAM.get(`${apiRoot}.GetHumanRequests`, {
          params: { agentExecutionTaskId: id },
          customAlert: true
        })
        this.humanRequests = response?.data?.data || []
      },
      selectTask(task) {
        this.loadDetail(task.id)
      },
      async startTask() {
        if (!this.startForm.agentTemplateId || !String(this.startForm.input || '').trim()) {
          this.$message.warning('请选择 Agent 并填写输入内容。')
          return
        }
        this.starting = true
        try {
          const response = await serviceAM.post(`${apiRoot}.Start`, {
            agentTemplateId: Number(this.startForm.agentTemplateId),
            name: this.startForm.name,
            input: this.startForm.input,
            source: 'Direct',
            allowFunctionCalls: this.startForm.allowFunctionCalls
          })
          const task = response?.data?.data?.task
          this.startDialogVisible = false
          this.startForm.name = ''
          this.startForm.input = ''
          if (task) {
            await this.loadTasks()
            await this.loadDetail(task.id)
          }
        } finally {
          this.starting = false
        }
      },
      async cancelTask() {
        if (!this.detail?.id) return
        await serviceAM.post(`${apiRoot}.Cancel`, null, { params: { id: this.detail.id } })
        await this.loadDetail(this.detail.id, false)
      },
      async resolveHumanRequest(request, approved) {
        if (!request?.requestId || !this.detail?.id) return
        await serviceAM.post(`${apiRoot}.ResolveHumanRequest`, null, {
          params: {
            requestId: request.requestId,
            approved: !!approved
          }
        })
        await this.loadDetail(this.detail.id, false)
      },
      openStream(id, status) {
        this.closeStream()
        if (!this.isRunning(status)) return
        this.eventSource = new EventSource(`${streamRoot}?agentExecutionTaskId=${encodeURIComponent(id)}&replayBuffered=true`)
        this.eventSource.onmessage = event => this.acceptEvent(event.data)
        ;['status', 'model', 'info', 'tool-start', 'tool-complete', 'tool-failed', 'assistant', 'error'].forEach(type => {
          this.eventSource.addEventListener(type, event => this.acceptEvent(event.data))
        })
        this.eventSource.onerror = () => {
          if (this.eventSource && this.eventSource.readyState === EventSource.CLOSED) this.closeStream()
        }
      },
      acceptEvent(raw) {
        let event
        try {
          event = JSON.parse(raw)
        } catch (_) {
          return
        }
        if (!event || this.events.some(item => Number(item.sequence) === Number(event.sequence))) return
        this.events.push(event)
        if (this.detail) {
          this.$set(this.detail, 'status', event.status === 'finished' ? 3 : event.status === 'failed' ? 5 : this.detail.status)
          if (event.text && event.eventType === 'assistant') this.$set(this.detail, 'output', event.text)
          if (event.errorMessage) this.$set(this.detail, 'errorMessage', event.errorMessage)
        }
        if (event.isFinal) {
          this.closeStream()
          this.loadTasks()
          if (this.detail?.id) this.loadDetail(this.detail.id, false)
        }
      },
      closeStream() {
        if (this.eventSource) {
          this.eventSource.close()
          this.eventSource = null
        }
      },
      backToAgents() {
        window.location.assign('/Admin/AgentsManager/Index')
      },
      isRunning(status) {
        return Number(status) === 0 || Number(status) === 1 || Number(status) === 2
      },
      statusText(status) {
        return { 0: '等待', 1: '运行中', 2: '暂停', 3: '完成', 4: '取消', 5: '失败' }[Number(status)] || '未知'
      },
      statusType(status) {
        return { 0: 'info', 1: 'warning', 2: 'warning', 3: 'success', 4: 'info', 5: 'danger' }[Number(status)] || 'info'
      },
      sourceText(source) {
        return { Direct: '直接调用', Workflow: 'Workflow', PublishedA2A: '发布型 A2A' }[source] || source || '未知来源'
      },
      eventTitle(event) {
        return {
          status: '状态',
          model: '模型准备',
          info: '运行信息',
          assistant: 'Agent 输出',
          'tool-start': '工具开始',
          'tool-complete': '工具完成',
          'tool-failed': '工具失败',
          error: '错误'
        }[event.eventType] || event.eventType || '事件'
      },
      formatNumber(value) {
        return Number(value || 0).toLocaleString()
      },
      formatDate(value) {
        if (!value) return '—'
        const date = new Date(value)
        return Number.isNaN(date.getTime()) ? String(value) : date.toLocaleString()
      }
    }
  })
})()

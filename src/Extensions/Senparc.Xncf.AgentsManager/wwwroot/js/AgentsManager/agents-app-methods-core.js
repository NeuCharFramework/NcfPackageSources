/*
 * AgentsManager 前端：核心工具、统计、Hash 路由、快捷跳转与 AgentGraph 3D 视图方法。
 * 注意：本文件与 agents-app-*.js 系列按 three-loader.js 中的顺序加载，
 * 依赖全局 var（classic script 顶层 var 挂到 window），请勿单独引入。
 */

var AgentsAppMethodsCore = {
    //寻找目标字符串
    findDest(arg1) {
      // 待判断的字符串
      //const str = '2025.05.07.1-T1-A1-草稿';
      const str = arg1;

      // 正则表达式：匹配 XXXX.XX.XX.X 的结构（X为数字）
      const regex = /^\d{4}\.\d{2}\.\d{2}\.\d+/;

      // 判断字符串是否符合规则
      if (regex.test(str)) {
        console.log('目标字符串');
        return true;
      } else {
        console.log('非目标字符串');
        return false;
      }
    },
    calculateDuration,
    scoreFormatter,
    escapeHtml(value) {
      return String(value ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;')
    },
    safeMarkdownUrl(value) {
      try {
        const url = new URL(String(value || ''), window.location.origin)
        return url.protocol === 'http:' || url.protocol === 'https:' ? url.href : ''
      } catch (e) {
        return ''
      }
    },
    renderSafeMarkdown(content) {
      const escapedContent = this.escapeHtml(content)
      if (typeof marked === 'undefined') {
        return escapedContent.replace(/\n/g, '<br>')
      }

      const viewModel = this
      const renderer = new marked.Renderer()
      renderer.link = function ({ href, title, tokens }) {
        const safeHref = viewModel.safeMarkdownUrl(href)
        const label = this.parser.parseInline(tokens)
        if (!safeHref) {
          return label
        }
        const safeTitle = title ? ` title="${viewModel.escapeHtml(title)}"` : ''
        return `<a href="${viewModel.escapeHtml(safeHref)}"${safeTitle}>${label}</a>`
      }
      renderer.image = function ({ href, title, text }) {
        const safeHref = viewModel.safeMarkdownUrl(href)
        if (!safeHref) {
          return viewModel.escapeHtml(text)
        }
        const safeTitle = title ? ` title="${viewModel.escapeHtml(title)}"` : ''
        return `<img src="${viewModel.escapeHtml(safeHref)}" alt="${viewModel.escapeHtml(text)}"${safeTitle}>`
      }

      return marked.parse(escapedContent, { renderer })
    },
    formatAgentGraphDebugTime(value) {
      if (!value) {
        return '--'
      }
      const date = value instanceof Date ? value : new Date(value)
      if (Number.isNaN(date.getTime())) {
        return '--'
      }
      const pad = n => String(n).padStart(2, '0')
      return pad(date.getHours()) + ':' + pad(date.getMinutes()) + ':' + pad(date.getSeconds())
    },
    getAgentStatisticMetricValue(agent) {
      const value = Number(agent?.[this.agentStatisticMetric] || 0)
      return Number.isFinite(value) && value > 0 ? value : 0
    },
    formatAgentStatisticValue(value) {
      const numeric = Number(value || 0)
      if (!Number.isFinite(numeric)) return '0'
      if (this.agentStatisticMetric === 'score') {
        return numeric.toLocaleString('en-US', { maximumFractionDigits: 1 })
      }
      return this.formatUsageCount(numeric)
    },
    agentStatisticTileStyle(tile) {
      const span = Math.max(2, Number(tile?.span || 2))
      return {
        gridColumn: `span ${span}`,
        gridRow: `span ${span}`
      }
    },
    async refreshAgentStatistics() {
      await this.getAgentListData('agent')
      this.$message.success('统计数据已刷新')
    },
    handleAgentStatisticTileClick(agent) {
      if (!agent) return
      this.handleEditDrawerOpenBtn('drawerAgent', agent)
    },
    parseHashRoute() {
      const raw = (window.location.hash || '').replace(/^#/, '')
      const route = {
        tab: '',
        view: '',
        agentId: null,
        groupId: null,
        taskId: null,
        remoteAgentId: null
      }
      if (!raw) {
        return route
      }
      const params = new URLSearchParams(raw)
      route.tab = params.get('tab') || ''
      route.view = params.get('view') || ''
      route.agentId = Number(params.get('agentId') || 0) || null
      route.groupId = Number(params.get('groupId') || 0) || null
      route.taskId = Number(params.get('taskId') || 0) || null
      route.remoteAgentId = Number(params.get('remoteAgentId') || 0) || null
      return route
    },
    setHashRoute(route) {
      if (this.isApplyingHashRoute) {
        return
      }
      const params = new URLSearchParams()
      if (route.tab) {
        params.set('tab', route.tab)
      }
      if (route.view) {
        params.set('view', route.view)
      }
      if (route.agentId) {
        params.set('agentId', String(route.agentId))
      }
      if (route.groupId) {
        params.set('groupId', String(route.groupId))
      }
      if (route.taskId) {
        params.set('taskId', String(route.taskId))
      }
      if (route.remoteAgentId) {
        params.set('remoteAgentId', String(route.remoteAgentId))
      }
      const nextHash = params.toString()
      if ((window.location.hash || '').replace(/^#/, '') === nextHash) {
        return
      }
      window.location.hash = nextHash
    },
    buildCurrentRoute(extra = {}) {
      const route = {
        tab: this.tabsActiveName || 'first'
      }
      if (route.tab === 'first') {
        if (['three', 'stats'].includes(this.agentListViewMode)) {
          route.view = this.agentListViewMode
        }
        if (this.scrollbarAgentIndex) {
          route.agentId = this.scrollbarAgentIndex
        }
      }
      if (route.tab === 'second') {
        const groupId = this.groupDetails?.chatGroupDto?.id || this.scrollbarGroupIndex || null
        const taskId = this.groupTaskDetails?.id || null
        if (groupId) {
          route.groupId = groupId
        }
        if (taskId) {
          route.taskId = taskId
        }
      }
      if (route.tab === 'third') {
        const taskId = this.taskDetails?.id || this.scrollbarTaskIndex || null
        if (taskId) {
          route.taskId = taskId
        }
      }
      return Object.assign(route, extra)
    },
    syncHashRoute(extra = {}) {
      this.setHashRoute(this.buildCurrentRoute(extra))
    },
    navigateByHash(route) {
      this.setHashRoute(route)
      this.applyHashRoute()
    },
    async applyHashRoute() {
      if (this.isApplyingHashRoute) {
        return
      }
      const route = this.parseHashRoute()
      if (!route.tab && !route.groupId && !route.taskId && !route.agentId && !route.remoteAgentId) {
        return
      }

      this.isApplyingHashRoute = true
      try {
        if (route.tab === 'remoteA2A') {
          this.visible.drawerRemoteAgent = true
          await this.getRemoteAgentListData()
          if (route.view === 'edit' && route.remoteAgentId) {
            const remoteAgent = (this.remoteAgentList || []).find(item => item.id === route.remoteAgentId)
            if (remoteAgent) {
              this.openRemoteAgentEditor(remoteAgent)
            }
          }
          return
        }

        const tab = ['first', 'second', 'third'].includes(route.tab) ? route.tab : 'first'
        if (this.tabsActiveName !== tab) {
          this.tabsActiveName = tab
          this.handleTabsClick()
        }

        if (tab === 'first') {
          if (route.view === 'edit' && route.agentId) {
            await this.getAgentListData('agent')
            const editAgent = (this.agentList || []).find(item => item.id === route.agentId)
            if (editAgent) {
              await this.handleEditDrawerOpenBtn('drawerAgent', editAgent)
            }
            return
          }
          if (['three', 'stats'].includes(route.view)) {
            this.handleAgentListViewModeChange(route.view, true)
          }
          if (route.agentId) {
            await this.getAgentListData('agent')
            const idx = (this.agentList || []).findIndex(item => item.id === route.agentId)
            if (idx >= 0) {
              this.handleAgentView(this.agentList[idx], idx, true)
            }
          }
          return
        }

        if (tab === 'second') {
          await this.getGroupListData('group')
          if (route.view === 'edit' && route.groupId) {
            const editGroup = (this.groupList || []).find(item => item.id === route.groupId)
            if (editGroup) {
              await this.handleEditDrawerOpenBtn('drawerGroup', editGroup)
            }
            return
          }
          if (route.groupId) {
            const groupItem = (this.groupList || []).find(item => item.id === route.groupId)
            if (groupItem) {
              this.handleGroupView('group', groupItem, 0, true)
            } else {
              this.groupShowType = '2'
              this.scrollbarGroupIndex = route.groupId
              await this.getGroupDetailData('groupTable', route.groupId, { id: route.groupId })
            }
          }
          if (route.taskId) {
            this.groupShowType = '3'
            await this.getTaskDetailData('groupTask', route.taskId, { id: route.taskId })
          }
          return
        }

        if (tab === 'third') {
          await this.gettaskListData('task')
          if (!route.taskId) {
            return
          }
          const idx = (this.taskList || []).findIndex(item => item.id === route.taskId)
          if (idx >= 0) {
            this.handleTaskView('task', this.taskList[idx], idx, true)
          } else {
            this.scrollbarTaskIndex = route.taskId
            await this.getTaskDetailData('task', route.taskId, { id: route.taskId })
          }
        }
      } finally {
        this.isApplyingHashRoute = false
      }
    },
    async refreshQuickJumpTaskOptions() {
      try {
        const res = await serviceAM.get('/api/Senparc.Xncf.AgentsManager/ChatTaskAppService/Xncf.AgentsManager_ChatTaskAppService.GetList?pageIndex=0&pageSize=0')
        const data = res?.data ?? {}
        if (!data.success) {
          return
        }
        const taskList = data?.data?.chatTaskList ?? []
        this.quickJumpTaskOptions = taskList.slice(0, 200).map(item => ({
          id: item.id,
          groupId: item.chatGroupId,
          name: item.name + ' (G' + item.chatGroupId + ')'
        }))
      } catch (e) {
        console.warn('refreshQuickJumpTaskOptions failed', e)
      }
    },
    handleQuickJumpGroup() {
      const groupId = Number(this.quickJumpGroupId || 0)
      if (!groupId) {
        return
      }
      this.navigateByHash({ tab: 'second', groupId: groupId })
    },
    handleQuickJumpTask() {
      const taskId = Number(this.quickJumpTaskId || 0)
      if (!taskId) {
        return
      }
      this.navigateByHash({ tab: 'third', taskId: taskId })
    },
    handleAgentGraphFilterChange() {
      this.renderAgentGraph()
    },
    buildAgentGraphSignature(snapshot) {
      if (!snapshot) {
        return ''
      }
      return JSON.stringify({
        agents: (snapshot.agents || []).map(item => [
          item.participantKey || `local:${item.id}`,
          item.chattingCount,
          item.pausedCount,
          item.humanInTheLoopPausedCount,
          item.score,
          item.enable,
          item.agentKind,
          item.connectionStatus,
          item.skillKinds
        ]),
        groups: (snapshot.groups || []).map(item => [
          item.id,
          item.enable,
          item.runningTaskCount,
          item.pausedTaskCount,
          item.humanInTheLoopPendingCount,
          item.state,
          item.taskStatusCounts
        ]),
        links: (snapshot.links || []).map(item => [item.groupId, item.participantKey || `local:${item.agentId}`]),
        collaborations: (snapshot.collaborations || []).map(item => [item.taskId, item.groupId, item.status, item.participantKeys || item.agentIds]),
        published: (snapshot.agents || []).map(item => [item.participantKey || `local:${item.id}`, item.hasPublishedA2A, item.publishedA2AEnabled])
      })
    },
    buildFilteredAgentGraphSnapshot(snapshot) {
      const source = snapshot || { agents: [], groups: [], links: [], collaborations: [] }
      const allGroups = source.groups || []
      const allLinks = source.links || []
      const allAgents = source.agents || []
      const allCollaborations = source.collaborations || []

      const selectedGroupId = this.agentGraphFilterGroupId
      const selectedStatuses = Array.isArray(this.agentGraphFilterTaskStatuses)
        ? this.agentGraphFilterTaskStatuses.map(item => Number(item))
        : []

      let filteredGroups = allGroups.filter(group => {
        if (selectedGroupId && group.id !== selectedGroupId) {
          return false
        }

        if (this.agentGraphShowOnlyActiveGroup && !(group.runningTaskCount > 0)) {
          return false
        }

        if (selectedStatuses.length > 0) {
          const statusMap = group.taskStatusCounts || {}
          return selectedStatuses.some(status => (statusMap[status] || statusMap[String(status)] || 0) > 0)
        }

        return true
      })

      const groupIdSet = new Set(filteredGroups.map(item => item.id))
      const filteredLinks = allLinks.filter(item => groupIdSet.has(item.groupId))
      const participantKeySet = new Set(filteredLinks.map(item => item.participantKey || `local:${item.agentId}`))

      const hasExplicitGroupConstraint = Boolean(selectedGroupId)
        || this.agentGraphShowOnlyActiveGroup
        || selectedStatuses.length > 0

      // Keep ungrouped agents visible when there is no explicit group/status constraint.
      const filteredAgents = hasExplicitGroupConstraint
        ? allAgents.filter(item => participantKeySet.has(item.participantKey || `local:${item.id}`))
        : allAgents

      const filteredCollaborations = allCollaborations.filter(item => {
        if (!groupIdSet.has(item.groupId)) {
          return false
        }
        if (selectedStatuses.length > 0) {
          return selectedStatuses.includes(Number(item.status))
        }
        return true
      })

      return {
        agents: filteredAgents,
        groups: filteredGroups,
        links: filteredLinks,
        collaborations: filteredCollaborations
      }
    },
    renderAgentGraph(snapshot = null) {
      if (!this.agentGraph3d) {
        return
      }
      const filtered = this.buildFilteredAgentGraphSnapshot(snapshot || this.agentGraphSnapshot)
      this.agentGraph3d.updateGraph(filtered)
      this.agentGraphLastRenderAt = new Date()
      this.agentGraphRenderCount += 1
    },
    handleAgentListViewModeChange(mode, fromHash = false) {
      if (!fromHash && !this.isApplyingHashRoute) {
        this.agentListViewMode = mode || 'panel'
        this.navigateByHash(this.buildCurrentRoute({
          tab: 'first',
          view: ['three', 'stats'].includes(this.agentListViewMode) ? this.agentListViewMode : null
        }))
        return
      }
      this.agentListViewMode = mode || 'panel'
      if (this.agentListViewMode === 'three' && this.tabsActiveName === 'first' && this.scrollbarAgentIndex === '') {
        this.$nextTick(() => {
          this.ensureAgentGraph3d()
          this.refreshAgentGraphSnapshot(true)
          this.startAgentGraphPolling()
        })
      } else {
        this.stopAgentGraphPolling()
        this.destroyAgentGraph3d()
      }
      this.syncHashRoute({
        tab: 'first',
        view: ['three', 'stats'].includes(this.agentListViewMode) ? this.agentListViewMode : null
      })
    },
    ensureAgentGraph3d() {
      if (!this.$refs.agent3dContainer || typeof AgentGraph3D === 'undefined') {
        return
      }
      if (this.agentGraph3d && this.agentGraph3d.renderer && this.agentGraph3d.renderer.domElement) {
        const currentCanvas = this.agentGraph3d.renderer.domElement
        const container = this.$refs.agent3dContainer
        if (!container.contains(currentCanvas)) {
          this.destroyAgentGraph3d()
        }
      }
      if (!this.agentGraph3d) {
        this.agentGraph3d = new AgentGraph3D(this.$refs.agent3dContainer, {
          onGroupHover: (groupId) => {
            this.hoveredAgentGroupId = groupId
          },
          onGroupLock: (groupId, locked) => {
            this.$set(this, 'agentGraphFocus', {
              groupId: groupId || null,
              locked: !!locked
            })
          },
          onAgentHover: (agent) => {
            this.$set(this, 'agentGraphFocusedAgent', agent || null)
          }
        })
        this.agentGraph3d.init()
        if ((this.agentGraphSnapshot.groups || []).length > 0) {
          this.renderAgentGraph(this.agentGraphSnapshot)
        }
      }
    },
    destroyAgentGraph3d() {
      if (this.agentGraph3d) {
        this.agentGraph3d.dispose()
        this.agentGraph3d = null
      }
    },
    startAgentGraphPolling() {
      this.stopAgentGraphPolling()
      this.agentGraphPollingTimer = setInterval(() => {
        if (this.tabsActiveName !== 'first' || this.scrollbarAgentIndex !== '' || this.agentListViewMode !== 'three') {
          return
        }
        this.refreshAgentGraphSnapshot(false)
      }, 1000)
    },
    stopAgentGraphPolling() {
      if (this.agentGraphPollingTimer) {
        clearInterval(this.agentGraphPollingTimer)
        this.agentGraphPollingTimer = null
      }
    },
    async refreshAgentGraphSnapshot(syncRender = false) {
      if (this.agentGraphRequesting) {
        return
      }
      this.agentGraphRequesting = true
      const query = {
        filter: this.agentQueryList.filter || ''
      }
      try {
        const res = await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/ChatGroupAppService/Xncf.AgentsManager_ChatGroupAppService.GetAgentGraphSnapshot?${getInterfaceQueryStr(query)}`)
        const data = res?.data ?? {}
        if (!data.success) {
          return
        }

        const snapshot = data.data || {}
        const normalizedSnapshot = {
          agents: snapshot.agents || [],
          groups: snapshot.groups || [],
          links: snapshot.links || [],
          collaborations: snapshot.collaborations || []
        }
        this.agentGraphSnapshot = normalizedSnapshot
        this.agentGraphLastRefreshAt = new Date()
        this.applyGraphMetricsToAgentList(normalizedSnapshot.agents)

        if (this.agentGraphFilterGroupId && !normalizedSnapshot.groups.some(item => item.id === this.agentGraphFilterGroupId)) {
          this.agentGraphFilterGroupId = null
        }

        const nextSignature = this.buildAgentGraphSignature(normalizedSnapshot)
        const isChanged = nextSignature !== this.agentGraphLastSignature
        this.agentGraphLastSignature = nextSignature

        if (syncRender || isChanged) {
          if (this.agentListViewMode === 'three') {
            this.$nextTick(() => {
              this.ensureAgentGraph3d()
              this.renderAgentGraph(normalizedSnapshot)
            })
          }
        }
      } finally {
        this.agentGraphRequesting = false
      }
    },
    applyGraphMetricsToAgentList(graphAgents) {
      if (!Array.isArray(this.agentList) || !Array.isArray(graphAgents) || graphAgents.length === 0) {
        return
      }

      const graphAgentMap = new Map(graphAgents
        .filter(item => !item.agentKind || item.agentKind === 'Local')
        .map(item => [item.id, item]))
      const mergedList = this.agentList.map(item => {
        const graph = graphAgentMap.get(item.id)
        if (!graph) {
          return item
        }
        return {
          ...item,
          chattingCount: graph.chattingCount,
          score: graph.score,
          promptCode: graph.promptCode
        }
      })
      this.$set(this, 'agentList', mergedList)

      if (this.agentDetails && this.agentDetails.agentTemplateDto) {
        const current = graphAgentMap.get(this.agentDetails.agentTemplateDto.id)
        if (current) {
          this.$set(this.agentDetails.agentTemplateDto, 'chattingCount', current.chattingCount)
          this.$set(this.agentDetails.agentTemplateDto, 'score', current.score)
          this.$set(this.agentDetails.agentTemplateDto, 'promptCode', current.promptCode)
        }
      }
    },
    // 计算 agent列表 需要填充的元素数量
    calcAgentFillNum() {
      // if (this.tabsActiveName === 'first' && this.scrollbarAgentIndex === '') {
      // }
      if (!this.agentListElResizeObserver) {
        // 计算 agent列表 需要填充的元素数量
        this.agentListElResizeObserver = new ResizeObserver(entries => {
          const elWidth = entries[0]?.contentRect?.width ?? 0
          const singleElWidth = 315
          const elSpac = 30
          const num = this.agentList.length
          // 单个元素 最小宽 315
          let rowNum = Math.floor(elWidth / singleElWidth)
          if (rowNum > 1) {
            rowNum = Math.floor((elWidth - ((rowNum - 1) * elSpac)) / singleElWidth)
            if (num > rowNum) {
              let _fillNum = num % rowNum
              this.fillCardNum = _fillNum > 0 ? rowNum - _fillNum : _fillNum
            } else {
              this.fillCardNum = 0
            }
          } else {
            this.fillCardNum = 0
          }
        });
      }
      if (this.agentListElResizeObserver && this.$refs.agentElListBox) {
        this.agentListElResizeObserver?.observe(this.$refs.agentElListBox);
      }

    },
    // 获取 状态文本
    getStatusText(item, showType) {
      // this.taskStateText this.agentStateText
      let statusText = ''
      // 智能体
      if (showType === '1') {
        let detailData = item.agentTemplateDto || item
        statusText = detailData.enable ? '待命' : '停用'
        let resultText = ''
        if (detailData.enable) {
          // state status
          resultText = this.taskStateText[item.status]
        }
        return resultText || statusText
      }
      // 组
      if (showType === '2') {
        let detailData = item.chatGroupDto || item
        statusText = detailData.enable ? '待命' : '停用'
        let resultText = ''
        if (detailData.enable) {
          resultText = this.taskStateText[item.state]
        }
        return resultText || statusText
      }
      // 任务
      if (showType === '3') {
        statusText = this.taskStateText[item.status]
        return statusText
      }
      return ''
    },
    // 获取 状态颜色
    getStatusColor(item, showType) {
      // this.taskStateColor this.agentStateColor
      let statusColor = ''
      // 智能体列表
      if (showType === '1') {
        let detailData = item.agentTemplateDto || item
        statusColor = detailData.enable ? 'standColor' : 'stopColor'
        let resultColor = ''
        if (detailData.enable) {
          resultColor = this.taskStateColor[item.status]
        }
        return resultColor || statusColor
      }
      // 组
      if (showType === '2') {
        let detailData = item.chatGroupDto || item
        statusColor = detailData.enable ? 'standColor' : 'stopColor'
        let resultColor = ''
        if (detailData.enable) {
          resultColor = this.taskStateColor[item.status]
        }
        return resultColor || statusColor
      }
      // 任务 
      if (showType === '3') {
        statusColor = this.taskStateColor[item.status]
        return statusColor
      }
    },
    getTaskStatusAccentColor(status) {
      const statusColorMap = {
        0: '#3376cd',
        1: '#409EFF',
        2: '#409EFF',
        3: '#67C23A',
        4: '#666666',
        5: '#F56C6C'
      }
      return statusColorMap[Number(status)] || '#C0C4CC'
    },
};


/*
 * AgentsManager 前端：Vue 根实例 computed 计算属性。
 * 注意：本文件与 agents-app-*.js 系列按 three-loader.js 中的顺序加载，
 * 依赖全局 var（classic script 顶层 var 挂到 window），请勿单独引入。
 */

var AgentsAppComputed = {
    // 计算未被选择的插件类型
    availablePluginTypes() {
      if (!this.agentForm.functionCallNames) {
        return this.pluginTypes;
      }
      // 将逗号分隔的字符串转换为数组进行比较
      const currentNames = this.agentForm.functionCallNames.split(',').filter(x => x);
      return this.pluginTypes.filter(type =>
        !currentNames.includes(type)
      );
    },
    functionBindingCount() {
      return Array.isArray(this.agentForm.functionBindings)
        ? this.agentForm.functionBindings.length
        : 0
    },
    functionBindingSummary() {
      const bindings = Array.isArray(this.agentForm.functionBindings)
        ? this.agentForm.functionBindings
        : []
      const counts = bindings.reduce((result, item) => {
        const kind = item?.kind || item?.Kind || 'plugin'
        result[kind] = (result[kind] || 0) + 1
        return result
      }, {})
      const parts = []
      if (counts.function) parts.push(`FunctionRender ${counts.function}`)
      if (counts.workflow) parts.push(`Workflow ${counts.workflow}`)
      if (counts.plugin) parts.push(`Plugin ${counts.plugin}`)
      return parts.length ? parts.join(' · ') : '未绑定工具或流程'
    },
    filteredFunctionBindingOptions() {
      const catalog = this.functionBindingCatalog || {}
      const tab = this.functionBindingTab
      const options = Array.isArray(catalog[`${tab}s`])
        ? catalog[`${tab}s`]
        : []
      const keyword = String(this.functionBindingSearch || '').trim().toLowerCase()
      if (!keyword) return options
      return options.filter(item => [
        item.name,
        item.description,
        item.moduleName,
        item.key
      ].some(value => String(value || '').toLowerCase().includes(keyword)))
    },
    // 解析 McpEndpoints JSON 字符串
    parsedMcpEndpoints() {
      try {
        if (!this.agentForm.mcpEndpoints) {
          return {};
        }
        return JSON.parse(this.agentForm.mcpEndpoints);
      } catch (e) {
        console.error('Failed to parse mcpEndpoints:', e);
        return {};
      }
    },
    // 从 MCP 列表选择：按关键字过滤可选端点
    filteredMcpEndpointOptions() {
      const options = Array.isArray(this.mcpEndpointOptions) ? this.mcpEndpointOptions : []
      const keyword = String(this.mcpSelectSearch || '').trim().toLowerCase()
      if (!keyword) return options
      return options.filter(item => [
        item.name,
        item.endpoint,
        item.description
      ].some(value => String(value || '').toLowerCase().includes(keyword)))
    },
    agentStatisticMetricOption() {
      return this.agentStatisticMetricOptions.find(item => item.value === this.agentStatisticMetric)
        || this.agentStatisticMetricOptions[0]
    },
    agentStatisticMetricLabel() {
      return this.agentStatisticMetricOption.label
    },
    agentStatisticMetricUnit() {
      return this.agentStatisticMetricOption.unit
    },
    agentStatisticMetricTotal() {
      return (this.agentList || []).reduce((total, agent) => total + this.getAgentStatisticMetricValue(agent), 0)
    },
    agentStatisticTiles() {
      const agents = this.agentList || []
      const values = agents.map(agent => this.getAgentStatisticMetricValue(agent))
      const maxValue = Math.max(0, ...values)

      return agents
        .map(agent => {
          const value = this.getAgentStatisticMetricValue(agent)
          // 使用平方根缩放，保留大用量的面积差异，同时避免少数极大值吞没整个图面。
          const ratio = maxValue > 0 ? Math.sqrt(value / maxValue) : 0
          const span = Math.max(2, Math.min(6, Math.round(2 + ratio * 4)))
          return {
            agent,
            value,
            span,
            title: `${agent.name || '未命名智能体'}：${this.agentStatisticMetricLabel} ${this.formatAgentStatisticValue(value)} ${this.agentStatisticMetricUnit}。点击编辑。`
          }
        })
        .sort((left, right) => right.value - left.value || String(left.agent.name || '').localeCompare(String(right.agent.name || '')))
    },
    agentGraphGroupOptions() {
      return (this.agentGraphSnapshot.groups || []).map(item => ({
        id: item.id,
        name: item.name
      }))
    },
    agentGraphDebugText() {
      const snapshot = this.agentGraphSnapshot || {}
      const agents = Array.isArray(snapshot.agents) ? snapshot.agents.length : 0
      const groups = Array.isArray(snapshot.groups) ? snapshot.groups.length : 0
      const links = Array.isArray(snapshot.links) ? snapshot.links.length : 0
      const cols = Array.isArray(snapshot.collaborations) ? snapshot.collaborations.length : 0
      const polling = this.agentGraphPollingTimer ? 'ON' : 'OFF'
      const requesting = this.agentGraphRequesting ? 'YES' : 'NO'

      return [
        '3D Debug',
        'Agents: ' + agents + '  Groups: ' + groups,
        'Links: ' + links + '  Collaborations: ' + cols,
        'Polling: ' + polling + '  Requesting: ' + requesting,
        'Rendered: ' + this.agentGraphRenderCount,
        'Refresh: ' + this.formatAgentGraphDebugTime(this.agentGraphLastRefreshAt),
        'Render: ' + this.formatAgentGraphDebugTime(this.agentGraphLastRenderAt)
      ].join('\n')
    },
    agentGraphOverview() {
      const snapshot = this.agentGraphSnapshot || {}
      const agents = Array.isArray(snapshot.agents) ? snapshot.agents : []
      const groups = Array.isArray(snapshot.groups) ? snapshot.groups : []
      const collaborations = Array.isArray(snapshot.collaborations) ? snapshot.collaborations : []
      const activeKeys = new Set(collaborations.flatMap(item => item.participantKeys || []))
      const local = agents.filter(item => item.agentKind !== 'RemoteA2A')
      const remote = agents.filter(item => item.agentKind === 'RemoteA2A')
      const activeTasks = groups.reduce((sum, group) => {
        const counts = group.taskStatusCounts || {}
        return sum + Number(counts[0] || counts['0'] || 0)
          + Number(counts[1] || counts['1'] || 0)
          + Number(counts[2] || counts['2'] || 0)
      }, 0)
      const chattingTasks = groups.reduce((sum, group) => {
        const counts = group.taskStatusCounts || {}
        return sum + Number(counts[1] || counts['1'] || 0)
      }, 0)
      const pausedTasks = groups.reduce((sum, group) => sum + Number(group.pausedTaskCount || 0), 0)
      const hilPending = groups.reduce((sum, group) => sum + Number(group.humanInTheLoopPendingCount || 0), 0)
      return {
        local: local.length,
        localEnabled: local.filter(item => item.enable !== false).length,
        localActive: local.filter(item => activeKeys.has(item.participantKey) || Number(item.chattingCount || 0) > 0).length,
        remote: remote.length,
        remoteEnabled: remote.filter(item => item.enable !== false).length,
        remoteActive: remote.filter(item => activeKeys.has(item.participantKey) || Number(item.chattingCount || 0) > 0).length,
        published: local.filter(item => item.hasPublishedA2A).length,
        publishedEnabled: local.filter(item => item.hasPublishedA2A && item.publishedA2AEnabled).length,
        groups: groups.length,
        groupsEnabled: groups.filter(item => item.enable !== false).length,
        groupsActive: groups.filter(item => Number(item.runningTaskCount || 0) > 0).length,
        activeTasks,
        chattingTasks,
        pausedTasks,
        hilPending
      }
    },
    agentGraphFocusedGroup() {
      const groupId = Number(this.agentGraphFocus?.groupId || 0)
      return (this.agentGraphSnapshot.groups || []).find(item => Number(item.id) === groupId) || null
    },
    agentGraphFocusedAgentSkills() {
      const skills = Array.isArray(this.agentGraphFocusedAgent?.skillKinds)
        ? this.agentGraphFocusedAgent.skillKinds
        : []
      const labels = {
        function: 'FunctionRender',
        workflow: 'Workflow',
        plugin: 'Plugin',
        mcp: 'MCP',
        a2a: 'A2A',
        human: 'Human'
      }
      return skills.map(skill => labels[skill] || skill).join(' · ') || '无额外技能'
    },
    quickJumpGroupOptions() {
      const map = new Map()
      ;(this.agentGraphSnapshot.groups || []).forEach(item => {
        map.set(item.id, { id: item.id, name: item.name })
      })
      ;(this.groupList || []).forEach(item => {
        if (!map.has(item.id)) {
          map.set(item.id, { id: item.id, name: item.name })
        }
      })
      return Array.from(map.values())
    },
    taskUsageSummaryByType() {
      return {
        task: this.buildTaskHistoryUsageSummary(this.taskHistoryList),
        agentTask: this.buildTaskHistoryUsageSummary(this.agentDetailsTaskHistoryList),
        agentGroupTask: this.buildTaskHistoryUsageSummary(this.agentDetailsGroupTaskHistoryList),
        groupTask: this.buildTaskHistoryUsageSummary(this.groupTaskHistoryList),
      }
    }
};


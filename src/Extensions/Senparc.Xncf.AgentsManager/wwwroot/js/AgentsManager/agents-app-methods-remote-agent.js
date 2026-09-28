/*
 * AgentsManager 前端：远程 A2A 智能体与 Published A2A 管理方法。
 * 注意：本文件与 agents-app-*.js 系列按 three-loader.js 中的顺序加载，
 * 依赖全局 var（classic script 顶层 var 挂到 window），请勿单独引入。
 */

var AgentsAppMethodsRemoteAgent = {
    remoteConnectionStatusText(status) {
      const statusMap = { 0: '未检测', 1: '可用', 2: '不可用' }
      return statusMap[Number(status)] || '未检测'
    },
    remoteConnectionStatusType(status) {
      const statusTypeMap = { 0: 'info', 1: 'success', 2: 'danger' }
      return statusTypeMap[Number(status)] || 'info'
    },
    remoteParticipantAvailabilityText(participant) {
      if (!participant?.enable) return '已停用'
      return this.remoteConnectionStatusText(participant.connectionStatus)
    },
    remoteParticipantAvailabilityType(participant) {
      if (!participant?.enable) return 'info'
      return this.remoteConnectionStatusType(participant.connectionStatus)
    },
    isRemoteAgentTesting(remoteAgentId) {
      return !!this.remoteAgentTestingIds?.[Number(remoteAgentId)]
    },
    setRemoteAgentTesting(remoteAgentIds, testing) {
      ;(remoteAgentIds || []).forEach(remoteAgentId => {
        const id = Number(remoteAgentId)
        if (id > 0) this.$set(this.remoteAgentTestingIds, id, testing)
      })
    },
    applyRemoteConnectionResults(results) {
      const resultById = new Map((results || [])
        .filter(result => Number(result?.remoteAgentId) > 0)
        .map(result => [Number(result.remoteAgentId), result]))
      if (!resultById.size) return

      const updateRemoteAgent = remoteAgent => {
        const remoteAgentId = Number(remoteAgent?.id || remoteAgent?.remoteAgentId || 0)
        const result = resultById.get(remoteAgentId)
        if (!remoteAgent || !result) return
        Object.assign(remoteAgent, result.remoteAgentDto || {})
        if (result.remoteAgentDto?.connectionStatus === undefined) {
          remoteAgent.connectionStatus = result.success ? 1 : 2
          remoteAgent.lastHealthCheckMessage = result.message || ''
        }
      }
      const updateParticipantList = participantList => {
        ;(participantList || []).forEach(participant => {
          if (participant?.agentKind === 'RemoteA2A') updateRemoteAgent(participant)
        })
      }
      const updateGroupDetail = groupDetail => {
        ;(groupDetail?.remoteMemberDtoList || []).forEach(member => updateRemoteAgent(member?.remoteAgentDto))
      }

      ;[this.remoteAgentList, this.groupRemoteAgentList, this.groupForm?.remoteMembers]
        .forEach(remoteAgentList => (remoteAgentList || []).forEach(updateRemoteAgent))
      updateParticipantList(this.groupStartParticipants)
      updateParticipantList(this.taskMemberList)
      updateParticipantList(this.agentDetailsTaskMemberList)
      updateGroupDetail(this.groupDetails)
      updateGroupDetail(this.agentDetailsGroupDetails)
    },
    async testRemoteAgentConnections(remoteAgentIds, options = {}) {
      const requestedIds = [...new Set((remoteAgentIds || []).map(Number).filter(id => id > 0))]
      const testAll = options.testAll === true
      if (!testAll && requestedIds.length === 0) return []

      const loadingIds = testAll
        ? (this.remoteAgentList || []).map(item => item.id)
        : requestedIds
      if (testAll) this.remoteAgentBatchTesting = true
      this.setRemoteAgentTesting(loadingIds, true)
      try {
        const response = await serviceAM.post(
          '/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.TestConnections',
          { remoteAgentIds: testAll ? [] : requestedIds })
        const data = response?.data ?? {}
        if (!data.success) throw new Error(data.errorMessage || data.data || '连接测试失败')

        const results = data?.data?.results ?? []
        this.applyRemoteConnectionResults(results)
        if (options.refreshLists) {
          await this.getRemoteAgentListData()
          if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
        }
        return results
      } catch (err) {
        if (!options.silent) this.$message.error(err?.message || '连接测试失败')
        throw err
      } finally {
        this.setRemoteAgentTesting(loadingIds, false)
        if (testAll) this.remoteAgentBatchTesting = false
      }
    },
    showRemoteAgentTestSummary(results) {
      const failedResults = (results || []).filter(result => !result.success)
      if (!results?.length) {
        this.$message.warning('没有可测试的远程 A2A 智能体')
        return
      }
      if (!failedResults.length) {
        this.$message.success(`全部通过：${results.length} 个远程 A2A 智能体均可用`)
        return
      }
      const failedLines = failedResults.map(result => `${result.name || `#${result.remoteAgentId}`}：${result.message || '不可用'}`)
      this.$alert(`通过 ${results.length - failedResults.length} 个，未通过 ${failedResults.length} 个。\n\n${failedLines.join('\n')}`,
        '远程 A2A 批量测试结果', { type: 'warning', confirmButtonText: '知道了' })
    },
    async getRemoteAgentListData(listType = 'remoteAgent') {
      const query = listType === 'groupRemoteAgent'
        ? { ...this.groupRemoteAgentQueryList }
        : { ...this.remoteAgentQueryList }
      try {
        const response = await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.GetList?${getInterfaceQueryStr(query)}`)
        const data = response?.data ?? {}
        if (!data.success) {
          throw new Error(data.errorMessage || data.data || '加载远程 A2A 智能体失败')
        }

        const list = data?.data?.list ?? []
        if (listType === 'groupRemoteAgent') {
          this.$set(this, 'groupRemoteAgentList', list)
          this.$nextTick(() => {
            this.isGetGroupRemoteAgent = false
            if (!this.visible.drawerGroup || !this.groupForm.remoteMembers?.length) return
            const selected = list.filter(item => this.groupForm.remoteMembers.some(member => member.id === item.id))
            this.toggleRemoteSelection(selected)
          })
        } else {
          this.$set(this, 'remoteAgentList', list)
        }
      } catch (err) {
        console.log('getRemoteAgentListData', err)
        this.$message.error(err?.message || '加载远程 A2A 智能体失败')
      }
    },
    openRemoteAgentManager() {
      this.visible.drawerRemoteAgent = true
      this.getRemoteAgentListData()
    },
    async openPublishedA2AEditor(item) {
      const agent = item?.agentTemplateDto || item || this.agentForm
      const agentTemplateId = Number(agent?.id || agent?.agentTemplateId || 0)
      if (!agentTemplateId) {
        this.$message.warning('请先保存本地智能体，再配置 A2A 对外发布')
        return
      }

      const defaults = this.$options.data().publishedA2AForm
      try {
        const response = await serviceAM.get(
          `/api/Senparc.Xncf.AgentsManager/PublishedA2AAgentAppService/Xncf.AgentsManager_PublishedA2AAgentAppService.GetByAgentTemplateId?agentTemplateId=${agentTemplateId}`)
        const data = response?.data ?? {}
        if (!data.success) throw new Error(data.errorMessage || data.data || '加载 A2A 发布配置失败')
        const existed = data.data || {}
        this.$set(this, 'publishedA2AForm', {
          ...defaults,
          ...existed,
          agentTemplateId,
          publicAgentKey: existed.publicAgentKey || `agent-${agentTemplateId}`,
          cardName: existed.cardName || agent.name || '',
          cardDescription: existed.cardDescription || agent.description || ''
        })
        this.visible.dialogPublishedA2A = true
        this.$nextTick(() => this.$refs.publishedA2AELForm?.clearValidate())
      } catch (err) {
        this.$message.error(err?.message || '加载 A2A 发布配置失败')
      }
    },
    closePublishedA2AEditor() {
      this.visible.dialogPublishedA2A = false
      this.$set(this, 'publishedA2AForm', this.$options.data().publishedA2AForm)
    },
    async savePublishedA2A() {
      this.$refs.publishedA2AELForm.validate(async (valid) => {
        if (!valid) return

        if (this.publishedA2AForm.enable) {
          const riskItems = [
            '外部系统调用后，输入及 Agent 的正常回复都会跨越本系统边界；请确认 Prompt、知识库和回复内容可对外共享。',
            '每次调用可能产生模型与工具成本；请在 HTTPS 网关配置访问控制、限流、监控和告警。'
          ]
          if (this.publishedA2AForm.authenticationMode === 0) {
            riskItems.push('当前未启用入站鉴权。仅可用于隔离、受控网络；不可直接暴露到公网。')
          }
          if (this.publishedA2AForm.allowFunctionCalls) {
            riskItems.push('已允许本地 Function / MCP 工具调用。外部输入可能间接触发读写或外部访问，请确认工具权限最小化。')
          }

          try {
            await this.$confirm(
              `<div>启用后，此本地 Agent 将作为标准 A2A 服务接受外部调用。</div><ul style="padding-left:20px;margin:10px 0 0;"><li>${riskItems.join('</li><li>')}</li></ul>`,
              '确认启用 A2A 对外服务',
              {
                type: 'warning',
                dangerouslyUseHTMLString: true,
                confirmButtonText: '我已知悉并保存',
                cancelButtonText: '取消'
              })
          } catch (_) {
            return
          }
        }

        try {
          const response = await serviceAM.post(
            '/api/Senparc.Xncf.AgentsManager/PublishedA2AAgentAppService/Xncf.AgentsManager_PublishedA2AAgentAppService.SetPublishedAgent',
            this.publishedA2AForm)
          const data = response?.data ?? {}
          if (!data.success) throw new Error(data.errorMessage || data.data || '保存失败')
          this.$set(this, 'publishedA2AForm', { ...this.publishedA2AForm, ...(data.data || {}) })
          await this.getAgentListData('agent')
          this.$message.success(this.publishedA2AForm.enable ? '本地 Agent 已发布为 A2A 服务' : 'A2A 发布配置已保存（当前未启用）')
        } catch (err) {
          this.$message.error(err?.message || '保存 A2A 发布配置失败')
        }
      })
    },
    async copyPublishedA2AUrl() {
      const url = this.publishedA2AForm.agentCardUrl
      if (!url) return
      try {
        await navigator.clipboard.writeText(url)
        this.$message.success('A2A Agent Card 地址已复制')
      } catch (err) {
        this.$message.warning('复制失败，请手动复制地址')
      }
    },
    openRemoteAgentEditor(item = null) {
      const defaults = this.$options.data().remoteAgentForm
      this.$set(this, 'remoteAgentForm', { ...defaults, ...(item || {}) })
      this.visible.dialogRemoteAgentEditor = true
    },
    closeRemoteAgentEditor() {
      this.visible.dialogRemoteAgentEditor = false
      this.$set(this, 'remoteAgentForm', this.$options.data().remoteAgentForm)
      this.$nextTick(() => this.$refs.remoteAgentELForm?.clearValidate())
    },
    async saveRemoteAgent() {
      this.$refs.remoteAgentELForm.validate(async (valid) => {
        if (!valid) return
        try {
          const response = await serviceAM.post(
            '/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.SetRemoteAgent',
            this.remoteAgentForm)
          const data = response?.data ?? {}
          if (!data.success) throw new Error(data.errorMessage || data.data || '保存失败')
          this.$message.success('远程 A2A 智能体已保存')
          this.closeRemoteAgentEditor()
          await this.getRemoteAgentListData()
          if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
        } catch (err) {
          this.$message.error(err?.message || '保存远程 A2A 智能体失败')
        }
      })
    },
    async testRemoteAgent(item) {
      try {
        const results = await this.testRemoteAgentConnections([item?.id], { silent: true, refreshLists: true })
        const result = results[0]
        if (!result?.success) {
          this.$message.error(result?.message || '连接测试失败')
          return
        }
        this.$message.success(result.message || 'A2A Agent Card 连接成功')
      } catch (err) {
        this.$message.error(err?.message || '连接测试失败')
      }
    },
    async testAllRemoteAgents() {
      try {
        const results = await this.testRemoteAgentConnections([], { testAll: true, silent: true, refreshLists: true })
        this.showRemoteAgentTestSummary(results)
      } catch (err) {
        this.$message.error(err?.message || '批量连接测试失败')
      }
    },
    async autoTestRemoteParticipants(participants) {
      const remoteAgentIds = (participants || [])
        .filter(participant => participant?.agentKind === 'RemoteA2A')
        .map(participant => participant.id)
      if (!remoteAgentIds.length) return []

      try {
        const results = await this.testRemoteAgentConnections(remoteAgentIds, { silent: true })
        const failedCount = results.filter(result => !result.success).length
        if (failedCount) {
          this.$message.warning(`${failedCount} 个远程 A2A 智能体当前不可用，可在成员列表中手动重测`)
        }
        return results
      } catch (err) {
        console.warn('autoTestRemoteParticipants failed', err)
        return []
      }
    },
    async testGroupStartRemoteAgent(participant) {
      try {
        const results = await this.testRemoteAgentConnections([participant?.id], { silent: true })
        const result = results[0]
        if (result?.success) {
          this.$message.success(result.message || 'A2A Agent Card 连接成功')
        } else {
          this.$message.error(result?.message || '连接测试失败')
        }
      } catch (err) {
        this.$message.error(err?.message || '连接测试失败')
      }
    },
    async testTaskRemoteAgent(participant) {
      try {
        const results = await this.testRemoteAgentConnections([participant?.id], { silent: true })
        const result = results[0]
        if (result?.success) {
          this.$message.success(result.message || 'A2A Agent Card 连接成功')
        } else {
          this.$message.error(result?.message || '连接测试失败')
        }
      } catch (err) {
        this.$message.error(err?.message || '连接测试失败')
      }
    },
    async setRemoteAgentEnable(item, enable) {
      try {
        const response = await serviceAM.post(
          `/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.Enable?id=${item.id}&enable=${enable}`,
          {})
        const data = response?.data ?? {}
        if (!data.success) throw new Error(data.errorMessage || data.data || '状态更新失败')
        this.$message.success(data.data || '状态已更新')
        await this.getRemoteAgentListData()
        if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
      } catch (err) {
        this.$message.error(err?.message || '状态更新失败')
      }
    },
    deleteRemoteAgent(item) {
      this.$confirm(`确认删除远程 A2A 智能体“${item.name}”？已加入群组的智能体不可删除。`, '删除确认', { type: 'warning' })
        .then(async () => {
          try {
            const response = await serviceAM.post(
              `/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.Delete?id=${item.id}`,
              {})
            const data = response?.data ?? {}
            if (!data.success) throw new Error(data.errorMessage || data.data || '删除失败')
            this.$message.success(data.data || '已删除')
            await this.getRemoteAgentListData()
            if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
          } catch (err) {
            this.$message.error(err?.message || '删除失败')
          }
        })
        .catch(() => { })
    },
    toggleRemoteSelection(rows) {
      if (rows) {
        rows.forEach(row => this.$refs?.groupRemoteAgentTable?.toggleRowSelection(row))
      } else {
        this.$refs?.groupRemoteAgentTable?.clearSelection()
      }
    },
    handleRemoteSelectionChange(val) {
      if (this.isGetGroupRemoteAgent) return
      const selectedIds = new Set((val || []).map(item => item.id))
      const visibleIds = new Set((this.groupRemoteAgentList || []).map(item => item.id))
      const retained = (this.groupForm.remoteMembers || []).filter(item => !visibleIds.has(item.id))
      const selected = (this.groupRemoteAgentList || []).filter(item => selectedIds.has(item.id))
      this.$set(this.groupForm, 'remoteMembers', [...retained, ...selected])
    },
    groupRemoteMembersCancel(item) {
      const index = this.groupForm.remoteMembers.findIndex(member => member.id === item.id)
      if (index !== -1) this.groupForm.remoteMembers.splice(index, 1)
      const row = this.groupRemoteAgentList.find(agent => agent.id === item.id)
      if (row) this.toggleRemoteSelection([row])
    },
};


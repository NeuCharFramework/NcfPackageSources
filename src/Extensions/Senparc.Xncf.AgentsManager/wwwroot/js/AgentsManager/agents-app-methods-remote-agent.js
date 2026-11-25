/*
 * Legacy comment translated to English.
 * Note: this file and the agents-app-*.js series are loaded in the order defined by three-loader.js,
 * depends on global variables (top-level classic-script vars are attached to window); do not import separately.
 */

var AgentsAppMethodsRemoteAgent = {
    remoteConnectionStatusText(status) {
      const statusMap = { 0: ncfST('Frontend.AgentsManager.NotTestedAlternate'), 1: ncfST('Frontend.AgentsManager.Available'), 2: ncfST('Frontend.AgentsManager.Unavailable') }
      return statusMap[Number(status)] || ncfST('Frontend.AgentsManager.NotTestedAlternate')
    },
    remoteConnectionStatusType(status) {
      const statusTypeMap = { 0: 'info', 1: 'success', 2: 'danger' }
      return statusTypeMap[Number(status)] || 'info'
    },
    remoteParticipantAvailabilityText(participant) {
      if (!participant?.enable) return ncfST('Frontend.AgentsManager.DisabledAlternate')
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
        if (!data.success) throw new Error(data.errorMessage || data.data || ncfST('Frontend.AgentsManager.ConnectionTestFailedAlternate'))

        const results = data?.data?.results ?? []
        this.applyRemoteConnectionResults(results)
        if (options.refreshLists) {
          await this.getRemoteAgentListData()
          if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
        }
        return results
      } catch (err) {
        if (!options.silent) this.$message.error(err?.message || ncfST('Frontend.AgentsManager.ConnectionTestFailedAlternate'))
        throw err
      } finally {
        this.setRemoteAgentTesting(loadingIds, false)
        if (testAll) this.remoteAgentBatchTesting = false
      }
    },
    showRemoteAgentTestSummary(results) {
      const failedResults = (results || []).filter(result => !result.success)
      if (!results?.length) {
        this.$message.warning(ncfST('Frontend.AgentsManager.NoRemoteATwoAAgentsCanBeTested'))
        return
      }
      if (!failedResults.length) {
        this.$message.success(ncfST('Frontend.AgentsManager.AllPassedValueRemoteATwoAAgentsAreAvailable', results.length))
        return
      }
      const failedLines = failedResults.map(result => `${result.name || `#${result.remoteAgentId}`}: ${result.message || ncfST('Frontend.AgentsManager.Unavailable')}`)
      this.$alert(ncfST('Frontend.AgentsManager.PassedValueFailedValueNNValue', results.length - failedResults.length, failedResults.length, failedLines.join('\n')),
        ncfST('Frontend.AgentsManager.RemoteATwoABatchTestResults'), { type: 'warning', confirmButtonText: ncfST('Frontend.AgentsManager.GotItAlternate') })
    },
    async getRemoteAgentListData(listType = 'remoteAgent') {
      const query = listType === 'groupRemoteAgent'
        ? { ...this.groupRemoteAgentQueryList }
        : { ...this.remoteAgentQueryList }
      try {
        const response = await serviceAM.get(`/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.GetList?${getInterfaceQueryStr(query)}`)
        const data = response?.data ?? {}
        if (!data.success) {
          throw new Error(data.errorMessage || data.data || ncfST('Frontend.AgentsManager.FailedToLoadRemoteATwoAAgents'))
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
        this.$message.error(err?.message || ncfST('Frontend.AgentsManager.FailedToLoadRemoteATwoAAgents'))
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
        this.$message.warning(ncfST('Frontend.AgentsManager.SaveTheLocalAgentBeforeConfiguringATwoAPublishing'))
        return
      }

      const defaults = this.$options.data().publishedA2AForm
      try {
        const response = await serviceAM.get(
          `/api/Senparc.Xncf.AgentsManager/PublishedA2AAgentAppService/Xncf.AgentsManager_PublishedA2AAgentAppService.GetByAgentTemplateId?agentTemplateId=${agentTemplateId}`)
        const data = response?.data ?? {}
        if (!data.success) throw new Error(data.errorMessage || data.data || ncfST('Frontend.AgentsManager.FailedToLoadATwoAPublishingConfiguration'))
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
        this.$message.error(err?.message || ncfST('Frontend.AgentsManager.FailedToLoadATwoAPublishingConfiguration'))
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
          const riskItems = [ncfST('Frontend.AgentsManager.AfterAnExternalSystemCallsThisServiceTheInputAndTheAgent'), ncfST('Frontend.AgentsManager.EachCallMayIncurModelAndToolCostsConfigureAccessControlRate')]
          if (this.publishedA2AForm.authenticationMode === 0) {
            riskItems.push(ncfST('Frontend.AgentsManager.InboundAuthenticationIsDisabledUseThisOnlyOnAnIsolatedControlledNetworkAlternate'))
          }
          if (this.publishedA2AForm.allowFunctionCalls) {
            riskItems.push(ncfST('Frontend.AgentsManager.LocalFunctionMCPToolCallsAreAllowedExternalInputMayIndirectlyTrigger'))
          }

          try {
            await this.$confirm(
              `<div>${ncfST('Frontend.AgentsManager.WhenEnabledThisLocalAgentAcceptsExternalCallsAsAStandardA')}</div><ul style="padding-left:20px;margin:10px 0 0;"><li>${riskItems.join('</li><li>')}</li></ul>`,
              ncfST('Frontend.AgentsManager.ConfirmEnablingTheATwoAPublicService'),
              {
                type: 'warning',
                dangerouslyUseHTMLString: true,
                confirmButtonText: ncfST('Frontend.AgentsManager.IUnderstandAndSave'),
                cancelButtonText: ncfST('Frontend.AgentsManager.Cancel')
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
          if (!data.success) throw new Error(data.errorMessage || data.data || ncfST('Frontend.AgentsManager.SaveFailed'))
          this.$set(this, 'publishedA2AForm', { ...this.publishedA2AForm, ...(data.data || {}) })
          await this.getAgentListData('agent')
          this.$message.success(this.publishedA2AForm.enable ? ncfST('Frontend.AgentsManager.RemoteATwoAAgentSaved') : ncfST('Frontend.AgentsManager.ATwoAPublishingConfigurationSavedCurrentlyDisabled'))
        } catch (err) {
          this.$message.error(err?.message || ncfST('Frontend.AgentsManager.FailedToSaveRemoteATwoAAgent'))
        }
      })
    },
    async copyPublishedA2AUrl() {
      const url = this.publishedA2AForm.agentCardUrl
      if (!url) return
      try {
        await navigator.clipboard.writeText(url)
        this.$message.success(ncfST('Frontend.AgentsManager.ATwoAAgentCardConnectionSucceeded'))
      } catch (err) {
        this.$message.warning(ncfST('Frontend.AgentsManager.CopyFailedCopyTheAddressManually'))
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
          if (!data.success) throw new Error(data.errorMessage || data.data || ncfST('Frontend.AgentsManager.SaveFailed'))
          this.$message.success(ncfST('Frontend.AgentsManager.RemoteATwoAAgentSaved'))
          this.closeRemoteAgentEditor()
          await this.getRemoteAgentListData()
          if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
        } catch (err) {
          this.$message.error(err?.message || ncfST('Frontend.AgentsManager.FailedToSaveRemoteATwoAAgent'))
        }
      })
    },
    async testRemoteAgent(item) {
      try {
        const results = await this.testRemoteAgentConnections([item?.id], { silent: true, refreshLists: true })
        const result = results[0]
        if (!result?.success) {
          this.$message.error(result?.message || ncfST('Frontend.AgentsManager.ConnectionTestFailedAlternate'))
          return
        }
        this.$message.success(result.message || ncfST('Frontend.AgentsManager.ATwoAAgentCardConnectionSucceeded'))
      } catch (err) {
        this.$message.error(err?.message || ncfST('Frontend.AgentsManager.ConnectionTestFailedAlternate'))
      }
    },
    async testAllRemoteAgents() {
      try {
        const results = await this.testRemoteAgentConnections([], { testAll: true, silent: true, refreshLists: true })
        this.showRemoteAgentTestSummary(results)
      } catch (err) {
        this.$message.error(err?.message || ncfST('Frontend.AgentsManager.BatchConnectionTestFailed'))
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
          this.$message.warning(ncfST('Frontend.AgentsManager.ValueRemoteATwoAAgentsAreUnavailableRetryThemManuallyFrom', failedCount))
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
          this.$message.success(result.message || ncfST('Frontend.AgentsManager.ATwoAAgentCardConnectionSucceeded'))
        } else {
          this.$message.error(result?.message || ncfST('Frontend.AgentsManager.ConnectionTestFailedAlternate'))
        }
      } catch (err) {
        this.$message.error(err?.message || ncfST('Frontend.AgentsManager.ConnectionTestFailedAlternate'))
      }
    },
    async testTaskRemoteAgent(participant) {
      try {
        const results = await this.testRemoteAgentConnections([participant?.id], { silent: true })
        const result = results[0]
        if (result?.success) {
          this.$message.success(result.message || ncfST('Frontend.AgentsManager.ATwoAAgentCardConnectionSucceeded'))
        } else {
          this.$message.error(result?.message || ncfST('Frontend.AgentsManager.ConnectionTestFailedAlternate'))
        }
      } catch (err) {
        this.$message.error(err?.message || ncfST('Frontend.AgentsManager.ConnectionTestFailedAlternate'))
      }
    },
    async setRemoteAgentEnable(item, enable) {
      try {
        const response = await serviceAM.post(
          `/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.Enable?id=${item.id}&enable=${enable}`,
          {})
        const data = response?.data ?? {}
        if (!data.success) throw new Error(data.errorMessage || data.data || ncfST('Frontend.AgentsManager.StatusUpdateFailed'))
        this.$message.success(data.data || ncfST('Frontend.AgentsManager.StatusUpdated'))
        await this.getRemoteAgentListData()
        if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
      } catch (err) {
        this.$message.error(err?.message || ncfST('Frontend.AgentsManager.StatusUpdateFailed'))
      }
    },
    deleteRemoteAgent(item) {
      this.$confirm(ncfST('Frontend.AgentsManager.DeleteRemoteATwoAAgentValueAgentsAlreadyInAGroup', item.name), ncfST('Frontend.AgentsManager.ConfirmDeletion'), { type: 'warning' })
        .then(async () => {
          try {
            const response = await serviceAM.post(
              `/api/Senparc.Xncf.AgentsManager/RemoteAgentAppService/Xncf.AgentsManager_RemoteAgentAppService.Delete?id=${item.id}`,
              {})
            const data = response?.data ?? {}
            if (!data.success) throw new Error(data.errorMessage || data.data || ncfST('Frontend.AgentsManager.DeleteFailed'))
            this.$message.success(data.data || ncfST('Frontend.AgentsManager.Deleted'))
            await this.getRemoteAgentListData()
            if (this.visible.drawerGroup) await this.getRemoteAgentListData('groupRemoteAgent')
          } catch (err) {
            this.$message.error(err?.message || ncfST('Frontend.AgentsManager.DeleteFailed'))
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

